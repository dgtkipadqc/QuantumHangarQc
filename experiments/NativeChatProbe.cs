using System;
using Eleon;
using Eleon.Modding;
// Laboratory helper ONLY. Not compiled into either production mod or update ZIP.
// Requires an authenticated server-side player ID and additive scenario keys.
// Never call for a player with a manual QH override: native resolution ignores it.
public static class NativeChatProbe {
 public const string Key="QH_QC_LOCALE_PROBE_R1";
 public static MessageData Create(int authenticatedPlayer,string first,string second,bool manualOverride) {
  if(authenticatedPlayer<=0||manualOverride||first==null||second==null||first.Length>80||second.Length>80)throw new ArgumentException("probe_input");
  return new MessageData{Channel=Eleon.MsgChannel.SinglePlayer,RecipientEntityId=authenticatedPlayer,SenderNameOverride="QH probe",SenderType=Eleon.SenderType.ServerPrio,IsTextLocaKey=true,Text=Key,Arg1=first,Arg2=second};
 }
 public static void Send(IApplication application,int authenticatedPlayer,string first,string second,bool manualOverride) {
  application.SendChatMessage(Create(authenticatedPlayer,first,second,manualOverride));
 }
}
