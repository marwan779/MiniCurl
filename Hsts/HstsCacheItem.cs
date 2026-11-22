using System;
using System.Collections.Generic;
using System.Text;

namespace MiniCurl.Hsts
{
    public class HstsCacheItem
    {
        public string Host { get;} = string.Empty;
        public DateTimeOffset ExpiresAt { get; private set; }
        public bool IncludeSubDomains { get; private set; }
        public DateTimeOffset LastObservedAt { get; private set; }

        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

        public HstsCacheItem(
           string host,
           TimeSpan maxAge,
           bool includeSubDomains = false)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host cannot be null or empty.", nameof(host));

            Host = host.Trim().ToLowerInvariant();
            IncludeSubDomains = includeSubDomains;
            LastObservedAt = DateTimeOffset.UtcNow;
            ExpiresAt = DateTimeOffset.MaxValue;
        }

    }
}
