using System;
using System.Globalization;
namespace QuantumHangarQc.Live {
 public sealed class MarkerRequest {
  public string Token,Steam,Playfield,Name;
  public int Player;
  public float X,Y,Z;
  public bool Activate;
  public DateTime CreatedUtc,ExpiresUtc;
  public int RemoveDelaySeconds;
 }
 public sealed class MarkerReply {
  public string Token,Steam,Name,Error;
  public int Player;
  public bool Activate,Success;
 }
 public static class MarkerRules {
  // This is the actual Vector3i key used by marker add in build 5150.
  public static int Coordinate(float value){return checked((int)Math.Round(value,MidpointRounding.AwayFromZero));}
  public static int MapId(MarkerRequest r){return unchecked(Coordinate(r.X)*8976890+Coordinate(r.Y)*981131+Coordinate(r.Z));}
  public static string Label(MarkerRequest r){return "QH - Sortie "+r.Name.Substring(10,8);}
  public static bool Valid(MarkerRequest r) {
   Guid id,token;ulong steam;
   return r!=null && Guid.TryParseExact(r.Token,"N",out token) &&
    r.Name!=null && r.Name.StartsWith("QH_SORTIE_",StringComparison.Ordinal) && Guid.TryParseExact(r.Name.Substring(10),"N",out id) &&
    r.Player>0 && ulong.TryParse(r.Steam,NumberStyles.None,CultureInfo.InvariantCulture,out steam) && steam>0 && r.Steam==steam.ToString(CultureInfo.InvariantCulture) &&
    !string.IsNullOrWhiteSpace(r.Playfield) && Finite(r.X) && Finite(r.Y) && Finite(r.Z);
  }
  static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n)&&Math.Abs(n)<=100000000;}
  public static bool Same(MarkerRequest a,MarkerRequest b) {
   return a.Player==b.Player && a.Steam==b.Steam && a.Name==b.Name && a.Playfield==b.Playfield && a.X==b.X && a.Y==b.Y && a.Z==b.Z;
  }
 }
}
