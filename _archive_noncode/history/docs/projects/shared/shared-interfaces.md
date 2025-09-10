# LYBT.Shared.Interfaces 共享接口定义项目文档

## 项目概览

**项目名称**: LYBT.Shared.Interfaces  
**项目类型**: 共享接口定义库  
**技术框架**: .NET 8.0 + Refit 8.0.0  
**业务领域**: API和服务接口契约定义  
**更新时间**: 2025-01-01

## 项目定位

### 核心功能
LYBT.Shared.Interfaces定义了整个系统的接口契约，包含三大类别接口：

1. **API接口层**: 基于Refit的HTTP API客户端接口定义（8个业务模块）
2. **服务接口层**: 前端业务服务接口契约定义（8个业务模块）  
3. **缓存接口层**: 缓存服务接口定义

### 架构角色
- **接口契约中心**: 定义系统内所有重要接口契约
- **类型安全保障**: 通过强类型接口确保调用安全
- **依赖注入基础**: 为IoC容器提供接口抽象
- **前后端协作桥梁**: 通过Refit实现类型安全的HTTP调用

## 技术架构

### 核心依赖
```xml
<PackageReference Include="Refit" Version="8.0.0" />
<ProjectReference Include="..\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
```

### 项目配置
```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

## API接口层 (基于Refit)

### 认证API接口

#### IAuthApi
```csharp
public interface IAuthApi
{
    /// <summary>用户登录</summary>
    [Post("/api/v1/auth/login")]
    Task<Refit.ApiResponse<ApiResponse<LoginResponse>>> LoginAsync([Body] LoginRequest request);
    
    /// <summary>用户登出</summary>
    [Post("/api/v1/auth/logout")]
    Task<Refit.ApiResponse<ApiResponse<object>>> LogoutAsync([Body] LogoutRequest request);
    
    /// <summary>刷新Token</summary>
    [Post("/api/v1/auth/refresh")]
    Task<Refit.ApiResponse<ApiResponse<LoginResponse>>> RefreshTokenAsync([Body] string refreshToken);
    
    /// <summary>修改系统管理员密码</summary>
    [Patch("/api/v1/auth/sysadmin-password")]
    Task<Refit.ApiResponse<ApiResponse<object>>> ChangeSysAdminPasswordAsync([Body] ChangeSysAdminPassword request);
    
    /// <summary>验证Token</summary>
    [Post("/api/v1/auth/validate")]
    Task<Refit.ApiResponse<ApiResponse<object>>> ValidateTokenAsync([Header("Authorization")] string token);
}
```

### 用户管理API接口

#### IUserApi
```csharp
public interface IUserApi
{
    /// <summary>获取用户列表（支持分页和查询）</summary>
    [Get("/api/v1/users")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<UserDto>>>> GetUsersAsync(
        [Query] int page = 1,
        [Query] int pageSize = 20,
        [Query] string? keyword = null,
        [Query] string? username = null,
        [Query] string? realName = null,
        [Query] string? email = null,
        [Query] string? phoneNumber = null,
        [Query] string? role = null,
        [Query] bool? isActive = null);

    /// <summary>获取用户详情</summary>
    [Get("/api/v1/users/{id}")]
    Task<Refit.ApiResponse<ApiResponse<UserDto>>> GetUserByIdAsync(Guid id);

    /// <summary>创建用户</summary>
    [Post("/api/v1/users")]
    Task<Refit.ApiResponse<ApiResponse<UserDto>>> CreateUserAsync([Body] UserMutationDto dto);

    /// <summary>更新用户</summary>
    [Put("/api/v1/users/{id}")]
    Task<Refit.ApiResponse<ApiResponse<UserDto>>> UpdateUserAsync(Guid id, [Body] UserMutationDto dto);

    /// <summary>切换用户状态</summary>
    [Patch("/api/v1/users/{id}/toggle-status")]
    Task<Refit.ApiResponse<ApiResponse<object>>> ToggleStatusAsync(Guid id);

    /// <summary>批量禁用用户</summary>
    [Patch("/api/v1/users/batch-disable")]
    Task<Refit.ApiResponse<ApiResponse<object>>> BatchDisableAsync([Body] BatchIdsDto dto);

    /// <summary>批量启用用户</summary>
    [Patch("/api/v1/users/batch-enable")]
    Task<Refit.ApiResponse<ApiResponse<object>>> BatchEnableAsync([Body] BatchIdsDto dto);

    /// <summary>重置用户密码</summary>
    [Post("/api/v1/users/reset-password/{id}")]
    Task<Refit.ApiResponse<ApiResponse<object>>> ResetPasswordAsync(Guid id);

    /// <summary>修改密码</summary>
    [Patch("/api/v1/users/password")]
    Task<Refit.ApiResponse<ApiResponse<object>>> ChangePasswordAsync([Body] ChangePasswordDto dto);

    /// <summary>修改个人信息</summary>
    [Put("/api/v1/users/profile")]
    Task<Refit.ApiResponse<ApiResponse<object>>> ChangeProfileAsync([Body] ChangeProfileDto dto);

    /// <summary>获取所有角色</summary>
    [Get("/api/v1/users/roles")]
    Task<Refit.ApiResponse<ApiResponse<IEnumerable<object>>>> GetRolesAsync();

    /// <summary>获取活跃用户列表</summary>
    [Get("/api/v1/users/active")]
    Task<Refit.ApiResponse<ApiResponse<IEnumerable<UserDto>>>> GetActiveUsersAsync();
}
```

### 业务模块API接口

#### IPatientApi
```csharp
public interface IPatientApi
{
    [Get("/api/v1/patients")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<PatientDto>>>> GetPatientsAsync([Query] PatientQueryDto query);
    
    [Get("/api/v1/patients/{id}")]
    Task<Refit.ApiResponse<ApiResponse<PatientDto>>> GetPatientByIdAsync(Guid id);
    
    [Post("/api/v1/patients")]
    Task<Refit.ApiResponse<ApiResponse<PatientDto>>> CreatePatientAsync([Body] PatientCreateDto dto);
    
    [Put("/api/v1/patients/{id}")]
    Task<Refit.ApiResponse<ApiResponse<PatientDto>>> UpdatePatientAsync(Guid id, [Body] PatientUpdateDto dto);
    
    // 更多患者相关API方法...
}
```

#### IMedicalCaseApi
```csharp
public interface IMedicalCaseApi
{
    [Get("/api/v1/medicalcases")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<MedicalCaseDto>>>> GetMedicalCasesAsync([Query] MedicalCaseQueryDto query);
    
    [Get("/api/v1/medicalcases/{id}")]
    Task<Refit.ApiResponse<ApiResponse<MedicalCaseDto>>> GetMedicalCaseByIdAsync(Guid id);
    
    [Post("/api/v1/medicalcases")]
    Task<Refit.ApiResponse<ApiResponse<MedicalCaseDto>>> CreateMedicalCaseAsync([Body] MedicalCaseCreateDto dto);
    
    // 更多医疗案例相关API方法...
}
```

#### IConsultationApi
```csharp
public interface IConsultationApi
{
    [Get("/api/v1/consultations")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<ConsultationDto>>>> GetConsultationsAsync([Query] ConsultationQueryDto query);
    
    [Post("/api/v1/consultations")]
    Task<Refit.ApiResponse<ApiResponse<ConsultationDto>>> CreateConsultationAsync([Body] ConsultationCreateDto dto);
    
    // 更多诊断相关API方法...
}
```

#### IPrescriptionApi
```csharp
public interface IPrescriptionApi
{
    [Get("/api/v1/prescriptions")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<PrescriptionDto>>>> GetPrescriptionsAsync([Query] PrescriptionQueryDto query);
    
    [Post("/api/v1/prescriptions")]
    Task<Refit.ApiResponse<ApiResponse<PrescriptionDto>>> CreatePrescriptionAsync([Body] PrescriptionCreateDto dto);
    
    [Post("/api/v1/prescriptions/calculate")]
    Task<Refit.ApiResponse<ApiResponse<PrescriptionCalculationDto>>> CalculatePrescriptionAsync([Body] PrescriptionCalculationRequestDto request);
    
    // 更多处方相关API方法...
}
```

#### IHerbApi
```csharp
public interface IHerbApi
{
    [Get("/api/v1/herbs")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<HerbDto>>>> GetHerbsAsync([Query] HerbQueryDto query);
    
    [Post("/api/v1/herbs")]
    Task<Refit.ApiResponse<ApiResponse<HerbDto>>> CreateHerbAsync([Body] HerbCreateDto dto);
    
    [Get("/api/v1/herbs/template")]
    Task<Refit.ApiResponse<byte[]>> GetImportTemplateAsync();
    
    [Post("/api/v1/herbs/import")]
    Task<Refit.ApiResponse<ApiResponse<HerbImportResultDto>>> ImportHerbsAsync([Body] List<HerbImportDto> herbs);
    
    // 更多中药材相关API方法...
}
```

#### IFormulaApi
```csharp
public interface IFormulaApi
{
    [Get("/api/v1/formulas")]
    Task<Refit.ApiResponse<ApiResponse<PagedResult<FormulaDto>>>> GetFormulasAsync([Query] FormulaQueryDto query);
    
    [Post("/api/v1/formulas")]
    Task<Refit.ApiResponse<ApiResponse<FormulaDto>>> CreateFormulaAsync([Body] FormulaCreateDto dto);
    
    [Get("/api/v1/formulas/categories")]
    Task<Refit.ApiResponse<ApiResponse<List<string>>>> GetCategoriesAsync();
    
    // 更多验方相关API方法...
}
```

## 服务接口层 (业务逻辑)

### 认证服务接口

#### IAuthService
```csharp
public interface IAuthService
{
    /// <summary>用户登录验证</summary>
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    
    /// <summary>用户登出</summary>
    Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request);
    
    /// <summary>修改sysadmin密码</summary>
    Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request);
    
    /// <summary>验证用户凭据</summary>
    Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request);
    
    /// <summary>刷新Token</summary>
    Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken);
    
    /// <summary>验证Token有效性</summary>
    Task<ServiceResult<bool>> ValidateTokenAsync(string token);
    
    /// <summary>获取用户会话信息</summary>
    Task<ServiceResult<object>> GetSessionInfoAsync(string token);
}
```

### 用户管理服务接口

#### IUserService
```csharp
public interface IUserService
{
    /// <summary>根据ID获取用户详情</summary>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    
    /// <summary>分页查询用户</summary>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserPagedQueryDto query);
    
    /// <summary>创建新用户 - UltraThink优化：使用统一变更DTO</summary>
    Task<ServiceResult<UserDto>> CreateAsync(UserMutationDto dto);
    
    /// <summary>更新用户信息 - UltraThink优化：消除ID参数重复</summary>
    Task<ServiceResult<UserDto>> UpdateAsync(UserMutationDto dto);
    
    /// <summary>删除用户（软删除）</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    
    /// <summary>启用用户</summary>
    Task<ServiceResult<bool>> EnableAsync(Guid id);
    
    /// <summary>禁用用户</summary>
    Task<ServiceResult<bool>> DisableAsync(Guid id);
    
    /// <summary>根据用户名获取用户</summary>
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
    
    /// <summary>批量启用用户</summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
    
    /// <summary>批量禁用用户</summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> ids);
    
    /// <summary>重置用户密码</summary>
    Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, string newPassword);
    
    /// <summary>修改用户密码</summary>
    Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
    
    /// <summary>修改用户个人信息 - UltraThink优化：使用DTO模式保持一致性</summary>
    Task<ServiceResult<bool>> ChangeProfileAsync(ChangeProfileDto dto);
    
    /// <summary>获取所有角色列表</summary>
    Task<ServiceResult<List<object>>> GetRolesAsync();
    
    /// <summary>获取活跃用户列表</summary>
    Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync();
    
    /// <summary>搜索用户</summary>
    Task<ServiceResult<List<UserDto>>> SearchAsync(string keyword);
    
    /// <summary>验证用户名是否可用</summary>
    Task<ServiceResult<bool>> ValidateUsernameAsync(string username);
    
    /// <summary>获取用户操作日志</summary>
    Task<ServiceResult<PagedResult<object>>> GetOperationLogsAsync(Guid userId, PagedQueryBaseDto query);
}
```

### 业务模块服务接口

#### IPatientService
```csharp
public interface IPatientService
{
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientQueryDto query);
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    // 患者特定业务方法...
}
```

#### IMedicalCaseService
```csharp
public interface IMedicalCaseService
{
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(MedicalCaseQueryDto query);
    Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto createDto);
    Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status);
    // 医疗案例特定业务方法...
}
```

#### IConsultationService
```csharp
public interface IConsultationService
{
    Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(ConsultationQueryDto query);
    Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto);
    Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto);
    // 诊断特定业务方法...
}
```

#### IPrescriptionService
```csharp
public interface IPrescriptionService
{
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto);
    Task<ServiceResult<PrescriptionCalculationDto>> CalculateAsync(PrescriptionCalculationRequestDto request);
    // 处方特定业务方法...
}
```

#### IHerbService
```csharp
public interface IHerbService
{
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(HerbQueryDto query);
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto);
    Task<ServiceResult<int>> ImportAsync(List<HerbImportDto> herbs);
    Task<ServiceResult<byte[]>> GetImportTemplateAsync();
    // 中药材特定业务方法...
}
```

#### IFormulaService
```csharp
public interface IFormulaService
{
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query);
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto createDto);
    Task<ServiceResult<List<string>>> GetCategoriesAsync();
    Task<ServiceResult<List<FormulaDto>>> GetByCategoryAsync(string category);
    // 验方特定业务方法...
}
```

## 缓存接口层

### ISimplifiedCacheService
```csharp
public interface ISimplifiedCacheService
{
    /// <summary>获取缓存项（同步）</summary>
    T? Get<T>(string key);

    /// <summary>设置缓存项（同步）</summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>移除缓存项（同步）</summary>
    bool Remove(string key);

    /// <summary>清空所有缓存（同步）</summary>
    void Clear();

    /// <summary>获取缓存项（异步）</summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>设置缓存项（异步）</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>移除缓存项（异步）</summary>
    Task<bool> RemoveAsync(string key);

    /// <summary>获取或设置缓存项（异步，核心方法）</summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
}
```

## Refit配置标准

### HTTP方法映射
```csharp
// GET请求
[Get("/api/v1/resource")]

// POST请求
[Post("/api/v1/resource")]

// PUT请求（完整更新）
[Put("/api/v1/resource/{id}")]

// PATCH请求（部分更新）
[Patch("/api/v1/resource/{id}/status")]

// DELETE请求
[Delete("/api/v1/resource/{id}")]
```

### 参数绑定规范
```csharp
// 路径参数
Task<ApiResponse<T>> GetByIdAsync(Guid id);

// 查询参数
Task<ApiResponse<T>> GetListAsync([Query] QueryDto query);

// 请求体参数
Task<ApiResponse<T>> CreateAsync([Body] CreateDto dto);

// 请求头参数
Task<ApiResponse<T>> AuthenticatedRequestAsync([Header("Authorization")] string token);
```

### 响应类型统一
所有API接口返回类型统一使用：
```csharp
Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<T>>>
```

确保类型安全和错误处理的一致性。

## 接口设计原则

### 1. 单一职责原则
每个接口专注于单一业务领域，避免接口功能混杂。

### 2. 异步优先原则
所有涉及IO操作的接口方法都使用异步模式（Task/Task&lt;T&gt;）。

### 3. 泛型类型安全
使用强类型泛型确保编译时类型检查，避免运行时错误。

### 4. ServiceResult包装
业务服务接口统一使用ServiceResult&lt;T&gt;包装返回值，确保错误处理一致性。

### 5. DTO参数模式
接口方法参数优先使用DTO对象，避免原始类型参数过多。

## 依赖注入支持

### 接口注册模式
```csharp
// API接口注册（Refit）
services.AddRefitClient<IUserApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

// 业务服务接口注册
services.AddScoped<IUserService, UserService>();

// 缓存服务接口注册
services.AddSingleton<ISimplifiedCacheService, MemoryCacheService>();
```

## 版本管理

### API版本控制
所有API接口路径包含版本号：`/api/v1/`，支持向后兼容。

### 接口演进
- 添加新方法：向后兼容
- 修改方法签名：使用新接口继承
- 删除方法：使用Obsolete标记

## 测试支持

### Mock接口
每个接口都支持Mock实现，便于单元测试：

```csharp
public class MockUserService : IUserService
{
    public Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        return Task.FromResult(ServiceResult<UserDto>.Success(new UserDto()));
    }
    
    // 其他Mock方法实现...
}
```

### 集成测试
使用真实的API接口进行集成测试，确保前后端接口契约一致。

## 性能考虑

### 1. 接口缓存
```csharp
Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
```

### 2. 批量操作
```csharp
Task<ServiceResult<int>> BatchEnableAsync(List<Guid> ids);
```

### 3. 分页查询
```csharp
Task<ServiceResult<PagedResult<T>>> GetPagedAsync(QueryDto query);
```

## 维护指南

### 添加新接口
1. 在对应的目录创建接口文件
2. 定义接口契约和方法签名
3. 添加XML文档注释
4. 更新依赖注入配置
5. 创建Mock实现用于测试

### 接口版本升级
1. 创建新版本接口（继承或独立）
2. 保持旧版本接口标记为Obsolete
3. 更新所有实现类
4. 更新文档和测试用例

---

**版本**: v1.0  
**维护**: 开发团队  
**更新**: 2025-01-01