using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using WebSessionDemo.Middlewares;
using WebSessionDemo.Interfaces;
using WebSessionDemo.Services;
using WebSessionDemo.Attributes;
using Microsoft.Extensions.Caching.Redis;

namespace WebSessionDemo
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDistributedCache>(factory =>
            {
                var cache = new RedisCache(new RedisCacheOptions
                {
                    Configuration = Configuration["redis:ConnectionString"],
                    InstanceName = Configuration["redis:Instance"]
                });

                return cache;
            });

            services.AddSingleton<ICacheService, RedisCacheService>();
           
            services.AddMvc();

            services.AddDistributedRedisCache(option =>
            {
                option.Configuration = Configuration["redis:ConnectionString"];
                option.InstanceName = Configuration["redis:Instance"];
            });

            services.AddSession(option =>
            {
                option.CookieHttpOnly = true;
                option.CookieName = "azure.websession";
                option.IdleTimeout = TimeSpan.FromSeconds(20);
            });

            services.AddTransient<CacheAttribute>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware<CacheMiddleware>();
            app.UseSession();
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
