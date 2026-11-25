using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using MiniCurl.Parsing;

namespace MiniCurl.TCP
{
    public static class TcpHandler
    {
        public static async Task<Stream> CreateConnection(ConnectionDetails details)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));

            Console.WriteLine($"[TcpHandler: CreateConnection] Establishing connection to {details.IpAddress}:{details.Port} ({details.Scheme})");
            // 1. Establish the TCP Socket
            var tcpSocket = await CreateTcpSocket(details.IpAddress, details.Port);

            // 2. Wrap in NetworkStream
            NetworkStream networkStream = new NetworkStream(tcpSocket);

            // 3. If HTTPS -> return TLSHandshake(...)

            if (details.Scheme == HttpScheme.Https)
            {
                return await TLSHandshake(networkStream, details.Host);
            }

            // 4. If HTTP  -> return networkStream
            return networkStream;
        }

        private static async Task<SslStream> TLSHandshake(NetworkStream networkStream, string host)
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
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpHandler: TLSHandshake] TLS handshake failed for '{host}': {ex.Message}");
                await sslStream.DisposeAsync();
                throw;
            }
        }

        private static async Task<Socket> CreateTcpSocket(IPAddress ipAddress, int port)
        {
            Console.WriteLine($"[TcpHandler: CreateTcpSocket] Connecting TCP socket to {ipAddress}:{port}");
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.ReceiveTimeout = 5000; // 5 seconds
            socket.SendTimeout = 5000;
            socket.NoDelay = true;

            try
            {
                await socket.ConnectAsync(ipAddress, port);
                Console.WriteLine($"[TcpHandler: CreateTcpSocket] TCP connected successfully: {socket.Connected}");
                return socket;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpHandler: CreateTcpSocket] Failed to connect to {ipAddress}:{port} - {ex.Message}");
                socket.Dispose();
                throw;
            }
        }
    }
}
