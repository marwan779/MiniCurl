# MiniCurl 🚀

> A lightweight, zero-dependency HTTP/1.1 client built from scratch in C# (.NET 10).

MiniCurl is a **personal educational and tutorial side-project** created to pull back the curtain on high-level networking abstractions (such as .NET's `HttpClient` or browser fetch APIs) and explore how web communication actually functions under the hood—from raw wire bytes and datagram sockets to encrypted TLS streams and HTTP/1.1 framing.

---

## 🧭 The End-to-End Pipeline

When you pass a URL to `MiniCurlClient`, it coordinates every layer of the networking stack manually:

```text
Raw URL ("https://httpbin.org/get")
   │
   ├─ 1. URL Parser           -> Extracts scheme, hostname, port, path, and query parameters.
   │
   ├─ 2. HSTS Cache Provider  -> Inspects local persistent cache for active HSTS policies;
   │                             auto-upgrades plaintext HTTP to HTTPS on port 443.
   │
   ├─ 3. Raw UDP DNS Resolver -> Crafts an RFC 1035 wire-format DNS packet from scratch,
   │                             sends over UDP port 53, decodes compression pointers (0xC0),
   │                             and resolves the hostname to an IPv4 address.
   │
   ├─ 4. TCP Socket Engine    -> Allocates a BSD Stream socket and completes the
   │                             asynchronous TCP 3-Way Handshake (SYN -> SYN-ACK -> ACK).
   │
   ├─ 5. TLS 1.3 Handshake    -> If HTTPS, wraps the stream in a TLS layer with SNI extension,
   │                             negotiating modern AEAD cipher suites (e.g. TLS_AES_256_GCM_SHA384).
   │
   ├─ 6. HTTP/1.1 Wire Engine -> Serializes the request line and headers using RFC 7230 CRLF (\r\n),
   │                             streams bytes over the wire, and reads response chunks until EOF.
   │
   ├─ 7. Response Parser      -> Locates the \r\n\r\n boundary, decodes the ASCII/UTF-8 status
   │                             line and headers, and preserves the raw binary body.
   │
   └─ 8. HSTS Auto-Ingestion  -> Parses any incoming Strict-Transport-Security headers and
                                 persists them to disk for future requests.
```

---

## 🛠️ Subsystems & Components

| Component | Location | Description |
| :--- | :--- | :--- |
| **`URLParser`** | `UrlParsing/` | Hand-rolled URL parser following RFC 3986 (supports scheme omission, default port deduction, query extraction, and fragment stripping). |
| **`HttpScheme`** | `UrlParsing/` | Strongly-typed enum (`Http`, `Https`) eliminating magic strings across the engine. |
| **`HstsCacheProvider`**| `Hsts/` | JSON-backed local persistent storage (`HSTSStorage.json`) tracking domain expiration dates and subdomain inclusion rules. |
| **`DnsProvider`** | `DNS/` | Low-level RFC 1035 UDP DNS resolver: builds 12-byte headers, generates random Transaction IDs, encodes domain labels, and unpacks Answer Resource Records (with `0xC0` pointer decompression). |
| **`TcpHandler`** | `TCP/` | Manages raw Berkeley sockets (`SocketType.Stream`, `ProtocolType.Tcp`), configures timeouts, disables Nagle's algorithm (`NoDelay`), and performs TLS 1.3 handshakes. |
| **`MiniCurlHttpClient`**| `Http/` | Validates requests, serializes HTTP/1.1 wire packets, streams payload bytes, and parses raw incoming bytes into a strongly-typed `HttpResponse`. |
| **`MiniCurlClient`** | Root | The public orchestrator providing a clean, developer-friendly facade. |

---

## 💻 Quickstart & Usage

### 1. Simple GET Request

```csharp
using MiniCurl;

var client = new MiniCurlClient();
var response = await client.SendAsync("https://httpbin.org/get");

Console.WriteLine($"Status: {response.StatusCode} {response.ReasonPhrase}");
string body = Encoding.UTF8.GetString(response.Body ?? Array.Empty<byte>());
Console.WriteLine(body);
```

### 2. POST Request with JSON Payload

```csharp
using MiniCurl;
using System.Net.Http;
using System.Text;

var client = new MiniCurlClient();

string json = "{\"project\": \"MiniCurl\", \"language\": \"C#\"}";
byte[] payload = Encoding.UTF8.GetBytes(json);

var headers = new Dictionary<string, string>
{
    { "Content-Type", "application/json" }
};

var response = await client.SendAsync(
    "https://httpbin.org/post",
    HttpMethod.Post,
    headers,
    payload
);

Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine(Encoding.UTF8.GetString(response.Body));
```

---

## 🔬 Sample Live Output

```text
=================================
    Testing MiniCurlClient       
=================================
[MiniCurlClient: SendAsync] Initiating request to: https://httpbin.org/post
[URLParser: ParseURL] Protocol: Https, Host: httpbin.org, Port: 443
[DnsProvider: Resolve] Resolved 'httpbin.org' -> 54.172.45.38 via UDP port 53
[TcpHandler: CreateTcpSocket] TCP connected successfully: True
[TcpHandler: TLSHandshake] Connected using: Tls13 (TLS_AES_256_GCM_SHA384)
[MiniCurlClient: SendAsync] Completed: 200 OK

=================================
POST Status: 200 OK
=================================
{
  "headers": {
    "Content-Length": "38",
    "Content-Type": "application/json",
    "Host": "httpbin.org"
  },
  "method": "POST",
  "origin": "156.206.77.13",
  "url": "https://httpbin.org/post",
  "data": "{\"project\": \"MiniCurl\", \"language\": \"C#\"}"
}
```

---

## 🎯 Learning Objectives Met

- **RFC 1035**: Bitwise flag operations, big-endian binary integers, label wire formatting, pointer decompression.
- **BSD Sockets**: Socket creation, non-blocking asynchronous `ConnectAsync`, streaming buffers, EOF detection (`bytesRead == 0`).
- **Transport Security**: TLS 1.3 handshake, Server Name Indication (SNI), cipher negotiation.
- **RFC 7230 / RFC 9112**: CRLF formatting, mandatory Host headers, Content-Length calculation, header/body boundary detection.
