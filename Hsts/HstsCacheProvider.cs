using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MiniCurl.Hsts
{
    public class HstsCacheProvider
    {
        private readonly string _cacheFilePath = "D:\\WorkSpace\\C#\\The Full Picture Roadmap\\MiniCurl\\Hsts\\HSTSStorage.json";

        public HstsCacheProvider() 
        {
            Console.WriteLine($"[HstsCacheProvider: Constructor] Initializing HSTS cache from '{_cacheFilePath}'");
            try
            {
                if (!File.Exists(_cacheFilePath) || new FileInfo(_cacheFilePath).Length == 0)
                {
                    Console.WriteLine("[HstsCacheProvider: Constructor] Cache file is empty or missing -> writing default '[]'");
                    File.WriteAllText(_cacheFilePath, "[]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HstsCacheProvider: Constructor] Error initializing cache file: {ex.Message}");
            }
        }

        public List<HstsCacheItem> GetAll()
        {
            Console.WriteLine("[HstsCacheProvider: GetAll] Deserializing all cached HSTS items");
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return new List<HstsCacheItem>();

                string json = File.ReadAllText(_cacheFilePath);
                return JsonSerializer.Deserialize<List<HstsCacheItem>>(json) ?? new List<HstsCacheItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HstsCacheProvider: GetAll] Error reading HSTS cache: {ex.Message}");
                return new List<HstsCacheItem>();
            }
        }

        public HstsCacheItem? Get(string host)
        {
            Console.WriteLine($"[HstsCacheProvider: Get] Querying HSTS cache for host: '{host}'");
            try
            {
                var items = GetAll();
                HstsCacheItem? hstsCacheItem = items.FirstOrDefault(i => 
                    string.Equals(i.Host, host, StringComparison.OrdinalIgnoreCase) || 
                    (i.IncludeSubDomains && host.EndsWith("." + i.Host, StringComparison.OrdinalIgnoreCase)));

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
            catch (Exception ex)
            {
                Console.WriteLine($"[HstsCacheProvider: Get] Error querying HSTS cache for '{host}': {ex.Message}");
                return null;
            }
        }

        public HstsCacheItem? Set(HstsCacheItem item)
        {
            if (item == null)
            {
                Console.WriteLine("[HstsCacheProvider: Set] Item cannot be null");
                return null;
            }

            Console.WriteLine($"[HstsCacheProvider: Set] Attempting to set HSTS entry for host: '{item.Host}'");
            try
            {
                var items = GetAll();

                items.RemoveAll(x => string.Equals(x.Host, item.Host, StringComparison.OrdinalIgnoreCase));
                items.Add(item);

                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_cacheFilePath, json);

                Console.WriteLine($"[HstsCacheProvider: Set] Successfully saved HSTS entry for '{item.Host}' to disk");

                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HstsCacheProvider: Set] Error saving HSTS entry for '{item.Host}': {ex.Message}");
                return null;
            }
        }

        public void Delete(string host)
        {
            Console.WriteLine($"[HstsCacheProvider: Delete] Deleting HSTS entry for host: '{host}'");
            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"[HstsCacheProvider: Delete] Error deleting HSTS entry for '{host}': {ex.Message}");
            }
        }
    }
}
