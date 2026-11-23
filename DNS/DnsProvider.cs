using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MiniCurl.DNS
{
    public class DnsProvider
    {
        // 1. helper to convert the raw domain (e.g. google.com) to DNS wire fromat 06 67 6F 6F 67 6C 65 03 63 6F 6D 00)

        private byte[] ConvertRawDomain(string domain)
        {
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


            return result;
        }
    }
}
