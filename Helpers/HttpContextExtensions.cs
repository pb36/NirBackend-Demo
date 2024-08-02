using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace NirvedBackend.Helpers;

public static class HttpContextExtensions
{
    public static string GetRemoteIpAddress(this HttpContext context, bool allowForwarded = true)
    {
        if (!allowForwarded) return context.Connection.RemoteIpAddress?.ToString();
        var header = (context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault());
        return IPAddress.TryParse(header, out var ip) ? ip.ToString() : context.Connection.RemoteIpAddress?.ToString();
    }
}