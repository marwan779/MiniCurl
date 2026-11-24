using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCurl.Parsing
{
    public class ParsedURL
    {
        public string FullURL { get; set; } = string.Empty; // https://www.google.com:443
        public string Protocol { get; set; } = string.Empty; // HTTP/HTTPS
        public string Host { get; set; } = string.Empty; // ://www.google.com:443 (Domain name + Port)
        public string HostName { get; set; } = string.Empty; // ://www.google.com (Domain name)
        public int? Port { get; set; } // 443
        public string? PathName { get; set; } = string.Empty; // v1/users/profile
        public string? SearchQuery { get; set; } = string.Empty; // ?role=editor&tag=a&tag=b
    }
}
