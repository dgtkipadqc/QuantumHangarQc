using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Eleon.Modding;
using QuantumHangarQc;

// These tests simulate API events and filesystem bytes. They do NOT simulate Empyrion cargo serialization.
class FakeApi : ModGameAPI {
    public HangarMod Mod; public string Root;
    public PlayerInfo P=new PlayerInfo { entityId=50,clientId=7,steamId="76561198000000001",permission=0,playfield="TestSpace",pos=new PVector3() };
    public GlobalStructureInfo Carrier=new GlobalStructureInfo { id=100,type=3,factionGroup=1,factionId=50,name="Carrier",pos=new PVector3(),dockedShips=new List<int>() };
    public GlobalStructureInfo Ship=new GlobalStructureInfo { id=200,type=4,factionGroup=1,factionId=50,name="Test SV",pos=new PVector3 {x=80},cntBlocks=45,cntDevices=12,pilotId=0,dockedShips=new List<int>() };
    public List<GlobalStructureInfo> Ships;
    public List<CmdId> Calls=new List<CmdId>(); public List<string> Messages=new List<string>(),Logs=new List<string>();
    public int DialogAnswer=0, NextId=900; public bool EmptyExport, FailExport, DropDestroyAck, DropSpawnAck, HoldDialogs, RejectDialog, BadRestoredStats;
    public bool ListUpdateReply, WrongUpdateSequence, MalformedUpdateList, KeepListCache;
    public PVector3? LivePosition;public bool WrongPositionId,RejectPosition;public Action AfterPosition;public string ListSector;
    public bool? LivePower;public int? LivePilot,LiveDockedTo;public bool NoState,WrongStateToken,WrongStateSteam,OldState;public List<int> LiveOccupants=new List<int>();
    public bool NoMarker,FailMarkerAdd;
    public List<string> ConsoleCommands=new List<string>();
    public List<QuantumHangarQc.Live.MarkerRequest> GpsRequests=new List<QuantumHangarQc.Live.MarkerRequest>();
    public void ReplyGps() {
        string dir=Path.Combine(Root,"Mods","QuantumHangarQc","MapMarkers");if(!Directory.Exists(dir))return;
        foreach(var file in Directory.GetFiles(dir,"*.request.xml")){
            QuantumHangarQc.Live.MarkerRequest r;try{r=QuantumHangarQc.Live.Transfer.Read<QuantumHangarQc.Live.MarkerRequest>(file);}catch(FileNotFoundException){continue;}
            string reply=Path.Combine(dir,r.Token+".reply.xml");if(File.Exists(reply))continue;
            GpsRequests.Add(r);
            QuantumHangarQc.Live.Transfer.Write(reply,new QuantumHangarQc.Live.MarkerReply{Token=r.Token,Name=r.Name,Steam=r.Steam,Player=r.Player,Activate=r.Activate,Success=!NoMarker,Error=NoMarker?"Pont indisponible":null});
        }
    }
    public bool UiEnabled,UiCancel,UiBadToken,UiBadArchive,UiChangeSession,UiChangeStatus;
    public int UiSelections; readonly HashSet<string> uiReplied=new HashSet<string>();
    public void ReplyUi() {
        string dir=Path.Combine(Root,"Mods","QuantumHangarQc","HangarUI");if(!UiEnabled||!Directory.Exists(dir))return;
        foreach(var file in Directory.GetFiles(dir,"*.request.xml")) {
            QuantumHangarQc.Live.HangarUiRequest r;try{r=QuantumHangarQc.Live.Transfer.Read<QuantumHangarQc.Live.HangarUiRequest>(file);}catch(FileNotFoundException){continue;}string reply=Path.Combine(dir,r.Token+".reply.xml");if(File.Exists(reply)||!uiReplied.Add(r.Token))continue;
            var row=r.Rows.FirstOrDefault(x=>x.Status=="STORED");UiSelections++;
            if(UiChangeSession)P.clientId++;
            if(UiChangeStatus&&row!=null){var e=Store().At(row.Slot,P.steamId);e.Status="QUARANTINE";Store().Save(e);}
            QuantumHangarQc.Live.Transfer.Write(reply,new QuantumHangarQc.Live.HangarUiReply{Token=UiBadToken?"wrong":r.Token,Steam=r.Steam,Player=r.Player,Client=r.Client,Slot=row==null?0:row.Slot,ArchiveId=UiBadArchive?"wrong":row==null?null:row.ArchiveId,Cancelled=UiCancel||row==null,CapturedUtc=DateTime.UtcNow});
        }
    }
    public void ReplyState() {
        ReplyGps();ReplyUi();
        string dir=Path.Combine(Root,"Mods","QuantumHangarQc","LiveState");if(NoState||!Directory.Exists(dir))return;
        foreach(var file in Directory.GetFiles(dir,"*.request.xml")) {
            QuantumHangarQc.Live.StateRequest r;try{r=QuantumHangarQc.Live.Transfer.Read<QuantumHangarQc.Live.StateRequest>(file);}catch(FileNotFoundException){continue;}string reply=Path.Combine(dir,r.Token+".reply.xml");if(File.Exists(reply))continue;
            var state=new QuantumHangarQc.Live.StateReply{Token=WrongStateToken?"wrong":r.Token,Entity=r.Entity,Player=r.Player,Steam=WrongStateSteam?"wrong":r.Steam,Playfield=r.Playfield,CapturedUtc=OldState?DateTime.UtcNow.AddMinutes(-1):DateTime.UtcNow,ObserverVersion="0.2.3",PlayersSeen=0,RequesterSeen=false,Ready=true,Powered=LivePower??Ship.powered,Pilot=LivePilot??Ship.pilotId,DockedTo=LiveDockedTo??(Ships.Where(x=>x.dockedShips!=null&&x.dockedShips.Contains(Ship.id)).Select(x=>x.id).FirstOrDefault()),Docked=Ship.dockedShips??new List<int>(),Occupants=LiveOccupants,OwnerGroup=Ship.factionGroup,OwnerId=Ship.factionId,Type=Ship.type,BlockCount=Ship.cntBlocks,DeviceCount=Ship.cntDevices};
            QuantumHangarQc.Live.Transfer.Write(reply,state);
        }
    }
    GlobalStructureList listCache;
    public bool StandardUpdateAck, InvalidUpdateAck, RejectUpdate;
    public int MissingUpdateParameters;
    public bool HostResponses, NoNoticeAck, WrongListSequence, WrongListPayload;
    public ushort HeldSeq; public DialogBoxData HeldDialog;
    public Action BeforeDialog;
    public FakeApi() { Ships=new List<GlobalStructureInfo> { Ship }; }
    public void Console_Write(string msg) { Logs.Add(msg); }
    public ulong Game_GetTickTime() { return 0; }
    public bool Game_Request(CmdId command,ushort seq,object data) {
        Calls.Add(command);
        if(command==CmdId.Request_Player_Info) Mod.Game_Event(CmdId.Event_Player_Info,seq,P);
        else if(command==CmdId.Request_Entity_PosAndRot) {
            Assert.That(((Id)data).id==Ship.id,"live position requests target ship only");
            if(RejectPosition)return false;
            Mod.Game_Event(CmdId.Event_Entity_PosAndRot,seq,new IdPositionRotation{id=WrongPositionId?999:Ship.id,pos=LivePosition??Ship.pos,rot=Ship.rot});
            if(AfterPosition!=null)AfterPosition();
        }
        else if(command==CmdId.Request_Player_List) Mod.Game_Event(CmdId.Event_Player_List,seq,new IdList { list=new List<int>{P.entityId} });
        else if(command==CmdId.Request_GlobalStructure_List) {
            int idx=Ships.FindIndex(s=>s.id==Ship.id);if(idx>=0) Ships[idx]=Ship;
            idx=Ships.FindIndex(s=>s.id==Carrier.id);if(idx>=0) Ships[idx]=Carrier;
            if(WrongListPayload) { Mod.Game_Event(CmdId.Request_GlobalStructure_List,seq,true); return true; }
            var freshList=new GlobalStructureList {globalStructures=new Dictionary<string,List<GlobalStructureInfo>> { { ListSector??P.playfield,Ships.ToList() } } };
            if(!KeepListCache||listCache==null) listCache=freshList;
            Mod.Game_Event(HostResponses?CmdId.Request_GlobalStructure_List:CmdId.Event_GlobalStructure_List,WrongListSequence?(ushort)(seq-1):seq,listCache);
        }
        else if(command==CmdId.Request_GlobalStructure_Update) {
            if(!(data is PString)||((PString)data).pstr!=P.playfield) {
                MissingUpdateParameters++;
                Mod.Game_Event(CmdId.Event_Error,seq,new ErrorInfo {errorType=ErrorType.MissingParameter});
            }
            else if(RejectUpdate) Mod.Game_Event(CmdId.Event_Error,seq,new ErrorInfo {errorType=ErrorType.MissingParameter});
            else if(InvalidUpdateAck) Mod.Game_Event(CmdId.Request_GlobalStructure_Update,seq,false);
            else if(ListUpdateReply) {
                int idx=Ships.FindIndex(s=>s.id==Ship.id);if(idx>=0) Ships[idx]=Ship;
                var refreshed=new GlobalStructureList {globalStructures=MalformedUpdateList?null:new Dictionary<string,List<GlobalStructureInfo>> { { ListSector??P.playfield,Ships.ToList() } }};
                Mod.Game_Event(CmdId.Event_GlobalStructure_List,WrongUpdateSequence?(ushort)(seq-1):seq,refreshed);
            }
            else if(StandardUpdateAck) Mod.Game_Event(CmdId.Event_Ok,seq,null);
            else Mod.Game_Event(CmdId.Request_GlobalStructure_Update,seq,true);
        }
        else if(command==CmdId.Request_ShowDialog_SinglePlayer) {
            HeldSeq=seq;HeldDialog=(DialogBoxData)data;
            if(BeforeDialog!=null) BeforeDialog();
            if(RejectDialog) return false;
            if(!HoldDialogs) Mod.Game_Event(CmdId.Event_DialogButtonIndex,seq,new IdAndIntValue {Id=P.entityId,Value=DialogAnswer});
        }
        else if(command==CmdId.Request_Entity_Export) {
            var e=(EntityExportInfo)data;
            Assert.That(e.id==Ship.id,"export ship ID, never player ID"); Assert.That(!e.isForceUnload,"no force unload");
            if(FailExport) return false;
            if(!EmptyExport) File.WriteAllText(e.filePath,"native export bytes for test only");
            
            Mod.Game_Event(CmdId.Event_Ok,seq,null);
        }
        else if(command==CmdId.Request_Entity_Destroy) {
            var e=Store().List("76561198000000001").Single();
            Assert.That(e.Status=="REMOVING","durable intent BEFORE destroy");
            Assert.That(e.Files.Count>=2,"archive files BEFORE destroy");
            Assert.That(((Id)data).id==Ship.id,"destroy only confirmed ship");
            Ships.RemoveAll(s=>s.id==Ship.id);
            if(!DropDestroyAck) Mod.Game_Event(CmdId.Event_Ok,seq,null);
        }
        else if(command==CmdId.Request_NewEntityId) Mod.Game_Event(CmdId.Event_NewEntityId,seq,new Id(NextId));
        else if(command==CmdId.Request_Entity_Spawn) {
            var e=Store().List("76561198000000001").Single(); var spawn=(EntitySpawnInfo)data;
            Assert.That(e.Status=="RESTORING"&&e.SpawnedId==spawn.forceEntityId,"durable intent BEFORE spawn");
            Assert.That(spawn.exportedEntityDat.EndsWith("Export.dat")&&string.IsNullOrEmpty(spawn.prefabName),"native archive, no factory/blueprint fallback");
            Assert.That(spawn.factionGroup==1&&spawn.factionId==P.entityId,"restored private owner");
            Assert.That(File.ReadAllText(Path.Combine(Root,"Shared",spawn.forceEntityId.ToString(),"0.area"))=="test bytes: chest A slot0=123x15; chest B slot2=456x9","saved bytes preserved (NOT engine cargo test)");
            Ships.Add(new GlobalStructureInfo {id=spawn.forceEntityId,type=spawn.type,factionGroup=spawn.factionGroup,factionId=spawn.factionId,name=spawn.name,pos=spawn.pos,rot=spawn.rot,cntBlocks=BadRestoredStats?1:Ship.cntBlocks,cntDevices=Ship.cntDevices});
            if(!DropSpawnAck) Mod.Game_Event(CmdId.Event_Ok,seq,null);
        }
        else if(command==CmdId.Request_ConsoleCommand) {
            if(NoMarker||FailMarkerAdd)return false;
            ConsoleCommands.Add(((PString)data).pstr);
        }
        else if(command==CmdId.Request_InGameMessage_SinglePlayer) { Messages.Add(data.ToString());if(!NoNoticeAck) Mod.Game_Event(CmdId.Event_Ok,seq,null); }
        else throw new Exception("unexpected command "+command);
        return true;
    }
    public ArchiveStore Store() { return new ArchiveStore(Path.Combine(Root,"Mods","QuantumHangarQc","Archives")); }
}
static class Assert {
    public static int Count;
    public static void That(bool yes,string name) { Count++;if(!yes) throw new Exception("FAIL: "+name); }
    public static void Throws(Action action,string name) { bool caught=false;try {action();}catch {caught=true;}That(caught,name); }
}
class Tests {
    static string root; static int serial;
    static FakeApi New() {
        var a=new FakeApi {Root=Path.Combine(root,(++serial).ToString())};
        Directory.CreateDirectory(Path.Combine(a.Root,"Shared","200"));
        File.WriteAllText(Path.Combine(a.Root,"Shared","200","0.area"),"test bytes: chest A slot0=123x15; chest B slot2=456x9");
        a.Mod=new HangarMod {RequestTimeout=120,DialogTimeout=200,ExportPoll=2,StateTimeout=180,StatePoll=2,UiOpenTimeout=80,UiTimeout=600};a.Mod.StartAt(a,a.Root);return a;
    }
    static void Pump(FakeApi a,int max=2500) {
        DateTime until=DateTime.UtcNow.AddMilliseconds(max);
        do { a.ReplyState();a.Mod.Game_Update();Thread.Sleep(1); if(a.Mod.ActiveTask!=null&&a.Mod.ActiveTask.IsCompleted) {a.Mod.Game_Update();return;} } while(DateTime.UtcNow<until);
        throw new Exception("test pump timeout");
    }
    static void WaitDialog(FakeApi a) {
        ushort previous=a.HeldSeq;DateTime until=DateTime.UtcNow.AddSeconds(1);
        while(DateTime.UtcNow<until){a.ReplyState();a.Mod.Game_Update();if(a.HeldSeq!=previous)return;Thread.Sleep(1);}throw new Exception("dialog wait failed");
    }
    static void Start(FakeApi a,string command) {
        var field=typeof(HangarMod).GetField("lastCommand",BindingFlags.Instance|BindingFlags.NonPublic);
        ((Dictionary<int,DateTime>)field.GetValue(a.Mod)).Clear();
        a.Mod.Game_Event(CmdId.Event_ChatMessage,0,new ChatInfo {playerId=a.P.entityId,msg=command});
    }
    static void Run(FakeApi a,string command) {Start(a,command);Pump(a);}
    static void Mark(FakeApi a) { a.P.pos=new PVector3{x=200};Run(a,"qh:mark");a.P.pos=new PVector3{x=140}; }
    static void Restart(FakeApi a) { a.Mod.Game_Exit();a.Mod=new HangarMod {RequestTimeout=120,DialogTimeout=200,ExportPoll=2,StateTimeout=180,StatePoll=2,UiOpenTimeout=80,UiTimeout=600};a.Mod.StartAt(a,a.Root); }
    static void CopyTree(string source,string destination) {
        Directory.CreateDirectory(destination);
        foreach(var f in Directory.GetFiles(source)) File.Copy(f,Path.Combine(destination,Path.GetFileName(f)),true);
        foreach(var d in Directory.GetDirectories(source)) CopyTree(d,Path.Combine(destination,Path.GetFileName(d)));
    }
    static Dictionary<string,string> Snapshot(string dir) {
        return Directory.GetFiles(dir,"*",SearchOption.AllDirectories).ToDictionary(p=>p.Substring(dir.Length),p=>Disk.Hash(p));
    }
    static void Unchanged(Dictionary<string,string> before,string dir,string label) {
        var after=Snapshot(dir);
        Assert.That(before.Count==after.Count && before.All(x=>after.ContainsKey(x.Key)&&after[x.Key]==x.Value),label);
    }
    static void QuarantineRegression(string fixture) {
        const string steam="76561198000000003";
        int initial=Assert.Count;
        var api=New();api.P.steamId=steam;
        var store=api.Store();CopyTree(fixture,store.Root);
        var old=store.At(3,steam);var recovery=Snapshot(store.DirectoryFor(old));
        var stored=store.List(steam).Where(e=>e.Status=="STORED").ToDictionary(e=>e.ArchiveId,e=>Snapshot(store.DirectoryFor(e)));
        Run(api,"qh:list");
        Assert.That(store.List(steam).Count==3 && api.HeldDialog.MsgText.Contains("3 : Libre") && api.HeldDialog.MsgText.Contains("3/10"),"uploaded case: list frees only slot 3");
        Unchanged(recovery,Path.Combine(store.Root,"Retired",old.ArchiveId),"all quarantined recovery files preserved byte for byte");
        foreach(var e in store.List(steam)) Unchanged(stored[e.ArchiveId],store.DirectoryFor(e),"stored archive unchanged "+e.PersonalSlot);
        Run(api,"qh:list");
        Assert.That(store.List(steam).Count==3 && api.Logs.Count(x=>x.Contains("RESTORED_QUARANTINE_RETIRED"))==1,"list cleanup idempotent");
        Assert.That(!api.Calls.Contains(CmdId.Request_Entity_Spawn)&&!api.Calls.Contains(CmdId.Request_Entity_Destroy),"cleanup sends no spawn or destroy request");
        api.Mod.Game_Exit();
        var changes=new Dictionary<string,Action<Entry,Entry>> {
            {"same name wrong entity",(a,b)=>a.SpawnedId=9999},
            {"no spawn",(a,b)=>a.SpawnedId=0},
            {"uncertain replacement",(a,b)=>b.Status="QUARANTINE"},
            {"unfinished deposit",(a,b)=>b.StoreStage="COPYING"},
            {"replacement already loaded",(a,b)=>b.SpawnedId=9999},
            {"changed owner",(a,b)=>b.Ship.factionId=9999},
            {"changed type",(a,b)=>b.Ship.type=99},
            {"changed blocks",(a,b)=>b.Ship.cntBlocks++},
            {"changed devices",(a,b)=>b.Ship.cntDevices++},
            {"old deposit",(a,b)=>b.Utc=a.Utc},
            {"invalid time",(a,b)=>b.Utc="invalid"},
            {"different blueprint",(a,b)=>b.Files.Single(f=>f.Path.EndsWith("backup.epb")).Sha256=new string('0',64)},
            {"missing blueprint",(a,b)=>b.Files.RemoveAll(f=>f.Path.EndsWith("backup.epb"))}
        };
        foreach(var test in changes) {
            var r=Path.Combine(root,"guard-"+(++serial));CopyTree(fixture,r);var st=new ArchiveStore(r);
            var a=st.At(3,steam);var b=st.At(1,steam);test.Value(a,b);st.Save(a);st.Save(b);
            var before=Snapshot(r);
            Assert.That(st.RetireRestoredQuarantines(steam,x=>{})==0,test.Key+" prevents retirement");
            Unchanged(before,r,test.Key+" leaves every file untouched");
        }
        foreach(var slot in new[]{1,3}) {
            var r=Path.Combine(root,"corrupt-"+(++serial));CopyTree(fixture,r);var st=new ArchiveStore(r);
            File.AppendAllText(Path.Combine(st.DirectoryFor(st.At(slot,steam)),"Shared","0.area"),"corrupt");
            var before=Snapshot(r);
            Assert.That(st.RetireRestoredQuarantines(steam,x=>{})==0,"corrupt archive blocks cleanup slot "+slot);
            Unchanged(before,r,"corrupt archive retained "+slot);
        }
        var collisionRoot=Path.Combine(root,"collision");CopyTree(fixture,collisionRoot);var collision=new ArchiveStore(collisionRoot);
        Directory.CreateDirectory(Path.Combine(collisionRoot,"Retired",collision.At(3,steam).ArchiveId));
        Assert.That(collision.RetireRestoredQuarantines(steam,x=>{})==0 && collision.List(steam).Count==4,"existing retired destination never overwritten");
        var ambiguousRoot=Path.Combine(root,"ambiguous");CopyTree(fixture,ambiguousRoot);var ambiguous=new ArchiveStore(ambiguousRoot);
        var duplicate=ambiguous.At(1,steam);string source=ambiguous.DirectoryFor(duplicate);
        duplicate.ArchiveId=Guid.NewGuid().ToString("N");duplicate.PersonalSlot=5;duplicate.Slot=5;
        CopyTree(source,ambiguous.DirectoryFor(duplicate));ambiguous.Save(duplicate);
        Assert.That(ambiguous.RetireRestoredQuarantines(steam,x=>{})==0 && ambiguous.List(steam).Count==5,"ambiguous lineage retained");
        Assert.That(ambiguous.RetireRestoredQuarantines("76561198000000002",x=>{})==0 && ambiguous.List(steam).Count==5,"other account cannot clean this hangar");
        Console.WriteLine("Uploaded archive regression PASS "+(Assert.Count-initial)+" assertions.");
    }
    static void PlayerBetaTests() {
        var a=New();Run(a,"qh:help");
        Assert.That(a.HeldDialog.MsgText.Contains("BETA 0.2.5")&&!a.HeldDialog.MsgText.Contains("TEST")&&a.HeldDialog.MsgText.Contains("10 places"),"ordinary player beta help");
        Run(a,"qh:store:200:11");Assert.That(!a.Calls.Contains(CmdId.Request_Entity_Export),"eleventh slot unavailable");
        Run(a,"qh:store:200:10");Assert.That(a.Store().At(10,a.P.steamId).Status=="STORED","player slot ten");
        a.P.pos=new PVector3{x=250.25f,y=-125.5f,z=88.75f};Run(a,"qh:mark");
        Assert.That(a.ConsoleCommands.Count==1&&a.ConsoleCommands[0].EndsWith("pos=250,-126,89 W expire=300'"),"single GPS created after cleanup handshake");
        Assert.That(a.GpsRequests.Count==1&&a.GpsRequests[0].Activate,"native cleanup armed first");
        a.Mod.GpsNow=()=>DateTime.UtcNow.AddSeconds(20);for(int i=0;i<10;i++)a.Mod.Game_Update();
        Assert.That(a.ConsoleCommands.Count==1,"no repeated marker add");
        a.P.pos=new PVector3{x=310.25f,y=-125.5f,z=88.75f};a.DialogAnswer=1;Run(a,"qh:load:10");
        Assert.That(!a.Calls.Contains(CmdId.Request_Entity_Spawn)&&a.GpsRequests.All(r=>r.Activate),"cancel keeps GPS and stored ship");
        a.DialogAnswer=0;Run(a,"qh:load:10");
        Assert.That(a.Ships.Any(x=>x.id==900)&&a.Store().List(a.P.steamId).Count==0,"restoration succeeds");
        Assert.That(a.GpsRequests.Last().RemoveDelaySeconds==8&&!a.GpsRequests.Last().Activate&&a.ConsoleCommands.Count==1,"confirmed spawn schedules targeted removal in eight seconds");a.Mod.Game_Exit();
        var noGps=New();Run(noGps,"qh:store:200");noGps.NoMarker=true;Mark(noGps);Run(noGps,"qh:load:1");
        Assert.That(noGps.Ships.Any(x=>x.id==900)&&noGps.ConsoleCommands.Count==0&&noGps.Logs.Any(x=>x.Contains("GPS_UNAVAILABLE")),"failed native handshake prevents unmanaged GPS but preserves hangar");noGps.Mod.Game_Exit();
        var replace=New();Mark(replace);Mark(replace);
        Assert.That(replace.ConsoleCommands.Count==2&&replace.GpsRequests.Count==3&&replace.GpsRequests[1].Activate==false&&replace.GpsRequests[1].RemoveDelaySeconds==0,"same-position replacement removes old GPS before new add");replace.Mod.Game_Exit();
        var cleanupFailure=New();Run(cleanupFailure,"qh:store:200");Mark(cleanupFailure);cleanupFailure.NoMarker=true;Run(cleanupFailure,"qh:load:1");
        Assert.That(cleanupFailure.Ships.Any(x=>x.id==900)&&cleanupFailure.Store().List(cleanupFailure.P.steamId).Count==0&&cleanupFailure.Logs.Any(x=>x.Contains("GPS_REMOVE_ERROR")),"GPS cleanup failure cannot quarantine a restored ship");cleanupFailure.Mod.Game_Exit();
        var legacy=New();Run(legacy,"qh:store:200");var entry=legacy.Store().At(1,legacy.P.steamId);entry.Slot=15;entry.PersonalSlot=15;legacy.Store().Save(entry);Mark(legacy);Run(legacy,"qh:load:15");
        Assert.That(legacy.Ships.Any(x=>x.id==900)&&legacy.Store().List(legacy.P.steamId).Count==0,"legacy slot recoverable");legacy.Mod.Game_Exit();
        var safe=new HangarMod.PlacementData{Request=new QuantumHangarQc.Live.MarkerRequest{Token=Guid.NewGuid().ToString("N"),Name="QH_SORTIE_"+Guid.NewGuid().ToString("N"),Player=50,Steam="76561198000000001",Playfield="P",X=1,Y=2,Z=3}};
        var culture=Thread.CurrentThread.CurrentCulture;Thread.CurrentThread.CurrentCulture=new System.Globalization.CultureInfo("fr-CA");
        Assert.That(HangarMod.GpsCommand(7,safe).Contains("pos=1,2,3 W expire=300"),"French numeric culture safe");Thread.CurrentThread.CurrentCulture=culture;
        safe.Request.X=float.NaN;Assert.Throws(()=>HangarMod.GpsCommand(7,safe),"nonfinite coordinate refused");safe.Request.X=1;safe.Request.Name="QH_SORTIE_'; destroy 123";Assert.Throws(()=>HangarMod.GpsCommand(7,safe),"console injection rejected");
    }
    static void Main(string[] args) {
        root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
        foreach(var mode in new[]{"success","cancel","bad-token","bad-archive","session","changed","no-mark","confirm-cancel"}) {
            var ui=New();Run(ui,"qh:store:200");
            if(mode!="no-mark"){Run(ui,"qh:mark");ui.P.pos=new PVector3{x=100};}
            ui.UiEnabled=true;ui.UiCancel=mode=="cancel";ui.UiBadToken=mode=="bad-token";ui.UiBadArchive=mode=="bad-archive";ui.UiChangeSession=mode=="session";ui.UiChangeStatus=mode=="changed";
            if(mode=="confirm-cancel")ui.DialogAnswer=1;
            Run(ui,"qh:list");
            Assert.That(ui.UiSelections==1,"one selection request "+mode);
            Assert.That(ui.Calls.Contains(CmdId.Request_Entity_Spawn)==(mode=="success"),"selection preserves spawn guards "+mode);
            if(mode=="success")Assert.That(ui.HeldDialog.PosButtonText=="Confirmer la sortie","click still asks final confirmation");
            Assert.That(Directory.GetFiles(Path.Combine(ui.Root,"Mods","QuantumHangarQc","HangarUI")).Length==0,"selection exchange cleaned "+mode);
            ui.Mod.Game_Exit();
        }
        var normal=New();normal.P.permission=0;Run(normal,"qh:store:200");Assert.That(normal.Calls.Contains(CmdId.Request_Entity_Export) && normal.Store().List(normal.P.steamId).Single().Status=="STORED","ordinary player store authorized");normal.Mod.Game_Exit();
        var cancel=New();cancel.DialogAnswer=1;Run(cancel,"qh:store:200");Assert.That(cancel.Store().List("76561198000000001").Count==0&&!cancel.Calls.Contains(CmdId.Request_Entity_Export),"cancel is nonmutating");cancel.Mod.Game_Exit();
        var stranger=New();stranger.Ship.factionId=99;Run(stranger,"qh:store:200");Assert.That(!stranger.Calls.Contains(CmdId.Request_ShowDialog_SinglePlayer),"non-owner refused");stranger.Mod.Game_Exit();
        var faction=New();faction.Ship.factionGroup=0;Run(faction,"qh:store:200");Assert.That(!faction.Calls.Contains(CmdId.Request_Entity_Export),"faction/public not treated as private owner");faction.Mod.Game_Exit();
        var dock=New();dock.Carrier.dockedShips.Add(200);dock.Ships.Add(dock.Carrier);Run(dock,"qh:store:200");Assert.That(!dock.Calls.Contains(CmdId.Request_Entity_Export),"docked ship refused");dock.Mod.Game_Exit();
        var powered=New();powered.Ship.powered=true;Run(powered,"qh:store:200");Assert.That(!powered.Calls.Contains(CmdId.Request_Entity_Export),"powered ship refused");powered.Mod.Game_Exit();
        var close=New();close.P.pos=new PVector3{x=75};Run(close,"qh:store:200");Assert.That(!close.Calls.Contains(CmdId.Request_Entity_Export),"nearby player blocks store");close.Mod.Game_Exit();
        var changed=New();changed.BeforeDialog=()=>changed.Ship.factionId=999;Run(changed,"qh:store:200");Assert.That(!changed.Calls.Contains(CmdId.Request_Entity_Export),"ownership rechecked after click");changed.Mod.Game_Exit();
        var reject=New();reject.RejectDialog=true;Run(reject,"qh:store:200");Assert.That(!reject.Calls.Contains(CmdId.Request_Entity_Destroy),"rejected dialog not confirmed");reject.Mod.Game_Exit();
        var spoof=New();spoof.HoldDialogs=true;Start(spoof,"qh:store:200");WaitDialog(spoof);
        ushort stale=spoof.HeldSeq;spoof.Mod.Game_Event(CmdId.Event_DialogButtonIndex,stale,new IdAndIntValue{Id=999,Value=0});
        Pump(spoof);Assert.That(!spoof.Calls.Contains(CmdId.Request_Entity_Export),"other player's reply ignored; timeout no export");
        Start(spoof,"qh:store:200");WaitDialog(spoof);Assert.That(spoof.HeldSeq!=stale,"dialog sequence not reused");
        spoof.Mod.Game_Event(CmdId.Event_DialogButtonIndex,stale,new IdAndIntValue{Id=50,Value=0});
        Assert.That(!spoof.Calls.Contains(CmdId.Request_Entity_Export),"late old click cannot confirm new operation");
        spoof.Mod.Game_Event(CmdId.Event_DialogButtonIndex,spoof.HeldSeq,new IdAndIntValue{Id=50,Value=1});Pump(spoof);spoof.Mod.Game_Exit();
        var missing=New();missing.EmptyExport=true;Run(missing,"qh:store:200");Assert.That(!missing.Calls.Contains(CmdId.Request_Entity_Destroy)&&missing.Store().List("76561198000000001").Single().Status=="QUARANTINE","empty export retains original and quarantines");missing.Mod.Game_Exit();
        var failed=New();failed.FailExport=true;Run(failed,"qh:store:200");Assert.That(!failed.Calls.Contains(CmdId.Request_Entity_Destroy),"export API refusal retains original");failed.Mod.Game_Exit();
        var lost=New();lost.DropDestroyAck=true;Run(lost,"qh:store:200");Assert.That(lost.Store().List("76561198000000001").Single().Status=="QUARANTINE","uncertain removal quarantines");
        Restart(lost);Mark(lost);Run(lost,"qh:load:1");Assert.That(!lost.Calls.Contains(CmdId.Request_Entity_Spawn),"restart cannot load uncertain removal");lost.Mod.Game_Exit();
        var good=New();Run(good,"qh:store:200");
        Assert.That(!good.Ships.Any(s=>s.id==200)&&good.Store().List("76561198000000001").Single().Status=="STORED","store removes only after archive");
        Restart(good);good.P.playfield="AnotherSpace";Mark(good);Run(good,"qh:load:1");
        Assert.That(good.Ships.Count(s=>s.id==900)==1&&good.Store().List("76561198000000001").Count==0,"one restoration with NO carrier, across restart and new playfield");
        Assert.That(Directory.GetDirectories(Path.Combine(good.Store().Root,"Retired")).Length==1,"recovery archive kept");
        Run(good,"qh:load:1");Assert.That(good.Calls.Count(c=>c==CmdId.Request_Entity_Spawn)==1,"repeat load does not duplicate ship");good.Mod.Game_Exit();
        var tamper=New();Run(tamper,"qh:store:200");var entry=tamper.Store().List("76561198000000001").Single();File.AppendAllText(Path.Combine(tamper.Store().DirectoryFor(entry),"Export.dat"),"tampered");
        Mark(tamper);Run(tamper,"qh:load:1");Assert.That(!tamper.Calls.Contains(CmdId.Request_Entity_Spawn),"changed archive cannot spawn");tamper.Mod.Game_Exit();
        var unknown=New();Run(unknown,"qh:store:200");Mark(unknown);unknown.DropSpawnAck=true;Run(unknown,"qh:load:1");
        Assert.That(unknown.Store().List("76561198000000001").Single().Status=="QUARANTINE"&&unknown.Ships.Any(s=>s.id==900),"spawn happened but ack lost -> quarantine");
        Restart(unknown);Mark(unknown);Run(unknown,"qh:load:1");Assert.That(unknown.Calls.Count(c=>c==CmdId.Request_Entity_Spawn)==1,"uncertain spawn never repeated after restart");unknown.Mod.Game_Exit();
        var mismatched=New();Run(mismatched,"qh:store:200");Mark(mismatched);mismatched.BadRestoredStats=true;Run(mismatched,"qh:load:1");
        Assert.That(mismatched.Store().List("76561198000000001").Single().Status=="QUARANTINE","mismatched restored hull remains quarantined");mismatched.Mod.Game_Exit();
        var obstacle=New();Run(obstacle,"qh:store:200");Mark(obstacle);obstacle.Ships.Add(new GlobalStructureInfo{id=300,pos=new PVector3{x=220}});Run(obstacle,"qh:load:1");Assert.That(!obstacle.Calls.Contains(CmdId.Request_Entity_Spawn),"nearby structure blocks placement");obstacle.Mod.Game_Exit();
        var full=New();for(int i=0;i<10;i++)full.Store().Reserve(new GlobalStructureInfo{id=1000+i,name="Capacity test"},full.P.steamId);
        Run(full,"qh:store:200");Assert.That(!full.Calls.Contains(CmdId.Request_Entity_Export)&&full.Store().List("76561198000000001").Count==10,"11th reservation refused including unfinished operations");full.Mod.Game_Exit();
        var concurrent=New();concurrent.HoldDialogs=true;Start(concurrent,"qh:store:200");WaitDialog(concurrent);
        Start(concurrent,"qh:store:200");concurrent.Mod.Game_Update();Assert.That(concurrent.Calls.Count(c=>c==CmdId.Request_ShowDialog_SinglePlayer)==1,"double command yields one dialog");
        concurrent.Mod.Game_Event(CmdId.Event_DialogButtonIndex,concurrent.HeldSeq,new IdAndIntValue{Id=50,Value=1});Pump(concurrent);concurrent.Mod.Game_Exit();
        string[] parsed;
        Assert.That(HangarMod.TryParseCommand("qh:store:10006:3",out parsed)&&parsed.SequenceEqual(new[]{"qh:store","10006","3"}),"user syntax and explicit slot parsed");
        Assert.That(HangarMod.TryParseCommand("QH:STORE 10006 15",out parsed)&&parsed[2]=="15","space alias and case-insensitive commands");
        Assert.That(HangarMod.TryParseCommand("qh:store:10006",out parsed)&&parsed.Length==2,"single ship ID means next free slot");
        foreach(var invalid in new[]{"qh:store", "qh:store:1:2:3", "qh:load:1:2", "qh:list:1", "qh:mark:1", "qh::store:1", "qh:store:1\nother", "qh:store:abc", "qh:store:-1", "qh:load:1;destroy"})
            Assert.That(!HangarMod.TryParseCommand(invalid,out parsed),"invalid command rejected: "+invalid);
        var chosen=New();Assert.That(!chosen.Ships.Any(x=>x.type==3),"fixture has no CV at all");Run(chosen,"qh:store:200:3");
        Assert.That(chosen.Store().List(chosen.P.steamId).Single().PersonalSlot==3,"chosen slot 3 used");
        Run(chosen,"qh:list");Assert.That(chosen.HeldDialog.MsgText.Contains("1 : Libre")&&chosen.HeldDialog.MsgText.Contains("3 : Test SV")&&chosen.HeldDialog.MsgText.Contains("10 : Libre") && !chosen.HeldDialog.MsgText.Contains("11 :"),"all ten slots shown, occupied and free");
        var occupied=chosen.Store().List(chosen.P.steamId).Single();string manifest=File.ReadAllText(Path.Combine(chosen.Store().DirectoryFor(occupied),"Manifest.xml"));
        Assert.Throws(()=>chosen.Store().Reserve(new GlobalStructureInfo{id=333},chosen.P.steamId,3),"occupied slot cannot be overwritten");
        Assert.That(File.ReadAllText(Path.Combine(chosen.Store().DirectoryFor(occupied),"Manifest.xml"))==manifest,"occupied archive unchanged");
        Mark(chosen);Run(chosen,"qh:load:3");Assert.That(chosen.Ships.Any(x=>x.id==900)&&chosen.Store().List(chosen.P.steamId).Count==0,"explicit slot restores without CV");chosen.Mod.Game_Exit();
        foreach(string input in new[]{"qh:store:200:0","qh:store:200:16","qh:load:0","qh:load:16","qh:store 100 200"}) {
            var range=New();Run(range,input);Assert.That(!range.Calls.Contains(CmdId.Request_Entity_Export)&&!range.Calls.Contains(CmdId.Request_Entity_Spawn),"slot range/old syntax fails safely: "+input);range.Mod.Game_Exit();
        }
        var owners=New();var otherSteam="76561198000000002";
        owners.Store().Reserve(owners.Ship,owners.P.steamId,10);
        owners.Store().Reserve(new GlobalStructureInfo{id=777},otherSteam,10);
        Assert.That(owners.Store().At(10,owners.P.steamId).ShipId==200&&owners.Store().At(10,otherSteam).ShipId==777,"slot 10 belongs independently to each SteamID");
        Assert.That(owners.Store().DirectoryFor(owners.Store().At(10,otherSteam))!=owners.Store().DirectoryFor(owners.Store().At(10,owners.P.steamId)),"separate persistent player paths");owners.Mod.Game_Exit();
        var stable=New();var slot3=stable.Store().Reserve(new GlobalStructureInfo{id=300},stable.P.steamId,3);
        stable.Store().Reserve(new GlobalStructureInfo{id=301},stable.P.steamId,10);stable.Store().Forget(slot3);
        Assert.That(stable.Store().List(stable.P.steamId).Single().PersonalSlot==10,"removing slot 3 never renumbers slot 10");
        var refill=stable.Store().Reserve(new GlobalStructureInfo{id=302},stable.P.steamId,3);
        Assert.That(refill.PersonalSlot==3&&stable.Store().List(stable.P.steamId).Count==2,"freed slot 3 reusable without overwriting slot 10");
        var automatic=stable.Store().Reserve(new GlobalStructureInfo{id=303},stable.P.steamId);Assert.That(automatic.PersonalSlot==1,"omitted slot selects the FIRST free place, not the next occupied index");stable.Mod.Game_Exit();
        var legacy=New();string legacyArchive=Guid.NewGuid().ToString("N");
        var oldEntry=new Entry{CarrierId=100,Slot=3,PersonalSlot=0,ArchiveId=legacyArchive,SteamId=legacy.P.steamId,ShipId=888,Status="QUARANTINE",Utc="2026-09-13T00:00:00Z"};
        string oldDir=legacy.Store().DirectoryFor(oldEntry);Directory.CreateDirectory(oldDir);legacy.Store().Save(oldEntry);
        var listed=legacy.Store().List(legacy.P.steamId).Single();
        Assert.That(listed.PersonalSlot==3&&listed.Status=="QUARANTINE"&&Directory.Exists(oldDir),"legacy archive adopted in place, no re-enable");
        var oldEntry2=new Entry{CarrierId=101,Slot=3,PersonalSlot=0,ArchiveId=Guid.NewGuid().ToString("N"),SteamId=legacy.P.steamId,ShipId=889,Status="RESERVED",Utc="2026-09-13T01:00:00Z"};
        Directory.CreateDirectory(legacy.Store().DirectoryFor(oldEntry2));legacy.Store().Save(oldEntry2);
        var combined=legacy.Store().List(legacy.P.steamId);
        Assert.That(combined.Count==2&&combined.Select(e=>e.PersonalSlot).Distinct().Count()==2&&combined.Single(e=>e.ArchiveId==legacyArchive).PersonalSlot==3,"legacy slot collision assigned without moving existing slot");
        Restart(legacy);var persisted=legacy.Store().List(legacy.P.steamId);Assert.That(persisted.Select(e=>e.PersonalSlot).SequenceEqual(combined.Select(e=>e.PersonalSlot)),"legacy numbering persists across restart");
        Mark(legacy);Run(legacy,"qh:load:3");Assert.That(!legacy.Calls.Contains(CmdId.Request_Entity_Spawn),"legacy quarantine cannot spawn");legacy.Mod.Game_Exit();
        var bridge=New();bridge.HostResponses=true;bridge.NoNoticeAck=true;Run(bridge,"qh:store:200:4");
        Assert.That(bridge.Store().At(4,bridge.P.steamId).Status=="STORED","host echoed request response permits deposit");
        Assert.That(bridge.Calls.Contains(CmdId.Request_GlobalStructure_Update),"host cache refresh requested");
        Mark(bridge);Run(bridge,"qh:load:4");
        Assert.That(bridge.Ships.Any(s=>s.id==900)&&bridge.Store().List(bridge.P.steamId).Count==0,"host echoed responses permit restore");
        Thread.Sleep(220);bridge.Mod.Game_Update();
        Assert.That(!bridge.Logs.Any(l=>l.Contains("NOTICE Delai")),"notification does not wait for absent ACK");bridge.Mod.Game_Exit();
        var badSeq=New();badSeq.HostResponses=true;badSeq.WrongListSequence=true;Run(badSeq,"qh:store:200");
        Assert.That(!badSeq.Calls.Contains(CmdId.Request_Entity_Export),"wrong sequence cannot authorize export");badSeq.Mod.Game_Exit();
        var badPayload=New();badPayload.HostResponses=true;badPayload.WrongListPayload=true;Run(badPayload,"qh:store:200");
        Assert.That(!badPayload.Calls.Contains(CmdId.Request_Entity_Export),"wrong payload cannot authorize export");badPayload.Mod.Game_Exit();
        var nativeUpdate=New();nativeUpdate.HostResponses=true;nativeUpdate.StandardUpdateAck=true;
        Run(nativeUpdate,"qh:store:200:5");
        Assert.That(nativeUpdate.MissingUpdateParameters==0,"refresh contains PString current playfield");
        Assert.That(nativeUpdate.Store().At(5,nativeUpdate.P.steamId).Status=="STORED","standard Event_Ok refresh permits deposit");
        Mark(nativeUpdate);Run(nativeUpdate,"qh:load:5");
        Assert.That(nativeUpdate.Ships.Any(s=>s.id==900),"standard Event_Ok refresh permits restore");nativeUpdate.Mod.Game_Exit();
        var refusal=New();refusal.HostResponses=true;refusal.RejectUpdate=true;Run(refusal,"qh:store:200");
        Assert.That(!refusal.Calls.Contains(CmdId.Request_Entity_Export)&&refusal.Store().List(refusal.P.steamId).Count==0,"API refresh error stops before archive/export");refusal.Mod.Game_Exit();
        var falseAck=New();falseAck.HostResponses=true;falseAck.InvalidUpdateAck=true;Run(falseAck,"qh:store:200");
        Assert.That(!falseAck.Calls.Contains(CmdId.Request_Entity_Export),"false refresh response cannot authorize deposit");falseAck.Mod.Game_Exit();
        var observed=New();observed.HostResponses=true;observed.ListUpdateReply=true;observed.KeepListCache=true;
        Run(observed,"qh:store:200:6");
        Assert.That(observed.Store().At(6,observed.P.steamId).Status=="STORED","server log protocol Update -> Event_GlobalStructure_List permits deposit");
        Assert.That(observed.Calls.Count(c=>c==CmdId.Request_GlobalStructure_List)==1,"fresh update list used directly, stale host cache never re-read");
        Mark(observed);Run(observed,"qh:load:6");
        Assert.That(observed.Ships.Any(s=>s.id==900)&&observed.Store().List(observed.P.steamId).Count==0,"observed protocol restores with stale initial cache");observed.Mod.Game_Exit();
        var badUpdateSeq=New();badUpdateSeq.HostResponses=true;badUpdateSeq.ListUpdateReply=true;badUpdateSeq.WrongUpdateSequence=true;
        Run(badUpdateSeq,"qh:store:200");Assert.That(!badUpdateSeq.Calls.Contains(CmdId.Request_Entity_Export),"unrelated update list cannot authorize export");badUpdateSeq.Mod.Game_Exit();
        var badUpdateList=New();badUpdateList.HostResponses=true;badUpdateList.ListUpdateReply=true;badUpdateList.MalformedUpdateList=true;
        Run(badUpdateList,"qh:store:200");Assert.That(!badUpdateList.Calls.Contains(CmdId.Request_Entity_Export),"missing structure dictionary cannot authorize export");badUpdateList.Mod.Game_Exit();
        var shared=New();
        using(var writer=new FileStream(Path.Combine(shared.Root,"Shared","200","0.area"),FileMode.Open,FileAccess.ReadWrite,FileShare.Read)) {
            Run(shared,"qh:store:200:7");
            Assert.That(shared.Store().At(7,shared.P.steamId).Status=="STORED","copy and final hash coexist with shared game writer");
        }
        shared.Mod.Game_Exit();
        var locked=New();
        using(var writer=new FileStream(Path.Combine(locked.Root,"Shared","200","0.area"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {
            Run(locked,"qh:store:200:8");
            Assert.That(!locked.Calls.Contains(CmdId.Request_Entity_Destroy),"exclusive file lock never causes source destruction");
            var blocked=locked.Store().At(8,locked.P.steamId);
            Assert.That(blocked.Status=="QUARANTINE"&&blocked.StoreStage=="COPYING","copy failure persists phase before removal");
            Assert.That(locked.Logs.Count(l=>l.Contains("SHARED_COPY_RETRY"))==3,"exclusive lock uses bounded three attempts");
        }
        var beforeRetry=locked.Store().At(8,locked.P.steamId);string oldArchiveDir=locked.Store().DirectoryFor(beforeRetry);
        locked.DialogAnswer=1;Run(locked,"qh:store:200");
        Assert.That(Directory.Exists(oldArchiveDir)&&locked.Store().At(8,locked.P.steamId).ArchiveId==beforeRetry.ArchiveId,"cancel retains quarantined attempt and slot");
        locked.DialogAnswer=0;Run(locked,"qh:store:200");
        Assert.That(locked.Store().At(8,locked.P.steamId).Status=="STORED","confirmed retry preserves chosen slot after releasing lock");
        Assert.That(Directory.Exists(Path.Combine(locked.Store().Root,"Retired",beforeRetry.ArchiveId)),"failed copy preserved in Retired");
        locked.Mod.Game_Exit();
        var oldLock=New();var oldAttempt=oldLock.Store().Reserve(oldLock.Ship,oldLock.P.steamId,1);
        oldAttempt.Status="QUARANTINE";oldAttempt.Detail="The process cannot access the file '0.area' because it is being used by another process.";oldLock.Store().Save(oldAttempt);
        Restart(oldLock);Run(oldLock,"qh:store:200:1");
        Assert.That(oldLock.Store().At(1,oldLock.P.steamId).Status=="STORED","legacy 0.1.5 empty-digest sharing violation safely retried after restart");
        oldLock.Mod.Game_Exit();
        var uncertainEntry=new Entry {Status="QUARANTINE",Detail="0.area because it is being used by another process",Files=new List<FileDigest>{new FileDigest()}};
        Assert.That(!HangarMod.RetryableCopy(uncertainEntry),"legacy nonempty manifest is not assumed safe to retry");
        uncertainEntry.Files.Clear();uncertainEntry.StoreStage="REMOVING";
        Assert.That(!HangarMod.RetryableCopy(uncertainEntry),"removal intent cannot be auto-retired");
        uncertainEntry.StoreStage="COPYING";uncertainEntry.SpawnedId=900;
        Assert.That(!HangarMod.RetryableCopy(uncertainEntry),"spawned archive cannot be auto-retired");
        var stalePos=New();stalePos.Ship.pos=new PVector3{x=9000};stalePos.LivePosition=new PVector3{x=80};Run(stalePos,"qh:store:200");
        Assert.That(stalePos.Store().At(1,stalePos.P.steamId).Status=="STORED","live position repairs stale structure-list distance");
        Assert.That(stalePos.Logs.Any(x=>x.Contains("cacheDeltaM=8920.0")&&x.Contains("distanceM=80.0")),"diagnostic records cached and actual distances");stalePos.Mod.Game_Exit();
        var trueFar=New();trueFar.LivePosition=new PVector3{x=500};Run(trueFar,"qh:store:200");
        Assert.That(!trueFar.Calls.Contains(CmdId.Request_ShowDialog_SinglePlayer)&&!trueFar.Calls.Contains(CmdId.Request_Entity_Export),"actual far ship cannot use near cached position");
        Assert.That(trueFar.Logs.Any(x=>x.Contains("500.0 m de son centre")),"actual measured distance in refusal");trueFar.Mod.Game_Exit();
        var wrongSector=New();wrongSector.ListSector="Elsewhere";Run(wrongSector,"qh:store:200");
        Assert.That(wrongSector.Logs.Any(x=>x.Contains("Secteurs differents"))&&!wrongSector.Calls.Contains(CmdId.Request_Entity_Export),"sector mismatch is distinct and blocks despite nearby coordinates");wrongSector.Mod.Game_Exit();
        var movingPlayer=New();movingPlayer.P.pos=new PVector3{x=9000};movingPlayer.AfterPosition=()=>movingPlayer.P.pos=new PVector3();Run(movingPlayer,"qh:store:200");
        Assert.That(movingPlayer.Store().At(1,movingPlayer.P.steamId).Status=="STORED","player position refreshed after structure position");movingPlayer.Mod.Game_Exit();
        var wrongPos=New();wrongPos.WrongPositionId=true;Run(wrongPos,"qh:store:200");Assert.That(!wrongPos.Calls.Contains(CmdId.Request_Entity_Export),"position of another entity cannot authorize export");wrongPos.Mod.Game_Exit();
        var noPos=New();noPos.RejectPosition=true;Run(noPos,"qh:store:200");Assert.That(!noPos.Calls.Contains(CmdId.Request_Entity_Export),"position request refused never falls back to stale cache");noPos.Mod.Game_Exit();
        var nanPos=New();nanPos.LivePosition=new PVector3{x=float.NaN};Run(nanPos,"qh:store:200");Assert.That(!nanPos.Calls.Contains(CmdId.Request_Entity_Export),"invalid coordinates cannot authorize export");nanPos.Mod.Game_Exit();
        var edge=New();edge.LivePosition=new PVector3{x=250};Run(edge,"qh:store:200");Assert.That(edge.Store().At(1,edge.P.steamId).Status=="STORED","250m boundary accepted");edge.Mod.Game_Exit();
        var beyond=New();beyond.LivePosition=new PVector3{x=250.1f};Run(beyond,"qh:store:200");Assert.That(!beyond.Calls.Contains(CmdId.Request_Entity_Export),"over 250m refused");beyond.Mod.Game_Exit();
        var info=New();Run(info,"qh:info:200");Assert.That(info.HeldDialog.MsgText.Contains("Distance au centre")&&info.Store().List(info.P.steamId).Count==0&&!info.Calls.Contains(CmdId.Request_Entity_Export),"qh info diagnostic is read-only");info.Mod.Game_Exit();
        var walked=New();walked.BeforeDialog=()=>walked.LivePosition=new PVector3{x=500};Run(walked,"qh:store:200");Assert.That(!walked.Calls.Contains(CmdId.Request_Entity_Export),"range rechecked after confirmation");walked.Mod.Game_Exit();
        var switched=New();switched.BeforeDialog=()=>switched.P.playfield="NewSector";Run(switched,"qh:store:200");Assert.That(!switched.Calls.Contains(CmdId.Request_Entity_Export)&&switched.Logs.Any(x=>x.Contains("Secteur change pendant")),"sector change after confirmation cannot export using old sector");switched.Mod.Game_Exit();
        var cachePower=New();cachePower.Ship.powered=true;cachePower.Ship.pilotId=50;cachePower.LivePower=false;cachePower.LivePilot=0;Run(cachePower,"qh:store:200");Assert.That(cachePower.Store().At(1,cachePower.P.steamId).Status=="STORED","stale cached power and pilot cannot block live empty unpowered ship");Assert.That(cachePower.Logs.Any(x=>x.Contains("cachedPower=True livePower=False")&&x.Contains("cachedPilot=50 livePilot=0")),"cached versus live state logged");cachePower.Mod.Game_Exit();
        var realPower=New();realPower.LivePower=true;Run(realPower,"qh:store:200");Assert.That(!realPower.Calls.Contains(CmdId.Request_Entity_Export)&&realPower.Logs.Any(x=>x.Contains("ALLUMEE")),"live powered ship refused despite cached off state");realPower.Mod.Game_Exit();
        var realPilot=New();realPilot.LivePilot=88;Run(realPilot,"qh:store:200");Assert.That(!realPilot.Calls.Contains(CmdId.Request_Entity_Export)&&realPilot.Logs.Any(x=>x.Contains("pilote ID 88")),"live pilot refused despite cached empty cockpit");realPilot.Mod.Game_Exit();
        var occupiedLive=New();occupiedLive.LiveOccupants.Add(99);Run(occupiedLive,"qh:store:200");Assert.That(!occupiedLive.Calls.Contains(CmdId.Request_Entity_Export),"live occupant blocks export");occupiedLive.Mod.Game_Exit();
        var missingState=New();missingState.NoState=true;Run(missingState,"qh:store:200");Assert.That(!missingState.Calls.Contains(CmdId.Request_Entity_Export)&&missingState.Logs.Any(x=>x.Contains("Etat en direct indisponible")),"missing helper fails closed");missingState.Mod.Game_Exit();
        var mismatchedState=New();mismatchedState.WrongStateToken=true;Run(mismatchedState,"qh:store:200");Assert.That(!mismatchedState.Calls.Contains(CmdId.Request_Entity_Export),"wrong state token cannot authorize export");mismatchedState.Mod.Game_Exit();
        var oldState=New();oldState.OldState=true;Run(oldState,"qh:store:200");Assert.That(!oldState.Calls.Contains(CmdId.Request_Entity_Export),"expired state cannot authorize export");oldState.Mod.Game_Exit();
        var stateChanged=New();stateChanged.BeforeDialog=()=>stateChanged.LivePower=true;Run(stateChanged,"qh:store:200");Assert.That(!stateChanged.Calls.Contains(CmdId.Request_Entity_Export),"power switched on while dialog open blocks export");stateChanged.Mod.Game_Exit();
        var staleDock=New();staleDock.Carrier.dockedShips.Add(200);staleDock.Ships.Add(staleDock.Carrier);staleDock.LiveDockedTo=0;Run(staleDock,"qh:store:200");Assert.That(staleDock.Store().At(1,staleDock.P.steamId).Status=="STORED","stale reverse docking link replaced by live state");staleDock.Mod.Game_Exit();
        var wrongSteamReply=New();wrongSteamReply.WrongStateSteam=true;Run(wrongSteamReply,"qh:store:200");Assert.That(!wrongSteamReply.Calls.Contains(CmdId.Request_Entity_Export),"wrong reply Steam correlation rejected");wrongSteamReply.Mod.Game_Exit();
        var changedSteam=New();changedSteam.AfterPosition=()=>changedSteam.P.steamId="76561198000000002";Run(changedSteam,"qh:store:200");Assert.That(!changedSteam.Calls.Contains(CmdId.Request_Entity_Export)&&changedSteam.Logs.Any(x=>x.Contains("Session changee")),"host authenticates Steam change during live observation");changedSteam.Mod.Game_Exit();
        var changedClient=New();changedClient.AfterPosition=()=>changedClient.P.clientId=99;Run(changedClient,"qh:store:200");Assert.That(!changedClient.Calls.Contains(CmdId.Request_Entity_Export)&&changedClient.Logs.Any(x=>x.Contains("Session changee")),"host authenticates client change during live observation");changedClient.Mod.Game_Exit();
        var changedAfterDialog=New();changedAfterDialog.BeforeDialog=()=>changedAfterDialog.P.steamId="76561198000000002";Run(changedAfterDialog,"qh:store:200");Assert.That(!changedAfterDialog.Calls.Contains(CmdId.Request_Entity_Export),"Steam session change after confirmation rejected");changedAfterDialog.Mod.Game_Exit();
        var foreignOwner=New();foreignOwner.Ship.factionId=77;Run(foreignOwner,"qh:store:200");Assert.That(!foreignOwner.Calls.Contains(CmdId.Request_Entity_Export),"native foreign owner cannot authorize export when requester absent from native list");foreignOwner.Mod.Game_Exit();
        var reset=New();Run(reset,"qh:store:200");
        var resetEntry=reset.Store().List(reset.P.steamId).Single();
        var resetBytes=Snapshot(reset.Store().DirectoryFor(resetEntry));
        reset.P.entityId=51;reset.P.clientId=8;Restart(reset);
        Run(reset,"qh:list");
        Assert.That(reset.HeldDialog.MsgText.Contains("1 : Test SV") && reset.Store().List(reset.P.steamId).Count==1,"same Steam after character reset can list previous hangar");
        Unchanged(resetBytes,reset.Store().DirectoryFor(resetEntry),"character ID change and restart preserve archive bytes");
        Mark(reset);Run(reset,"qh:load:1");
        Assert.That(reset.Ships.Any(e=>e.id==900 && e.factionId==51 && e.factionGroup==1) && reset.Store().List(reset.P.steamId).Count==0,"same Steam restores previous ship to new character owner");
        reset.Mod.Game_Exit();
        var resetForeign=New();Run(resetForeign,"qh:store:200");
        resetForeign.P.entityId=51;resetForeign.P.clientId=8;resetForeign.P.steamId="76561198000000002";Restart(resetForeign);
        Mark(resetForeign);Run(resetForeign,"qh:load:1");
        Assert.That(!resetForeign.Calls.Contains(CmdId.Request_Entity_Spawn) && resetForeign.Store().List("76561198000000001").Count==1,"different Steam cannot recover another account after reset");
        resetForeign.Mod.Game_Exit();
        PlayerBetaTests();
        if(args.Length>1) QuarantineRegression(Path.GetFullPath(args[1]));
        Console.WriteLine("PASS "+Assert.Count+" assertions. Real game export contents, cargo, terrain, GUI and placement NOT simulated.");
    }
}
