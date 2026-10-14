using System.Net;
using System.Text.Json;
using TicketShow.Application.Common.Exceptions;
using TicketShow.Application.Common.Models;

namespace TicketShow.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        var response = new ApiResponse<object>();

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Message = "Dữ liệu gửi lên không hợp lệ.";
                response.Errors = validationEx.Errors.SelectMany(x => x.Value).ToList();
                break;

            case AppException appEx:
                context.Response.StatusCode = appEx.StatusCode;
                response.Success = false;
                response.Message = appEx.Message;
                break;

            default:
                _logger.LogError(exception, "Unhandled Exception occurred: {Message}", exception.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Success = false;
                response.Message = "Đã xảy ra lỗi máy chủ nội bộ. Vui lòng thử lại sau.";
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
