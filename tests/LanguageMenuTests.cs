using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Threading;using System.Threading.Tasks;using System.Reflection;
using Eleon.Modding;using QuantumHangarQc;using QuantumHangarQc.Live;using QuantumHangarQc.State;using QuantumHangarQc.Localization;
// Minimal native dialog surface; production UiBridge.cs is compiled unchanged into this harness.
namespace Eleon.Modding {
 public class DialogConfig {public string TitleText,BodyText;public bool CloseOnLinkClick;public string[] ButtonTexts;public int ButtonIdxForEsc,ButtonIdxForEnter,MaxChars;}
 public interface IApplication {bool ShowDialogBox(int player,DialogConfig config,Action<int,string,string,int,int> callback,int custom);}
 public interface IPlayer {int Id{get;}string SteamId{get;}}
 public interface IPlayfield {string Name{get;}Dictionary<int,IPlayer> Players{get;}}
}
class LanguageMenuTests {
 static int checks,serial;static string root;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 class Player:IPlayer {public int Id{get;set;}public string SteamId{get;set;}}
 class Field:IPlayfield {public string Name{get;set;}public Dictionary<int,IPlayer> Players{get;set;}}
 class App:IApplication {
  public DialogConfig Dialog;public Action<int,string,string,int,int> Callback;public int Player,Custom,Opens;public bool Reject,Throw;
  public bool ShowDialogBox(int p,DialogConfig c,Action<int,string,string,int,int> action,int custom){Player=p;Dialog=c;Callback=action;Custom=custom;Opens++;if(Throw)throw new InvalidOperationException("test unavailable");return !Reject;}
  public void Click(string link,int? player=null){Callback(-1,link,null,player??Player,Custom);}
  public void Close(){Callback(0,null,null,Player,Custom);}
 }
 class Case {
  public FakeApi Api;public App App;public UiBridge Bridge;public Field Field;public Task Task;public string Dir,Pref;public bool NoBridge;
  public void Tick(){Api.Mod.Game_Update();if(!NoBridge)Bridge.Tick(Dir,new IPlayfield[]{Field},DateTime.UtcNow);Thread.Sleep(2);}
  public void Finish(){var until=DateTime.UtcNow.AddSeconds(3);while(!Task.IsCompleted&&DateTime.UtcNow<until)Tick();Check(Task.IsCompleted,"menu task completes");Task.GetAwaiter().GetResult();Api.Mod.Game_Update();}
  public HangarUiRequest Request(){return Transfer.Read<HangarUiRequest>(Directory.GetFiles(Dir,"*.request.xml").Single());}
 }
 static Case Open(string failure=null){
  var c=new Case();c.Api=new FakeApi{Root=Path.Combine(root,(++serial).ToString())};Directory.CreateDirectory(c.Api.Root);
  c.Api.Mod=new HangarMod{RequestTimeout=300,DialogTimeout=300,UiOpenTimeout=500,UiTimeout=2000};c.Api.Mod.StartAt(c.Api,c.Api.Root);
  c.Dir=Path.Combine(c.Api.Root,"Mods","QuantumHangarQc","HangarUI");Directory.CreateDirectory(c.Dir);
  c.Pref=Path.Combine(c.Api.Root,"Mods","QuantumHangarQc","Preferences",c.Api.P.steamId+".xml");
  c.NoBridge=failure=="missing";c.Api.HoldDialogs=failure!=null;c.App=new App{Reject=failure=="reject",Throw=failure=="throw"};c.Bridge=new UiBridge(c.App,s=>{});c.Field=new Field{Name=c.Api.P.playfield,Players=new Dictionary<int,IPlayer>{{c.Api.P.entityId,new Player{Id=c.Api.P.entityId,SteamId=c.Api.P.steamId}}}};
  c.Api.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo{playerId=c.Api.P.entityId,msg="qh:lang"});c.Task=c.Api.Mod.LanguageTask;
  var until=DateTime.UtcNow.AddSeconds(2);while((failure==null?c.App.Dialog==null:c.Api.HeldDialog==null)&&DateTime.UtcNow<until)c.Tick();Check(failure==null?c.App.Dialog!=null:c.Api.HeldDialog!=null,"qh:lang opens requested dialog transport");return c;
 }
 static void Answer(Case c,int value){c.Api.Mod.Game_Event(CmdId.Event_DialogButtonIndex,c.Api.HeldSeq,new IdAndIntValue{Id=c.Api.P.entityId,Value=value});}
 static void Main(string[] args){
  root=args[0];Directory.CreateDirectory(root);
  var c=Open();var r=c.Request();
  Check(c.App.Dialog.CloseOnLinkClick,"same clickable behavior as ship list");
  Check(Languages.Names.All(n=>c.App.Dialog.BodyText.Contains(n)),"all eight native language names visible");
  Check(c.App.Dialog.BodyText.Split(new[]{"<link="},StringSplitOptions.None).Length==10,"eight languages and server default");
  Check(!File.Exists(c.Pref),"opening does not alter preference");
  c.App.Click(HangarUiRules.LanguageLink(r,"fr"),999);Check(!File.Exists(c.Pref)&&!c.Task.IsCompleted,"wrong player callback ignored");
  c.App.Click(HangarUiRules.LanguageLink(r,"fr"));c.Finish();Check(Transfer.Read<Preference>(c.Pref).Language=="fr","click saves French");
  c.App.Click(HangarUiRules.LanguageLink(r,"de"));Check(Transfer.Read<Preference>(c.Pref).Language=="fr","duplicate callback cannot change choice");
  c.Api.Mod.Game_Exit();Check(new Preferences(Path.GetDirectoryName(c.Pref),null).Resolve(c.Api.P.steamId,"en").Code=="fr","click survives new preference store");
  Check(Directory.GetFiles(c.Dir).Length==0,"exchange files cleaned");
  var closed=Open();closed.App.Close();closed.Finish();Check(!File.Exists(closed.Pref),"close preserves existing/default language");closed.Api.Mod.Game_Exit();
  var invalid=Open();invalid.App.Click("qh-language:wrong-token:fr");invalid.Finish();Check(!File.Exists(invalid.Pref),"foreign token cannot select");invalid.Api.Mod.Game_Exit();
  var disconnected=Open();r=disconnected.Request();disconnected.Api.P.clientId++;disconnected.App.Click(HangarUiRules.LanguageLink(r,"fr"));disconnected.Finish();Check(!File.Exists(disconnected.Pref),"old connection click cannot save");disconnected.Api.Mod.Game_Exit();
  var automatic=Open();new Preferences(Path.GetDirectoryName(automatic.Pref),null).Set(automatic.Api.P.steamId,"fr");automatic.App.Click(HangarUiRules.LanguageLink(automatic.Request(),"auto"));automatic.Finish();Check(Transfer.Read<Preference>(automatic.Pref).Language==null,"server default click clears saved override");automatic.Api.Mod.Game_Exit();
  var unknown=Open();unknown.App.Click(HangarUiRules.LanguageLink(unknown.Request(),"bogus"));unknown.Finish();Check(!File.Exists(unknown.Pref),"unsupported code rejected");unknown.Api.Mod.Game_Exit();
  foreach(var mode in new[]{"reject","missing","throw"}) {
   var fallback=Open(mode);
   Check(Languages.Names.All(n=>fallback.Api.HeldDialog.MsgText.Contains(n)),"fallback shows all language names: "+mode);
   Check(fallback.Api.HeldDialog.PosButtonText=="English","positive button explicitly names language");
   Check(!File.Exists(fallback.Pref),"fallback opening cannot select");
   ushort seq=fallback.Api.HeldSeq;Answer(fallback,1);
   var until=DateTime.UtcNow.AddSeconds(2);while(fallback.Api.HeldSeq==seq&&DateTime.UtcNow<until)fallback.Tick();
   Check(fallback.Api.HeldDialog.PosButtonText=="Français","next advances to French");
   Answer(fallback,0);fallback.Finish();
   Check(Transfer.Read<Preference>(fallback.Pref).Language=="fr","fallback click saves French: "+mode);
   Check(Directory.GetFiles(fallback.Dir).Length==0,"failed bridge request cleaned before fallback");
   fallback.Api.Mod.Game_Exit();
  }
  var escaped=Open("reject");Answer(escaped,-1);escaped.Finish();Check(!File.Exists(escaped.Pref),"escape never saves a fallback language");escaped.Api.Mod.Game_Exit();
  var changed=Open("reject");changed.Api.P.clientId++;Answer(changed,0);changed.Finish();Check(!File.Exists(changed.Pref),"fallback selection rejected after reconnect");changed.Api.Mod.Game_Exit();
  var timeout=Open("reject");timeout.Finish();Check(!File.Exists(timeout.Pref),"fallback timeout never saves");timeout.Api.Mod.Game_Exit();
  Console.WriteLine("PASS "+checks+" language menu assertions; native dialog API simulated, real UiBridge and files.");
 }
}
