using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSessionDemo.Interfaces;

namespace WebSessionDemo.Services
{
    using StackExchange.Redis;

    public class RedisCacheService : ICacheService
    {
        protected IDatabase Cache;

        public RedisCacheService(IDatabase cache)
        {
            this.Cache = cache;
        }

        public Task Store<T>(string key, T content, int duration = 60)
        {
            var serializer = new JsonSerializer();
            byte[] data;

            using (var ms = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(ms))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
            {
                JsonSerializer ser = new JsonSerializer();
                ser.Serialize(jsonWriter, content);
                jsonWriter.Flush();
                data = ms.ToArray();
            }

            return this.Cache.StringSetAsync(
                key,
                data,
                TimeSpan.FromSeconds(duration),
                flags: CommandFlags.FireAndForget);
        }

        public async Task<T> Get<T>(string key) where T : class
        {
            var data = await this.Cache.StringGetAsync(key);

            if (data.IsNullOrEmpty)
            {
                return null;
            }

            var serializer = new JsonSerializer();
            T result;

            using (var ms = new MemoryStream(data))
            using (var sr = new StreamReader(ms))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                result = serializer.Deserialize<T>(jsonTextReader);
            }

            return result;
        }
    }
}
