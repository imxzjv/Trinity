using System;
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Trinity.Components.Swarm.Internals
{
    public enum MessageType
    {
        None,
        Message,
        Response,
    }

    public enum ConnectionResult
    {
        None = 0,
        AddressInUse,
        Connected,
        Failed,
    }

    public class NamedPipeCommunicationDevice : CommunicationDevice
    {
        private static Uri _defaultUri = new Uri("net.pipe://localhost/Pipe");

        public static Uri SetPipeFormat(Uri requestUrl)
        {
            var ub = new UriBuilder(requestUrl + "Pipe")
            {
                Scheme = "net.pipe://",
                Port = -1
            };
            return ub.Uri;
        }

        public NamedPipeCommunicationDevice(Identity id, Uri address = null, string channel = "") 
            : base(id, address != null ? SetPipeFormat(address) : _defaultUri, channel) {  }

        protected override ServiceHost CreateHost()
        {
            var host = new ServiceHost(Service, new Uri(HostUri));
            host.AddServiceEndpoint(typeof(ITransmissionService), CreateBinding(), Identity.Role.ToString());
            return host;
        }

        public override Binding CreateBinding()
        {
            var binding = new NetNamedPipeBinding
            {
                OpenTimeout = TimeSpan.FromMinutes(15),
                SendTimeout = TimeSpan.FromMinutes(15),
                CloseTimeout = TimeSpan.FromMinutes(15),
                MaxConnections = 200,
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                TransactionFlow = false,
                TransactionProtocol = TransactionProtocol.WSAtomicTransaction11,
                TransferMode = TransferMode.StreamedRequest,
                HostNameComparisonMode = HostNameComparisonMode.WeakWildcard
            };
            binding.Security.Transport.ProtectionLevel = ProtectionLevel.None;
            return binding;
        }
    }

    public class HttpCommunicationDevice : CommunicationDevice
    {
        // To inspect data locally use ipv4.fiddler:12345/; 
        // Fiddler4 -> https://www.telerik.com/download/fiddler 

        public HttpCommunicationDevice(Identity id, Uri address, string channel = "") : base(id, address, channel) { }

        protected override ServiceHost CreateHost()
        {
            var host = new ServiceHost(Service, new Uri(HostUri));
            host.AddServiceEndpoint(typeof(ITransmissionService), CreateBinding(), Identity.Role.ToString());
            return host;
        }

        public override Binding CreateBinding()
        {
            var binding = new BasicHttpBinding
            {
                OpenTimeout = TimeSpan.FromMinutes(15),
                SendTimeout = TimeSpan.FromMinutes(15),
                CloseTimeout = TimeSpan.FromMinutes(15),
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferPoolSize = int.MaxValue,
                //TransferMode = TransferMode.StreamedRequest,
                //HostNameComparisonMode = HostNameComparisonMode.WeakWildcard
            };
            return binding;
        }
    }

    public abstract class CommunicationDevice
    {
        private ServiceHost _host;

        public Identity Identity { get; }

        public delegate void MessageErrprDelegate(Exception message);
        public delegate void MessageReceivedDelegate(CommunicationMessage message);

        public event MessageErrprDelegate ErrorOccurred;
        public event MessageReceivedDelegate MessageReceived;
        public event MessageReceivedDelegate ResponseReceived;

        public bool IsStarted { get; set; }

        public TransmissionService Service { get; }

        public bool IsOperational => 
            _host?.State == CommunicationState.Opened || 
            _host?.State == CommunicationState.Opening;

        protected CommunicationDevice(Identity id, Uri baseUri, string channel)
        {
            Identity = id;
            Service = new TransmissionService();
            Service.DataReceived += ServiceOnDataReceived;
            BaseUri = baseUri.AbsoluteUri;
            Channel = channel;
        }

        private void ServiceOnDataReceived(CommunicationMessage message)
        {
            switch (message.Type)
            {
                case MessageType.Response:
                    ResponseReceived?.Invoke(message);
                    break;
                default:
                    MessageReceived?.Invoke(message);
                    break;
            }
        }

        public virtual ConnectionResult Start()
        {
            try
            {
                if (_host != null)
                {
                    _host.Abort();
                }

                // Bug workaround for .net framework.
                // https://stackoverflow.com/questions/2977630/wcf-instance-already-exists-in-counterset-error-when-reopening-servicehost?rq=1

                GC.Collect();
                GC.WaitForPendingFinalizers();

                _host = CreateHost();

                //var ipgp = IPGlobalProperties.GetIPGlobalProperties();
                //var tcpListeners = ipgp.GetActiveTcpListeners();
                //var port = _host.BaseAddresses.FirstOrDefault().Port;
                //if (tcpListeners.Any(listner => listner.Port == port))
                //    return ConnectionResult.AddressInUse;

                _host.Open();
                IsStarted = true;
                return ConnectionResult.Connected;
            }
            catch (AddressAlreadyInUseException)
            {
                Stop();
                return ConnectionResult.AddressInUse;
            }
            catch (InvalidOperationException ex)
            {
                if (ex.InnerException?.Message.StartsWith("A registration already exists") ?? false)
                {
                    Stop();
                    return ConnectionResult.AddressInUse;
                }
                ErrorOccurred?.Invoke(ex);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
            Stop();
            return ConnectionResult.Failed;
        }

        private string Channel { get; }

        public string BaseUri { get; }

        public string HostUri => GetHostUri(Identity);

        public string Uri => GetUri(Identity);

        public string GetHostUri(Identity id) => BaseUri + (id?.Role == IdentityRole.Client ? id.ChannelName : string.Empty) + Channel;

        public string GetUri(Identity id) => GetHostUri(id) + "/" + id?.Role + Channel;

        protected abstract ServiceHost CreateHost();

        public abstract Binding CreateBinding();

        public void Stop()
        {
            _host?.Abort();
            _host?.Close();
            _host = null;
            IsStarted = false;
        }

    }
}