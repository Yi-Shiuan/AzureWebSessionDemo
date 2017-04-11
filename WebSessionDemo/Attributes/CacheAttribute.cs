using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebSessionDemo.Interfaces;

namespace WebSessionDemo.Attributes
{
    using Microsoft.AspNetCore.Routing;

    public class CacheAttribute : ActionFilterAttribute
    {
        protected ICacheService CacheService { get; set; }

        public int Duration { get; set; }
        
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            this.GetServices(context);
            var cacheKey = $"{context.HttpContext.Request.HttpContext.GetRouteData().Values["controller"]}{context.HttpContext.Request.HttpContext.GetRouteData().Values["action"]}";
            var httpResponse = context.HttpContext.Response;
            var responseStream = httpResponse.Body;

            responseStream.Seek(0, SeekOrigin.Begin);

            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8, true, 512, true))
            {
                var toCache = streamReader.ReadToEnd();
                var contentType = httpResponse.ContentType;
                var statusCode = httpResponse.StatusCode.ToString();
                Task.Factory.StartNew(() =>
                {
                    this.CacheService.Store(cacheKey + "_contentType", contentType, this.Duration);
                    this.CacheService.Store(cacheKey + "_statusCode", statusCode, this.Duration);
                    this.CacheService.Store(cacheKey, toCache, this.Duration);
                });

            }

            base.OnResultExecuted(context);
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ContentResult)
            {
                context.Cancel = true;
            }

            base.OnResultExecuting(context);
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            this.GetServices(context);
            var cacheKey = $"{context.HttpContext.Request.HttpContext.GetRouteData().Values["controller"]}{context.HttpContext.Request.HttpContext.GetRouteData().Values["action"]}";
            var cachedResult = await this.CacheService.Get<string>(cacheKey);
            var contentType = await this.CacheService.Get<string>(cacheKey + "_contentType");
            var statusCode = await this.CacheService.Get<string>(cacheKey + "_statusCode");
            if (!string.IsNullOrEmpty(cachedResult) && !string.IsNullOrEmpty(contentType) &&
                !string.IsNullOrEmpty(statusCode))
            {
                //cache hit
                var httpResponse = context.HttpContext.Response;
                httpResponse.ContentType = contentType;
                httpResponse.StatusCode = Convert.ToInt32(statusCode);

                var responseStream = httpResponse.Body;
                responseStream.Seek(0, SeekOrigin.Begin);
                if (responseStream.Length <= cachedResult.Length)
                {
                    responseStream.SetLength((long)cachedResult.Length << 1);
                }
                using (var writer = new StreamWriter(responseStream, Encoding.UTF8, 4096, true))
                {
                    writer.Write(cachedResult);
                    writer.Flush();
                    responseStream.Flush();
                    context.Result = new ContentResult { Content = cachedResult };
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }

        protected void GetServices(FilterContext context)
        {
            this.CacheService = context.HttpContext.RequestServices.GetService(typeof(ICacheService)) as ICacheService;
        }
    }
}
