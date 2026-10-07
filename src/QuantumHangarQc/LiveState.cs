using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
namespace QuantumHangarQc.Live {
 public class StateRequest {public string Token,Playfield,Steam;public int Entity,Player;public DateTime CreatedUtc;}
 public class StateReply {
  public string Token,Playfield,Steam,Error;public int Entity,Player,Type,OwnerGroup,OwnerId,Pilot,DockedTo,BlockCount,DeviceCount;
  public string ObserverVersion;public int PlayersSeen;public bool RequesterSeen;
  public bool Ready,Powered;public DateTime CapturedUtc;
  public List<int> Docked=new List<int>(),Occupants=new List<int>();
 }
 public static class Transfer {
  public static T Read<T>(string path){using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read|FileShare.Delete))using(var r=XmlReader.Create(f,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=65536}))return (T)new XmlSerializer(typeof(T)).Deserialize(r);}
  public static void Write<T>(string path,T value){
   Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(T)).Serialize(f,value);f.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
 }
}
