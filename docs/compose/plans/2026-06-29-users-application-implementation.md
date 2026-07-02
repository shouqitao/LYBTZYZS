# Users Application Layer Implementation Plan

> **For agentic workers:** Use compose:subagent or compose:execute to implement this plan task-by-task.

**Goal:** Create MediatR Commands/Queries for Users module, replacing direct IUserService calls with CQRS pattern.

**Architecture:** Each use case becomes a MediatR IRequest<T> with corresponding handler. Validation via FluentValidation in MediatR pipeline. Domain events published via IDomainEventDispatcher.

**Tech Stack:** .NET 8, MediatR 12.x, FluentValidation 12.x, LYBT.SharedKernel

## Global Constraints

- .NET 8.0, LangVersion=latest, Nullable=enable
- All public types must have XML documentation
- Commands return `Result<T>` (from LYBT.SharedKernel)
- Queries return `Result<T>` with DTOs from LYBT.Shared.Models
- Domain events dispatched via IDomainEventDispatcher

---

### Task 1: Create Application Directory Structure

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Application/` directory
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/` directory
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Queries/` directory
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Validators/` directory
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Mappers/` directory

- [ ] **Step 1: Create directories**

```bash
New-Item -ItemType Directory -Force -Path "src/Server/Modules/LYBT.Module.Users/Application/Commands" | Out-Null
New-Item -ItemType Directory -Force -Path "src/Server/Modules/LYBT.Module.Users/Application/Queries" | Out-Null
New-Item -ItemType Directory -Force -Path "src/Server/Modules/LYBT.Module.Users/Application/Validators" | Out-Null
New-Item -ItemType Directory -Force -Path "src/Server/Modules/LYBT.Module.Users/Application/Mappers" | Out-Null
```

---

### Task 2: Result Type (SharedKernel)

**Files:**
- Create: `src/Server/Core/LYBT.SharedKernel/Common/Result.cs`

**Interfaces:**
- Consumes: None
- Produces: `Result<T>`, `Result` for all commands/queries

- [ ] **Step 1: Create Result types**

```csharp
// src/Server/Core/LYBT.SharedKernel/Common/Result.cs
using LYBT.Shared.Primitives;

namespace LYBT.SharedKernel.Common;

/// <summary>
/// 操作结果封装。用于命令和查询的统一返回类型。
/// </summary>
public class Result<T>
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; }

    /// <summary>返回数据</summary>
    public T? Value { get; }

    /// <summary>错误信息</summary>
    public string? Error { get; }

    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>创建成功结果</summary>
    public static Result<T> Success(T value) => new(true, value, null, default);

    /// <summary>创建失败结果</summary>
    public static Result<T> Failure(ErrorCode code, string error) => new(false, default, error, code);

    /// <summary>创建验证失败结果</summary>
    public static Result<T> ValidationFailure(string error) => new(false, default, error, ErrorCode.ValidationFailed);

    /// <summary>隐式转换：值 → Result&lt;T&gt;</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// 无数据的操作结果。
/// </summary>
public class Result
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; }

    /// <summary>错误信息</summary>
    public string? Error { get; }

    /// <summary>错误码</summary>
    public ErrorCode ErrorCode { get; }

    private Result(bool isSuccess, string? error, ErrorCode errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    /// <summary>创建成功结果</summary>
    public static Result Success() => new(true, null, default);

    /// <summary>创建失败结果</summary>
    public static Result Failure(ErrorCode code, string error) => new(false, error, code);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Core/LYBT.SharedKernel/Common/
git commit -m "feat(sharedkernel): add Result<T> and Result types for CQRS"
```

---

### Task 3: User Mapper

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Mappers/UserMapper.cs`

**Interfaces:**
- Consumes: Domain entities, DTOs
- Produces: Mapping between Domain ↔ DTOs

- [ ] **Step 1: Create UserMapper**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Mappers/UserMapper.cs
using Riok.Mapperly;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Module.Users.Domain;

namespace LYBT.Module.Users.Application.Mappers;

/// <summary>
/// 用户实体与DTO之间的映射器（编译时生成）。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserMapper
{
    /// <summary>实体 → 列表DTO</summary>
    public static partial UserListDto ToListDto(ApplicationUser entity);

    /// <summary>实体 → 详情DTO</summary>
    public static partial UserDetailDto ToDetailDto(ApplicationUser entity);

    /// <summary>输入DTO → 实体（创建用）</summary>
    [MapperIgnoreTarget(nameof(ApplicationUser.Id))]
    [MapperIgnoreTarget(nameof(ApplicationUser.CreatedAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.CreatedBy))]
    [MapperIgnoreTarget(nameof(ApplicationUser.UpdatedAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.UpdatedBy))]
    [MapperIgnoreTarget(nameof(ApplicationUser.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationUser.RowVersion))]
    [MapperIgnoreTarget(nameof(ApplicationUser.LastLoginAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.IsSysAdmin))]
    public static partial ApplicationUser ToEntity(UserInputDto dto);

    /// <summary>输入DTO → 更新实体字段</summary>
    [MapperIgnoreTarget(nameof(ApplicationUser.Id))]
    [MapperIgnoreTarget(nameof(ApplicationUser.UserName))]
    [MapperIgnoreTarget(nameof(ApplicationUser.CreatedAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.CreatedBy))]
    [MapperIgnoreTarget(nameof(ApplicationUser.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationUser.RowVersion))]
    [MapperIgnoreTarget(nameof(ApplicationUser.LastLoginAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.IsSysAdmin))]
    public static partial void UpdateEntity(UserInputDto dto, ApplicationUser entity);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded (with Mapperly source generation)

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Application/Mappers/
git commit -m "feat(users-application): add UserMapper with Mapperly"
```

---

### Task 4: Query Handlers

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUsersQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUsersQueryHandler.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUserQuery.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUserQueryHandler.cs`

**Interfaces:**
- Consumes: `IMapperlyMapper`, `IUserRepository`
- Produces: Query handlers for user list and detail

- [ ] **Step 1: Create GetUsersQuery**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUsersQuery.cs
using MediatR;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 获取用户分页列表查询。
/// </summary>
public record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Keyword = null,
    UserRole? Role = null,
    CommonStatus? Status = null
) : IRequest<Result<PagedResult<UserListDto>>>;
```

- [ ] **Step 2: Create GetUsersQueryHandler**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUsersQueryHandler.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;
using LYBT.Module.Users.Domain;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 获取用户分页列表查询处理器。
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserListDto>>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResult<UserListDto>>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _userRepository.GetPagedAsync(
            request.Page, request.PageSize, request.Keyword,
            request.Role, request.Status, cancellationToken);

        var dtos = result.Items.Select(UserMapper.ToListDto).ToList();

        var pagedResult = new PagedResult<UserListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };

        return Result<PagedResult<UserListDto>>.Success(pagedResult);
    }
}
```

- [ ] **Step 3: Create GetUserQuery**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUserQuery.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 根据ID获取用户详情查询。
/// </summary>
public record GetUserQuery(Guid Id) : IRequest<Result<UserDetailDto>>;
```

- [ ] **Step 4: Create GetUserQueryHandler**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Queries/GetUserQueryHandler.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives;
using LYBT.SharedKernel.Common;
using LYBT.Module.Users.Domain;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Queries;

/// <summary>
/// 根据ID获取用户详情查询处理器。
/// </summary>
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, "用户不存在");

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}
```

- [ ] **Step 5: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Application/Queries/
git commit -m "feat(users-application): add GetUsers and GetUser query handlers"
```

---

### Task 5: Command Handlers

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/CreateUserCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/CreateUserCommandHandler.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/UpdateUserCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/UpdateUserCommandHandler.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/DeleteUserCommand.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Commands/DeleteUserCommandHandler.cs`

**Interfaces:**
- Consumes: Domain entities, `IUserRepository`, `IDomainEventDispatcher`
- Produces: Command handlers for user CRUD

- [ ] **Step 1: Create CreateUserCommand**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Commands/CreateUserCommand.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 创建用户命令。
/// </summary>
public record CreateUserCommand(
    UserInputDto Input,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<UserDetailDto>>;
```

- [ ] **Step 2: Create CreateUserCommandHandler**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Commands/CreateUserCommandHandler.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives;
using LYBT.SharedKernel.Common;
using LYBT.SharedKernel.Events;
using LYBT.Module.Users.Domain;
using LYBT.Module.Users.Domain.Events;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 创建用户命令处理器。
/// </summary>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result<UserDetailDto>> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        // 检查权限
        if (!request.IsAdmin)
            return Result<UserDetailDto>.Failure(ErrorCode.Unauthorized, "无权创建用户");

        // 检查用户名是否已存在
        if (await _userRepository.ExistsByUserNameAsync(dto.UserName!, cancellationToken))
            return Result<UserDetailDto>.Failure(ErrorCode.Conflict, "用户名已存在");

        // 创建领域实体
        var user = ApplicationUser.Create(
            dto.UserName!,
            dto.RealName!,
            dto.Role ?? Shared.Models.Enums.UserRole.Doctor,
            dto.PhoneNumber,
            dto.Email,
            dto.Remark,
            request.CurrentUserId);

        // 保存
        await _userRepository.AddAsync(user, cancellationToken);

        // 发布领域事件
        await _eventDispatcher.DispatchAsync(new[]
        {
            new UserCreatedEvent(user.Id, user.UserName, user.RealName, user.Role, request.CurrentUserId)
        }, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}
```

- [ ] **Step 3: Create UpdateUserCommand**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Commands/UpdateUserCommand.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 更新用户命令。
/// </summary>
public record UpdateUserCommand(
    Guid Id,
    UserInputDto Input,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result<UserDetailDto>>;
```

- [ ] **Step 4: Create UpdateUserCommandHandler**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Commands/UpdateUserCommandHandler.cs
using MediatR;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Primitives;
using LYBT.SharedKernel.Common;
using LYBT.Module.Users.Domain;
using LYBT.Module.Users.Application.Mappers;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 更新用户命令处理器。
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<UserDetailDto>>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDetailDto>> Handle(
        UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result<UserDetailDto>.Failure(ErrorCode.NotFound, "用户不存在");

        // 更新领域实体
        user.UpdateProfile(
            request.Input.RealName!,
            request.Input.PhoneNumber,
            request.Input.Email,
            request.Input.Remark,
            request.CurrentUserId);

        // 保存
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<UserDetailDto>.Success(UserMapper.ToDetailDto(user));
    }
}
```

- [ ] **Step 5: Create DeleteUserCommand**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Commands/DeleteUserCommand.cs
using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 删除用户命令（软删除）。
/// </summary>
public record DeleteUserCommand(
    Guid Id,
    Guid CurrentUserId,
    bool IsAdmin
) : IRequest<Result>;
```

- [ ] **Step 6: Create DeleteUserCommandHandler**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Commands/DeleteUserCommandHandler.cs
using MediatR;
using LYBT.Shared.Primitives;
using LYBT.SharedKernel.Common;
using LYBT.SharedKernel.Events;
using LYBT.Module.Users.Domain;
using LYBT.Module.Users.Domain.Events;

namespace LYBT.Module.Users.Application.Commands;

/// <summary>
/// 删除用户命令处理器（软删除）。
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _userRepository = userRepository;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Result> Handle(
        DeleteUserCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
            return Result.Failure(ErrorCode.Unauthorized, "无权删除用户");

        var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return Result.Failure(ErrorCode.NotFound, "用户不存在");

        // 领域方法封装业务规则（sysadmin不可删除）
        user.SoftDelete(request.CurrentUserId);

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new UserDeletedEvent(user.Id, user.UserName, user.RealName, request.CurrentUserId)
        }, cancellationToken);

        return Result.Success();
    }
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Application/Commands/
git commit -m "feat(users-application): add CreateUser, UpdateUser, DeleteUser command handlers"
```

---

### Task 6: FluentValidation Validators

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Validators/CreateUserValidator.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Application/Validators/UpdateUserValidator.cs`

**Interfaces:**
- Consumes: Commands, FluentValidation
- Produces: Input validation for commands

- [ ] **Step 1: Create CreateUserValidator**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Validators/CreateUserValidator.cs
using FluentValidation;
using LYBT.Module.Users.Application.Commands;

namespace LYBT.Module.Users.Application.Validators;

/// <summary>
/// 创建用户命令验证器。
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Input.UserName)
            .NotEmpty().WithMessage("用户名不能为空")
            .Length(3, 32).WithMessage("用户名长度必须在3-32个字符之间")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");

        RuleFor(x => x.Input.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名长度不能超过50个字符");

        RuleFor(x => x.Input.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.PhoneNumber));

        RuleFor(x => x.Input.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.Email));
    }
}
```

- [ ] **Step 2: Create UpdateUserValidator**

```csharp
// src/Server/Modules/LYBT.Module.Users/Application/Validators/UpdateUserValidator.cs
using FluentValidation;
using LYBT.Module.Users.Application.Commands;

namespace LYBT.Module.Users.Application.Validators;

/// <summary>
/// 更新用户命令验证器。
/// </summary>
public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("用户ID不能为空");

        RuleFor(x => x.Input.RealName)
            .NotEmpty().WithMessage("真实姓名不能为空")
            .MaximumLength(50).WithMessage("真实姓名长度不能超过50个字符");

        RuleFor(x => x.Input.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.PhoneNumber));

        RuleFor(x => x.Input.Email)
            .EmailAddress().WithMessage("邮箱格式不正确")
            .When(x => !string.IsNullOrEmpty(x.Input.Email));
    }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Application/Validators/
git commit -m "feat(users-application): add FluentValidation validators for user commands"
```

---

### Task 7: Full Solution Build Verification

**Files:**
- None (verification only)

- [ ] **Step 1: Build full solution**

Run: `dotnet build LYBTZYZS.sln --verbosity quiet`
Expected: Build succeeded (0 errors)

- [ ] **Step 2: Verify no new errors from Application layer**

Expected: Only pre-existing warnings

- [ ] **Step 3: Final commit (if any fixes needed)**

```bash
git add -A
git commit -m "fix(users-application): address build issues"
```
