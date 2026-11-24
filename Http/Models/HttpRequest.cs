using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MiniCurl.Http.Models
{
    public class HttpRequest
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public Uri Uri { get; set; } = null!;

        public Version Version { get; set; } = HttpVersion.Version11;

        public Dictionary<string, string> Headers { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public byte[]? Body { get; set; }
    }
}
