namespace PatientTracker.Application.Common;

public enum ErrorCodes
{
    // Authentication & Authorization
    InvalidCredentials = 1001,
    UserNotFound = 1002,
    UserAlreadyExists = 1003,
    InvalidToken = 1004,
    TokenExpired = 1005,
    AccessDenied = 1006,
    
    // Validation
    ValidationError = 2001,
    RequiredField = 2002,
    InvalidEmail = 2003,
    InvalidPassword = 2004,
    InvalidDate = 2005,
    
    // Business Logic
    ResourceNotFound = 3001,
    DuplicateResource = 3002,
    InvalidOperation = 3003,
    InsufficientPermissions = 3004,
    
    // Profile
    ProfileNotFound = 3101,
    ProfileAlreadyExists = 3102,
    
    // Medication
    MedicationNotFound = 3201,
    
    // Lab Test
    LabTestNotFound = 3301,
    
    // Radiology
    RadiologyNotFound = 3401,
    
    // Diagnosis
    DiagnosisNotFound = 3501,
    
    // Surgery
    SurgeryNotFound = 3601,
    
    // Shared Link
    SharedLinkNotFound = 3701,
    
    // Document
    DocumentNotFound = 3801,
    
    // Database
    DatabaseError = 4001,
    DatabaseConnectionError = 4002,
    DatabaseTimeout = 4003,
    ConstraintViolation = 4004,
    
    // System
    UnexpectedError = 5001,
    ServiceUnavailable = 5002,
    FileNotFound = 5003,
    ConfigurationError = 5004
}
