using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace MiniCurl.TCP
{
    public class TcpHandler
    {
        public async Task<Stream> CreateConnection(IPAddress ipAddress, string host, int port, string scheme)
        {
            Console.WriteLine($"[TcpHandler: CreateConnection] Establishing connection to {ipAddress}:{port} ({scheme.ToUpper()})");
            // 1. Establish the TCP Socket
            var tcpSocket = await CreateTcpSocket(ipAddress, port);

            // 2. Wrap in NetworkStream
            NetworkStream networkStream = new NetworkStream(tcpSocket);

            // 3. If HTTPS -> return TLSHandshake(...)

            if (scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                return await TLSHandshake(networkStream, host);
            }

            // 4. If HTTP  -> return networkStream
            return networkStream;
        }

        private async Task<SslStream> TLSHandshake(NetworkStream networkStream, string host)
        {
            Console.WriteLine($"[TcpHandler: TLSHandshake] Initiating TLS handshake for host: '{host}'");
            var sslStream = new SslStream(
                    networkStream,
                    leaveInnerStreamOpen: false
                );

            var client = new SslClientAuthenticationOptions{ 
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.None 
            };

            try
            {
                await sslStream.AuthenticateAsClientAsync(client);

                Console.WriteLine($"[TcpHandler: TLSHandshake] Connected using: {sslStream.SslProtocol}");
                Console.WriteLine($"[TcpHandler: TLSHandshake] Cipher suite: {sslStream.NegotiatedCipherSuite}");

                return sslStream;
            }
            catch
            {
                await sslStream.DisposeAsync();
                throw;
            }
        }

        private async Task<Socket> CreateTcpSocket(IPAddress ipAddress, int port)
        {
            Console.WriteLine($"[TcpHandler: CreateTcpSocket] Connecting TCP socket to {ipAddress}:{port}");
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await socket.ConnectAsync(ipAddress, port);
            socket.ReceiveTimeout = 5000; // 5 seconds
            socket.SendTimeout = 5000;

            socket.NoDelay = true;
            Console.WriteLine($"[TcpHandler: CreateTcpSocket] TCP connected successfully: {socket.Connected}");


            return socket;
        }
    }
}
