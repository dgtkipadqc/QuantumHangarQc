using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace QuantumHangarQc.Live {
 public class HangarRow { public int Slot; public string ArchiveId,Name,Status; }
 public class HangarUiRequest { public string Token,Steam,Playfield; public int Player,Client; public DateTime CreatedUtc; public List<HangarRow> Rows=new List<HangarRow>(); }
 public class HangarUiReply { public string Token,Steam,ArchiveId,Error; public int Player,Client,Slot; public bool Cancelled; public DateTime CapturedUtc; }
 public static class HangarUiRules {
  public static string Escape(string text) { return (text??"").Replace("<","‹").Replace(">","›").Replace("[","(").Replace("]",")").Replace("\r"," ").Replace("\n"," "); }
  public static string Link(HangarUiRequest r,HangarRow row) { return "qh:"+r.Token+":"+row.Slot+":"+row.ArchiveId; }
  public static HangarRow Select(HangarUiRequest r,string link) { return r.Rows.SingleOrDefault(x=>x.Status=="STORED"&&!string.IsNullOrEmpty(x.ArchiveId)&&Link(r,x)==link); }
  public static string Body(HangarUiRequest r) {
   var text=new StringBuilder("<color=#66DFFF>Clique un vaisseau pour le sortir / Click a ship to retrieve it</color>\n\n");
   text.Append(r.Rows.Count(x=>!string.IsNullOrEmpty(x.ArchiveId))).Append("/10 places occupees / occupied\n\n");
   foreach(var row in r.Rows.OrderBy(x=>x.Slot)) {
    string label=row.Slot+" : "+(string.IsNullOrEmpty(row.ArchiveId)?"Libre / Empty":Escape(row.Name)+" — "+Escape(row.Status));
    if(row.Status=="STORED"&&!string.IsNullOrEmpty(row.ArchiveId))text.Append("<link=\"").Append(Link(r,row)).Append("\"><color=#66DFFF><u>").Append(label).Append("</u></color></link>");
    else text.Append(label);
    text.Append('\n');
   }
   text.Append("\nqh:mark avant la sortie / before retrieval.\nConfirmation obligatoire / Confirmation required.\nAutre methode / Alternative: qh:load:NUMERO");
   return text.ToString();
  }
 }
}
