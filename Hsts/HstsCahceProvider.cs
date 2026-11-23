using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MiniCurl.Hsts
{
    public class HstsCahceProvider
    {
        private readonly string _cacheFilePath = "D:\\WorkSpace\\C#\\The Full Picture Roadmap\\MiniCurl\\Hsts\\HSTSStorage.json";
        private readonly string _json = null;
        public HstsCahceProvider() 
        {
            Console.WriteLine($"[HstsCacheProvider: Constructor] Initializing HSTS cache from '{_cacheFilePath}'");
            _json = File.ReadAllText(_cacheFilePath);
            if (new FileInfo(_cacheFilePath).Length == 0)
            {
                Console.WriteLine("[HstsCacheProvider: Constructor] Cache file is empty -> writing default '[]'");
                File.WriteAllText(_cacheFilePath, "[]");
            }
        }
        public List<HstsCacheItem>? GetAll()
        {
            Console.WriteLine("[HstsCacheProvider: GetAll] Deserializing all cached HSTS items");
            return JsonSerializer.Deserialize<List<HstsCacheItem>>(_json)
           ?? null;
        }

        public HstsCacheItem? Get(string host)
        {
            Console.WriteLine($"[HstsCacheProvider: Get] Querying HSTS cache for host: '{host}'");
            HstsCacheItem? hstsCacheItem = JsonSerializer.Deserialize<List<HstsCacheItem>>(_json).Where(i => i.Host.Contains(host)).FirstOrDefault();

            if (hstsCacheItem != null)
            {
                Console.WriteLine($"[HstsCacheProvider: Get] Found HSTS entry for '{host}' (ExpiresAt: {hstsCacheItem.ExpiresAt}, IncludeSubDomains: {hstsCacheItem.IncludeSubDomains})");
            }
            else
            {
                Console.WriteLine($"[HstsCacheProvider: Get] No HSTS entry found for '{host}'");
            }

            return hstsCacheItem;
        }

        public HstsCacheItem? Set(HstsCacheItem item)
        {
            if (item == null)
            {
                Console.WriteLine("[HstsCacheProvider: Set] Item cannot be null");
                return null;
            }

            Console.WriteLine($"[HstsCacheProvider: Set] Attempting to set HSTS entry for host: '{item.Host}'");

            if (Get(item.Host) == null)
            {
                Console.WriteLine($"[HstsCacheProvider: Set] Entry for '{item.Host}' does not exist");

                return null;
            }

            var items = GetAll();

            items.Add(item);

            string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_cacheFilePath, json);

            Console.WriteLine($"[HstsCacheProvider: Set] Successfully saved HSTS entry for '{item.Host}' to disk");

            return item;

        }

        public void Delete(string host)
        {
            Console.WriteLine($"[HstsCacheProvider: Delete] Deleting HSTS entry for host: '{host}'");
            var items = GetAll();

            Console.WriteLine($"[HstsCacheProvider: Delete] File: {_cacheFilePath}");
            Console.WriteLine($"[HstsCacheProvider: Delete] Requested host: '{host}'");
            Console.WriteLine($"[HstsCacheProvider: Delete] Items loaded: {items.Count}");

            foreach (var item in items)
            {
                Console.WriteLine($"[HstsCacheProvider: Delete] Stored host: '{item.Host}'");
            }

            int removedCount = items.RemoveAll(
                x => string.Equals(
                    x.Host?.Trim(),
                    host?.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );

            Console.WriteLine($"[HstsCacheProvider: Delete] Items removed: {removedCount}");

            string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_cacheFilePath, json);

            Console.WriteLine($"[HstsCacheProvider: Delete] Remaining items: {items.Count}");
        }

    }
}
