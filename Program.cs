using MiniCurl.Parsing;
using System;

class Program
{
    static void Main(string[] args)
    {
        var parser = new URLParser();

        string[] testUrls = new string[]
        {
            "https://www.example.com:8080/products?id=10#reviews", // Full URL with port, path, query, fragment
            "https://www.google.com",                              // No trailing slash, no port
            "google.com",                                          // Scheme omitted
            "http://localhost:5000",                               // HTTP with port, no path
            "example.com/api/v1/users?role=admin",                 // Scheme omitted with path & query
            "https://my-site.org:443/"                             // Explicit default port with trailing slash
        };

        foreach (var url in testUrls)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine($"INPUT: {url}");
            Console.WriteLine("--------------------------------------------------");
            var result = parser.ParseURL(url);
            if (result != null)
            {
                Console.WriteLine($"-> SUCCESS: HostName={result.HostName}, Port={result.Port}, Path={result.PathName}, Query={result.SearchQuery}");
            }
            else
            {
                Console.WriteLine("-> FAILED");
            }
            Console.WriteLine();
        }
    }
}
