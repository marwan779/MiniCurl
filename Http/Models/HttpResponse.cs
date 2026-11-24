
using System.Net;

namespace MiniCurl.Http.Models;

public class HttpResponse
{
    public Version Version { get; set; } = HttpVersion.Version11;

    public int StatusCode { get; set; }

    public string? ReasonPhrase { get; set; }

    public Dictionary<string, string> Headers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public byte[]? Body { get; set; }
}
