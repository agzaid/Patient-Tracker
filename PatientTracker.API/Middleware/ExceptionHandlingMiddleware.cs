using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.Common;
using PatientTracker.API.Resources;
using Serilog;

namespace PatientTracker.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IStringLocalizer<ErrorMessages> localizer)
    {
        _next = next;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogExceptionAsync(ex);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task LogExceptionAsync(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validationEx:
                Log.Warning("Validation exception occurred: {Errors}", validationEx.ValidationErrors);
                break;
                
            case DatabaseException dbEx:
                Log.Error(dbEx, "Database exception occurred: {ErrorCode} - {Message}", dbEx.ErrorCode, dbEx.Message);
                break;
                
            case BusinessException businessEx:
                Log.Warning("Business exception occurred: {ErrorCode} - {Message}", businessEx.ErrorCode, businessEx.Message);
                break;
                
            case DbUpdateException dbUpdateEx:
                Log.Error(dbUpdateEx, "Database update exception occurred");
                break;
                
            case TimeoutException timeoutEx:
                Log.Error(timeoutEx, "Timeout exception occurred");
                break;
                
            case NullReferenceException nullEx:
                Log.Error(nullEx, "Null reference exception occurred at {StackTrace}", nullEx.StackTrace);
                break;
                
            case UnauthorizedAccessException authEx:
                Log.Warning("Unauthorized access attempt: {Message}", authEx.Message);
                break;
                
            default:
                Log.Error(exception, "Unexpected exception occurred: {ExceptionType} - {Message}", exception.GetType().Name, exception.Message);
                break;
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json";
        
        var response = exception switch
        {
            ValidationException validationEx => CreateValidationErrorResponse(validationEx),
            DatabaseException dbEx => CreateErrorResponse(dbEx.ErrorCode, dbEx.Message, HttpStatusCode.InternalServerError),
            BusinessException businessEx => CreateErrorResponse(businessEx.ErrorCode, businessEx.Message, HttpStatusCode.BadRequest),
            DbUpdateException => CreateErrorResponse(ErrorCodes.DatabaseError, _localizer["DatabaseError"], HttpStatusCode.InternalServerError),
            TimeoutException => CreateErrorResponse(ErrorCodes.DatabaseTimeout, _localizer["DatabaseTimeout"], HttpStatusCode.InternalServerError),
            UnauthorizedAccessException => CreateErrorResponse(ErrorCodes.AccessDenied, _localizer["AccessDenied"], HttpStatusCode.Unauthorized),
            KeyNotFoundException => CreateErrorResponse(ErrorCodes.ResourceNotFound, _localizer["ResourceNotFound"], HttpStatusCode.NotFound),
            NullReferenceException => CreateErrorResponse(ErrorCodes.UnexpectedError, _localizer["UnexpectedError"], HttpStatusCode.InternalServerError),
            _ => CreateErrorResponse(ErrorCodes.UnexpectedError, _localizer["UnexpectedError"], HttpStatusCode.InternalServerError)
        };

        context.Response.StatusCode = response.statusCode;
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonResponse = JsonSerializer.Serialize(response.responseObj, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    private (object responseObj, int statusCode) CreateErrorResponse(ErrorCodes errorCode, string message, HttpStatusCode statusCode)
    {
        var localizedMessage = _localizer[errorCode.ToString()].Value ?? message;
        return (new { error = localizedMessage, errorCode = (int)errorCode }, (int)statusCode);
    }

    private (object responseObj, int statusCode) CreateValidationErrorResponse(ValidationException validationEx)
    {
        return (new { 
            error = _localizer["ValidationError"].Value,
            errorCode = (int)validationEx.ErrorCode,
            validationErrors = validationEx.ValidationErrors 
        }, (int)HttpStatusCode.BadRequest);
    }
}
