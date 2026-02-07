# 测试设计方案 - LYBT.Desktop.Users.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Client/Desktop/Modules/LYBT.Desktop.Users/` |
| **测试路径** | `tests/UnitTests/Client/Desktop/LYBT.Desktop.Users.Tests/` |
| **现有测试数** | 1 (占位测试) |
| **目标测试数** | 70 |
| **新增测试数** | +69 |
| **优先级** | P1 |

---

## 2. 被测组件清单

### 2.1 Services & Components

| 类 | 方法数 | 目标测试 |
|----|--------|----------|
| UserService | 14 | 20 |
| UserPasswordHandler | 2 | 4 |
| UserStatusHandler | 4 | 6 |
| UserAuditHandler | 2 | 2 |
| UserImportExportHandler | 3 | 4 |

### 2.2 Repository & CommandHandler

| 类 | 方法数 | 目标测试 |
|----|--------|----------|
| UserRepository | 15 | 22 |
| UserCommandHandler | 6 | 8 |

### 2.3 Models

| 类 | 方法数 | 目标测试 |
|----|--------|----------|
| UserDetailModel | 2 | 4 |
| UserItem | 11 | 6 |

---

## 3. UserService 测试设计 (20个)

### 3.1 CRUD 操作 (8个)

```
CreateAsync_WithValidInput_ShouldReturnSuccess
CreateAsync_WithDuplicateUsername_ShouldReturnFailure
CreateAsync_WithInvalidRole_ShouldReturnFailure
UpdateAsync_WithValidInput_ShouldReturnSuccess
UpdateAsync_WithNonExistentId_ShouldReturnFailure
DeleteAsync_WithExistingId_ShouldReturnSuccess
DeleteAsync_WithSystemUser_ShouldReturnFailure
BatchDeleteAsync_WithValidIds_ShouldDeleteAll
```

### 3.2 查询操作 (6个)

```
GetByIdAsync_WithExistingId_ShouldReturnUser
GetByIdAsync_WithNonExistentId_ShouldReturnFailure
GetPagedAsync_ShouldReturnPagedResult
GetByUsernameAsync_WithExistingUsername_ShouldReturn
SearchAsync_WithKeyword_ShouldReturnMatches
GetDoctorsAsync_ShouldReturnOnlyDoctors
```

### 3.3 个人资料和状态 (6个)

```
ChangeProfileAsync_WithValidInput_ShouldUpdate
ChangeProfileAsync_WithNonExistentUser_ShouldReturnFailure
ToggleStatusAsync_ShouldToggleUserStatus
ChangePasswordAsync_WithValidOldPassword_ShouldChange
ChangePasswordAsync_WithInvalidOldPassword_ShouldReturnFailure
ResetPasswordAsync_ShouldResetAndReturnNewPassword
```

---

## 4. UserRepository 测试设计 (22个)

### 4.1 基础 CRUD (8个)

```
GetPagedAsync_ShouldReturnPagedResult
GetPagedAsync_WithKeyword_ShouldFilter
GetByIdAsync_WithExistingId_ShouldReturnUser
GetByIdAsync_WithNonExistentId_ShouldReturnNull
CreateAsync_WithValidInput_ShouldCreate
UpdateAsync_WithExistingId_ShouldUpdate
DeleteAsync_WithExistingId_ShouldSoftDelete
DeleteAsync_WithNonExistentId_ShouldReturnFalse
```

### 4.2 特殊查询 (4个)

```
GetByUsernameAsync_WithExistingUsername_ShouldReturn
GetByUsernameAsync_WithNonExistentUsername_ShouldReturnNull
SearchAsync_WithKeyword_ShouldReturnMatches
GetDoctorsAsync_ShouldReturnOnlyDoctors
```

### 4.3 密码管理 (4个)

```
ChangePasswordAsync_WithValidOldPassword_ShouldSucceed
ChangePasswordAsync_WithInvalidOldPassword_ShouldFail
ResetPasswordAsync_ShouldResetPassword
ResetPasswordAsync_WithNonExistentUser_ShouldFail
```

### 4.4 批量操作 (4个)

```
BatchImportAsync_WithValidData_ShouldImportAll
BatchDeleteAsync_WithValidIds_ShouldDeleteAll
BatchEnableAsync_ShouldEnableAll
BatchDisableAsync_ShouldDisableAll
```

### 4.5 状态管理 (2个)

```
ToggleStatusAsync_ShouldToggleStatus
RestoreAsync_WithDeletedUser_ShouldRestore
```

---

## 5. UserCommandHandler 测试设计 (8个)

```
GetListAsync_ShouldReturnPagedResult
GetDetailAsync_WithExistingId_ShouldReturnDetail
GetDetailAsync_WithNonExistentId_ShouldReturnNull
SaveAsync_WithNewUser_ShouldCreate
SaveAsync_WithExistingUser_ShouldUpdate
SearchByUsernameAsync_ShouldReturnMatches
ResetPasswordAsync_ShouldResetAndReturnResult
SetActiveStatusAsync_ShouldSetStatus
```

---

## 6. Handler 测试设计

### 6.1 UserPasswordHandler (4个)

```
ResetPasswordAsync_WithValidUser_ShouldReset
ResetPasswordAsync_ShouldShowDialog
CanResetPassword_WithSelectedUser_ShouldReturnTrue
CanResetPassword_WhenBusy_ShouldReturnFalse
```

### 6.2 UserStatusHandler (6个)

```
ToggleUserStatusAsync_ShouldToggle
ToggleUserStatusAsync_ShouldRefreshList
RestoreAsync_ShouldRestoreUser
RestoreAsync_ShouldRefreshList
CanToggleUserStatus_WithSelectedUser_ShouldReturnTrue
CanRestore_WhenNotAdmin_ShouldReturnFalse
```

### 6.3 UserAuditHandler (2个)

```
ShowAuditLog_ShouldOpenDialog
CanShowAuditLog_WithSelectedUser_ShouldReturnTrue
```

### 6.4 UserImportExportHandler (4个)

```
ImportAsync_WithValidFile_ShouldImport
ExportAsync_ShouldExportToFile
ExportAsync_WithSearchText_ShouldFilterExport
DownloadTemplateAsync_ShouldDownloadTemplate
```

---

## 7. Models 测试设计

### 7.1 UserDetailModel (4个)

```
CreateNew_ShouldReturnNewInstance
CreateNew_ShouldSetIsNewTrue
Clone_ShouldCopyAllProperties
Clone_ShouldReturnNewInstance
```

### 7.2 UserItem (6个)

```
RoleDisplayText_WithAdmin_ShouldReturnChineseText
RoleDisplayText_WithDoctor_ShouldReturnChineseText
StatusText_WithEnabled_ShouldReturn正常
StatusText_WithDisabled_ShouldReturn禁用
CanDelete_WithSysadmin_ShouldReturnFalse
UpdateFromDto_ShouldUpdateAllProperties
```

---

## 8. 测试数据设计

### 8.1 TestUserBuilder

```csharp
public static class TestUserBuilder
{
    public static User Create(
        Guid? id = null,
        string? username = null,
        string? realName = null,
        UserRole? role = null,
        CommonStatus? status = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username ?? $"user_{Guid.NewGuid():N}".Substring(0, 15),
            RealName = realName ?? "测试用户",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Role = role ?? UserRole.Doctor,
            Status = status ?? CommonStatus.Enabled,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UserInputDto CreateInputDto(
        string? username = null,
        string? realName = null,
        UserRole? role = null)
    {
        return new UserInputDto
        {
            UserName = username ?? $"user_{Guid.NewGuid():N}".Substring(0, 15),
            RealName = realName ?? "测试用户",
            Role = role ?? UserRole.Doctor,
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };
    }

    public static UserDetailModel CreateDetailModel(
        Guid? id = null,
        string? username = null)
    {
        return new UserDetailModel
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username ?? "testuser",
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled
        };
    }

    public static UserListDto CreateListDto(
        Guid? id = null,
        string? username = null,
        UserRole? role = null)
    {
        return new UserListDto
        {
            Id = id ?? Guid.NewGuid(),
            UserName = username ?? "testuser",
            RealName = "测试用户",
            Role = role ?? UserRole.Doctor,
            Status = CommonStatus.Enabled
        };
    }
}
```

---

## 9. Mock 策略

### 9.1 UserServiceTests Mock

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();

        // 默认: 用户名不存在
        _repositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((UserDetailDto?)null);

        _sut = new UserService(
            _repositoryMock.Object,
            NullLogger<UserService>.Instance);
    }
}
```

### 9.2 UserPasswordHandlerTests Mock

```csharp
public class UserPasswordHandlerTests
{
    private readonly Mock<IUserService> _serviceMock;
    private readonly Mock<ICommonDialogService> _dialogMock;
    private readonly UserPasswordHandler _sut;

    public UserPasswordHandlerTests()
    {
        _serviceMock = new Mock<IUserService>();
        _dialogMock = new Mock<ICommonDialogService>();

        // 默认: 显示对话框
        _dialogMock
            .Setup(x => x.ShowInputDialog(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("NewPassword@123");

        _sut = new UserPasswordHandler(
            _serviceMock.Object,
            _dialogMock.Object,
            NullLogger<UserPasswordHandler>.Instance);
    }
}
```

---

## 10. 验收标准

| 指标 | 目标 |
|------|------|
| UserService 测试数 | 20 |
| UserRepository 测试数 | 22 |
| UserCommandHandler 测试数 | 8 |
| UserPasswordHandler 测试数 | 4 |
| UserStatusHandler 测试数 | 6 |
| UserAuditHandler 测试数 | 2 |
| UserImportExportHandler 测试数 | 4 |
| UserDetailModel 测试数 | 4 |
| UserItem 测试数 | 6 |
| 总测试数 | 76 |
| 全部测试通过 | 100% |

---

## 11. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | UserService 测试 (20个) | 40min |
| 2 | UserRepository 测试 (22个) | 45min |
| 3 | UserCommandHandler 测试 (8个) | 20min |
| 4 | Handler 测试 (16个) | 35min |
| 5 | Models 测试 (10个) | 20min |
| 6 | 编译验证和修复 | 20min |
| **总计** | | **~3h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
