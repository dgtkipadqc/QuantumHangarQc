using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Eleon.Modding;
using QuantumHangarQc.Live;
using QuantumHangarQc.Localization;
[assembly: AssemblyVersion("0.2.6.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2")]
[assembly: AssemblyTitle("Quantum Hangar Qc - Live State")]
namespace QuantumHangarQc.State {
 // Live structure observer plus targeted GPS removal. No ship or inventory mutations.
 public sealed class StateMod:IMod {
  IModApi api;readonly Dictionary<string,IPlayfield> fields=new Dictionary<string,IPlayfield>();
  UiBridge ui;GpsBridge gps;DateTime gpsNext;DateTime next;string directory;readonly object gate=new object();
  public void Init(IModApi value){api=value;if(api.Application.Mode!=ApplicationMode.PlayfieldServer){api.LogError("QH_STATE: PfServer requis");return;}Texts.Initialize(Path.Combine(Path.GetDirectoryName(typeof(StateMod).Assembly.Location),"Languages"),api.Log);gps=new GpsBridge(api.Log);ui=new UiBridge(api.Application,api.Log);api.Application.OnPlayfieldLoaded+=Loaded;api.Application.OnPlayfieldUnloading+=Unloaded;api.Application.Update+=Update;api.Log("QH_STATE START 0.2.6; observation + retrait GPS cible");}
  public void Shutdown(){if(api==null)return;if(ui!=null)ui.Clear();api.Application.OnPlayfieldLoaded-=Loaded;api.Application.OnPlayfieldUnloading-=Unloaded;api.Application.Update-=Update;lock(gate)fields.Clear();}
  void Loaded(IPlayfield pf){lock(gate)fields[pf.Name]=pf;api.Log("QH_STATE PLAYFIELD "+pf.Name);}
  void Unloaded(IPlayfield pf){lock(gate)fields.Remove(pf.Name);}
  void Update(){
   if(DateTime.UtcNow<next)return;next=DateTime.UtcNow.AddMilliseconds(100);
   try{
    if(directory==null){string save=api.Application.GetPathFor(AppFolder.SaveGame);if(string.IsNullOrWhiteSpace(save)||!Directory.Exists(save))return;directory=Path.Combine(save,"Mods","QuantumHangarQc","LiveState");Directory.CreateDirectory(directory);}
    if(DateTime.UtcNow>=gpsNext){gpsNext=DateTime.UtcNow.AddMilliseconds(500);IPlayfield[] snapshot;lock(gate)snapshot=fields.Values.ToArray();gps.Tick(Path.Combine(Path.GetDirectoryName(directory),"MapMarkers"),snapshot,DateTime.UtcNow);}
    IPlayfield[] uiFields;lock(gate)uiFields=fields.Values.ToArray();ui.Tick(Path.Combine(Path.GetDirectoryName(directory),"HangarUI"),uiFields,DateTime.UtcNow);
    foreach(string path in Directory.GetFiles(directory,"*.request.xml").OrderByDescending(File.GetLastWriteTimeUtc).Take(64)){
     StateRequest r;try{r=Transfer.Read<StateRequest>(path);}catch(Exception e){api.LogWarning("QH_STATE requete illisible: "+e.Message);continue;}
     Guid token;if(r==null||!Guid.TryParseExact(r.Token,"N",out token)||Path.GetFileName(path)!=r.Token+".request.xml")continue;
     IPlayfield pf;lock(gate)if(!fields.TryGetValue(r.Playfield??"",out pf))continue;
     string reply=Path.Combine(directory,r.Token+".reply.xml");if(File.Exists(reply))continue;
     // Expired requests cannot authorize anything. Host removes its own exchange files.
     if((DateTime.UtcNow-r.CreatedUtc).TotalSeconds>10||(r.CreatedUtc-DateTime.UtcNow).TotalSeconds>1)continue;
     var result=Capture(r,pf);Transfer.Write(reply,result);
     api.Log("QH_STATE SNAPSHOT token="+r.Token+" ship="+r.Entity+" powered="+result.Powered+" pilot="+result.Pilot+" dockedTo="+result.DockedTo+" occupants="+result.Occupants.Count+" playersSeen="+result.PlayersSeen+" requesterSeen="+result.RequesterSeen+" error="+result.Error);
    }
   }catch(Exception e){api.LogError("QH_STATE ERROR "+e.Message);}
  }
  public static StateReply Capture(StateRequest r,IPlayfield pf){
   var answer=new StateReply{Token=r.Token,Entity=r.Entity,Player=r.Player,Steam=r.Steam,Playfield=pf.Name,CapturedUtc=DateTime.UtcNow};
   try{
    if(r.Playfield!=pf.Name)throw new Exception("Secteur du capteur incorrect");
    // Requester identity is authenticated by ModHost through Request_Player_Info.
    // This observer grants no permission and performs no mutation. Player/Steam in
    // the reply are correlation data only, NOT native identity verification.
    // Native requester presence/SteamId must not gate a structure observation.
    var players=pf.Players.Values.ToList();
    answer.ObserverVersion="0.2.6";answer.PlayersSeen=players.Count;
    answer.RequesterSeen=players.Any(p=>p.Id==r.Player);
    IEntity e;if(!pf.Entities.TryGetValue(r.Entity,out e)||e.Structure==null)throw new Exception("Vaisseau absent du playfield charge");
    var s=e.Structure;
    answer.Ready=s.IsReady&&!e.IsProxy;answer.Type=(int)e.Type;answer.OwnerGroup=(int)e.Faction.Group;answer.OwnerId=e.Faction.Id;
    answer.Powered=s.IsPowered;answer.Pilot=s.Pilot==null?0:s.Pilot.Id;answer.DockedTo=e.DockedTo;
    answer.BlockCount=s.BlockCount;answer.DeviceCount=s.DeviceCount;
    answer.Docked=(s.GetDockedVessels()??new List<IStructure>()).Select(d=>d.Id).ToList();
    // Also use live reverse links so an attached craft is never mistaken for a free ship.
    foreach(var other in pf.Entities.Values){if(other.Id==e.Id)continue;if(other.DockedTo==e.Id&&!answer.Docked.Contains(other.Id))answer.Docked.Add(other.Id);}
    answer.Occupants=(s.GetPassengers()??new List<IPlayer>()).Select(p=>p.Id).ToList();
    foreach(var p in players)if((p.CurrentStructure!=null&&p.CurrentStructure.Id==e.Id)||(p.DrivingEntity!=null&&p.DrivingEntity.Id==e.Id))if(!answer.Occupants.Contains(p.Id))answer.Occupants.Add(p.Id);
    if(answer.Pilot>0&&!answer.Occupants.Contains(answer.Pilot))answer.Occupants.Add(answer.Pilot);
   }catch(Exception ex){answer.Ready=false;answer.Error=ex.Message;}
   return answer;
  }
 }
}
