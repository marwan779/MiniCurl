using MiniCurl.DNS;
using MiniCurl.Hsts;
using MiniCurl.Parsing;
using System;

class Program
{
    static void Main(string[] args)
    {
        var parser = new URLParser();

        HstsCahceProvider hstsCahceProvider = new HstsCahceProvider();

        Console.WriteLine("\n==============================");
        Console.WriteLine("    Testing DNS Resolution    ");
        Console.WriteLine("==============================");

        var dnsProvider = new DnsProvider();

        // Test 1: Resolve google.com using 8.8.8.8
        var googleIps = dnsProvider.Resolve("google.com");
        Console.WriteLine($"\n[Program: Main] google.com resolved to: {string.Join(", ", googleIps)}");

        // Test 2: Resolve cloudflare.com
        var cfIps = dnsProvider.Resolve("cloudflare.com");
        Console.WriteLine($"\n[Program: Main] cloudflare.com resolved to: {string.Join(", ", cfIps)}");
    }
}
