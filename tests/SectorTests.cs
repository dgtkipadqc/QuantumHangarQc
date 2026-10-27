using System;using System.IO;using System.Linq;using System.Threading;using System.Collections.Generic;using System.Reflection;
using Eleon.Modding;using QuantumHangarQc;using QuantumHangarQc.Live;
class SectorTests {
 static string root;static int serial,checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static FakeApi New(){var a=new FakeApi{Root=Path.Combine(root,(++serial).ToString()),HostResponses=true,KeepListCache=true,RejectUpdate=true};
  Directory.CreateDirectory(Path.Combine(a.Root,"Shared","200"));File.WriteAllText(Path.Combine(a.Root,"Shared","200","0.area"),"test bytes: chest A slot0=123x15; chest B slot2=456x9");
  a.Mod=new HangarMod{RequestTimeout=150,ExportPoll=2,RestorePoll=2,StateTimeout=150,StatePoll=2,UiOpenTimeout=200,UiTimeout=1000};a.Mod.StartAt(a,a.Root);return a;}
 static void Run(FakeApi a,string cmd){
  ((Dictionary<int,DateTime>)typeof(HangarMod).GetField("lastCommand",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a.Mod)).Clear();a.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo{playerId=a.P.entityId,msg=cmd});var task=a.Mod.ActiveTask;var until=DateTime.UtcNow.AddSeconds(5);
  while(!task.IsCompleted&&DateTime.UtcNow<until){a.ReplyState();a.Mod.Game_Update();Thread.Sleep(2);}Check(task.IsCompleted,"command completed: "+cmd);task.GetAwaiter().GetResult();a.Mod.Game_Update();
  Check(!a.Calls.Contains(CmdId.Request_GlobalStructure_Update),"never sends blocking global update, including failure paths");}
 static void Main(string[] args){root=args[0];Directory.CreateDirectory(root);
  var round=New();Run(round,"qh:store:200");Check(round.Store().At(1,round.P.steamId).Status=="STORED","native absence overrides original still in host cache");Run(round,"qh:mark");Run(round,"qh:load:1");Check(round.Ships.Any(e=>e.id==900)&&round.Store().List(round.P.steamId).Count==0,"native presence finds restored ship missing from host cache");round.Mod.Game_Exit();
  foreach(string mode in new[]{"missing","incomplete","token","steam","client","player","sector","protocol","stale","future","duplicate","nan","null","oversized","session"}){
   var a=New();if(mode=="missing")a.NoSector=true;
   a.AfterSector=s=>{if(mode=="incomplete")s.Complete=false;if(mode=="token")s.Token="wrong";if(mode=="steam")s.Steam="76561198000000002";if(mode=="client")s.Client++;if(mode=="player")s.Player++;if(mode=="sector")s.Playfield="Other";if(mode=="protocol")s.Protocol=0;if(mode=="stale")s.CapturedUtc=DateTime.UtcNow.AddSeconds(-10);if(mode=="future")s.CapturedUtc=DateTime.UtcNow.AddSeconds(10);if(mode=="duplicate")s.Entities.Add(s.Entities[0]);if(mode=="nan")s.Entities[0].X=float.NaN;if(mode=="null")s.Entities=null;if(mode=="oversized")s.Entities=Enumerable.Repeat(s.Entities[0],2049).ToList();if(mode=="session")a.P.clientId++;};
   Run(a,"qh:store:200");Check(!a.Calls.Contains(CmdId.Request_Entity_Export)&&!a.Calls.Contains(CmdId.Request_Entity_Destroy)&&a.Store().List("76561198000000001").Count==0,"invalid sector stops before reservation/export: "+mode);Check(a.Ships.Any(e=>e.id==200),"original retained: "+mode);a.Mod.Game_Exit();
  }
  var obstacle=New();Run(obstacle,"qh:store:200");Run(obstacle,"qh:mark");obstacle.Ships.Add(new GlobalStructureInfo{id=300,name="new obstacle",pos=new PVector3{z=30}});Run(obstacle,"qh:load:1");Check(!obstacle.Calls.Contains(CmdId.Request_Entity_Spawn)&&obstacle.Store().At(1,obstacle.P.steamId).Status=="STORED","new obstacle absent from cache still blocks retrieval");obstacle.Mod.Game_Exit();
  var original=New();Run(original,"qh:store:200");Run(original,"qh:mark");original.Ships.Add(original.Ship);Run(original,"qh:load:1");Check(!original.Calls.Contains(CmdId.Request_NewEntityId),"reappeared original blocks allocation and spawn");original.Mod.Game_Exit();
  var missingAfter=New();Run(missingAfter,"qh:store:200");Run(missingAfter,"qh:mark");missingAfter.NoSector=true;Run(missingAfter,"qh:load:1");Check(!missingAfter.Calls.Contains(CmdId.Request_NewEntityId)&&missingAfter.Store().At(1,missingAfter.P.steamId).Status=="STORED","missing State at retrieval preserves archive");missingAfter.Mod.Game_Exit();
  Console.WriteLine("SECTOR PASS "+checks+"; simulated API, stale cache and fail-closed replies; actual server NOT_RUN.");
 }
}
