using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Eleon.Modding;

namespace QuantumHangarQc {
    // Ask the client on THIS host's existing pipes. Never guess from directory names,
    // timestamps, another mod's settings or a shared machine-wide descriptor.
    internal static class ActiveSavePath {
        internal static string Resolve(ModGameAPI api, int timeout=20000) {
            var host=api.GetType();
            var handlersProperty=host.GetProperty("InServerMessageHandler");
            var pipeProperty=host.GetProperty("ToEmpyrion");
            if(handlersProperty==null || pipeProperty==null)
                throw new InvalidOperationException("Active save detection requires the bundled EmpyrionModHost");
            var handlers=(Dictionary<Type,Action<object>>)handlersProperty.GetValue(api,null);
            var pipe=pipeProperty.GetValue(api,null);
            var assembly=Assembly.Load("ModExtenderCommunication");
            var messageType=assembly.GetType("ModExtenderCommunication.ModComData",true);
            var commandType=assembly.GetType("ModExtenderCommunication.ModCommand",true);
            var command=messageType.GetProperty("Command");
            var sequence=messageType.GetProperty("SequenceId");
            var data=messageType.GetProperty("Data");
            var token=Guid.NewGuid();
            var response=new TaskCompletionSource<string>();
            Action<object> previous;
            lock(handlers) {
                handlers.TryGetValue(messageType,out previous);
                // Keep the router alive: delayed responses must never reach a disposed wait handle.
                handlers[messageType]=value=> {
                    if((Guid)sequence.GetValue(value,null)==token && command.GetValue(value,null).ToString()=="GetPathFor")
                        response.TrySetResult(data.GetValue(value,null) as string);
                    else if(previous!=null) previous(value);
                };
            }
            var request=Activator.CreateInstance(messageType);
            command.SetValue(request,Enum.Parse(commandType,"GetPathFor"),null);
            sequence.SetValue(request,token,null);
            // AppFolder.SaveGame = 2 in ModApi. A boxed Int32 can be unboxed as
            // the Int32-backed enum by the client, without loading ModApi in Host.
            data.SetValue(request,2,null);
            pipe.GetType().GetMethod("SendMessage",new[]{typeof(object)}).Invoke(pipe,new[]{request});
            if(!response.Task.Wait(timeout)) throw new TimeoutException("Active save path request timed out; QH disabled");
            var path=response.Task.Result;
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||!Directory.Exists(path))
                throw new InvalidDataException("Client returned an invalid active save path; QH disabled");
            return Path.GetFullPath(path);
        }
    }
}
