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
        Console.WriteLine("    Testing Refactored Pipeline    ");
        Console.WriteLine("==============================");

        var parser = new URLParser();
        var hstsCache = new HstsCacheProvider();
        var dnsResolver = new DnsProvider();
        var tcpHandler = new TcpHandler();

        // 1. Parse URL with HttpScheme enum
        string url = "https://google.com";
        var parsedUrl = parser.ParseURL(url);

        if (parsedUrl == null)
        {
            Console.WriteLine("[Program: Main] URL parsing failed");
            return;
        }

        // 2. Check HSTS Cache
        var hstsEntry = hstsCache.Get(parsedUrl.HostName);
        if (hstsEntry != null && !hstsEntry.IsExpired)
        {
            Console.WriteLine($"[Program: Main] Upgrading scheme to HTTPS due to active HSTS policy");
            parsedUrl.Scheme = HttpScheme.Https;
            parsedUrl.Port = 443;
        }

        // 3. Resolve Domain to IP
        var ips = dnsResolver.Resolve(parsedUrl.HostName, "192.168.1.1");
        var targetIp = ips.First();

        // 4. Test HTTPS connection using HttpScheme enum
        Console.WriteLine("\n--- Testing HTTPS Connection ---");
        await using (var httpsStream = await tcpHandler.CreateConnection(targetIp, parsedUrl.HostName, parsedUrl.Port ?? 443, parsedUrl.Scheme))
        {
            Console.WriteLine($"[Program: Main] Connection active: CanRead={httpsStream.CanRead}, CanWrite={httpsStream.CanWrite}");
        }

        // 5. Test Plain HTTP connection using HttpScheme enum
        Console.WriteLine("\n--- Testing HTTP Connection ---");
        await using (var httpStream = await tcpHandler.CreateConnection(targetIp, parsedUrl.HostName, 80, HttpScheme.Http))
        {
            Console.WriteLine($"[Program: Main] Connection active: CanRead={httpStream.CanRead}, CanWrite={httpStream.CanWrite}");
        }
    }
}
