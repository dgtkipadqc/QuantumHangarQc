using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Eleon.Modding;
using QuantumHangarQc.Localization;

[assembly: AssemblyVersion("0.2.6.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2")]
[assembly: AssemblyTitle("Quantum Hangar Qc — BETA")]
[assembly: AssemblyCopyright("Copyright 2026 Le Spot Studio")]
namespace QuantumHangarQc
{
    public sealed class Settings { public string SaveGameName = ""; public string DefaultLanguage = "en"; }
    public sealed class FileDigest { public string Path; public long Bytes; public string Sha256; }
    public sealed class Entry {
        // CarrierId/Slot retain the legacy on-disk address; PersonalSlot is the new player-facing number.
        public int Slot, CarrierId, ShipId, SpawnedId, PersonalSlot;
        public string DestinationPlayfield; public PVector3 DestinationPos;
        public string ArchiveId, SteamId, Status, Utc, Detail;
        public string StoreStage;
        public GlobalStructureInfo Ship;
        public List<FileDigest> Files = new List<FileDigest>();
        public bool CargoVerified = false;
        public bool AutoRecoveryAllowed = false;
    }
    public static class Disk {
        public static T Read<T>(string path) {
            using(var r=XmlReader.Create(path,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null }))
                return (T)new XmlSerializer(typeof(T)).Deserialize(r);
        }
        public static void Write<T>(string path,T value) {
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try {
                using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    new XmlSerializer(typeof(T)).Serialize(f,value); f.Flush(true);
                }
                if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
            } finally { if(File.Exists(temp)) File.Delete(temp); }
        }
        public static string HashShared(string path) {
            using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) using(var h=SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();
        }
        public static void CopyShared(string source,string destination) {
            using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            using(var output=new FileStream(destination,FileMode.Create,FileAccess.Write,FileShare.None)) {
                input.CopyTo(output);output.Flush(true);
            }
        }
        public static string Hash(string path) {
            using(var f=File.OpenRead(path)) using(var h=SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(f)).Replace("-","").ToLowerInvariant();
        }
    }
    public sealed class ArchiveStore {
        public const int Capacity=10;
        public readonly string Root;
        public ArchiveStore(string root) { Root=root; Directory.CreateDirectory(root); }
        static string Steam(string id) {
            ulong n;
            if(string.IsNullOrEmpty(id)||!ulong.TryParse(id,NumberStyles.None,CultureInfo.InvariantCulture,out n)||n==0||n.ToString(CultureInfo.InvariantCulture)!=id)
                throw Texts.Tag(new InvalidDataException("Identite du hangar invalide"),"error.identity");
            return id;
        }
        string Personal(string steam) { return Path.Combine(Root,"Players",Steam(steam)); }
        public string DirectoryFor(Entry e) {
            Guid guid;
            if(!Guid.TryParseExact(e.ArchiveId,"N",out guid)||e.CarrierId<0) throw Texts.Tag(new InvalidDataException("ID archive invalide"),"error.archive");
            Steam(e.SteamId);
            return e.CarrierId>0
                ? Path.Combine(Root,e.CarrierId.ToString(CultureInfo.InvariantCulture),e.ArchiveId)
                : Path.Combine(Personal(e.SteamId),e.ArchiveId);
        }
        void AddEntries(string dir,string steam,List<Entry> list,bool legacy) {
            if(!Directory.Exists(dir)) return;
            foreach(var d in Directory.GetDirectories(dir).OrderBy(x=>x,StringComparer.Ordinal)) {
                var e=Disk.Read<Entry>(Path.Combine(d,"Manifest.xml"));
                if(e.Slot<1||e.Slot>15||DirectoryFor(e)!=d||e.CargoVerified||e.AutoRecoveryAllowed||legacy!=(e.CarrierId>0))
                    throw Texts.Tag(new InvalidDataException("Manifest non compatible; conserver le dossier pour analyse"),"error.archive");
                if(e.SteamId==steam) list.Add(e);
            }
        }
        public List<Entry> List(string steam) {
            Steam(steam);var list=new List<Entry>();
            AddEntries(Personal(steam),steam,list,false);
            // Read old archives in place: never clone or re-enable a legacy transaction.
            foreach(var dir in Directory.GetDirectories(Root).OrderBy(x=>x,StringComparer.Ordinal)) {
                int carrier;
                if(int.TryParse(Path.GetFileName(dir),NumberStyles.None,CultureInfo.InvariantCulture,out carrier)&&carrier>0) AddEntries(dir,steam,list,true);
            }
            if(list.Count>15) throw Texts.Tag(new InvalidDataException("Plus de 15 anciennes archives pour ce compte : migration manuelle requise; aucune archive modifiee"),"error.archive");
            if(list.Select(e=>e.ArchiveId).Distinct().Count()!=list.Count) throw Texts.Tag(new InvalidDataException("Archive dupliquee; restitution bloquee"),"error.archive");
            var assigned=list.Where(e=>e.PersonalSlot!=0).ToList();
            if(assigned.Any(e=>e.PersonalSlot<1||e.PersonalSlot>15)||assigned.Select(e=>e.PersonalSlot).Distinct().Count()!=assigned.Count)
                throw Texts.Tag(new InvalidDataException("Numeros de places incoherents"),"error.archive");
            var used=new HashSet<int>(assigned.Select(e=>e.PersonalSlot));
            foreach(var e in list.Where(e=>e.PersonalSlot==0).OrderBy(e=>e.Utc,StringComparer.Ordinal).ThenBy(e=>e.ArchiveId,StringComparer.Ordinal)) {
                e.PersonalSlot=!used.Contains(e.Slot)?e.Slot:Enumerable.Range(1,15).First(x=>!used.Contains(x));
                Save(e);used.Add(e.PersonalSlot);
            }
            return list.OrderBy(e=>e.PersonalSlot).ToList();
        }
        public Entry Reserve(GlobalStructureInfo ship,string steam,int requestedSlot=0) {
            if(requestedSlot<0||requestedSlot>Capacity) throw Texts.Tag(new InvalidOperationException("Choisis une place de 1 a 10"),"error.slot_range");
            var entries=List(steam);
            if(entries.Count>=ArchiveStore.Capacity) throw Texts.Tag(new InvalidOperationException("Hangar plein : 10 places maximum"),"error.hangar_full");
            if(entries.Any(existing=>existing.ShipId==ship.id)) throw Texts.Tag(new InvalidOperationException("Ce vaisseau possede deja une transaction dans ton hangar"),"error.duplicate");
            if(requestedSlot>0&&entries.Any(existing=>existing.PersonalSlot==requestedSlot)) throw Texts.Tag(new InvalidOperationException("Place "+requestedSlot+" occupee : aucun remplacement autorise"),"error.slot_occupied");
            int slot=requestedSlot>0?requestedSlot:Enumerable.Range(1,Capacity).First(x=>!entries.Any(existing=>existing.PersonalSlot==x));
            var entry=new Entry { CarrierId=0,ShipId=ship.id,Ship=ship,SteamId=Steam(steam),ArchiveId=Guid.NewGuid().ToString("N"),
                Slot=slot,PersonalSlot=slot,Status="RESERVED",Utc=DateTime.UtcNow.ToString("o"),Detail="Transaction reservee avant export. Ne pas restituer tant que le depot n est pas confirme." };
            Directory.CreateDirectory(DirectoryFor(entry)); Save(entry);return entry;
        }
        public void Save(Entry e) { Disk.Write(Path.Combine(DirectoryFor(e),"Manifest.xml"),e); }
        public Entry At(int slot,string steam) {
            if(slot<1||slot>15) throw Texts.Tag(new InvalidOperationException("Choisis une place de 1 a 15"),"error.legacy_slot_range");
            var e=List(steam).SingleOrDefault(x=>x.PersonalSlot==slot);
            if(e==null) throw Texts.Tag(new InvalidOperationException("Place "+slot+" libre"),"error.slot_empty");
            return e;
        }
        public void Verify(Entry e) {
            if((e.Status!="STORED" && e.Status!="EXPORTED_UNVERIFIED") || e.Files.Count==0) throw Texts.Tag(new InvalidOperationException("Export incomplet : "+e.Status),"error.archive");
            VerifyFiles(e);
        }
        void VerifyFiles(Entry e) {
            string dir=DirectoryFor(e);
            if(e.Files.Count(f=>f.Path=="Export.dat" && f.Bytes>0)!=1 || !e.Files.Any(f=>f.Path.StartsWith("Shared"+Path.DirectorySeparatorChar,StringComparison.Ordinal))) throw Texts.Tag(new InvalidDataException("Archive native incomplete"),"error.archive");
            foreach(var f in e.Files) {
                if(string.IsNullOrEmpty(f.Path) || System.IO.Path.IsPathRooted(f.Path) || f.Path.Split('/', '\\').Any(part=>part=="..")) throw Texts.Tag(new InvalidDataException("Chemin archive invalide"),"error.archive");
                string p=Path.Combine(dir,f.Path);
                if(!File.Exists(p) || new FileInfo(p).Length!=f.Bytes || Disk.Hash(p)!=f.Sha256)
                    throw Texts.Tag(new InvalidDataException("Archive absente ou modifiee : "+f.Path),"error.archive");
            }
        }
        static bool RestoredInto(Entry old,Entry stored) {
            DateTime before,after;
            if(old.Status!="QUARANTINE" || old.SpawnedId<=0 ||
                stored.Status!="STORED" || stored.StoreStage!="REMOVING" || stored.SpawnedId!=0 ||
                old.ArchiveId==stored.ArchiveId || old.SteamId!=stored.SteamId || old.SpawnedId!=stored.ShipId ||
                old.Ship.id!=old.ShipId || stored.Ship.id!=stored.ShipId ||
                old.Ship.type!=stored.Ship.type || old.Ship.cntBlocks!=stored.Ship.cntBlocks ||
                old.Ship.cntDevices!=stored.Ship.cntDevices || old.Ship.factionGroup!=stored.Ship.factionGroup ||
                old.Ship.factionId!=stored.Ship.factionId ||
                !DateTime.TryParse(old.Utc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out before) ||
                !DateTime.TryParse(stored.Utc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out after) || after<=before)
                return false;
            var a=old.Files.Where(f=>f.Path!=null && f.Path.Replace('\\','/')=="Shared/backup.epb").ToList();
            var b=stored.Files.Where(f=>f.Path!=null && f.Path.Replace('\\','/')=="Shared/backup.epb").ToList();
            return a.Count==1 && b.Count==1 && a[0].Bytes>0 && a[0].Bytes==b[0].Bytes &&
                !string.IsNullOrEmpty(a[0].Sha256) && a[0].Sha256==b[0].Sha256;
        }
        // Called under the player's operation lock. Preserve every recovery file in Retired.
        // An equal name is never proof: require the exact spawned entity and a verified later deposit.
        public int RetireRestoredQuarantines(string steam,Action<string> log) {
            var entries=List(steam);int count=0;
            foreach(var old in entries.Where(e=>e.Status=="QUARANTINE" && e.SpawnedId>0)) {
                try {
                    var matches=entries.Where(e=>RestoredInto(old,e)).ToList();
                    if(matches.Count!=1) continue;
                    var stored=matches[0];
                    VerifyFiles(old);Verify(stored);
                    Forget(old);count++;
                    log("RESTORED_QUARANTINE_RETIRED archive="+old.ArchiveId+" slot="+old.PersonalSlot+
                        " spawnedId="+old.SpawnedId+" storedArchive="+stored.ArchiveId+" storedSlot="+stored.PersonalSlot+
                        " recovery="+Path.Combine(Root,"Retired",old.ArchiveId));
                } catch(Exception ex) {
                    log("RESTORED_QUARANTINE_RETAINED archive="+old.ArchiveId+" reason="+ex.Message);
                }
            }
            return count;
        }
        public void Forget(Entry e) {
            // Retire only this mod's completed transaction archive; never Saves/Shared or an in-world entity.
            string source=DirectoryFor(e), trash=Path.Combine(Root,"Retired");
            Directory.CreateDirectory(trash);
            Directory.Move(source,Path.Combine(trash,e.ArchiveId));
        }
    }
    public sealed class HangarMod : ModInterface {
        sealed class Pending {
            public bool SendOnly; public CmdId Expected; public int Player; public object Data; public CmdId Command;
            public TaskCompletionSource<object> Result=new TaskCompletionSource<object>();
        }
        sealed class Placement { public string Playfield, SteamId; public bool GpsStopped, GpsArmed; public int ClientId,PlayerId; public string MarkerName; public PVector3 Position; public DateTime Created; }
        readonly Dictionary<string,Placement> placements=new Dictionary<string,Placement>();
        readonly HashSet<string> owners=new HashSet<string>();
        readonly object gate=new object();
        readonly Dictionary<ushort,Pending> pending=new Dictionary<ushort,Pending>();
        readonly Queue<ushort> outbox=new Queue<ushort>();
        readonly HashSet<int> busy=new HashSet<int>(), entities=new HashSet<int>();
        readonly Dictionary<int,DateTime> lastCommand=new Dictionary<int,DateTime>();
        ModGameAPI api; ArchiveStore store; string saveDir, logDir;
        Preferences preferences; string defaultLanguage="en";
        string Language(PlayerInfo p) { return preferences.Resolve(p.steamId,defaultLanguage).Code; }
        string T(PlayerInfo p,string key,params object[] values) { return Texts.Render(Language(p),key,values); }
        readonly SemaphoreSlim gpsGate=new SemaphoreSlim(1,1);
        internal Task GpsTask;
        internal Func<DateTime> GpsNow=()=>DateTime.UtcNow;
        readonly Dictionary<ushort,DateTime> gpsReuseAfter=new Dictionary<ushort,DateTime>();
        ushort gpsSequence=1000;
        readonly SemaphoreSlim structureGate=new SemaphoreSlim(1,1);
        bool hostStructureResponses;
        ushort sequence=43000; bool stopped; FileStream instanceLock;
        internal int RequestTimeout=20000, DialogTimeout=60000, ExportPoll=1000, StateTimeout=8000, StatePoll=100;
        internal void StartAt(ModGameAPI a,string save) {
            api=a; saveDir=Path.GetFullPath(save);
            if(!Directory.Exists(saveDir)) throw Texts.Tag(new DirectoryNotFoundException("Sauvegarde introuvable : "+saveDir),"error.unexpected");
            logDir=Path.Combine(saveDir,"Mods","QuantumHangarQc"); Directory.CreateDirectory(logDir);
            instanceLock=new FileStream(Path.Combine(logDir,"Instance.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            store=new ArchiveStore(Path.Combine(logDir,"Archives"));
            preferences=new Preferences(Path.Combine(logDir,"Preferences"),Log);
            Texts.Initialize(Path.Combine(Path.GetDirectoryName(typeof(HangarMod).Assembly.Location),"Languages"),Log);
            Log("START 0.2.6 BETA PERSONAL 10 SLOTS; store/destroy/restore enabled; save="+saveDir);
        }
        public void Game_Start(ModGameAPI gameApi) {
            api=gameApi;
            try {
                string dir=Path.GetDirectoryName(typeof(HangarMod).Assembly.Location);
                string configPath=Path.Combine(dir,"Configuration.xml");
                var cfg=File.Exists(configPath)?Disk.Read<Settings>(configPath):new Settings();
                defaultLanguage=cfg.DefaultLanguage;
                string save=cfg.SaveGameName;
                if(string.IsNullOrWhiteSpace(save)) {
                    string cb=Path.GetFullPath(Path.Combine(dir,"..","AtlantisPlayerCommands","Configuration.xml"));
                    var xml=new XmlDocument { XmlResolver=null };
                    using(var r=XmlReader.Create(cb,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null })) xml.Load(r);
                    save=xml.SelectSingleNode("/Configuration/SaveGameName").InnerText;
                }
                if(string.IsNullOrWhiteSpace(save) || save!=save.Trim() || save=="." || save==".." || save.IndexOfAny(new[]{'/', '\\', ':'})>=0)
                    throw Texts.Tag(new InvalidDataException("SaveGameName invalide"),"error.unexpected");
                var root=new DirectoryInfo(dir); for(int i=0;i<5;i++) root=root.Parent;
                StartAt(gameApi,Path.Combine(root.FullName,"Saves","Games",save));
            } catch(Exception ex) { stopped=true; if(instanceLock!=null) instanceLock.Dispose(); Log("DISABLED "+ex.Message); }
        }
        public void Game_Update() {
            ScheduleGps();
            for(int i=0;i<32;i++) {
                Pending p; ushort seq;
                lock(gate) {
                    if(stopped || outbox.Count==0) return;
                    seq=outbox.Dequeue(); if(!pending.TryGetValue(seq,out p)) continue;
                }
                try {
                    Log("API SEND seq="+seq+" command="+p.Command);
                    if(!api.Game_Request(p.Command,seq,p.Data)) Fail(seq,Texts.Tag(new InvalidOperationException("Requete refusee : "+p.Command),"error.unexpected"));
                    else if(p.SendOnly) {
                        lock(gate) pending.Remove(seq);
                        p.Result.TrySetResult(null);
                    }
                }
                catch(Exception ex) { Fail(seq,ex); }
            }
        }
        void Fail(ushort seq,Exception ex) {
            Pending p; lock(gate) { if(!pending.TryGetValue(seq,out p)) return; pending.Remove(seq); }
            p.Result.TrySetException(ex);
        }
        public void Game_Exit() {
            Pending[] jobs;
            lock(gate) { stopped=true; jobs=pending.Values.ToArray(); pending.Clear(); outbox.Clear(); }
            foreach(var p in jobs) p.Result.TrySetCanceled();
            // Export worker checks stopped before finishing; process lock is released on process exit.
            Log("STOP"); lock(gate) { if(busy.Count==0 && instanceLock!=null) { instanceLock.Dispose(); instanceLock=null; } }
        }
        public void Game_Event(CmdId kind,ushort seq,object data) {
            if(stopped) return;
            if(kind==CmdId.Event_ChatMessage && data is ChatInfo) {
                var c=(ChatInfo)data; if(c.playerId<=0 || c.msg==null) return;
                string input=c.msg.Trim();
                if(!input.Equals("qh",StringComparison.OrdinalIgnoreCase)&&!input.StartsWith("qh:",StringComparison.OrdinalIgnoreCase)) return;
                string[] args; if(!TryParseCommand(input,out args)) args=new[]{"qh:invalid"};
                if(args[0]=="qh:lang") { RunLanguage(c.playerId,args);return; }
                if(c.msg.Length>120 || c.msg.IndexOfAny(new[]{'\n','\r','\t'})>=0) return;
                lock(gate) {
                    DateTime last;
                    if(busy.Contains(c.playerId) || languageBusy.Contains(c.playerId) || (lastCommand.TryGetValue(c.playerId,out last) && DateTime.UtcNow-last<TimeSpan.FromSeconds(1))) return;
                    busy.Add(c.playerId); lastCommand[c.playerId]=DateTime.UtcNow;
                }
                Run(c.playerId,args); return;
            }
            Pending p;
            lock(gate) {
                if(!pending.TryGetValue(seq,out p)) return;
                if(kind==CmdId.Event_Player_Info && (!(data is PlayerInfo)||((PlayerInfo)data).entityId!=p.Player)) return;
                if(kind==CmdId.Event_DialogButtonIndex && (!(data is IdAndIntValue)||((IdAndIntValue)data).Id!=p.Player)) return;
                // ModHost's DB bridge echoes Request_GlobalStructure_List, not Event_GlobalStructure_List.
                // Match ONLY this read response, with the exact outstanding sequence and payload type.
                bool hostList=p.Command==CmdId.Request_GlobalStructure_List && kind==CmdId.Request_GlobalStructure_List && data is GlobalStructureList;
                bool updatedList=p.Command==CmdId.Request_GlobalStructure_Update &&
                    kind==CmdId.Event_GlobalStructure_List && data is GlobalStructureList &&
                    ((GlobalStructureList)data).globalStructures!=null;
                bool updateReply=p.Command==CmdId.Request_GlobalStructure_Update &&
                    (updatedList || kind==CmdId.Event_Ok || (kind==CmdId.Request_GlobalStructure_Update && data is bool && (bool)data));
                if(kind!=p.Expected && kind!=CmdId.Event_Error && !hostList && !updateReply) {
                    Log("API IGNORED seq="+seq+" command="+p.Command+" received="+kind+" type="+(data==null?"null":data.GetType().Name));
                    return;
                }
                if(hostList) hostStructureResponses=true;
                if(p.Command==CmdId.Request_GlobalStructure_List && kind!=CmdId.Event_Error && !(data is GlobalStructureList)) return;
                if(p.Command==CmdId.Request_GlobalStructure_Update && kind!=CmdId.Event_Error && !updateReply) return;
                Log("API RECEIVE seq="+seq+" command="+p.Command+" received="+kind);
                pending.Remove(seq);
            }
            if(kind==CmdId.Event_Error) p.Result.TrySetException(Texts.Tag(new InvalidOperationException("API "+p.Command+" : "+(data is ErrorInfo?((ErrorInfo)data).errorType.ToString():"erreur")),"error.unexpected"));
            else p.Result.TrySetResult(data);
        }
        async Task<object> Request(CmdId command,object data,CmdId expected,int player,int timeout=0,bool sendOnly=false,bool gps=false) {
            var p=new Pending { Command=command,Data=data,Expected=expected,Player=player,SendOnly=sendOnly }; ushort seq;
            lock(gate) {
                if(stopped) throw Texts.Tag(new InvalidOperationException("Module arrete"),"error.stopped");
                // Never reuse sequence IDs in this process: late dialog responses cannot confirm a later operation.
                if(gps) {
                    int tries=0;
                    do {gpsSequence=(ushort)(gpsSequence>=40000?1000:gpsSequence+1);if(++tries>39001)throw Texts.Tag(new InvalidOperationException("Trop de requetes GPS en attente"),"error.timeout");}
                    while(pending.ContainsKey(gpsSequence)||(gpsReuseAfter.ContainsKey(gpsSequence)&&gpsReuseAfter[gpsSequence]>DateTime.UtcNow));
                    seq=gpsSequence;
                } else {
                if(sequence>=58000) throw Texts.Tag(new InvalidOperationException("Limite de requetes atteinte : redemarrer le module"),"error.timeout");
                seq=++sequence;} pending.Add(seq,p); outbox.Enqueue(seq);
            }
            try {
                if(await Task.WhenAny(p.Result.Task,Task.Delay(timeout==0?RequestTimeout:timeout))!=p.Result.Task)
                    throw Texts.Tag(new TimeoutException("Delai depasse : "+command+" (seq="+seq+"). Aucune relance automatique."),"error.timeout");
                return await p.Result.Task;
            } finally { lock(gate) {pending.Remove(seq);if(gps)gpsReuseAfter[seq]=DateTime.UtcNow.AddMinutes(2);} }
        }
        async Task<PlayerInfo> Player(int id,bool gps=false) {
            var p=(PlayerInfo)await Request(CmdId.Request_Player_Info,new Id(id),CmdId.Event_Player_Info,id,gps:gps);
            ulong steam;
            if(p.entityId!=id || p.clientId<0 || !ulong.TryParse(p.steamId,out steam) || steam==0 || p.steamId!=steam.ToString(CultureInfo.InvariantCulture)) throw Texts.Tag(new InvalidOperationException("Identite joueur invalide"),"error.identity");
            return p;
        }
        async Task<GlobalStructureList> Structures(int player) {
            await structureGate.WaitAsync();
            try {
                bool knownHost=hostStructureResponses;
                // Native Update can return the refreshed list itself. Use it directly;
                // a separate List request can return an older ModHost cache.
                var list=knownHost?await RefreshStructures(player):null;
                if(list==null) list=await ReadStructures(player);
                if(!knownHost && hostStructureResponses) {
                    list=await RefreshStructures(player);
                    if(list==null) list=await ReadStructures(player);
                }
                return list;
            } finally { structureGate.Release(); }
        }
        async Task<GlobalStructureList> RefreshStructures(int player) {
            // Observed on this server: PString playfield -> Event_GlobalStructure_List.
            // Also support Event_Ok and ModHost request/true acknowledgements.
            var current=await Player(player);
            if(string.IsNullOrWhiteSpace(current.playfield)) throw Texts.Tag(new InvalidDataException("Secteur joueur absent pour rafraichir les structures"),"error.playfield");
            var result=await Request(CmdId.Request_GlobalStructure_Update,new PString(current.playfield),CmdId.Event_Ok,player);
            var refreshed=result as GlobalStructureList;
            if(refreshed!=null) Log("STRUCTURES_REFRESHED count="+refreshed.globalStructures.Values.Sum(v=>v==null?0:v.Count));
            return refreshed;
        }
        async Task<GlobalStructureList> ReadStructures(int player) {
            var list=(GlobalStructureList)await Request(CmdId.Request_GlobalStructure_List,null,CmdId.Event_GlobalStructure_List,player);
            if(list==null||list.globalStructures==null) throw Texts.Tag(new InvalidDataException("Liste des structures absente"),"error.ship_absent");
            Log("STRUCTURES count="+list.globalStructures.Values.Sum(v=>v==null?0:v.Count));
            return list;
        }
        static GlobalStructureInfo Find(GlobalStructureList list,int id) {
            var matches=list.globalStructures.SelectMany(k=>k.Value??new List<GlobalStructureInfo>()).Where(x=>x.id==id).ToArray();
            if(matches.Length!=1) throw Texts.Tag(new InvalidOperationException("Structure absente ou ambigue : "+id),"error.ship_absent");
            return matches[0];
        }
        static string Playfield(GlobalStructureList l,int id) { return l.globalStructures.Single(k=>k.Value!=null&&k.Value.Any(s=>s.id==id)).Key; }
        static double Distance(PVector3 a,PVector3 b) {
            double dx=(double)a.x-b.x,dy=(double)a.y-b.y,dz=(double)a.z-b.z; return Math.Sqrt(dx*dx+dy*dy+dz*dz);
        }
        static string PositionText(PVector3 p) { return string.Format(CultureInfo.InvariantCulture,"({0:0.0}, {1:0.0}, {2:0.0})",p.x,p.y,p.z); }
        static bool Finite(PVector3 p) { return !float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z); }
        async Task<Live.StateReply> ReadLiveState(PlayerInfo player,int shipId) {
            var r=new Live.StateRequest{Token=Guid.NewGuid().ToString("N"),Player=player.entityId,Steam=player.steamId,Entity=shipId,Playfield=player.playfield,CreatedUtc=DateTime.UtcNow};
            string root=Path.Combine(logDir,"LiveState");Directory.CreateDirectory(root);
            string request=Path.Combine(root,r.Token+".request.xml"),reply=Path.Combine(root,r.Token+".reply.xml");
            try {
                Live.Transfer.Write(request,r);DateTime until=DateTime.UtcNow.AddMilliseconds(StateTimeout);
                while(DateTime.UtcNow<until) {
                    if(stopped)throw Texts.Tag(new InvalidOperationException("Module arrete pendant lecture de l etat"),"error.stopped");
                    if(File.Exists(reply)) {
                        var state=Live.Transfer.Read<Live.StateReply>(reply);
                        if(state.Token!=r.Token||state.Entity!=r.Entity||state.Player!=r.Player||state.Steam!=r.Steam||state.Playfield!=r.Playfield||state.CapturedUtc<r.CreatedUtc.AddSeconds(-1)||state.CapturedUtc>DateTime.UtcNow.AddSeconds(1)||(DateTime.UtcNow-state.CapturedUtc).TotalSeconds>5)
                            throw Texts.Tag(new InvalidDataException("Reponse QH_STATE incorrecte ou perimee; aucun depot"),"error.live_state");
                        if(!string.IsNullOrEmpty(state.Error))throw Texts.Tag(new InvalidOperationException("QH_STATE : "+state.Error),"error.live_state");
                        if(state.Docked==null||state.Occupants==null||state.OwnerGroup<0||state.OwnerGroup>255||state.Type<0||state.Type>255||state.BlockCount<0||state.DeviceCount<0)throw Texts.Tag(new InvalidDataException("Etat QH_STATE incomplet"),"error.live_state");
                        return state;
                    }
                    await Task.Delay(StatePoll);
                }
                throw Texts.Tag(new TimeoutException("Etat en direct indisponible. Installer aussi QuantumHangarQcState dans Content/Mods/ et redemarrer les PlayfieldServer. Aucun depot"),"error.live_state");
            } finally {
                try{if(File.Exists(request))File.Delete(request);if(File.Exists(reply))File.Delete(reply);}catch(Exception e){Log("STATE_CLEANUP "+e.Message);}
            }
        }
        async Task<PlayerInfo> LiveStorePosition(PlayerInfo expected,GlobalStructureList list,int shipId,string phase) {
            int player=expected.entityId,client=expected.clientId;string steam=expected.steamId;
            var ship=Find(list,shipId);string sector=Playfield(list,shipId);var cached=ship.pos;
            Log("RANGE_REQUEST phase="+phase+" player="+player+" ship="+shipId+" name="+Clean(ship.name)+" listSector="+sector+" playerSector="+expected.playfield+" listPos="+PositionText(cached)+" playerInfoPos="+PositionText(expected.pos));
            var raw=await Request(CmdId.Request_Entity_PosAndRot,new Id(shipId),CmdId.Event_Entity_PosAndRot,player);
            if(!(raw is IdPositionRotation))throw Texts.Tag(new InvalidDataException("Position directe du vaisseau absente; aucun depot"),"error.live_state");
            var live=(IdPositionRotation)raw;
            if(live.id!=shipId||!Finite(live.pos)||!Finite(live.rot))throw Texts.Tag(new InvalidDataException("Position directe du vaisseau invalide ou ID incorrect; aucun depot"),"error.live_state");
            var stateTask=ReadLiveState(expected,shipId);
            var playerTask=Player(player);await Task.WhenAll(stateTask,playerTask);
            var current=await playerTask;var state=await stateTask;
            if((DateTime.UtcNow-state.CapturedUtc).TotalSeconds>2)throw Texts.Tag(new InvalidDataException("Etat QH_STATE devenu trop ancien pendant la lecture joueur; relance le depot"),"error.live_state");
            if(current.playfield!=state.Playfield)throw Texts.Tag(new InvalidOperationException("Secteur change pendant la lecture QH_STATE"),"error.session");
            if(current.steamId!=steam||current.clientId!=client)throw Texts.Tag(new InvalidOperationException("Session changee pendant le controle de distance"),"error.session");
            if(!Finite(current.pos))throw Texts.Tag(new InvalidDataException("Position joueur invalide; aucun depot"),"error.live_state");
            Log("STATE_RESULT phase="+phase+" ship="+shipId+" cachedPower="+ship.powered+" livePower="+state.Powered+" cachedPilot="+ship.pilotId+" livePilot="+state.Pilot+" dockedTo="+state.DockedTo+" docked="+state.Docked.Count+" occupants="+string.Join(",",state.Occupants)+" ready="+state.Ready+" owner="+state.OwnerGroup+":"+state.OwnerId+" observer="+state.ObserverVersion+" playersSeen="+state.PlayersSeen+" requesterSeen="+state.RequesterSeen+" identitySource=Request_Player_Info");
            ship.powered=state.Powered;ship.pilotId=state.Pilot;ship.dockedShips=state.Docked;
            ship.factionGroup=(byte)state.OwnerGroup;ship.factionId=state.OwnerId;ship.type=(byte)state.Type;
            ship.cntBlocks=state.BlockCount;ship.cntDevices=state.DeviceCount;
            if(phase!="INFO") {
                if(!state.Ready)throw Texts.Tag(new InvalidOperationException("Vaisseau non pret sur le playfield; aucun depot"),"error.ship_not_ready");
                if(state.DockedTo>0)throw Texts.Tag(new InvalidOperationException("Vaisseau encore arrime a l entite "+state.DockedTo+" selon le playfield"),"error.docked");
                if(state.Occupants.Count>0&&state.Pilot==0)throw Texts.Tag(new InvalidOperationException("Joueurs encore dans le vaisseau selon le playfield : "+string.Join(",",state.Occupants)),"error.occupants");
            }
            ship.pos=live.pos;ship.rot=live.rot;
            int index=list.globalStructures[sector].FindIndex(x=>x.id==shipId);list.globalStructures[sector][index]=ship;
            double distance=Distance(current.pos,live.pos);
            Log("RANGE_RESULT phase="+phase+" player="+player+" ship="+shipId+" playerSector="+current.playfield+" shipSector="+sector+" playerPos="+PositionText(current.pos)+" shipPos="+PositionText(live.pos)+" listPos="+PositionText(cached)+" cacheDeltaM="+Distance(cached,live.pos).ToString("0.0",CultureInfo.InvariantCulture)+" distanceM="+distance.ToString("0.0",CultureInfo.InvariantCulture)+" maxM=250");
            return current;
        }
        internal static void ValidateOwner(PlayerInfo p,GlobalStructureInfo s) {
            if(s.factionGroup!=1 || s.factionId!=p.entityId) throw Texts.Tag(new InvalidOperationException("Utilise un vaisseau en Prive, appartenant a ton personnage"),"error.owner");
        }
        void Validate(PlayerInfo p,GlobalStructureList l,int shipId) {
            var ship=Find(l,shipId); ValidateOwner(p,ship);
            if(ship.type<3||ship.type>5) throw Texts.Tag(new InvalidOperationException("Vaisseaux CV, SV ou HV uniquement"),"error.ship_type");
            string sector=Playfield(l,shipId);
            if(sector!=p.playfield) throw Texts.Tag(new InvalidOperationException("Secteurs differents : toi = "+Clean(p.playfield)+" ; vaisseau "+shipId+" = "+Clean(sector)+". Le depot exige le meme secteur"),"error.playfield");
            double distance=Distance(p.pos,ship.pos);
            if(!(distance<=250)) throw Texts.Tag(new InvalidOperationException("Vaisseau "+shipId+" ("+Clean(ship.name)+") : "+distance.ToString("0.0",CultureInfo.InvariantCulture)+" m de son centre selon l API (maximum 250 m). qh:info:"+shipId+" pour les positions"),"error.distance");
            if(ship.powered)throw Texts.Tag(new InvalidOperationException("Alimentation encore ALLUMEE selon le playfield pour "+shipId+". Eteins le vaisseau puis relance"),"error.ship_powered");
            if(ship.pilotId>0)throw Texts.Tag(new InvalidOperationException("Cockpit encore occupe selon le playfield : pilote ID "+ship.pilotId+". Quitte le cockpit puis relance"),"error.pilot");
            if((ship.dockedShips!=null&&ship.dockedShips.Count>0))
                throw Texts.Tag(new InvalidOperationException("Detache les vaisseaux arrimes avant l operation"),"error.docked");
        }
        async Task<bool> Dialog(PlayerInfo identity,string message,string positive) {
            int player=identity.entityId;
            var answer=(IdAndIntValue)await Request(CmdId.Request_ShowDialog_SinglePlayer,
                new DialogBoxData { Id=player,MsgText="Quantum Hangar Qc — BETA 0.2.6\n\n"+message,PosButtonText=positive,NegButtonText=T(identity,"button.cancel") },CmdId.Event_DialogButtonIndex,player,DialogTimeout);
            return answer.Id==player && answer.Value==0;
        }
        internal int UiOpenTimeout=6000,UiTimeout=90000;
        async Task<Live.HangarUiReply> ChooseShip(PlayerInfo player,List<Entry> entries) {
            var r=new Live.HangarUiRequest{Token=Guid.NewGuid().ToString("N"),Player=player.entityId,Client=player.clientId,Steam=player.steamId,Playfield=player.playfield,Language=Language(player),Protocol=2,CreatedUtc=DateTime.UtcNow};
            for(int slot=1;slot<=Math.Max(ArchiveStore.Capacity,entries.Count==0?0:entries.Max(e=>e.PersonalSlot));slot++) {
                var e=entries.SingleOrDefault(x=>x.PersonalSlot==slot);
                r.Rows.Add(new Live.HangarRow{Slot=slot,ArchiveId=e==null?null:e.ArchiveId,Name=e==null?null:e.Ship.name,Status=e==null?null:e.Status});
            }
            string dir=Path.Combine(logDir,"HangarUI"),request=Path.Combine(dir,r.Token+".request.xml"),reply=Path.Combine(dir,r.Token+".reply.xml"),opened=Path.Combine(dir,r.Token+".opened.xml");
            Live.Transfer.Write(request,r);
            try {
                var start=DateTime.UtcNow;
                while(!stopped&&(DateTime.UtcNow-start).TotalMilliseconds<UiTimeout) {
                    if(File.Exists(reply)) {
                        var a=Live.Transfer.Read<Live.HangarUiReply>(reply);
                        if(a.Token!=r.Token||a.Player!=r.Player||a.Client!=r.Client||a.Steam!=r.Steam||a.CapturedUtc<r.CreatedUtc.AddSeconds(-1)||a.CapturedUtc>DateTime.UtcNow.AddSeconds(1))throw Texts.Tag(new InvalidDataException("Reponse de selection non correspondante"),"error.ui");
                        if(!string.IsNullOrEmpty(a.Error))throw Texts.Tag(new InvalidOperationException(a.Error),"error.ui");
                        if(!a.Cancelled&&!r.Rows.Any(x=>x.Slot==a.Slot&&x.ArchiveId==a.ArchiveId&&x.Status=="STORED"))throw Texts.Tag(new InvalidDataException("Vaisseau selectionne absent de la liste disponible"),"error.ui");
                        return a;
                    }
                    if(!File.Exists(opened)&&(DateTime.UtcNow-start).TotalMilliseconds>UiOpenTimeout)throw Texts.Tag(new TimeoutException("Composant de liste cliquable sans reponse"),"error.ui");
                    await Task.Delay(50);
                }
                return new Live.HangarUiReply{Cancelled=true};
            } finally {foreach(string f in new[]{request,reply,opened})try{if(File.Exists(f))File.Delete(f);}catch(IOException){}}
        }
        static string Clean(string s) { return Texts.Escape(s??""); }
        internal static bool TryParseCommand(string text,out string[] args) {
            args=null;
            if(text==null||text.Length>120||text.IndexOfAny(new[]{'\r','\n','\t'})>=0) return false;
            var lang=Regex.Match(text.Trim(),@"^qh:lang(?::(?<code>[A-Za-z][A-Za-z0-9_-]{0,23}))?$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
            if(lang.Success) {args=lang.Groups["code"].Success?new[]{"qh:lang",lang.Groups["code"].Value}:new[]{"qh:lang"};return true;}
            var m=Regex.Match(text.Trim(),@"^qh(?::(?<verb>help|status|list|mark|store|load|info))?(?:(?: +|:)(?<a>[0-9]+))?(?:(?: +|:)(?<b>[0-9]+))?$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
            if(!m.Success) return false;
            string verb=m.Groups["verb"].Value.ToLowerInvariant();
            var result=new List<string>{verb==""?"qh":"qh:"+verb};
            if(m.Groups["a"].Success) result.Add(m.Groups["a"].Value);
            if(m.Groups["b"].Success) result.Add(m.Groups["b"].Value);
            int wanted=verb=="store"?2:(verb=="load"||verb=="info")?2:1;
            if(result.Count!=wanted && !(verb=="store"&&result.Count==3)) return false;
            args=result.ToArray();return true;
        }
        static int Number(string s) { int n; if(!int.TryParse(s,NumberStyles.None,CultureInfo.InvariantCulture,out n)||n<=0) throw Texts.Tag(new InvalidOperationException("ID ou emplacement invalide"),"error.syntax"); return n; }
        internal static bool RetryableCopy(Entry e) {
            if(e.Status!="QUARANTINE"||e.SpawnedId!=0||e.Files==null) return false;
            if(e.StoreStage=="COPYING"||e.StoreStage=="ARCHIVED") return true;
            // Legacy v0.1.5: digests are only added after all Shared copies finish.
            // An empty digest list + this exact copy error cannot have reached Verify/Destroy.
            return string.IsNullOrEmpty(e.StoreStage)&&e.Files.Count==0&&e.Detail!=null&&
                e.Detail.Contains("because it is being used by another process")&&e.Detail.Contains(".area");
        }
        internal Task ActiveTask;
        async void Run(int id,string[] args) {
            try { ActiveTask=Execute(id,args); await ActiveTask; }
            catch(Exception ex) { Log("ERROR player="+id+" "+ex.Message);  }
            finally { lock(gate) { busy.Remove(id); if(stopped && busy.Count==0 && instanceLock!=null) { instanceLock.Dispose();instanceLock=null; } } }
        }
        async Task Execute(int id,string[] args) {
            var initial=await Player(id);string steam=initial.steamId;int client=initial.clientId;
            try { await ExecuteCore(id,args,initial); }
            catch(Exception ex) {
                Log("ERROR player="+id+" code="+(ex.Data["QhKey"]??"error.unexpected")+" "+ex.Message);
                try {var current=await Player(id);if(current.steamId==steam&&current.clientId==client)Notify(id,T(current,"operation.failed",Texts.Error(Language(current),ex)));}
                catch(Exception){Log("QH_LOCALE ERROR_RECIPIENT_UNVERIFIED");}
            }
        }
        readonly HashSet<int> languageBusy=new HashSet<int>();
        readonly Dictionary<int,DateTime> languageLast=new Dictionary<int,DateTime>();
        internal Task LanguageTask;
        async void RunLanguage(int id,string[] args) {
            lock(gate){DateTime last;if(languageBusy.Contains(id)||(languageLast.TryGetValue(id,out last)&&(DateTime.UtcNow-last).TotalSeconds<1))return;languageBusy.Add(id);languageLast[id]=DateTime.UtcNow;}
            try { LanguageTask=ChangeLanguage(id,args);await LanguageTask; }
            catch(Exception ex){Log("QH_LOCALE COMMAND_ERROR "+ex.Message);}
            finally{lock(gate)languageBusy.Remove(id);}
        }
        async Task ChangeLanguage(int id,string[] args) {
            string steam=null;int client=-1;
            try {
                var p=await Player(id);string key=null;
                steam=p.steamId;client=p.clientId;
                if(args.Length==2) {
                    bool auto=args[1].Equals("auto",StringComparison.OrdinalIgnoreCase);string code=Languages.Normalize(args[1]);
                    if(!auto&&code==null)key="language.unknown";
                    else {preferences.Set(p.steamId,auto?null:code);key=auto?"language.cleared":"language.saved";}
                }
                var current=await Player(id);if(current.steamId!=steam||current.clientId!=client)return;
                p=current;
                var resolved=preferences.Resolve(p.steamId,defaultLanguage);
                string message=(key==null?"":T(p,key)+"\n\n")+T(p,"language.effective",Languages.Name(resolved.Code),T(p,resolved.Origin))+"\n\n"+T(p,"language.auto_unavailable")+"\n\n"+Languages.Choices();
                bool operation;lock(gate)operation=busy.Contains(id);
                // Do not replace a pending confirmation dialog while changing language.
                if(operation)Notify(id,message);else await Dialog(p,message,T(p,"button.close"));
            }catch(Exception ex){
                Log("QH_LOCALE COMMAND_ERROR "+ex.Message);
                if(steam!=null)try{var current=await Player(id);if(current.steamId==steam&&current.clientId==client)Notify(id,T(current,"operation.failed",Texts.Error(Language(current),ex)));}catch(Exception){Log("QH_LOCALE ERROR_RECIPIENT_UNVERIFIED");}
            }
        }
        async Task ExecuteCore(int id,string[] args,PlayerInfo p) {
            // Preserve session values across awaits even if the API reuses a PlayerInfo object.
            string sessionSteam=p.steamId;int sessionClient=p.clientId;
            string cmd=args[0].ToLowerInvariant();
            Log("COMMAND player="+id+" command="+string.Join(" ",args));
            if((cmd=="qh"||cmd=="qh:help"||cmd=="qh:status") && args.Length==1) {
                await Dialog(p,T(p,"help.body"),T(p,"button.close")); return;
            }
            if(cmd!="qh:store"&&cmd!="qh:list"&&cmd!="qh:load"&&cmd!="qh:mark"&&cmd!="qh:info") throw Texts.Tag(new InvalidOperationException("Syntaxe : qh:store:ID:PLACE / qh:load:PLACE / qh:list / qh:mark"),"error.syntax");
            int value=args.Length>=2?Number(args[1]):0;
            int requestedSlot=args.Length==3?Number(args[2]):0;
            if(requestedSlot>ArchiveStore.Capacity) throw Texts.Tag(new InvalidOperationException("Choisis une place de 1 a 10"),"error.slot_range");
            if(cmd=="qh:load"&&(value<1||value>15)) throw Texts.Tag(new InvalidOperationException("Numero de place invalide"),"error.slot_range");
            if(cmd=="qh:info") {
                var infoList=await Structures(id);p=await LiveStorePosition(p,infoList,value,"INFO");var infoShip=Find(infoList,value);string sector=Playfield(infoList,value);
                await Dialog(p,T(p,"info.body",value,infoShip.name,p.playfield,sector,PositionText(p.pos),PositionText(infoShip.pos),sector==p.playfield?Distance(p.pos,infoShip.pos).ToString("0.0",CultureInfo.InvariantCulture):"—",infoShip.factionGroup,infoShip.factionId,p.entityId,T(p,infoShip.powered?"power.on":"power.off"),infoShip.pilotId),T(p,"button.close"));return;
            }
            int shipLock=cmd=="qh:store"?value:0;
            lock(gate) {
                if(owners.Contains(sessionSteam)||(shipLock>0&&entities.Contains(shipLock))) throw Texts.Tag(new InvalidOperationException("Une operation sur ton hangar ou ce vaisseau est deja ouverte"),"error.busy");
                owners.Add(sessionSteam); if(shipLock>0) entities.Add(shipLock);
            }
            try {
                var entries=store.List(sessionSteam);
                string selectedArchive=null;
                if(cmd=="qh:mark") {
                    var markNew=new Placement {Playfield=p.playfield,Position=p.pos,SteamId=sessionSteam,ClientId=sessionClient,PlayerId=id,Created=DateTime.UtcNow,MarkerName="QH_SORTIE_"+Guid.NewGuid().ToString("N")};
                    Placement previous;lock(gate)placements.TryGetValue(sessionSteam,out previous);
                    if(previous!=null) await ClearMarker(previous);
                    bool gpsSent=true;
                    try {await Marker(markNew,true);}catch(Exception ex){gpsSent=false;Log("GPS_UNAVAILABLE "+ex.Message);}
                    var markSession=await Player(id);
                    if(markSession.steamId!=sessionSteam||markSession.clientId!=sessionClient||markSession.playfield!=markNew.Playfield)throw Texts.Tag(new InvalidOperationException("Session ou secteur change : refaire qh:mark"),"error.session");
                    lock(gate)placements[sessionSteam]=markNew;
                    Notify(id,T(p,"mark.success")+" "+T(p,gpsSent?"mark.gps_sent":"mark.gps_unavailable"));return;
                }
                if(cmd=="qh:list") {
                    int retired=store.RetireRestoredQuarantines(sessionSteam,Log);
                    if(retired>0) entries=store.List(sessionSteam);
                    Live.HangarUiReply choice;
                    try {choice=await ChooseShip(p,entries);}
                    catch(Exception ex) {
                        Log("UI_FALLBACK "+ex.Message);
                        var rows=Enumerable.Range(1,Math.Max(ArchiveStore.Capacity,entries.Count==0?0:entries.Max(e=>e.PersonalSlot))).Select(slot=>{var item=entries.SingleOrDefault(e=>e.PersonalSlot==slot);return slot+" : "+(item==null?T(p,"hangar.list.empty"):Clean(item.Ship.name)+" — "+Live.HangarUiRules.Status(Language(p),item.Status));});
                        await Dialog(p,T(p,"hangar.list.title",entries.Count)+"\n\n"+string.Join("\n",rows)+"\n\n"+T(p,"hangar.list.fallback"),T(p,"button.close"));return;
                    }
                    if(choice.Cancelled)return;
                    p=await Player(id);
                    if(p.steamId!=sessionSteam||p.clientId!=sessionClient)throw Texts.Tag(new InvalidOperationException("Session changee pendant la selection"),"error.session");
                    var selected=store.At(choice.Slot,sessionSteam);
                    if(selected.ArchiveId!=choice.ArchiveId||selected.Status!="STORED")throw Texts.Tag(new InvalidOperationException("Archive modifiee depuis l ouverture de la liste"),"error.archive");
                    value=choice.Slot;selectedArchive=choice.ArchiveId;cmd="qh:load";
                    Log("UI_LOAD_SELECTED player="+id+" slot="+value+" archive="+selectedArchive);
                }

                if(cmd=="qh:store") {
                    if(requestedSlot>ArchiveStore.Capacity) throw Texts.Tag(new InvalidOperationException("Choisis une place de 1 a 10"),"error.slot_range");
                    var l=await Structures(id);p=await LiveStorePosition(p,l,value,"PREVIEW");
                    if(p.steamId!=sessionSteam||p.clientId!=sessionClient)throw Texts.Tag(new InvalidOperationException("Session changee pendant la preparation"),"error.session");
                    Validate(p,l,value);
                    string storeSector=p.playfield;
                    var ship=Find(l,value);
                    var previousEntries=entries.Where(existing=>existing.ShipId==value).ToList();
                    Entry retry=previousEntries.Count==1&&RetryableCopy(previousEntries[0])?previousEntries[0]:null;
                    if(retry!=null) {
                        if(retry.Ship.type!=ship.type||retry.Ship.cntBlocks!=ship.cntBlocks||retry.Ship.cntDevices!=ship.cntDevices)
                            throw Texts.Tag(new InvalidOperationException("Ancien depot incomplet mais structure modifiee : conserver l archive pour analyse"),"error.archive");
                        entries=entries.Where(existing=>existing.ArchiveId!=retry.ArchiveId).ToList();
                        if(requestedSlot==0) requestedSlot=retry.PersonalSlot;
                    }
                    if(entries.Count>=ArchiveStore.Capacity) throw Texts.Tag(new InvalidOperationException("Hangar plein : 10 places maximum"),"error.hangar_full");
                    if(entries.Any(existing=>existing.ShipId==value)) throw Texts.Tag(new InvalidOperationException("Ce vaisseau possede deja une transaction dans ton hangar"),"error.duplicate");
                    if(requestedSlot>0&&entries.Any(existing=>existing.PersonalSlot==requestedSlot)) throw Texts.Tag(new InvalidOperationException("Place "+requestedSlot+" occupee : choisis une place libre"),"error.slot_occupied");
                    int selectedSlot=requestedSlot>0?requestedSlot:Enumerable.Range(1,ArchiveStore.Capacity).First(x=>!entries.Any(existing=>existing.PersonalSlot==x));
                    if(!await Dialog(p,(retry==null?"":T(p,"store.retry")+"\n\n")+T(p,"store.confirm.body",ship.name,ship.id,selectedSlot),T(p,"button.store"))) { Log("CANCEL store player="+id); return; }
                    var fresh=await Player(id); if(fresh.steamId!=sessionSteam||fresh.clientId!=sessionClient) throw Texts.Tag(new InvalidOperationException("Session changee"),"error.session");
                    l=await Structures(id); fresh=await LiveStorePosition(fresh,l,value,"CONFIRM");Validate(fresh,l,value);
                    if(fresh.playfield!=storeSector)throw Texts.Tag(new InvalidOperationException("Secteur change pendant la confirmation; relance le depot dans le secteur actuel"),"error.session");
                    await CheckPlayersClear(id,storeSector,Find(l,value).pos,30);
                    if(retry!=null) {
                        var current=store.At(retry.PersonalSlot,sessionSteam);
                        if(current.ArchiveId!=retry.ArchiveId||!RetryableCopy(current)) throw Texts.Tag(new InvalidOperationException("La tentative precedente a change"),"error.archive");
                        store.Forget(current);
                        Log("INCOMPLETE_COPY_RETIRED archive="+current.ArchiveId+" entity="+value+"; original present; no replay or spawn");
                    }
                    var e=store.Reserve(Find(l,value),sessionSteam,selectedSlot);
                    try {
                        string export=Path.Combine(store.DirectoryFor(e),"Export.dat");
                        Log("EXPORT_REQUEST archive="+e.ArchiveId+" entity="+value+" forceUnload=false");
                        await Request(CmdId.Request_Entity_Export,new EntityExportInfo { id=value,playfield=storeSector,filePath=export,isForceUnload=false },CmdId.Event_Ok,id);
                        // Acknowledgement/file presence is only evidence of export, not a coherent live snapshot.
                        long previous=-1; bool present=false;
                        for(int i=0;i<10;i++) {
                            await Task.Delay(ExportPoll); if(stopped) throw Texts.Tag(new InvalidOperationException("Arret pendant export"),"error.stopped");
                            long size=File.Exists(export)?new FileInfo(export).Length:0;
                            if(size>0&&size==previous) { present=true; break; } previous=size;
                        }
                        if(!present) throw Texts.Tag(new InvalidDataException("Export.dat absent, vide ou toujours en cours d'ecriture sur le chemin serveur"),"error.copy");
                        e.StoreStage="COPYING";store.Save(e);
                        await Task.Run(()=>CaptureFiles(e));
                        e.StoreStage="ARCHIVED";e.Status="EXPORTED_UNVERIFIED"; e.Detail="Archive native copiee. Coffres et coherence du moteur non encore valides."; store.Save(e);
                        store.Verify(e);
                        var finalPlayer=await Player(id);
                        if(finalPlayer.steamId!=sessionSteam || finalPlayer.clientId!=sessionClient) throw Texts.Tag(new InvalidOperationException("Session changee avant retrait"),"error.session");
                        var finalList=await Structures(id); finalPlayer=await LiveStorePosition(finalPlayer,finalList,value,"BEFORE_REMOVE");Validate(finalPlayer,finalList,value);
                        if(finalPlayer.playfield!=storeSector)throw Texts.Tag(new InvalidOperationException("Secteur change pendant l export; aucun retrait"),"error.session");
                        await CheckPlayersClear(id,storeSector,Find(finalList,value).pos,30);
                        await Task.Run(()=> { CompareSource(e); store.Verify(e); });
                        // Durable intent BEFORE a destructive request. Unknown outcome is never retried.
                        e.StoreStage="REMOVING";e.Status="REMOVING"; e.Detail="Demande de retrait imminente; aucun rejeu automatique."; store.Save(e);
                        Log("DESTROY_REQUEST archive="+e.ArchiveId+" entity="+value);
                        await Request(CmdId.Request_Entity_Destroy,new Id(value),CmdId.Event_Ok,id);
                        await WaitPresence(id,value,false);
                        e.Status="STORED";e.Detail="Absence de l original confirmee par la liste globale. Coffres a verifier apres restitution.";store.Save(e);
                        Log("STORED archive="+e.ArchiveId+" slot="+e.PersonalSlot+" files="+e.Files.Count);
                        Notify(id,T(p,"store.success",e.PersonalSlot));
                    } catch(Exception ex) { e.Status="QUARANTINE";e.Detail=ex.Message;store.Save(e);throw; }
                    return;
                }
                var entry=store.At(value,sessionSteam); string archive=entry.ArchiveId;
                if(selectedArchive!=null&&selectedArchive!=archive)throw Texts.Tag(new InvalidOperationException("Archive selectionnee modifiee"),"error.archive");
                if(entry.Status!="STORED") throw Texts.Tag(new InvalidOperationException("Restitution bloquee : "+entry.Status+". Conserver le journal et les archives."),"error.quarantine");
                Placement mark; lock(gate) { if(!placements.TryGetValue(sessionSteam,out mark)) throw Texts.Tag(new InvalidOperationException("Choisis d abord une zone libre avec qh:mark"),"error.mark_missing"); }
                ValidateMark(p,await Structures(id),mark);
                await Task.Run(()=>store.Verify(entry));
                string destination=mark.Position.x.ToString("F0",CultureInfo.InvariantCulture)+", "+mark.Position.y.ToString("F0",CultureInfo.InvariantCulture)+", "+mark.Position.z.ToString("F0",CultureInfo.InvariantCulture);
                if(!await Dialog(p,T(p,"load.confirm.body",entry.Ship.name,value,mark.Playfield,destination),T(p,"button.load"))) return;
                var again=await Player(id); if(again.steamId!=sessionSteam||again.clientId!=sessionClient) throw Texts.Tag(new InvalidOperationException("Session changee"),"error.session");
                var now=await Structures(id); ValidateMark(again,now,mark);
                await CheckPlayersClear(id,mark.Playfield,mark.Position,50);
                entry=store.At(value,sessionSteam); if(entry.ArchiveId!=archive || entry.Status!="STORED") throw Texts.Tag(new InvalidOperationException("L archive a change"),"error.archive");
                if(Contains(now,entry.ShipId)) throw Texts.Tag(new InvalidOperationException("Original encore present dans le monde : restitution refusee"),"error.original_present");
                await Task.Run(()=>store.Verify(entry));
                var newId=(Id)await Request(CmdId.Request_NewEntityId,null,CmdId.Event_NewEntityId,id);
                if(newId.id<=0||Contains(now,newId.id)) throw Texts.Tag(new InvalidDataException("Nouvel ID invalide ou deja utilise"),"error.spawn");
                entry.SpawnedId=newId.id;entry.DestinationPlayfield=mark.Playfield;entry.DestinationPos=mark.Position;
                entry.Status="RESTORING";entry.Detail="ID et destination reserves. Ne jamais relancer automatiquement une apparition.";store.Save(entry);
                try {
                    string dest=Path.Combine(saveDir,"Shared",newId.id.ToString(CultureInfo.InvariantCulture));
                    if(Directory.Exists(dest)) throw Texts.Tag(new IOException("Dossier destination deja present : "+newId.id),"error.spawn");
                    await Task.Run(()=>RestoreFiles(entry,dest));
                    Log("SPAWN_REQUEST archive="+archive+" newId="+newId.id+" playfield="+mark.Playfield+" at="+destination);
                    await Request(CmdId.Request_Entity_Spawn,new EntitySpawnInfo {
                        forceEntityId=newId.id,playfield=mark.Playfield,pos=mark.Position,rot=entry.Ship.rot,
                        name=entry.Ship.name,type=entry.Ship.type,entityTypeName="",factionGroup=1,factionId=again.entityId,
                        exportedEntityDat=Path.Combine(store.DirectoryFor(entry),"Export.dat")
                    },CmdId.Event_Ok,id);
                    var observed=await WaitPresence(id,newId.id,true);
                    await ClearMarker(mark,8);
                    lock(gate) placements.Remove(sessionSteam);
                    var restored=Find(observed,newId.id); ValidateOwner(again,restored);
                    if(restored.type!=entry.Ship.type || restored.cntBlocks!=entry.Ship.cntBlocks || restored.cntDevices!=entry.Ship.cntDevices || Playfield(observed,newId.id)!=mark.Playfield)
                        throw Texts.Tag(new InvalidDataException("Structure restituee differente : conserver l archive pour analyse"),"error.spawn");
                    entry.Status="RESTORED_CARGO_UNVERIFIED";entry.Detail="Entite, proprietaire, blocs et appareils verifies. Coffres NON verifies par le module; comparaison manuelle requise.";store.Save(entry);
                    store.Forget(entry);
                    Log("RESTORED archive="+archive+" newId="+newId.id+" CARGO_UNVERIFIED; archived recovery retained");
                    Notify(id,T(again,"load.success",newId.id));
                } catch(Exception ex) { entry.Status="QUARANTINE";entry.Detail=ex.Message;store.Save(entry);throw; }

            } finally { lock(gate) { owners.Remove(sessionSteam); if(shipLock>0) entities.Remove(shipLock); } }
        }
        static bool Contains(GlobalStructureList list,int id) { return list.globalStructures.Values.Any(v=>v!=null&&v.Any(s=>s.id==id)); }
        async Task<GlobalStructureList> WaitPresence(int player,int entity,bool present) {
            for(int i=0;i<10;i++) { await Task.Delay(ExportPoll); var l=await Structures(player); if(Contains(l,entity)==present) return l; }
            throw Texts.Tag(new TimeoutException("Presence/absence non confirmee pour "+entity+"; transaction en quarantaine"),"error.quarantine");
        }
        async Task CheckPlayersClear(int player,string playfield,PVector3 pos,double radius) {
            var ids=(IdList)await Request(CmdId.Request_Player_List,null,CmdId.Event_Player_List,player);
            if(ids==null||ids.list==null) throw Texts.Tag(new InvalidDataException("Liste joueurs indisponible"),"error.live_state");
            foreach(int id in ids.list) {
                var person=(PlayerInfo)await Request(CmdId.Request_Player_Info,new Id(id),CmdId.Event_Player_Info,id);
                if(person.playfield==playfield && !(Distance(person.pos,pos)>=radius)) throw Texts.Tag(new InvalidOperationException("Eloigner tous les joueurs a "+radius+" m du centre du vaisseau/point avant l operation (y compris toi)"),"error.players_near",radius);
            }
        }
        internal static string GpsCommand(int client,PlacementData point) {
            if(client<0||!Live.MarkerRules.Valid(point.Request))throw Texts.Tag(new InvalidDataException("Point GPS invalide"),"error.gps");
            var r=point.Request;
            // Native bridge explicitly removes the HUD key. Expire only filters the saved bookmark.
            // Engine marker command uses integer coordinates. Keep exact floating point coordinates for the ship itself.
            string xyz=string.Join(",",new[]{r.X,r.Y,r.Z}.Select(v=>Math.Round(v,MidpointRounding.AwayFromZero).ToString("0",CultureInfo.InvariantCulture)));
            return "remoteex cl="+client.ToString(CultureInfo.InvariantCulture)+" 'marker add name=QH_Sortie_"+r.Name.Substring(10,8)+" pos="+xyz+" W expire=300'";
        }
        internal sealed class PlacementData {public Live.MarkerRequest Request;}
        Live.MarkerRequest MarkerData(Placement mark,bool activate,int delay=0) {
            return new Live.MarkerRequest{Token=Guid.NewGuid().ToString("N"),Name=mark.MarkerName,Steam=mark.SteamId,Player=mark.PlayerId,Playfield=mark.Playfield,X=mark.Position.x,Y=mark.Position.y,Z=mark.Position.z,Activate=activate,CreatedUtc=DateTime.UtcNow,ExpiresUtc=mark.Created.AddMinutes(5),RemoveDelaySeconds=delay};
        }
        async Task MarkerExchange(Live.MarkerRequest r) {
            string dir=Path.Combine(logDir,"MapMarkers");Directory.CreateDirectory(dir);
            string request=Path.Combine(dir,r.Token+".request.xml"),reply=Path.Combine(dir,r.Token+".reply.xml");
            Live.Transfer.Write(request,r);
            try {
                DateTime until=DateTime.UtcNow.AddMilliseconds(StateTimeout);
                while(DateTime.UtcNow<until) {
                    if(File.Exists(reply)) {
                        var answer=Live.Transfer.Read<Live.MarkerReply>(reply);
                        if(answer.Token!=r.Token||answer.Steam!=r.Steam||answer.Player!=r.Player||answer.Name!=r.Name||answer.Activate!=r.Activate)throw Texts.Tag(new InvalidDataException("Reponse GPS non correspondante"),"error.gps");
                        if(!answer.Success)throw Texts.Tag(new InvalidOperationException(answer.Error??"Pont GPS indisponible"),"error.gps");
                        return;
                    }
                    await Task.Delay(StatePoll);
                }
                throw Texts.Tag(new TimeoutException("Pont GPS sans reponse : verifier QuantumHangarQcState 0.2.6"),"error.gps");
            }finally {if(File.Exists(request))File.Delete(request);if(File.Exists(reply))File.Delete(reply);}
        }
        async Task Marker(Placement mark,bool activate) {
            if(!activate){await ClearMarker(mark);return;}
            await gpsGate.WaitAsync();
            try {
                if(mark.GpsStopped||mark.GpsArmed)return;
                var p=await Player(mark.PlayerId,true);
                if(p.steamId!=mark.SteamId||p.clientId!=mark.ClientId||p.playfield!=mark.Playfield)throw Texts.Tag(new InvalidOperationException("Session ou secteur du GPS change"),"error.session");
                if(mark.GpsStopped||DateTime.UtcNow-mark.Created>TimeSpan.FromMinutes(5))return;
                var request=MarkerData(mark,true);
                await MarkerExchange(request);mark.GpsArmed=true;
                // Recheck after the asynchronous native handshake, before targeting a client ID.
                p=await Player(mark.PlayerId,true);
                if(p.steamId!=mark.SteamId||p.clientId!=mark.ClientId||p.playfield!=mark.Playfield)throw Texts.Tag(new InvalidOperationException("Session ou secteur du GPS change"),"error.session");
                string command=GpsCommand(p.clientId,new PlacementData{Request=request});
                Log("GPS_COMMAND "+command);
                await Request(CmdId.Request_ConsoleCommand,new PString(command),CmdId.Event_Ok,p.entityId,sendOnly:true,gps:true);
                Log("GPS_SENT_ONCE player="+p.entityId+" name="+mark.MarkerName+" position="+PositionText(mark.Position));
            } finally {gpsGate.Release();}
        }
        async Task ClearMarker(Placement mark,int delay=0) {
            mark.GpsStopped=true;
            await gpsGate.WaitAsync();
            try {
                if(!mark.GpsArmed)return;
                await MarkerExchange(MarkerData(mark,false,delay));
                Log("GPS_REMOVE_REQUEST_ACCEPTED name="+mark.MarkerName+" delaySeconds="+delay+" (voir QH_GPS REMOVE_SENT dans le journal playfield)");
            }catch(Exception ex){Log("GPS_REMOVE_ERROR name="+mark.MarkerName+" reason="+ex.Message+"; expiration serveur de secours conservee");}
            finally{gpsGate.Release();}
        }
        void ScheduleGps() {
            Placement mark=null;
            lock(gate) {
                if(stopped||(GpsTask!=null&&!GpsTask.IsCompleted))return;
                foreach(var pair in placements.ToArray()) {
                    var p=pair.Value;
                    if(p.GpsStopped){placements.Remove(pair.Key);continue;}
                    if(GpsNow()-p.Created>TimeSpan.FromMinutes(5)){placements.Remove(pair.Key);mark=p;break;}
                }
                if(mark!=null)GpsTask=ClearMarker(mark);
            }
        }
        void ValidateMark(PlayerInfo p,GlobalStructureList l,Placement mark) {
            if(mark.SteamId!=p.steamId||mark.ClientId!=p.clientId||mark.PlayerId!=p.entityId||mark.Playfield!=p.playfield||DateTime.UtcNow-mark.Created>TimeSpan.FromMinutes(5))
                throw Texts.Tag(new InvalidOperationException("Point expire ou session/secteur change : refaire qh:mark"),"error.mark_expired");
            if(!(Distance(p.pos,mark.Position)>=50) || !(Distance(p.pos,mark.Position)<=1000)) throw Texts.Tag(new InvalidOperationException("Reste entre 50 et 1000 m du point de sortie"),"error.mark_distance");
            foreach(var pair in l.globalStructures.Where(k=>k.Key==mark.Playfield)) foreach(var ship in pair.Value??new List<GlobalStructureInfo>())
                if(!(Distance(ship.pos,mark.Position)>=100)) throw Texts.Tag(new InvalidOperationException("Une structure est a moins de 100 m du point : choisir un autre point"),"error.obstacle");
            // Centre distances are only a coarse check. Terrain and full hull bounds are NOT exposed here.
        }
        void CompareSource(Entry e) {
            string source=Path.Combine(saveDir,"Shared",e.ShipId.ToString(CultureInfo.InvariantCulture));
            var files=new List<string>();Collect(source,files);
            var expected=e.Files.Where(f=>f.Path.StartsWith("Shared"+Path.DirectorySeparatorChar,StringComparison.Ordinal)).ToArray();
            if(files.Count!=expected.Length) throw Texts.Tag(new InvalidDataException("Liste Shared modifiee avant retrait"),"error.source_changed");
            foreach(var f in expected) { string p=Path.Combine(source,f.Path.Substring(7)); if(!File.Exists(p)||Disk.HashShared(p)!=f.Sha256) throw Texts.Tag(new InvalidDataException("Shared modifie avant retrait; depot refuse"),"error.source_changed"); }
            // Does not lock in-memory cargo or other mods; experimental test world only.
        }
        void RestoreFiles(Entry e,string dest) {
            Directory.CreateDirectory(dest);
            foreach(var f in e.Files.Where(f=>f.Path.StartsWith("Shared"+Path.DirectorySeparatorChar,StringComparison.Ordinal))) {
                if(stopped) throw Texts.Tag(new InvalidOperationException("Arret pendant restauration"),"error.stopped");
                string output=Path.Combine(dest,f.Path.Substring(7));Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.Copy(Path.Combine(store.DirectoryFor(e),f.Path),output,false);
                if(Disk.Hash(output)!=f.Sha256) throw Texts.Tag(new InvalidDataException("Copie de restauration differente"),"error.spawn");
            }
        }
        void CaptureFiles(Entry e) {
            string target=store.DirectoryFor(e), source=Path.Combine(saveDir,"Shared",e.ShipId.ToString(CultureInfo.InvariantCulture));
            if(!Directory.Exists(source)) throw Texts.Tag(new DirectoryNotFoundException("Dossier Shared du vaisseau absent"),"error.copy");
            var all=new List<string>(); Collect(source,all);
            if(all.Count==0) throw Texts.Tag(new InvalidDataException("Dossier Shared vide"),"error.copy");
            long total=new FileInfo(Path.Combine(target,"Export.dat")).Length;
            foreach(var file in all) { total=checked(total+new FileInfo(file).Length); if(total>512L*1024*1024) throw Texts.Tag(new InvalidDataException("Export trop volumineux (>512 Mio)"),"error.size_limit"); }
            foreach(var file in all) {
                if(stopped) throw Texts.Tag(new InvalidOperationException("Module arrete pendant copie"),"error.stopped");
                string rel=file.Substring(source.Length+1), dest=Path.Combine(target,"Shared",rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                for(int attempt=1;;attempt++) {
                    if(stopped) throw Texts.Tag(new InvalidOperationException("Module arrete pendant copie"),"error.stopped");
                    try {
                        string before=Disk.HashShared(file);
                        Disk.CopyShared(file,dest);
                        string copied=Disk.Hash(dest),after=Disk.HashShared(file);
                        if(before!=copied||copied!=after) throw Texts.Tag(new IOException("Fichier source modifie pendant copie : "+rel),"error.source_changed");
                        Log("SHARED_COPIED file="+rel+" bytes="+new FileInfo(dest).Length+" attempt="+attempt);
                        break;
                    } catch(IOException ex) {
                        Log("SHARED_COPY_RETRY file="+rel+" attempt="+attempt+" error="+ex.Message);
                        if(attempt>=3) throw Texts.Tag(new IOException("Copie Shared indisponible apres 3 essais : "+rel+". Le vaisseau n a pas ete retire. "+ex.Message,ex),"error.copy");
                        Thread.Sleep(200);
                    }
                }
            }
            var saved=new List<string>(); Collect(target,saved);
            foreach(var file in saved.Where(f=>Path.GetFileName(f)!="Manifest.xml")) e.Files.Add(new FileDigest { Path=file.Substring(target.Length+1),Bytes=new FileInfo(file).Length,Sha256=Disk.Hash(file) });
        }
        static void Collect(string dir,List<string> files) {
            if((File.GetAttributes(dir)&FileAttributes.ReparsePoint)!=0) throw Texts.Tag(new InvalidDataException("Lien filesystem refuse"),"error.copy");
            foreach(var f in Directory.GetFiles(dir)) { if((File.GetAttributes(f)&FileAttributes.ReparsePoint)!=0) throw Texts.Tag(new InvalidDataException("Lien filesystem refuse"),"error.copy"); files.Add(f); if(files.Count>20000) throw Texts.Tag(new InvalidDataException("Trop de fichiers dans cette archive"),"error.size_limit"); }
            foreach(var d in Directory.GetDirectories(dir)) Collect(d,files);
        }
        void Notify(int player,string text) {
            if(stopped) return;
            // Dispatcher marshals this to Game_Update just like other requests.
            Notice(player,text);
        }
        async void Notice(int player,string text) { try { await Request(CmdId.Request_InGameMessage_SinglePlayer,new IdMsgPrio(player,text,1,12),CmdId.Event_Ok,player,sendOnly:true); } catch(Exception ex) { Log("NOTICE "+ex.Message); } }
        void Log(string message) {
            string line=DateTime.UtcNow.ToString("o")+" [QuantumHangarQc] "+message;
            if(api!=null) api.Console_Write(line);
            if(logDir!=null) lock(gate) { try { File.AppendAllText(Path.Combine(logDir,"QuantumHangarQc.log"),line+Environment.NewLine); } catch { } }
        }
    }
}
