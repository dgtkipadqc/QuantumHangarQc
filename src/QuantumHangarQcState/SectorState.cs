using System;using System.Collections.Generic;using System.Linq;using System.IO;
namespace QuantumHangarQc.Live {
 public class SectorRequest {public string Token,Steam,Playfield;public int Player,Client,Protocol=1;public DateTime CreatedUtc;}
 public class SectorEntity {public int Id,Type,OwnerGroup,OwnerId,Blocks,Devices,Pilot;public string Name;public float X,Y,Z,RX,RY,RZ;public bool Powered;public List<int> Docked=new List<int>();}
 public class SectorReply {public string Token,Steam,Playfield,Error;public int Player,Client,Protocol;public bool Complete;public DateTime CapturedUtc;public List<SectorEntity> Entities=new List<SectorEntity>();}
 public static class SectorRules {
  public const int MaxEntities=2048;
  public static void Validate(SectorRequest r,SectorReply a,DateTime now){
   if(a==null||a.Protocol!=1||!a.Complete||!string.IsNullOrEmpty(a.Error)||a.Token!=r.Token||a.Steam!=r.Steam||a.Player!=r.Player||a.Client!=r.Client||a.Playfield!=r.Playfield||a.CapturedUtc<r.CreatedUtc||a.CapturedUtc>now.AddSeconds(1)||(now-a.CapturedUtc).TotalSeconds>2)
    throw new InvalidDataException("QH_STATE sector incomplete, stale or mismatched: "+(a==null?"missing":a.Error));
   if(a.Entities==null||a.Entities.Count>MaxEntities||a.Entities.Any(e=>e==null||e.Id<=0||e.Type<0||e.Type>255||e.OwnerGroup<0||e.OwnerGroup>255||e.Blocks<0||e.Devices<0||e.Docked==null||e.Docked.Count>MaxEntities||e.Docked.Any(x=>x<=0)||e.Name==null||e.Name.Length>512||!new[]{e.X,e.Y,e.Z,e.RX,e.RY,e.RZ}.TrueForAllFinite())||a.Entities.Select(e=>e.Id).Distinct().Count()!=a.Entities.Count)
    throw new InvalidDataException("QH_STATE sector entities invalid");
  }
 }
}
