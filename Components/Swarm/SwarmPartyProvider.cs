using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Demonbuddy;
using Trinity.Components.Combat;
using Trinity.Components.Combat.Resources;
using Trinity.Components.Swarm.Internals;
using Trinity.Framework;
using Trinity.Framework.Helpers;
using Trinity.Framework.Objects;
using Trinity.UI.Visualizer.RadarCanvas;
using Zeta.Common;

namespace Trinity.Components.Swarm
{
    public class SwarmPartyProvider : Module, IPartyProvider
    {
        private SwarmClient<SwarmPlayer> _network;

        public Identity Identity => _network?.Id;

        public bool Start(Uri addr = default(Uri))
        {
            try
            {
                Stop();

                _network = new SwarmClient<SwarmPlayer>(addr, "Group1");
                _network.MessageRecieved += NetworkOnMessageRecieved;
                _network.ClientsUpdated += NetworkOnClientsUpdated;
            
                if (_network.Start())
                {
                    Core.Logger.Log(LogCategory.Party, "Connected to Swarm");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Log(LogCategory.Party, $"Failed to connect to Swarm. { ex.GetType().Name}: { ex.Message}");
            }
            return false;
        }

        private void NetworkOnClientsUpdated()
        {
            Core.Logger.Log(LogCategory.Party, $"Swarm Changed: {_network.Swarm.Count(s => s.Key.State == ClientState.Active)} clients are connected");
        }

        private void NetworkOnMessageRecieved(CommunicationMessage message, RemoteClient client, SwarmPlayer payload)
        {
            Core.Logger.Verbose(LogCategory.Party, $"Update received from {client.Identity}, {payload}");
        }

        public Dictionary<RemoteClient, SwarmPlayer> Clients = new Dictionary<RemoteClient, SwarmPlayer>();

        public SwarmPlayer GetCurrentPlayer()
        {
            var player = new SwarmPlayer();
            player.Update();
            return player;
        }

        public void Stop()
        {
            if (_network == null) return;
            _network.MessageRecieved -= NetworkOnMessageRecieved;
            _network.ClientsUpdated -= NetworkOnClientsUpdated;
            _network?.Shutdown();
        }

        protected override int UpdateIntervalMs => 250;

        protected override void OnPulse()
        {
            if (!Core.TrinityIsReady)
                return;

            if (Core.TrinityIsReady && (_network == null || !_network.IsRunning))
            {
                Start();
            }
            else
            {
                if (!(TrinityCombat.Party is SwarmPartyProvider))
                {
                    TrinityCombat.Party = this;
                }
            }

            _network?.Update();

            SendPlayerData();

            //RadarDebug.DrawElipse(Members.Select(m => m.Position), 250, RadarDebug.DrawColor.Blue);
        }

        public void SendPlayerData(int toClientId = 0)
        {
            if (_network == null)
                return;

            if (toClientId <= 0)
            {
                Task.Run(() => _network.BroadcastMessage(GetCurrentPlayer(), ResponseHandler));
                return;
            }

            var remoteClient = _network?.Swarm?.Keys.FirstOrDefault(c => c.Identity.Id == toClientId);
            if (remoteClient != null && toClientId > 0)
            {
                Task.Run(() => _network.SendMessage(GetCurrentPlayer(), remoteClient, ResponseHandler));
            }
        }

        private void ResponseHandler(CallbackArgs message)
        {
            Core.Logger.Verbose(LogCategory.Party, $"Update received from {message.Response.From}: {message.Response.ResponseText}");
        }

        #region IPartyProvider

        public IEnumerable<IPartyMember> Members => _network.Swarm.Values.ToList();
        public IEnumerable<IPartyMember> Followers => Members.Where(m => m.IsFollower);
        public IPartyMember Leader => null;
        public ITargetable PriorityTarget => Leader?.Target;
        public Vector3 FightLocation => Leader?.Position ?? Vector3.Zero;

        #endregion

    }
}
