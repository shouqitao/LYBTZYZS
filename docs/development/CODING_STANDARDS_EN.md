# Coding Standards

## Table of Contents

1. [Overview](#overview)
2. [C# Coding Conventions](#c-coding-conventions)
3. [Naming Conventions](#naming-conventions)
4. [Code Organization](#code-organization)
5. [Programming Practices](#programming-practices)
6. [API Design Standards](#api-design-standards)
7. [Data Access Standards](#data-access-standards)
8. [Exception Handling](#exception-handling)
9. [Logging](#logging)
10. [Testing Standards](#testing-standards)
11. [Security Coding](#security-coding)
12. [Performance Optimization](#performance-optimization)
13. [Code Review](#code-review)

## Overview

This document defines the coding standards and best practices for the LYBT Traditional Chinese Medicine Clinic Management System. All developers should follow these standards to ensure code consistency, readability, and maintainability.

### Core Principles

1. **Clarity over Cleverness**: Code should be easy to understand
2. **Consistency**: Maintain uniform coding style throughout the project
3. **Simplicity**: Avoid over-engineering, keep it simple
4. **Testability**: Code should be easy to test
5. **Maintainability**: Consider future maintenance needs

## C# Coding Conventions

### 1. Basic Formatting

#### Indentation and Spacing

```csharp
// Use 4 spaces for indentation, not tabs
public class PatientService
{
    private readonly IPatientRepository _repository;
    
    public PatientService(IPatientRepository repository)
    {
        _repository = repository;
    }
}
```

#### Braces

```csharp
// Braces on their own line (Allman style)
if (condition)
{
    // Code block
}
else
{
    // Code block
}

// Use braces even for single-line statements
if (condition)
{
    return true;
}
```

#### Line Length

- Keep lines under 120 characters
- Break long lines appropriately

```csharp
// Break long parameter lists
public async Task<ApiResponse<PatientDetailDto>> RegisterPatientAsync(
    string name,
    string idNumber,
    DateTime birthDate,
    string phoneNumber,
    string address)
{
    // Method implementation
}
```

### 2. Language Feature Usage

#### Using var

```csharp
// Use var when type is obvious
var patient = new Patient();
var patients = new List<Patient>();

// Use explicit type when not obvious
IPatientService service = serviceFactory.CreatePatientService();
Dictionary<string, object> config = GetConfiguration();
```

#### String Interpolation

```csharp
// Prefer string interpolation
var message = $"Patient {patientName} registered successfully";

// Avoid string concatenation
var message = "Patient " + patientName + " registered successfully"; // Avoid
```

#### Null Conditional Operators

```csharp
// Use null conditional operators
var name = patient?.Name ?? "Unknown";
var count = patients?.Count() ?? 0;

// Null coalescing assignment (C# 8.0+)
_cache ??= new MemoryCache();
```

## Naming Conventions

### 1. General Rules

| Element | Convention | Example |
|---------|------------|---------|
| Class | PascalCase | `PatientService` |
| Interface | I + PascalCase | `IPatientService` |
| Method | PascalCase | `GetPatientById` |
| Property | PascalCase | `FirstName` |
| Field (private) | _camelCase | `_patientRepository` |
| Field (public) | PascalCase | `DefaultTimeout` |
| Parameter | camelCase | `patientId` |
| Local Variable | camelCase | `isValid` |
| Constant | PascalCase | `MaxRetryCount` |

### 2. Specific Naming Guidelines

#### Async Methods

```csharp
// Async methods should end with "Async"
public async Task<Patient> GetPatientByIdAsync(Guid id)
{
    // Implementation
}
```

#### Boolean Properties and Variables

```csharp
// Use positive names for booleans
public bool IsActive { get; set; }
public bool HasPrescription { get; set; }

// Avoid negative names
public bool IsNotActive { get; set; } // Avoid
```

#### Collections

```csharp
// Use plural names for collections
public List<Patient> Patients { get; set; }
public IEnumerable<Doctor> Doctors { get; set; }
```

## Code Organization

### 1. File Organization

```csharp
// Order of elements in a file
// 1. Using statements
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

// 2. Namespace
namespace LYBT.Module.Patients.Services
{
    // 3. Class/Interface
    public class PatientService : IPatientService
    {
        // 4. Fields
        private readonly IPatientRepository _repository;
        private readonly ILogger<PatientService> _logger;
        
        // 5. Constructors
        public PatientService(IPatientRepository repository, ILogger<PatientService> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        
        // 6. Properties
        public int MaxRetryCount { get; set; } = 3;
        
        // 7. Public methods
        public async Task<Patient> GetPatientByIdAsync(Guid id)
        {
            // Implementation
        }
        
        // 8. Private methods
        private bool ValidatePatient(Patient patient)
        {
            // Implementation
        }
    }
}
```

### 2. Project Structure

```
LYBT.Module.Patients/
├── Interfaces/
│   ├── IPatientService.cs
│   └── IPatientRepository.cs
├── Services/
│   └── PatientService.cs
├── Repositories/
│   └── PatientRepository.cs
├── Models/
│   ├── Patient.cs
│   └── PatientDto.cs
├── Mapping/
│   └── PatientMappingProfile.cs
└── PatientsModule.cs
```

## Programming Practices

### 1. SOLID Principles

#### Single Responsibility Principle

```csharp
// Good: Each class has a single responsibility
public class PatientService : IPatientService
{
    // Only handles patient business logic
}

public class PatientValidator : IPatientValidator
{
    // Only handles patient validation
}
```

#### Dependency Inversion Principle

```csharp
// Depend on abstractions, not concretions
public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository; // Interface
    
    public PatientService(IPatientRepository repository)
    {
        _repository = repository;
    }
}
```

### 2. DRY (Don't Repeat Yourself)

```csharp
// Extract common logic into methods
private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    for (int i = 0; i < MaxRetryCount; i++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (i < MaxRetryCount - 1)
        {
            _logger.LogWarning(ex, "Operation failed, retrying...");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
    }
    throw new OperationFailedException("Operation failed after retries");
}
```

### 3. Guard Clauses

```csharp
public async Task<Patient> UpdatePatientAsync(PatientUpdateDto dto)
{
    // Use guard clauses for early returns
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));
        
    if (dto.Id == Guid.Empty)
        throw new ArgumentException("Patient ID cannot be empty", nameof(dto));
        
    // Main logic here
}
```

## API Design Standards

### 1. RESTful Conventions

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : BaseController
{
    // GET: api/v1/patients
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetAll()
    
    // GET: api/v1/patients/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    
    // POST: api/v1/patients
    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(PatientCreateDto dto)
    
    // PUT: api/v1/patients/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, PatientUpdateDto dto)
    
    // DELETE: api/v1/patients/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
}
```

### 2. Response Format

```csharp
// Use consistent response format
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
}

// In controller
return Ok(new ApiResponse<PatientDto>
{
    Success = true,
    Data = patientDto,
    Message = "Patient retrieved successfully"
});
```

## Data Access Standards

### 1. Repository Pattern

```csharp
public interface IPatientRepository : IBaseRepository<Patient>
{
    Task<Patient> GetByIdNumberAsync(string idNumber);
    Task<IEnumerable<Patient>> GetActivePatients();
}

public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context)
    {
    }
    
    public async Task<Patient> GetByIdNumberAsync(string idNumber)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.IdNumber == idNumber);
    }
}
```

### 2. Async/Await Best Practices

```csharp
// Always use async/await for I/O operations
public async Task<IEnumerable<Patient>> GetPatientsAsync()
{
    return await _context.Patients
        .Where(p => p.IsActive)
        .OrderBy(p => p.Name)
        .ToListAsync(); // Use async versions
}

// Configure await when context doesn't matter
var data = await GetDataAsync().ConfigureAwait(false);
```

## Exception Handling

### 1. Exception Types

```csharp
// Use appropriate exception types
throw new ArgumentNullException(nameof(patient));
throw new ArgumentException("Invalid ID number", nameof(idNumber));
throw new InvalidOperationException("Patient already exists");
throw new NotFoundException($"Patient with ID {id} not found");
```

### 2. Global Exception Handling

```csharp
// Handle exceptions at appropriate levels
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await HandleNotFoundException(context, ex);
        }
        catch (ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericException(context, ex);
        }
    }
}
```

## Logging

### 1. Logging Levels

```csharp
// Use appropriate logging levels
_logger.LogTrace("Entering method GetPatientById with id: {PatientId}", id);
_logger.LogDebug("Query executed in {ElapsedMs}ms", elapsedMs);
_logger.LogInformation("Patient {PatientId} retrieved successfully", id);
_logger.LogWarning("Patient {PatientId} has no recent visits", id);
_logger.LogError(ex, "Error retrieving patient {PatientId}", id);
_logger.LogCritical(ex, "Database connection failed");
```

### 2. Structured Logging

```csharp
// Use structured logging with proper placeholders
_logger.LogInformation("User {UserId} created patient {PatientId} at {Timestamp}",
    userId, patientId, DateTime.UtcNow);

// Include relevant context
using (_logger.BeginScope("PatientId: {PatientId}", patientId))
{
    // All logs within this scope will include PatientId
    _logger.LogInformation("Processing patient registration");
}
```

## Testing Standards

### 1. Unit Testing

```csharp
[TestClass]
public class PatientServiceTests
{
    private Mock<IPatientRepository> _repositoryMock;
    private PatientService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _service = new PatientService(_repositoryMock.Object);
    }
    
    [TestMethod]
    public async Task GetPatientById_ValidId_ReturnsPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedPatient = new Patient { Id = patientId };
        _repositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(expectedPatient);
        
        // Act
        var result = await _service.GetPatientByIdAsync(patientId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(patientId, result.Id);
        _repositoryMock.Verify(r => r.GetByIdAsync(patientId), Times.Once);
    }
}
```

### 2. Test Naming

```csharp
// Method_Scenario_ExpectedBehavior
[TestMethod]
public async Task RegisterPatient_ValidData_ReturnsSuccess()
public async Task RegisterPatient_DuplicateIdNumber_ThrowsException()
public async Task UpdatePatient_PatientNotFound_ReturnsNotFound()
```

## Security Coding

### 1. Input Validation

```csharp
public class PatientValidator : AbstractValidator<PatientCreateDto>
{
    public PatientValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
            
        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("ID number is required")
            .Matches(@"^\d{18}$").WithMessage("Invalid ID number format");
            
        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
```

### 2. SQL Injection Prevention

```csharp
// Always use parameterized queries
var patients = await _context.Patients
    .Where(p => p.Name.Contains(searchTerm))
    .ToListAsync();

// Never concatenate SQL strings
string query = $"SELECT * FROM Patients WHERE Name = '{name}'"; // NEVER DO THIS
```

### 3. Sensitive Data

```csharp
// Don't log sensitive information
_logger.LogInformation("User {UserId} logged in", userId); // Good
_logger.LogInformation($"User logged in with password {password}"); // NEVER DO THIS

// Use SecureString for sensitive data when possible
// Hash passwords using proper algorithms
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
```

## Performance Optimization

### 1. Database Queries

```csharp
// Use projection to select only needed fields
var patientNames = await _context.Patients
    .Where(p => p.IsActive)
    .Select(p => new { p.Id, p.Name })
    .ToListAsync();

// Use pagination for large datasets
var pagedResults = await _context.Patients
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Include related data to avoid N+1 queries
var patientsWithRecords = await _context.Patients
    .Include(p => p.MedicalRecords)
    .ToListAsync();
```

### 2. Caching

```csharp
public async Task<Patient> GetPatientByIdAsync(Guid id)
{
    var cacheKey = $"patient_{id}";
    
    if (!_cache.TryGetValue(cacheKey, out Patient patient))
    {
        patient = await _repository.GetByIdAsync(id);
        
        if (patient != null)
        {
            _cache.Set(cacheKey, patient, TimeSpan.FromMinutes(10));
        }
    }
    
    return patient;
}
```

## Code Review

### 1. Code Review Checklist

- [ ] Code follows naming conventions
- [ ] Methods are small and focused (< 20 lines)
- [ ] No code duplication
- [ ] Proper error handling
- [ ] Adequate logging
- [ ] Unit tests included
- [ ] No security vulnerabilities
- [ ] Performance considerations addressed
- [ ] Documentation updated

### 2. Pull Request Guidelines

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
```

## Summary

These coding standards are designed to ensure high-quality, maintainable code across the LYBT system. All developers should familiarize themselves with these guidelines and apply them consistently. Regular code reviews help ensure adherence to these standards and continuous improvement of code quality.