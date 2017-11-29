using System;

namespace Trinity.Components.Swarm.Internals
{
    [Serializable]
    public class CommunicationMessage
    {
        public CommunicationMessage()
        {
            Guid = Guid.NewGuid();
        }

        public Guid Guid { get; }
        public string Name { get; set; }
        public Identity To { get; set; }
        public Identity From { get; set; }
        public string FromAddress { get; set; }
        public DateTime Sent { get; set; } 
        public DateTime Received { get; set; } = DateTime.MaxValue;
        public TimeSpan TransmissionTime { get; set; }
        public string RawData { get; set; }
        public Guid CallbackId { get; set; }        
        public MessageType Type { get; set; } = MessageType.Message;
        public T GetValue<T>() where T : class => DataSerializer.Current.Deserialize<T>(RawData);
        public void SetValue<T>(T obj) where T : class => RawData = DataSerializer.Current.Serialize(obj);
        public static CommunicationMessage Deserialize(string input) => DataSerializer.Current.Deserialize<CommunicationMessage>(input);
        public string Serialize() => DataSerializer.Current.Serialize(this);
        public override int GetHashCode()  => HashHelper.GetHashCode(Guid);
        public override string ToString() => $"{nameof(CommunicationMessage)}: {Name}, From: {From?.Name}, To: {To?.Name}";

    }
}

