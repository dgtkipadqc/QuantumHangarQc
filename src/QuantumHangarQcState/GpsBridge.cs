using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Eleon.Modding;
using QuantumHangarQc.Live;
namespace QuantumHangarQc.State {
 // Uses the existing, targeted playfield -> player PDA marker removal packet.
 // No game binary patch, database edit, broadcast or client installation.
 internal sealed class NativeGpsRemoval {
  readonly ConstructorInfo constructor;
  readonly FieldInfo singleton;
  readonly MethodInfo connectionFor,send;
  readonly object operation;
  public NativeGpsRemoval() {
   var assembly=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp");
   if(assembly.ManifestModule.ModuleVersionId!=new Guid("6f096e23-09d8-4159-b44a-baeca78318ee"))
    throw new NotSupportedException("GPS: Assembly-CSharp non valide pour cet adaptateur (build 5150 requis), MVID="+assembly.ManifestModule.ModuleVersionId);
   var module=assembly.ManifestModule;
   constructor=(ConstructorInfo)module.ResolveMethod(0x06000000+15082);
   connectionFor=(MethodInfo)module.ResolveMethod(0x06000000+23293);
   send=(MethodInfo)module.ResolveMethod(0x06000000+23294);
   singleton=module.ResolveField(0x04000000+21742);
   if(constructor.DeclaringType.FullName!="Assembly-CSharp.ToolbarEditor"||constructor.GetParameters().Length!=4||send.Name!="FormatBitmap"||connectionFor.Name!="BuildNode"||singleton.DeclaringType.FullName!="Assembly-CSharp.ControlToken")throw new NotSupportedException("GPS: signatures natives inattendues");
   operation=Enum.ToObject(constructor.GetParameters()[0].ParameterType,10);
  }
  public void Check(int player) {
   if(player<=0)throw new InvalidDataException("Destinataire GPS invalide");
   object network=singleton.GetValue(null);
   if(network==null||connectionFor.Invoke(network,new object[]{player})==null)throw new InvalidOperationException("Connexion native du joueur indisponible");
  }
  public void Remove(int player,int markerId) {
   if(markerId==0)throw new InvalidDataException("Identifiant GPS nul");
   Check(player);
   object packet=constructor.Invoke(new object[]{operation,true,null,markerId});
   send.Invoke(singleton.GetValue(null),new object[]{packet,player});
  }
 }
 internal sealed class GpsBridge {
  readonly Action<string> log;
  internal Action<int> Check;
  internal Action<int,int> Send;
  NativeGpsRemoval native;
  internal GpsBridge(Action<string> logger){log=logger;}
  void Ready(int player){
   if(Check!=null){Check(player);return;}
   if(native==null)native=new NativeGpsRemoval();native.Check(player);
  }
  void Remove(MarkerRequest r){
   if(Send!=null)Send(r.Player,MarkerRules.MapId(r));else {Ready(r.Player);native.Remove(r.Player,MarkerRules.MapId(r));}
   log("QH_GPS REMOVE_SENT player="+r.Player+" name="+r.Name+" mapId="+MarkerRules.MapId(r)+" (affichage client non acquitte)");
  }
  static bool PlayerMatches(MarkerRequest r,IPlayfield pf){
   var p=pf.Players.Values.FirstOrDefault(value=>value.Id==r.Player);
   return pf.Name==r.Playfield&&p!=null&&p.SteamId==r.Steam;
  }
  internal void Tick(string directory,IPlayfield[] fields,DateTime now){
   Directory.CreateDirectory(directory);
   foreach(string stale in Directory.GetFiles(directory,"*.request.xml").Where(f=>now-File.GetLastWriteTimeUtc(f)>TimeSpan.FromSeconds(30)).Take(256)){
    try{File.Delete(stale);string reply=stale.Replace(".request.xml",".reply.xml");if(File.Exists(reply))File.Delete(reply);}catch(IOException){}
   }
   foreach(string path in Directory.GetFiles(directory,"*.request.xml").OrderByDescending(File.GetLastWriteTimeUtc).Take(64)) {
    MarkerRequest r;
    try{r=Transfer.Read<MarkerRequest>(path);}catch{continue;}
    if(!MarkerRules.Valid(r)||Path.GetFileName(path)!=r.Token+".request.xml")continue;
    var pf=fields.FirstOrDefault(f=>f.Name==r.Playfield);if(pf==null)continue;
    string reply=Path.Combine(directory,r.Token+".reply.xml");if(File.Exists(reply))continue;
    if(now-r.CreatedUtc>TimeSpan.FromSeconds(10)||r.CreatedUtc-now>TimeSpan.FromSeconds(1))continue;
    var answer=new MarkerReply{Token=r.Token,Steam=r.Steam,Name=r.Name,Player=r.Player,Activate=r.Activate};
    try{
     string lease=Path.Combine(directory,r.Name+".lease.xml");
     if(r.Activate){
      if(!PlayerMatches(r,pf))throw new InvalidOperationException("Joueur/Steam absent du playfield GPS");
      if(r.ExpiresUtc<=now||r.ExpiresUtc>now.AddMinutes(5).AddSeconds(1)||MarkerRules.MapId(r)==0)throw new InvalidDataException("Duree ou identifiant GPS invalide");
      Ready(r.Player);
      // A later request must never inherit a pending deletion for the same map key.
      foreach(string previous in Directory.GetFiles(directory,"*.lease.xml")){
       var old=Transfer.Read<MarkerRequest>(previous);
       if(old.Name!=r.Name&&old.Steam==r.Steam&&old.Player==r.Player&&old.Playfield==r.Playfield&&MarkerRules.MapId(old)==MarkerRules.MapId(r)){
        Remove(old);File.Delete(previous);
       }
      }
      Transfer.Write(lease,r);
      log("QH_GPS ARMED player="+r.Player+" name="+r.Name+" mapId="+MarkerRules.MapId(r)+" deadline="+r.ExpiresUtc.ToString("O"));
     }else if(File.Exists(lease)){
      var old=Transfer.Read<MarkerRequest>(lease);
      if(!MarkerRules.Same(old,r))throw new InvalidDataException("Identite du repere GPS differente");
      if(r.RemoveDelaySeconds<0||r.RemoveDelaySeconds>8)throw new InvalidDataException("Delai GPS invalide");
      if(r.RemoveDelaySeconds==0){
       if(PlayerMatches(old,pf))Remove(old);
       File.Delete(lease);
      }else{
       DateTime due=now.AddSeconds(r.RemoveDelaySeconds);if(due<old.ExpiresUtc)old.ExpiresUtc=due;
       Transfer.Write(lease,old);
       log("QH_GPS REMOVE_SCHEDULED player="+r.Player+" name="+r.Name+" due="+old.ExpiresUtc.ToString("O"));
      }
     }
     answer.Success=true;
    }catch(Exception e){answer.Error=e.GetBaseException().Message;log("QH_GPS ERROR "+answer.Error);}
    Transfer.Write(reply,answer);
   }
   foreach(string path in Directory.GetFiles(directory,"*.lease.xml").Take(256)){
    try{
     var r=Transfer.Read<MarkerRequest>(path);
     if(!MarkerRules.Valid(r)||Path.GetFileName(path)!=r.Name+".lease.xml"||now<r.ExpiresUtc)continue;
     var pf=fields.FirstOrDefault(f=>f.Name==r.Playfield);if(pf==null)continue;
     if(!PlayerMatches(r,pf)){
      // Do not send a position key into another playfield/session. Keep for a return.
      if(now-r.ExpiresUtc>TimeSpan.FromDays(1))File.Delete(path);
      continue;
     }
     Remove(r);File.Delete(path);
    }catch(Exception e){log("QH_GPS REMOVE_ERROR "+e.GetBaseException().Message);}
   }
  }
 }
}
