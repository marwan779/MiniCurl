using MiniCurl.DNS;
using MiniCurl.Hsts;
using MiniCurl.Parsing;
using MiniCurl.TCP;
using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("\n==============================");
        Console.WriteLine("    Testing TCP & TLS Flow    ");
        Console.WriteLine("==============================");

        var dnsProvider = new DnsProvider();
        var tcpHandler = new TcpHandler();

        // 1. Resolve host
        string host = "google.com";
        var ips = dnsProvider.Resolve(host, "192.168.1.1");
        var targetIp = ips.First();

        // 2. Test HTTPS (Port 443 with TLS 1.3)
        Console.WriteLine("\n--- Testing HTTPS (Port 443 + TLS) ---");
        await using (var httpsStream = await tcpHandler.CreateConnection(targetIp, host, 443, "https"))
        {
            Console.WriteLine($"[Program: Main] HTTPS Stream established! (CanRead: {httpsStream.CanRead}, CanWrite: {httpsStream.CanWrite})");
        }

        // 3. Test HTTP (Port 80 Plain TCP)
        Console.WriteLine("\n--- Testing HTTP (Port 80 Plain TCP) ---");
        await using (var httpStream = await tcpHandler.CreateConnection(targetIp, host, 80, "http"))
        {
            Console.WriteLine($"[Program: Main] HTTP Stream established! (CanRead: {httpStream.CanRead}, CanWrite: {httpStream.CanWrite})");
        }
    }
}
