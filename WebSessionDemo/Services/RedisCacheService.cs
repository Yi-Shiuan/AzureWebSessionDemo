﻿using Microsoft.Extensions.Caching.Distributed;
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
            byte[] data;

            using (var ms = new MemoryStream())
            using (var writer = new StreamWriter(ms))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                var ser = new JsonSerializer();
                ser.Serialize(jsonWriter, content);
                jsonWriter.Flush();
                data = ms.ToArray();
            }

            return this.Cache.StringSetAsync(
                $"Web:Cache:{key}",
                data,
                TimeSpan.FromSeconds(duration),
                flags: CommandFlags.FireAndForget);
        }

        public async Task<T> Get<T>(string key) where T : class
        {
            var data = await this.Cache.StringGetAsync($"Web:Cache:{key}");

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
