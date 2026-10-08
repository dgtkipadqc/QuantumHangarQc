using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace QuantumHangarQc.Localization {
 public sealed class TextEntry { [XmlAttribute] public string Key; [XmlAttribute] public int Args; [XmlText] public string Text; }
 public sealed class Catalog { [XmlAttribute] public int Schema; [XmlAttribute] public string Code; [XmlElement("Text")] public List<TextEntry> Entries=new List<TextEntry>(); }
 public static class Languages {
  public static readonly string[] Codes={"en","fr","de","ru","it","pt-BR","zh-Hans","el"};
  public static readonly string[] Names={"English","Français","Deutsch","Русский","Italiano","Português do Brasil","简体中文","Ελληνικά"};
  public static string Normalize(string value) {
   if(value==null||value.Length>24)return null;
   string v=value.Trim().Replace('_','-');
   // Deliberately do not map pt-PT, zh-TW, or an arbitrary region to another language.
   if(v.Equals("pt",StringComparison.OrdinalIgnoreCase))v="pt-BR";
   if(v.Equals("zh",StringComparison.OrdinalIgnoreCase)||v.Equals("zh-CN",StringComparison.OrdinalIgnoreCase))v="zh-Hans";
   return Codes.FirstOrDefault(c=>c.Equals(v,StringComparison.OrdinalIgnoreCase));
  }
  public static string Name(string code){int i=Array.IndexOf(Codes,Normalize(code));return i<0?"English":Names[i];}
  public static string Choices(){return string.Join("\n",Codes.Select((c,i)=>"qh:lang:"+c+" — "+Names[i]));}
 }
 public static class Texts {
  static readonly object gate=new object();
  static Dictionary<string,Dictionary<string,TextEntry>> catalogs;
  static Action<string> log;
  static readonly HashSet<string> warnings=new HashSet<string>();
  static void Warn(string code){lock(gate){if(warnings.Count>=256||!warnings.Add(code))return;}if(log!=null)log("QH_LOCALE "+code);}
  public static string Escape(object value){return Convert.ToString(value,CultureInfo.InvariantCulture).Replace("<","‹").Replace(">","›").Replace("[","(").Replace("]",")").Replace("\r"," ").Replace("\n"," ");}
  public static Dictionary<string,TextEntry> Read(Stream stream,string code) {
   string xml;using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),false))xml=reader.ReadToEnd();
   if(xml.Length>0&&xml[0]=='\uFEFF')xml=xml.Substring(1);
   if(xml.Length>1024*1024)throw new InvalidDataException("catalog_size");
   Catalog c;using(var reader=XmlReader.Create(new StringReader(xml),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024*1024}))c=(Catalog)new XmlSerializer(typeof(Catalog)).Deserialize(reader);
   if(c.Schema!=1||c.Code!=code||c.Entries==null||c.Entries.Count>512)throw new InvalidDataException("catalog_schema");
   var result=new Dictionary<string,TextEntry>(StringComparer.Ordinal);
   foreach(var e in c.Entries) {
    if(e==null||e.Key==null||!Regex.IsMatch(e.Key,@"^[a-z][a-z0-9_.]+$")||result.ContainsKey(e.Key)||string.IsNullOrWhiteSpace(e.Text)||e.Text.Length>8192||e.Args<0||e.Args>20)throw new InvalidDataException("catalog_entry");
    var matches=Regex.Matches(e.Text,@"\{([0-9]+)\}");
    if(Regex.Replace(e.Text,@"\{([0-9]+)\}","").IndexOfAny(new[]{'{','}'})>=0)throw new InvalidDataException("catalog_format");
    var indices=matches.Cast<Match>().Select(m=>int.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture)).Distinct().OrderBy(i=>i).ToArray();
    if(!indices.SequenceEqual(Enumerable.Range(0,e.Args)))throw new InvalidDataException("catalog_args");
    string.Format(CultureInfo.InvariantCulture,e.Text,Enumerable.Repeat<object>("x",e.Args).ToArray());result.Add(e.Key,e);
   }
   return result;
  }
  public static void Initialize(string directory,Action<string> logger) {
   lock(gate) {
    if(catalogs!=null)return;log=logger;
    var packs=new Dictionary<string,Dictionary<string,TextEntry>>(StringComparer.Ordinal);
    using(var s=typeof(Texts).Assembly.GetManifestResourceStream("QH.en.xml")) {
     if(s==null)throw new InvalidDataException("embedded_english_missing");packs.Add("en",Read(s,"en"));
    }
    foreach(string code in Languages.Codes) {
     string path=Path.Combine(directory,code+".xml");
     try {
      using(var s=File.OpenRead(path)) {
       var pack=Read(s,code);
       if(pack.Any(k=>!packs["en"].ContainsKey(k.Key)||packs["en"][k.Key].Args!=k.Value.Args))throw new InvalidDataException("catalog_contract");
       if(code!="en")packs[code]=pack;
      }
     }catch(Exception){Warn("CATALOG_FALLBACK "+code);}
    }
    catalogs=packs;
   }
  }
  static void Ensure(){if(catalogs==null)Initialize(Path.Combine(Path.GetDirectoryName(typeof(Texts).Assembly.Location),"Languages"),null);}
  public static string Render(string language,string key,params object[] args) {
   Ensure();string code=Languages.Normalize(language)??"en";TextEntry e;Dictionary<string,TextEntry> pack;
   if(!catalogs.TryGetValue(code,out pack)||!pack.TryGetValue(key,out e)){Warn("KEY_FALLBACK "+code+" "+key);catalogs["en"].TryGetValue(key,out e);}
   if(e==null||args==null||args.Length!=e.Args){Warn("RENDER_FAILED "+key);return "QH: message unavailable (LOCALE_RENDER).";}
   try{return string.Format(CultureInfo.InvariantCulture,e.Text,args.Select(a=>(object)Escape(a)).ToArray());}
   catch(Exception){Warn("RENDER_FAILED "+key);return "QH: message unavailable (LOCALE_RENDER).";}
  }
  public static string Error(string language,Exception error) {
   string key=error.Data["QhKey"] as string;
   var args=error.Data["QhArgs"] as object[];
   return Render(language,key??"error.unexpected",args??new object[0])+" ("+(key??"error.unexpected")+")";
  }
  public static T Tag<T>(T error,string key,params object[] args) where T:Exception {error.Data["QhKey"]=key;error.Data["QhArgs"]=args;return error;}
 }
 public sealed class Preference {public int Schema=1;public string Language;}
 public sealed class Resolution {public string Code,Origin;}
 public sealed class Preferences {
  readonly string root;readonly Action<string> log;readonly object gate=new object();
  readonly Dictionary<string,string> manual=new Dictionary<string,string>();
  public Preferences(string directory,Action<string> logger){root=directory;log=logger;Directory.CreateDirectory(root);}
  static string Identity(string steam){ulong id;if(!ulong.TryParse(steam,NumberStyles.None,CultureInfo.InvariantCulture,out id)||id==0||id.ToString(CultureInfo.InvariantCulture)!=steam)throw new InvalidDataException("preference_identity");return steam;}
  string FileFor(string steam){return Path.Combine(root,Identity(steam)+".xml");}
  string Get(string steam) {
   lock(gate) {
    string selected;if(manual.TryGetValue(steam,out selected))return selected;
    string path=FileFor(steam);selected=null;
    if(File.Exists(path))try {
     var p=Live.Transfer.Read<Preference>(path);
     if(p.Schema!=1||(p.Language!=null&&Languages.Normalize(p.Language)!=p.Language))throw new InvalidDataException("preference_schema");selected=p.Language;
    }catch(Exception){
     // Keep the original evidence; never overwrite a corrupt preference during recovery.
     string backup=path+".corrupt."+Guid.NewGuid().ToString("N");
     try{File.Move(path,backup);}catch(Exception){throw Texts.Tag(new IOException("preference_recovery_failed"),"error.preference_write");}
     if(log!=null)log("QH_LOCALE PREFERENCE_CORRUPT");
    }
    manual[steam]=selected;return selected;
   }
  }
  public void Set(string authenticatedSteam,string language) {
   lock(gate) {
    Get(authenticatedSteam);string code=language==null?null:Languages.Normalize(language);
    if(language!=null&&code==null)throw new ArgumentException("language_invalid");
    try{Live.Transfer.Write(FileFor(authenticatedSteam),new Preference{Language=code});}
    catch(Exception e){throw Texts.Tag(new IOException("preference_write_failed",e),"error.preference_write");}
    manual[authenticatedSteam]=code;
   }
  }
  // No server API currently verified for a session language. No automatic session cache exists.
  // A future verified adapter must bind its signal to Steam + client + connection generation.
  public Resolution Resolve(string authenticatedSteam,string serverDefault) {
   string selected=Get(authenticatedSteam);
   if(selected!=null)return new Resolution{Code=selected,Origin="language.origin.manual"};
   string configured=Languages.Normalize(serverDefault);
   return new Resolution{Code=configured??"en",Origin=configured==null?"language.origin.fallback":"language.origin.server"};
  }
 }
}
