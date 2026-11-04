using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace QuantumHangarQc {
    public sealed class GiftIntent {
        public string Id, RequestHash, Donor, Recipient, ArchiveId, ManifestHash;
        public long Expires;
    }
    public sealed class GiftRecord {
        public string Id, RequestHash, Donor, Recipient, ArchiveId, State, Reason;
        public int Slot;
        public byte[] Before, After;
        public List<FileDigest> Files = new List<FileDigest>();
    }
    // Only the native mod transfers ownership. It moves one directory, never clones a usable archive.
    public sealed class GiftEngine {
        readonly ArchiveStore store; readonly string journal; readonly object sync=new object();
        public Action<string> Fault = stage=>{};
        public Func<long> Now=()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public bool Blocked { get; private set; }
        public GiftEngine(ArchiveStore value,string directory) {
            store=value;journal=Path.GetFullPath(directory);Safe(store.Root);Safe(journal);Directory.CreateDirectory(journal);
        }
        public static bool Hex(string s,int n) { return s!=null&&Regex.IsMatch(s,"\\A[a-f0-9]{"+n+"}\\z"); }
        public static bool Steam(string s) { return s!=null&&Regex.IsMatch(s,"\\A[0-9]{17}\\z")&&s!="00000000000000000"; }
        public static string Hash(byte[] bytes) { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        public static T Parse<T>(byte[] bytes) {
            if(bytes==null||bytes.Length>4*1024*1024)throw new InvalidDataException("RECORD_INVALID");
            using(var m=new MemoryStream(bytes))using(var r=XmlReader.Create(m,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=4*1024*1024}))return (T)new XmlSerializer(typeof(T)).Deserialize(r);
        }
        public static byte[] Bytes<T>(T value) { using(var m=new MemoryStream()){new XmlSerializer(typeof(T)).Serialize(m,value);return m.ToArray();} }
        public static void Safe(string path) {
            for(var p=Path.GetFullPath(path);p!=null;p=Path.GetDirectoryName(p))
                if((File.Exists(p)||Directory.Exists(p))&&(File.GetAttributes(p)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("LINK_REFUSED");
        }
        static void AtomicBytes(string path,byte[] bytes) {
            Safe(path);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
        string RecordPath(string id) { if(!Hex(id,32))throw new InvalidDataException("REQUEST_INVALID");var p=Path.Combine(journal,id+".xml");Safe(p);return p; }
        void Save(GiftRecord r) { Disk.Write(RecordPath(r.Id),r); }
        static string Manifest(string dir) { var p=Path.Combine(dir,"Manifest.xml");Safe(p);return p; }
        void Walk(string root,string dir,List<FileDigest> files) {
            Safe(dir);
            foreach(var f in Directory.GetFiles(dir).OrderBy(x=>x,StringComparer.Ordinal)) {
                Safe(f);var relative=f.Substring(root.Length+1);
                if(relative=="Manifest.xml")continue;
                if(files.Count>=10000)throw new InvalidDataException("ARCHIVE_TOO_MANY_FILES");
                files.Add(new FileDigest{Path=relative,Bytes=new FileInfo(f).Length,Sha256=Disk.Hash(f)});
            }
            foreach(var d in Directory.GetDirectories(dir).OrderBy(x=>x,StringComparer.Ordinal))Walk(root,d,files);
        }
        List<FileDigest> Snapshot(string dir) { var files=new List<FileDigest>();Walk(dir,dir,files);return files; }
        void VerifySnapshot(string dir,List<FileDigest> expected) {
            var actual=Snapshot(dir);
            if(actual.Count!=expected.Count||actual.Where((f,i)=>f.Path!=expected[i].Path||f.Bytes!=expected[i].Bytes||f.Sha256!=expected[i].Sha256).Any())throw new InvalidDataException("ARCHIVE_CHANGED");
        }
        void ValidateRecord(GiftRecord r) {
            if(!Hex(r.Id,32)||!Hex(r.RequestHash,64)||!Steam(r.Donor)||!Steam(r.Recipient)||r.Donor==r.Recipient||!Hex(r.ArchiveId,32))throw new InvalidDataException("JOURNAL_INVALID");
            if(r.Before==null||r.After==null)throw new InvalidDataException("JOURNAL_INVALID");
            var a=Parse<Entry>(r.Before);var b=Parse<Entry>(r.After);
            if(a.ArchiveId!=r.ArchiveId||b.ArchiveId!=r.ArchiveId||a.SteamId!=r.Donor||b.SteamId!=r.Recipient||a.Status!="STORED"||b.Status!="STORED"||a.SpawnedId!=0||b.SpawnedId!=0||b.CarrierId!=0||b.PersonalSlot!=r.Slot||b.Slot!=r.Slot||r.Slot<1||r.Slot>15)throw new InvalidDataException("JOURNAL_INVALID");
            // Only these ownership fields may differ. Preserve ship metadata and original provenance verbatim in the journal.
            b.SteamId=a.SteamId;b.CarrierId=a.CarrierId;b.PersonalSlot=a.PersonalSlot;b.Slot=a.Slot;
            if(Hash(Bytes(a))!=Hash(Bytes(b)))throw new InvalidDataException("JOURNAL_INVALID");
        }
        GiftRecord Recover(GiftRecord r) {
            ValidateRecord(r);var a=Parse<Entry>(r.Before);var b=Parse<Entry>(r.After);string source=store.DirectoryFor(a),dest=store.DirectoryFor(b);Safe(source);Safe(dest);
            bool src=Directory.Exists(source),dst=Directory.Exists(dest);
            if(src&&!dst) {
                if(Disk.Hash(Manifest(source))!=Hash(r.Before))throw new InvalidDataException("SOURCE_CHANGED");VerifySnapshot(source,r.Files);
                r.State="REJECTED";r.Reason="INTERRUPTED_BEFORE_MOVE";Save(r);return r;
            }
            if(!src&&dst) {
                var hash=Disk.Hash(Manifest(dest));if(hash!=Hash(r.Before)&&hash!=Hash(r.After))throw new InvalidDataException("DESTINATION_CHANGED");VerifySnapshot(dest,r.Files);
                // Hide the in-progress manifest while checking native quota/slot collisions. Do not use List here:
                // its address validation intentionally rejects the old-owner manifest during recovery.
                var occupants=AllEntries(dest).Where(e=>e.SteamId==r.Recipient).ToArray();
                if(occupants.Length>=store.Capacity||r.Slot>store.Capacity||occupants.Any(e=>e.PersonalSlot==r.Slot||e.PersonalSlot==0&&e.Slot==r.Slot||e.ArchiveId==r.ArchiveId||e.ShipId==b.ShipId))throw new InvalidDataException("DESTINATION_CONFLICT");
                if(hash!=Hash(r.After))AtomicBytes(Manifest(dest),r.After);
                r.State="COMMITTED";r.Reason="OWNERSHIP_TRANSFERRED";Save(r);return r;
            }
            throw new InvalidDataException("ARCHIVE_LOCATION_CONFLICT");
        }
        IEnumerable<Entry> AllEntries(string ignore=null) {
            var parents=new List<string>();string players=Path.Combine(store.Root,"Players");Safe(players);
            if(Directory.Exists(players))parents.AddRange(Directory.GetDirectories(players));
            parents.AddRange(Directory.GetDirectories(store.Root).Where(p=>Regex.IsMatch(Path.GetFileName(p),"\\A[0-9]+\\z")));
            int count=0;
            foreach(var parent in parents){Safe(parent);foreach(var dir in Directory.GetDirectories(parent)) {
                Safe(dir);if(dir==ignore)continue;if(++count>10000)throw new InvalidDataException("HANGAR_LIMIT");
                var e=Parse<Entry>(File.ReadAllBytes(Manifest(dir)));
                if(store.DirectoryFor(e)!=dir||e.Slot<1||e.Slot>15||e.PersonalSlot<0||e.PersonalSlot>15||e.CargoVerified||e.AutoRecoveryAllowed)throw new InvalidDataException("MANIFEST_INVALID");yield return e;
            }}
        }
        public void RecoverAll() { lock(sync)try {
            Safe(journal);foreach(var p in Directory.GetFiles(journal,"*.xml")){Safe(p);var r=Parse<GiftRecord>(File.ReadAllBytes(p));if(RecordPath(r.Id)!=p)throw new InvalidDataException("JOURNAL_ADDRESS_INVALID");ValidateRecord(r);if(r.State=="PREPARED")Recover(r);else if(r.State!="COMMITTED"&&r.State!="REJECTED")throw new InvalidDataException("RECOVERY_REQUIRED");}
        }catch{Blocked=true;throw;} }
        public GiftRecord Apply(GiftIntent i) { lock(sync) {
            if(Blocked)throw new InvalidDataException("RECOVERY_REQUIRED");
            if(i==null||!Hex(i.Id,32)||!Hex(i.RequestHash,64)||!Steam(i.Donor)||!Steam(i.Recipient)||i.Donor==i.Recipient||!Hex(i.ArchiveId,32)||!Hex(i.ManifestHash,64))throw new InvalidDataException("REQUEST_INVALID");
            string recordPath=RecordPath(i.Id);
            if(File.Exists(recordPath)) {
                var old=Parse<GiftRecord>(File.ReadAllBytes(recordPath));ValidateRecord(old);if(old.RequestHash!=i.RequestHash)throw new InvalidDataException("REQUEST_ID_REUSED");
                if(old.State=="PREPARED")try{return Recover(old);}catch{Blocked=true;throw;}
                if(old.State!="COMMITTED"&&old.State!="REJECTED")throw new InvalidDataException("RECOVERY_REQUIRED");return old;
            }
            if(i.Expires<Now()||i.Expires>Now()+125000)throw new InvalidDataException("REQUEST_EXPIRED");
            var all=AllEntries().ToArray();var candidates=all.Where(e=>e.ArchiveId==i.ArchiveId).ToArray();
            if(candidates.Length!=1)throw new InvalidDataException("ARCHIVE_AMBIGUOUS");var entry=candidates[0];
            if(entry.SteamId!=i.Donor)throw new InvalidDataException("NOT_OWNER");
            if(entry.Status!="STORED"||entry.SpawnedId!=0||entry.StoreStage!="REMOVING")throw new InvalidDataException("ARCHIVE_NOT_STORED");
            if(all.Count(e=>e.ShipId==entry.ShipId)!=1)throw new InvalidDataException("SHIP_AMBIGUOUS");
            string source=store.DirectoryFor(entry);byte[] before=File.ReadAllBytes(Manifest(source));if(Hash(before)!=i.ManifestHash)throw new InvalidDataException("MANIFEST_CHANGED");
            var files=Snapshot(source);store.Verify(entry);
            var recipient=store.List(i.Recipient);if(recipient.Count>=store.Capacity)throw new InvalidDataException("RECIPIENT_FULL");
            int slot=Enumerable.Range(1,store.Capacity).First(s=>recipient.All(e=>e.PersonalSlot!=s));
            var changed=Parse<Entry>(before);changed.SteamId=i.Recipient;changed.CarrierId=0;changed.Slot=slot;changed.PersonalSlot=slot;byte[] after=Bytes(changed);string dest=store.DirectoryFor(changed);Safe(dest);
            if(Directory.Exists(dest)||File.Exists(dest))throw new InvalidDataException("DESTINATION_EXISTS");
            var r=new GiftRecord{Id=i.Id,RequestHash=i.RequestHash,Donor=i.Donor,Recipient=i.Recipient,ArchiveId=i.ArchiveId,Slot=slot,Before=before,After=after,Files=files,State="PREPARED",Reason="TRANSFER_PREPARED"};
            ValidateRecord(r);Save(r);Fault("PREPARED");
            try {
                if(Disk.Hash(Manifest(source))!=Hash(before))throw new InvalidDataException("MANIFEST_CHANGED");VerifySnapshot(source,files);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));Directory.Move(source,dest);Fault("MOVED");
                AtomicBytes(Manifest(dest),after);Fault("MANIFEST_WRITTEN");
                VerifySnapshot(dest,files);r.State="COMMITTED";r.Reason="OWNERSHIP_TRANSFERRED";Save(r);Fault("COMMITTED");return r;
            }catch {try{return Recover(r);}catch{Blocked=true;throw;}}
        } }
    }
    public sealed class QuantumWebGiftSettings { public bool Enabled;public string Mode,BoundModPath,Secret,InstallationId; }
    public sealed class GiftEnvelope { public string Payload,Signature; }
    public sealed class GiftMailbox {
        public const string Contract="QU-QH-GIFT-1";
        readonly string root;readonly QuantumWebGiftSettings pair;readonly GiftEngine engine;readonly ArchiveStore store;
        readonly Func<string,string,bool> acquire;readonly Action<string,string> release;readonly Action<string> log;
        public GiftMailbox(string modRoot,ArchiveStore archive,Func<string,string,bool> acquireOwners,Action<string,string> releaseOwners,Action<string> logger) {
            root=Path.Combine(modRoot,"QuantumWebGift");store=archive;acquire=acquireOwners;release=releaseOwners;log=logger;
            var cfg=Path.Combine(modRoot,"QuantumWebGift.xml");GiftEngine.Safe(cfg);pair=GiftEngine.Parse<QuantumWebGiftSettings>(File.ReadAllBytes(cfg));
            if(!pair.Enabled||pair.Mode!="TEST"||Path.GetFullPath(pair.BoundModPath)!=Path.GetFullPath(modRoot)||!GiftEngine.Hex(pair.Secret,64)||!GiftEngine.Hex(pair.InstallationId,32))throw new InvalidDataException("PAIRING_INVALID");
            GiftEngine.Safe(root);Directory.CreateDirectory(root);foreach(var n in new[]{"requests","responses","journal"}){GiftEngine.Safe(Path.Combine(root,n));Directory.CreateDirectory(Path.Combine(root,n));}
            engine=new GiftEngine(archive,Path.Combine(root,"journal"));engine.RecoverAll();
        }
        string Sign(string payload) { using(var h=new HMACSHA256(Encoding.UTF8.GetBytes(pair.Secret)))return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","").ToLowerInvariant(); }
        public void Write(string file,string[] fields) {
            var payload=string.Join("\n",new[]{Contract,"TEST",pair.InstallationId}.Concat(fields));GiftEngine.Safe(file);
            Disk.Write(file,new GiftEnvelope{Payload=Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)),Signature=Sign(payload)});
        }
        public void Pump() {
            Write(Path.Combine(root,"status.xml"),new[]{"STATUS",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),store.Capacity.ToString(CultureInfo.InvariantCulture),engine.Blocked?"BLOCKED":"READY"});
            if(engine.Blocked)throw new InvalidDataException("RECOVERY_REQUIRED");
            foreach(var file in Directory.GetFiles(Path.Combine(root,"requests"),"*.xml").OrderBy(x=>x,StringComparer.Ordinal).Take(16)) {
                GiftIntent intent=null;GiftRecord completed=null;bool locked=false;
                try {
                    GiftEngine.Safe(file);if(new FileInfo(file).Length>16384)throw new InvalidDataException("ENVELOPE_TOO_BIG");
                    var env=GiftEngine.Parse<GiftEnvelope>(File.ReadAllBytes(file));string raw=Encoding.UTF8.GetString(Convert.FromBase64String(env.Payload));
                    string signature=Sign(raw);if(!GiftEngine.Hex(env.Signature,64)||!signature.Zip(env.Signature,(a,b)=>a^b).Aggregate(0,(a,b)=>a|b).Equals(0))throw new InvalidDataException("SIGNATURE_INVALID");
                    var f=raw.Split('\n');long expires;if(f.Length!=10||f[0]!=Contract||f[1]!="TEST"||f[2]!=pair.InstallationId||f[3]!="GIFT"||!long.TryParse(f[9],NumberStyles.None,CultureInfo.InvariantCulture,out expires)||!GiftEngine.Hex(f[4],32)||Path.GetFileName(file)!=f[4]+".xml")throw new InvalidDataException("REQUEST_INVALID");
                    intent=new GiftIntent{Id=f[4],RequestHash=GiftEngine.Hash(Encoding.UTF8.GetBytes(raw)),Donor=f[5],Recipient=f[6],ArchiveId=f[7],ManifestHash=f[8],Expires=expires};
                    if(!GiftEngine.Steam(intent.Donor)||!GiftEngine.Steam(intent.Recipient)||intent.Donor==intent.Recipient)throw new InvalidDataException("REQUEST_INVALID");
                    string receipt=Path.Combine(root,"responses",intent.Id+".xml");GiftEngine.Safe(receipt);
                    if(File.Exists(receipt)) {
                        var previous=GiftEngine.Parse<GiftEnvelope>(File.ReadAllBytes(receipt));var text=Encoding.UTF8.GetString(Convert.FromBase64String(previous.Payload));var parts=text.Split('\n');
                        if(previous.Signature!=Sign(text)||parts.Length!=11||parts[0]!=Contract||parts[1]!="TEST"||parts[2]!=pair.InstallationId||parts[3]!="RESULT"||parts[4]!=intent.Id)throw new InvalidDataException("RECEIPT_INVALID");
                        if(parts[5]!=intent.RequestHash)throw new InvalidDataException("REQUEST_ID_REUSED");File.Delete(file);continue;
                    }
                    if(!acquire(intent.Donor,intent.Recipient))continue;locked=true;
                    completed=engine.Apply(intent);Reply(intent,completed.State,completed.Reason,completed.Slot);File.Delete(file);
                }catch(Exception e){
                    log("WEB_GIFT "+(intent==null?"INVALID_ENVELOPE":intent.Id)+" "+e.Message);
                    if(engine.Blocked)throw;
                    if(completed!=null)continue; // Response write failed after a durable outcome: retry the same journal.
                    if(e.Message=="REQUEST_ID_REUSED"||e.Message=="RECEIPT_INVALID"||intent==null||!locked) {
                        File.Move(file,file+"."+Guid.NewGuid().ToString("N")+".invalid");continue;
                    }
                    Reply(intent,"REJECTED",Known(e.Message),0);File.Delete(file);
                }finally{if(locked)release(intent.Donor,intent.Recipient);}
            }
        }
        static string Known(string e){var reasons=new[]{"NOT_OWNER","RECIPIENT_FULL","ARCHIVE_NOT_STORED","ARCHIVE_AMBIGUOUS","SHIP_AMBIGUOUS","MANIFEST_CHANGED","REQUEST_EXPIRED","REQUEST_ID_REUSED","DESTINATION_EXISTS","ARCHIVE_CHANGED","LINK_REFUSED","REQUEST_INVALID"};return reasons.Contains(e)?e:"NATIVE_VALIDATION_FAILED";}
        void Reply(GiftIntent i,string state,string reason,int slot) { Write(Path.Combine(root,"responses",i.Id+".xml"),new[]{"RESULT",i.Id,i.RequestHash,state,reason,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),i.Recipient,slot.ToString(CultureInfo.InvariantCulture)}); }
    }
}
