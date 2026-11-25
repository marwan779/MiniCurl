using MiniCurl.Http.Models;
using MiniCurl.TCP;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MiniCurl.Http
{
    public class MiniCurlHttpClient
    {
       
        public async Task<HttpResponse> SendRequestAsync(HttpRequest request, ConnectionDetails connectionDetails)
        {

            //Validate the request
            if (request == null)
            {
                Console.WriteLine("[MiniCurlHttpClient: ValidateRequest] Request cannot be null");
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Uri == null)
            {
                Console.WriteLine("[MiniCurlHttpClient: ValidateRequest] Request URI cannot be null");
                throw new ArgumentException("Request URI cannot be null.", nameof(request));
            }

            if (!request.Uri.IsAbsoluteUri)
            {
                Console.WriteLine($"[MiniCurlHttpClient: ValidateRequest] URI '{request.Uri}' is not an absolute URI");
                throw new ArgumentException("Request URI must be an absolute URI (e.g. https://example.com/)", nameof(request));
            }

            if (request.Uri.Scheme != Uri.UriSchemeHttp && request.Uri.Scheme != Uri.UriSchemeHttps)
            {
                Console.WriteLine($"[MiniCurlHttpClient: ValidateRequest] Unsupported scheme '{request.Uri.Scheme}'");
                throw new NotSupportedException($"Only HTTP and HTTPS schemes are supported, got '{request.Uri.Scheme}'.");
            }

            if (request.Method == null)
            {
                Console.WriteLine("[MiniCurlHttpClient: ValidateRequest] Request Method cannot be null");
                throw new ArgumentException("HTTP Method cannot be null.", nameof(request));
            }

            if (request.Version != HttpVersion.Version11 && request.Version != HttpVersion.Version10)
            {
                Console.WriteLine($"[MiniCurlHttpClient: ValidateRequest] Unsupported HTTP version: {request.Version}");
                throw new NotSupportedException($"Only HTTP/1.0 and HTTP/1.1 are supported, got {request.Version}.");
            }

            Console.WriteLine($"[MiniCurlHttpClient: ValidateRequest] Request validated successfully ({request.Method} {request.Uri})");

            //Serialize the request
            var serializedRequest = SerializeRequest( request );

            //Esiblash the TCP connection
            var connection = await TcpHandler.CreateConnection(connectionDetails);

            //Send the request over the established TCP connection
            await connection.WriteAsync(serializedRequest, 0, serializedRequest.Length);

            //Flush to ensure the bytes are immediately transmitted without buffering
            await connection.FlushAsync();

            //Receive the response
            using var responseMs = new MemoryStream();
            byte[] buffer = new byte[4096]; // 4 KB chunk buffer
            int bytesRead;
            // Keep reading chunks from the network until the server closes the connection (bytesRead == 0)
            while ((bytesRead = await connection.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                responseMs.Write(buffer, 0, bytesRead);
            }
            byte[] fullResponseBytes = responseMs.ToArray();

            var parsedResponse = ParseResponse(fullResponseBytes);

            return parsedResponse;
        }

        // Helper to serialize the request 

        //┌────────────────────────────────────────────────────────┐
        //│  METHOD PathAndQuery  HTTP / 1.1\r\n<-- Request Line   │
        //├────────────────────────────────────────────────────────┤
        //│  Host: example.com\r\n                                 │
        //│  User-Agent: MiniCurl/1.0\r\n<-- Headers               │
        //│  Accept: */*\r\n                                       │
        //│  Content-Length: 15\r\n (if body)                      │
        //├────────────────────────────────────────────────────────┤
        //│  \r\n                               <-- Empty Delimiter│
        //├────────────────────────────────────────────────────────┤
        //│  {"name":"John"}                     <-- Optional Body │
        //└────────────────────────────────────────────────────────┘
        private byte[] SerializeRequest(HttpRequest request)
        {

            StringBuilder sb = new StringBuilder();


            // Format the reqeust line (e.g. METHOD  PathAndQuery  HTTP/1.1\r\n)
            string pathAndQuery = string.IsNullOrEmpty(request.Uri.PathAndQuery) ? "/" : request.Uri.PathAndQuery;
            string fromatedRequestLine = $"{request.Method} {request.Uri.PathAndQuery} HTTP/{request.Version.Major}.{request.Version.Minor}\r\n";

            sb.Append( fromatedRequestLine );

            // Host Header (Mandatory in HTTP/1.1!)
            if (!request.Headers.ContainsKey("Host"))
            {
                sb.Append($"Host: {request.Uri.Authority}\r\n");
            }

            // Fromat the headers
            foreach (var header in request.Headers )
            {
                sb.Append($"{header.Key}: {header.Value}\r\n");
            }

            if(request.Method == HttpMethod.Post && request.Body != null && !request.Headers.ContainsKey("Content-Length"))
            {
                sb.Append($"Content-Length: {request.Body.Length}\r\n");

            }


            // Connection: close(tells server to close socket when response is done)
            if (!request.Headers.ContainsKey("Connection"))
            {
                sb.Append("Connection: close\r\n");
            }

            // Adding the empty delimiter
            sb.Append("\r\n");

            //Converting string fromated header section to bytes
            byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());

            //If their is no body -> return only the serialized headers
            if(request.Body == null || request.Body.Length == 0)
            {
                return headerBytes;
            }

            // If there is a body, concatenate headerBytes + request.Body
            byte[] fullPacket = new byte[headerBytes.Length + request.Body.Length];
            Buffer.BlockCopy(headerBytes, 0, fullPacket, 0, headerBytes.Length);
            Buffer.BlockCopy(request.Body, 0, fullPacket, headerBytes.Length, request.Body.Length);

            return fullPacket;
        }
        

        //Helper to parese the response

        //┌────────────────────────────────────────────────────────┐
        //│ HTTP/1.1 200 OK\r\n<-- Status Line                     │
        //├────────────────────────────────────────────────────────┤
        //│ Date: Wed, 25 Nov 2026 04:38:00 GMT\r\n                │
        //│ Content-Type: text/html; charset=UTF-8\r\n             │
        //│ Content-Length: 1256\r\n<-- Headers                    │
        //│ Strict-Transport-Security: max-age = ...               │
        //│ Connection: close\r\n                                  │
        //├────────────────────────────────────────────────────────┤
        //│ \r\n<-- Empty Delim                                    │
        //├────────────────────────────────────────────────────────┤
        //│ <!doctype html><html>...               <-- Body Bytes  │
        //│ (Raw HTML, JSON, image, etc.)                          │
        //└────────────────────────────────────────────────────────┘

        private HttpResponse ParseResponse(byte[] responseBytes)
        {

            //First we need to get the index of the end of the headers section
            int delimiterIndex = -1;

            for(int i = 0; i < responseBytes.Length; i++)
            {
                if (responseBytes[i] == '\r' && responseBytes[i + 1] == '\n' //Connection: close\r\n
                    && responseBytes[i + 2] == '\r' && responseBytes[i + 3] == '\n') //\r\n<-- Empty Delim  
                {
                    delimiterIndex = i;
                    break;
                }
            }

            if (delimiterIndex == -1)
                throw new InvalidDataException("[MiniCurlHttpClient: ParseResponse] Invalid HTTP response: Missing header delimiter.");

            int bodyStart = delimiterIndex + 4;
            byte[] bodySection = responseBytes[bodyStart..];
            string headerSection = Encoding.ASCII.GetString(responseBytes, 0, delimiterIndex);

            //Now we need to divid the status line and the headers

            string[] headerLines = headerSection.Split("\r\n");
            string statusLine = headerLines[0];

            //Also split the status line parts
            string[] statusLineParts = statusLine.Split(' ', 3);

            // statusParts[0] = "HTTP/1.1"
            // statusParts[1] = "200"
            // statusParts[2] = "OK"

            Version version = statusLineParts[0].Contains("1.0") ? HttpVersion.Version10 : HttpVersion.Version11;
            int statusCode = int.Parse(statusLineParts[1]);
            string reasonPhrase = statusLineParts.Length > 2 ? statusLineParts[2] : string.Empty;

           //Now parse the headers

            Dictionary<string, string> headers  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for(int i = 1; i < headerLines.Length - 3; i++)
            {
                string headerLine = headerLines[i];
                if (string.IsNullOrWhiteSpace(headerLine)) continue;

                int colonIndex = headerLine.IndexOf(':');
                if(colonIndex != -1)
                {
                    string key = headerLine.Substring(0, colonIndex).Trim();
                    string value = headerLine.Substring(colonIndex + 1).Trim();

                    headers[key] = value;
                }
            }


            return new HttpResponse
            {
                Version = version,
                StatusCode = statusCode,
                ReasonPhrase = reasonPhrase,
                Headers = headers,
                Body = bodySection
            };
        }
    }
}
