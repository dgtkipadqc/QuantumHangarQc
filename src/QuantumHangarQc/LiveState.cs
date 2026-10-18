using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
namespace QuantumHangarQc.Live {
 public class StateRequest {public string Token,Playfield,Steam;public int Entity,Player;public DateTime CreatedUtc;}
 public class StateReply {
  public string Token,Playfield,Steam,Error;public int Entity,Player,Type,OwnerGroup,OwnerId,Pilot,DockedTo,BlockCount,DeviceCount;
  public HullBounds Hull;public string ObserverVersion;public int PlayersSeen;public bool RequesterSeen;
  public bool Ready,Powered;public DateTime CapturedUtc;
  public List<int> Docked=new List<int>(),Occupants=new List<int>();
 }
 public class HullBounds {
  public float MinX,MinY,MinZ,MaxX,MaxY,MaxZ,RotX,RotY,RotZ;
  public bool Valid() {return new[]{MinX,MinY,MinZ,MaxX,MaxY,MaxZ,RotX,RotY,RotZ}.TrueForAllFinite() && MinX<MaxX && MinY<MaxY && MinZ<MaxZ && Math.Max(Math.Max(Math.Abs(MinX),Math.Abs(MaxX)),Math.Max(Math.Abs(MinZ),Math.Abs(MaxZ)))<=1024 && Math.Abs(MinY)<=1024 && Math.Abs(MaxY)<=1024;}
 }
 public static class FloatChecks {
  public static bool TrueForAllFinite(this float[] values){foreach(float v in values)if(float.IsNaN(v)||float.IsInfinity(v))return false;return true;}
 }
 public class TerrainRequest {public string Token,Steam,Playfield;public int Player,Client;public float X,Y,Z,Clearance;public HullBounds Hull;public DateTime CreatedUtc;}
 public class TerrainReply {public string Token,Steam,Playfield,Error;public int Player,Client,Samples;public float X,Y,Z;public bool Space;public DateTime CapturedUtc;}
 // Incremental sampling: at most 256 API calls per playfield update, on its own thread.
 public sealed class TerrainScan {
  readonly TerrainRequest request;readonly Func<float,float,float> height;readonly int nx,nz;
  int index;float highest=float.NegativeInfinity;
  public readonly TerrainReply Reply;
  public bool Done {get;private set;}
  public TerrainScan(TerrainRequest r,string kind,Func<float,float,float> getHeight){
   request=r;height=getHeight;Reply=new TerrainReply{Token=r.Token,Steam=r.Steam,Playfield=r.Playfield,Player=r.Player,Client=r.Client,X=r.X,Y=r.Y,Z=r.Z};
   if(!new[]{r.X,r.Y,r.Z,r.Clearance}.TrueForAllFinite()||r.Clearance<1||r.Clearance>50)throw new InvalidDataException("error.terrain_unavailable");
   if(string.Equals(kind,"Space",StringComparison.OrdinalIgnoreCase)){Reply.Space=true;Finish();return;}
   if(!string.Equals(kind,"Planet",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("error.terrain_unavailable");
   if(r.Hull==null)throw new InvalidDataException("error.terrain_legacy");
   if(!r.Hull.Valid())throw new InvalidDataException("error.terrain_unavailable");
   nx=(int)Math.Ceiling((r.Hull.MaxX-r.Hull.MinX)/2.0);nz=(int)Math.Ceiling((r.Hull.MaxZ-r.Hull.MinZ)/2.0);
   if((long)(nx+1)*(nz+1)>16384)throw new InvalidDataException("error.terrain_size");
  }
  public void Step(int budget){
   while(!Done&&budget-->0){
    int ix=index%(nx+1),iz=index/(nx+1);
    float x=request.X+request.Hull.MinX+(request.Hull.MaxX-request.Hull.MinX)*ix/nx;
    float z=request.Z+request.Hull.MinZ+(request.Hull.MaxZ-request.Hull.MinZ)*iz/nz;
    float y=height(x,z);if(float.IsNaN(y)||float.IsInfinity(y))throw new InvalidDataException("error.terrain_unavailable");
    highest=Math.Max(highest,y);index++;Reply.Samples=index;
    if(index==(nx+1)*(nz+1)){
     Reply.Y=Math.Max(request.Y,highest)-Math.Min(0,request.Hull.MinY)+request.Clearance;
     if(!new[]{Reply.Y}.TrueForAllFinite())throw new InvalidDataException("error.terrain_unavailable");
     Finish();
    }
   }
  }
  void Finish(){Done=true;Reply.CapturedUtc=DateTime.UtcNow;}
 }
 public static class Transfer {
  public static T Read<T>(string path){using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete))using(var r=XmlReader.Create(f,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=65536}))return (T)new XmlSerializer(typeof(T)).Deserialize(r);}
  public static void Write<T>(string path,T value){
   Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(T)).Serialize(f,value);f.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
