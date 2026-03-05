using Microsoft.AspNetCore.Mvc;
using Npgsql;
using CvStudio.Application.Exceptions;

namespace CvStudio.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred.");
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

