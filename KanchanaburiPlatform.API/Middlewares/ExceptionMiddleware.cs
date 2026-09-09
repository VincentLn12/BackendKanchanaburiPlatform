using System.Net;
using System.Text.Json;
using API.Errors;

namespace API.Middleware;

public class ExceptionMiddleware(IHostEnvironment env, RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(context, e, env);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception e, IHostEnvironment env)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var message = e.InnerException != null ? $"{e.Message} (Inner: {e.InnerException.Message})" : e.Message;

        var response = env.IsDevelopment()
            ? new ApiErrorResponse(context.Response.StatusCode, message, e.StackTrace)
            : new ApiErrorResponse(context.Response.StatusCode, message, "Internal Server error");

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}