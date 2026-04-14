using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PatientTracker.Infrastructure.Data;
using PatientTracker.Infrastructure.Repositories;
using PatientTracker.Infrastructure.Services;
using PatientTracker.Application.Services;
using PatientTracker.Application.Validators;
using PatientTracker.Application.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Reflection;
using Serilog;
using PatientTracker.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddMvc()
    .AddDataAnnotationsLocalization()
    .AddViewLocalization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Patient Tracker API", 
        Version = "v1",
        Description = "A comprehensive patient health records management API"
    });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML Comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:8081")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<ILabTestService, LabTestService>();
builder.Services.AddScoped<IRadiologyService, RadiologyService>();
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();
builder.Services.AddScoped<ISurgeryService, SurgeryService>();
builder.Services.AddScoped<ISharedLinkService, SharedLinkService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<ILabTestRepository, LabTestRepository>();
builder.Services.AddScoped<IRadiologyRepository, RadiologyRepository>();
builder.Services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
builder.Services.AddScoped<ISurgeryRepository, SurgeryRepository>();
builder.Services.AddScoped<ISharedLinkRepository, SharedLinkRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Patient Tracker API v1");
        c.RoutePrefix = string.Empty; // Sets Swagger UI at app's root
    });
}

app.UseHttpsRedirection();

// Custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

try
{
    Log.Information("Starting Patient Tracker API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
