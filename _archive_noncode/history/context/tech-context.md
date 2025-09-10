---
created: 2025-09-03T13:20:35Z
last_updated: 2025-09-03T13:20:35Z
version: 1.0
author: Claude Code PM System
---

# Technology Context

## Primary Technologies

### Core Framework
- **.NET 8.0** - Latest LTS version
  - C# 12 language features enabled
  - Nullable reference types enabled
  - Global using statements
  - File-scoped namespaces

### Frontend Stack
- **WPF (Windows Presentation Foundation)**
  - MVVM architecture pattern
  - Data binding with INotifyPropertyChanged
  - Dependency injection with DryIoc
  - Prism framework for modularity

### Backend Stack
- **ASP.NET Core 8.0**
  - Web API controllers
  - Minimal API endpoints
  - Middleware pipeline
  - Dependency injection container

### Data Access
- **Entity Framework Core 8.0.17**
  - Code-first approach
  - Migrations support
  - LINQ queries
  - Change tracking
  - Lazy loading proxies

### Database
- **SQL Server**
  - LocalDB for development
  - SQL Server Express for production
  - T-SQL stored procedures (limited use)
  - Database-first migrations

## Key Dependencies

### Frontend Libraries
```xml
<!-- Core MVVM and DI -->
<PackageReference Include="Prism.DryIoc" Version="9.0.537" />
<PackageReference Include="Prism.Core" Version="9.0.537" />
<PackageReference Include="DryIoc.dll" Version="6.0.0" />

<!-- REST Client -->
<PackageReference Include="Refit" Version="8.0.0" />
<PackageReference Include="Refit.HttpClientFactory" Version="8.0.0" />

<!-- Object Mapping -->
<PackageReference Include="AutoMapper" Version="13.0.1" />

<!-- Excel Support -->
<PackageReference Include="EPPlus" Version="8.4.2" />

<!-- JSON -->
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- UI Components -->
<PackageReference Include="MaterialDesignThemes" Version="5.1.0" />
<PackageReference Include="MaterialDesignColors" Version="3.1.0" />
```

### Backend Libraries
```xml
<!-- Entity Framework -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.17" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.17" />

<!-- API Documentation -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.1" />
<PackageReference Include="Swashbuckle.AspNetCore.Swagger" Version="9.0.1" />

<!-- Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.17" />
<PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.2.0" />

<!-- Logging -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.5" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />

<!-- Validation -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
```

### Testing Libraries
```xml
<!-- Unit Testing -->
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />

<!-- Mocking -->
<PackageReference Include="Moq" Version="4.20.72" />

<!-- Test Data -->
<PackageReference Include="Bogus" Version="35.7.1" />

<!-- In-Memory Database -->
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.17" />
```

## Development Tools

### IDEs and Editors
- **Visual Studio 2022** (primary)
  - Version 17.8 or later
  - .NET 8 SDK installed
  - WPF designer support
  - ReSharper (optional)

- **Visual Studio Code** (secondary)
  - C# extension
  - .NET Core extension pack
  - GitLens
  - REST Client

### Build Tools
- **MSBuild** - Solution building
- **dotnet CLI** - Command-line builds
- **PowerShell** - Build scripts
- **Batch files** - Windows automation

### Version Control
- **Git** - Source control
- **GitHub** - Remote repository
- **GitHub CLI** - Command-line operations
- **GitHub Actions** - CI/CD pipelines

### Package Management
- **NuGet** - .NET packages
- **npm** - Node.js packages (for tools)
- **pip** - Python packages (for scripts)

## Architecture Patterns

### Frontend Architecture (UltraThink Dual-Layer)
```
Module (Pure Delegation)
├── QueryService (Complex Queries)
└── BusinessService (Business Logic + CRUD)
```

### Backend Architecture (Traditional 3-Layer)
```
Controller (API Endpoints)
├── Service (Business Logic)
└── Repository (Data Access)
```

### Cross-Cutting Concerns
- **Dependency Injection** - Constructor injection pattern
- **Logging** - Serilog with structured logging
- **Caching** - IMemoryCache for performance
- **Validation** - FluentValidation for complex rules
- **Mapping** - AutoMapper for DTO conversions
- **Error Handling** - Global exception middleware

## API Design

### RESTful Principles
- Resource-based URLs
- HTTP verbs (GET, POST, PUT, DELETE)
- Status codes for responses
- JSON content type
- Versioning support (v1)

### Authentication
- **JWT Bearer Tokens**
  - 8-hour token lifetime
  - 30-day refresh token (Remember Me)
  - Role-based claims (Admin, Doctor)

### Response Format
```json
{
  "success": true,
  "data": {},
  "message": "Operation successful",
  "timestamp": "2025-09-03T13:20:35Z",
  "requestId": "abc-123"
}
```

## Database Design

### Connection String
```
Server=(localdb)\\mssqllocaldb;Database=LYBTDB;Trusted_Connection=True;
```

### Migration Strategy
- Code-first migrations
- Automatic migration on startup (development)
- Manual migrations for production
- Seed data for initial setup

### Performance Optimizations
- Connection pooling (Min=2, Max=20)
- Async database operations
- Indexed queries
- Batch operations with ExecuteUpdate

## Security Measures

### Authentication & Authorization
- JWT token-based authentication
- Role-based access control (RBAC)
- Password hashing with BCrypt
- Session management

### Data Protection
- HTTPS only in production
- SQL injection prevention (EF Core)
- XSS protection
- CSRF tokens

### Logging & Auditing
- User action logging
- API request/response logging
- Error logging with stack traces
- Security audit trail

## Performance Considerations

### Caching Strategy
- Memory caching for frequently accessed data
- 10-minute default expiration
- Cache invalidation on updates
- Statistics tracking

### Database Optimization
- Lazy loading disabled by default
- Eager loading with Include()
- Query optimization with AsNoTracking()
- Batch operations for bulk updates

### API Performance
- Response compression
- Pagination for large datasets
- Async/await throughout
- Connection pooling

## Deployment Configuration

### Development Environment
- LocalDB for database
- IIS Express for hosting
- Hot reload support
- Debug logging enabled

### Production Environment
- SQL Server Express/Standard
- IIS hosting
- Windows Service option
- Production logging levels
- Health check endpoints

## Monitoring & Diagnostics

### Health Checks
- Database connectivity
- Memory usage
- CPU usage
- Disk space
- API endpoints

### Logging Sinks
- Console output
- File rotation (daily)
- Windows Event Log
- Application Insights (optional)

### Performance Metrics
- Request duration
- Database query time
- Cache hit ratio
- Error rates
- User session metrics