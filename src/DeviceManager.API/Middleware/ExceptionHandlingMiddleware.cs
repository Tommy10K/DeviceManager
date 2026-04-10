using System.Net;
using DeviceManager.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManager.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = GetStatusCodeAndTitle(exception);

        _logger.LogError(exception, "Request failed with status code {StatusCode}", (int)statusCode);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static (HttpStatusCode statusCode, string title) GetStatusCodeAndTitle(Exception exception)
    {
        return exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ConflictException => (HttpStatusCode.Conflict, "Conflict"),
            BadRequestException => (HttpStatusCode.BadRequest, "Bad request"),
            ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };
    }
}