using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NirvedBackend.Models.Generic;
using StackExchange.Redis;

namespace NirvedBackend.Helpers;

public class CustomJwtValidate : TypeFilterAttribute
{
    public CustomJwtValidate() : base(typeof(CustomJwtValidateImpl))
    {
    }

    private class CustomJwtValidateImpl(IConnectionMultiplexer redisCache) : IAsyncActionFilter
    {
        private readonly IDatabase _cachingProvider = redisCache.GetDatabase((int)RedisDatabases.UserSession);

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                await next();
                return;
            }
            
            var jtiClaim = context.HttpContext.User.FindFirst("jti");
            var idClaim = context.HttpContext.User.FindFirst("id");
    
            if (jtiClaim == null || idClaim == null)
            {
                context.Result = new JsonResult(new  { error = "Invalid or missing authentication token" })
                {
                    StatusCode = (int)System.Net.HttpStatusCode.Forbidden
                };
                return;
            }

            var cachedToken = await _cachingProvider.StringGetAsync(idClaim.Value);
            if (cachedToken.IsNull)
            {
                context.Result = new JsonResult(new  { error = "Authentication token expired or terminated" })
                {
                    StatusCode = (int)System.Net.HttpStatusCode.Forbidden
                };
            }
            else if (cachedToken.ToString() != jtiClaim.Value)
            {
                context.Result = new JsonResult(new  { error = "Authentication token mismatch" })
                {
                    StatusCode = (int)System.Net.HttpStatusCode.Forbidden
                };
            }
            else
            {
                await next();
            }
        }
    }
}