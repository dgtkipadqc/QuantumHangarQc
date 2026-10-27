using System;using System.IO;using System.Linq;using System.Collections.Generic;using QuantumHangarQc.Live;using QuantumHangarQc.State;
namespace Eleon.Modding {
 public struct Vec{public float x,y,z;}
 public struct Quat{public Vec eulerAngles{get;set;}}
 public struct Faction{public int Group,Id;}
 public interface IPlayer{int Id{get;}}
 public interface IStructure{int BlockCount{get;}int DeviceCount{get;}bool IsPowered{get;}IPlayer Pilot{get;}}
 public interface IEntity{int Id{get;}int Type{get;}string Name{get;}Faction Faction{get;}IStructure Structure{get;}Vec Position{get;}Quat Rotation{get;}}
 public interface IPlayfield{string Name{get;}Dictionary<int,IEntity> Entities{get;}}
}
class NativeSectorTests {
 static int n;static void Check(bool ok,string message){n++;if(!ok)throw new Exception(message);}
 class Entity:Eleon.Modding.IEntity,Eleon.Modding.IStructure{
  public int Id{get;set;}public int Type{get;set;}public string Name{get;set;}public Eleon.Modding.Faction Faction{get;set;}public bool Missing;public float X=80;
  public Eleon.Modding.IStructure Structure{get{return Missing?null:this;}}public Eleon.Modding.Vec Position{get{return new Eleon.Modding.Vec{x=X,y=-12,z=30};}}public Eleon.Modding.Quat Rotation{get{return new Eleon.Modding.Quat{eulerAngles=new Eleon.Modding.Vec{y=90}};}}
  public int BlockCount{get{return 45;}}public int DeviceCount{get{return 12;}}public bool IsPowered{get{return false;}}public Eleon.Modding.IPlayer Pilot{get{return null;}}
 }
 class Field:Eleon.Modding.IPlayfield{public string Name{get;set;}public Dictionary<int,Eleon.Modding.IEntity> Entities{get;set;}}
 static SectorRequest Request(){return new SectorRequest{Token=Guid.NewGuid().ToString("N"),Steam="76561198000000001",Player=50,Client=7,Playfield="Space",CreatedUtc=DateTime.UtcNow};}
 static void Main(string[] args){
  var pf=new Field{Name="Space",Entities=new Dictionary<int,Eleon.Modding.IEntity>()};var e=new Entity{Id=200,Type=4,Name="Native ship",Faction=new Eleon.Modding.Faction{Group=1,Id=50}};pf.Entities.Add(e.Id,e);
  var r=Request();var a=SectorBridge.Capture(r,pf);Check(a.Complete&&a.Entities.Count==1,"native capture complete");var s=a.Entities[0];Check(s.Id==200&&s.Name==e.Name&&s.Type==4&&s.OwnerId==50&&s.OwnerGroup==1,"native identity preserved");Check(s.X==80&&s.Y==-12&&s.Z==30&&s.RY==90&&s.Blocks==45&&s.Devices==12,"native position rotation counters preserved");SectorRules.Validate(r,a,DateTime.UtcNow);Check(true,"capture passes correlation and freshness rules");
  e.X=float.NaN;Check(!SectorBridge.Capture(Request(),pf).Complete,"invalid native geometry fails closed");e.X=80;e.Missing=true;Check(!SectorBridge.Capture(Request(),pf).Complete,"unloaded structure never interpreted as absence");e.Type=0;Check(SectorBridge.Capture(Request(),pf).Entities.Count==0,"non-structure entity ignored");e.Type=4;e.Missing=false;
  r=Request();r.Protocol=0;Check(!SectorBridge.Capture(r,pf).Complete,"old protocol rejected");r=Request();r.Playfield="Other";Check(!SectorBridge.Capture(r,pf).Complete,"wrong field rejected");
  pf.Entities.Clear();Check(SectorBridge.Capture(Request(),pf).Complete,"empty loaded sector is valid");
  for(int i=1;i<=2049;i++)pf.Entities.Add(i,new Entity{Id=i,Type=4,Name="ship"});a=SectorBridge.Capture(Request(),pf);Check(!a.Complete&&a.Entities.Count==0,"over-limit or over-budget snapshot returns no partial entities");pf.Entities.Clear();pf.Entities.Add(200,e);
  string dir=args[0];Directory.CreateDirectory(dir);var bridge=new SectorBridge(x=>{});r=Request();string request=Path.Combine(dir,r.Token+".request.xml"),reply=Path.Combine(dir,r.Token+".reply.xml");Transfer.Write(request,r);
  bridge.Tick(dir,new Eleon.Modding.IPlayfield[0],DateTime.UtcNow);Check(!File.Exists(reply),"unloaded field yields no false empty snapshot");bridge.Tick(dir,new[]{pf},DateTime.UtcNow);Check(Transfer.Read<SectorReply>(reply).Complete,"real bridge writes complete reply");string before=File.ReadAllText(reply);bridge.Tick(dir,new[]{pf},DateTime.UtcNow);Check(File.ReadAllText(reply)==before,"reply not replayed");
  File.Delete(request);File.Delete(reply);r=Request();r.CreatedUtc=DateTime.UtcNow.AddSeconds(-20);Transfer.Write(Path.Combine(dir,r.Token+".request.xml"),r);bridge.Tick(dir,new[]{pf},DateTime.UtcNow);Check(!Directory.GetFiles(dir,"*.reply.xml").Any(),"expired request ignored");
  Console.WriteLine("NATIVE SECTOR PASS "+n+"; real SectorBridge with simulated native surface; production separately compiles against real ModApi.");
 }
}
