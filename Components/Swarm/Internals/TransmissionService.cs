using System;
using System.ServiceModel;
using System.Text;

namespace Trinity.Components.Swarm.Internals
{
    public delegate void DataReceivedDelegate(CommunicationMessage message);

    [ServiceContract]
    public interface ITransmissionService
    {
        [OperationContract]
        void SendData(byte[] message);

        event DataReceivedDelegate DataReceived;
    }

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        IgnoreExtensionDataObject = true, 
        MaxItemsInObjectGraph = int.MaxValue)]        
    public class TransmissionService : ITransmissionService
    {
        public TransmissionService()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 200;
        }

        public event DataReceivedDelegate DataReceived;

        public void SendData(byte[] data)
        {
            var message = GetMessage(data);
            message.Received = DateTime.UtcNow;
            message.TransmissionTime = message.Received.Subtract(message.Sent);
            DataReceived?.Invoke(message);
        }

        public static CommunicationMessage GetMessage(byte[] bytes) => CommunicationMessage.Deserialize(GetString(bytes));
        public static byte[] PrepareMessage(CommunicationMessage msg) => GetBytes(msg.Serialize());
        public static byte[] GetBytes(string s) => Encoding.ASCII.GetBytes(s);
        public static string GetString(byte[] b) => Encoding.ASCII.GetString(b);
    }
}