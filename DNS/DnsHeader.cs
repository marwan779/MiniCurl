using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCurl.DNS
{
    public class DnsHeader
    {
        public ushort TransactionId { get; set; }
        public ushort Flags { get; set; }

        public ushort QuestionCount { get; set; }
        public ushort AnswerCount { get; set; }
        public ushort AuthorityCount { get; set; }
        public ushort AdditionalCount { get; set; }

        public bool IsResponse => (Flags & 0x8000) != 0;
        public bool IsTruncated => (Flags & 0x0200) != 0;
        public bool RecursionAvailable => (Flags & 0x0080) != 0;

        public ushort ResponseCode => (ushort)(Flags & 0x000F);
    }
}
