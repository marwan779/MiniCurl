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

        //var item = new HstsCacheItem(
        //    "github.com",
        //    TimeSpan.FromSeconds(31536000),
        //    includeSubDomains: true
        //);

        //var res = hstsCahceProvider.Set( item );

        //hstsCahceProvider.Delete("github.com");

        Console.WriteLine("\n--- Testing DNS Query Packet Builder ---");
        var dnsProvider = new DnsProvider();
        byte[] packet = dnsProvider.BuildQueryPacket("google.com");

        Console.WriteLine("\n[Program: Main] Packet Hex Dump:");
        Console.WriteLine(BitConverter.ToString(packet));
        Console.WriteLine($"Total Bytes: {packet.Length}");
    }
}
