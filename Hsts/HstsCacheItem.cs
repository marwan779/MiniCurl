using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MiniCurl.Hsts
{
    public class HstsCacheItem
    {
        [JsonInclude]
        public string Host { get;} = string.Empty;
        [JsonInclude]
        public DateTimeOffset ExpiresAt { get; private set; }
        [JsonInclude]
        public bool IncludeSubDomains { get; private set; }
        [JsonInclude]
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

        [JsonConstructor]
        public HstsCacheItem(
        string host,
        DateTimeOffset expiresAt,
        bool includeSubDomains,
        DateTimeOffset lastObservedAt)
        {
            Host = host;
            ExpiresAt = expiresAt;
            IncludeSubDomains = includeSubDomains;
            LastObservedAt = lastObservedAt;
        }

    }
}
