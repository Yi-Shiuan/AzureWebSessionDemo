using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebSessionDemo.Attributes;

namespace WebSessionDemo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            this.HttpContext.Session.SetString("demo", "demo");
            return this.View();
        }

        [Cache(Duration = 10)]
        public IActionResult About()
        {
            this.ViewData["time"] = DateTime.Now;

            return this.View();
        }

        public IActionResult Contact()
        {
            this.ViewData["Message"] = "Your contact page.";

            return this.View();
        }

        public IActionResult Error()
        {
            return this.View();
        }
    }
}
