using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantumHangarQc.Localization;
namespace QuantumHangarQc.Live {
 public class HangarRow { public int Slot; public string ArchiveId,Name,Status; }
 public class HangarUiRequest { public string Token,Steam,Playfield,Language,Purpose; public int Player,Client,Protocol; public int Capacity=10; public DateTime CreatedUtc; public List<HangarRow> Rows=new List<HangarRow>(); }
 public class HangarUiReply { public string Token,Steam,ArchiveId,Error,SelectedLanguage; public int Player,Client,Slot; public bool Cancelled; public DateTime CapturedUtc; }
 public static class HangarUiRules {
  public static string Escape(string text) { return Texts.Escape(text??""); }
  public static string Link(HangarUiRequest r,HangarRow row) { return "qh:"+r.Token+":"+row.Slot+":"+row.ArchiveId; }
  public static HangarRow Select(HangarUiRequest r,string link) { return r.Rows.SingleOrDefault(x=>x.Status=="STORED"&&!string.IsNullOrEmpty(x.ArchiveId)&&Link(r,x)==link); }
  public static string Status(string language,string status) { return Texts.Render(language,status=="STORED"?"status.stored":status=="QUARANTINE"?"status.quarantine":"status.pending",status=="STORED"||status=="QUARANTINE"?new object[0]:new object[]{status}); }
  public static string LanguageLink(HangarUiRequest r,string code) { if(r.Protocol==2) {
    int index=Array.IndexOf(Languages.Codes.Concat(new[]{"auto"}).ToArray(),code);
    var row=r.Rows.SingleOrDefault(x=>x.Slot==index+1);
    return row==null?null:Link(r,row);
   }
   return "qh-language:"+r.Token+":"+code; }
  public static string SelectLanguage(HangarUiRequest r,string link) {
   if(r.Purpose!="language"||(r.Protocol!=2&&r.Protocol!=3))return null;
   return Languages.Codes.Concat(new[]{"auto"}).SingleOrDefault(code=>LanguageLink(r,code)==link);
  }
  public static string Body(HangarUiRequest r) {
   if(r.Purpose=="language") {
    var menu=new StringBuilder(Texts.Render(r.Language,"language.title")+"\n\n");
    foreach(var code in Languages.Codes.Concat(new[]{"auto"})) {
     string label=code=="auto"?Texts.Render(r.Language,"language.origin.server"):Languages.Name(code);
     menu.Append("<link=\"").Append(LanguageLink(r,code)).Append("\"><color=#66DFFF><u>").Append(Escape(label)).Append("</u></color></link>\n");
    }
    return menu.ToString();
   }
   var text=new StringBuilder(Texts.Render(r.Language,"hangar.list.choose")+"\n\n");
   text.Append(Texts.Render(r.Language,"hangar.list.title",r.Rows.Count(x=>!string.IsNullOrEmpty(x.ArchiveId)),r.Capacity)).Append("\n\n");
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
