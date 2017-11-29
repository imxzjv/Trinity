using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Trinity.Components.Swarm.Internals
{
    public static class IpHelper
    {
        public static IPAddress FindIp4FromIp6(IPAddress ipAddress)
        {
            var ipHostEntry = Dns.GetHostEntry(ipAddress);
            return ipHostEntry.AddressList.Any(address => address.AddressFamily == AddressFamily.InterNetwork) ? ipAddress : IPAddress.None;
        }

        public static IPAddress ParseIp4Address(string input)
        {
            IPAddress address;
            if (!IPAddress.TryParse(input, out address))
                return IPAddress.None;

            switch (address.AddressFamily)
            {
                case System.Net.Sockets.AddressFamily.InterNetwork:
                    return address;
                case System.Net.Sockets.AddressFamily.InterNetworkV6:
                    return FindIp4FromIp6(address);
            }
            return IPAddress.None;
        }

        public static IPAddress GetMachineAddress()
        {
            var addresses = Dns.GetHostAddresses(Dns.GetHostName()).ToList();

            var ip4 = addresses.FirstOrDefault(ip 
                => !IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetwork);

            return ip4 ?? FindIp4FromIp6(addresses.FirstOrDefault(ip 
                => !IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetworkV6));
        }

        public static IPAddress GetExternalAddress()
        {
            IPAddress address;
            WebClient webClient = new WebClient();
            var result = webClient.DownloadString("http://myip.ozymo.com/");
            return !IPAddress.TryParse(result, out address) ? IPAddress.None : address;
        }

        public static Uri AddressToUri(IPAddress address, bool https = false, int port = 0)
        {
            var portPart = port != 0 ? ":" + port : string.Empty;
            var protocolPart = https ? "https" : "http";
            return new Uri($"{protocolPart}://" + GetMachineAddress() + portPart + "/");
        }

        public static Uri ChangeUriPort(Uri uri, int newPort)
        {
            var builder = new UriBuilder(uri);
            builder.Port = newPort;
            return builder.Uri;
        }


    }
}






