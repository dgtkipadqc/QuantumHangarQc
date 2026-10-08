using System;using System.IO;using System.Linq;using System.Text;using System.Collections.Generic;using QuantumHangarQc.Localization;
class FallbackTests {
 static int checks;static void Check(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static void Main(string[] args){
  string source=args[0],dir=args[1];Directory.CreateDirectory(dir);var logs=new List<string>();
  File.WriteAllText(Path.Combine(dir,"fr.xml"),"broken XML",Encoding.UTF8);
  string german=File.ReadAllText(Path.Combine(source,"de.xml"));
  german=System.Text.RegularExpressions.Regex.Replace(german,"<Text Key=\"button.close\".*?</Text>","");File.WriteAllText(Path.Combine(dir,"de.xml"),german,new UTF8Encoding(false));
  string italian=File.ReadAllText(Path.Combine(source,"it.xml")).Replace("Key=\"button.close\" Args=\"0\"","Key=\"button.close\" Args=\"1\"");File.WriteAllText(Path.Combine(dir,"it.xml"),italian,new UTF8Encoding(false));
  File.WriteAllText(Path.Combine(dir,"en.xml"),"broken English override",Encoding.UTF8);
  Texts.Initialize(dir,logs.Add);
  Check(Texts.Render("fr","button.close")=="Close","corrupt catalog falls back to embedded English");
  Check(Texts.Render("de","button.close")=="Close","individual missing key uses English");
  Check(Texts.Render("de","button.cancel")=="Abbrechen","partial valid catalog preserves present translations");
  Check(Texts.Render("it","button.cancel")=="Cancel","invalid argument contract rejects catalog");
  Check(Texts.Render("el","button.close")=="Close","missing catalog falls back");
  File.Delete(Path.Combine(dir,"de.xml"));for(int i=0;i<10000;i++)Check(Texts.Render("de","button.cancel")=="Abbrechen","catalog cached without disk file");
  for(int i=0;i<1000;i++)Texts.Render("fr","button.close");Check(logs.Count(x=>x=="QH_LOCALE CATALOG_FALLBACK fr")==1,"fallback warning is logged once");
  Console.WriteLine("PASS "+checks+" fallback/cache assertions. Local files only.");
 }
}
