using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eleon.Modding;
using ModExtenderCommunication;
using QuantumHangarQc;
using QuantumHangarQc.Localization;
class ConfigTests {
 static int count;
 static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
 static void Bad(Action action,string name){bool failed=false;try{action();}catch{failed=true;}Check(failed,name);}
 public class ClientPipe {
  public string Save; public bool Reply=true,WrongFirst;
  public Dictionary<Type,Action<object>> Handlers;
  enum AppFolder {Root,Content,SaveGame}
  public void SendMessage(object item) {
   var r=(ModComData)item;
   Check((AppFolder)r.Data==AppFolder.SaveGame,"wire Int32 unboxes as AppFolder.SaveGame");
   Check(r.Command==ModCommand.GetPathFor,"native path command");
   if(!Reply)return;
   if(WrongFirst) Handlers[typeof(ModComData)](new ModComData{Command=r.Command,SequenceId=Guid.NewGuid(),Data="wrong"});
   Handlers[typeof(ModComData)](new ModComData{Command=r.Command,SequenceId=r.SequenceId,Data=Save});
  }
 }
 public class Host : ModGameAPI {
  public Dictionary<Type,Action<object>> InServerMessageHandler {get;set;}
  public ClientPipe ToEmpyrion {get;set;}
  public Host(string save){InServerMessageHandler=new Dictionary<Type,Action<object>>();ToEmpyrion=new ClientPipe{Save=save,Handlers=InServerMessageHandler};}
  public void Console_Write(string s){} public ulong Game_GetTickTime(){return 0;} public bool Game_Request(CmdId c,ushort n,object d){return true;}
 }
 static void Main(string[] args) {
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  var a=Path.Combine(root,"SaveA");var b=Path.Combine(root,"SaveB");Directory.CreateDirectory(a);Directory.CreateDirectory(b);
  var hostA=new Host(a);var hostB=new Host(b);int forwarded=0;
  hostA.InServerMessageHandler[typeof(ModComData)]=o=>forwarded++;
  hostA.ToEmpyrion.WrongFirst=true;
  var paths=Task.WhenAll(Task.Run(()=>ActiveSavePath.Resolve(hostA)),Task.Run(()=>ActiveSavePath.Resolve(hostB))).Result;
  Check(paths[0]==a&&paths[1]==b,"concurrent hosts resolve their own save");Check(forwarded==1,"unrelated response forwarded");
  var timeout=new Host(a);timeout.ToEmpyrion.Reply=false;Bad(()=>ActiveSavePath.Resolve(timeout,5),"timeout refuses guessing");
  Bad(()=>ActiveSavePath.Resolve(new Host("missing")),"invalid path rejected");
  Bad(()=>new Settings{MaxShipsPerPlayer=0}.Validate(),"zero rejected");Bad(()=>new Settings{MaxShipsPerPlayer=16}.Validate(),"over archive limit rejected");
  Bad(()=>new Settings{DefaultLanguage="invalid"}.Validate(),"invalid language rejected");
  var modA=new HangarMod();modA.StartAt(hostA,a);modA.Game_Exit();
  string cfgA=Path.Combine(a,"Mods","QuantumHangarQc","Configuration.xml");
  Check(Disk.Read<Settings>(cfgA).MaxShipsPerPlayer==10,"default config created");
  Disk.Write(cfgA,new Settings{MaxShipsPerPlayer=5,DefaultLanguage="fr"});
  modA=new HangarMod();modA.StartAt(hostA,a);
  var modB=new HangarMod();modB.StartAt(hostB,b);
  var field=typeof(HangarMod).GetField("store",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  var storeA=(ArchiveStore)field.GetValue(modA);var storeB=(ArchiveStore)field.GetValue(modB);
  Check(storeA.Capacity==5&&storeB.Capacity==10,"save configs isolated");
  const string steam="76561198000000001";
  for(int i=1;i<=5;i++)storeA.Reserve(new GlobalStructureInfo{id=i},steam);
  Bad(()=>storeA.Reserve(new GlobalStructureInfo{id=6},steam),"sixth ship rejected");
  Bad(()=>storeA.Reserve(new GlobalStructureInfo{id=6},steam,6),"slot above quota rejected");
  modA.Game_Exit();modB.Game_Exit();
  var archive=Path.Combine(root,"Reduction");var before=new ArchiveStore(archive,10);
  for(int i=1;i<=10;i++)before.Reserve(new GlobalStructureInfo{id=i},steam);
  var after=new ArchiveStore(archive,5);
  Check(after.List(steam).Count==10&&after.At(10,steam).ShipId==10,"reducing quota preserves and exposes all old slots");
  Bad(()=>after.Reserve(new GlobalStructureInfo{id=11},steam),"reduced quota blocks additions");
  Texts.Initialize(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Languages"),null);
  foreach(string lang in new[]{"en","fr","de","el","it","pt-BR","ru","zh-Hans"}) {
   foreach(string key in new[]{"help.body","error.hangar_full","error.slot_range"})Check(!Texts.Render(lang,key,5).Contains("LOCALE_RENDER"),lang+key);
   Check(Texts.Render(lang,"hangar.list.title",2,5).Contains("2/5"),lang+" quota title");
  }
  Console.WriteLine("CONFIG PASS "+count);
 }
}
