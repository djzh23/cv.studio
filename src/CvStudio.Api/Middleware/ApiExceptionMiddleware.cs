using Microsoft.AspNetCore.Mvc;
using Npgsql;
using CvStudio.Application.Exceptions;

namespace CvStudio.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (UnprocessableEntityException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity, "Validation Failed", string.Join("; ", ex.Errors));
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error for {Path}", context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Database Unavailable",
                "Database is unavailable or not initialized. Check PostgreSQL connection and migrations.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            var detail = _environment.IsDevelopment()
                ? $"An unexpected error occurred. {ex.GetType().Name}: {ex.Message}"
                : "An unexpected error occurred.";
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Server Error", detail);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
