using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace NirvedBackend.Helpers;

public class ExcelRateLimitPolicy : IRateLimiterPolicy<string>
{
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
            var key=httpContext.User.GetUserId().ToString()+httpContext.Request.Path;
            return RateLimitPartition.GetFixedWindowLimiter(key, partition => new FixedWindowRateLimiterOptions()
            {
               AutoReplenishment = true,
               PermitLimit = 2,
               Window = TimeSpan.FromMinutes(5)
            });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected { get; }= (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        var jsonResult = new { error = "Max 2 excel reports per 5 minutes allowed" };
        context.HttpContext.Response.WriteAsJsonAsync(jsonResult);
        
        return new ValueTask();
    };
}