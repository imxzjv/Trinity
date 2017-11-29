using System;

namespace Trinity.Components.Swarm.Internals
{
    public enum IdentityRole
    {
        None = 0,
        Host,
        Client,
    }

    [Serializable]
    public class Identity
    {
        public Identity(int processId, string name, string pipeName, IdentityRole role)
        {
            ProcessId = processId;
            Name = name;
            ChannelName = pipeName;
            Role = role;
            Id = HashHelper.GetHashCode(Role, Name, ChannelName);
        }

        public int Id { get; }
        public IdentityRole Role { get; }
        public int ProcessId { get; }
        public string Name { get; }
        public string ChannelName { get; }
        public string Address { get; set; }

        public bool Equals(Identity other) => other != null && other.GetHashCode() == GetHashCode();
        public override bool Equals(object obj) => (obj as Identity)?.Equals(this) ?? false;
        public override int GetHashCode() => Id;
        public override string ToString() => $"{nameof(Identity)}: {Name}";
    }
}
