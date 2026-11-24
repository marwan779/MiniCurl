using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
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

        //4. build the main orchestrator 
        public byte[] BuildQueryPacket(string domain, ushort questionType = 0x0001, ushort questionClass = 0x0001)
        {
            Console.WriteLine($"[DnsProvider: BuildQueryPacket] Building DNS query packet for '{domain}'");
            //Track the length
            int totalLegnth = 0;

            //Encode the domain.
            var encodedDomain = EncodeDomainName(domain);

            totalLegnth += encodedDomain.Length;

            //Build the headers.
            ushort transactionId;
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
    }
}
