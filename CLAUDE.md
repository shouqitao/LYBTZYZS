# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)** - Enterprise-grade Traditional Chinese Medicine clinic management system built with .NET 8, featuring a hybrid architecture design.

**Current Status**: 🎆 Production Ready | ✅ Zero Compilation Errors | ✅ IService Unified Interface Architecture

## High-Level Architecture

### Technology Stack
- **Backend**: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0.17, SQL Server
- **Frontend**: WPF (.NET 8), Prism.DryIoc 9.0.537, Refit (type-safe REST client)
- **Authentication**: JWT Bearer Token with type-safe UserRole enum
- **Architecture Pattern**:
  - Frontend: UltraThink dual-layer architecture
  - Backend: Traditional three-layer architecture

### Core Business Modules (8)
1. **Auth** - Authentication & Authorization (JWT + RBAC)
2. **Users** - User Management (Doctor/Admin roles)
3. **Patients** - Patient Records & Medical History
4. **MedicalCase** - Medical Cases (Consultation container, 1:1 relationship)
5. **Consultation** - TCM Diagnosis (Four Diagnostic Methods: 望闻问切)
6. **Prescriptions** - Prescription Management (Smart Compatibility)
7. **Herbs** - Herbal Medicine Management (Prescription-only, no inventory)
8. **Formula** - Formula Management (Classic prescription templates)

## Common Development Commands

### Building
```bash
# Build solutions
dotnet build LYBT.Server.sln     # Backend
dotnet build LYBT.Desktop.sln    # Frontend
dotnet build LYBT.All.sln        # Complete solution

# Run development server
dotnet run --project src/Server/Services/LYBT.WebAPI
```

### Database Management
```bash
# Add migration - MUST use Infrastructure project
dotnet ef migrations add [MigrationName] --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI

# Update database
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

### Testing
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture Patterns

### Frontend UltraThink Dual-Layer Architecture

All frontend modules follow this pattern:

```csharp
// Main Module (Pure Delegation)
public class UserModule : IUserService
{
    private readonly IUserQueryService _queryService;
    private readonly IUserBusinessService _businessService;

    // Pure delegation to appropriate service
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
        => await _queryService.SearchUsersAsync(criteria);

    public async Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto)
        => await _businessService.CreateUserAsync(dto);
}

// QueryService - Complex queries and searches
public class UserQueryService : IUserQueryService
{
    // Search, filter, statistics, reports
}

// BusinessService - Business logic and CRUD
public class UserBusinessService : IUserBusinessService
{
    // Business processes, CRUD operations, validations
}
```

### Backend Traditional Three-Layer Architecture

```csharp
// Controller Layer - RESTful API endpoints
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
}

// Service Layer - Business logic
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
}

// Repository Layer - Data access with EF Core
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
}
```

## Critical Development Rules

### 1. Architecture Standards (MANDATORY)

- **Frontend**: ALL modules MUST follow `Module (delegation) + QueryService + BusinessService` architecture
- **Interface Unity**: ALL Modules MUST only implement IService interface, NO duplicate IModule interfaces
- **Dependency Injection**: ALL ViewModels MUST inject IService interface, NOT concrete Module types
- **Backend**: Maintain traditional Repository + Service + Controller pattern

### 2. Security Standards

- **Zero SQL Injection**: MUST use LINQ queries + EF Core parameterized queries, NO raw SQL
- **Unified Data Access**: ALL modules share AppDbContext, NO independent database connections
- **JWT Authentication**: 8-hour expiry + Remember Me 30 days
- **RBAC Permissions**: Only support Doctor/Admin roles

### 3. API Design Standards

- **RESTful Naming**: Lowercase convention (e.g., `/api/v1/users`)
- **Unified Response**: ALL APIs MUST use `ApiResponse<T>` format
- **Exception Handling**: Complete exception handling with standardized error returns

### 4. Code Quality Standards

- **File Size**: Never create files over 500 lines
- **Async/Await**: ALL database operations MUST be async
- **Null Checks**: Use null-conditional operators and null-coalescing
- **Comments**: Code should be self-documenting; add comments only for complex logic

## Project Structure

```
src/
├── Server/                      # Backend (11 projects)
│   ├── Core/
│   │   ├── LYBT.Infrastructure/    # Unified AppDbContext, all migrations here
│   │   └── LYBT.Entities/          # Entity models
│   ├── Modules/                    # 8 business modules
│   └── Services/LYBT.WebAPI/       # Web API entry point
├── Client/Desktop/              # WPF Client (17 projects)
│   ├── Modules/                 # 8 business modules (UltraThink architecture)
│   └── Shell/                   # Application shell
└── Shared/                      # Shared components (3 projects)
    ├── LYBT.Shared.Models/      # DTOs and models
    ├── LYBT.Shared.Interfaces/  # Service interfaces
    └── LYBT.Shared.Utilities/   # Utility classes (72 methods)
```

## Development Environment

- **Database**: SQL Server (localhost/LYBTDB)
- **API Port**: http://localhost:5001 (development)
- **Default Login**: sysadmin / LybtAdmin2025@SecurePass!
- **JWT Expiry**: 8 hours (Remember Me: 30 days)

## Common Pitfalls to Avoid

1. **DO NOT** create IModule interfaces - use IService instead
2. **DO NOT** inject concrete Module types in ViewModels - use IService interfaces
3. **DO NOT** use raw SQL - always use LINQ with EF Core
4. **DO NOT** create Helper classes - use QueryService/BusinessService pattern
5. **DO NOT** mix business logic in Controllers - keep them thin
6. **DO NOT** create files in root directory - follow project structure

## Testing Guidelines

- **Unit Tests**: Use xUnit with FluentAssertions
- **Mocking**: Use Moq for dependencies
- **Database**: Use InMemory database for unit tests
- **Test Data**: Use Bogus for consistent test data generation
- **Coverage Goal**: 60% minimum (current: 2.76%)

## AutoMapper Configuration

IMPORTANT: AutoMapper 15.0.1 requires ILoggerFactory parameter:

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new MappingProfile());
}, NullLoggerFactory.Instance);  // Required second parameter
```

## Git Commit Standards

```bash
# Format: <type>: <subject>
# Types:
feat: New feature
fix: Bug fix
docs: Documentation update
refactor: Code refactoring
test: Test-related changes
chore: Build/tool changes
```

## Documentation

Key documentation files:
- `/docs/requirements/` - System requirements (authoritative source)
- `/docs/architecture/` - Architecture design documents
- `/docs/development/` - Development guides
- `/docs/ultrathink/` - UltraThink methodology documentation
- `/CLAUDE.md` - This file (AI assistant guidance)

## Special Notes

1. **Language**: All responses and displays in Chinese (中文)
2. **Database**: SQL Server (not LocalDB)
3. **Development**: Use Visual Studio for running projects
4. **Scripts**: Python scripts preferred for automation

## Recent Achievements (2025)

- ✅ Interface unification completed - removed 8 duplicate IModule interfaces
- ✅ Frontend UltraThink architecture refactoring completed - 15,000 lines reduced
- ✅ Zero compilation warnings and errors achieved
- ✅ 48 projects in production-ready state
- ✅ Enterprise-grade security with separated admin authentication