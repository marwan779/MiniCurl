using MiniCurl;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("\n=================================");
        Console.WriteLine("    Testing MiniCurlClient       ");
        Console.WriteLine("=================================");

        var client = new MiniCurlClient();

        // Test 1: GET request to https://httpbin.org/get
        Console.WriteLine("\n>>> 1. Testing GET Request <<<");
        var getResponse = await client.SendAsync("https://httpbin.org/get");

        Console.WriteLine("\n=================================");
        Console.WriteLine($"GET Status: {getResponse.StatusCode} {getResponse.ReasonPhrase}");
        Console.WriteLine("=================================");
        Console.WriteLine(Encoding.UTF8.GetString(getResponse.Body ?? Array.Empty<byte>()));

        // Test 2: Safe POST request to https://httpbin.org/post with JSON payload
        Console.WriteLine("\n>>> 2. Testing POST Request with JSON Payload <<<");
        string jsonPayload = "{\"project\": \"MiniCurl\", \"author\": \"You\", \"feature\": \"Raw Socket HTTP Client\"}";
        byte[] postBody = Encoding.UTF8.GetBytes(jsonPayload);

        var postHeaders = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "User-Agent", "MiniCurl/1.0" }
        };

        var postResponse = await client.SendAsync(
            "https://httpbin.org/post",
            HttpMethod.Post,
            postHeaders,
            postBody
        );

        Console.WriteLine("\n=================================");
        Console.WriteLine($"POST Status: {postResponse.StatusCode} {postResponse.ReasonPhrase}");
        Console.WriteLine("=================================");

        Console.WriteLine("\n--- Response Headers ---");
        foreach (var header in postResponse.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }

        Console.WriteLine("\n--- Echoed Response Body ---");
        string postResponseBody = Encoding.UTF8.GetString(postResponse.Body ?? Array.Empty<byte>());
        Console.WriteLine(postResponseBody);
    }
}
