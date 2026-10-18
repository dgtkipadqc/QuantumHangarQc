using System;using System.IO;using System.Linq;using QuantumHangarQc.Live;using QuantumHangarQc.State;
// Minimal native surface for terrain/geometry tests; never used to build shipped DLLs.
namespace Eleon.Modding {
 public struct VectorInt3{public int x,y,z;public VectorInt3(int a,int b,int c){x=a;y=b;z=c;}}
 public struct Vec{public float x,y,z;public Vec(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Quat{public float x,y,z,w;public Vec Euler;public Vec eulerAngles{get{return Euler;}}}
 public interface IEntity{IStructure Structure{get;}Vec Position{get;}Quat Rotation{get;}}
 public interface IStructure{VectorInt3 MinPos{get;}VectorInt3 MaxPos{get;}Vec StructToGlobalPos(VectorInt3 p);}
 public interface IPlayfield{string Name{get;}string PlayfieldType{get;}float GetTerrainHeightAt(float x,float z);}
}
class TerrainTests {
 static int n;static void Check(bool b,string s){n++;if(!b)throw new Exception(s);}
 static void Bad(Action a,string s){try{a();}catch{Check(true,s);return;}throw new Exception(s);}
 static HullBounds Hull(){return new HullBounds{MinX=-10,MaxX=10,MinZ=-10,MaxZ=10,MinY=-4,MaxY=6};}
 static TerrainRequest Request(){return new TerrainRequest{Token=Guid.NewGuid().ToString("N"),Steam="76561198000000001",Player=50,Client=7,Playfield="Planet",X=0,Y=10,Z=0,Clearance=5,Hull=Hull(),CreatedUtc=DateTime.UtcNow};}
 static TerrainReply Run(TerrainRequest r,string kind,Func<float,float,float> height){var scan=new TerrainScan(r,kind,height);while(!scan.Done)scan.Step(16);return scan.Reply;}
 class Field:Eleon.Modding.IPlayfield{public string Name{get;set;}public string PlayfieldType{get;set;}public int Calls;public float GetTerrainHeightAt(float x,float z){Calls++;return 10+x/2+z/4;}}
 class Ship:Eleon.Modding.IEntity,Eleon.Modding.IStructure{
  public Eleon.Modding.IStructure Structure{get{return this;}}public Eleon.Modding.Vec Position{get{return new Eleon.Modding.Vec(100,200,300);}}
  public Eleon.Modding.Quat Rotation{get{return new Eleon.Modding.Quat{x=.707f,w=.707f,Euler=new Eleon.Modding.Vec(90,0,0)};}}
  public Eleon.Modding.VectorInt3 MinPos{get{return new Eleon.Modding.VectorInt3(-1,-2,-3);}}public Eleon.Modding.VectorInt3 MaxPos{get{return new Eleon.Modding.VectorInt3(4,5,6);}}
  public Eleon.Modding.Vec StructToGlobalPos(Eleon.Modding.VectorInt3 p){return new Eleon.Modding.Vec(100+p.x*2,200-p.z*2,300+p.y*2);}
 }
 static void Main(string[] args){
  var r=Request();var a=Run(r,"Planet",(x,z)=>10+x/2+z/4);Check(a.Y==26.5f&&a.Samples==121,"slope highest corner plus lower hull and clearance");
  a=Run(r,"Planet",(x,z)=>x==0&&z==0?40:10);Check(a.Y==49,"interior peak sampled, not only corners");
  r.Y=100;a=Run(r,"Planet",(x,z)=>-20);Check(a.Y==-11&&a.Y+r.Hull.MinY==-15,"elevated marker does not add height above ground");
  r.Hull=null;a=Run(r,"Space",(x,z)=>{throw new Exception("space must not query terrain");});Check(a.Space&&a.Y==100&&a.Samples==0,"legacy space retrieval unchanged");
  Bad(()=>Run(r,"Planet",(x,z)=>0),"unknown legacy hull fails closed on planet");
  r=Request();Bad(()=>Run(r,"Unknown",(x,z)=>0),"unknown playfield fails closed");
  Bad(()=>Run(r,"Planet",(x,z)=>float.NaN),"NaN terrain rejected");Bad(()=>Run(r,"Planet",(x,z)=>float.PositiveInfinity),"infinite terrain rejected");
  r.Clearance=0;Bad(()=>Run(r,"Planet",(x,z)=>0),"clearance zero rejected");r.Clearance=float.NaN;Bad(()=>Run(r,"Planet",(x,z)=>0),"NaN config rejected");
  r=Request();r.Hull.MinX=r.Hull.MinZ=-1024;r.Hull.MaxX=r.Hull.MaxZ=1024;Bad(()=>Run(r,"Planet",(x,z)=>0),"oversize scan rejected");
  r=Request();r.Hull.MinX=float.NaN;Bad(()=>Run(r,"Planet",(x,z)=>0),"invalid bounds rejected");
  r=Request();r.Clearance=0.5f;a=Run(r,"Planet",(x,z)=>10);Check(a.Y+r.Hull.MinY==10.5f,"bottom edge only half a metre above flat ground");
  r.Clearance=0.25f;a=Run(r,"Planet",(x,z)=>10);Check(a.Y+r.Hull.MinY==10.25f,"minimum clearance accepted");
  r.Clearance=0.24f;Bad(()=>Run(r,"Planet",(x,z)=>0),"clearance below minimum rejected");
  r=Request();r.Y=100;r.Clearance=2;a=Run(r,"Planet",(x,z)=>10);Check(a.Y==16&&a.Y+r.Hull.MinY==12,"2m clearance despite marker 90m above terrain");
  r.Hull.MinY=40;r.Hull.MaxY=50;a=Run(r,"Planet",(x,z)=>10);Check(a.Y==-28&&a.Y+r.Hull.MinY==12,"positive lower hull offset still places bottom 2m above terrain");
  Check(a.PlacementVersion==2&&a.GroundY==10,"reply carries signed-placement version and sampled ground");
  r=Request();int calls=0;var scan=new TerrainScan(r,"Planet",(x,z)=>{calls++;return 0;});scan.Step(7);Check(calls==7&&!scan.Done,"work budget bounded");
  while(!scan.Done)scan.Step(7);Check(calls==121,"no duplicate or skipped grid samples");
  var h=TerrainBridge.CaptureHull(new Ship());Check(h.MinX==-3&&h.MaxX==9&&h.MinY==-13&&h.MaxY==7&&h.MinZ==-5&&h.MaxZ==11,"native scale, rotation and half-block edges included");
  Check(h.RotX==90&&h.RotY==0,"native quaternion converted to Euler angles");
  string dir=args[0];Directory.CreateDirectory(dir);r=Request();r.Hull.MinX=r.Hull.MinZ=-40;r.Hull.MaxX=r.Hull.MaxZ=40;
  string request=Path.Combine(dir,r.Token+".request.xml"),reply=Path.Combine(dir,r.Token+".reply.xml");Transfer.Write(request,r);
  var pf=new Field{Name=r.Playfield,PlayfieldType="Planet"};var bridge=new TerrainBridge(x=>{});
  bridge.Tick(dir,new[]{pf},DateTime.UtcNow);Check(pf.Calls==256&&!File.Exists(reply),"bridge spread over updates");
  for(int i=0;i<10&&!File.Exists(reply);i++)bridge.Tick(dir,new[]{pf},DateTime.UtcNow);
  var answer=Transfer.Read<TerrainReply>(reply);Check(answer.Error==null&&answer.Samples==1681&&answer.Token==r.Token&&answer.Client==r.Client,"correlated successful bridge response");
  int previous=pf.Calls;bridge.Tick(dir,new[]{pf},DateTime.UtcNow);Check(pf.Calls==previous,"completed request not scanned again");
  File.Delete(request);File.Delete(reply);r=Request();r.Hull.MinX=r.Hull.MinZ=-40;r.Hull.MaxX=r.Hull.MaxZ=40;request=Path.Combine(dir,r.Token+".request.xml");reply=Path.Combine(dir,r.Token+".reply.xml");Transfer.Write(request,r);
  bridge.Tick(dir,new[]{pf},DateTime.UtcNow);bridge.Tick(dir,new Eleon.Modding.IPlayfield[0],DateTime.UtcNow);Check(Transfer.Read<TerrainReply>(reply).Error=="error.terrain_unavailable","unloaded field refuses spawn");
  File.Delete(request);File.Delete(reply);r=Request();r.CreatedUtc=DateTime.UtcNow.AddMinutes(-1);Transfer.Write(Path.Combine(dir,r.Token+".request.xml"),r);previous=pf.Calls;bridge.Tick(dir,new[]{pf},DateTime.UtcNow);Check(pf.Calls==previous,"expired request ignored");
  Console.WriteLine("TERRAIN PASS "+n);
 }
}
