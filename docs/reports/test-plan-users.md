# Users 模块测试计划

**日期**: 2025-10-03
**相关Issue**: #864 - Phase 2.2
**目标覆盖率**: 17% → 80%
**预计工作量**: 1周

---

## 模块概览

### 源代码结构
- **Services**: `UserService.cs` (29个方法 - 最复杂的模块)
- **Repositories**: `UserRepository.cs`
- **Validators**: `UserCreateDtoValidator.cs`, `UserUpdateDtoValidator.cs`
- **Mapping**: `UserMappingProfile.cs`
- **Interfaces**: `IUserService.cs`, `IUserRepository.cs`

### 现有测试
- `UserServiceTests.cs` (基础测试)
- `UserMappingProfileTests.cs` (AutoMapper测试)

---

## 测试计划清单

### 1️⃣ UserService 测试 (优先级: 🔴 高)

**测试文件**: `UserServiceTests.cs`

#### 1.1 CRUD 操作 (8个测试)

##### CreateUserAsync
- [ ] `CreateUserAsync_WithValidData_ReturnsSuccessResult`
- [ ] `CreateUserAsync_WithDuplicateUsername_ReturnsFailureResult`
- [ ] `CreateUserAsync_WithNullDto_ThrowsArgumentNullException`
- [ ] `CreateUserAsync_HashesPasswordCorrectly`

##### GetByIdAsync
- [ ] `GetByIdAsync_WithExistingId_ReturnsUserDto`
- [ ] `GetByIdAsync_WithNonExistentId_ReturnsNull`

##### UpdateUserAsync
- [ ] `UpdateUserAsync_WithValidData_ReturnsSuccessResult`
- [ ] `UpdateUserAsync_WithNonExistentId_ReturnsNotFoundResult`

##### DeleteUserAsync
- [ ] `DeleteUserAsync_WithExistingId_ReturnsSuccessResult`
- [ ] `DeleteUserAsync_WithNonExistentId_ReturnsNotFoundResult`

#### 1.2 查询操作 (8个测试)

##### GetPagedAsync
- [ ] `GetPagedAsync_WithValidParameters_ReturnsPagedResult`
- [ ] `GetPagedAsync_WithInvalidPageSize_ThrowsArgumentException`

##### GetByUsernameAsync
- [ ] `GetByUsernameAsync_WithExistingUsername_ReturnsUser`
- [ ] `GetByUsernameAsync_WithNonExistentUsername_ReturnsNull`
- [ ] `GetByUsernameAsync_CaseInsensitive_ReturnsUser`

##### GetByEmailAsync
- [ ] `GetByEmailAsync_WithExistingEmail_ReturnsUser`
- [ ] `GetByEmailAsync_WithNonExistentEmail_ReturnsNull`

##### GetByUsernameOrEmailAsync
- [ ] `GetByUsernameOrEmailAsync_ByUsername_ReturnsUser`
- [ ] `GetByUsernameOrEmailAsync_ByEmail_ReturnsUser`

##### SearchAsync
- [ ] `SearchAsync_WithKeyword_ReturnsMatchingUsers`
- [ ] `SearchAsync_WithEmptyKeyword_ReturnsAllUsers`

#### 1.3 角色与权限 (6个测试)

##### GetRolesAsync
- [ ] `GetRolesAsync_WithExistingUser_ReturnsRoles`
- [ ] `GetRolesAsync_WithNonExistentUser_ReturnsEmptyList`

##### GetActiveUsersAsync
- [ ] `GetActiveUsersAsync_ReturnsOnlyActiveUsers`
- [ ] `GetActiveUsersAsync_ExcludesDisabledUsers`

##### GetDoctorsAsync
- [ ] `GetDoctorsAsync_ReturnsOnlyDoctorRoleUsers`
- [ ] `GetDoctorsAsync_WithFilters_ReturnsFilteredDoctors`

##### IsDoctorAvailableAsync
- [ ] `IsDoctorAvailableAsync_WhenAvailable_ReturnsTrue`
- [ ] `IsDoctorAvailableAsync_WhenNotAvailable_ReturnsFalse`

#### 1.4 用户名与密码验证 (8个测试)

##### ValidateUsernameAsync
- [ ] `ValidateUsernameAsync_WithValidUsername_ReturnsTrue`
- [ ] `ValidateUsernameAsync_WithInvalidFormat_ReturnsFalse`
- [ ] `ValidateUsernameAsync_WithExistingUsername_ReturnsFalse`

##### ValidatePasswordAsync
- [ ] `ValidatePasswordAsync_WithStrongPassword_ReturnsTrue`
- [ ] `ValidatePasswordAsync_WithWeakPassword_ReturnsFalse`
- [ ] `ValidatePasswordAsync_WithCorrectCurrentPassword_ReturnsTrue`
- [ ] `ValidatePasswordAsync_WithIncorrectCurrentPassword_ReturnsFalse`

##### ChangePasswordAsync
- [ ] `ChangePasswordAsync_WithValidData_UpdatesPassword`
- [ ] `ChangePasswordAsync_WithWrongOldPassword_ReturnsFailure`
- [ ] `ChangePasswordAsync_HashesNewPasswordCorrectly`

#### 1.5 登录尝试与锁定 (10个测试)

##### UpdateLastLoginTimeAsync
- [ ] `UpdateLastLoginTimeAsync_UpdatesTimestamp`
- [ ] `UpdateLastLoginTimeAsync_NonExistentUser_DoesNotThrow`

##### IncrementFailedLoginCountAsync
- [ ] `IncrementFailedLoginCountAsync_IncrementsCount`
- [ ] `IncrementFailedLoginCountAsync_SetsLockoutTimeAfterMaxAttempts`
- [ ] `IncrementFailedLoginCountAsync_DoesNotLockBeforeMaxAttempts`

##### ResetFailedLoginCountAsync
- [ ] `ResetFailedLoginCountAsync_ResetsCountToZero`
- [ ] `ResetFailedLoginCountAsync_ClearsLockoutTime`

##### IsAccountLockedAsync
- [ ] `IsAccountLockedAsync_WhenLocked_ReturnsTrue`
- [ ] `IsAccountLockedAsync_WhenNotLocked_ReturnsFalse`
- [ ] `IsAccountLockedAsync_WhenLockoutExpired_ReturnsFalse`

#### 1.6 用户状态管理 (8个测试)

##### EnableAsync
- [ ] `EnableAsync_WithDisabledUser_EnablesUser`
- [ ] `EnableAsync_WithAlreadyEnabledUser_DoesNothing`

##### DisableAsync
- [ ] `DisableAsync_WithEnabledUser_DisablesUser`
- [ ] `DisableAsync_WithReason_SavesReason`

##### BatchEnableAsync
- [ ] `BatchEnableAsync_WithMultipleUsers_EnablesAll`
- [ ] `BatchEnableAsync_WithEmptyList_DoesNothing`

##### BatchDisableAsync
- [ ] `BatchDisableAsync_WithMultipleUsers_DisablesAll`
- [ ] `BatchDisableAsync_WithReason_SavesReasonForAll`

#### 1.7 密码重置与个人资料 (4个测试)

##### ResetPasswordAsync
- [ ] `ResetPasswordAsync_WithValidData_ResetsPassword`
- [ ] `ResetPasswordAsync_GeneratesRandomPasswordIfNotProvided`

##### ChangeProfileAsync
- [ ] `ChangeProfileAsync_WithValidData_UpdatesProfile`
- [ ] `ChangeProfileAsync_PreservesPasswordAndUsername`

**预计测试数**: 52个

---

### 2️⃣ UserRepository 测试 (优先级: 🔴 高)

**测试文件**: `UserRepositoryTests.cs`

#### 基本查询方法

- [ ] `GetByUsernameAsync_WithExistingUsername_ReturnsUser`
- [ ] `GetByUsernameAsync_CaseInsensitive_ReturnsUser`
- [ ] `GetByEmailAsync_WithExistingEmail_ReturnsUser`
- [ ] `GetWithRolesAsync_IncludesRoleData`
- [ ] `GetActiveUsersAsync_ReturnsOnlyActiveUsers`
- [ ] `GetUsersByRoleAsync_ReturnsUsersWithSpecificRole`
- [ ] `IsUsernameExistsAsync_WithExistingUsername_ReturnsTrue`
- [ ] `IsEmailExistsAsync_WithExistingEmail_ReturnsTrue`
- [ ] `SearchUsersAsync_ByKeyword_ReturnsMatches`

**预计测试数**: 9个

---

### 3️⃣ Validator 测试 (优先级: 🟡 中)

#### 3.1 UserCreateDtoValidator

**测试文件**: `UserCreateDtoValidatorTests.cs`

- [ ] `Validate_WithValidData_PassesValidation`
- [ ] `Validate_WithEmptyUsername_FailsValidation`
- [ ] `Validate_WithUsernameTooShort_FailsValidation`
- [ ] `Validate_WithUsernameTooLong_FailsValidation`
- [ ] `Validate_WithInvalidUsernameFormat_FailsValidation`
- [ ] `Validate_WithEmptyPassword_FailsValidation`
- [ ] `Validate_WithWeakPassword_FailsValidation`
- [ ] `Validate_WithInvalidEmail_FailsValidation`
- [ ] `Validate_WithEmptyRealName_FailsValidation`

#### 3.2 UserUpdateDtoValidator

**测试文件**: `UserUpdateDtoValidatorTests.cs`

- [ ] `Validate_WithValidData_PassesValidation`
- [ ] `Validate_WithEmptyId_FailsValidation`
- [ ] `Validate_WithInvalidEmail_FailsValidation`
- [ ] `Validate_WithEmptyRealName_FailsValidation`

**预计测试数**: 13个

---

### 4️⃣ Mapping 测试 (优先级: 🟡 中)

**测试文件**: `UserMappingProfileTests.cs` (已存在，需补充)

#### 补充测试

- [ ] `Map_UserToDto_MapsAllProperties`
- [ ] `Map_UserToDto_ExcludesPasswordHash`
- [ ] `Map_UserToDto_MapsRolesCorrectly`
- [ ] `Map_CreateDtoToUser_MapsAllProperties`
- [ ] `Map_UpdateDtoToUser_MapsAllProperties`
- [ ] `Map_UserList_MapsAllItems`

**预计测试数**: 6个

---

## 测试数据准备

### 使用 Bogus 生成测试数据

```csharp
public class UserTestData
{
    public static Faker<User> UserFaker = new Faker<User>()
        .RuleFor(u => u.Id, f => Guid.NewGuid())
        .RuleFor(u => u.Username, f => f.Internet.UserName())
        .RuleFor(u => u.RealName, f => f.Name.FullName())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.PasswordHash, f => BCrypt.Net.BCrypt.HashPassword("Test@123"))
        .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber("1##########"))
        .RuleFor(u => u.Status, f => CommonStatus.Enabled)
        .RuleFor(u => u.FailedLoginCount, f => 0)
        .RuleFor(u => u.CreatedAt, f => f.Date.Past(1));

    public static UserCreateDto CreateValidDto()
    {
        return new UserCreateDto
        {
            Username = "testuser",
            Password = "Test@123456",
            RealName = "测试用户",
            Email = "test@example.com",
            PhoneNumber = "13800138000"
        };
    }
}
```

---

## Mock 对象配置

### UserService 测试的 Mock 设置

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<UserService>>();

        // 配置默认值
        _mockConfig.Setup(c => c["Auth:MaxFailedLoginAttempts"]).Returns("5");
        _mockConfig.Setup(c => c["Auth:LockoutDurationMinutes"]).Returns("30");

        _sut = new UserService(_mockRepo.Object, _mockMapper.Object, _mockConfig.Object, _mockLogger.Object);
    }
}
```

---

## 验收标准

- ✅ **行覆盖率**: ≥80%
- ✅ **分支覆盖率**: ≥70%
- ✅ **方法覆盖率**: 100% (所有29个public方法)
- ✅ **测试数量**: 80个测试
- ✅ **测试通过率**: 100%
- ✅ **遵循AAA模式**
- ✅ **使用FluentAssertions断言**

---

## 实施步骤

1. **Step 1**: 创建测试文件骨架 (20分钟)
2. **Step 2**: 实现 CRUD 测试 (1.5小时)
3. **Step 3**: 实现查询操作测试 (1.5小时)
4. **Step 4**: 实现角色与权限测试 (1小时)
5. **Step 5**: 实现密码与验证测试 (1.5小时)
6. **Step 6**: 实现登录锁定测试 (1.5小时)
7. **Step 7**: 实现状态管理测试 (1小时)
8. **Step 8**: 实现 Repository 测试 (1小时)
9. **Step 9**: 实现 Validator 测试 (1小时)
10. **Step 10**: 运行并验证 (30分钟)

---

**下一步**: 开始实施 Step 1 - 创建测试文件骨架
