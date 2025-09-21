# LYBT.Shared.Interfaces

> **前后端统一契约库** - .NET 8接口定义与API客户端
> UltraThink架构支持 | Refit类型安全 | ServiceResult统一返回
> **模块状态**: ✅ **生产就绪** | 🎆 **接口统一完成** | **零编译错误** | **2025-09-20更新**

## 🎯 项目概述

LYBT.Shared.Interfaces 是系统核心接口定义库，提供前后端之间的统一契约规范。定义了所有业务服务接口、API客户端接口，确保系统架构的一致性和类型安全性。完成了接口统一化改造，移除了重复的IModule接口。

**技术栈**: .NET 8 + Refit 8.0.0 + ServiceResult模式
**架构模式**: UltraThink双层架构支持 + 纯委托模式 + 接口统一化
**最新成就**: 接口统一完成，删除8个重复IModule接口，实现IService单一接口模式

## 🎆 接口统一化成果

### 接口重构成就

- ✅ **移除重复接口**: 删除所有IModule接口（8个）
- ✅ **统一服务接口**: 所有模块仅实现IService接口
- ✅ **依赖注入优化**: ViewModels统一注入IService接口
- ✅ **纯委托模式**: Module作为纯委托层，不包含业务逻辑

## 📦 项目结构

```
LYBT.Shared.Interfaces/
├── Services/                         # 业务服务接口（统一接口）
│   ├── IAuthService.cs              # 认证服务接口
│   ├── IUserService.cs              # 用户服务接口
│   ├── IPatientService.cs          # 患者服务接口
│   ├── IMedicalCaseService.cs      # 医案服务接口
│   ├── IConsultationService.cs     # 诊疗服务接口
│   ├── IPrescriptionService.cs     # 处方服务接口
│   ├── IHerbService.cs             # 药材服务接口
│   ├── IFormulaService.cs          # 验方服务接口
│   └── ICompatibilityNoteService.cs # 配伍禁忌服务接口
├── Api/                              # API客户端接口（Refit）
│   ├── IAuthApi.cs                  # 认证API接口
│   ├── IUserApi.cs                  # 用户API接口
│   ├── IPatientApi.cs               # 患者API接口
│   ├── IMedicalCaseApi.cs          # 医案API接口
│   ├── IConsultationApi.cs         # 诊疗API接口
│   ├── IPrescriptionApi.cs         # 处方API接口
│   ├── IHerbApi.cs                 # 药材API接口
│   └── IFormulaApi.cs              # 验方API接口
└── Caching/                         # （已移除，使用Infrastructure.ICacheService）
```

## 🎯 核心服务接口

### IUserService - 用户服务接口

```csharp
/// <summary>
/// 用户服务接口 - UltraThink双层架构标准
/// </summary>
/// <remarks>
/// 架构设计: UltraThink双层架构 - Module委托 → QueryService/BusinessService专业分工
/// 业务范围: 医生和管理员用户的完整生命周期管理
/// 安全特性: RBAC权限控制、密码安全策略、操作审计日志
/// </remarks>
public interface IUserService
{
    #region 查询操作 - QueryService专业负责

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    /// <remarks>
    /// 委托: Module → QueryService.GetByIdAsync
    /// 缓存: 用户信息缓存10分钟
    /// </remarks>
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页查询用户列表
    /// </summary>
    /// <remarks>
    /// 委托: Module → QueryService.GetPagedAsync
    /// 支持: 角色筛选、状态筛选、关键字搜索、多字段排序
    /// </remarks>
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto query);

    /// <summary>
    /// 高级用户搜索
    /// </summary>
    /// <remarks>
    /// 委托: Module → QueryService.SearchAsync
    /// 特性: 拼音搜索、模糊匹配、组合条件
    /// </remarks>
    Task<ServiceResult<IEnumerable<UserDto>>> SearchAsync(UserSearchDto searchDto);

    #endregion

    #region 业务操作 - BusinessService专业负责

    /// <summary>
    /// 创建新用户
    /// </summary>
    /// <remarks>
    /// 委托: Module → BusinessService.CreateAsync
    /// 验证: 用户名唯一性、密码强度、角色合法性
    /// </remarks>
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <remarks>
    /// 委托: Module → BusinessService.UpdateAsync
    /// 特性: 部分更新支持、并发控制、审计日志
    /// </remarks>
    Task<ServiceResult<UserDto>> UpdateAsync(UserUpdateDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <remarks>
    /// 委托: Module → BusinessService.DeleteAsync
    /// 策略: 软删除（状态标记）、级联处理
    /// </remarks>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <remarks>
    /// 委托: Module → BusinessService.ChangePasswordAsync
    /// 安全: 原密码验证、强度检查、历史记录
    /// </remarks>
    Task<ServiceResult<bool>> ChangePasswordAsync(ChangePasswordDto dto);

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <remarks>
    /// 委托: Module → BusinessService.ResetPasswordAsync
    /// 权限: 仅管理员可执行
    /// </remarks>
    Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto dto);

    #endregion
}
```

### IPrescriptionService - 处方服务接口

```csharp
/// <summary>
/// 处方服务接口 - 中医处方管理核心
/// </summary>
/// <remarks>
/// 业务范围: 处方开具、药材配伍、剂量计算、价格结算
/// 核心功能: 智能配伍检查、剂量自动计算、处方模板
/// </remarks>
public interface IPrescriptionService
{
    #region 查询操作

    /// <summary>
    /// 根据ID获取处方详情
    /// </summary>
    /// <remarks>
    /// 包含: 处方项目、药材明细、价格计算
    /// </remarks>
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据医案ID获取处方列表
    /// </summary>
    Task<ServiceResult<IEnumerable<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    /// <summary>
    /// 分页查询处方
    /// </summary>
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionSearchDto searchDto);

    #endregion

    #region 业务操作

    /// <summary>
    /// 创建处方
    /// </summary>
    /// <remarks>
    /// 验证: 配伍禁忌检查、剂量合理性、价格计算
    /// </remarks>
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);

    /// <summary>
    /// 更新处方
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(PrescriptionUpdateDto dto);

    /// <summary>
    /// 复制处方
    /// </summary>
    /// <remarks>
    /// 功能: 快速开具相似处方、处方模板化
    /// </remarks>
    Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid sourceId, string newName);

    /// <summary>
    /// 计算处方价格
    /// </summary>
    /// <remarks>
    /// 包含: 单帖价、总价、折扣计算
    /// </remarks>
    Task<ServiceResult<PrescriptionCalculationDto>> CalculatePriceAsync(Guid id);

    /// <summary>
    /// 验证配伍禁忌
    /// </summary>
    Task<ServiceResult<CompatibilityCheckResult>> CheckCompatibilityAsync(List<Guid> herbIds);

    #endregion
}
```

## 🔧 API客户端接口（Refit）

### Refit配置

```csharp
// 服务注册
services.AddRefitClient<IUserApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("http://localhost:5001");
        c.DefaultRequestHeaders.Add("Accept", "application/json");
    })
    .AddHttpMessageHandler<AuthHeaderHandler>();

// JWT认证处理器
public class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
```

### IUserApi - 用户API客户端

```csharp
/// <summary>
/// 用户管理API客户端接口
/// </summary>
[Headers("Authorization: Bearer")]
public interface IUserApi
{
    /// <summary>
    /// 获取用户列表
    /// </summary>
    [Get("/api/v1/users")]
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
        [Query] UserSearchDto searchDto);

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [Get("/api/v1/users/{id}")]
    Task<ApiResponse<UserDto>> GetUserAsync(Guid id);

    /// <summary>
    /// 创建用户
    /// </summary>
    [Post("/api/v1/users")]
    Task<ApiResponse<UserDto>> CreateUserAsync(
        [Body] UserCreateDto dto);

    /// <summary>
    /// 更新用户
    /// </summary>
    [Put("/api/v1/users/{id}")]
    Task<ApiResponse<UserDto>> UpdateUserAsync(
        Guid id,
        [Body] UserUpdateDto dto);

    /// <summary>
    /// 删除用户
    /// </summary>
    [Delete("/api/v1/users/{id}")]
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
}
```

## 📊 ServiceResult统一返回模式

### ServiceResult<T>定义

```csharp
/// <summary>
/// 服务层统一结果包装
/// </summary>
public class ServiceResult<T>
{
    /// <summary>操作是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>返回数据</summary>
    public T? Data { get; set; }

    /// <summary>错误消息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>异常信息（仅开发环境）</summary>
    public Exception? Exception { get; set; }

    // 静态工厂方法
    public static ServiceResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };

    public static ServiceResult<T> Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
```

### 使用示例

```csharp
// 服务实现
public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
{
    try
    {
        // 验证业务规则
        if (await _repository.ExistsAsync(u => u.Username == dto.Username))
        {
            return ServiceResult<UserDto>.Failure("用户名已存在");
        }

        // 创建用户
        var user = _mapper.Map<User>(dto);
        user.PasswordHash = _passwordHasher.HashPassword(dto.Password);

        await _repository.AddAsync(user);
        await _unitOfWork.CommitAsync();

        var userDto = _mapper.Map<UserDto>(user);
        return ServiceResult<UserDto>.Success(userDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建用户失败");
        return ServiceResult<UserDto>.Failure($"创建失败: {ex.Message}");
    }
}

// 控制器调用
public async Task<IActionResult> CreateUser(UserCreateDto dto)
{
    var result = await _userService.CreateAsync(dto);

    if (result.IsSuccess)
        return Ok(ApiResponse<UserDto>.CreateSuccess(result.Data));

    return BadRequest(ApiResponse<UserDto>.CreateFailure(result.ErrorMessage));
}
```

## 🎆 UltraThink架构支持

### 纯委托模式实现

```csharp
/// <summary>
/// UserModule - 纯委托实现
/// </summary>
public class UserModule : IUserService
{
    private readonly IUserQueryService _queryService;
    private readonly IUserBusinessService _businessService;

    public UserModule(
        IUserQueryService queryService,
        IUserBusinessService businessService)
    {
        _queryService = queryService;
        _businessService = businessService;
    }

    // 查询操作委托到QueryService
    public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(
        UserSearchDto query)
        => await _queryService.GetPagedAsync(query);

    // 业务操作委托到BusinessService
    public async Task<ServiceResult<UserDto>> CreateAsync(
        UserCreateDto dto)
        => await _businessService.CreateAsync(dto);
}
```

### 依赖注入配置

```csharp
// 服务注册 - 统一使用IService接口
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册主服务 - 使用IService接口
    containerRegistry.Register<IUserService, UserModule>();

    // 注册专业服务
    containerRegistry.Register<IUserQueryService, UserQueryService>();
    containerRegistry.Register<IUserBusinessService, UserBusinessService>();

    // 注册API客户端
    containerRegistry.RegisterSingleton<IUserApi>(() =>
        RestService.For<IUserApi>(containerProvider.Resolve<HttpClient>()));
}

// ViewModel注入 - 使用IService接口
public class UserViewModel : ViewModelBase
{
    private readonly IUserService _userService;  // 注入IService，不是Module

    public UserViewModel(IUserService userService)
    {
        _userService = userService;
    }
}
```

## 🎯 最佳实践

### 1. 接口设计原则

- ✅ 单一职责：每个接口专注一个业务领域
- ✅ 接口隔离：细粒度接口，避免大而全
- ✅ 依赖倒置：依赖抽象而非具体实现
- ✅ 统一命名：IXxxService模式

### 2. 方法命名规范

- ✅ 查询操作：GetXxxAsync、SearchAsync、FindAsync
- ✅ 创建操作：CreateAsync、AddAsync
- ✅ 更新操作：UpdateAsync、ModifyAsync
- ✅ 删除操作：DeleteAsync、RemoveAsync

### 3. 返回值约定

- ✅ 统一使用ServiceResult<T>包装
- ✅ 分页查询返回PagedResult<T>
- ✅ 批量操作返回BatchResult<T>
- ✅ 异步方法返回Task<ServiceResult<T>>

### 4. 参数设计

- ✅ 查询使用SearchDto
- ✅ 创建使用CreateDto
- ✅ 更新使用UpdateDto
- ✅ ID参数使用Guid类型

## 📈 性能优化

- **接口粒度**: 合理划分接口，避免过度设计
- **异步优先**: 所有I/O操作使用异步方法
- **批量操作**: 提供批量接口减少网络往返
- **缓存策略**: 接口级别定义缓存策略

## 🔒 安全考虑

- **认证授权**: 所有接口需要JWT认证
- **权限控制**: 基于角色的访问控制（RBAC）
- **数据验证**: DTO级别的输入验证
- **审计日志**: 关键操作记录审计日志

---

> 📌 **最新成果**: 接口统一完成，IService单一接口模式，架构更清晰
> 🎆 **生产就绪**: 完整的接口体系，支持UltraThink双层架构