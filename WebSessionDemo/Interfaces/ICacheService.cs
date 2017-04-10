using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSessionDemo.Interfaces
{
    public interface ICacheService
    {
        Task Store<T>(string key, T content, int duration);

        Task<T> Get<T>(string key) where T : class;
    }
}
