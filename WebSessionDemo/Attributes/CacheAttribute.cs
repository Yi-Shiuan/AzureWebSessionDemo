using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSessionDemo.Interfaces;

namespace WebSessionDemo.Attributes
{
    public class CacheAttribute : ActionFilterAttribute
    {
        public ICacheService CacheService { set; get; }

        public int Duration { set; get; }
        
        public override void OnResultExecuted(ResultExecutedContext context)
        {
            var cacheKey = context.HttpContext.Request.GetEncodedUrl();
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
                    CacheService.Store(cacheKey + "_contentType", contentType, Duration);
                    CacheService.Store(cacheKey + "_statusCode", statusCode, Duration);
                    CacheService.Store(cacheKey, toCache, Duration);
                });

            }
            base.OnResultExecuted(context);
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var requestUrl = context.HttpContext.Request.GetEncodedUrl();
            var cacheKey = requestUrl;
            var cachedResult = await CacheService.Get<string>(cacheKey);
            var contentType = await CacheService.Get<string>(cacheKey + "_contentType");
            var statusCode = await CacheService.Get<string>(cacheKey + "_statusCode");
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
            else
            {
                //cache miss
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}
