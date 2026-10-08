using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eleon.Modding;
using QuantumHangarQc;
using QuantumHangarQc.Live;
using QuantumHangarQc.Localization;

class LocaleTests {
 static int checks,serial;static string root;
 static void Check(bool yes,string name){checks++;if(!yes)throw new Exception("FAIL "+name);}
 static void Bad(Action a,string name){bool bad=false;try{a();}catch{bad=true;}Check(bad,name);}
 static FakeApi New(){
  var a=new FakeApi{Root=Path.Combine(root,(++serial).ToString())};
  Directory.CreateDirectory(Path.Combine(a.Root,"Shared","200"));
  File.WriteAllText(Path.Combine(a.Root,"Shared","200","0.area"),"test bytes: chest A slot0=123x15; chest B slot2=456x9");
  a.Mod=new HangarMod{RequestTimeout=300,DialogTimeout=500,ExportPoll=2,StateTimeout=300,StatePoll=2,UiOpenTimeout=30,UiTimeout=300};a.Mod.StartAt(a,a.Root);return a;
 }
 static Task Start(FakeApi a,string command){
  foreach(string name in new[]{"lastCommand","languageLast"})((Dictionary<int,DateTime>)typeof(HangarMod).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(a.Mod)).Clear();
  a.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo{playerId=a.P.entityId,msg=command});
  return command.StartsWith("qh:lang",StringComparison.OrdinalIgnoreCase)?a.Mod.LanguageTask:a.Mod.ActiveTask;
 }
 static void Pump(FakeApi a,Task task){DateTime until=DateTime.UtcNow.AddSeconds(5);while(DateTime.UtcNow<until){a.ReplyState();a.Mod.Game_Update();Thread.Sleep(1);if(task!=null&&task.IsCompleted){task.GetAwaiter().GetResult();a.Mod.Game_Update();return;}}throw new Exception("pump timeout");}
 static void Run(FakeApi a,string command){Pump(a,Start(a,command));}
 static Dictionary<string,TextEntry> Pack(string path,string code){using(var s=File.OpenRead(path))return Texts.Read(s,code);}
 static void ParseBad(string xml,string message){Bad(()=>{using(var s=new MemoryStream(Encoding.UTF8.GetBytes(xml)))Texts.Read(s,"en");},message);}
 static void Main(string[] args){
  root=Path.GetFullPath(args[1]);Directory.CreateDirectory(root);Texts.Initialize(args[0],null);
  var en=Pack(Path.Combine(args[0],"en.xml"),"en");
  foreach(var code in Languages.Codes){
   var pack=Pack(Path.Combine(args[0],code+".xml"),code);
   Check(pack.Keys.OrderBy(x=>x).SequenceEqual(en.Keys.OrderBy(x=>x)),"full coverage "+code);
   foreach(var entry in pack.Values){Check(entry.Args==en[entry.Key].Args,"parameter contract "+code+" "+entry.Key);Check(!Texts.Render(code,entry.Key,Enumerable.Repeat<object>("value",entry.Args).ToArray()).Contains("LOCALE_RENDER"),"render "+code+" "+entry.Key);}
  }
  ParseBad("<Catalog Schema='1' Code='en'><Text Key='x.a' Args='0'>x</Text><Text Key='x.a' Args='0'>y</Text></Catalog>","duplicate keys rejected");
  ParseBad("<Catalog Schema='1' Code='en'><Text Key='x.a' Args='1'>{3}</Text></Catalog>","invalid parameter indices rejected");
  ParseBad("<Catalog Schema='1' Code='en'><Text Key='x.a' Args='0'> </Text></Catalog>","empty translations rejected");
  ParseBad("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><Catalog Schema='1' Code='en'/>","DTD rejected");
  Bad(()=>{using(var s=new MemoryStream(new byte[]{255,255}))Texts.Read(s,"en");},"invalid UTF8 rejected");
  Check(Texts.Render("fr","missing.key").Contains("LOCALE_RENDER"),"missing English key gives minimal safe message");
  Check(Texts.Render("fr","store.success").Contains("LOCALE_RENDER"),"wrong argument count gives safe fallback");
  Check(Texts.Render("unknown","button.close")=="Close","unknown catalog falls back to embedded English");
  Check(Languages.Normalize("PT_br")=="pt-BR"&&Languages.Normalize("ZH_cn")=="zh-Hans","documented aliases");
  Check(Languages.Normalize("pt-PT")==null&&Languages.Normalize("zh-TW")==null,"regions not silently mixed");
  Check(Languages.Normalize("../../en")==null,"path injection rejected");
  var prefs=new Preferences(Path.Combine(root,"prefs"),null);string steam="76561198000000001",other="76561198000000002";
  Check(prefs.Resolve(steam,"fr").Code=="fr"&&prefs.Resolve(steam,"fr").Origin=="language.origin.server","server French preserved");
  Check(prefs.Resolve(steam,"bad").Code=="en"&&prefs.Resolve(steam,"bad").Origin=="language.origin.fallback","invalid default falls back to English");
  prefs.Set(steam,"de");Check(prefs.Resolve(steam,"fr").Code=="de","manual overrides server");
  Check(prefs.Resolve(other,"fr").Code=="fr","separate Steam identity");
  prefs=new Preferences(Path.Combine(root,"prefs"),null);Check(prefs.Resolve(steam,"fr").Code=="de","restart persistence");
  prefs.Set(steam,null);Check(prefs.Resolve(steam,"fr").Code=="fr","auto clears manual choice");
  Bad(()=>prefs.Set(steam,"bad"),"unknown language rejected");Check(prefs.Resolve(steam,"fr").Code=="fr","unknown preserves previous choice");
  File.WriteAllText(Path.Combine(root,"prefs",steam+".xml"),"not XML");prefs=new Preferences(Path.Combine(root,"prefs"),null);
  Check(prefs.Resolve(steam,"en").Code=="en","corrupt preference safe recovery");
  Check(Directory.GetFiles(Path.Combine(root,"prefs"),"*.corrupt.*").Length==1,"corrupt evidence preserved");prefs.Set(steam,"fr");
  Bad(()=>prefs.Set("../../outside","fr"),"invalid authenticated identity cannot create a path");
  var row=new HangarRow{Slot=1,Name="é <link=evil>\n[ship]",ArchiveId=Guid.NewGuid().ToString("N"),Status="STORED"};
  var req=new HangarUiRequest{Token=Guid.NewGuid().ToString("N"),Language="fr",Rows=new List<HangarRow>{row}};
  string body=HangarUiRules.Body(req);Check(!body.Contains("<link=evil>")&&body.Contains("é ‹link=evil› (ship)"),"untrusted name stays text");
  Check(body.Contains("<link=\""+HangarUiRules.Link(req,row))&&HangarUiRules.Select(req,HangarUiRules.Link(req,row))==row,"generated selection token preserved");
  Check(HangarUiRules.Status("fr","QUARANTINE").Contains("Quarantaine"),"persistent status displayed in French");
  var a=New();Run(a,"qh:help");Check(a.HeldDialog.MsgText.Contains("Personal hangar"),"generic installation defaults to English");
  foreach(var code in Languages.Codes){Run(a,"qh:lang:"+code);Check(a.HeldDialog.MsgText.Contains(Languages.Name(code)),"real command language "+code);Run(a,"qh:help");Check(a.HeldDialog.MsgText.Contains(Texts.Render(code,"help.body")),"help integration "+code);}
  Run(a,"qh:lang:fr");Run(a,"qh:lang:unknown");Check(a.HeldDialog.MsgText.Contains("Langue inconnue"),"unknown command retains French");
  Run(a,"qh:lang:auto");Run(a,"qh:help");Check(a.HeldDialog.MsgText.Contains("Personal hangar"),"auto command actually removed override");
  Run(a,"qh:lang:fr");a.P.entityId=77;Run(a,"qh:help");Check(a.HeldDialog.MsgText.Contains("Hangar personnel"),"character reset keeps Steam preference");
  a.P.steamId=other;Run(a,"qh:help");Check(a.HeldDialog.MsgText.Contains("Personal hangar"),"entity reuse with another Steam does not leak language");a.Mod.Game_Exit();
  var during=New();during.HoldDialogs=true;var task=Start(during,"qh:store:200");DateTime until=DateTime.UtcNow.AddSeconds(2);
  while(during.HeldDialog==null&&DateTime.UtcNow<until){during.ReplyState();during.Mod.Game_Update();Thread.Sleep(1);}
  Check(during.HeldDialog!=null,"store confirmation opened");ushort token=during.HeldSeq;
  Run(during,"qh:lang:de");Check(during.HeldSeq==token&&during.Calls.Count(c=>c==CmdId.Request_ShowDialog_SinglePlayer)==1,"language change cannot replace confirmation");
  during.Mod.Game_Event(CmdId.Event_DialogButtonIndex,token,new IdAndIntValue{Id=during.P.entityId,Value=0});Pump(during,task);
  Check(during.Calls.Count(c=>c==CmdId.Request_Entity_Export)==1&&during.Store().List(steam).Single().Status=="STORED","old click completes exactly one transaction");
  during.HoldDialogs=false;Run(during,"qh:help");Check(during.HeldDialog.MsgText.Contains("Persönlicher Hangar"),"new dialog takes new language");during.Mod.Game_Exit();
  Console.WriteLine("PASS "+checks+" localization assertions; simulated APIs / local files. No game-language source or game rendering tested.");
 }
}
