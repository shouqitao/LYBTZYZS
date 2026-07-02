# Users Domain Layer Implementation Plan

> **For agentic workers:** Use compose:subagent or compose:execute to implement this plan task-by-task.

**Goal:** Create Domain layer for Users/Auth module with rich domain model, domain events, and domain services.

**Architecture:** Move User-related entities from LYBT.Entities to LYBT.Module.Users.Domain, enrich with domain methods, add domain events for lifecycle changes. AuthSession becomes aggregate root with domain logic.

**Tech Stack:** .NET 8, C# 12, records, LYBT.SharedKernel

## Global Constraints

- .NET 8.0, LangVersion=latest, Nullable=enable
- All public types must have XML documentation
- Domain methods enforce business rules (validation, state transitions)
- Domain events published on state changes
- No infrastructure dependencies in Domain layer

---

### Task 1: Create Users Domain Project Structure

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/` directory
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/` directory

- [ ] **Step 1: Create directory structure**

```bash
New-Item -ItemType Directory -Force -Path "src/Server/Modules/LYBT.Module.Users/Domain" | Out-Null
New-Item -ItemType Directory -Force -Path "src/Server/Modules/LYBT.Module.Users/Domain/Events" | Out-Null
```

---

### Task 2: User Domain Entity

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/ApplicationUser.cs`

**Interfaces:**
- Consumes: `LYBT.SharedKernel.Primitives.Entity`, `LYBT.Shared.Models.Enums`
- Produces: `ApplicationUser` with domain methods

- [ ] **Step 1: Create ApplicationUser domain entity**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/ApplicationUser.cs
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Primitives;

namespace LYBT.Module.Users.Domain;

/// <summary>
/// 用户聚合根。封装用户生命周期的业务规则和状态转换。
/// </summary>
public class ApplicationUser : Entity, IAggregateRoot
{
    /// <summary>用户名（登录名）</summary>
    [StringLength(32)]
    public string UserName { get; private set; } = string.Empty;

    /// <summary>真实姓名</summary>
    [StringLength(50)]
    public string RealName { get; private set; } = string.Empty;

    /// <summary>拼音码（用于快速搜索）</summary>
    [StringLength(50)]
    public string? PinYinCode { get; private set; }

    /// <summary>用户角色</summary>
    public UserRole Role { get; private set; } = UserRole.Doctor;

    /// <summary>系统管理员标识</summary>
    public bool IsSysAdmin { get; private set; }

    /// <summary>用户状态</summary>
    public CommonStatus Status { get; private set; } = CommonStatus.Enabled;

    /// <summary>下次登录须改密</summary>
    public bool MustChangeOnNextLogin { get; private set; }

    /// <summary>最后登录时间 (UTC)</summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>手机号码</summary>
    [StringLength(20)]
    public string? PhoneNumber { get; private set; }

    /// <summary>邮箱</summary>
    [StringLength(100)]
    public string? Email { get; private set; }

    /// <summary>备注</summary>
    [StringLength(500)]
    public string? Remark { get; private set; }

    /// <summary>创建时间 (UTC)</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>更新时间 (UTC)</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>创建者ID</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>更新者ID</summary>
    public Guid? UpdatedBy { get; private set; }

    /// <summary>软删除标记</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>乐观并发控制</summary>
    [Timestamp]
    public byte[]? RowVersion { get; private set; }

    // EF Core needs parameterless constructor
    private ApplicationUser() { }

    /// <summary>
    /// 创建新用户。
    /// </summary>
    public static ApplicationUser Create(
        string userName,
        string realName,
        UserRole role,
        string? phoneNumber = null,
        string? email = null,
        string? remark = null,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));
        if (userName.Length < 3 || userName.Length > 32)
            throw new ArgumentException("用户名长度必须在3-32个字符之间", nameof(userName));
        if (string.IsNullOrWhiteSpace(realName))
            throw new ArgumentException("真实姓名不能为空", nameof(realName));

        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            RealName = realName.Trim(),
            Role = role,
            PhoneNumber = phoneNumber?.Trim(),
            Email = email?.Trim(),
            Remark = remark?.Trim(),
            Status = CommonStatus.Enabled,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新用户基本信息。
    /// </summary>
    public void UpdateProfile(string realName, string? phoneNumber, string? email, string? remark, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(realName))
            throw new ArgumentException("真实姓名不能为空", nameof(realName));

        RealName = realName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        Email = email?.Trim();
        Remark = remark?.Trim();
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改用户状态（启用/禁用）。
    /// sysadmin不可被禁用。
    /// </summary>
    public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
    {
        if (IsSysAdmin && newStatus == CommonStatus.Disabled)
            throw new InvalidOperationException("系统管理员不能被禁用");

        Status = newStatus;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 更改用户角色。
    /// sysadmin角色不可更改。
    /// </summary>
    public void ChangeRole(UserRole newRole, Guid updatedBy)
    {
        if (IsSysAdmin)
            throw new InvalidOperationException("系统管理员角色不可更改");

        Role = newRole;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 记录登录时间。
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 标记下次登录须改密。
    /// </summary>
    public void RequirePasswordChange()
    {
        MustChangeOnNextLogin = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 清除须改密标记（用户成功改密后调用）。
    /// </summary>
    public void ClearPasswordChangeRequirement()
    {
        MustChangeOnNextLogin = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 软删除用户。
    /// sysadmin不可被删除。
    /// </summary>
    public void SoftDelete(Guid deletedBy)
    {
        if (IsSysAdmin)
            throw new InvalidOperationException("系统管理员不能被删除");

        IsDeleted = true;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 恢复已软删除的用户。
    /// </summary>
    public void Restore(Guid restoredBy)
    {
        IsDeleted = false;
        UpdatedBy = restoredBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Domain/
git commit -m "feat(users-domain): add ApplicationUser aggregate root with domain methods"
```

---

### Task 3: AuthSession Aggregate Root

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/AuthSession.cs`

**Interfaces:**
- Consumes: `LYBT.SharedKernel.Primitives.Entity`, `LYBT.Shared.Models.Enums`
- Produces: `AuthSession` with domain methods

- [ ] **Step 1: Create AuthSession aggregate root**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/AuthSession.cs
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Primitives;

namespace LYBT.Module.Users.Domain;

/// <summary>
/// 认证会话聚合根。管理用户登录会话的生命周期。
/// </summary>
public class AuthSession : Entity, IAggregateRoot
{
    /// <summary>用户ID</summary>
    public Guid UserId { get; private set; }

    /// <summary>会话令牌哈希</summary>
    [StringLength(256)]
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>登录时间 (UTC)</summary>
    public DateTime LoginTime { get; private set; }

    /// <summary>登出时间 (UTC)</summary>
    public DateTime? LogoutTime { get; private set; }

    /// <summary>过期时间 (UTC)</summary>
    public DateTime ExpiryTime { get; private set; }

    /// <summary>IP地址</summary>
    [StringLength(45)]
    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>用户代理</summary>
    [StringLength(500)]
    public string? UserAgent { get; private set; }

    /// <summary>是否已撤销</summary>
    public bool IsRevoked { get; private set; }

    /// <summary>会话状态</summary>
    public CommonStatus Status { get; private set; } = CommonStatus.Enabled;

    // EF Core needs parameterless constructor
    private AuthSession() { }

    /// <summary>
    /// 创建新的认证会话。
    /// </summary>
    public static AuthSession Create(
        Guid userId,
        string tokenHash,
        DateTime expiryTime,
        string ipAddress,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("令牌哈希不能为空", nameof(tokenHash));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP地址不能为空", nameof(ipAddress));

        return new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            LoginTime = DateTime.UtcNow,
            ExpiryTime = expiryTime,
            IpAddress = ipAddress.Trim(),
            UserAgent = userAgent?.Trim(),
            Status = CommonStatus.Enabled
        };
    }

    /// <summary>
    /// 登出会话。
    /// </summary>
    public void Logout()
    {
        if (IsRevoked)
            throw new InvalidOperationException("会话已被撤销");

        LogoutTime = DateTime.UtcNow;
        Status = CommonStatus.Disabled;
    }

    /// <summary>
    /// 撤销会话（强制登出）。
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
        LogoutTime = DateTime.UtcNow;
        Status = CommonStatus.Disabled;
    }

    /// <summary>
    /// 检查会话是否有效。
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked
            && Status == CommonStatus.Enabled
            && LogoutTime == null
            && DateTime.UtcNow < ExpiryTime;
    }

    /// <summary>
    /// 检查会话是否已过期。
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiryTime;
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Domain/AuthSession.cs
git commit -m "feat(users-domain): add AuthSession aggregate root with session lifecycle"
```

---

### Task 4: Domain Events

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/UserCreatedEvent.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/UserDeletedEvent.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/UserStatusChangedEvent.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/UserRoleChangedEvent.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/UserLoggedInEvent.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Events/UserLoggedOutEvent.cs`

**Interfaces:**
- Consumes: `LYBT.SharedKernel.Events.IDomainEvent`
- Produces: Domain events for user lifecycle

- [ ] **Step 1: Create UserCreatedEvent**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Events/UserCreatedEvent.cs
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户创建事件。当新用户被创建时触发。
/// </summary>
public sealed record UserCreatedEvent(
    Guid UserId,
    string UserName,
    string RealName,
    UserRole Role,
    Guid? CreatedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create UserDeletedEvent**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Events/UserDeletedEvent.cs
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户删除事件。当用户被软删除时触发。
/// </summary>
public sealed record UserDeletedEvent(
    Guid UserId,
    string UserName,
    string RealName,
    Guid DeletedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Create UserStatusChangedEvent**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Events/UserStatusChangedEvent.cs
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户状态变更事件。当用户启用/禁用时触发。
/// </summary>
public sealed record UserStatusChangedEvent(
    Guid UserId,
    string UserName,
    CommonStatus OldStatus,
    CommonStatus NewStatus,
    Guid ChangedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

- [ ] **Step 4: Create UserRoleChangedEvent**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Events/UserRoleChangedEvent.cs
using LYBT.Shared.Models.Enums;
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户角色变更事件。当用户角色被修改时触发。
/// </summary>
public sealed record UserRoleChangedEvent(
    Guid UserId,
    string UserName,
    UserRole OldRole,
    UserRole NewRole,
    Guid ChangedBy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

- [ ] **Step 5: Create UserLoggedInEvent**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Events/UserLoggedInEvent.cs
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户登录事件。当用户成功登录时触发。
/// </summary>
public sealed record UserLoggedInEvent(
    Guid UserId,
    string UserName,
    string IpAddress,
    string? UserAgent
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

- [ ] **Step 6: Create UserLoggedOutEvent**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Events/UserLoggedOutEvent.cs
using LYBT.SharedKernel.Events;

namespace LYBT.Module.Users.Domain.Events;

/// <summary>
/// 用户登出事件。当用户登出时触发。
/// </summary>
public sealed record UserLoggedOutEvent(
    Guid UserId,
    string UserName,
    Guid SessionId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

- [ ] **Step 7: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Domain/Events/
git commit -m "feat(users-domain): add domain events for user lifecycle"
```

---

### Task 5: Domain Services

**Files:**
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Services/PasswordPolicy.cs`
- Create: `src/Server/Modules/LYBT.Module.Users/Domain/Services/IUserNumberGenerator.cs`

**Interfaces:**
- Consumes: `LYBT.Shared.Models.Enums`
- Produces: Domain services for user management

- [ ] **Step 1: Create PasswordPolicy domain service**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Services/PasswordPolicy.cs
namespace LYBT.Module.Users.Domain.Services;

/// <summary>
/// 密码策略领域服务。封装密码复杂度验证规则。
/// </summary>
public static class PasswordPolicy
{
    /// <summary>最小密码长度</summary>
    public const int MinLength = 8;

    /// <summary>最大密码长度</summary>
    public const int MaxLength = 128;

    /// <summary>
    /// 验证密码是否符合策略要求。
    /// </summary>
    /// <param name="password">待验证密码</param>
    /// <returns>验证结果</returns>
    public static PasswordValidationResult Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("密码不能为空");
            return new PasswordValidationResult(false, errors);
        }

        if (password.Length < MinLength)
            errors.Add($"密码长度不能少于{MinLength}个字符");

        if (password.Length > MaxLength)
            errors.Add($"密码长度不能超过{MaxLength}个字符");

        if (!char.IsUpper(password[0]))
            errors.Add("密码必须以大写字母开头");

        if (!password.Any(char.IsUpper))
            errors.Add("密码必须包含至少一个大写字母");

        if (!password.Any(char.IsLower))
            errors.Add("密码必须包含至少一个小写字母");

        if (!password.Any(char.IsDigit))
            errors.Add("密码必须包含至少一个数字");

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            errors.Add("密码必须包含至少一个特殊字符");

        return new PasswordValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 检查密码是否为常见弱密码。
    /// </summary>
    public static bool IsCommonPassword(string password)
    {
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Password1!", "Admin123!", "Qwerty1!", "Letmein1!",
            "Welcome1!", "Monkey12!", "Dragon1!", "Master1!"
        };
        return commonPasswords.Contains(password);
    }
}

/// <summary>
/// 密码验证结果。
/// </summary>
public record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors);
```

- [ ] **Step 2: Create IUserNumberGenerator**

```csharp
// src/Server/Modules/LYBT.Module.Users/Domain/Services/IUserNumberGenerator.cs
namespace LYBT.Module.Users.Domain.Services;

/// <summary>
/// 用户编号生成器接口。用于生成用户工号等唯一标识。
/// </summary>
public interface IUserNumberGenerator
{
    /// <summary>
    /// 生成下一个用户工号。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户工号</returns>
    Task<string> GenerateNextAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build src/Server/Modules/LYBT.Module.Users/LYBT.Module.Users.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/Server/Modules/LYBT.Module.Users/Domain/Services/
git commit -m "feat(users-domain): add PasswordPolicy and IUserNumberGenerator domain services"
```

---

### Task 6: Verify Full Solution Build

**Files:**
- None (verification only)

- [ ] **Step 1: Build full solution**

Run: `dotnet build LYBTZYZS.sln --verbosity minimal`
Expected: Build succeeded (0 errors)

- [ ] **Step 2: Verify no new warnings from Domain layer**

Expected: Only pre-existing warnings, no new warnings from Users.Domain

- [ ] **Step 3: Final commit (if any fixes needed)**

```bash
git add -A
git commit -m "fix(users-domain): address build issues"
```
