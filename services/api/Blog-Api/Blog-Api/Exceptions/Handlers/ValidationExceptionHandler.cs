using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Exceptions.Handlers;

public class ValidationExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ValidationExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        ValidationProblemDetails problemDetails = new ValidationProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occured",
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"
        };

        foreach (IGrouping<string, ValidationFailure> propertyErrors in validationException.Errors.GroupBy(e =>
                     e.PropertyName))
        {
            problemDetails.Errors[propertyErrors.Key] = propertyErrors.Select(e => e.ErrorMessage).ToArray();
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = validationException,
            ProblemDetails = problemDetails
        });
    }
}