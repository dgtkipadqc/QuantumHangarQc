using System;using System.IO;using System.Linq;using System.Threading;using System.Collections.Generic;using System.Reflection;
using Eleon.Modding;using QuantumHangarQc;
class MarkerTests {
 static string root;static int serial,checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
 static double Distance(PVector3 a,PVector3 b){return Math.Sqrt(Math.Pow((double)a.x-b.x,2)+Math.Pow((double)a.y-b.y,2)+Math.Pow((double)a.z-b.z,2));}
 static FakeApi New(){
  var a=new FakeApi{Root=Path.Combine(root,(++serial).ToString()),HostResponses=true,ListUpdateReply=true};
  Directory.CreateDirectory(Path.Combine(a.Root,"Shared","200"));File.WriteAllText(Path.Combine(a.Root,"Shared","200","0.area"),"test bytes: chest A slot0=123x15; chest B slot2=456x9");
  Start(a);return a;
 }
 static void Start(FakeApi a){a.Mod=new HangarMod{RequestTimeout=500,DialogTimeout=300,ExportPoll=2,StateTimeout=500,StatePoll=2,UiOpenTimeout=300,UiTimeout=1000};a.Mod.StartAt(a,a.Root);}
 static void Run(FakeApi a,string command){
  ((Dictionary<int,DateTime>)typeof(HangarMod).GetField("lastCommand",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a.Mod)).Clear();
  a.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo{playerId=a.P.entityId,msg=command});var task=a.Mod.ActiveTask;var until=DateTime.UtcNow.AddSeconds(5);
  while(!task.IsCompleted&&DateTime.UtcNow<until){a.ReplyState();a.Mod.Game_Update();Thread.Sleep(2);}Check(task.IsCompleted,"operation completed: "+command);task.GetAwaiter().GetResult();a.Mod.Game_Update();
 }
 static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);
  float[] headings={0,90,180,270,-90,450};float[] dx={0,30,0,-30,-30,30};float[] dz={30,0,-30,0,0,0};
  for(int i=0;i<headings.Length;i++) {
   var p=new PlayerInfo{pos=new PVector3{x=-7590.4f,y=-1221.9f,z=-7322.7f},rot=new PVector3{x=89,y=headings[i],z=75}};var point=HangarMod.ForwardMark(p);
   Check(Math.Abs(point.x-p.pos.x-dx[i])<0.01&&Math.Abs(point.z-p.pos.z-dz[i])<0.01,"correct cardinal forward direction: "+headings[i]);
   Check(point.y==p.pos.y&&Math.Abs(Distance(point,p.pos)-30)<0.01,"horizontal 30m offset preserves altitude despite pitch/roll");
  }
  foreach(float yaw in new[]{0f,45f,90f,180f,270f,-45f}) {
   var a=New();Run(a,"qh:store:200");a.P.pos=new PVector3{x=1000.25f,y=-50.75f,z=-8000.3f};a.P.rot=new PVector3{x=80,y=yaw,z=15};var before=a.P.pos;
   Run(a,"qh:mark");var gps=a.GpsRequests.Last(x=>x.Activate);var target=new PVector3{x=gps.X,y=gps.Y,z=gps.Z};
   Check(Math.Abs(Distance(before,target)-30)<=0.01,"actual marker at 30m, including diagonal float rounding");
   a.P.rot=new PVector3{y=yaw+180};Run(a,"qh:load:1");Check(a.Ships.Count(x=>x.id==900)==1,"standing still retrieves without backing away: "+yaw);
   Check(Distance(a.P.pos,before)==0&&Distance(a.Ships.Single(x=>x.id==900).pos,target)<0.01,"player unchanged and spawn uses recorded point");a.Mod.Game_Exit();
  }
  var turn=New();Run(turn,"qh:store:200");Run(turn,"qh:mark");turn.P.rot=new PVector3{y=90};Run(turn,"qh:mark");
  Check(turn.GpsRequests.Count(x=>!x.Activate)==1&&Math.Abs(turn.GpsRequests.Last().X-30)<0.01,"new heading replaces old marker and removes previous GPS");Run(turn,"qh:load:1");Check(turn.Ships.Any(x=>x.id==900),"replacement marker retrieves while stationary");turn.Mod.Game_Exit();
  var restart=New();Run(restart,"qh:store:200");Run(restart,"qh:mark");restart.Mod.Game_Exit();Start(restart);Run(restart,"qh:load:1");
  Check(!restart.Calls.Contains(CmdId.Request_Entity_Spawn)&&restart.Store().At(1,restart.P.steamId).Status=="STORED","restart refuses stale marker without losing archive");Run(restart,"qh:mark");Run(restart,"qh:load:1");Check(restart.Ships.Any(x=>x.id==900),"new forward marker after restart retrieves");restart.Mod.Game_Exit();
  var obstacle=New();Run(obstacle,"qh:store:200");Run(obstacle,"qh:mark");obstacle.Ships.Add(new GlobalStructureInfo{id=300,pos=new PVector3{z=30}});Run(obstacle,"qh:load:1");
  Check(!obstacle.Calls.Contains(CmdId.Request_NewEntityId)&&obstacle.Store().At(1,obstacle.P.steamId).Status=="STORED","obstacle still blocks new ID and spawn at forward point");obstacle.Mod.Game_Exit();
  foreach(float invalid in new[]{float.NaN,float.PositiveInfinity}) {
   var a=New();a.P.rot=new PVector3{y=invalid};Run(a,"qh:mark");Check(a.GpsRequests.Count==0&&a.Logs.Any(x=>x.Contains("code=error.mark_direction")),"invalid heading cannot create a misleading marker");a.Mod.Game_Exit();
  }
  Console.WriteLine("MARKER PASS "+checks+"; simulated player heading, GPS and retrieval; real game orientation NOT_RUN.");
 }
}
