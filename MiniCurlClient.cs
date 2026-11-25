using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MiniCurl.DNS;
using MiniCurl.Hsts;
using MiniCurl.Http;
using MiniCurl.Http.Models;
using MiniCurl.Parsing;
using MiniCurl.TCP;

namespace MiniCurl
{
    public class MiniCurlClient
    {
        private readonly URLParser _urlParser;
        private readonly HstsCacheProvider _hstsCache;
        private readonly DnsProvider _dnsResolver;
        private readonly MiniCurlHttpClient _httpClient;

        public MiniCurlClient()
        {
            _urlParser = new URLParser();
            _hstsCache = new HstsCacheProvider();
            _dnsResolver = new DnsProvider();
            _httpClient = new MiniCurlHttpClient();
        }

        public async Task<HttpResponse> SendAsync(
            string rawUrl, 
            HttpMethod? method = null, 
            Dictionary<string, string>? headers = null, 
            byte[]? body = null, 
            string dnsServer = "192.168.1.1")
        {
            Console.WriteLine($"\n[MiniCurlClient: SendAsync] Initiating request to: {rawUrl}");

            // 1. Parse the URL
            var parsedUrl = _urlParser.ParseURL(rawUrl);
            if (parsedUrl == null)
            {
                throw new ArgumentException($"Failed to parse URL: '{rawUrl}'", nameof(rawUrl));
            }

            // 2. Check HSTS policy & auto-upgrade HTTP -> HTTPS
            var hstsEntry = _hstsCache.Get(parsedUrl.HostName);
            if (hstsEntry != null && !hstsEntry.IsExpired)
            {
                if (parsedUrl.Scheme == HttpScheme.Http)
                {
                    Console.WriteLine($"[MiniCurlClient: SendAsync] Active HSTS policy found -> Upgrading '{parsedUrl.HostName}' from HTTP to HTTPS");
                    parsedUrl.Scheme = HttpScheme.Https;
                    parsedUrl.Port = 443;
                }
            }

            // 3. Resolve the IP address via DNS
            var ips = _dnsResolver.Resolve(parsedUrl.HostName, dnsServer);
            var targetIp = ips.First();

            int port = parsedUrl.Port ?? (parsedUrl.Scheme == HttpScheme.Https ? 443 : 80);

            // 4. Build connection details
            var connectionDetails = new ConnectionDetails(
                targetIp, 
                parsedUrl.HostName, 
                port, 
                parsedUrl.Scheme
            );

            // 5. Construct the HttpRequest object
            string schemeStr = parsedUrl.Scheme == HttpScheme.Https ? "https" : "http";
            string path = string.IsNullOrEmpty(parsedUrl.PathName) ? "/" : parsedUrl.PathName;
            string constructedUri = $"{schemeStr}://{parsedUrl.HostName}:{port}{path}";
            if (!string.IsNullOrEmpty(parsedUrl.SearchQuery))
            {
                constructedUri += $"?{parsedUrl.SearchQuery}";
            }

            var request = new HttpRequest
            {
                Method = method ?? HttpMethod.Get,
                Uri = new Uri(constructedUri),
                Version = HttpVersion.Version11,
                Body = body
            };

            // Copy custom headers if any
            if (headers != null)
            {
                foreach (var h in headers)
                {
                    request.Headers[h.Key] = h.Value;
                }
            }

            // 6. Execute request via HTTP client
            var response = await _httpClient.SendRequestAsync(request, connectionDetails);

            if (response == null)
            {
                throw new InvalidOperationException("[MiniCurlClient: SendAsync] Failed to receive response from server.");
            }

            // 7. Auto-ingest HSTS header if returned by server
            if (response.Headers.TryGetValue("Strict-Transport-Security", out var hstsHeaderValue))
            {
                IngestHstsHeader(parsedUrl.HostName, hstsHeaderValue);
            }

            Console.WriteLine($"[MiniCurlClient: SendAsync] Completed: {response.StatusCode} {response.ReasonPhrase}");

            return response;
        }

        private void IngestHstsHeader(string host, string hstsValue)
        {
            try
            {
                var parts = hstsValue.Split(';');
                long maxAgeSeconds = 0;
                bool includeSubDomains = false;

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith("max-age=", StringComparison.OrdinalIgnoreCase))
                    {
                        if (long.TryParse(trimmed.Substring(8), out var age))
                        {
                            maxAgeSeconds = age;
                        }
                    }
                    else if (trimmed.Equals("includeSubDomains", StringComparison.OrdinalIgnoreCase))
                    {
                        includeSubDomains = true;
                    }
                }

                if (maxAgeSeconds > 0)
                {
                    var item = new HstsCacheItem(
                        host, 
                        DateTimeOffset.UtcNow.AddSeconds(maxAgeSeconds), 
                        includeSubDomains, 
                        DateTimeOffset.UtcNow
                    );

                    _hstsCache.Set(item);
                    Console.WriteLine($"[MiniCurlClient: IngestHsts] Saved HSTS policy for '{host}' (Max-Age: {maxAgeSeconds}s, SubDomains: {includeSubDomains})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MiniCurlClient: IngestHsts] Error parsing HSTS header '{hstsValue}': {ex.Message}");
            }
        }
    }
}
