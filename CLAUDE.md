# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

### Building the Solution

```bash
dotnet build LYBTZYZS.sln
```

### Running the WebAPI

```bash
cd LYBT.WebAPI
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000` with Swagger UI at the root path.

### Running Tests

```bash
dotnet test
```

### Database Migrations

**Note: This system uses a unified database architecture with AppDbContext in LYBT.Infrastructure.**

#### Standard Migration Operations

```bash
# Add a new migration (replace MigrationName)
dotnet ef migrations add MigrationName --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# Update database
dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# Remove last migration (if not applied)
dotnet ef migrations remove --project LYBT.Infrastructure --startup-project LYBT.WebAPI
```

#### Complete Database Rebuild

**⚠️ Warning: This will permanently delete ALL data. Use only in development environment.**

When entity models undergo major changes and you need to completely rebuild the database:

```bash
# 1. Drop the entire database (will lose all data)
dotnet ef database drop --project LYBT.Infrastructure --startup-project LYBT.WebAPI --force

# 2. Delete all migration files
rmdir /s /q "LYBT.Infrastructure\Migrations"
# On Linux/Mac: rm -rf "LYBT.Infrastructure/Migrations"

# 3. Create fresh initial migration
dotnet ef migrations add InitialCreate --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# 4. Create new database with updated schema
dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI
```

#### One-Command Database Rebuild

```bash
# Quick rebuild sequence (development only)
dotnet ef database drop --project LYBT.Infrastructure --startup-project LYBT.WebAPI --force && rmdir /s /q "LYBT.Infrastructure\Migrations" && dotnet ef migrations add InitialCreate --project LYBT.Infrastructure --startup-project LYBT.WebAPI && dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI
```

#### When to Use Complete Rebuild

- Major entity model restructuring
- Changing primary key types or relationships
- Database schema architectural changes
- Development phase rapid iteration
- Cleaning up test/development data

#### Production Migration Best Practices

- Always use incremental migrations in production
- Test migrations on development database first
- Backup production database before applying migrations
- Use `dotnet ef migrations script` to generate SQL scripts for review

## Architecture Overview

This is a **modular Traditional Chinese Medicine (TCM) clinic management system** built with ASP.NET Core 8.0 using a clean, modular architecture pattern.

### Core Architecture Principles

- **Modular Design**: Each business domain is implemented as a separate module with its own services, repositories, and models
- **Unified Infrastructure**: Shared infrastructure services for logging, caching, configuration, and authentication
- **Unified Database**: Single AppDbContext in LYBT.Infrastructure manages all business entities
- **Agent-Based Services**: Each module exposes standardized service interfaces for cross-module communication

### Module Structure

Each business module follows this structure:

```
LYBT.Module.[Name]/
├── Models/                  # Domain models and DTOs
├── Services/                # Business logic services
├── Repositories/            # Data access layer
├── Interfaces/              # Service contracts
├── Mapping/                 # AutoMapper profiles
└── [Name]Module.cs         # Module registration
```

**Note: Database context and migrations are centralized in LYBT.Infrastructure.**

### Key Modules

**Core Infrastructure:**

- `LYBT.Infrastructure` - Unified logging, caching, configuration, authentication, and storage services
- `LYBT.Common` - Shared enums, extensions, helpers, and response models
- `LYBT.Models` - Shared domain models and DTOs

**Business Modules:**

- `LYBT.Module.Auth` - Authentication and authorization
- `LYBT.Module.Users` - User account management and roles
- `LYBT.Module.Patients` - Patient records and information management
- `LYBT.Module.Doctors` - Doctor profiles and credentials
- `LYBT.Module.Registration` - Patient registration and appointment booking
- `LYBT.Module.Queueing` - Clinic queue management
- `LYBT.Module.DiagnosisTreatment` - Diagnosis records and treatment plans
- `LYBT.Module.Prescriptions` - Prescription management
- `LYBT.Module.Herbs` - Traditional Chinese herb catalog and inventory
- `LYBT.Module.FormulaTemplates` - Prescription template management
- `LYBT.Module.Pharmacy` - Dispensing and pharmacy operations
- `LYBT.Module.Billing` - Fee calculation and payment processing
- `LYBT.Module.Records` - Medical record management
- `LYBT.Module.TreatmentRoom` - Treatment room and facility management
- `LYBT.Module.Sync` - Data synchronization services
- `LYBT.Module.Settings` - System configuration management

### Service Registration Pattern

Modules are registered in `LYBT.WebAPI/Extensions/ServiceCollectionExtension.cs` using extension methods:

```csharp
public static void AddLybtModules(this IServiceCollection services)
{
    services.AddUsersModuleServices();
    services.AddPatientsModuleServices();
    // ... other modules
}
```

### Database Configuration

The system uses a unified database architecture with a single AppDbContext in LYBT.Infrastructure:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlServer(connectionString, sqlOptions => {
        sqlOptions.MigrationsAssembly("LYBT.Infrastructure");
        sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(30), null);
    });
});
```

### API Controller Pattern

Controllers inherit from `BaseController` and follow RESTful conventions. They use dependency injection to access module services and return standardized `ApiResponse<T>` objects.

### Unified Services

The infrastructure provides these unified services accessible across all modules:

- `IUnifiedLogService` - Centralized logging with audit trails
- `IUnifiedConfigService` - Global configuration management
- `ICacheService` - Distributed caching
- `IFileStorageService` - File storage abstraction
- `IJwtAuthenticationService` - JWT token management

### Development Guidelines

- The system uses a unified AppDbContext for all modules in LYBT.Infrastructure
- All database migrations are centralized in LYBT.Infrastructure/Migrations
- Use AutoMapper for DTO conversions with profiles defined in each module
- Follow the Repository pattern for data access
- Implement comprehensive logging for all business operations
- Use standardized DTOs for API requests/responses
- Maintain separation of concerns between modules
- Follow async/await patterns consistently

### Configuration

- Connection strings and module settings are configured in `appsettings.json`
- Each module can have its own configuration section
- JWT authentication is configured through `JwtOptions`
- User defaults are configurable through `UserOptions`

This architecture supports horizontal scaling, independent module development, and maintains clean separation between business domains while providing shared infrastructure services.