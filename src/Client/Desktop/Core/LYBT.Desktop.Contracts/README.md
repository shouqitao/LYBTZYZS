# LYBT.Desktop.Contracts - Desktop端API契约层

## 📦 项目定位

- **层级**:Desktop核心层(Core)
- **类型**:API契约定义
- **职责**:定义Desktop端调用WebAPI的所有接口契约，采用Refit框架实现类型安全的HTTP客户端。包含8个模块的API契约（Auth、Users、Patients、MedicalCase、Consultation、Prescription、Herbs、Formula）和处方编辑器服务契约。确保Client与Server端API保持强类型同步，编译时检查接口一致性。

## 📂 代码结构

```
LYBT.Desktop.Contracts/
├── Api/                          # Refit API接口定义(8个模块)
│   ├── IAuthApi.cs               # 认证API(6个方法)
│   │   ├── LoginAsync()          # 用户登录
│   │   ├── LogoutAsync()         # 用户登出
│   │   ├── ChangeSysAdminPasswordAsync() # 修改超管密码
│   │   ├── ValidateTokenAsync()  # 验证Token
│   │   ├── ValidateTokenFromHeaderAsync() # 从Header验证Token
│   │   └── HealthCheckAsync()    # 健康检查
│   ├── IUserApi.cs               # 用户管理API(5个方法)
│   │   ├── GetUsersAsync()       # 分页查询用户
│   │   ├── GetUserByIdAsync()    # 按ID查询用户
│   │   ├── CreateUserAsync()     # 创建用户
│   │   ├── UpdateUserAsync()     # 更新用户
│   │   └── DeleteUserAsync()     # 删除用户
│   ├── IPatientApi.cs            # 患者管理API
│   ├── IMedicalCaseApi.cs        # 医案管理API
│   ├── IConsultationApi.cs       # 诊断记录API
│   ├── IPrescriptionApi.cs       # 处方管理API
│   ├── IHerbApi.cs               # 中药材管理API
│   └── IFormulaApi.cs            # 验方管理API
└── Services/                     # 跨模块服务契约(1个)
    └── IPrescriptionEditorService.cs # 处方编辑器服务(9个方法)
        ├── LoadAllHerbsAsync()   # 加载所有药材
        ├── FilterHerbs()         # 过滤药材
        ├── LoadRecentPrescriptionsAsync() # 加载最近处方
        ├── LoadFormulasAsync()   # 加载验方
        ├── ImportFormulaAsync()  # 导入验方
        ├── BuildPrescriptionDraftAsync() # 构建处方草稿
        ├── ValidatePrescriptionAsync() # 验证处方
        ├── CalculateTotalAmountAsync() # 计算总金额
        └── PrescriptionChanged   # 处方变更事件
```

**说明**:
- **Api/**:8个Refit API接口，对应Server端8个业务模块
- **Services/**:跨模块服务契约，用于复杂业务逻辑（如处方编辑器）
- **Refit框架**:通过特性标注自动生成HTTP客户端实现
- **类型安全**:所有API方法使用强类型DTO，编译时检查
- **命名空间迁移**:从LYBT.Shared.Interfaces.Api迁移至LYBT.Desktop.Contracts.Api（v2.0）

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Shared.Models** - 共享DTO模型(UserDto、PatientDto、ApiResponse等)
2. **Refit** (NuGet) - HTTP客户端框架

### 被依赖项目
1. **LYBT.Desktop.Foundation** - Refit客户端注册和配置
2. **LYBT.Desktop.Models** - Repository层调用API接口
3. **LYBT.Desktop.Presentation** - ViewModel层间接调用（通过Repository）
4. **所有Desktop业务模块** - Auth、Users、Patients、MedicalCase、Consultation、Prescription、Herbs、Formula

### NuGet包
- **Refit** (7.x) - 类型安全的HTTP客户端框架
- **Refit.HttpClientFactory** (7.x) - 与HttpClientFactory集成

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Refit 7.x**: 类型安全的HTTP客户端（通过特性标注生成实现）
- **HttpClient**: .NET标准HTTP客户端
- **System.Text.Json**: JSON序列化/反序列化
- **异步编程**: 全异步方法(async/await),提升性能

## 🚀 快速开始

此项目是一个类库,作为Desktop客户端的一部分被引用。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Contracts/LYBT.Desktop.Contracts.csproj
```

**集成说明**:

### 1. Refit API接口定义
```csharp
using Refit;
using LYBT.Shared.Models;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 用户管理API契约
    /// </summary>
    public interface IUserApi
    {
        /// <summary>
        /// 分页查询用户
        /// </summary>
        [Get("/api/v1/users")]
        Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null);

        /// <summary>
        /// 按ID查询用户详情
        /// </summary>
        [Get("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建用户
        /// </summary>
        [Post("/api/v1/users")]
        Task<ApiResponse<UserDto>> CreateUserAsync([Body] UserCreateDto request);

        /// <summary>
        /// 更新用户
        /// </summary>
        [Put("/api/v1/users/{id}")]
        Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, [Body] UserUpdateDto request);

        /// <summary>
        /// 删除用户
        /// </summary>
        [Delete("/api/v1/users/{id}")]
        Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
    }
}
```

### 2. DI注册(在Foundation层的ContractsModule.cs中)
```csharp
using Microsoft.Extensions.DependencyInjection;
using Refit;
using LYBT.Desktop.Contracts.Api;

public static class ContractsModule
{
    public static IServiceCollection AddApiContracts(
        this IServiceCollection services,
        string apiBaseUrl)
    {
        // 注册所有Refit API客户端
        services.AddRefitClient<IAuthApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>(); // 添加认证处理器

        services.AddRefitClient<IUserApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddRefitClient<IPatientApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddRefitClient<IMedicalCaseApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddRefitClient<IConsultationApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddRefitClient<IPrescriptionApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddRefitClient<IHerbApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        services.AddRefitClient<IFormulaApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<AuthenticationHandler>();

        return services;
    }
}
```

### 3. Repository层调用(在Desktop.Models中)
```csharp
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models;

namespace LYBT.Desktop.Models.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IUserApi _userApi;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IUserApi userApi, ILogger<UserRepository> logger)
        {
            _userApi = userApi;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<UserDto>>> GetUsersAsync(
            int page,
            int pageSize,
            string? keyword = null)
        {
            try
            {
                var response = await _userApi.GetUsersAsync(page, pageSize, keyword);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ServiceResult<PagedResult<UserDto>>.Success(response.Content);
                }

                return ServiceResult<PagedResult<UserDto>>.Failure(
                    response.Error?.Message ?? "获取用户列表失败"
                );
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "调用用户API失败");
                return ServiceResult<PagedResult<UserDto>>.Failure($"API调用失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<UserDto>> CreateUserAsync(UserCreateDto dto)
        {
            try
            {
                var response = await _userApi.CreateUserAsync(dto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ServiceResult<UserDto>.Success(response.Content);
                }

                return ServiceResult<UserDto>.Failure(
                    response.Error?.Message ?? "创建用户失败"
                );
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "创建用户API调用失败");
                return ServiceResult<UserDto>.Failure($"API调用失败: {ex.Message}");
            }
        }
    }
}
```

### 4. 认证API接口(IAuthApi)
```csharp
namespace LYBT.Desktop.Contracts.Api
{
    public interface IAuthApi
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        [Post("/api/v1/auth/login")]
        Task<ApiResponse<LoginResponseDto>> LoginAsync([Body] LoginRequestDto request);

        /// <summary>
        /// 用户登出
        /// </summary>
        [Post("/api/v1/auth/logout")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<bool>> LogoutAsync();

        /// <summary>
        /// 修改超级管理员密码
        /// </summary>
        [Post("/api/v1/auth/admin/change-password")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<bool>> ChangeSysAdminPasswordAsync(
            [Body] ChangeSysAdminPasswordDto request);

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        [Post("/api/v1/auth/validate")]
        Task<ApiResponse<TokenValidationDto>> ValidateTokenAsync(
            [Body] ValidateTokenRequestDto request);

        /// <summary>
        /// 从Header验证Token
        /// </summary>
        [Get("/api/v1/auth/validate")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<bool>> ValidateTokenFromHeaderAsync();

        /// <summary>
        /// 健康检查
        /// </summary>
        [Get("/api/v1/auth/health")]
        Task<ApiResponse<HealthCheckResponseDto>> HealthCheckAsync();
    }
}
```

### 5. 处方编辑器服务契约(IPrescriptionEditorService)
```csharp
using LYBT.Shared.Models;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 处方编辑器服务契约（跨模块服务）
    /// 用于处方编辑、药材选择、验方导入等复杂业务逻辑
    /// </summary>
    public interface IPrescriptionEditorService
    {
        /// <summary>
        /// 加载所有药材
        /// </summary>
        Task<List<HerbDto>> LoadAllHerbsAsync();

        /// <summary>
        /// 过滤药材（按名称、拼音、功效）
        /// </summary>
        List<HerbDto> FilterHerbs(List<HerbDto> allHerbs, string keyword);

        /// <summary>
        /// 加载患者最近处方（用于参考）
        /// </summary>
        Task<List<PrescriptionDto>> LoadRecentPrescriptionsAsync(Guid patientId, int count = 5);

        /// <summary>
        /// 加载验方列表
        /// </summary>
        Task<List<FormulaDto>> LoadFormulasAsync();

        /// <summary>
        /// 导入验方到当前处方
        /// </summary>
        Task<PrescriptionDto> ImportFormulaAsync(Guid formulaId);

        /// <summary>
        /// 构建处方草稿（用于预览）
        /// </summary>
        Task<PrescriptionDto> BuildPrescriptionDraftAsync(
            Guid medicalCaseId,
            List<PrescriptionItemDto> items);

        /// <summary>
        /// 验证处方有效性
        /// </summary>
        bool ValidatePrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 计算处方总金额
        /// </summary>
        decimal CalculateTotalAmountAsync(List<PrescriptionItemDto> items);

        /// <summary>
        /// 处方变更事件（用于ViewModel订阅）
        /// </summary>
        event EventHandler<PrescriptionChangedEventArgs> PrescriptionChanged;
    }
}
```

### 6. 自定义HTTP消息处理器(AuthenticationHandler)
```csharp
using System.Net.Http.Headers;

namespace LYBT.Desktop.Foundation.Http
{
    /// <summary>
    /// 认证处理器：自动为所有API请求添加JWT Token
    /// </summary>
    public class AuthenticationHandler : DelegatingHandler
    {
        private readonly ITokenService _tokenService;

        public AuthenticationHandler(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 获取当前Token
            var token = await _tokenService.GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                // 添加Authorization Header
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            // 发送请求
            var response = await base.SendAsync(request, cancellationToken);

            // 处理401未授权响应
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // 清除无效Token，触发重新登录
                await _tokenService.ClearTokenAsync();
            }

            return response;
        }
    }
}
```

### 7. ViewModel层间接调用(通过Repository)
```csharp
using LYBT.Desktop.Models.Repositories;
using Prism.Commands;

namespace LYBT.Desktop.Modules.Users.ViewModels
{
    public class UserListViewModel : ViewModelBase
    {
        private readonly IUserRepository _userRepository;

        public UserListViewModel(IUserRepository userRepository)
        {
            _userRepository = userRepository;
            LoadUsersCommand = new DelegateCommand(async () => await LoadUsersAsync());
        }

        public DelegateCommand LoadUsersCommand { get; }

        private async Task LoadUsersAsync()
        {
            IsBusy = true;

            // 通过Repository调用API（Repository内部使用IUserApi）
            var result = await _userRepository.GetUsersAsync(
                page: CurrentPage,
                pageSize: PageSize,
                keyword: SearchKeyword
            );

            if (result.IsSuccess)
            {
                Users = new ObservableCollection<UserDto>(result.Data.Items);
                TotalCount = result.Data.TotalCount;
            }
            else
            {
                // 显示错误消息
                await _dialogService.ShowErrorAsync("错误", result.Message);
            }

            IsBusy = false;
        }
    }
}
```

## 🎯 设计原则

1. **单一职责**：仅定义API契约，不包含业务逻辑或HTTP实现
2. **类型安全**：使用Refit特性确保编译时检查，避免运行时错误
3. **契约驱动**：与Server端WebAPI保持完全一致，通过DTO强类型同步
4. **无状态**：接口方法无状态，依赖Refit自动生成实现
5. **异步优先**：所有API方法使用async/await模式
6. **错误处理**：通过ApiResponse包装响应，统一错误处理
7. **认证集成**：通过HttpMessageHandler统一添加JWT Token

## 🔌 API接口完整列表

此项目定义的API契约通过Refit框架调用 `LYBT.WebAPI` 项目。

**8个模块的API契约**:

| 模块 | 接口 | 方法数 | 主要功能 |
|------|------|--------|---------|
| **Auth** | IAuthApi | 6 | 登录、登出、Token验证、修改密码 |
| **Users** | IUserApi | 5 | 用户CRUD、分页查询 |
| **Patients** | IPatientApi | ~8 | 患者CRUD、搜索、档案管理 |
| **MedicalCase** | IMedicalCaseApi | ~10 | 医案CRUD、状态管理、查询 |
| **Consultation** | IConsultationApi | ~6 | 诊断记录、四诊录入、辨证论治 |
| **Prescription** | IPrescriptionApi | ~8 | 处方CRUD、状态管理、打印 |
| **Herbs** | IHerbApi | ~8 | 药材CRUD、搜索、批量导入 |
| **Formula** | IFormulaApi | ~8 | 验方CRUD、搜索、克隆 |

**跨模块服务契约**:
- **IPrescriptionEditorService**: 处方编辑器服务（9个方法 + 1个事件）

## 🔄 历史变更

### v2.0 (2025-10-12) - 命名空间迁移
- **重大变更**:从 `Shared.Interfaces/Api/` 迁移至 `Desktop.Contracts/Api/`
- **命名空间变更**:`LYBT.Shared.Interfaces.Api` → `LYBT.Desktop.Contracts.Api`
- **定位明确**:从"Shared"（误导）变更为"Desktop专用"（准确）
- **架构优化**:消除"Shared"概念，明确Desktop专用契约
- **相关Issue**:#1204

### v1.0 (初始版本)
- 位于Shared.Interfaces项目中
- 命名为"Shared"但实际仅被Desktop端使用
- 8个模块的Refit API接口定义

## 📚 详细文档

- **完整模块文档**:[docs/reference/core/contracts/](../../../../../docs/reference/core/contracts/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/contracts-design.md](../../../../../docs/explanation/architecture/client/contracts-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/contracts-development.md](../../../../../docs/how-to-guides/client/contracts-development.md) *(待创建)*
- **架构标准**:[DESKTOP_ARCHITECTURE_STANDARD.md](../../DESKTOP_ARCHITECTURE_STANDARD.md)
- **ADR-002**:Desktop移除Service层决策
- **Issue #1204**:从Shared.Interfaces迁移至Desktop.Contracts

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
