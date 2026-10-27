using System;using System.IO;using System.Linq;using System.Diagnostics;using Eleon.Modding;using QuantumHangarQc.Live;
namespace QuantumHangarQc.State {
 // Read on Application.Update only. No database queries, entity or inventory mutations.
 public sealed class SectorBridge {
  readonly Action<string> log;public SectorBridge(Action<string> logger){log=logger;}
  public static SectorReply Capture(SectorRequest r,IPlayfield pf){
   var a=new SectorReply{Token=r.Token,Steam=r.Steam,Player=r.Player,Client=r.Client,Playfield=pf.Name,Protocol=1};var clock=Stopwatch.StartNew();
   try{
    if(r.Protocol!=1||r.Playfield!=pf.Name)throw new InvalidDataException("Sector protocol mismatch");
    int seen=0;
    foreach(var e in pf.Entities.Values){
     if(++seen>16384||clock.ElapsedMilliseconds>50)throw new InvalidDataException("Sector snapshot budget exceeded");
     var s=e.Structure;
     if(s==null){if((int)e.Type>=2&&(int)e.Type<=5)throw new InvalidDataException("Structure not loaded");continue;}
     if(a.Entities.Count>=SectorRules.MaxEntities)throw new InvalidDataException("Sector structure limit exceeded");
     // Proxies still occupy space and count as present. Store's targeted check requires readiness.
     var pos=typeof(IEntity).GetProperty("Position").GetValue(e,null);
     var quat=typeof(IEntity).GetProperty("Rotation").GetValue(e,null);
     var rot=quat.GetType().GetProperty("eulerAngles").GetValue(quat,null);
     a.Entities.Add(new SectorEntity{Id=e.Id,Type=(int)e.Type,Name=e.Name,OwnerGroup=(int)e.Faction.Group,OwnerId=e.Faction.Id,
      Blocks=s.BlockCount,Devices=s.DeviceCount,Powered=s.IsPowered,Pilot=s.Pilot==null?0:s.Pilot.Id,
      X=Axis(pos,"x"),Y=Axis(pos,"y"),Z=Axis(pos,"z"),RX=Axis(rot,"x"),RY=Axis(rot,"y"),RZ=Axis(rot,"z")});
    }
    if(clock.ElapsedMilliseconds>50)throw new InvalidDataException("Sector snapshot budget exceeded");
    a.Complete=true;a.CapturedUtc=DateTime.UtcNow;SectorRules.Validate(r,a,DateTime.UtcNow);
   }catch(Exception ex){a.Complete=false;a.Entities.Clear();a.Error=ex.Message;a.CapturedUtc=DateTime.UtcNow;}
   return a;
  }
  static float Axis(object v,string axis){return Convert.ToSingle(v.GetType().GetField(axis).GetValue(v),System.Globalization.CultureInfo.InvariantCulture);}
  public void Tick(string dir,IPlayfield[] fields,DateTime now){
   Directory.CreateDirectory(dir);
   foreach(var path in Directory.EnumerateFiles(dir,"*.request.xml").Take(64)){
    SectorRequest r;try{r=Transfer.Read<SectorRequest>(path);}catch(IOException){continue;}
    Guid token;if(r==null||!Guid.TryParseExact(r.Token,"N",out token)||Path.GetFileName(path)!=r.Token+".request.xml"||r.CreatedUtc>now.AddSeconds(1)||(now-r.CreatedUtc).TotalSeconds>10)continue;
    var pf=fields.FirstOrDefault(p=>p.Name==r.Playfield);if(pf==null)continue;
    string reply=Path.Combine(dir,r.Token+".reply.xml");if(File.Exists(reply))continue;
    var result=Capture(r,pf);Transfer.Write(reply,result);log("QH_SECTOR complete="+result.Complete+" count="+result.Entities.Count+" error="+result.Error);break;
   }
  }
 }
}
