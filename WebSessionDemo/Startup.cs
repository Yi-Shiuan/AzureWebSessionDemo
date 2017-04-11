using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebSessionDemo.Middlewares;
using WebSessionDemo.Interfaces;
using WebSessionDemo.Services;
using WebSessionDemo.Attributes;


namespace WebSessionDemo
{
    using StackExchange.Redis;

    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICacheService, RedisCacheService>();
            
            services.AddMvc();

            services.AddDistributedRedisCache(option =>
                {
                    option.Configuration = this.Configuration["redis:ConnectionString"];
                    option.InstanceName = this.Configuration["redis:SessionInstance"];
                });

            services.AddSingleton<IDatabase>(
                func =>
                    {
                        var redis = ConnectionMultiplexer.Connect(this.Configuration["redis:ConnectionString"]);
                        return redis.GetDatabase();
                    });
            
            services.AddSession(option =>
            {
                option.CookieHttpOnly = true;
                option.CookieName = ".azure.web.session";
                option.IdleTimeout = TimeSpan.FromSeconds(20);
            });

            services.AddTransient<CacheAttribute>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware<CacheMiddleware>();
            app.UseSession();
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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
