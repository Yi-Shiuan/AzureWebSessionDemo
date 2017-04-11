using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebSessionDemo.Attributes;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

namespace WebSessionDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDatabase redis;

        public HomeController(IDatabase db)
        {
            this.redis = db;
        }

        public IActionResult Index()
        {
            return this.View();
        }

        [Cache(Duration = 10)]
        public IActionResult About()
        {
            this.ViewData["time"] = DateTime.Now;

            return this.View();
        }

        [Authorize]
        public IActionResult Contact()
        {
            this.ViewData["Message"] = "Your contact page.";

            return this.View();
        }

        public IActionResult SharedSet()
        {
            this.redis.StringSetAsync($"Redis:Shared", "test", TimeSpan.FromSeconds(10), flags: CommandFlags.FireAndForget);
            return this.RedirectToAction("Shared");
        }

        public async Task<IActionResult> Shared()
        {
            this.ViewData["data"] = await this.redis.StringGetAsync($"Redis:Shared");
            return this.View();
        }

        public IActionResult Pub()
        {
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> Pub(string message)
        {
            await this.redis.PublishAsync("AzureRedisPub", message);
            return this.RedirectToAction("Pub");
        }
        
        public IActionResult Error()
        {
            return this.View();
        }
    }
}
