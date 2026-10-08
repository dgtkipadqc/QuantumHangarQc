using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantumHangarQc.Localization;
namespace QuantumHangarQc.Live {
 public class HangarRow { public int Slot; public string ArchiveId,Name,Status; }
 public class HangarUiRequest { public string Token,Steam,Playfield,Language; public int Player,Client,Protocol; public DateTime CreatedUtc; public List<HangarRow> Rows=new List<HangarRow>(); }
 public class HangarUiReply { public string Token,Steam,ArchiveId,Error; public int Player,Client,Slot; public bool Cancelled; public DateTime CapturedUtc; }
 public static class HangarUiRules {
  public static string Escape(string text) { return Texts.Escape(text??""); }
  public static string Link(HangarUiRequest r,HangarRow row) { return "qh:"+r.Token+":"+row.Slot+":"+row.ArchiveId; }
  public static HangarRow Select(HangarUiRequest r,string link) { return r.Rows.SingleOrDefault(x=>x.Status=="STORED"&&!string.IsNullOrEmpty(x.ArchiveId)&&Link(r,x)==link); }
  public static string Status(string language,string status) { return Texts.Render(language,status=="STORED"?"status.stored":status=="QUARANTINE"?"status.quarantine":"status.pending",status=="STORED"||status=="QUARANTINE"?new object[0]:new object[]{status}); }
  public static string Body(HangarUiRequest r) {
   var text=new StringBuilder(Texts.Render(r.Language,"hangar.list.choose")+"\n\n");
   text.Append(Texts.Render(r.Language,"hangar.list.title",r.Rows.Count(x=>!string.IsNullOrEmpty(x.ArchiveId)))).Append("\n\n");
   foreach(var row in r.Rows.OrderBy(x=>x.Slot)) {
    string label=row.Slot+" : "+(string.IsNullOrEmpty(row.ArchiveId)?Texts.Render(r.Language,"hangar.list.empty"):Escape(row.Name)+" — "+Status(r.Language,row.Status));
    if(row.Status=="STORED"&&!string.IsNullOrEmpty(row.ArchiveId))text.Append("<link=\"").Append(Link(r,row)).Append("\"><color=#66DFFF><u>").Append(label).Append("</u></color></link>");
    else text.Append(label);
    text.Append('\n');
   }
   text.Append("\n").Append(Texts.Render(r.Language,"hangar.list.footer"));
   return text.ToString();
  }
 }
}
