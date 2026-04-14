namespace PatientTracker.Application.Common;

public class BusinessException : Exception
{
    public ErrorCodes ErrorCode { get; }
    
    public BusinessException(ErrorCodes errorCode) : base()
    {
        ErrorCode = errorCode;
    }
    
    public BusinessException(ErrorCodes errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public BusinessException(ErrorCodes errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

public class ValidationException : BusinessException
{
    public IDictionary<string, string[]> ValidationErrors { get; }
    
    public ValidationException(IDictionary<string, string[]> validationErrors) 
        : base(ErrorCodes.ValidationError)
    {
        ValidationErrors = validationErrors;
    }
    
    public ValidationException(ErrorCodes errorCode, IDictionary<string, string[]> validationErrors) 
        : base(errorCode)
    {
        ValidationErrors = validationErrors;
    }
}

public class DatabaseException : BusinessException
{
    public DatabaseException(ErrorCodes errorCode) : base(errorCode) { }
    
    public DatabaseException(ErrorCodes errorCode, string message) : base(errorCode, message) { }
    
    public DatabaseException(ErrorCodes errorCode, string message, Exception innerException) : base(errorCode, message, innerException) { }
}
