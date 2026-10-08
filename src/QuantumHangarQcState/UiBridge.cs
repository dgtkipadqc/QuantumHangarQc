using System;using System.IO;using System.Linq;using System.Collections.Generic;using Eleon.Modding;using QuantumHangarQc.Live;using QuantumHangarQc.Localization;
namespace QuantumHangarQc.State {
 public sealed class UiBridge {
  sealed class Pending {public string File,Reply;public HangarUiRequest Request;}
  readonly IApplication app;readonly Action<string> log;readonly Dictionary<int,Pending> pending=new Dictionary<int,Pending>();readonly object gate=new object();int serial;
  public UiBridge(IApplication value,Action<string> logger){app=value;log=logger;}
  public void Clear(){lock(gate)pending.Clear();}
  public void Tick(string directory,IPlayfield[] fields,DateTime now) {
   Directory.CreateDirectory(directory);
   lock(gate)foreach(var id in pending.Keys.ToArray()) {
    var p=pending[id];if(!File.Exists(p.File)||(now-p.Request.CreatedUtc).TotalSeconds>95)pending.Remove(id);
   }
   foreach(var file in Directory.GetFiles(directory,"*.request.xml").Take(64)) {
    HangarUiRequest r;try{r=Transfer.Read<HangarUiRequest>(file);}catch(IOException){continue;}
    Guid token;if(r==null||!Guid.TryParseExact(r.Token,"N",out token)||Path.GetFileName(file)!=r.Token+".request.xml")continue;
    if((now-r.CreatedUtc).TotalSeconds>95||(r.CreatedUtc-now).TotalSeconds>1)continue;
    var pf=fields.FirstOrDefault(x=>x.Name==r.Playfield);if(pf==null)continue;
    string reply=Path.Combine(directory,r.Token+".reply.xml");if(File.Exists(reply))continue;
    lock(gate)if(pending.Values.Any(x=>x.Request.Token==r.Token))continue;
    var item=new Pending{File=file,Reply=reply,Request=r};
    var player=pf.Players.Values.FirstOrDefault(x=>x.Id==r.Player);
    if(player==null||string.IsNullOrEmpty(player.SteamId)||player.SteamId!=r.Steam){Reply(item,null,true,"error.session");continue;}
    if((r.Protocol!=0&&r.Protocol!=2)||r.Rows==null||r.Rows.Count>15||r.Rows.Any(x=>!ValidRow(x))||r.Rows.Select(x=>x.Slot).Distinct().Count()!=r.Rows.Count){Reply(item,null,true,"error.ui");continue;}
    int key;lock(gate){key=++serial;if(key<=0){serial=1;key=1;}while(pending.ContainsKey(key))key=++serial;pending.Add(key,item);}
    try {
     // One shared callback dispatches by customValue; simultaneous players cannot overwrite each other's handler.
     bool shown=app.ShowDialogBox(r.Player,new DialogConfig{TitleText="Quantum Hangar Qc — BETA 0.2.6",BodyText=HangarUiRules.Body(r),CloseOnLinkClick=true,ButtonTexts=new[]{Texts.Render(r.Language,"button.close")},ButtonIdxForEsc=0,ButtonIdxForEnter=-1,MaxChars=0},Action,key);
     if(!shown){lock(gate)pending.Remove(key);Reply(item,null,true,"error.ui");}
     else {Transfer.Write(Path.Combine(directory,r.Token+".opened.xml"),new HangarUiReply{Token=r.Token,Player=r.Player,Steam=r.Steam,Client=r.Client,CapturedUtc=now});log("QH_UI OPEN player="+r.Player+" token="+r.Token);}
    } catch(Exception e){lock(gate)pending.Remove(key);Reply(item,null,true,e.Message);}
   }
  }
  static bool ValidRow(HangarRow row) {
   Guid archive;
   return row!=null&&row.Slot>=1&&row.Slot<=15&&(row.Name==null||row.Name.Length<=256)&&(row.Status==null||row.Status.Length<=64)&&(string.IsNullOrEmpty(row.ArchiveId)||Guid.TryParseExact(row.ArchiveId,"N",out archive));
  }
  void Action(int button,string link,string input,int player,int custom) {
   Pending p;lock(gate){if(!pending.TryGetValue(custom,out p)||p.Request.Player!=player)return;pending.Remove(custom);}
   if(!File.Exists(p.File)||(DateTime.UtcNow-p.Request.CreatedUtc).TotalSeconds>95)return;
   try {
    var row=button==-1?HangarUiRules.Select(p.Request,link):null;
    Reply(p,row,row==null,null);log("QH_UI SELECT player="+player+" slot="+(row==null?0:row.Slot));
   }catch(Exception e){log("QH_UI CALLBACK_ERROR "+e.Message);}
  }
  static void Reply(Pending p,HangarRow row,bool cancelled,string error){
   if(!File.Exists(p.File))return;
   Transfer.Write(p.Reply,new HangarUiReply{Token=p.Request.Token,Player=p.Request.Player,Client=p.Request.Client,Steam=p.Request.Steam,Slot=row==null?0:row.Slot,ArchiveId=row==null?null:row.ArchiveId,Cancelled=cancelled,Error=error,CapturedUtc=DateTime.UtcNow});
  }
 }
}
