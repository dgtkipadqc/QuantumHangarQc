using System;using System.IO;using System.Linq;using System.Collections.Generic;using Eleon.Modding;using QuantumHangarQc.Live;using QuantumHangarQc.State;using RecycleTests;
class QhStateTests {
 static int checks;static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 class S:StubIStructure {public List<IStructure> Children=new List<IStructure>();public List<IPlayer> Passengers=new List<IPlayer>();public override List<IStructure> GetDockedVessels(){return Children;}public override List<IPlayer> GetPassengers(){return Passengers;}}
 class App:StubIApplication {public string Root;public override string GetPathFor(AppFolder folder){return Root;}}
 static MarkerRequest GpsRequest(string name,DateTime now,bool activate=true){return new MarkerRequest{Token=Guid.NewGuid().ToString("N"),Name=name,Steam="76561198000000001",Player=50,Playfield="Test",X=995,Y=113,Z=-279,Activate=activate,CreatedUtc=now,ExpiresUtc=now.AddMinutes(5)};}
 static MarkerReply GpsRun(GpsBridge bridge,string dir,IPlayfield pf,MarkerRequest request,DateTime now){
  Transfer.Write(Path.Combine(dir,request.Token+".request.xml"),request);bridge.Tick(dir,new[]{pf},now);
  return Transfer.Read<MarkerReply>(Path.Combine(dir,request.Token+".reply.xml"));
 }
 static void GpsTests(string root,StubIPlayfield pf,StubIPlayer p){
  string dir=Path.Combine(root,"gps-tests");Directory.CreateDirectory(dir);DateTime now=DateTime.UtcNow;
  var sent=new List<string>();var logs=new List<string>();
  Func<GpsBridge> make=()=>new GpsBridge(logs.Add){Check=id=>{if(id!=50)throw new Exception("wrong player");},Send=(id,key)=>sent.Add(id+":"+key)};
  var bridge=make();string name="QH_SORTIE_"+Guid.NewGuid().ToString("N");var create=GpsRequest(name,now);
  // Native dictionary keys need not equal player entity IDs.
  pf.Players.Clear();pf.Players.Add(7,p);
  Check(GpsRun(bridge,dir,pf,create,now).Success&&sent.Count==0,"arm cleanup for actual entity ID, no deletion at creation");
  bridge.Tick(dir,new[]{pf},now.AddSeconds(7));Check(sent.Count==0,"GPS remains before restoration");
  var remove=GpsRequest(name,now.AddSeconds(10),false);remove.RemoveDelaySeconds=8;
  Check(GpsRun(bridge,dir,pf,remove,now.AddSeconds(10)).Success,"accept delayed removal");
  bridge=make();bridge.Tick(dir,new[]{pf},now.AddSeconds(17.9));Check(sent.Count==0,"persistent deadline survives bridge restart, not premature");
  bridge.Tick(dir,new[]{pf},now.AddSeconds(18));
  Check(sent.SequenceEqual(new[]{"50:"+MarkerRules.MapId(create)})&&!File.Exists(Path.Combine(dir,name+".lease.xml")),"one targeted removal at eight seconds and lease retired");
  bridge.Tick(dir,new[]{pf},now.AddSeconds(30));Check(sent.Count==1,"no repeated deletion after success");
  string exp="QH_SORTIE_"+Guid.NewGuid().ToString("N");var expRequest=GpsRequest(exp,now);GpsRun(bridge,dir,pf,expRequest,now);
  bridge.Tick(dir,new[]{pf},now.AddSeconds(299));Check(sent.Count==1,"five minute marker still active");
  bridge.Tick(dir,new[]{pf},now.AddMinutes(5));Check(sent.Count==2,"server timer clears unused marker without host activity");
  string foreign="QH_SORTIE_"+Guid.NewGuid().ToString("N");var fr=GpsRequest(foreign,now);p.SteamId="76561198000000002";
  Check(!GpsRun(bridge,dir,pf,fr,now).Success&&sent.Count==2,"Steam mismatch cannot arm cleanup");p.SteamId=fr.Steam;
  string old="QH_SORTIE_"+Guid.NewGuid().ToString("N"),fresh="QH_SORTIE_"+Guid.NewGuid().ToString("N");
  GpsRun(bridge,dir,pf,GpsRequest(old,now),now);var replacement=GpsRequest(fresh,now.AddSeconds(1));GpsRun(bridge,dir,pf,replacement,now.AddSeconds(1));
  Check(sent.Count==3&&!File.Exists(Path.Combine(dir,old+".lease.xml"))&&File.Exists(Path.Combine(dir,fresh+".lease.xml")),"same-coordinate replacement retires old deadline before plotting new GPS");
  GpsRun(bridge,dir,pf,GpsRequest(old,now.AddSeconds(2),false),now.AddSeconds(2));Check(sent.Count==3,"late old removal cannot remove replacement");
  var bad=GpsRequest(fresh,now.AddSeconds(3),false);bad.X+=1;Check(!GpsRun(bridge,dir,pf,bad,now.AddSeconds(3)).Success&&sent.Count==3,"changed coordinate deletion rejected");
  pf.Name="Other";bridge.Tick(dir,new[]{pf},now.AddMinutes(6));Check(sent.Count==3,"never remove position key on another playfield");pf.Name="Test";
  p.SteamId="76561198000000002";bridge.Tick(dir,new[]{pf},now.AddMinutes(6));Check(sent.Count==3,"never remove another Steam account's marker");p.SteamId=replacement.Steam;
  bridge.Tick(dir,new[]{pf},now.AddMinutes(6));Check(sent.Count==4,"retained expired marker clears upon valid return");
  var zeros=GpsRequest("QH_SORTIE_"+Guid.NewGuid().ToString("N"),now);zeros.X=zeros.Y=zeros.Z=0;
  Check(!GpsRun(bridge,dir,pf,zeros,now).Success,"zero map key cannot be sent as a removal");
 }
 static void Main(string[] args){
  string root=args[0];Directory.CreateDirectory(root);var p=new StubIPlayer{Id=50,SteamId="76561198000000001"};
  var s=new S{Id=200,IsReady=true,IsPowered=false,BlockCount=45,DeviceCount=12};
  var entity=new StubIEntity{Id=200,Type=EntityType.CV,Structure=s,Faction=new FactionData{Group=FactionGroup.Player,Id=50}};
  var pf=new StubIPlayfield{Name="Test",Players=new Dictionary<int,IPlayer>{{50,p}},Entities=new Dictionary<int,IEntity>{{200,entity}}};
  var request=new StateRequest{Token=Guid.NewGuid().ToString("N"),Playfield="Test",Entity=200,Player=50,Steam=p.SteamId,CreatedUtc=DateTime.UtcNow};
  var r=StateMod.Capture(request,pf);Check(r.Ready&&!r.Powered&&r.Pilot==0&&r.Occupants.Count==0,"empty off ship captured");Check(r.OwnerGroup==1&&r.OwnerId==50&&r.BlockCount==45&&r.DeviceCount==12,"live identity statistics captured");
  s.IsPowered=true;s.Pilot=p;p.CurrentStructure=s;r=StateMod.Capture(request,pf);Check(r.Powered&&r.Pilot==50&&r.Occupants.SequenceEqual(new[]{50}),"power and cockpit read from live interfaces");
  s.Pilot=null;s.IsPowered=false;p.CurrentStructure=null;p.DrivingEntity=entity;r=StateMod.Capture(request,pf);Check(r.Occupants.SequenceEqual(new[]{50}),"driving occupant captured independently of pilot");p.DrivingEntity=null;
  var passenger=new StubIPlayer{Id=77};s.Passengers.Add(passenger);r=StateMod.Capture(request,pf);Check(r.Occupants.Contains(77),"passengers captured");s.Passengers.Clear();
  var child=new StubIEntity{Id=300,DockedTo=200};pf.Entities.Add(300,child);r=StateMod.Capture(request,pf);Check(r.Docked.SequenceEqual(new[]{300}),"reverse live docking captured");pf.Entities.Remove(300);
  entity.DockedTo=100;r=StateMod.Capture(request,pf);Check(r.DockedTo==100,"carrier link captured");entity.DockedTo=0;
  p.SteamId="other";r=StateMod.Capture(request,pf);Check(r.Ready&&r.Steam==request.Steam,"observer correlation is not native Steam authentication");
  p.SteamId=null;r=StateMod.Capture(request,pf);Check(r.Ready&&r.RequesterSeen,"native Steam unavailable does not block observation");p.SteamId=request.Steam;
  pf.Players.Clear();pf.Players.Add(7,p);r=StateMod.Capture(request,pf);Check(r.Ready&&r.RequesterSeen&&r.PlayersSeen==1,"requester presence uses entity ID values not dictionary key");
  pf.Players.Clear();r=StateMod.Capture(request,pf);Check(r.Ready&&!r.RequesterSeen&&r.PlayersSeen==0,"requester absent does not block read-only ship observation");
  s.Passengers.Add(passenger);r=StateMod.Capture(request,pf);Check(r.Ready&&r.Occupants.Contains(77),"structure passengers remain detected without requester in player dictionary");s.Passengers.Clear();
  pf.Players.Add(99,passenger);passenger.CurrentStructure=s;r=StateMod.Capture(request,pf);Check(r.Occupants.Contains(77),"other players at structure detected independently of requester");passenger.CurrentStructure=null;
  pf.Players.Clear();pf.Players.Add(50,p);
  request.Playfield="Other";r=StateMod.Capture(request,pf);Check(!r.Ready&&r.Error.Contains("Secteur"),"wrong playfield still rejected");request.Playfield=pf.Name;
  pf.Entities.Remove(200);r=StateMod.Capture(request,pf);Check(!r.Ready&&r.Error.Contains("absent"),"missing source fails closed");pf.Entities.Add(200,entity);
  entity.IsProxy=true;r=StateMod.Capture(request,pf);Check(!r.Ready,"proxy not ready");entity.IsProxy=false;
  var app=new App{Root=root,Mode=ApplicationMode.PlayfieldServer};var api=new StubIModApi{Application=app};var mod=new StateMod();mod.Init(api);app.Raise_OnPlayfieldLoaded(pf);
  string dir=Path.Combine(root,"Mods","QuantumHangarQc","LiveState");Directory.CreateDirectory(dir);Transfer.Write(Path.Combine(dir,request.Token+".request.xml"),request);app.Raise_Update();
  r=Transfer.Read<StateReply>(Path.Combine(dir,request.Token+".reply.xml"));Check(r.Token==request.Token&&r.Ready&&!r.Powered,"actual helper lifecycle and file response");Check(s.BlockCount==45&&s.DeviceCount==12&&s.IsPowered==false,"observer leaves structure unchanged");mod.Shutdown();
  GpsTests(root,pf,p);
  Console.WriteLine("PASS "+checks+" native observer assertions on mocked engine interfaces.");
 }
}
