using System;using System.IO;using System.Linq;using System.Threading;using System.Collections.Generic;using System.Reflection;
using Eleon.Modding;using QuantumHangarQc;
class RestoreTests {
 static string root;static int serial,checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static FakeApi New(){var a=new FakeApi{Root=Path.Combine(root,(++serial).ToString()),HostResponses=true,ListUpdateReply=true,UiEnabled=true,UiCancel=true};
  Directory.CreateDirectory(Path.Combine(a.Root,"Shared","200"));File.WriteAllText(Path.Combine(a.Root,"Shared","200","0.area"),"test bytes: chest A slot0=123x15; chest B slot2=456x9");
  a.Mod=new HangarMod{RequestTimeout=150,DialogTimeout=300,ExportPoll=2,RestorePoll=2,StateTimeout=150,StatePoll=2,UiOpenTimeout=300,UiTimeout=1000};a.Mod.StartAt(a,a.Root);return a;}
 static void Run(FakeApi a,string command){
  ((Dictionary<int,DateTime>)typeof(HangarMod).GetField("lastCommand",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a.Mod)).Clear();
  a.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo{playerId=a.P.entityId,msg=command});var task=a.Mod.ActiveTask;var until=DateTime.UtcNow.AddSeconds(5);
  while(!task.IsCompleted&&DateTime.UtcNow<until){a.ReplyState();a.Mod.Game_Update();Thread.Sleep(2);}Check(task.IsCompleted,"completed "+command);task.GetAwaiter().GetResult();a.Mod.Game_Update();}
 static FakeApi Quarantine(){var a=New();Run(a,"qh:store:200");Run(a,"qh:mark");a.DropSpawnAck=true;Run(a,"qh:load:1");Check(a.Store().At(1,a.P.steamId).Status=="QUARANTINE"&&a.Ships.Any(x=>x.id==900),"lost acknowledgement with live ship");return a;}
 static Dictionary<string,string> Snapshot(string path){return Directory.GetFiles(path,"*",SearchOption.AllDirectories).ToDictionary(p=>p.Substring(path.Length),Disk.Hash);}
 static void Same(Dictionary<string,string> expected,string path){var actual=Snapshot(path);Check(expected.Count==actual.Count&&expected.All(x=>actual.ContainsKey(x.Key)&&actual[x.Key]==x.Value),"all bytes preserved");}
 static void ListReadOnly(FakeApi a){int start=a.Calls.Count;Run(a,"qh:list");Check(!a.Calls.Skip(start).Any(c=>c==CmdId.Request_Entity_Spawn||c==CmdId.Request_Entity_Destroy||c==CmdId.Request_Entity_Export||c==CmdId.Request_NewEntityId),"list never replays world mutation");}
 static void Main(string[] args){root=args[0];Directory.CreateDirectory(root);
  var global=New();Run(global,"qh:store:200");Run(global,"qh:mark");global.GlobalRestoredStatsOnly=true;Run(global,"qh:load:1");
  Check(global.Store().List(global.P.steamId).Count==0&&global.RestoreStateReplies==2,"native counters accept restored ship despite different global counts");Check(global.Calls.Count(c=>c==CmdId.Request_Entity_Spawn)==1,"exactly one spawn");global.Mod.Game_Exit();
  var delayed=New();Run(delayed,"qh:store:200");Run(delayed,"qh:mark");delayed.RestoreUnreadyReplies=3;Run(delayed,"qh:load:1");
  Check(delayed.Store().List(delayed.P.steamId).Count==0&&delayed.RestoreStateReplies==5,"waits for readiness and two matching snapshots");delayed.Mod.Game_Exit();
  var mismatch=New();Run(mismatch,"qh:store:200");Run(mismatch,"qh:mark");mismatch.BadRestoredStats=true;Run(mismatch,"qh:load:1");
  Check(mismatch.Store().At(1,mismatch.P.steamId).Status=="QUARANTINE"&&mismatch.RestoreStateReplies==6,"persistent native mismatch retained after bounded checks");Check(mismatch.Logs.Any(l=>l.Contains("expectedBlocks=45 actualBlocks=1")),"specific expected and actual counters logged");mismatch.Mod.Game_Exit();
  var success=Quarantine();var e=success.Store().At(1,success.P.steamId);string archive=success.Store().DirectoryFor(e);var saved=Snapshot(archive);string live=Path.Combine(success.Root,"Shared");var liveBytes=Snapshot(live);
  ListReadOnly(success);Check(success.Store().List(success.P.steamId).Count==0,"existing quarantine frees slot");string retired=Path.Combine(success.Store().Root,"Retired",e.ArchiveId);Same(saved,retired);Same(liveBytes,live);Check(!Disk.Read<Entry>(Path.Combine(retired,"Manifest.xml")).CargoVerified,"never claims engine cargo verification");
  int replies=success.RestoreStateReplies;ListReadOnly(success);Check(replies==success.RestoreStateReplies,"idempotent list does not check retired archive again");Same(saved,retired);success.Mod.Game_Exit();
  foreach(string fault in new[]{"owner","type","blocks","devices","sector","missing","original","corrupt","duplicate","session","token","manifest","unready","state_missing"}) {
   var a=Quarantine();var entry=a.Store().At(1,a.P.steamId);var ship=a.Ships.Single(x=>x.id==900);
   if(fault=="owner")ship.factionId=51;if(fault=="type")ship.type=3;if(fault=="blocks")ship.cntBlocks--;if(fault=="devices")ship.cntDevices--;
   a.Ships[a.Ships.FindIndex(x=>x.id==900)]=ship;
   if(fault=="sector")a.ListSector="AnotherSector";if(fault=="missing")a.Ships.Clear();if(fault=="original")a.Ships.Add(a.Ship);
   if(fault=="corrupt")File.AppendAllText(Path.Combine(a.Store().DirectoryFor(entry),"Export.dat"),"bad");
   if(fault=="duplicate"){var duplicate=Disk.Read<Entry>(Path.Combine(a.Store().DirectoryFor(entry),"Manifest.xml"));duplicate.ArchiveId=Guid.NewGuid().ToString("N");duplicate.PersonalSlot=2;duplicate.Slot=2;Directory.CreateDirectory(a.Store().DirectoryFor(duplicate));a.Store().Save(duplicate);}
   if(fault=="session")a.AfterRestoreState=()=>a.P.clientId++;
   if(fault=="token")a.WrongStateToken=true;
   if(fault=="manifest")a.AfterRestoreState=()=>{entry.Detail="external change";a.Store().Save(entry);};
   if(fault=="unready")a.RestoreUnreadyReplies=100;if(fault=="state_missing")a.NoState=true;
   var bytes=Snapshot(Path.Combine(a.Root,"Shared"));ListReadOnly(a);Check(a.Store().At(1,entry.SteamId).Status=="QUARANTINE","retains quarantine for "+fault);Check(Directory.Exists(a.Store().DirectoryFor(entry)),"recovery archive retained for "+fault);Same(bytes,Path.Combine(a.Root,"Shared"));a.Mod.Game_Exit();
  }
  Console.WriteLine("RESTORE PASS "+checks+"; simulated API and archive bytes; real engine cargo NOT_RUN.");
 }
}
