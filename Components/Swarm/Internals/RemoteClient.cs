using System;

namespace Trinity.Components.Swarm.Internals
{
    /// <summary>
    /// Represents a client that the host has been in contact with.
    /// </summary>
    [Serializable]
    public class RemoteClient : IEquatable<RemoteClient>
    {
        public RemoteClient(Identity identity, CommunicationMessage message = null)
        {
            Identity = identity;
            LastMessage = message;
        }

        public Identity Identity { get; }
        public CommunicationMessage LastMessage { get; set; }
        public ClientState State { get; set; } = ClientState.Active;

        public bool Equals(RemoteClient other) => other != null && other.GetHashCode() == GetHashCode();
        public override bool Equals(object obj) => (obj as RemoteClient)?.Equals(this) ?? false;
        public override int GetHashCode() => HashHelper.GetHashCode(Identity);
        public override string ToString() => $"{nameof(RemoteClient)}: {Identity.Name}";
    }
}
