# 测试设计方案 - LYBT.Module.Users.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Server/Modules/LYBT.Module.Users/` |
| **测试路径** | `tests/UnitTests/Server/Modules/LYBT.Module.Users.Tests/` |
| **现有测试数** | 14 |
| **目标测试数** | 50 |
| **新增测试数** | +36 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 UserService (14个公开方法)

| 方法 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| GetPagedAsync | 2 | 4 | +2 |
| GetByIdAsync | 2 | 3 | +1 |
| SearchAsync | 1 | 3 | +2 |
| CreateAsync | 3 | 6 | +3 |
| UpdateAsync | 2 | 5 | +3 |
| DeleteAsync | 2 | 5 | +3 |
| ResetPasswordAsync | 0 | 4 | +4 |
| ValidatePasswordAsync | 0 | 3 | +3 |
| ChangePasswordAsync | 0 | 4 | +4 |
| ChangeProfileAsync | 0 | 3 | +3 |
| ToggleStatusAsync | 0 | 3 | +3 |
| RestoreAsync | 0 | 3 | +3 |
| BatchDeleteAsync | 0 | 4 | +4 |
| BatchUpdateStatusAsync | 0 | 3 | +3 |
| **小计** | **14** | **53** | **+38** |

---

## 3. UserService 补充测试设计

### 3.1 权限控制测试 (8个)

```
CreateAsync_SuperAdminCreatingAdmin_ShouldSucceed
CreateAsync_AdminCreatingDoctor_ShouldSucceed
CreateAsync_AdminCreatingAdmin_ShouldReturnForbidden
CreateAsync_DoctorCreatingAny_ShouldReturnForbidden
UpdateAsync_WithInsufficientPermission_ShouldReturnForbidden
DeleteAsync_WithInsufficientPermission_ShouldReturnForbidden
DeleteAsync_LastAdminProtection_ShouldReturnFailure
DeleteAsync_SelfDelete_ShouldReturnFailure
```

**测试要点**:
- 验证 CanManageUser 权限检查
- 验证角色层级: SuperAdmin > Admin > Doctor
- 验证最后一个管理员保护

### 3.2 密码管理测试 (11个)

```
ResetPasswordAsync_WithValidUser_ShouldResetToDefault
ResetPasswordAsync_WithNonExistentUser_ShouldReturnFailure
ResetPasswordAsync_ShouldReturnNewPassword
ResetPasswordAsync_WithInsufficientPermission_ShouldReturnForbidden
ValidatePasswordAsync_WithValidPassword_ShouldReturnUser
ValidatePasswordAsync_WithInvalidPassword_ShouldReturnFailure
ValidatePasswordAsync_WithNonExistentUser_ShouldReturnFailure
ChangePasswordAsync_WithValidOldPassword_ShouldChange
ChangePasswordAsync_WithInvalidOldPassword_ShouldReturnFailure
ChangePasswordAsync_WithNonExistentUser_ShouldReturnFailure
ChangePasswordAsync_ShouldHashNewPassword
```

### 3.3 个人资料测试 (3个)

```
ChangeProfileAsync_WithValidInput_ShouldUpdate
ChangeProfileAsync_WithNonExistentUser_ShouldReturnFailure
ChangeProfileAsync_ShouldNotChangeRole
```

### 3.4 状态管理测试 (6个)

```
ToggleStatusAsync_EnabledToDisabled_ShouldToggle
ToggleStatusAsync_DisabledToEnabled_ShouldToggle
ToggleStatusAsync_WithNonExistentId_ShouldReturnFailure
RestoreAsync_WithDeletedUser_ShouldRestore
RestoreAsync_WithNonDeletedUser_ShouldReturnFailure
RestoreAsync_WithPermissionCheck_ShouldValidateRole
```

### 3.5 批量操作测试 (7个)

```
BatchDeleteAsync_WithValidIds_ShouldDeleteAll
BatchDeleteAsync_WithSelfInList_ShouldRejectSelf
BatchDeleteAsync_WithLastAdmin_ShouldProtect
BatchDeleteAsync_WithInsufficientPermission_ShouldReject
BatchUpdateStatusAsync_WithValidIds_ShouldUpdateAll
BatchUpdateStatusAsync_WithSelfInList_ShouldRejectSelf
BatchUpdateStatusAsync_WithMixedResults_ShouldReportPartial
```

### 3.6 查询补充测试 (3个)

```
GetPagedAsync_WithRoleFilter_ShouldFilter
GetPagedAsync_WithStatusFilter_ShouldFilter
SearchAsync_ShouldSearchMultipleFields
```

---

## 4. 测试数据设计

### 4.1 TestUserBuilder (Server)

```csharp
public static class TestUserBuilder
{
    public static User Create(
        Guid? id = null,
        string? username = null,
        string? realName = null,
        UserRole? role = null,
        CommonStatus? status = null,
        bool isDeleted = false)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username ?? $"user_{Guid.NewGuid():N}".Substring(0, 15),
            RealName = realName ?? "测试用户",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Role = role ?? UserRole.Doctor,
            Status = status ?? CommonStatus.Enabled,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
    }

    public static User CreateSuperAdmin(Guid? id = null)
    {
        return Create(id, "superadmin", "超级管理员", UserRole.SuperAdmin);
    }

    public static User CreateAdmin(Guid? id = null)
    {
        return Create(id, "admin", "管理员", UserRole.Admin);
    }

    public static User CreateDoctor(Guid? id = null)
    {
        return Create(id, "doctor", "医师", UserRole.Doctor);
    }

    public static UserInputDto CreateInputDto(
        string? username = null,
        UserRole? role = null,
        string? password = null)
    {
        return new UserInputDto
        {
            UserName = username ?? $"user_{Guid.NewGuid():N}".Substring(0, 15),
            RealName = "测试用户",
            Role = role ?? UserRole.Doctor,
            Password = password ?? "Test@123",
            ConfirmPassword = password ?? "Test@123"
        };
    }
}
```

### 4.2 TestHttpContextBuilder

```csharp
public static class TestHttpContextBuilder
{
    public static Mock<IHttpContextAccessor> CreateWithUser(
        Guid userId,
        UserRole role)
    {
        var mock = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        mock.Setup(x => x.HttpContext).Returns(context);

        return mock;
    }
}
```

---

## 5. Mock 策略

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IValidator<UserInputDto>> _validatorMock;
    private readonly Mock<IHttpContextAccessor> _httpContextMock;
    private readonly Mock<IOptions<PasswordOptions>> _passwordOptionsMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _validatorMock = new Mock<IValidator<UserInputDto>>();
        _passwordOptionsMock = new Mock<IOptions<PasswordOptions>>();

        // 默认: 管理员身份
        _httpContextMock = TestHttpContextBuilder.CreateWithUser(
            Guid.NewGuid(), UserRole.Admin);

        // 默认: 验证通过
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<UserInputDto>(), default))
            .ReturnsAsync(new ValidationResult());

        // 默认: 默认密码配置
        _passwordOptionsMock
            .Setup(x => x.Value)
            .Returns(new PasswordOptions { DefaultPassword = "Default@123" });

        _sut = new UserService(
            _repositoryMock.Object,
            _validatorMock.Object,
            _httpContextMock.Object,
            _passwordOptionsMock.Object,
            NullLogger<UserService>.Instance);
    }

    // 辅助方法: 设置当前用户
    private void SetCurrentUser(UserRole role)
    {
        _httpContextMock = TestHttpContextBuilder.CreateWithUser(
            Guid.NewGuid(), role);
        // 重建 SUT...
    }
}
```

---

## 6. 验收标准

| 指标 | 目标 |
|------|------|
| UserService 测试数 | 53 |
| 权限控制覆盖 | 100% |
| 密码管理覆盖 | 100% |
| 批量操作覆盖 | 100% |
| 状态管理覆盖 | 100% |

---

## 7. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | 权限控制测试 (8个) | 35min |
| 2 | 密码管理测试 (11个) | 40min |
| 3 | 个人资料测试 (3个) | 10min |
| 4 | 状态管理测试 (6个) | 20min |
| 5 | 批量操作测试 (7个) | 25min |
| 6 | 查询补充测试 (3个) | 10min |
| 7 | 编译验证和修复 | 15min |
| **总计** | | **~2.5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
