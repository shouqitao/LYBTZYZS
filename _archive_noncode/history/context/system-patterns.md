---
created: 2025-09-03T13:20:35Z
last_updated: 2025-09-03T13:20:35Z
version: 1.0
author: Claude Code PM System
---

# System Patterns

## Architectural Patterns

### UltraThink Dual-Layer Architecture (Frontend)

The frontend uses a revolutionary dual-layer architecture that eliminates traditional complexity:

```csharp
// Pure Delegation Pattern in Main Module
public class UserModule : IUserService
{
    private readonly IUserQueryService _queryService;
    private readonly IUserBusinessService _businessService;
    
    // All methods delegate to specialized services
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
        => await _queryService.SearchUsersAsync(criteria);
        
    public async Task<ServiceResult<User>> CreateUserAsync(UserCreateDto dto)
        => await _businessService.CreateUserAsync(dto);
}
```

**Benefits:**
- 93% code reduction compared to traditional architectures
- Clear separation of concerns
- Easy to maintain and test
- Unified service interface (IService)

### Traditional 3-Layer Architecture (Backend)

The backend maintains a proven 3-layer pattern:

```csharp
Controller → Service → Repository → Database
```

**Implementation:**
```csharp
// Controller Layer
[ApiController]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return HandleServiceResult(result);
    }
}

// Service Layer
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    
    public async Task<ServiceResult<User>> GetByIdAsync(Guid id)
    {
        var user = await _repository.GetByIdAsync(id);
        return ServiceResult<User>.Success(user);
    }
}

// Repository Layer
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public async Task<User> GetByIdAsync(Guid id)
        => await _context.Users.FindAsync(id);
}
```

## Design Patterns

### 1. Repository Pattern
- Abstracts data access logic
- Enables unit testing with mocks
- Supports multiple data sources
- LINQ-based queries for safety

### 2. Unit of Work Pattern
- Transaction management
- Ensures data consistency
- Batch operations support
- Automatic rollback on failure

### 3. Dependency Injection Pattern
- Constructor injection throughout
- Interface-based dependencies
- Scoped lifetime for services
- Singleton for caching

### 4. Factory Pattern
- Service creation abstraction
- Configuration-based instantiation
- Plugin architecture support

### 5. Observer Pattern (MVVM)
- INotifyPropertyChanged for data binding
- Event aggregation with Prism
- Reactive UI updates
- Command pattern for actions

## Data Flow Patterns

### Request Flow (API to Database)
```
Client Request
    ↓
API Controller (Validation)
    ↓
Service Layer (Business Logic)
    ↓
Repository Layer (Data Access)
    ↓
Entity Framework (ORM)
    ↓
SQL Server (Persistence)
```

### Response Flow (Database to Client)
```
SQL Server Result
    ↓
Entity Framework (Mapping)
    ↓
Repository (Entity)
    ↓
Service (DTO Conversion)
    ↓
Controller (API Response)
    ↓
Client (JSON)
```

## Module Communication Patterns

### Frontend Module Communication
```
Module A → Event Aggregator → Module B
Module A → Shared Service → Module C
Module A → Navigation Service → Module D
```

### Backend Module Communication
```
Controller → Direct Service Injection
Service A → Injected Service B
Repository → Shared DbContext
```

## Error Handling Patterns

### Global Exception Middleware
```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            await HandleBusinessException(context, ex);
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

### Service Result Pattern
```csharp
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<string> Errors { get; set; }
    
    public static ServiceResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };
        
    public static ServiceResult<T> Failure(string error)
        => new() { IsSuccess = false, Errors = new() { error } };
}
```

## Caching Patterns

### Memory Cache Strategy
```csharp
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
{
    if (_cache.TryGetValue(key, out T cached))
        return cached;
        
    var value = await factory();
    _cache.Set(key, value, expiry ?? TimeSpan.FromMinutes(10));
    return value;
}
```

### Cache Invalidation Pattern
```csharp
// Invalidate on update
public async Task UpdateAsync(Entity entity)
{
    await _repository.UpdateAsync(entity);
    _cache.Remove($"entity_{entity.Id}");
    _cache.Remove("entity_list");
}
```

## Security Patterns

### JWT Authentication Flow
```
Login Request → Validate Credentials → Generate JWT → Return Token
API Request → Validate JWT → Extract Claims → Authorize → Process
```

### Role-Based Authorization
```csharp
[Authorize(Roles = "Admin")]
public class AdminController : BaseController { }

[Authorize(Roles = "Doctor,Admin")]
public class PatientController : BaseController { }
```

## Database Patterns

### Soft Delete Pattern
```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

// Query filter in DbContext
modelBuilder.Entity<User>()
    .HasQueryFilter(u => !u.IsDeleted);
```

### Audit Trail Pattern
```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string UpdatedBy { get; set; }
}
```

## Testing Patterns

### Arrange-Act-Assert (AAA)
```csharp
[Fact]
public async Task GetUser_ValidId_ReturnsUser()
{
    // Arrange
    var userId = Guid.NewGuid();
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(x => x.GetByIdAsync(userId))
        .ReturnsAsync(new User { Id = userId });
    
    // Act
    var service = new UserService(mockRepo.Object);
    var result = await service.GetByIdAsync(userId);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(userId, result.Data.Id);
}
```

### Test Data Builder Pattern
```csharp
public class UserBuilder
{
    private User _user = new();
    
    public UserBuilder WithName(string name)
    {
        _user.Name = name;
        return this;
    }
    
    public UserBuilder WithRole(UserRole role)
    {
        _user.Role = role;
        return this;
    }
    
    public User Build() => _user;
}
```

## Validation Patterns

### FluentValidation Rules
```csharp
public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9_]*$");
            
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
```

## Mapping Patterns

### AutoMapper Profiles
```csharp
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, 
                opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
                
        CreateMap<UserCreateDto, User>()
            .ForMember(d => d.Id, opt => opt.Ignore());
    }
}
```

## Event-Driven Patterns

### Prism Event Aggregator
```csharp
// Event Definition
public class UserLoggedInEvent : PubSubEvent<UserInfo> { }

// Publisher
_eventAggregator.GetEvent<UserLoggedInEvent>().Publish(userInfo);

// Subscriber
_eventAggregator.GetEvent<UserLoggedInEvent>()
    .Subscribe(OnUserLoggedIn, ThreadOption.UIThread);
```

## Performance Patterns

### Async/Await Throughout
```csharp
public async Task<IActionResult> GetUsersAsync()
{
    var users = await _userService.GetAllAsync();
    return Ok(users);
}
```

### Pagination Pattern
```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

### Query Optimization
```csharp
// Use AsNoTracking for read-only queries
var users = await _context.Users
    .AsNoTracking()
    .Include(u => u.Roles)
    .Where(u => u.IsActive)
    .ToListAsync();
```