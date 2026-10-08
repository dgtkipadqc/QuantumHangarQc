using System;using System.IO;using System.Linq;using System.Collections.Generic;using Eleon.Modding;using QuantumHangarQc.Live;using QuantumHangarQc.State;using RecycleTests;
class UiTests {
 static int checks;static void Check(bool x,string text){checks++;if(!x)throw new Exception(text);}
 class App:StubIApplication {public DialogActionHandler Handler;public Dictionary<int,int> Keys=new Dictionary<int,int>();public Dictionary<int,DialogConfig> Configs=new Dictionary<int,DialogConfig>();public bool Reject;public override bool ShowDialogBox(int player,DialogConfig config,DialogActionHandler handler,int key){Handler=handler;Keys[player]=key;Configs[player]=config;return !Reject;}}
 static HangarUiRequest Request(int player,string steam){return new HangarUiRequest{Token=Guid.NewGuid().ToString("N"),Player=player,Client=7,Steam=steam,Playfield="Space",CreatedUtc=DateTime.UtcNow,Rows=new List<HangarRow>{new HangarRow{Slot=1,ArchiveId=Guid.NewGuid().ToString("N"),Name="Test <link=evil>\n[ship]",Status="STORED"},new HangarRow{Slot=2,ArchiveId=Guid.NewGuid().ToString("N"),Name="Blocked",Status="QUARANTINE"},new HangarRow{Slot=3}}};}
 static string Put(string dir,HangarUiRequest r){string p=Path.Combine(dir,r.Token+".request.xml");Transfer.Write(p,r);return p;}
 static HangarUiReply Read(string dir,HangarUiRequest r){return Transfer.Read<HangarUiReply>(Path.Combine(dir,r.Token+".reply.xml"));}
 static void Main(string[] args){
  string dir=Path.GetFullPath(args[0]);Directory.CreateDirectory(dir);
  var pf=new StubIPlayfield{Name="Space",Players=new Dictionary<int,IPlayer>{{7,new StubIPlayer{Id=50,SteamId="a"}},{9,new StubIPlayer{Id=60,SteamId="b"}}}};
  var app=new App();var bridge=new UiBridge(app,x=>{});var r=Request(50,"a");r.Language="fr";r.Protocol=2;Put(dir,r);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);
  Check(app.Configs[50].CloseOnLinkClick&&app.Configs[50].ButtonIdxForEnter==-1,"click closes selection, enter cannot select");
  string body=app.Configs[50].BodyText;Check(body.Contains("<link=\"qh:"+r.Token),"native TMP links rendered");Check(!body.Contains("<link=evil>"),"ship names cannot inject rich text");Check(!body.Contains(HangarUiRules.Link(r,r.Rows[1])),"quarantine not clickable");Check(body.Contains("3 : Libre"),"French free slot shown");
  int first=app.Keys[50];bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);Check(app.Keys[50]==first,"no duplicate dialog on repeated tick");
  var second=Request(60,"b");second.Language="en";second.Protocol=2;Put(dir,second);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);
  Check(app.Configs[50].BodyText.Contains("Clique")&&app.Configs[60].BodyText.Contains("Click"),"simultaneous French and English bodies isolated");
  Check(app.Configs[50].ButtonTexts[0]=="Fermer"&&app.Configs[60].ButtonTexts[0]=="Close","buttons follow per-request language");
  var oldLanguage=Request(50,"a");Check(HangarUiRules.Body(oldLanguage).Contains("Click"),"old request defaults to English");
  app.Handler(-1,HangarUiRules.Link(r,r.Rows[0]),"",999,first);Check(!File.Exists(Path.Combine(dir,r.Token+".reply.xml")),"wrong player callback ignored");
  app.Handler(-1,HangarUiRules.Link(second,second.Rows[0]),"",50,first);Check(Read(dir,r).Cancelled,"other player's link cannot select");
  app.Handler(-1,HangarUiRules.Link(second,second.Rows[0]),"",60,app.Keys[60]);Check(Read(dir,second).ArchiveId==second.Rows[0].ArchiveId,"second player preserved by shared dispatcher");
  var valid=Request(50,"a");Put(dir,valid);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);int key=app.Keys[50];app.Handler(-1,HangarUiRules.Link(valid,valid.Rows[0]),"",50,key);
  Check(Read(dir,valid).Slot==1&&!Read(dir,valid).Cancelled,"valid row selected");
  var original=File.ReadAllText(Path.Combine(dir,valid.Token+".reply.xml"));app.Handler(0,null,"",50,key);Check(original==File.ReadAllText(Path.Combine(dir,valid.Token+".reply.xml")),"duplicate callback cannot replace choice");
  var closed=Request(50,"a");Put(dir,closed);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);app.Handler(0,null,"",50,app.Keys[50]);Check(Read(dir,closed).Cancelled,"close cancels");
  var missing=Request(50,"a");string path=Put(dir,missing);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);key=app.Keys[50];File.Delete(path);app.Handler(-1,HangarUiRules.Link(missing,missing.Rows[0]),"",50,key);Check(!File.Exists(Path.Combine(dir,missing.Token+".reply.xml")),"late callback after host cancellation ignored");
  var reject=Request(50,"a");Put(dir,reject);app.Reject=true;bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);Check(!string.IsNullOrEmpty(Read(dir,reject).Error),"API rejection returned for fallback");app.Reject=false;
  var stranger=Request(50,"b");Put(dir,stranger);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);Check(!string.IsNullOrEmpty(Read(dir,stranger).Error),"identity mismatch refused");
  var old=Request(50,"a");old.CreatedUtc=DateTime.UtcNow.AddMinutes(-5);Put(dir,old);key=app.Keys[50];bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);Check(app.Keys[50]==key,"expired request not shown");
  var invalid=Request(50,"a");invalid.Rows[2].Slot=1;Put(dir,invalid);bridge.Tick(dir,new IPlayfield[]{pf},DateTime.UtcNow);Check(!string.IsNullOrEmpty(Read(dir,invalid).Error),"duplicate slot rejected");
  var legacy=Request(50,"a");legacy.Rows[0].Slot=15;Check(HangarUiRules.Select(legacy,HangarUiRules.Link(legacy,legacy.Rows[0])).Slot==15,"legacy slot 15 remains selectable");
  bridge.Clear();Console.WriteLine("PASS "+checks+" selection bridge checks; mocked UI only.");
 }
}
