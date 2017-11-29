using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Trinity.Components.Swarm.Internals;

namespace Trinity.Components.Swarm
{
    public class SwarmClient<T> : IDisposable where T : class
    {
        public ConcurrentDictionary<RemoteClient, T> Swarm { get; } = new ConcurrentDictionary<RemoteClient, T>();
        public delegate void DebugMessageDelegate(string message);
        public event DebugMessageDelegate DebugMessage;
        public delegate void MessageDelegate(CommunicationMessage message, RemoteClient client, T data);
        public event MessageDelegate MessageRecieved;
        public delegate void ClientChangeDelegate(RemoteClient client, T data);
        public event ClientChangeDelegate ClientExpired;
        public event ClientChangeDelegate ClientAdded;
        public event ClientChangeDelegate ClientChanged;
        public delegate void ClientsUpdatedDelegate();
        public event ClientsUpdatedDelegate ClientsUpdated;
        private CommunicationCoordinator _coordinator;
    
        public SwarmClient(Uri addr, string pipeName = null)
        {
            _coordinator = new CommunicationCoordinator(CommunicationMethod.Tcp, addr, pipeName);
            _coordinator.DebugMessage += msg => DebugMessage?.Invoke(msg);
            _coordinator.ClientDevice.MessageReceived += HandleMessageReceived;    
            _coordinator.ClientDevice.ResponseReceived += HandleResponseReceived;            
            _coordinator.ClientsUpdated += UpdatedClients;
        }

        /// <summary>
        /// Update wrapper of client list since we're storing last T received per client.
        /// </summary>
        private void UpdatedClients(List<RemoteClient> clients)
        {
            foreach (var client in clients)
            {
                UpdateClient(client, null);
            }            
        }

        private void HandleResponseReceived(CommunicationMessage message)
        {

        }

        public Identity Id => _coordinator.ClientDevice.Identity;
        public string ClientAddress => _coordinator.ClientDevice.Uri;
        public string HostAddress => _coordinator.HostDevice.Uri;

        /// <summary>
        /// Event handler for when each message arrives.
        /// Converts the raw input into T and bubbles it to 'MessageRecieved'
        /// </summary>
        private void HandleMessageReceived(CommunicationMessage message)
        {
            var data = message.GetValue<T>();
            if (data == null)
                return;

            var remoteClient = FindClientForMessage(message);
            UpdateClient(remoteClient, data);
            MessageRecieved?.Invoke(message, remoteClient, data);
        }

        private RemoteClient FindClientForMessage(CommunicationMessage message)
        {
            return _coordinator.Clients.FirstOrDefault(c => c.Identity.Equals(message.From)) 
                   ?? new RemoteClient(message.From, message);
        }

        private void UpdateClient(RemoteClient remoteClient, T data)
        {            
            var found = false;
            foreach (var entry in Swarm)
            {
                if (entry.Key.Equals(remoteClient))
                {
                    entry.Key.State = remoteClient.State;
                    Swarm.TryUpdate(entry.Key, data, entry.Value);
                    ClientChanged?.Invoke(remoteClient, entry.Value);
                    found = true;
                }
                if (DateTime.UtcNow.Subtract(entry.Key.LastMessage.Sent) > _coordinator.ClientExpiryTime)
                { 
                    T removed;
                    if (Swarm.TryRemove(entry.Key, out removed))
                    {
                        ClientExpired?.Invoke(remoteClient, removed);
                    }
                }
            }
            if (!found)
            {
                Swarm.TryAdd(remoteClient, data);
                ClientAdded?.Invoke(remoteClient, data);
            }

            ClientsUpdated?.Invoke();
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        public void SendMessage(T data, RemoteClient to = null, Action<CallbackArgs> responseHandler = null)
        {
            var msg = new CommunicationMessage
            {
                From = _coordinator.ClientDevice.Identity                
            };
            msg.SetValue(data);
            _coordinator.SendMessage(msg, to.Identity, responseHandler);
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        public void BroadcastMessage(T data, Action<CallbackArgs> responseHandler = null)
        {
            var msg = new CommunicationMessage
            {
                From = _coordinator.ClientDevice.Identity
            };
            msg.SetValue(data);
            _coordinator.SendMessage(msg, responseHandler);
        }

        /// <summary>
        /// Begin sending/receiving communications.
        /// </summary>
        public bool Start() => _coordinator.Start();

        /// <summary>
        /// Stop communicating and destroy all resources.
        /// </summary>
        public void Shutdown()
        {
            _coordinator?.Shutdown();
            _coordinator = null;
        }

        public bool IsRunning => _coordinator != null && (_coordinator.CurrentStatus.IsOperational || _coordinator.CurrentStatus.IsConnected);

        public void Dispose() => Shutdown();
        public void Update() => _coordinator.Sync();    
            
    }
}

