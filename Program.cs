using MiniCurl.DNS;
using MiniCurl.Http;
using MiniCurl.Http.Models;
using MiniCurl.Parsing;
using MiniCurl.TCP;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("\n=================================");
        Console.WriteLine("    Testing MiniCurlHttpClient   ");
        Console.WriteLine("=================================");

        // 1. Choose a safe test URL
        string testUrl = "https://httpbin.org/get";
        // Or: "http://example.com/" for plain HTTP

        var uri = new Uri(testUrl);

        // 2. Resolve target IP via your custom DnsProvider
        var dnsResolver = new DnsProvider();
        var ips = dnsResolver.Resolve(uri.Host, "192.168.1.1");
        var targetIp = ips.First();

        // 3. Prepare ConnectionDetails
        var connectionDetails = new ConnectionDetails(
            targetIp,
            uri.Host,
            uri.Port,
            uri.Scheme == "https" ? HttpScheme.Https : HttpScheme.Http
        );

        // 4. Construct your HttpRequest
        var request = new HttpRequest
        {
            Method = HttpMethod.Get,
            Uri = uri,
            Version = HttpVersion.Version11
        };

        // Custom headers (optional)
        request.Headers["User-Agent"] = "MiniCurl/1.0";
        request.Headers["Accept"] = "application/json";

        // 5. Send request and receive response!
        var client = new MiniCurlHttpClient();
        var response = await client.SendRequestAsync(request, connectionDetails);

        // 6. Inspect the parsed response
        Console.WriteLine("\n=================================");
        Console.WriteLine($"Status: {response.StatusCode} {response.ReasonPhrase}");
        Console.WriteLine($"Version: HTTP/{response.Version.Major}.{response.Version.Minor}");
        Console.WriteLine("=================================");

        Console.WriteLine("\n--- Response Headers ---");
        foreach (var header in response.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }

        Console.WriteLine("\n--- Response Body ---");
        string bodyText = Encoding.UTF8.GetString(response.Body ?? Array.Empty<byte>());
        Console.WriteLine(bodyText);
    }
}