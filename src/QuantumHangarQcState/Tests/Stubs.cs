using System; namespace RecycleTests {
public class StubIModApi : Eleon.Modding.IModApi {
 public virtual Eleon.Modding.IPlayfield ClientPlayfield { get; set; }
 public virtual Eleon.Modding.INetwork Network { get; set; }
 public virtual Eleon.Modding.IGui GUI { get; set; }
 public virtual Eleon.Modding.IPda PDA { get; set; }
 public virtual Eleon.Modding.IScript Scripting { get; set; }
 public virtual Eleon.Modding.ISoundPlayer SoundPlayer { get; set; }
 public virtual Eleon.Modding.IApplication Application { get; set; }
public event Eleon.Modding.GameEventDelegate GameEvent;
public void Raise_GameEvent(GameEventType p0,System.Object p1,System.Object p2,System.Object p3,System.Object p4,System.Object p5) { if(GameEvent!=null) GameEvent(p0,p1,p2,p3,p4,p5); }
public virtual void Log(System.String p0) {}
public virtual void LogWarning(System.String p0) {}
public virtual void LogError(System.String p0) {}
}
public class StubIApplication : Eleon.Modding.IApplication {
 public virtual Eleon.Modding.GameState State { get; set; }
 public virtual Eleon.Modding.ApplicationMode Mode { get; set; }
 public virtual Eleon.Modding.IPlayer LocalPlayer { get; set; }
 public virtual System.UInt64 GameTicks { get; set; }
public event Eleon.Modding.PlayfieldDelegate OnPlayfieldLoaded;
public void Raise_OnPlayfieldLoaded(Eleon.Modding.IPlayfield p0) { if(OnPlayfieldLoaded!=null) OnPlayfieldLoaded(p0); }
public event Eleon.Modding.PlayfieldDelegate OnPlayfieldUnloading;
public void Raise_OnPlayfieldUnloading(Eleon.Modding.IPlayfield p0) { if(OnPlayfieldUnloading!=null) OnPlayfieldUnloading(p0); }
public event Eleon.Modding.UpdateDelegate Update;
public void Raise_Update() { if(Update!=null) Update(); }
public event Eleon.Modding.UpdateDelegate FixedUpdate;
public void Raise_FixedUpdate() { if(FixedUpdate!=null) FixedUpdate(); }
public event Eleon.Modding.GamEnteredEventHandler GameEntered;
public void Raise_GameEntered(System.Boolean p0) { if(GameEntered!=null) GameEntered(p0); }
public event Eleon.Modding.ChatMessageSentEventHandler ChatMessageSent;
public void Raise_ChatMessageSent(Eleon.MessageData p0) { if(ChatMessageSent!=null) ChatMessageSent(p0); }
public virtual System.String GetPathFor(Eleon.Modding.AppFolder p0) {return default(System.String);}
public virtual Eleon.Modding.IPlayfieldDescr[] GetAllPlayfields() {return default(Eleon.Modding.IPlayfieldDescr[]);}
public virtual System.Collections.Generic.Dictionary<System.Int32,System.Collections.Generic.List<System.String>> GetPfServerInfos() {return default(System.Collections.Generic.Dictionary<System.Int32,System.Collections.Generic.List<System.String>>);}
public virtual System.Collections.Generic.IEnumerable<System.Int32> GetPlayerEntityIds() {return default(System.Collections.Generic.IEnumerable<System.Int32>);}
public virtual System.Nullable<Eleon.Modding.PlayerData> GetPlayerDataFor(System.Int32 p0) {return default(System.Nullable<Eleon.Modding.PlayerData>);}
public virtual void SendChatMessage(Eleon.MessageData p0) {}
public virtual System.Boolean ShowDialogBox(System.Int32 p0,Eleon.Modding.DialogConfig p1,Eleon.Modding.DialogActionHandler p2,System.Int32 p3) {return default(System.Boolean);}
public virtual System.Boolean GetStructure(System.Int32 p0,System.Action<Eleon.Modding.GlobalStructureInfo> p1) {return default(System.Boolean);}
public virtual System.Boolean GetStructures(System.String p0,System.Nullable<FactionData> p1,System.Nullable<EntityType> p2,System.Action<System.Collections.Generic.IEnumerable<Eleon.Modding.GlobalStructureInfo>> p3) {return default(System.Boolean);}
public virtual System.Collections.Generic.Dictionary<System.String,System.Int32> GetBlockAndItemMapping() {return default(System.Collections.Generic.Dictionary<System.String,System.Int32>);}
}
public class StubINetwork : Eleon.Modding.INetwork {
public virtual System.Boolean SendToDedicatedServer(System.String p0,System.Byte[] p1,System.String p2) {return default(System.Boolean);}
public virtual System.Boolean SendToPlayfieldServer(System.String p0,System.String p1,System.Byte[] p2) {return default(System.Boolean);}
public virtual System.Boolean SendToPlayer(System.String p0,System.Int32 p1,System.Byte[] p2) {return default(System.Boolean);}
public virtual System.Boolean RegisterReceiverForDediPackets(Eleon.Modding.ModDataReceivedDelegate p0) {return default(System.Boolean);}
public virtual System.Boolean RegisterReceiverForPlayfieldPackets(Eleon.Modding.ModDataReceivedDelegate p0) {return default(System.Boolean);}
public virtual System.Boolean RegisterReceiverForClientPackets(Eleon.Modding.PlayerDataReceivedDelegate p0) {return default(System.Boolean);}
}
public class StubIPlayfield : Eleon.Modding.IPlayfield {
 public virtual System.String Name { get; set; }
 public virtual System.String PlayfieldType { get; set; }
 public virtual System.String PlanetType { get; set; }
 public virtual System.String PlanetClass { get; set; }
 public virtual System.String SolarSystemName { get; set; }
 public virtual Eleon.Modding.VectorInt3 SolarSystemCoordinates { get; set; }
 public virtual System.Boolean IsPvP { get; set; }
 public virtual System.Collections.Generic.Dictionary<System.Int32,Eleon.Modding.IPlayer> Players { get; set; }
 public virtual System.Collections.Generic.Dictionary<System.Int32,Eleon.Modding.IEntity> Entities { get; set; }
public event Eleon.Modding.EntityDelegate OnEntityLoaded;
public void Raise_OnEntityLoaded(Eleon.Modding.IEntity p0) { if(OnEntityLoaded!=null) OnEntityLoaded(p0); }
public event Eleon.Modding.EntityDelegate OnEntityUnloaded;
public void Raise_OnEntityUnloaded(Eleon.Modding.IEntity p0) { if(OnEntityUnloaded!=null) OnEntityUnloaded(p0); }
public virtual System.Int32 SpawnEntity(System.String p0,UnityEngine.Vector3 p1,UnityEngine.Quaternion p2) {return default(System.Int32);}
public virtual System.Int32 SpawnPrefab(System.String p0,UnityEngine.Vector3 p1) {return default(System.Int32);}
public virtual void RemoveEntity(System.Int32 p0) {}
public virtual System.Boolean LockStructureDevice(System.Int32 p0,Eleon.Modding.VectorInt3 p1,System.Boolean p2,Eleon.Modding.LockResultCallback p3) {return default(System.Boolean);}
public virtual System.Boolean IsStructureDeviceLocked(System.Int32 p0,Eleon.Modding.VectorInt3 p1) {return default(System.Boolean);}
public virtual System.Int32 AddVoxelArea(UnityEngine.Vector3 p0,System.Int32 p1) {return default(System.Int32);}
public virtual System.Boolean MoveVoxelArea(System.Int32 p0,UnityEngine.Vector3 p1) {return default(System.Boolean);}
public virtual System.Boolean RemoveVoxelArea(System.Int32 p0) {return default(System.Boolean);}
public virtual System.Int32 SpawnTestPlayer(UnityEngine.Vector3 p0) {return default(System.Int32);}
public virtual System.Boolean RemoveTestPlayer(System.Int32 p0) {return default(System.Boolean);}
public virtual System.Single GetTerrainHeightAt(System.Single p0,System.Single p1) {return default(System.Single);}
}
public class StubIPlayer : Eleon.Modding.IPlayer {
 public virtual System.String SteamId { get; set; }
 public virtual System.String StartPlayfield { get; set; }
 public virtual System.Byte Origin { get; set; }
 public virtual FactionData FactionData { get; set; }
 public virtual FactionRole FactionRole { get; set; }
 public virtual System.Single Health { get; set; }
 public virtual System.Single HealthMax { get; set; }
 public virtual System.Single Oxygen { get; set; }
 public virtual System.Single OxygenMax { get; set; }
 public virtual System.Single Stamina { get; set; }
 public virtual System.Single StaminaMax { get; set; }
 public virtual System.Single Food { get; set; }
 public virtual System.Single FoodMax { get; set; }
 public virtual System.Single Radiation { get; set; }
 public virtual System.Single RadiationMax { get; set; }
 public virtual System.Single BodyTemp { get; set; }
 public virtual System.Single BodyTempMax { get; set; }
 public virtual System.Int32 Kills { get; set; }
 public virtual System.Int32 Died { get; set; }
 public virtual System.Double Credits { get; set; }
 public virtual System.Int32 ExperiencePoints { get; set; }
 public virtual System.Int32 UpgradePoints { get; set; }
 public virtual System.Int32 Ping { get; set; }
 public virtual Eleon.Modding.IStructure CurrentStructure { get; set; }
 public virtual Eleon.Modding.IEntity DrivingEntity { get; set; }
 public virtual System.Boolean IsPilot { get; set; }
 public virtual System.Int32 HomeBaseId { get; set; }
 public virtual System.String SteamOwnerId { get; set; }
 public virtual System.Int32 Permission { get; set; }
 public virtual System.Collections.Generic.List<Eleon.Modding.ItemStack> Toolbar { get; set; }
 public virtual System.Collections.Generic.List<Eleon.Modding.ItemStack> Bag { get; set; }
 public virtual System.Int32 Id { get; set; }
 public virtual System.String Name { get; set; }
 public virtual FactionData Faction { get; set; }
 public virtual UnityEngine.Vector3 Position { get; set; }
 public virtual UnityEngine.Vector3 Forward { get; set; }
 public virtual UnityEngine.Quaternion Rotation { get; set; }
 public virtual System.Boolean IsLocal { get; set; }
 public virtual System.Boolean IsProxy { get; set; }
 public virtual System.Boolean IsPoi { get; set; }
 public virtual System.Int32 BelongsTo { get; set; }
 public virtual System.Int32 DockedTo { get; set; }
 public virtual EntityType Type { get; set; }
 public virtual Eleon.Modding.IStructure Structure { get; set; }
public virtual System.Boolean Teleport(System.String p0,UnityEngine.Vector3 p1,UnityEngine.Vector3 p2) {return default(System.Boolean);}
public virtual System.Boolean Teleport(UnityEngine.Vector3 p0) {return default(System.Boolean);}
public virtual void DamageEntity(System.Int32 p0,System.Int32 p1) {}
public virtual void MoveForward(System.Single p0) {}
public virtual void Move(UnityEngine.Vector3 p0) {}
public virtual void MoveStop() {}
public virtual System.Boolean LoadFromDSL() {return default(System.Boolean);}
}
public class StubIEntity : Eleon.Modding.IEntity {
 public virtual System.Int32 Id { get; set; }
 public virtual System.String Name { get; set; }
 public virtual FactionData Faction { get; set; }
 public virtual UnityEngine.Vector3 Position { get; set; }
 public virtual UnityEngine.Vector3 Forward { get; set; }
 public virtual UnityEngine.Quaternion Rotation { get; set; }
 public virtual System.Boolean IsLocal { get; set; }
 public virtual System.Boolean IsProxy { get; set; }
 public virtual System.Boolean IsPoi { get; set; }
 public virtual System.Int32 BelongsTo { get; set; }
 public virtual System.Int32 DockedTo { get; set; }
 public virtual EntityType Type { get; set; }
 public virtual Eleon.Modding.IStructure Structure { get; set; }
public virtual void DamageEntity(System.Int32 p0,System.Int32 p1) {}
public virtual void MoveForward(System.Single p0) {}
public virtual void Move(UnityEngine.Vector3 p0) {}
public virtual void MoveStop() {}
public virtual System.Boolean LoadFromDSL() {return default(System.Boolean);}
}
public class StubIStructure : Eleon.Modding.IStructure {
public virtual void SetColorOfBlocks(System.Collections.Generic.List<Eleon.Modding.BlockPosColor> blocks, BlockSide side) {}
 public virtual Eleon.Modding.VectorInt3 MinPos { get; set; }
 public virtual Eleon.Modding.VectorInt3 MaxPos { get; set; }
 public virtual System.Int32 Id { get; set; }
 public virtual System.Boolean IsReady { get; set; }
 public virtual System.Boolean IsPowered { get; set; }
 public virtual System.Boolean IsOfflineProtectable { get; set; }
 public virtual System.Single DamageLevel { get; set; }
 public virtual System.Int32 BlockCount { get; set; }
 public virtual System.Int32 DeviceCount { get; set; }
 public virtual System.Int32 LightCount { get; set; }
 public virtual System.Int32 TriangleCount { get; set; }
 public virtual System.Single Fuel { get; set; }
 public virtual System.Int32 PowerOutCapacity { get; set; }
 public virtual System.Int32 PowerConsumption { get; set; }
 public virtual System.String PlayerCreatedSteamId { get; set; }
 public virtual CoreType CoreType { get; set; }
 public virtual System.Int32 SizeClass { get; set; }
 public virtual System.Boolean IsShieldActive { get; set; }
 public virtual System.Int32 ShieldLevel { get; set; }
 public virtual System.Single TotalMass { get; set; }
 public virtual System.Boolean HasLandClaimDevice { get; set; }
 public virtual System.UInt64 LastVisitedTicks { get; set; }
 public virtual Eleon.Modding.IStructureTank FuelTank { get; set; }
 public virtual Eleon.Modding.IStructureTank OxygenTank { get; set; }
 public virtual Eleon.Modding.IStructureTank PentaxidTank { get; set; }
 public virtual Eleon.Modding.IEntity Entity { get; set; }
 public virtual Eleon.Modding.IPlayer Pilot { get; set; }
public event Eleon.Modding.SignalChangedEventHandler SignalChanged;
public void Raise_SignalChanged(System.String p0,System.Boolean p1,System.Int32 p2) { if(SignalChanged!=null) SignalChanged(p0,p1,p2); }
public virtual System.String[] GetAllCustomDeviceNames() {return default(System.String[]);}
public virtual System.Collections.Generic.List<Eleon.Modding.VectorInt3> GetDevicePositions(System.String p0) {return default(System.Collections.Generic.List<Eleon.Modding.VectorInt3>);}
public virtual Eleon.Modding.IDevicePosList GetDevices(DeviceTypeName p0) {return default(Eleon.Modding.IDevicePosList);}
public virtual T GetDevice<T>(System.Int32 p0,System.Int32 p1,System.Int32 p2) where T : class, Eleon.Modding.IDevice {return default(T);}
public virtual T GetDevice<T>(System.String p0) where T : class, Eleon.Modding.IDevice {return default(T);}
public virtual T GetDevice<T>(Eleon.Modding.VectorInt3 p0) where T : class, Eleon.Modding.IDevice {return default(T);}
public virtual T GetDevice<T>(UnityEngine.Vector3 p0) where T : class, Eleon.Modding.IDevice {return default(T);}
public virtual Eleon.Modding.IBlock GetBlock(Eleon.Modding.VectorInt3 p0) {return default(Eleon.Modding.IBlock);}
public virtual Eleon.Modding.IBlock GetBlock(System.Int32 p0,System.Int32 p1,System.Int32 p2) {return default(Eleon.Modding.IBlock);}
public virtual void SetFaction(FactionGroup p0,System.Int32 p1) {}
public virtual System.Collections.Generic.List<Eleon.Modding.IStructure> GetDockedVessels() {return default(System.Collections.Generic.List<Eleon.Modding.IStructure>);}
public virtual System.Collections.Generic.List<Eleon.Modding.IPlayer> GetPassengers() {return default(System.Collections.Generic.List<Eleon.Modding.IPlayer>);}
public virtual System.Collections.Generic.List<Eleon.Modding.SenderSignal> GetBlockSignals(System.String p0) {return default(System.Collections.Generic.List<Eleon.Modding.SenderSignal>);}
public virtual System.Collections.Generic.List<Eleon.Modding.SenderSignal> GetControlPanelSignals() {return default(System.Collections.Generic.List<Eleon.Modding.SenderSignal>);}
public virtual System.Collections.Generic.List<Eleon.Modding.SignalFunction> GetSignalReceivers(System.String p0) {return default(System.Collections.Generic.List<Eleon.Modding.SignalFunction>);}
public virtual System.Boolean GetSignalState(System.String p0) {return default(System.Boolean);}
public virtual System.String GetSendSignalName(Eleon.Modding.VectorInt3 p0) {return default(System.String);}
public virtual UnityEngine.Vector3 StructToGlobalPos(Eleon.Modding.VectorInt3 p0) {return default(UnityEngine.Vector3);}
public virtual Eleon.Modding.VectorInt3 GlobalToStructPos(UnityEngine.Vector3 p0) {return default(Eleon.Modding.VectorInt3);}
}
public class StubIBlock : Eleon.Modding.IBlock {
 public virtual Eleon.Modding.IBlock ParentBlock { get; set; }
 public virtual System.Nullable<System.Int32> LockCode { get; set; }
 public virtual System.String CustomName { get; set; }
public virtual void Set(System.Nullable<System.Int32> p0,System.Nullable<System.Int32> p1,System.Nullable<System.Int32> p2,System.Nullable<System.Boolean> p3) {}
public virtual void Get(out System.Int32 p0,out System.Int32 p1,out System.Int32 p2,out System.Boolean p3) {p0=default(System.Int32);p1=default(System.Int32);p2=default(System.Int32);p3=default(System.Boolean);}
public virtual System.Int32 GetDamage() {return default(System.Int32);}
public virtual void SetDamage(System.Int32 p0) {}
public virtual System.Int32 GetHitPoints() {return default(System.Int32);}
public virtual void GetTextures(out System.Int32 p0,out System.Int32 p1,out System.Int32 p2,out System.Int32 p3,out System.Int32 p4,out System.Int32 p5) {p0=default(System.Int32);p1=default(System.Int32);p2=default(System.Int32);p3=default(System.Int32);p4=default(System.Int32);p5=default(System.Int32);}
public virtual void SetTextures(System.Nullable<System.Int32> p0,System.Nullable<System.Int32> p1,System.Nullable<System.Int32> p2,System.Nullable<System.Int32> p3,System.Nullable<System.Int32> p4,System.Nullable<System.Int32> p5) {}
public virtual void SetTextureForWholeBlock(System.Int32 p0) {}
public virtual void GetColors(out System.Int32 p0,out System.Int32 p1,out System.Int32 p2,out System.Int32 p3,out System.Int32 p4,out System.Int32 p5) {p0=default(System.Int32);p1=default(System.Int32);p2=default(System.Int32);p3=default(System.Int32);p4=default(System.Int32);p5=default(System.Int32);}
public virtual void SetColors(System.Nullable<System.Int32> p0,System.Nullable<System.Int32> p1,System.Nullable<System.Int32> p2,System.Nullable<System.Int32> p3,System.Nullable<System.Int32> p4,System.Nullable<System.Int32> p5) {}
public virtual void SetColorForWholeBlock(System.Int32 p0) {}
public virtual System.Nullable<System.Boolean> GetSwitchState(System.Int32 p0) {return default(System.Nullable<System.Boolean>);}
public virtual System.Nullable<System.Boolean> SetSwitchState(System.Boolean p0,System.Int32 p1) {return default(System.Nullable<System.Boolean>);}
}
public class StubIContainer : Eleon.Modding.IContainer {
 public virtual System.Single VolumeCapacity { get; set; }
 public virtual System.Single DecayFactor { get; set; }
public virtual void Clear() {}
public virtual System.Boolean Contains(System.Int32 p0) {return default(System.Boolean);}
public virtual System.Int32 GetTotalItems(System.Int32 p0) {return default(System.Int32);}
public virtual System.Int32 AddItems(System.Int32 p0,System.Int32 p1) {return default(System.Int32);}
public virtual System.Int32 RemoveItems(System.Int32 p0,System.Int32 p1) {return default(System.Int32);}
public virtual System.Collections.Generic.List<Eleon.Modding.ItemStack> GetContent() {return default(System.Collections.Generic.List<Eleon.Modding.ItemStack>);}
public virtual void SetContent(System.Collections.Generic.List<Eleon.Modding.ItemStack> p0) {}
}
public class StubIStructureTank : Eleon.Modding.IStructureTank {
 public virtual System.Single Capacity { get; set; }
 public virtual System.Single Content { get; set; }
 public virtual System.Boolean UsesIntegerAmounts { get; set; }
public virtual void AddContent(System.Single p0) {}
}
}
