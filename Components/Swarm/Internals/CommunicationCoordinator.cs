using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Trinity.Components.Swarm.Internals
{
    public enum CommunicationMethod
    {
        None = 0,
        Tcp,
        Pipe,
    }

    public class CommunicationCoordinator : IDisposable
    {
        public const string DefaultPipeName = "DefaultCoordinator";

        private Identity _hostId;
        private Identity _clientId;

        private Dictionary<Guid, PendingCallback> _callbacks = new Dictionary<Guid, PendingCallback>();
        private Queue<QueuedAction> _actionQueue = new Queue<QueuedAction>();
        private List<RemoteClient> _hostClientsList = new List<RemoteClient>();

        public CommunicationCoordinator(CommunicationMethod method, Uri hostAddr = null, string channel = "")
        {
            Channel = string.IsNullOrEmpty(channel) ? DefaultPipeName : channel;

            var localAddr = IpHelper.AddressToUri(IpHelper.GetMachineAddress(), false, 8081);
            if (hostAddr == null)
                hostAddr = localAddr;

            var proc = Process.GetCurrentProcess();
            CreateHost(method, hostAddr, proc);
            CreateClient(method, localAddr, proc);
        }

        private void CreateHost(CommunicationMethod method, Uri hostAddr, Process proc)
        {
            _hostId = new Identity(proc.Id, "Host", Channel, IdentityRole.Host);

            switch (method)
            {
                case CommunicationMethod.Tcp:
                    HostDevice = new HttpCommunicationDevice(_hostId, hostAddr);
                    break;
                default:
                    HostDevice = new HttpCommunicationDevice(_hostId, hostAddr);
                    break;
            }

            HostDevice.MessageReceived += ReceivedMessage;
            _hostId.Address = HostDevice.Uri;
        }

        private void CreateClient(CommunicationMethod method, Uri localAddr, Process proc)
        {
            _clientId = new Identity(proc.Id, "Client-" + proc.Id, "Client-" + Channel + proc.Id, IdentityRole.Client);

            switch (method)
            {
                case CommunicationMethod.Tcp:
                    ClientDevice = new HttpCommunicationDevice(_clientId, localAddr);
                    break;
                default:
                    ClientDevice = new NamedPipeCommunicationDevice(_clientId, localAddr);
                    break;
            }

            ClientDevice.ResponseReceived += ReceivedResponse;
            ClientDevice.MessageReceived += ReceivedMessage;
            _clientId.Address = ClientDevice.Uri;
        }

        public delegate void DebugMessageDelegate(string message);
        public event DebugMessageDelegate DebugMessage;
        public delegate void ClientsUpdatedDelegate(List<RemoteClient> clients);
        public ClientsUpdatedDelegate ClientsUpdated = null;

        public bool IsMe(Identity id) => id.Equals(_clientId) || id.Equals(_hostId);
        public bool IsMeAsHost(Identity id) => id.Equals(_hostId);
        public bool IsMeAsClient(Identity id) => id.Equals(_clientId);

        public string Channel { get; }
        public Status CurrentStatus { get; } = new Status();
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan ClientInactiveTime { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan ClientExpiryTime { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan HostPortCheckInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan ClientListUpdateInterval { get; set; } = TimeSpan.FromSeconds(1);
        public List<RemoteClient> Clients { get; set; } = new List<RemoteClient>();
        public CommunicationDevice ClientDevice { get; set; }
        public CommunicationDevice HostDevice { get; set; }

        public bool Start()
        {
            ClientDevice.Start();
            HostDevice.Start();
            Sync();

            var result = ClientDevice.IsOperational;
            CurrentStatus.IsOperational = result;
            return result;
        }

        public void Shutdown()
        {
            ClientDevice.Stop();
            HostDevice.Stop();
            CurrentStatus.IsOperational = false;
        }

        /// <summary>
        /// Connection status
        /// </summary>
        public class Status
        {
            public bool IsConnected { get; set; }
            public bool IsHost { get; set; }
            public DateTime LastResponseReceived { get; set; }
            public DateTime LastSendTime { get; set; }
            public bool IsOperational { get; set; }
            public DateTime LastSyncTime { get; set; }
            public DateTime LastHostAddressOpenAttempt { get; set; }
            public DateTime LastUpdatedClients { get; set; }
        }

        /// <summary>
        /// Represents some code to be executed when a response is received
        /// </summary>
        public class PendingCallback
        {
            public Guid Guid { get; set; }
            public Action<CallbackArgs> Callback { get; set; }
            public DateTime Created { get; set; }
        }

        /// <summary>
        /// A wrapper to store functions to be fired at a later time
        /// </summary>
        public class QueuedAction
        {
            public QueuedAction(Action<CallbackArgs> callback = null)
            {
                Callback = callback;
            }
            public Action<CallbackArgs> Callback { get; }
        }


        private void ReceivedMessage(CommunicationMessage message)
        {
            if (!IsMe(message.To))
            {
                DebugMessage?.Invoke($"Received an invalid message: addressed to someone other than me - to: {message.To.Name}, {message.Type}, from {message.From.Name})");
                return;
            }

            DebugMessage?.Invoke($"{message.To.Name} << Received ({message.Type}) from {message.From.Name}");

            if (message.To.Role == IdentityRole.Host && !message.From.Equals(_hostId))
            {
                UpdateHostClientList(message.From, message);       
            }
            else
            {
                RequestClientListFromHost();
            }

            SendResponse(message.To, message, _hostClientsList);           
        }

        /// <summary>
        /// Keep track of who sent a message
        /// </summary>        
        private void UpdateHostClientList(Identity id, CommunicationMessage message = null)
        {
            var found = false;
            foreach (var client in _hostClientsList.ToList())
            {
                if (client.Identity.Equals(id))
                {
                    found = true;
                    client.State = ClientState.Active;                    
                    client.LastMessage = message;
                }
                else if (DateTime.UtcNow.Subtract(client.LastMessage.Received) > ClientExpiryTime)
                {
                    _hostClientsList.Remove(client);
                }
                else if (DateTime.UtcNow.Subtract(client.LastMessage.Received) > ClientInactiveTime)
                {
                    client.State = ClientState.Inactive;
                } 
            }
            if (!found)
            {
                DebugMessage?.Invoke($"{_hostId.Name}: Adding new client information - {id}");
                _hostClientsList.Add(new RemoteClient(id, message));
            }
        }

        private void SendResponse(Identity from, CommunicationMessage message, List<RemoteClient> clientList = null)
        {
            var msg = new CommunicationMessage
            {
                From = from,
                CallbackId = message.CallbackId,
                Type = MessageType.Response
            };
            msg.SetValue(new ResponseMessage
            {
                From = _clientId,
                Clients = clientList ?? new List<RemoteClient>(),
                ResponseText = "Acknowledged",
                Result = true
            });
            SendMessageInternal(msg, message.From);
        }

        /// <summary>
        /// Send message to all clients.
        /// </summary>
        public void SendMessage(CommunicationMessage message, Action<CallbackArgs> responseHandler = null)
        {
            if (TryValidateConnection(message, null, args => SendBroadcastInternal(message, responseHandler)))
                return;
    
            SendBroadcastInternal(message, responseHandler);
        }

        private void SendBroadcastInternal(CommunicationMessage message, Action<CallbackArgs> responseHandler)
        {
            foreach (var client in Clients)
            {
                SendMessage(message, client.Identity, responseHandler);
            }
        }

        /// <summary>
        /// Send a message to a specific client.
        /// </summary>
        public void SendMessage(CommunicationMessage message, Identity to, Action<CallbackArgs> responseHandler = null)
        {
            if (TryValidateConnection(message, to, responseHandler))
                return;

            AddCallback(message, responseHandler);
            SendMessageInternal(message, to);
        }

        private bool TryValidateConnection(CommunicationMessage message, Identity to, Action<CallbackArgs> action)
        {
            if (DateTime.UtcNow.Subtract(CurrentStatus.LastResponseReceived) > SyncInterval)
            {
                if (to != null)
                {
                    _actionQueue.Enqueue(new QueuedAction(args => SendMessage(message, to, action)));
                }
                else if (action != null)
                {
                    _actionQueue.Enqueue(new QueuedAction(action));
                }
                Sync();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Make sure a valid host exists or become a host if nessesary
        /// </summary>
        public void Sync()
        {
            if (DateTime.UtcNow.Subtract(CurrentStatus.LastSyncTime) < SyncInterval)
                return;

            CurrentStatus.LastSyncTime = DateTime.UtcNow;

            if (!HostDevice.IsStarted)
            {
                if (DateTime.UtcNow.Subtract(CurrentStatus.LastHostAddressOpenAttempt) > HostPortCheckInterval)
                {
                    CurrentStatus.LastHostAddressOpenAttempt = DateTime.UtcNow;

                    var startResult = HostDevice.Start();
                    if (startResult == ConnectionResult.Connected)
                    {
                        CurrentStatus.IsConnected = true;
                        CurrentStatus.IsHost = true;
                        _hostClientsList = Clients;
                    }
                    else if (startResult != ConnectionResult.Failed)
                    {
                        // Assume if address is in use that a host is operational elsewhere.                      
                        CurrentStatus.IsConnected = true;
                        CurrentStatus.IsHost = false;
                    }
                }
            }
            else
            {
                if (!HostDevice.IsOperational)
                {
                    CurrentStatus.IsConnected = false;
                    CurrentStatus.IsHost = false;
                    CloseHost();
                }
            }
            Ping();
        }

        private void CloseHost()
        {
            DebugMessage?.Invoke($"Closing Host");
            HostDevice?.Stop();
        }

        #region Ping

        /// <summary>
        /// Send a message on the host channel to see if we get a response.
        /// </summary>
        public void Ping()
        {
            var msg = new CommunicationMessage
            {
                Name = "Ping",
                From = _clientId
            };
            AddCallback(msg, HandlePingSuccess);
            SendMessageInternal(msg);
            DebugMessage?.Invoke($"Ping...");
        }

        /// <summary>
        /// Event handler that occurs when we recieve a response from the host to our Ping().
        /// </summary>
        private void HandlePingSuccess(CallbackArgs obj)
        {
            DebugMessage?.Invoke($"Pong.. ({obj.OriginalMessage.TransmissionTime.TotalSeconds}s)");

            CurrentStatus.IsConnected = true;
            CurrentStatus.IsHost = false;
            CurrentStatus.LastResponseReceived = DateTime.UtcNow;

            if (_actionQueue.Any())
            {
                DebugMessage?.Invoke($"{_clientId.Name}: Sending Queued Messages ({_actionQueue.Count})");

                while (_actionQueue.Count > 0 && CurrentStatus.IsConnected)
                {
                    var message = _actionQueue.Dequeue();
                    message?.Callback(obj);
                }
            }
        }

        #endregion


        public void RequestClientListFromHost()
        {
            if (DateTime.UtcNow.Subtract(CurrentStatus.LastUpdatedClients) < ClientListUpdateInterval)
                return;

            CurrentStatus.LastUpdatedClients = DateTime.UtcNow;     
            SendMessageInternal(new CommunicationMessage
            {
                Name = "GetUpdatedClientList",
                From = _clientId
            });
        }

        /// <summary>
        /// Prepare a function and identify it with an ID in the message, 
        /// so that it can be executed later when the host responds.
        /// </summary>
        private void AddCallback(CommunicationMessage message, Action<CallbackArgs> responseHandler)
        {
            if (responseHandler != null && message != null)
            {
                var pid = Guid.NewGuid();
                _callbacks.Add(pid, new PendingCallback
                {
                    Guid = pid,
                    Created = DateTime.UtcNow,
                    Callback = responseHandler
                });
                message.CallbackId = pid;
            }
        }



        /// <summary>
        /// Actually send a message.
        /// </summary>
        private void SendMessageInternal(CommunicationMessage message, Identity to = null)
        {
            try
            {
                var senderId = message.From.Equals(ClientDevice.Identity) ? message.From : _hostId;
                var receiverId = to ?? _hostId;
                //var uri = to?.Role == IdentityRole.Client && !to.Equals(HostDevice.Identity) ? HostDevice.GetUri(to) : HostDevice.Uri;
                var uri = to?.Address ?? HostDevice.Uri;
                var binding = HostDevice.CreateBinding();
                var proxy = ChannelFactory<ITransmissionService>.CreateChannel(binding, new EndpointAddress(uri));

                message.Sent = DateTime.UtcNow;
                message.To = receiverId;                

                Task.Run(() =>
                {
                    DebugMessage?.Invoke($"{senderId.Name} >> Sending {message.Name} ({message.Type}) to {receiverId.Name}");
                    CurrentStatus.LastSendTime = DateTime.UtcNow;
                    try
                    {
                        proxy.SendData(TransmissionService.PrepareMessage(message));
                    }
                    //catch (EndpointNotFoundException ex)
                    catch (Exception ex)
                    {
                        DebugMessage?.Invoke($"Exception: {ex.GetType().Name} {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Exception: {ex.GetType().Name} {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler that occurs whenever we recieve a response from the host.
        /// Executes the associated callback if nessesary.
        /// </summary>
        private void ReceivedResponse(CommunicationMessage message)
        {
            if (!IsMe(message.To))
            {
                DebugMessage?.Invoke($"Received an invalid response: addressed to someone other than me - to: {message.To.Name}, {message.Type}, from {message.From.Name})");
                return;
            }

            CurrentStatus.LastResponseReceived = DateTime.UtcNow;

            var responseData = message.GetValue<ResponseMessage>();

            DebugMessage?.Invoke($"{message.To.Name} << Received ({message.Type}) from {message.From.Name}: {responseData.ResponseText}");

            switch (message.From.Role)
            {
                case IdentityRole.Host:
                    if (responseData.Clients.Any())
                        UpdateClientList(responseData);
                    break;

                case IdentityRole.Client:
                    RequestClientListFromHost();
                    break;
            }

            FireCallbacks(message, responseData);
        }

        private void FireCallbacks(CommunicationMessage message, ResponseMessage responseData)
        {
            if (_callbacks.ContainsKey(message.CallbackId))
            {
                var args = new CallbackArgs
                {
                    OriginalMessage = message,
                    Response = responseData
                };
                _callbacks[message.CallbackId]?.Callback.Invoke(args);
                _callbacks.Remove(message.CallbackId);
            }
        }

        private void UpdateClientList(ResponseMessage responseData)
        {
            CurrentStatus.LastUpdatedClients = DateTime.UtcNow;
            Clients = responseData.Clients;
            ClientsUpdated?.Invoke(Clients);
            DebugMessage?.Invoke($"{_clientId.Name}: Client List Updated ({Clients.Count})");
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}