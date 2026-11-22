using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCurl.Parsing
{
    public class URLParser
    {

        //https://www.example.com:8080/products?id=10#reviews
        public ParsedURL? ParseURL(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine("[URLParser: ParseURL] URL can't be null or empty");
                return null;
            }

            url = url.Trim();

            int left, right = 0;
            string? protocol = null;

            //Extract the protocol
            right = url.IndexOf(":");
            if (right == -1)
            {
                Console.WriteLine("[URLParser: ParseURL] counldn't find protocol (Http/Https)");
                return null;
            }

            protocol = url.Substring(0, right);

            protocol = protocol.ToLower();

            if (protocol != "http" && protocol != "https")
            {

                Console.WriteLine("[URLParser: ParseURL] invaild protocol (only Http/Https)");
                return null;
            }

            Console.WriteLine($"[URLParser: ParseURL] protocol: {protocol}");

            //Extract host
            left = right + 3; //skip ://
            right = url.IndexOf("/", left);

            string? host = url.Substring(left, right - left);

            if (host == null)
            {
                Console.WriteLine("[URLParser: ParseURL] invaild host");
                return null;
            }

            Console.WriteLine($"[URLParser: ParseURL] host: {host}");

            //parse the host
            int colonIndex = host.IndexOf(":");
            int port;
            string? hostName = null;

           int.TryParse((host.Substring(colonIndex + 1)), out port);

            //if port wasn't specified, falling back to defualt port
            if(port == 0)
            {
                port = protocol == "https" ? 443 : 80;
            }    

           Console.WriteLine($"[URLParser: ParseURL] port: {port}");

            hostName = host.Substring(0, colonIndex);

            Console.WriteLine($"[URLParser: ParseURL] host name: {hostName}");

            //Extract PathName and SearchQuery
            string? pathName = null;
            string? searchQuery = null;

            if (right != -1)
            {
                left = right;
                right = url.IndexOf("?", left);

                if (right == -1)
                {
                    pathName = url.Substring(left);
                }
                else
                {
                    pathName = url.Substring(left, right - left);
                    searchQuery = url.Substring(right + 1);
                }
            }

            Console.WriteLine($"[URLParser: ParseURL] path name: {pathName}");
            Console.WriteLine($"[URLParser: ParseURL] search query: {searchQuery}");


            return new ParsedURL
            {
                FullURL = url,
                Protocol = protocol,
                Host = host,
                HostName = hostName,
                Port = port,
                PathName = pathName,
                SearchQuery = searchQuery
            };
        }
    }
}
