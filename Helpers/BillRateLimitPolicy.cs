using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace NirvedBackend.Helpers;

public class BillRateLimitPolicy: IRateLimiterPolicy<string>
{
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var key=httpContext.User.GetUserId().ToString()+httpContext.Request.Path;
        return RateLimitPartition.GetFixedWindowLimiter(key, partition => new FixedWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(3)
        });
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected { get; }= (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        var jsonResult = new { error = "Max 1 bill request per 3 seconds allowed" };
        context.HttpContext.Response.WriteAsJsonAsync(jsonResult);
        
        return new ValueTask();
    };
}