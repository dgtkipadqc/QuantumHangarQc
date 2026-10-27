using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using System.Reflection;
using Eleon.Modding;using QuantumHangarQc;using QuantumHangarQc.Live;using QuantumHangarQc.State;
// Uses the real Host and UiBridge with the minimal native surface in LanguageMenuTests.
class ConfirmationTests {
 static string root;static int serial,checks;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
 class Player:IPlayer {public int Id{get;set;}public string SteamId{get;set;}}
 class Field:IPlayfield {public string Name{get;set;}public Dictionary<int,IPlayer> Players{get;set;}}
 class App:IApplication {
  public DialogConfig Dialog;public Action<int,string,string,int,int> Callback;public int Player,Custom,Opens;public bool Reject;
  public bool ShowDialogBox(int player,DialogConfig config,Action<int,string,string,int,int> callback,int custom){Dialog=config;Callback=callback;Player=player;Custom=custom;Opens++;return !Reject;}
  public void Click(int button,int? player=null,string link=null){Callback(button,link,null,player??Player,Custom);}
 }
 class Case {
  public FakeApi Api;public App App;public UiBridge Bridge;public Field Field;public string Dir;public Task Task;public bool BridgeEnabled=true,Legacy;
  QuantumHangarQc.LegacyState.UiBridge old;
  public void Tick(){Api.ReplyState();Api.Mod.Game_Update();if(BridgeEnabled){if(Legacy){if(old==null)old=new QuantumHangarQc.LegacyState.UiBridge(App,s=>{});old.Tick(Dir,new IPlayfield[]{Field},DateTime.UtcNow);}else Bridge.Tick(Dir,new IPlayfield[]{Field},DateTime.UtcNow);}Thread.Sleep(2);}
  public void Start(string command){((Dictionary<int,DateTime>)typeof(HangarMod).GetField("lastCommand",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Api.Mod)).Clear();Api.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo{playerId=Api.P.entityId,msg=command});Task=Api.Mod.ActiveTask;}
  public void Open(string command){int before=App.Opens;Start(command);var until=DateTime.UtcNow.AddSeconds(3);while(App.Opens==before&&!Task.IsCompleted&&DateTime.UtcNow<until)Tick();Check(App.Opens==before+1,"native confirmation opens");}
  public void Finish(){var until=DateTime.UtcNow.AddSeconds(5);while(!Task.IsCompleted&&DateTime.UtcNow<until)Tick();Check(Task.IsCompleted,"operation completes");Task.GetAwaiter().GetResult();Api.Mod.Game_Update();}
  public HangarUiRequest Request(){return Transfer.Read<HangarUiRequest>(Directory.GetFiles(Dir,"*.request.xml").Single());}
  public void NoMutation(string label){Check(!Api.Calls.Contains(CmdId.Request_Entity_Export)&&!Api.Calls.Contains(CmdId.Request_Entity_Destroy),label);Check(Api.Store().List(Api.P.steamId).Count==0,"no reservation before confirmation");Api.Mod.Game_Exit();}
 }
 static Case New(){
  var c=new Case();c.Api=new FakeApi{Root=Path.Combine(root,(++serial).ToString()),NativeConfirmEnabled=false,HostResponses=true,ListUpdateReply=true,KeepListCache=true,RejectDialog=true};
  Directory.CreateDirectory(Path.Combine(c.Api.Root,"Shared","200"));File.WriteAllText(Path.Combine(c.Api.Root,"Shared","200","0.area"),"test bytes: chest A slot0=123x15; chest B slot2=456x9");
  c.Api.Mod=new HangarMod{RequestTimeout=500,DialogTimeout=200,ExportPoll=2,StateTimeout=500,StatePoll=2,UiOpenTimeout=200,UiTimeout=700};c.Api.Mod.StartAt(c.Api,c.Api.Root);
  c.Dir=Path.Combine(c.Api.Root,"Mods","QuantumHangarQc","HangarUI");Directory.CreateDirectory(c.Dir);
  c.App=new App();c.Bridge=new UiBridge(c.App,s=>{});c.Field=new Field{Name=c.Api.P.playfield,Players=new Dictionary<int,IPlayer>{{50,new Player{Id=50,SteamId=c.Api.P.steamId}}}};return c;
 }
 static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);
  var normal=New();normal.Open("qh:store:200");
  Check(normal.Api.SectorReplies==1&&!normal.Api.Calls.Contains(CmdId.Request_GlobalStructure_Update),"fresh native preview without blocking update");
  Check(!normal.Api.Calls.Contains(CmdId.Request_ShowDialog_SinglePlayer),"store does not use broken legacy dialog API");
  Check(normal.App.Dialog.ButtonTexts.Length==2&&normal.App.Dialog.ButtonTexts[0]=="Confirm storage"&&normal.App.Dialog.ButtonTexts[1]=="Cancel","translated explicit confirmation buttons");
  Check(normal.App.Dialog.ButtonIdxForEsc==1&&normal.App.Dialog.ButtonIdxForEnter==-1&&!normal.App.Dialog.CloseOnLinkClick,"escape cancels and enter/link cannot confirm");
  Check(normal.App.Dialog.BodyText.Contains("Test SV")&&normal.App.Dialog.BodyText.Contains("200"),"exact ship and ID presented");
  var token=normal.Request().Token;normal.App.Click(0,999);Check(!File.Exists(Path.Combine(normal.Dir,token+".reply.xml")),"foreign player callback ignored");
  normal.App.Click(0);normal.App.Click(0);normal.Finish();
  Check(normal.Api.Store().At(1,normal.Api.P.steamId).Status=="STORED","native click completes store");
  Check(normal.Api.Calls.Count(x=>x==CmdId.Request_Entity_Export)==1&&normal.Api.Calls.Count(x=>x==CmdId.Request_Entity_Destroy)==1,"duplicate click cannot duplicate transaction");
  Check(normal.Api.SectorReplies==4&&!normal.Api.Calls.Contains(CmdId.Request_GlobalStructure_Update),"native preview, confirmation, before-removal and absence checks retained");
  Check(Directory.GetFiles(normal.Dir).Length==0,"UI files cleaned");
  normal.Api.P.pos=new PVector3{x=200};normal.Start("qh:mark");normal.Finish();normal.Api.P.pos=new PVector3{x=160};normal.Open("qh:load:1");Check(normal.App.Dialog.ButtonTexts[0]=="Confirm retrieval","retrieval uses native confirmation too");normal.App.Click(0);normal.Finish();
  Check(normal.Api.Ships.Count(x=>x.id==900)==1&&!normal.Api.Calls.Contains(CmdId.Request_ShowDialog_SinglePlayer),"store and retrieve succeed with legacy dialogs unavailable");normal.Api.Mod.Game_Exit();
  foreach(int button in new[]{1,-1,2}){var c=New();c.Open("qh:store:200");c.App.Click(button);c.Finish();c.NoMutation("cancel/close/unexpected button cannot export");}
  var link=New();link.Open("qh:store:200");link.App.Click(0,null,"qh:any-link");link.Finish();link.NoMutation("link cannot confirm");
  var timeout=New();timeout.Open("qh:store:200");var callback=timeout.App.Callback;int custom=timeout.App.Custom;timeout.Finish();callback(0,null,null,50,custom);timeout.NoMutation("timeout and late click cannot export");
  var replay=New();replay.Open("qh:store:200");callback=replay.App.Callback;custom=replay.App.Custom;string old=replay.Request().Token;replay.App.Click(1);replay.Finish();replay.Open("qh:store:200");Check(replay.Request().Token!=old,"confirmation token never reused");callback(0,null,null,50,custom);Check(!Directory.GetFiles(replay.Dir,"*.reply.xml").Any(),"old callback cannot answer new confirmation");replay.App.Click(1);replay.Finish();replay.NoMutation("replayed confirmation remains inert");
  foreach(string mode in new[]{"token","player","client","steam","time","missing-explicit"}){
   var c=New();c.Open("qh:store:200");var r=c.Request();var a=new HangarUiReply{Token=r.Token,Player=r.Player,Client=r.Client,Steam=r.Steam,Confirmed=true,CapturedUtc=DateTime.UtcNow};
   if(mode=="token")a.Token=Guid.NewGuid().ToString("N");if(mode=="player")a.Player++;if(mode=="client")a.Client++;if(mode=="steam")a.Steam="76561198000000009";if(mode=="time")a.CapturedUtc=DateTime.UtcNow.AddMinutes(-1);if(mode=="missing-explicit")a.Confirmed=false;
   Transfer.Write(Path.Combine(c.Dir,r.Token+".reply.xml"),a);c.Finish();c.NoMutation("invalid reply rejected: "+mode);
  }
  foreach(string mode in new[]{"client","steam","power","owner"}){
   var c=New();c.Open("qh:store:200");if(mode=="client")c.Api.P.clientId++;if(mode=="steam")c.Api.P.steamId="76561198000000009";if(mode=="power")c.Api.LivePower=true;if(mode=="owner")c.Api.Ship.factionId=999;
   c.App.Click(0);c.Finish();c.NoMutation("revalidation after click: "+mode);
  }
  foreach(string mode in new[]{"missing","legacy","rejected"}){
   var c=New();c.BridgeEnabled=mode!="missing";c.Legacy=mode=="legacy";c.App.Reject=mode=="rejected";c.Start("qh:store:200");c.Finish();Check(c.Api.Logs.Any(x=>x.Contains("code=error.confirmation")),"confirmation failure has actionable diagnostic: "+mode);c.NoMutation("unavailable bridge cannot confirm: "+mode);
  }
  // A ship edited before storage is valid. Later writes must still block removal.
  var edited=New();string changed="edited warp drive before storage";
  File.WriteAllText(Path.Combine(edited.Api.Root,"Shared","200","0.area"),changed);
  edited.Open("qh:store:200");edited.App.Click(0);edited.Finish();
  var entry=edited.Api.Store().At(1,edited.Api.P.steamId);
  Check(entry.Status=="STORED"&&File.ReadAllText(Path.Combine(edited.Api.Store().DirectoryFor(entry),"Shared","0.area"))==changed,"pre-existing ship edit archived unchanged");edited.Api.Mod.Game_Exit();
  foreach(string mode in new[]{"content","added","missing","timestamp"}) {
   var c=New();int positions=0;string file=Path.Combine(c.Api.Root,"Shared","200","0.area");
   c.Api.AfterPosition=()=>{if(++positions!=3)return;
    if(mode=="content")File.AppendAllText(file,"changed after copy");
    if(mode=="added")File.WriteAllText(Path.Combine(Path.GetDirectoryName(file),"ents.dat"),"new file");
    if(mode=="missing")File.Delete(file);
    if(mode=="timestamp")File.SetLastWriteTimeUtc(file,DateTime.UtcNow.AddMinutes(1));
   };
   c.Open("qh:store:200");c.App.Click(0);c.Finish();var e=c.Api.Store().At(1,c.Api.P.steamId);
   if(mode=="timestamp")Check(e.Status=="STORED","timestamp alone does not block an unchanged copy");
   else {
    Check(e.Status=="QUARANTINE"&&e.StoreStage=="ARCHIVED","source change quarantines before removal: "+mode);
    Check(!c.Api.Calls.Contains(CmdId.Request_Entity_Destroy)&&c.Api.Ships.Any(x=>x.id==200),"source change never deletes ship: "+mode);
    Check(c.Api.Logs.Any(x=>x.Contains("SOURCE_CHANGED")&&x.Contains("reason="+mode)&&x.Contains("file="+(mode=="added"?"ents.dat":"0.area"))),"specific changed file logged: "+mode);
    Check(e.Files.All(f=>File.Exists(Path.Combine(c.Api.Store().DirectoryFor(e),f.Path))&&Disk.Hash(Path.Combine(c.Api.Store().DirectoryFor(e),f.Path))==f.Sha256),"quarantined recovery bytes match manifest: "+mode);
   }
   c.Api.Mod.Game_Exit();
  }
  // Shared native callback must dispatch by custom value as well as player.
  var multi=New();var r1=new HangarUiRequest{Token=Guid.NewGuid().ToString("N"),Player=50,Client=7,Steam=multi.Api.P.steamId,Playfield=multi.Field.Name,Language="en",Purpose="confirm",Protocol=4,Message="First ship",Positive="Store",CreatedUtc=DateTime.UtcNow};
  Transfer.Write(Path.Combine(multi.Dir,r1.Token+".request.xml"),r1);multi.Bridge.Tick(multi.Dir,new IPlayfield[]{multi.Field},DateTime.UtcNow);var cb1=multi.App.Callback;int key1=multi.App.Custom;
  var r2=new HangarUiRequest{Token=Guid.NewGuid().ToString("N"),Player=51,Client=8,Steam="76561198000000002",Playfield=multi.Field.Name,Language="en",Purpose="confirm",Protocol=4,Message="Second ship",Positive="Store",CreatedUtc=DateTime.UtcNow};multi.Field.Players.Add(51,new Player{Id=51,SteamId=r2.Steam});
  Transfer.Write(Path.Combine(multi.Dir,r2.Token+".request.xml"),r2);multi.Bridge.Tick(multi.Dir,new IPlayfield[]{multi.Field},DateTime.UtcNow);multi.App.Click(1);cb1(0,null,null,50,key1);
  Check(Transfer.Read<HangarUiReply>(Path.Combine(multi.Dir,r1.Token+".reply.xml")).Confirmed,"first player confirmation survives second dialog");Check(Transfer.Read<HangarUiReply>(Path.Combine(multi.Dir,r2.Token+".reply.xml")).Cancelled,"second player cancellation is independent");int opens=multi.App.Opens;File.Delete(Path.Combine(multi.Dir,r1.Token+".reply.xml"));multi.Bridge.Tick(multi.Dir,new IPlayfield[]{multi.Field},DateTime.UtcNow);Check(multi.App.Opens==opens&&!File.Exists(Path.Combine(multi.Dir,r1.Token+".reply.xml")),"consumed reply cannot reopen an already answered request");multi.Api.Mod.Game_Exit();
  Console.WriteLine("PASS "+checks+" native confirmation assertions; simulated API, real Host/UiBridge and files. No game test.");
 }
}
