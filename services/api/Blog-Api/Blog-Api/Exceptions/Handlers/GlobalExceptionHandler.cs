using System.Data.Common;
using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Exceptions.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;
        switch (exception)
        {
            case UniqueConstraintException constraintException:
                problemDetails = HandleUniqueConstraintException(constraintException);
                break;
            case DbUpdateConcurrencyException concurrencyException:
                problemDetails = HandleUpdateConcurrencyException(concurrencyException);
                break;
            case DbUpdateException updateException:
                problemDetails = HandleDbUpdateException(updateException);
                break;
            case DbException dbException:
                problemDetails = HandleDbException(dbException);
                break;
            default:
                _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}",
                    httpContext.TraceIdentifier);
                problemDetails = CreateGenericProblemDetails();
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails CreateBaseProblemDetails(int statusCode, string title, string? details = null)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = details
        };
    }

    private static ProblemDetails CreateGenericProblemDetails()
    {
        return CreateBaseProblemDetails(StatusCodes.Status500InternalServerError, "Internal server error",
            "An error occured when processing this request.");
    }

    private ProblemDetails HandleUniqueConstraintException(UniqueConstraintException exception)
    {
        return CreateBaseProblemDetails(StatusCodes.Status409Conflict, "Unique constraint violation",
            "A resource with conflicting data already exists.");
    }

    private ProblemDetails HandleUpdateConcurrencyException(
        DbUpdateConcurrencyException exception)
    {
        return CreateBaseProblemDetails(StatusCodes.Status409Conflict, "Update concurrency error",
            "The resource was modified by another transaction. Please retry.");
    }

    private ProblemDetails HandleDbUpdateException(
        DbUpdateException exception)
    {
        return CreateGenericProblemDetails();
    }

    private ProblemDetails HandleDbException(
        DbException exception)
    {
        return CreateGenericProblemDetails();
    }
}