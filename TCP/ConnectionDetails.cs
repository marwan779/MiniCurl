using System.Net;
using MiniCurl.Parsing;

namespace MiniCurl.TCP
{
    public class ConnectionDetails
    {
        public IPAddress IpAddress { get; set; } = null!;

        public string Host { get; set; } = string.Empty;

        public int Port { get; set; }

        public HttpScheme Scheme { get; set; }

        public ConnectionDetails()
        {
        }

        public ConnectionDetails(IPAddress ipAddress, string host, int port, HttpScheme scheme)
        {
            IpAddress = ipAddress;
            Host = host;
            Port = port;
            Scheme = scheme;
        }
    }
}
