# Desktop层UltraThink重构实施指南

## 一、重构准备清单

### 1.1 环境准备
- [ ] 创建feature分支：`feature/desktop-ultrathink-refactoring`
- [ ] 备份当前代码
- [ ] 确认BaseApiService.cs已创建
- [ ] 准备性能测试基准（启动时间、内存占用）

### 1.2 工具准备
- Visual Studio 2022
- Git版本控制
- 性能分析工具

## 二、模块重构详细步骤

### 2.1 示例：UserService重构

#### Step 1: 创建新的UserService

```csharp
// 文件：src/Client/Desktop/Modules/Users/Services/UserService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Services
{
    /// <summary>
    /// 用户服务 - 基于BaseApiService的精简实现
    /// 替代原有的Module + QueryService + BusinessService三层架构
    /// </summary>
    public class UserService : BaseApiService<IUserApi>, IUserService
    {
        public UserService(
            IUserApi userApi,
            IExceptionHandler exceptionHandler,
            ILogger<UserService> logger)
            : base(userApi, logger, exceptionHandler)
        {
        }

        #region 查询方法（原QueryService）

        public async Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(
            int pageNumber, int pageSize, string sortBy = null)
        {
            return await ExecuteApiCall(
                () => Api.GetPagedAsync(pageNumber, pageSize, sortBy),
                "GetPagedUsers");
        }

        public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                return ServiceResult<UserDto>.Failure("用户ID不能为空");

            return await ExecuteApiCall(
                () => Api.GetByIdAsync(id),
                $"GetUserById:{id}");
        }

        public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ServiceResult<UserDto>.Failure("用户名不能为空");

            return await ExecuteApiCall(
                () => Api.GetByUsernameAsync(username),
                $"GetUserByUsername:{username}");
        }

        public async Task<ServiceResult<List<UserDto>>> GetActiveUsersAsync()
        {
            return await ExecuteApiCall(
                () => Api.GetActiveUsersAsync(),
                "GetActiveUsers");
        }

        public async Task<ServiceResult<List<UserDto>>> SearchAsync(UserSearchDto criteria)
        {
            if (criteria == null)
                return ServiceResult<List<UserDto>>.Failure("搜索条件不能为空");

            return await ExecuteApiCall(
                () => Api.SearchAsync(criteria),
                "SearchUsers");
        }

        public async Task<ServiceResult<List<RoleDto>>> GetRolesAsync()
        {
            return await ExecuteApiCallWithCache(
                "user_roles",
                () => Api.GetRolesAsync(),
                TimeSpan.FromHours(1),
                "GetRoles");
        }

        #endregion

        #region 业务方法（原BusinessService）

        public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<UserDto>.Failure("用户信息不能为空");

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(dto.Username))
                return ServiceResult<UserDto>.Failure("用户名不能为空");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return ServiceResult<UserDto>.Failure("密码不能为空");

            if (dto.Password.Length < 6)
                return ServiceResult<UserDto>.Failure("密码长度至少6位");

            return await ExecuteApiCall(
                () => Api.CreateAsync(dto),
                "CreateUser");
        }

        public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UserUpdateDto dto)
        {
            if (id == Guid.Empty)
                return ServiceResult<UserDto>.Failure("用户ID不能为空");

            if (dto == null)
                return ServiceResult<UserDto>.Failure("更新信息不能为空");

            return await ExecuteApiCall(
                () => Api.UpdateAsync(id, dto),
                $"UpdateUser:{id}");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure("用户ID不能为空");

            return await ExecuteApiCall(
                () => Api.DeleteAsync(id),
                $"DeleteUser:{id}");
        }

        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure("用户ID不能为空");

            return await ExecuteApiCall(
                () => Api.EnableAsync(id),
                $"EnableUser:{id}");
        }

        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure("用户ID不能为空");

            return await ExecuteApiCall(
                () => Api.DisableAsync(id),
                $"DisableUser:{id}");
        }

        public async Task<ServiceResult<bool>> BatchEnableAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return ServiceResult<bool>.Failure("用户ID列表不能为空");

            return await ExecuteApiCall(
                () => Api.BatchEnableAsync(ids),
                "BatchEnableUsers");
        }

        public async Task<ServiceResult<bool>> BatchDisableAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return ServiceResult<bool>.Failure("用户ID列表不能为空");

            return await ExecuteApiCall(
                () => Api.BatchDisableAsync(ids),
                "BatchDisableUsers");
        }

        public async Task<ServiceResult<bool>> ResetPasswordAsync(Guid id, ResetPasswordDto dto)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure("用户ID不能为空");

            if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword))
                return ServiceResult<bool>.Failure("新密码不能为空");

            if (dto.NewPassword.Length < 6)
                return ServiceResult<bool>.Failure("密码长度至少6位");

            return await ExecuteApiCall(
                () => Api.ResetPasswordAsync(id, dto),
                $"ResetPassword:{id}");
        }

        public async Task<ServiceResult<bool>> ChangePasswordAsync(Guid id, ChangePasswordDto dto)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure("用户ID不能为空");

            if (dto == null)
                return ServiceResult<bool>.Failure("密码信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.OldPassword))
                return ServiceResult<bool>.Failure("原密码不能为空");

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return ServiceResult<bool>.Failure("新密码不能为空");

            if (dto.NewPassword.Length < 6)
                return ServiceResult<bool>.Failure("新密码长度至少6位");

            return await ExecuteApiCall(
                () => Api.ChangePasswordAsync(id, dto),
                $"ChangePassword:{id}");
        }

        public async Task<ServiceResult<bool>> ChangeProfileAsync(Guid id, UserProfileDto dto)
        {
            if (id == Guid.Empty)
                return ServiceResult<bool>.Failure("用户ID不能为空");

            if (dto == null)
                return ServiceResult<bool>.Failure("用户资料不能为空");

            return await ExecuteApiCall(
                () => Api.ChangeProfileAsync(id, dto),
                $"ChangeProfile:{id}");
        }

        public async Task<ServiceResult<bool>> ValidateUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ServiceResult<bool>.Failure("用户名不能为空");

            return await ExecuteApiCall(
                () => Api.ValidateUsernameAsync(username),
                $"ValidateUsername:{username}");
        }

        #endregion
    }
}
```

#### Step 2: 更新服务注册

```csharp
// 文件：src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs

// 删除原有的5层注册方法，改为单层注册
private static void RegisterBusinessServices(IContainerRegistry containerRegistry)
{
    // Auth服务 - 使用新的简化版本
    containerRegistry.RegisterScoped<IAuthService,
        LYBT.Desktop.Auth.Services.AuthService>();

    // User服务 - 使用新的简化版本
    containerRegistry.RegisterScoped<IUserService,
        LYBT.Desktop.Users.Services.UserService>();

    // Patient服务
    containerRegistry.RegisterScoped<IPatientService,
        LYBT.Desktop.Patients.Services.PatientService>();

    // Herb服务
    containerRegistry.RegisterScoped<IHerbService,
        LYBT.Desktop.Herbs.Services.HerbService>();

    // Formula服务
    containerRegistry.RegisterScoped<IFormulaService,
        LYBT.Desktop.Formula.Services.FormulaService>();

    // MedicalCase服务
    containerRegistry.RegisterScoped<IMedicalCaseService,
        LYBT.Desktop.MedicalCase.Services.MedicalCaseService>();

    // Consultation服务
    containerRegistry.RegisterScoped<IConsultationService,
        LYBT.Desktop.Consultation.Services.ConsultationService>();

    // Prescription服务
    containerRegistry.RegisterScoped<IPrescriptionService,
        LYBT.Desktop.Prescriptions.Services.PrescriptionService>();
}
```

#### Step 3: 验证ViewModel兼容性

```csharp
// ViewModel无需修改，因为接口保持不变
public class UserListViewModel : BindableBase
{
    private readonly IUserService _userService; // 接口不变

    public UserListViewModel(IUserService userService) // 注入接口不变
    {
        _userService = userService;
    }

    // 使用方式完全不变
    private async Task LoadUsersAsync()
    {
        var result = await _userService.GetPagedAsync(1, 20);
        if (result.IsSuccess)
        {
            Users = result.Data.Items;
        }
    }
}
```

#### Step 4: 删除旧文件

```bash
# 删除旧的三层架构文件
rm src/Client/Desktop/Modules/Users/Services/UserModule.cs
rm src/Client/Desktop/Modules/Users/Services/UserQueryService.cs
rm src/Client/Desktop/Modules/Users/Services/UserBusinessService.cs
rm src/Client/Desktop/Modules/Users/Interfaces/IUserQueryService.cs
rm src/Client/Desktop/Modules/Users/Interfaces/IUserBusinessService.cs
```

## 三、批量重构脚本

### 3.1 Python重构辅助脚本

```python
# refactor_modules.py
import os
import shutil
from pathlib import Path

def refactor_module(module_name: str, base_path: str):
    """重构单个模块"""
    module_path = Path(base_path) / "Modules" / module_name

    # 1. 备份原文件
    backup_path = module_path / "backup"
    backup_path.mkdir(exist_ok=True)

    services_path = module_path / "Services"
    for file in services_path.glob("*.cs"):
        shutil.copy2(file, backup_path / file.name)

    # 2. 生成新的Service文件
    service_template = generate_service_template(module_name)
    new_service_path = services_path / f"{module_name}Service.cs"
    with open(new_service_path, 'w', encoding='utf-8') as f:
        f.write(service_template)

    # 3. 删除旧文件
    old_files = [
        f"{module_name}Module.cs",
        f"{module_name}QueryService.cs",
        f"{module_name}BusinessService.cs"
    ]
    for old_file in old_files:
        file_path = services_path / old_file
        if file_path.exists():
            file_path.unlink()

    print(f"✅ {module_name}模块重构完成")

def generate_service_template(module_name: str) -> str:
    """生成Service模板代码"""
    return f'''using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{module_name};

namespace LYBT.Desktop.{module_name}.Services
{{
    public class {module_name}Service : BaseApiService<I{module_name}Api>, I{module_name}Service
    {{
        public {module_name}Service(
            I{module_name}Api api,
            IExceptionHandler exceptionHandler,
            ILogger<{module_name}Service> logger)
            : base(api, logger, exceptionHandler)
        {{
        }}

        // TODO: 从{module_name}QueryService和{module_name}BusinessService迁移方法
    }}
}}'''

# 执行批量重构
modules = ["Auth", "Users", "Patients", "Herbs", "Formula",
           "MedicalCase", "Consultation", "Prescriptions"]

base_path = r"D:\source\repos\LYBTZYZS\src\Client\Desktop"

for module in modules:
    refactor_module(module, base_path)
```

## 四、测试验证清单

### 4.1 单元测试

```csharp
[TestClass]
public class UserServiceTests
{
    private Mock<IUserApi> _mockApi;
    private Mock<IExceptionHandler> _mockExceptionHandler;
    private Mock<ILogger<UserService>> _mockLogger;
    private UserService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockApi = new Mock<IUserApi>();
        _mockExceptionHandler = new Mock<IExceptionHandler>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _service = new UserService(_mockApi.Object, _mockExceptionHandler.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetPagedAsync_Should_Return_Success()
    {
        // Arrange
        var expectedResult = new ApiResponse<PagedResult<UserDto>>();
        _mockApi.Setup(x => x.GetPagedAsync(1, 20, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _service.GetPagedAsync(1, 20);

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }
}
```

### 4.2 集成测试检查点

- [ ] 登录功能正常
- [ ] 用户列表加载正常
- [ ] 用户创建/编辑/删除正常
- [ ] 患者管理功能正常
- [ ] 病历创建流程正常
- [ ] 处方开具功能正常
- [ ] 草药查询功能正常
- [ ] 方剂模板功能正常

### 4.3 性能测试对比

```csharp
// 性能测试代码
public class PerformanceTest
{
    [TestMethod]
    public void MeasureStartupTime()
    {
        var stopwatch = Stopwatch.StartNew();

        var app = new App();
        app.InitializeComponent();

        stopwatch.Stop();

        Console.WriteLine($"启动时间: {stopwatch.ElapsedMilliseconds}ms");

        // 预期: < 3000ms (原来约5000ms)
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 3000);
    }

    [TestMethod]
    public void MeasureMemoryUsage()
    {
        GC.Collect();
        var beforeMemory = GC.GetTotalMemory(false);

        // 创建所有服务实例
        var services = CreateAllServices();

        var afterMemory = GC.GetTotalMemory(false);
        var memoryUsed = (afterMemory - beforeMemory) / 1024 / 1024; // MB

        Console.WriteLine($"内存占用: {memoryUsed}MB");

        // 预期: < 50MB (原来约80MB)
        Assert.IsTrue(memoryUsed < 50);
    }
}
```

## 五、问题处理指南

### 5.1 常见问题及解决方案

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| ViewModel报错找不到服务 | 接口名称变化 | 保持IService接口名不变 |
| API调用失败 | Api属性访问错误 | 确认BaseApiService的Api属性正确初始化 |
| 编译错误：缺少方法 | 方法未迁移完整 | 对比旧Service确保所有方法都迁移 |
| 性能未改善 | 服务生命周期配置错误 | 使用Scoped而非Singleton |

### 5.2 回滚步骤

```bash
# 如果需要回滚
git stash              # 保存当前修改
git checkout main      # 切回主分支
git branch -D feature/desktop-ultrathink-refactoring  # 删除feature分支

# 恢复备份
cd src/Client/Desktop/Modules/[ModuleName]/Services
cp backup/*.cs ./      # 恢复备份文件
```

## 六、验收标准

### 6.1 功能验收
- [ ] 所有8个模块完成重构
- [ ] 编译0错误0警告
- [ ] 所有现有功能正常工作
- [ ] 单元测试全部通过

### 6.2 性能验收
- [ ] 启动时间 < 3秒（原5秒）
- [ ] 内存占用 < 300MB（原500MB）
- [ ] 代码行数减少60%以上
- [ ] 服务数量从24个减至8个

### 6.3 代码质量验收
- [ ] 所有Service继承BaseApiService
- [ ] 删除所有Module/QueryService/BusinessService文件
- [ ] 服务注册简化为单层
- [ ] 无循环依赖

## 七、后续优化建议

1. **添加缓存层**
```csharp
// 在BaseApiService中添加缓存支持
protected async Task<ServiceResult<T>> ExecuteApiCallWithCache<T>(
    string cacheKey,
    Func<Task<IApiResponse<T>>> apiCall,
    TimeSpan? duration = null)
{
    // 先查缓存
    if (_cache.TryGetValue(cacheKey, out T cached))
        return ServiceResult<T>.Success(cached);

    // 调用API
    var result = await ExecuteApiCall(apiCall);

    // 写入缓存
    if (result.IsSuccess)
        _cache.Set(cacheKey, result.Data, duration ?? TimeSpan.FromMinutes(5));

    return result;
}
```

2. **批量操作优化**
```csharp
// 添加批量操作支持
public async Task<ServiceResult<List<T>>> ExecuteBatchApiCalls<T>(
    IEnumerable<Func<Task<IApiResponse<T>>>> apiCalls)
{
    var tasks = apiCalls.Select(call => ExecuteApiCall(call));
    var results = await Task.WhenAll(tasks);

    // 聚合结果
    return AggregateResults(results);
}
```

3. **离线支持**
```csharp
// 添加离线数据支持
public abstract class OfflineCapableService<TApi> : BaseApiService<TApi>
{
    private readonly IOfflineStorage _offlineStorage;

    protected async Task<ServiceResult<T>> ExecuteWithOfflineFallback<T>(
        Func<Task<IApiResponse<T>>> apiCall,
        string offlineKey)
    {
        try
        {
            return await ExecuteApiCall(apiCall);
        }
        catch (NetworkException)
        {
            // 网络错误时返回离线数据
            var offlineData = await _offlineStorage.GetAsync<T>(offlineKey);
            return ServiceResult<T>.Success(offlineData);
        }
    }
}
```

---
*实施指南版本: 1.0*
*创建日期: 2025-09-23*
*适用项目: LYBT Desktop UltraThink重构*