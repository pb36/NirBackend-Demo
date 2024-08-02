using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
// using Sentry;

namespace NirvedBackend.Helpers;

public class ErrorHandlerMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            
          

            response.StatusCode = error switch
            {
                AppException =>
                    (int)HttpStatusCode.BadRequest,
                KeyNotFoundException =>
                    (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };
            
            // if (response.StatusCode == (int)HttpStatusCode.InternalServerError)
            // {
            //     SentrySdk.CaptureException(error);
            // }
            
            var result = JsonSerializer.Serialize(new { error = error.Message });
            await response.WriteAsync(result);
        }
    }
}