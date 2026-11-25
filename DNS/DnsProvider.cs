using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Transactions;

namespace MiniCurl.DNS
{
    public class DnsProvider
    {
        // 1. Helper to convert the raw domain (e.g. google.com) to DNS wire fromat 06 67 6F 6F 67 6C 65 03 63 6F 6D 00)
        private byte[] EncodeDomainName(string domain)
        {
            Console.WriteLine($"[DnsProvider: EncodeDomainName] Encoding domain '{domain}' into DNS wire-format");
            byte[] result = {};

            //validate the given domain
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentNullException("[DnsProvider: ConvertRawDomain] Domain can't be null or empty");

            //remove the trailing . root if present (e.g. google.com. -> google.com)
            domain = domain.TrimEnd('.');

            //convert the raw domain that might have unicode to ASCII/Punycode
            var idn = new IdnMapping();
            var asciiDomain = idn.GetAscii(domain);

            //split the domain by .(e.g. google.com. -> ["google", "com"])
            using var ms = new MemoryStream();
            string[] labels = asciiDomain.Split('.');

            foreach (var label in labels)
            {
                if (label.Length == 0 || label.Length > 63)
                    throw new ArgumentException($"[DnsProvider: ConvertRawDomain] Invalid DNS label length for '{label}'. Must be 1–63 bytes.");

                //converting the ascii characters to bytes
                byte[] bytes = Encoding.ASCII.GetBytes(label);

                //get its length to set it as a prefix before the actual bytes
                ms.WriteByte((byte)bytes.Length);
                ms.Write(bytes, 0, bytes.Length);
            }

            //set the 0x00 terminator
            ms.WriteByte(0x00);


            //finally validate the overall length of the result array
            result = ms.ToArray();
            if (result.Length > 255)
                throw new ArgumentException("[DnsProvider: ConvertRawDomain] Total DNS wire-format name cannot exceed 255 bytes.");

            Console.WriteLine($"[DnsProvider: EncodeDomainName] Successfully encoded domain: {result.Length} bytes");

            return result;
        }

        //2. أelper to build the 12-byte header 
        private byte[] BuildHeader(out ushort transactionId)
        {

            byte[] result = new byte[12];

            //Generate random transaction id for the query.
            Random random = new Random();

            transactionId = (ushort)random.Next(0, 65536);
            Console.WriteLine($"[DnsProvider: BuildHeader] Generated Transaction ID: 0x{transactionId:X4} ({transactionId})");

            BinaryPrimitives.WriteUInt16BigEndian(result, transactionId);

            //Write the DNS Flags(0x0100) at offset 2.
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(2), 0x0100);
            Console.WriteLine("[DnsProvider: BuildHeader] Set Flags: 0x0100 (Standard Query, RD=1)");

            //Write the QDCOUNT Flag(0x0001) at offset 4.
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(4), 0x0001);
            Console.WriteLine("[DnsProvider: BuildHeader] Set QDCOUNT: 1");

            return result;
        }

        //3. Helper to build the question 
        private byte[] BuildQuestion(byte[] domain, ushort questionType = 0x0001, ushort questionClass = 0x0001)
        {

            //validate the given domain
            if (domain == null)
                throw new ArgumentNullException("[DnsProvider: ConvertRawDomain] Domain can't be null or empty");

            //Calculate the question length 
            int domainLenght = domain.Length;

            byte[] result = new byte[domainLenght + 4];

            //Append the domain to the question
            Array.Copy(domain, 0, result, 0, domainLenght);

            //Appedn the quetion type and question class to the question
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(domain.Length), questionType);
            BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan((domain.Length) + 2), questionClass);

            Console.WriteLine($"[DnsProvider: BuildQuestion] Built question: {result.Length} bytes (QTYPE: 0x{questionType:X4}, QCLASS: 0x{questionClass:X4})");

            return result;
        }

        //4. Build the main orchestrator 
        private byte[] BuildQueryPacket(string domain, out ushort transactionId, ushort questionType = 0x0001, ushort questionClass = 0x0001)
        {
            Console.WriteLine($"[DnsProvider: BuildQueryPacket] Building DNS query packet for '{domain}'");
            //Track the length
            int totalLegnth = 0;

            //Encode the domain.
            var encodedDomain = EncodeDomainName(domain);

            //Build the headers.
            var headers = BuildHeader(out transactionId);

            totalLegnth += headers.Length;

            //Build the question
            var qeustion = BuildQuestion(encodedDomain);

            totalLegnth += qeustion.Length;

            //Create the final bucket
            byte[] bucket = new byte[totalLegnth];

            Array.Copy(headers, 0, bucket, 0, headers.Length);
            Array.Copy(qeustion, 0, bucket, headers.Length, qeustion.Length);

            Console.WriteLine($"[DnsProvider: BuildQueryPacket] DNS query packet assembled: {bucket.Length} bytes (TxID: 0x{transactionId:X4})");

            return bucket;

        }

        //5. Build the bucket sender
        private (DnsHeader Header, byte[] RawResponse) SendQuery(byte[] queryPacket, ushort transactionId, string dnsServer = "8.8.8.8", int port = 53)
        {
            if (queryPacket == null || queryPacket.Length == 0)
                throw new ArgumentNullException(nameof(queryPacket), "[DnsProvider: SendQuery] Query packet can't be null or empty");

            //Setup the response buffer with max size of 512
            byte[] responseBuffer = new byte[512];

            //Setup the UDP socket
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 3000;
            socket.SendTimeout = 3000;

            //Setup the endpoint
            var endPoint = new IPEndPoint(IPAddress.Parse(dnsServer), port);

            //Send the query and receive the response 
            try
            {
                socket.SendTo(queryPacket, endPoint);

                int bytesReceived = socket.Receive(responseBuffer);

                byte[] actualResponse = responseBuffer[..bytesReceived];
                Console.WriteLine($"[DnsProvider: SendQuery] Received {bytesReceived} bytes from {dnsServer}:{port}");

                // Validate the response length
                var header = ParseDnsHeader(actualResponse);

                if (header.TransactionId != transactionId)
                    throw new InvalidDataException($"Transaction ID mismatch: sent 0x{transactionId:X4}, got 0x{header.TransactionId:X4}");

                if (!header.IsResponse)
                    throw new InvalidDataException("Received a query packet instead of a response.");

                if (header.ResponseCode != 0)
                    throw new Exception($"DNS server replied with error code: {header.ResponseCode} (e.g. 3 = NXDOMAIN)");

                return (header, actualResponse);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[DnsProvider: SendQuery] Socket communication error with {dnsServer}:{port}: {ex.Message}");
                throw;
            }
        }


        private DnsHeader ParseDnsHeader(byte[] response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (response.Length < 12)
                throw new InvalidDataException("DNS response header is incomplete.");

            return new DnsHeader
            {
                TransactionId = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(0, 2)),
                Flags = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(2, 2)),

                QuestionCount = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(4, 2)),
                AnswerCount = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(6, 2)),
                AuthorityCount = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(8, 2)),
                AdditionalCount = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(10, 2))
            };
        }

        //6. Helper to parse Answer records from the DNS response
        private List<IPAddress> ParseAnswers(byte[] response, DnsHeader header)
        {
            Console.WriteLine($"[DnsProvider: ParseAnswers] Parsing {header.AnswerCount} answer records from response ({response.Length} bytes)");
            var ipAddresses = new List<IPAddress>();

            if (header.AnswerCount == 0)
            {
                Console.WriteLine("[DnsProvider: ParseAnswers] No answers returned in DNS response");
                return ipAddresses;
            }

            // Step 1: Start immediately after the 12-byte header
            int offset = 12;

            // Step 2: Skip the Question section (QNAME + QTYPE[2] + QCLASS[2])
            while (offset < response.Length && response[offset] != 0x00)
            {
                // If it's a pointer (0xC0), it's 2 bytes
                if ((response[offset] & 0xC0) == 0xC0)
                {
                    offset += 2;
                    break;
                }

                int labelLength = response[offset];
                offset += 1 + labelLength;
            }

            if (offset < response.Length && response[offset] == 0x00)
            {
                offset++; // Skip the 0x00 null byte at the end of the domain name
            }

            offset += 4; // Skip QTYPE (2 bytes) + QCLASS (2 bytes)

            // Step 3: Parse each Answer Resource Record
            for (int i = 0; i < header.AnswerCount; i++)
            {
                if (offset >= response.Length)
                    break;

                // 3a. Read NAME: If it starts with 11xxxxxx (0xC0), it's a 2-byte compression pointer
                if ((response[offset] & 0xC0) == 0xC0)
                {
                    offset += 2;
                }
                else
                {
                    // Literal domain name labels
                    while (offset < response.Length && response[offset] != 0x00)
                    {
                        int len = response[offset];
                        offset += 1 + len;
                    }
                    if (offset < response.Length) offset++; // skip 0x00
                }

                if (offset + 10 > response.Length)
                    break;

                // 3b. Read TYPE (2 bytes), CLASS (2 bytes), TTL (4 bytes), RDLENGTH (2 bytes)
                ushort type = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(offset, 2));
                offset += 2;

                ushort @class = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(offset, 2));
                offset += 2;

                uint ttl = BinaryPrimitives.ReadUInt32BigEndian(response.AsSpan(offset, 4));
                offset += 4;

                ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(offset, 2));
                offset += 2;

                // 3c. If Type == 1 (A record / IPv4) and RDLENGTH == 4, extract the IP!
                if (type == 1 && rdLength == 4 && offset + 4 <= response.Length)
                {
                    byte[] ipBytes = response.AsSpan(offset, 4).ToArray();
                    var ip = new IPAddress(ipBytes);
                    ipAddresses.Add(ip);
                    Console.WriteLine($"[DnsProvider: ParseAnswers] Found IPv4 A Record: {ip} (TTL: {ttl}s)");
                }
                else
                {
                    Console.WriteLine($"[DnsProvider: ParseAnswers] Skipping non-A record (Type: {type}, RDLENGTH: {rdLength})");
                }

                offset += rdLength;
            }

            return ipAddresses;
        }

        // 7. Public orchestrator: Resolves a domain name to IP addresses
        public List<IPAddress> Resolve(string domain, string dnsServer = "8.8.8.8", int port = 53)
        {
            Console.WriteLine($"\n[DnsProvider: Resolve] Resolving domain '{domain}' using DNS server '{dnsServer}'");

            try
            {
                // 1. Build wire-format query packet
                byte[] queryPacket = BuildQueryPacket(domain, out ushort transactionId);

                // 2. Send over UDP and receive validated response
                var (header, rawResponse) = SendQuery(queryPacket, transactionId, dnsServer, port);

                // 3. Parse and extract IP addresses from answer section
                var ips = ParseAnswers(rawResponse, header);

                if (ips.Count == 0)
                    throw new Exception($"[DnsProvider: Resolve] Could not resolve an IPv4 address for '{domain}'");

                Console.WriteLine($"[DnsProvider: Resolve] Successfully resolved '{domain}' -> {string.Join(", ", ips)}");
                return ips;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DnsProvider: Resolve] Failed to resolve '{domain}': {ex.Message}");
                throw;
            }
        }
    }
}
