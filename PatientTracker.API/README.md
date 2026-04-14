# Patient Tracker API

A comprehensive .NET Web API backend for managing patient health records with clean architecture principles.

## Features

- **Authentication & Authorization**: JWT-based authentication with refresh tokens
- **User Management**: Registration, login, logout functionality
- **Patient Profiles**: Complete patient information management
- **Medical Records**: 
  - Medications tracking
  - Lab tests management
  - Radiology scans
  - Diagnoses tracking
  - Surgeries records
- **Timeline View**: Chronological view of all medical events
- **Secure Sharing**: Token-based profile sharing with clinics
- **Data Validation**: Comprehensive input validation with FluentValidation
- **Error Handling**: Global exception handling middleware
- **Logging**: Structured logging with Serilog
- **API Documentation**: OpenAPI/Swagger documentation
- **Database**: MySQL with Entity Framework Core
- **Docker Support**: Complete containerization setup

## Architecture

This project follows **Clean Architecture** principles:

```
src/
  Domain/                 # Core business logic and entities
    Entities/            # Domain models
  Application/           # Application services and DTOs
    DTOs/                # Data transfer objects
    Services/            # Business logic services
    Validators/          # Input validation
  Infrastructure/         # External concerns
    Data/                # Database context and migrations
    Repositories/        # Data access layer
    Services/            # External services (JWT, etc.)
  Presentation/          # API layer
    Controllers/         # API endpoints
    Middleware/          # Custom middleware
  Config/               # Configuration files
```

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- MySQL Server 8.0+
- Docker (optional, for containerized setup)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PatientTracker.API
   ```

2. **Configure database connection**
   
   Update `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=PatientTracker;Uid=root;Pwd=your_password;"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Start the API**
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5000` and Swagger UI at `https://localhost:5000`.

### Docker Setup

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

This will start:
- API at `http://localhost:5000`
- MySQL at `localhost:3306`
- Adminer (DB admin) at `http://localhost:8080`

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | User logout |

### Profile Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/profile` | Get user profile |
| POST | `/api/profile` | Create profile |
| PUT | `/api/profile` | Update profile |
| DELETE | `/api/profile` | Delete profile |

### Medical Records

#### Medications
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/medications` | Get all medications |
| GET | `/api/medications/{id}` | Get specific medication |
| POST | `/api/medications` | Create medication |
| PUT | `/api/medications/{id}` | Update medication |
| DELETE | `/api/medications/{id}` | Delete medication |

#### Lab Tests
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/labtests` | Get all lab tests |
| GET | `/api/labtests/{id}` | Get specific lab test |
| POST | `/api/labtests` | Create lab test |
| PUT | `/api/labtests/{id}` | Update lab test |
| DELETE | `/api/labtests/{id}` | Delete lab test |

#### Radiology
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/radiology` | Get all radiology scans |
| GET | `/api/radiology/{id}` | Get specific scan |
| POST | `/api/radiology` | Create scan |
| PUT | `/api/radiology/{id}` | Update scan |
| DELETE | `/api/radiology/{id}` | Delete scan |

#### Diagnoses
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/diagnoses` | Get all diagnoses |
| GET | `/api/diagnoses/{id}` | Get specific diagnosis |
| POST | `/api/diagnoses` | Create diagnosis |
| PUT | `/api/diagnoses/{id}` | Update diagnosis |
| DELETE | `/api/diagnoses/{id}` | Delete diagnosis |

#### Surgeries
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/surgeries` | Get all surgeries |
| GET | `/api/surgeries/{id}` | Get specific surgery |
| POST | `/api/surgeries` | Create surgery |
| PUT | `/api/surgeries/{id}` | Update surgery |
| DELETE | `/api/surgeries/{id}` | Delete surgery |

### Timeline & Sharing

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/timeline` | Get user timeline |
| GET | `/api/sharedlinks` | Get shared links |
| POST | `/api/sharedlinks` | Create shared link |
| DELETE | `/api/sharedlinks/{id}` | Delete shared link |
| PUT | `/api/sharedlinks/{id}/toggle` | Toggle link status |
| GET | `/api/share/{token}` | Get shared profile (public) |

## API Usage Examples

### Authentication

**Register User:**
```bash
curl -X POST "https://localhost:5000/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'
```

**Login:**
```bash
curl -X POST "https://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'
```

### Using the API

After authentication, include the JWT token in the Authorization header:

```bash
curl -X GET "https://localhost:5000/api/medications" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Configuration

### JWT Settings

Update `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "PatientTracker.API",
    "Audience": "PatientTracker.Client",
    "AccessTokenExpiration": 15,
    "RefreshTokenExpiration": 7
  }
}
```

### Database Connection

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PatientTracker;Uid=root;Pwd=password;"
  }
}
```

## Development

### Adding New Features

1. **Create Entity** in `Domain/Entities/`
2. **Add Repository** in `Infrastructure/Repositories/`
3. **Create Service** in `Application/Services/`
4. **Add Controller** in `Presentation/Controllers/`
5. **Update DbContext** if needed
6. **Create Migration**:
   ```bash
   dotnet ef migrations add MigrationName
   dotnet ef database update
   ```

### Running Tests

```bash
dotnet test
```

### Code Quality

The project follows these practices:
- SOLID principles
- Dependency Injection
- Clean Architecture
- Comprehensive error handling
- Input validation
- Structured logging
- API documentation

## Security

- **Password Hashing**: BCrypt.NET
- **JWT Tokens**: Secure token-based authentication
- **Input Validation**: FluentValidation
- **CORS**: Configured for React frontend
- **HTTPS**: Enabled in production
- **SQL Injection Protection**: EF Core parameterized queries

## Deployment

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Database connection string |
| `Jwt__Key` | JWT signing key |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) |

### Production Deployment

1. **Configure production settings**
2. **Set up database**
3. **Configure HTTPS**
4. **Set up monitoring/logging**
5. **Deploy using Docker or direct hosting**

## Support

For issues and questions:
1. Check the API documentation at `/swagger`
2. Review the logs for detailed error information
3. Ensure database connection is properly configured
4. Verify JWT configuration

## License

This project is licensed under the MIT License.
