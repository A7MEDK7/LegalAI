using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Service.Abstraction.Contracts;
using System.Text;
using System.Text.Json;

namespace Presentation.Attributes {
    internal class RedisCacheAttribute(int durationInSec = 60) : ActionFilterAttribute {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {

            var cacheService = context.HttpContext.RequestServices.GetRequiredService<IServiceManger>().CacheService;

            string cacheKey = GenerateCacheKey(context);

            var cachedResult = await cacheService.GetCachedValueAsync(cacheKey);

            if (cachedResult != null) {
                context.Result = new ContentResult {
                    Content = cachedResult,
                    ContentType = "application/json",
                    StatusCode = StatusCodes.Status200OK
                };
                return;
            }

            var executedContext = await next.Invoke();

            if (executedContext.Result is OkObjectResult okObject && okObject.Value is not null) {
                await cacheService.SetCacheValueAsync(cacheKey, okObject.Value, TimeSpan.FromSeconds(durationInSec));
            }
        }
        private string GenerateCacheKey(ActionExecutingContext context) {
            var request = context.HttpContext.Request;
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(request.Path);

            // This is useful if someone uses this attribute on a GET Endpoint containing a Query String
            foreach (var item in request.Query.OrderBy(q => q.Key))
                keyBuilder.Append($"|{item.Key}-{item.Value}");

            // This Is The Part That's Actually Important For Your Situation: It Covers Any [FromBody]/[FromForm] Parameter
            // (like AskQueryDTO.Query), Not Just Query Strings
            foreach (var arg in context.ActionArguments.OrderBy(a => a.Key)) {
                var serialized = JsonSerializer.Serialize(arg.Value);
                keyBuilder.Append($"|{arg.Key}={serialized}");
            }

            return keyBuilder.ToString();
        }
    }
}
