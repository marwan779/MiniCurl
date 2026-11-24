using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCurl.Parsing
{
    public class URLParser
    {
        // Example: https://www.example.com:8080/products?id=10#reviews
        public ParsedURL? ParseURL(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    Console.WriteLine("[URLParser: ParseURL] URL cannot be null or empty");
                    return null;
                }

                url = url.Trim();

                // 1. If scheme is omitted (e.g. "google.com" or "example.com/api"), default to https://
                if (!url.Contains("://"))
                {
                    Console.WriteLine("[URLParser: ParseURL] Protocol omitted -> defaulting scheme to https://");
                    url = "https://" + url;
                }

                // 2. Extract the protocol
                int colonIndex = url.IndexOf(":");
                if (colonIndex == -1)
                {
                    Console.WriteLine("[URLParser: ParseURL] Invalid URL format (missing colon)");
                    return null;
                }

                string protocol = url.Substring(0, colonIndex).ToLower();
                if (protocol != "http" && protocol != "https")
                {
                    Console.WriteLine($"[URLParser: ParseURL] Invalid protocol '{protocol}' (only http / https supported)");
                    return null;
                }

                HttpScheme scheme = protocol == "https" ? HttpScheme.Https : HttpScheme.Http;
                Console.WriteLine($"[URLParser: ParseURL] Protocol: {scheme}");

                // 3. Locate where host begins (after "://")
                int hostStart = colonIndex + 3; // skip "://"
                if (hostStart >= url.Length)
                {
                    Console.WriteLine("[URLParser: ParseURL] Invalid URL (no host specified)");
                    return null;
                }

                // Find where host ends (first occurrence of '/', '?', or '#')
                int slashIndex = url.IndexOf("/", hostStart);
                int questionIndex = url.IndexOf("?", hostStart);
                int hashIndex = url.IndexOf("#", hostStart);

                int hostEnd = url.Length;
                if (slashIndex != -1) hostEnd = Math.Min(hostEnd, slashIndex);
                if (questionIndex != -1) hostEnd = Math.Min(hostEnd, questionIndex);
                if (hashIndex != -1) hostEnd = Math.Min(hostEnd, hashIndex);

                string host = url.Substring(hostStart, hostEnd - hostStart);
                if (string.IsNullOrWhiteSpace(host))
                {
                    Console.WriteLine("[URLParser: ParseURL] Invalid host (empty)");
                    return null;
                }

                Console.WriteLine($"[URLParser: ParseURL] Host: {host}");

                // 4. Parse host into HostName and Port
                int hostColonIndex = host.IndexOf(":");
                string hostName;
                int port;

                if (hostColonIndex != -1)
                {
                    hostName = host.Substring(0, hostColonIndex);
                    string portStr = host.Substring(hostColonIndex + 1);

                    if (!int.TryParse(portStr, out port) || port <= 0 || port > 65535)
                    {
                        Console.WriteLine($"[URLParser: ParseURL] Invalid port number: '{portStr}'");
                        return null;
                    }
                }
                else
                {
                    hostName = host;
                    port = scheme == HttpScheme.Https ? 443 : 80;
                }

                Console.WriteLine($"[URLParser: ParseURL] HostName: {hostName}");
                Console.WriteLine($"[URLParser: ParseURL] Port: {port}");

                // 5. Extract PathName, SearchQuery, and ignore Fragment (#)
                string remaining = url.Substring(hostEnd);

                // Strip fragment (#) because fragments are client-only and never sent to servers
                int remainingHash = remaining.IndexOf("#");
                if (remainingHash != -1)
                {
                    remaining = remaining.Substring(0, remainingHash);
                }

                string pathName = "/";
                string? searchQuery = null;

                int remainingQuestion = remaining.IndexOf("?");
                if (remainingQuestion != -1)
                {
                    string rawPath = remaining.Substring(0, remainingQuestion);
                    pathName = string.IsNullOrEmpty(rawPath) ? "/" : rawPath;
                    searchQuery = remaining.Substring(remainingQuestion + 1);
                }
                else
                {
                    pathName = string.IsNullOrEmpty(remaining) ? "/" : remaining;
                }

                Console.WriteLine($"[URLParser: ParseURL] PathName: {pathName}");
                Console.WriteLine($"[URLParser: ParseURL] SearchQuery: {searchQuery}");

                return new ParsedURL
                {
                    FullURL = url,
                    Scheme = scheme,
                    Host = host,
                    HostName = hostName,
                    Port = port,
                    PathName = pathName,
                    SearchQuery = searchQuery
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[URLParser: ParseURL] Unexpected error parsing URL '{url}': {ex.Message}");
                return null;
            }
        }
    }
}
