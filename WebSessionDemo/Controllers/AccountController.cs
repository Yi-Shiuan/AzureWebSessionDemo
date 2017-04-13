using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace WebSessionDemo.Controllers
{
    public class AccountController : Controller
    {
        public async Task<IActionResult> Login()
        {
            var claims = new List<Claim>
        {
            new Claim("Read", "true"),
            new Claim(ClaimTypes.Name, "test"),
            new Claim(ClaimTypes.Sid, "12345")
        };
            HttpContext.Session.Set("test", Encoding.UTF8.GetBytes("test"));
            var claimsIdentity = new ClaimsIdentity(claims, "password");
            var claimsPrinciple = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.Authentication.SignInAsync("Cookies", claimsPrinciple);
            return Redirect("/");
        }
    }
}