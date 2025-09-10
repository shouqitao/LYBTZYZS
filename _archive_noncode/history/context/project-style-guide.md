---
created: 2025-09-03T13:20:35Z
last_updated: 2025-09-03T13:20:35Z
version: 1.0
author: Claude Code PM System
---

# Project Style Guide

## Code Style Standards

### C# Coding Conventions

#### Naming Conventions
```csharp
// Classes and Interfaces - PascalCase
public class UserService { }
public interface IUserService { }

// Methods and Properties - PascalCase
public async Task<User> GetUserAsync() { }
public string UserName { get; set; }

// Private fields - _camelCase
private readonly ILogger<UserService> _logger;
private readonly AppDbContext _context;

// Local variables and parameters - camelCase
var userName = "admin";
public void ProcessUser(string userId) { }

// Constants - UPPER_CASE or PascalCase
public const int MAX_RETRY_COUNT = 3;
public const string DefaultPassword = "Admin@123";
```

#### File Organization
```csharp
// Standard file structure
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LYBT.Modules.Users.Services
{
    /// <summary>
    /// User management service
    /// </summary>
    public class UserService : IUserService
    {
        #region Fields
        private readonly IUserRepository _repository;
        private readonly ILogger<UserService> _logger;
        #endregion

        #region Constructor
        public UserService(IUserRepository repository, ILogger<UserService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        #endregion

        #region Public Methods
        public async Task<ServiceResult<User>> GetUserAsync(Guid id)
        {
            // Implementation
        }
        #endregion

        #region Private Methods
        private bool ValidateUser(User user)
        {
            // Implementation
        }
        #endregion
    }
}
```

#### Async/Await Patterns
```csharp
// Always use Async suffix for async methods
public async Task<User> GetUserAsync(Guid id)
{
    return await _repository.GetByIdAsync(id);
}

// ConfigureAwait(false) for library code
public async Task<Data> ProcessDataAsync()
{
    return await GetDataAsync().ConfigureAwait(false);
}

// Avoid async void except for event handlers
// Bad
public async void ProcessData() { }

// Good
public async Task ProcessDataAsync() { }

// OK for event handlers
private async void OnButtonClick(object sender, EventArgs e) { }
```

#### LINQ Usage
```csharp
// Prefer method syntax for simple queries
var activeUsers = users.Where(u => u.IsActive).ToList();

// Use query syntax for complex queries
var userStats = from user in users
                where user.IsActive
                group user by user.Role into g
                select new { Role = g.Key, Count = g.Count() };

// Always use AsNoTracking for read-only queries
var users = await _context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync();
```

### Architecture Patterns

#### Service Result Pattern
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }

    public static ServiceResult<T> Success(T data, string message = null)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static ServiceResult<T> Failure(string error)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Errors = new List<string> { error }
        };
    }
}
```

#### Repository Pattern
```csharp
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(Guid id);
}

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    
    public Repository(AppDbContext context)
    {
        _context = context;
    }
    
    // Implementation
}
```

### API Design Standards

#### RESTful Endpoints
```csharp
// Resource naming - plural, lowercase
[Route("api/v1/users")]
[Route("api/v1/patients")]
[Route("api/v1/prescriptions")]

// HTTP verbs usage
[HttpGet]           // GET /api/v1/users
[HttpGet("{id}")]   // GET /api/v1/users/{id}
[HttpPost]          // POST /api/v1/users
[HttpPut("{id}")]   // PUT /api/v1/users/{id}
[HttpDelete("{id}")] // DELETE /api/v1/users/{id}

// Query parameters for filtering
[HttpGet]
public async Task<IActionResult> GetUsers([FromQuery] string role, [FromQuery] bool? isActive)
```

#### Response Format
```csharp
// Unified API response
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}

// Usage in controller
return Ok(new ApiResponse<User>
{
    Success = true,
    Data = user,
    Message = "用户获取成功",
    Timestamp = DateTime.UtcNow,
    RequestId = HttpContext.TraceIdentifier
});
```

### Database Conventions

#### Entity Design
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class User : BaseEntity
{
    public string Username { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; }
    
    // Navigation properties
    public virtual ICollection<Consultation> Consultations { get; set; }
}
```

#### Migration Naming
```bash
# Format: Add/Update/Remove + Description
Add-Migration AddUserTable
Add-Migration UpdatePatientAddPhoneNumber
Add-Migration RemoveDeprecatedFields
```

### Testing Standards

#### Test Naming
```csharp
// Format: Method_Scenario_ExpectedResult
[Fact]
public async Task GetUser_ValidId_ReturnsUser() { }

[Fact]
public async Task CreateUser_DuplicateUsername_ThrowsException() { }

[Fact]
public async Task DeleteUser_NonExistentId_ReturnsFalse() { }
```

#### Test Organization
```csharp
public class UserServiceTests
{
    // Arrange
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;
    
    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }
    
    [Fact]
    public async Task GetUser_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User { Id = userId };
        _mockRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);
        
        // Act
        var result = await _service.GetUserAsync(userId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedUser.Id, result.Data.Id);
    }
}
```

### Documentation Standards

#### XML Documentation
```csharp
/// <summary>
/// Manages user authentication and authorization
/// </summary>
public class AuthService
{
    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="username">User's username</param>
    /// <param name="password">User's password</param>
    /// <returns>Authentication result with JWT token</returns>
    /// <exception cref="UnauthorizedException">Thrown when credentials are invalid</exception>
    public async Task<AuthResult> AuthenticateAsync(string username, string password)
    {
        // Implementation
    }
}
```

#### README Structure
```markdown
# Module Name

## Overview
Brief description of the module's purpose

## Features
- Feature 1
- Feature 2

## Usage
```csharp
// Code example
```

## API Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET    | /api/v1/resource | Get all resources |

## Configuration
Required configuration settings

## Dependencies
- Dependency 1
- Dependency 2
```

### Git Conventions

#### Branch Naming
```bash
# Feature branches
feature/add-patient-search
feature/implement-prescription-printing

# Bug fix branches
bugfix/fix-login-validation
bugfix/resolve-null-reference

# Hotfix branches
hotfix/critical-security-patch

# Release branches
release/v1.0.0
```

#### Commit Messages
```bash
# Format: type(scope): subject

# Types:
feat(users): add user search functionality
fix(auth): resolve JWT token expiration issue
docs(api): update API documentation
refactor(patients): simplify patient service
test(herbs): add herb repository tests
chore(deps): update NuGet packages

# Multi-line for complex changes
feat(prescriptions): implement prescription printing

- Add PDF generation support
- Include barcode for tracking
- Format according to regulations
- Add print preview functionality

Closes #123
```

### Error Handling

#### Exception Types
```csharp
// Business logic exceptions
public class BusinessException : Exception
{
    public string Code { get; set; }
    public BusinessException(string message, string code = null) 
        : base(message) 
    {
        Code = code;
    }
}

// Validation exceptions
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; set; }
}

// Not found exceptions
public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"Entity '{entity}' with key '{key}' was not found.")
    {
    }
}
```

#### Error Response
```csharp
public class ErrorResponse
{
    public string Type { get; set; }
    public string Title { get; set; }
    public int Status { get; set; }
    public string Detail { get; set; }
    public string Instance { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
}
```

### Performance Guidelines

#### Caching Strategy
```csharp
// Cache keys format
public static class CacheKeys
{
    public static string User(Guid id) => $"user_{id}";
    public static string UserList => "user_list";
    public static string HerbList => "herb_list";
}

// Cache duration
public static class CacheDuration
{
    public static TimeSpan Short => TimeSpan.FromMinutes(5);
    public static TimeSpan Medium => TimeSpan.FromMinutes(30);
    public static TimeSpan Long => TimeSpan.FromHours(2);
}
```

#### Query Optimization
```csharp
// Use projection for read-only data
var userDtos = await _context.Users
    .AsNoTracking()
    .Select(u => new UserDto
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email
    })
    .ToListAsync();

// Use Include for eager loading
var consultations = await _context.Consultations
    .Include(c => c.Patient)
    .Include(c => c.Prescriptions)
    .ToListAsync();

// Use pagination for large datasets
var pagedResult = await _context.Patients
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### Security Standards

#### Input Validation
```csharp
public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 50).WithMessage("用户名长度必须在3-50个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");
            
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Email));
            
        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("密码至少8个字符")
            .Matches(@"[A-Z]").WithMessage("密码必须包含大写字母")
            .Matches(@"[a-z]").WithMessage("密码必须包含小写字母")
            .Matches(@"[0-9]").WithMessage("密码必须包含数字")
            .Matches(@"[!@#$%^&*]").WithMessage("密码必须包含特殊字符");
    }
}
```

#### SQL Injection Prevention
```csharp
// Always use parameterized queries
// Good - Using EF Core LINQ
var users = await _context.Users
    .Where(u => u.Username == username)
    .ToListAsync();

// Good - Using parameters
var users = await _context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Username = {0}", username)
    .ToListAsync();

// Bad - String concatenation (NEVER DO THIS)
// var users = await _context.Users
//     .FromSqlRaw($"SELECT * FROM Users WHERE Username = '{username}'")
//     .ToListAsync();
```

### Logging Standards

#### Log Levels
```csharp
// Debug - Development information
_logger.LogDebug("Processing user {UserId}", userId);

// Information - General flow
_logger.LogInformation("User {Username} logged in", username);

// Warning - Unexpected but handled
_logger.LogWarning("Rate limit exceeded for IP {IpAddress}", ipAddress);

// Error - Errors that need attention
_logger.LogError(ex, "Failed to create user {Username}", username);

// Critical - System failures
_logger.LogCritical(ex, "Database connection lost");
```

#### Structured Logging
```csharp
// Use structured logging with properties
_logger.LogInformation("Prescription created", new
{
    PrescriptionId = prescription.Id,
    PatientId = prescription.PatientId,
    HerbCount = prescription.Herbs.Count,
    TotalAmount = prescription.TotalAmount
});
```

### Code Review Checklist

#### Before Submitting PR
- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Code follows naming conventions
- [ ] No commented-out code
- [ ] No console.log or debug statements
- [ ] Error handling implemented
- [ ] Input validation added
- [ ] Documentation updated
- [ ] Performance considered
- [ ] Security reviewed

#### Review Focus Areas
1. **Logic Correctness**: Does the code do what it's supposed to?
2. **Error Handling**: Are all edge cases handled?
3. **Performance**: Are there any obvious bottlenecks?
4. **Security**: Are there any vulnerabilities?
5. **Maintainability**: Is the code easy to understand?
6. **Testing**: Are tests comprehensive?
7. **Documentation**: Is the code well-documented?

### Development Best Practices

#### SOLID Principles
1. **Single Responsibility**: Each class has one reason to change
2. **Open/Closed**: Open for extension, closed for modification
3. **Liskov Substitution**: Derived classes must be substitutable
4. **Interface Segregation**: Many specific interfaces over general ones
5. **Dependency Inversion**: Depend on abstractions, not concretions

#### DRY (Don't Repeat Yourself)
- Extract common code to methods
- Use constants for magic values
- Create base classes for shared behavior
- Leverage generic types

#### KISS (Keep It Simple, Stupid)
- Avoid over-engineering
- Choose clarity over cleverness
- Minimize dependencies
- Reduce complexity

#### YAGNI (You Aren't Gonna Need It)
- Don't add functionality until needed
- Avoid speculative generality
- Remove dead code
- Keep interfaces minimal