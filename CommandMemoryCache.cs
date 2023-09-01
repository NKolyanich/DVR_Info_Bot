using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Metaler.DVR_Info_Bot
{
    public class CommandMemoryCache<TItem>
    {
        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public TItem GetOrCreate(object key, Func<TItem> createItem)
        {
            TItem cacheEntry;
            if (!_cache.TryGetValue(key, out cacheEntry)) // Ищем ключ в кэше.
            {
                // Ключ отсутствует в кэше, поэтому получаем данные.
                cacheEntry = createItem();

                // Сохраняем данные в кэше. 
                _cache.Set(key, cacheEntry);
            }
            return cacheEntry;
        }

        public TItem Get(object key)
        {
            TItem cacheEntry;
            if (_cache.TryGetValue(key, out cacheEntry)) // 
                _cache.Get(key);
            return cacheEntry;
        }

        public void Remove(object key)
        {
            _cache.Remove(key);
        }
    }
}
