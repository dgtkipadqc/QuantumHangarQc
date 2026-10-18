using System;
using System.IO;
using System.Linq;
using Eleon.Modding;
using QuantumHangarQc.Live;
namespace QuantumHangarQc.State {
 // No entity mutation. Calls the native terrain API only on Application.Update.
 public sealed class TerrainBridge {
  readonly Action<string> log;TerrainScan scan;TerrainRequest request;IPlayfield field;string pendingPath,replyPath;
  public TerrainBridge(Action<string> logger){log=logger;}
  public static HullBounds CaptureHull(IEntity entity){
   var s=entity.Structure;var min=s.MinPos;var max=s.MaxPos;
   if(min.x>max.x||min.y>max.y||min.z>max.z)throw new InvalidDataException("Hull bounds unavailable");
   // Reflection avoids a compile-time Unity dependency in the observer. The vectors
   // and conversion still come from the installed native ModApi, not guessed units.
   var position=typeof(IEntity).GetProperty("Position").GetValue(entity,null);
   var quaternion=typeof(IEntity).GetProperty("Rotation").GetValue(entity,null);
   var rotation=quaternion.GetType().GetProperty("eulerAngles").GetValue(quaternion,null);
   var convert=typeof(IStructure).GetMethod("StructToGlobalPos");
   var h=new HullBounds{MinX=float.PositiveInfinity,MinY=float.PositiveInfinity,MinZ=float.PositiveInfinity,MaxX=float.NegativeInfinity,MaxY=float.NegativeInfinity,MaxZ=float.NegativeInfinity,RotX=Axis(rotation,"x"),RotY=Axis(rotation,"y"),RotZ=Axis(rotation,"z")};
   for(int i=0;i<8;i++){
    // Interpolate to half a block beyond the extreme block centres.
    // This covers block edges without adding a whole extra block of air.
    var cell=new VectorInt3((i&1)==0?checked(min.x-1):checked(max.x+1),(i&2)==0?checked(min.y-1):checked(max.y+1),(i&4)==0?checked(min.z-1):checked(max.z+1));
    object outer=convert.Invoke(s,new object[]{cell});
    var innerCell=new VectorInt3((i&1)==0?min.x:max.x,(i&2)==0?min.y:max.y,(i&4)==0?min.z:max.z);
    object inner=convert.Invoke(s,new object[]{innerCell});
    float x=(Axis(outer,"x")+Axis(inner,"x"))/2-Axis(position,"x"),y=(Axis(outer,"y")+Axis(inner,"y"))/2-Axis(position,"y"),z=(Axis(outer,"z")+Axis(inner,"z"))/2-Axis(position,"z");
    if(!new[]{x,y,z}.TrueForAllFinite())throw new InvalidDataException("Hull conversion invalid");
    h.MinX=Math.Min(h.MinX,x);h.MinY=Math.Min(h.MinY,y);h.MinZ=Math.Min(h.MinZ,z);
    h.MaxX=Math.Max(h.MaxX,x);h.MaxY=Math.Max(h.MaxY,y);h.MaxZ=Math.Max(h.MaxZ,z);
   }
   if(!h.Valid())throw new InvalidDataException("Hull conversion unavailable");return h;
  }
  static float Axis(object v,string name){if(v==null)throw new InvalidDataException("Native vector missing");return Convert.ToSingle(v.GetType().GetField(name).GetValue(v),System.Globalization.CultureInfo.InvariantCulture);}
  public void Tick(string directory,IPlayfield[] fields,DateTime now){
   Directory.CreateDirectory(directory);
   if(request!=null&&(!File.Exists(pendingPath)||now-request.CreatedUtc>TimeSpan.FromSeconds(20))){Clear();}
   if(request==null){
    foreach(string path in Directory.GetFiles(directory,"*.request.xml").Where(p=>now-File.GetLastWriteTimeUtc(p)<=TimeSpan.FromSeconds(25)).OrderBy(File.GetLastWriteTimeUtc).Take(64)){
     TerrainRequest r;try{r=Transfer.Read<TerrainRequest>(path);}catch{continue;}
     Guid token;if(r==null||!Guid.TryParseExact(r.Token,"N",out token)||Path.GetFileName(path)!=r.Token+".request.xml"||now-r.CreatedUtc>TimeSpan.FromSeconds(20)||r.CreatedUtc>now.AddSeconds(1))continue;
     string reply=Path.Combine(directory,r.Token+".reply.xml");if(File.Exists(reply))continue;
     var pf=fields.FirstOrDefault(p=>p.Name==r.Playfield);if(pf==null)continue;
     request=r;field=pf;pendingPath=path;replyPath=reply;break;
    }
   }
   if(request==null)return;
   try {
    if(!fields.Any(p=>object.ReferenceEquals(p,field)))throw new InvalidOperationException("Playfield unloaded");
    if(scan==null)scan=new TerrainScan(request,field.PlayfieldType,field.GetTerrainHeightAt);
    scan.Step(256);
    if(scan.Done){Transfer.Write(replyPath,scan.Reply);log("QH_TERRAIN samples="+scan.Reply.Samples+" space="+scan.Reply.Space+" spawnY="+scan.Reply.Y+" markerY="+request.Y+" groundY="+scan.Reply.GroundY+" hullMinY="+(request.Hull==null?"none":request.Hull.MinY.ToString(System.Globalization.CultureInfo.InvariantCulture))+" clearance="+request.Clearance+" placementVersion="+scan.Reply.PlacementVersion);Clear();}
   }catch(Exception e){
    string code=e.Message=="error.terrain_legacy"||e.Message=="error.terrain_size"?e.Message:"error.terrain_unavailable";
    Transfer.Write(replyPath,new TerrainReply{Token=request.Token,Steam=request.Steam,Player=request.Player,Client=request.Client,Playfield=request.Playfield,Error=code,CapturedUtc=DateTime.UtcNow});
    log("QH_TERRAIN ERROR "+e.Message);Clear();
   }
  }
  void Clear(){scan=null;request=null;field=null;pendingPath=null;replyPath=null;}
 }
}
