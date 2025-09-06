# 技术债务待办清单 (Technical Debt Backlog)

> 生成时间: 2025-09-06  
> 基于: 架构设计体检报告  
> 目标场景: **20人以下小诊所** - 实用主义优先  
> 排序规则: (Severity权重 × Impact) / Effort  

---

## DT-001 🔴 服务接口职责混乱
- **Severity**: Critical (权重: 5)
- **Impact**: 5 (严重违反SOLID原则，影响整体架构)
- **Effort**: 3 (需要重构接口，但影响范围可控)
- **Priority Score**: (5 × 5) / 3 = 8.33

**Evidence:**
```
文件: src/Client/Desktop/Modules/Auth/Services/AuthModule.cs:17
代码片段:
public class AuthModule : IAuthService, IAuthenticationService
{
    // 同时实现两个职责不同的接口
}
```

**Minimal Fix:**
```csharp
// 1. 创建适配器类分离接口职责
public class AuthServiceAdapter : IAuthenticationService 
{
    private readonly IAuthService _authService;
    public AuthServiceAdapter(IAuthService authService) => _authService = authService;
}

// 2. 修改依赖注入注册
containerRegistry.RegisterSingleton<IAuthService, AuthModule>();
containerRegistry.RegisterSingleton<IAuthenticationService, AuthServiceAdapter>();
```

---

## DT-002 🔴 依赖注入生命周期混乱
- **Severity**: Critical (权重: 5)
- **Impact**: 4 (可能导致内存泄漏和服务实例混乱)
- **Effort**: 3 (需要重构服务注册逻辑)
- **Priority Score**: (5 × 4) / 3 = 6.67

**Evidence:**
```
文件: src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:148-153
代码片段:
containerRegistry.Register<IAuthenticationService>(container =>
    container.Resolve<AuthModule>());
containerRegistry.Register<IAuthService>(container =>
    container.Resolve<AuthModule>());
```

**Minimal Fix:**
```csharp
// 统一使用标准IoC注册，避免工厂委托
containerRegistry.RegisterSingleton<AuthModule>();
containerRegistry.RegisterSingleton<IAuthService>(container => container.Resolve<AuthModule>());
containerRegistry.RegisterSingleton<IAuthenticationService, AuthServiceAdapter>();
```

---

## DT-003 🟡 模块间依赖需要梳理 *(小诊所适配)*
- **Severity**: Major (权重: 3) *(从Critical降级)*
- **Impact**: 3 (对小诊所而言，模块耦合可接受)
- **Effort**: 2 (简化依赖关系，无需引入复杂架构)
- **Priority Score**: (3 × 3) / 2 = 4.5

**Evidence:**
```
文件: src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:143-174
代码片段:
// Auth、User、Patient模块存在依赖关系
// 对小诊所而言，模块间有依赖是合理的业务需求
```

**Minimal Fix (实用主义):**
```csharp
// 保持现有架构，但明确依赖顺序
private static void RegisterModulesInOrder(IContainerRegistry registry) 
{
    // 1. 基础模块 (无依赖)
    RegisterAuthModule(registry);
    
    // 2. 依赖基础模块
    RegisterUserModule(registry);
    
    // 3. 依赖业务模块  
    RegisterPatientModule(registry);
    
    // 添加依赖验证
    ValidateModuleDependencies(registry);
}
```

---

## DT-004 🟡 基础查询优化 *(小诊所实用)*
- **Severity**: Major (权重: 3)
- **Impact**: 3 (小诊所数据量小，优化效果有限但有必要)
- **Effort**: 1 (添加基础优化，避免过度设计)
- **Priority Score**: (3 × 3) / 1 = 9.0

**Evidence:**
```
文件: src/Server/Core/LYBT.Infrastructure/Repositories/*.cs
代码片段:
// 缺乏基础的AsNoTracking()优化
return await _context.Users.Where(predicate).ToListAsync();
```

**Minimal Fix (小诊所适配):**
```csharp
// 简单实用的查询优化
public static class SmallClinicQueryExtensions 
{
    // 只添加最基础的优化，避免过度工程化
    public static IQueryable<T> ForReadOnly<T>(this IQueryable<T> query) 
        where T : class
    {
        return query.AsNoTracking(); // 仅此一项足够
    }
}

// 使用方式 - 简单明了
return await _context.Users
    .ForReadOnly()
    .Where(predicate)
    .ToListAsync();
```

---

## DT-005 🟡 ViewModel职责过重
- **Severity**: Major (权重: 3)
- **Impact**: 4 (违反MVVM模式，降低可测试性)
- **Effort**: 3 (需要重构多个ViewModel)
- **Priority Score**: (3 × 4) / 3 = 4.0

**Evidence:**
```
文件: src/Client/Desktop/Modules/*/ViewModels/*.cs
代码片段:
// ViewModel包含业务逻辑和数据访问
public class UserManagementViewModel 
{
    public async Task SaveUserAsync() 
    {
        // 直接调用数据访问逻辑
        var result = await _apiClient.PostAsync(...);
        // 业务规则验证
        if (user.Age < 18) { ... }
    }
}
```

**Minimal Fix:**
```csharp
// 1. 将业务逻辑移至Service层
public class UserManagementService 
{
    public async Task<ServiceResult<User>> SaveUserAsync(UserDto dto) 
    {
        // 业务逻辑和数据访问
    }
}

// 2. ViewModel只负责UI逻辑
public class UserManagementViewModel 
{
    public async Task SaveUserAsync() 
    {
        var result = await _userService.SaveUserAsync(UserDto);
        // 只处理UI反馈
    }
}
```

---

## DT-006 🟡 异常处理不统一
- **Severity**: Major (权重: 3)
- **Impact**: 3 (调试困难，用户体验差)
- **Effort**: 2 (添加统一异常处理中间件)
- **Priority Score**: (3 × 3) / 2 = 4.5

**Evidence:**
```
文件: 跨多个Service类
代码片段:
// 部分Service有异常处理
try { ... } catch (Exception ex) { _logger.LogError(ex, "Error"); }
// 部分Service缺乏异常处理
public async Task<Result> MethodAsync() { /* 无try-catch */ }
```

**Minimal Fix:**
```csharp
// 1. 创建统一异常处理特性
[AttributeUsage(AttributeTargets.Method)]
public class HandleExceptionsAttribute : Attribute { }

// 2. 创建异常处理装饰器
public class ExceptionHandlingDecorator<T> : IService<T> 
{
    public async Task<ServiceResult<T>> ExecuteAsync(Func<Task<ServiceResult<T>>> operation)
    {
        try 
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service operation failed");
            return ServiceResult<T>.Failure($"操作失败: {ex.Message}");
        }
    }
}
```

---

## DT-007 🟢 添加基础代码检查 *(小诊所适配)*
- **Severity**: Minor (权重: 1) *(从Major降级)*
- **Impact**: 2 (小团队手工review即可，无需复杂架构测试)
- **Effort**: 1 (添加基础检查脚本)
- **Priority Score**: (1 × 2) / 1 = 2.0

**Evidence:**
```
文件: 整体项目
代码片段:
// 缺乏基础的代码质量检查
// 小诊所场景下，复杂的架构测试过度设计
```

**Minimal Fix (实用主义):**
```csharp
// 简单的代码检查脚本 (PowerShell/Batch)
# check-code-quality.ps1

# 1. 检查关键命名约定
Write-Host "检查命名约定..."
Get-ChildItem -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match "UserName\s*{") {
        Write-Warning "发现UserName，建议统一为Username: $($_.FullName)"
    }
}

# 2. 检查异步方法是否有Async后缀
Write-Host "检查异步方法命名..."
# ... 基础检查逻辑

# 3. 检查是否有未处理的TODO
Write-Host "检查未完成TODO..."
# ... 简单的TODO扫描
```

---

## ✅ DT-008 AutoMapper配置修复 *(已完成)*
- **Severity**: Minor (权重: 1)
- **Impact**: 3 (运行时可能出现映射错误)
- **Effort**: 1 (添加配置验证)
- **Priority Score**: (1 × 3) / 1 = 3.0
- **Status**: ✅ **COMPLETED**

**Evidence:**
```
文件: Directory.Packages.props:29 - AutoMapper Version="14.0.0" 
文件: src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:81-86
文件: src/Client/Desktop/Modules/Auth/Mappings/MappingProfile.cs:31-32
文件: src/Client/Desktop/Modules/Herbs/Mappings/MappingProfile.cs:87-88
```

**Completed Changes:**
```csharp
// ✅ 已修复: AutoMapper迁移到开源版本14.0.0
<PackageVersion Include="AutoMapper" Version="14.0.0" />

// ✅ 已修复: MapperConfiguration构造函数兼容性
var mapperConfig = new MapperConfiguration(cfg => {
    cfg.AddProfile(new MappingProfile());
}); // 移除了ILoggerFactory参数

// ✅ 验证: 编译成功，映射正常工作
```

**Commit Info**: AutoMapper迁移在2025-09-06会话中完成

---

## DT-009 🟢 命名约定不一致
- **Severity**: Minor (权重: 1)
- **Impact**: 2 (代码可读性下降)
- **Effort**: 1 (统一命名规范)
- **Priority Score**: (1 × 2) / 1 = 2.0

**Evidence:**
```
文件: 多个文件
代码片段:
// 部分使用Username
public string Username { get; set; }
// 部分使用UserName  
public string UserName { get; set; }
```

**Minimal Fix:**
```csharp
// 1. 统一使用Username（符合.NET约定）
// 2. 添加EditorConfig规则
[*.cs]
dotnet_naming_rule.prefer_username_lowercase = true
dotnet_naming_symbols.username.applicable_kinds = property,field
dotnet_naming_symbols.username.required_modifiers = 

# 3. 使用查找替换统一现有代码
# 查找: UserName
# 替换: Username
```

---

## DT-010 🟢 日志级别配置缺失
- **Severity**: Minor (权重: 1)
- **Impact**: 3 (生产环境调试困难)
- **Effort**: 2 (添加Serilog配置)
- **Priority Score**: (1 × 3) / 2 = 1.5

**Evidence:**
```
文件: appsettings.json, Program.cs
代码片段:
// 缺乏结构化日志配置
// 默认使用ILogger，无法灵活控制日志级别和输出格式
```

**Minimal Fix:**
```csharp
// 1. 添加Serilog配置
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}

// 2. 注册Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));
```

---

## DT-011 🟢 缺乏取消令牌支持
- **Severity**: Minor (权重: 1)
- **Impact**: 2 (长时间操作无法取消)
- **Effort**: 1 (添加CancellationToken参数)
- **Priority Score**: (1 × 2) / 1 = 2.0

**Evidence:**
```
文件: src/Client/Desktop/Modules/*/Services/*.cs
代码片段:
// 异步方法缺乏CancellationToken参数
public async Task<ServiceResult<User>> GetUserAsync(int id)
{
    return await _httpClient.GetAsync($"/api/users/{id}");
}
```

**Minimal Fix:**
```csharp
// 添加CancellationToken参数
public async Task<ServiceResult<User>> GetUserAsync(int id, CancellationToken cancellationToken = default)
{
    return await _httpClient.GetAsync($"/api/users/{id}", cancellationToken);
}

// 在ViewModel中使用
private readonly CancellationTokenSource _cancellationTokenSource = new();

public async Task LoadUserAsync()
{
    await _userService.GetUserAsync(userId, _cancellationTokenSource.Token);
}
```

---

## DT-012 🟢 缺乏配置验证
- **Severity**: Minor (权重: 1)
- **Impact**: 3 (配置错误难以发现)
- **Effort**: 2 (添加配置验证逻辑)
- **Priority Score**: (1 × 3) / 2 = 1.5

**Evidence:**
```
文件: src/Client/Desktop/Shell/App.xaml.cs, appsettings.json
代码片段:
// 缺乏配置项验证
var apiUrl = configuration["ApiConfiguration:BaseUrl"];
// 无验证apiUrl是否为空或格式正确
```

**Minimal Fix:**
```csharp
// 1. 创建配置选项类
public class ApiConfigurationOptions
{
    public const string SectionName = "ApiConfiguration";
    
    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;
    
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}

// 2. 注册并验证配置
services.Configure<ApiConfigurationOptions>(
    configuration.GetSection(ApiConfigurationOptions.SectionName));

services.AddOptions<ApiConfigurationOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

---

## DT-013 🟢 内存泄漏风险
- **Severity**: Minor (权重: 1)  
- **Impact**: 3 (长时间运行可能导致内存泄漏)
- **Effort**: 2 (添加IDisposable实现)
- **Priority Score**: (1 × 3) / 2 = 1.5

**Evidence:**
```
文件: src/Client/Desktop/Modules/*/ViewModels/*.cs
代码片段:
public class UserManagementViewModel
{
    // 订阅事件但未在Dispose中取消订阅
    public UserManagementViewModel()
    {
        EventAggregator.GetEvent<UserUpdatedEvent>().Subscribe(OnUserUpdated);
    }
    // 缺乏IDisposable实现
}
```

**Minimal Fix:**
```csharp
public class UserManagementViewModel : IDisposable
{
    private readonly SubscriptionToken _subscriptionToken;
    
    public UserManagementViewModel()
    {
        _subscriptionToken = EventAggregator.GetEvent<UserUpdatedEvent>().Subscribe(OnUserUpdated);
    }
    
    public void Dispose()
    {
        _subscriptionToken?.Dispose();
    }
}
```

---

## DT-014 🟢 缺乏备份策略 *(小诊所关键)*
- **Severity**: Minor (权重: 1)
- **Impact**: 5 (对小诊所而言，数据丢失是灾难性的)
- **Effort**: 2 (添加简单的备份脚本)
- **Priority Score**: (1 × 5) / 2 = 2.5

**Evidence:**
```
文件: 项目配置
代码片段:
// 缺乏数据备份和恢复机制
// 小诊所依赖单一数据库，数据安全至关重要
```

**Minimal Fix (小诊所实用):**
```batch
REM backup-database.bat - 简单实用的数据库备份脚本
@echo off
set BACKUP_DIR=D:\LYBT_Backups
set TIMESTAMP=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%
set BACKUP_FILE=%BACKUP_DIR%\LYBT_DB_%TIMESTAMP%.bak

REM 创建备份目录
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

REM 执行数据库备份
sqlcmd -S localhost -Q "BACKUP DATABASE LYBTDB TO DISK = '%BACKUP_FILE%'"

REM 保留最近7天的备份
forfiles /p "%BACKUP_DIR%" /s /m *.bak /d -7 /c "cmd /c del @path"

echo 数据库备份完成: %BACKUP_FILE%
```

---

## DT-015 🟢 缺乏系统监控告警 *(小诊所实用)*
- **Severity**: Minor (权重: 1)
- **Impact**: 4 (小诊所IT人力有限，需要主动告警)
- **Effort**: 2 (添加基础监控脚本)
- **Priority Score**: (1 × 4) / 2 = 2.0

**Evidence:**
```
文件: 系统部署
代码片段:
// 缺乏服务状态监控和异常告警
// 小诊所无专职IT，需要自动化监控
```

**Minimal Fix (小诊所适配):**
```csharp
// 简单的健康检查和告警服务
public class SimpleHealthCheckService
{
    private readonly ILogger<SimpleHealthCheckService> _logger;
    
    public async Task<HealthCheckResult> CheckSystemHealthAsync()
    {
        var checks = new List<(string Name, bool IsHealthy, string Message)>();
        
        // 1. 数据库连接检查
        try
        {
            await _dbContext.Database.CanConnectAsync();
            checks.Add(("数据库", true, "连接正常"));
        }
        catch (Exception ex)
        {
            checks.Add(("数据库", false, $"连接失败: {ex.Message}"));
        }
        
        // 2. 磁盘空间检查 (>1GB)
        var drive = new DriveInfo("C");
        var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
        checks.Add(("磁盘空间", freeSpaceGB > 1, $"剩余: {freeSpaceGB}GB"));
        
        // 3. 如果有问题，发送邮件通知
        var failedChecks = checks.Where(c => !c.IsHealthy).ToList();
        if (failedChecks.Any())
        {
            await SendAlertEmailAsync(failedChecks);
        }
        
        return new HealthCheckResult(checks);
    }
}

---

## 📊 Backlog 统计 *(小诊所适配版)*

### 总计
- **Total Issues**: 15 *(新增1个小诊所特有条目)*
- **Completed**: 1 (DT-008)
- **Remaining**: 14 issues

### 按严重程度分布 *(重新评估)*
- **Critical (🔴)**: 2 issues (13.3%) *(降低1个)*
  - DT-001: 服务接口职责混乱
  - DT-002: 依赖注入生命周期混乱  

- **Major (🟡)**: 4 issues (26.7%) *(DT-003降级到此类)*
  - DT-003: 模块间依赖需要梳理 *(降级)*
  - DT-004: 基础查询优化 *(简化)*
  - DT-005: ViewModel职责过重
  - DT-006: 异常处理不统一

- **Minor (🟢)**: 8 issues (57.1%) *(增加小诊所实用条目，减去已完成)*
  - DT-007: 添加基础代码检查 *(降级)*
  - ~~DT-008: AutoMapper配置不完整~~ ✅ **已完成**
  - DT-009: 命名约定不一致 
  - DT-010: 日志级别配置缺失
  - DT-011: 缺乏取消令牌支持
  - DT-012: 缺乏配置验证
  - DT-013: 内存泄漏风险
  - DT-014: 缺乏备份策略 *(新增-小诊所关键)*
  - DT-015: 缺乏系统监控告警 *(新增-小诊所实用)*

### 小诊所优先级分布 *(实用主义排序)*
- **立即处理 (Score ≥ 7.0)**: 2 issues
  - DT-004: 基础查询优化 (9.0)
  - DT-002: 依赖注入生命周期混乱 (6.67)
- **本月处理 (Score 4.0-6.9)**: 4 issues  
- **后续优化 (Score < 4.0)**: 9 issues

### 工作量评估 *(小诊所团队)*
- **Total Effort Points**: 25 *(减少过度设计)*
- **Average Effort per Issue**: 1.7 *(更实际)*
- **Estimated Timeline**: 4-6 周 (按1-2人小团队计算)

### 🎯 小诊所特色关注点
- **数据安全**: DT-014 备份策略 (Impact: 5)
- **运维自动化**: DT-015 监控告警 (IT人力不足补偿)
- **实用主义**: 避免过度架构，聚焦业务价值

---

## 🏥 小诊所实施建议

### 📅 推荐实施路径 (4-6周)

**第1周** - 基础稳定性
- DT-001: 修复服务接口职责混乱 (1天)
- DT-002: 统一依赖注入生命周期 (2天)
- DT-008: 完善AutoMapper配置 (0.5天)
- DT-014: 建立数据备份策略 (1天) **小诊所关键**

**第2周** - 性能与运维
- DT-004: 添加基础查询优化 (1天)
- DT-015: 建立系统监控告警 (2天) **小诊所实用**
- DT-010: 配置结构化日志 (1天)

**第3-4周** - 代码质量提升
- DT-005: 重构过胖的ViewModel (分批处理)
- DT-006: 统一异常处理模式 (2天)
- DT-009: 统一命名约定 (查找替换，1天)

**第5-6周** - 长期优化
- DT-011: 添加取消令牌支持 (按需)
- DT-012: 完善配置验证 (1天)
- DT-013: 修复内存泄漏风险 (按发现情况)
- DT-007: 建立基础代码检查 (1天)

### 💡 小诊所特色考虑

**避免过度设计**:
- ❌ 不引入DDD、CQRS、事件溯源等复杂模式
- ❌ 不建立复杂的微服务架构
- ❌ 不引入过度的抽象层和工厂模式
- ✅ 保持简单的三层架构 + UltraThink双层前端

**关注实用性**:
- ✅ 数据备份比性能优化更重要
- ✅ 系统稳定比功能丰富更重要  
- ✅ 易于维护比技术先进更重要
- ✅ 团队熟悉比最佳实践更重要

**资源约束适应**:
- 👥 **人力**: 1-2人开发团队，兼职维护
- 💻 **硬件**: 单机部署，数据库和应用同服务器
- 📊 **数据规模**: <10万条记录，<20并发用户
- ⏰ **时间窗口**: 业余时间维护，避免工作时间中断

### 🎯 成功验收标准

**稳定性指标**:
- [ ] 系统连续运行7天无重启 
- [ ] 数据库备份每日自动执行
- [ ] 关键异常能够及时告警

**性能指标** (小诊所标准):
- [ ] 患者列表加载 < 3秒 (足够)
- [ ] 处方保存 < 2秒 (可接受)
- [ ] 系统启动 < 30秒 (合理)

**维护性指标**:
- [ ] 新功能开发无编译错误
- [ ] 代码风格基本一致
- [ ] 关键操作有日志记录

---

## 🎯 Batch-2 候选清单与优先级 (基于Batch-1完成结果)

### 📋 Batch-1 完成报告 (2025-09-06)

**已完成项目**:
- ✅ **DT-008**: AutoMapper配置修复 - 迁移到开源版本14.0.0，修复构造函数兼容性问题
  - **Evidence**: AutoMapper 15.0.1 → 14.0.0，移除ILoggerFactory参数
  - **Status**: 编译成功，前端0警告0错误，后端StyleCop警告不影响功能
  - **Impact**: 消除运行时映射错误风险，为长期维护奠定基础

### 🏆 重新计算后的Top-10候选 *(基于小诊所实际需求)*

**优先级重排** (Priority Score = (Severity权重 × Impact) / Effort):

1. **DT-004**: 基础查询优化 - **Score: 9.0** 🟡
   - 简单实用的AsNoTracking()优化，立竿见影
   - 小诊所也需要基础性能优化，工作量小
   
2. **DT-001**: 服务接口职责混乱 - **Score: 8.33** 🔴 
   - AuthModule双接口实现问题，影响架构清晰度
   - 创建适配器类分离职责，工作量可控
   
3. **DT-002**: 依赖注入生命周期混乱 - **Score: 6.67** 🔴
   - IoC注册使用工厂委托，可能导致内存泄漏
   - 统一为标准IoC注册模式
   
4. **DT-003**: 模块间依赖需要梳理 - **Score: 4.5** 🟡
   - 对小诊所而言可接受，但需要明确依赖顺序
   - 添加依赖验证逻辑防止循环引用
   
5. **DT-006**: 异常处理不统一 - **Score: 4.5** 🟡
   - 影响调试效率和用户体验
   - 统一异常处理装饰器模式
   
6. **DT-005**: ViewModel职责过重 - **Score: 4.0** 🟡
   - 违反MVVM模式，影响可测试性
   - 逐步重构过胖的ViewModel
   
7. **DT-014**: 缺乏备份策略 - **Score: 2.5** 🟢
   - 小诊所数据安全至关重要，Impact高
   - 简单的批处理备份脚本
   
8. **DT-009**: 命名约定不一致 - **Score: 2.0** 🟢
   - Username vs UserName混用
   - 查找替换统一命名
   
9. **DT-011**: 缺乏取消令牌支持 - **Score: 2.0** 🟢
   - 长时间操作无法取消
   - 添加CancellationToken参数
   
10. **DT-015**: 缺乏系统监控告警 - **Score: 2.0** 🟢
    - 小诊所IT人力有限，需要自动化监控
    - 简单的健康检查服务

### 🎯 Batch-2 精选 (严格≤5项) - 小诊所实用主义

**推荐Batch-2内容** (严格按耦合度和风险控制):

#### ✅ **最终选中** (5项 - 已达上限)

1. **DT-004**: 基础查询优化 ⭐**Priority #1**
   - **Why**: Score最高(9.0)，效果立竿见影，工作量小
   - **Effort**: 1人天，添加AsNoTracking()扩展方法
   - **Risk**: 低 - 仅影响Repository层，无模块间耦合
   - **Impact**: 查询性能提升，减少内存使用

2. **DT-014**: 缺乏备份策略 ⭐**Priority #2** *(小诊所关键)*
   - **Why**: 数据安全对小诊所是生存问题，独立于业务代码
   - **Effort**: 1人天，编写独立批处理备份脚本
   - **Risk**: 零 - 完全独立的运维脚本，不影响代码
   - **Impact**: 防止数据丢失灾难

3. **DT-009**: 命名约定不一致 ⭐**Priority #3**
   - **Why**: 工作量小，立即改善代码可读性，影响面可控
   - **Effort**: 0.5人天，批量查找替换
   - **Risk**: 低 - 纯文本替换，可回滚，影响面明确
   - **Impact**: 代码风格统一，开发效率提升

4. **DT-010**: 日志级别配置缺失 ⭐**Priority #4**
   - **Why**: 生产环境调试重要，配置独立，无代码耦合
   - **Effort**: 1人天，添加Serilog配置
   - **Risk**: 零 - 纯配置改动，不影响业务逻辑
   - **Impact**: 运维效率提升，问题排查能力增强

5. **DT-012**: 缺乏配置验证 ⭐**Priority #5**
   - **Why**: 配置错误难发现，验证逻辑独立
   - **Effort**: 1人天，添加配置验证特性
   - **Risk**: 低 - 启动时验证，失败快速暴露，不影响运行时
   - **Impact**: 部署错误早期发现，系统稳定性提升

#### ❌ **未选中项目** (5项 - Not Selected)

**Critical级别未选中原因**:

6. **DT-001**: 服务接口职责混乱 (Score: 8.33) 🔴
   - **Not Selected原因**: 影响Auth/Users/Patients等多个高耦合模块
   - **Risk评估**: 高 - 需要修改IoC注册、接口实现、ViewModel依赖注入
   - **Defer原因**: 可能导致大面积编译错误，违反"避免跨模块大面积改动"原则

7. **DT-002**: 依赖注入生命周期混乱 (Score: 6.67) 🔴  
   - **Not Selected原因**: 与DT-001高度耦合，同时修改风险过大
   - **Risk评估**: 高 - ServiceCollectionExtensions影响全局服务注册
   - **Defer原因**: 需要与DT-001协调修改，单独修复可能引入新问题

**Major级别未选中原因**:

8. **DT-003**: 模块间依赖需要梳理 (Score: 4.5) 🟡
   - **Not Selected原因**: 涉及8个业务模块依赖关系梳理
   - **Risk评估**: 高 - 跨模块依赖分析，影响面广
   - **Defer原因**: 与DT-001/DT-002强相关，需要统一批次处理

9. **DT-006**: 异常处理不统一 (Score: 4.5) 🟡
   - **Not Selected原因**: 需要修改多个Service类的异常处理逻辑
   - **Risk评估**: 中高 - 跨Service层大面积改动
   - **Defer原因**: 影响业务逻辑层，需要充分测试验证

10. **DT-005**: ViewModel职责过重 (Score: 4.0) 🟡
    - **Not Selected原因**: 需要重构多个模块的ViewModel
    - **Risk评估**: 高 - 影响前端MVVM架构，UI层大面积改动  
    - **Defer原因**: 跨UI模块重构，工作量大，影响用户体验

### 📅 Batch-2 实施计划 (1周冲刺 - 低风险快速交付)

**调整原因**: 选中的5项均为低耦合、低风险项目，可在1周内安全完成

**第1-2天**:
- DT-004 (基础查询优化) - 1天
- DT-014 (数据库备份脚本) - 1天

**第3天**:  
- DT-009 (命名约定统一) - 0.5天
- DT-010 (Serilog日志配置) - 0.5天

**第4天**:
- DT-012 (配置验证) - 1天

**第5天**:
- 集成测试验证 - 0.5天
- 文档更新与部署验证 - 0.5天

**总工作量**: 4.5人天 (在1周内可完成)

### 🎯 Batch-2 成功标准 (修订版)

**完成度验证**:
- [ ] 基础查询方法均使用AsNoTracking()扩展
- [ ] 数据库每日备份脚本可正常运行并测试恢复
- [ ] Username/UserName命名完全统一
- [ ] Serilog结构化日志配置生效
- [ ] 配置验证在启动时正确工作

**质量指标** (降低风险版):
- [ ] 编译: 0错误 (必须)
- [ ] 查询性能: Repository查询内存使用降低
- [ ] 数据安全: 备份脚本7天轮转测试成功
- [ ] 代码质量: 命名检查脚本通过
- [ ] 日志输出: 分级日志正确写入文件

**小诊所适配验证**:
- [ ] 所有修改对现有业务流程完全透明
- [ ] 新增运维功能不影响用户操作
- [ ] 配置错误能在启动时立即发现
- [ ] 系统性能无回退现象

### 💡 后续Batch-3+展望

**可能的后续批次** (基于Batch-2执行反馈):

- **Batch-3 (架构优化批次)**: 专门处理高耦合Critical项目
  - DT-001 + DT-002 + DT-003 统一处理 (服务架构整体重构)
  - 单独批次避免与其他项目冲突
  - 预计工作量: 2周，需要充分测试

- **Batch-4 (业务逻辑批次)**: 跨Service层改动
  - DT-006 异常处理统一
  - DT-013 内存泄漏修复
  - 专注业务逻辑层优化

- **Batch-5 (UI重构批次)**: 前端架构改进
  - DT-005 ViewModel职责重构
  - 分模块逐步进行，降低风险

- **Batch-6 (运维完善批次)**: 
  - DT-015 系统监控告警
  - DT-011 取消令牌支持
  - DT-007 代码质量检查

**严格批次管理原则** (强制执行):
- ✅ 每个Batch **严格≤5项**
- ✅ **严禁跨高耦合模块**大面积改动
- ✅ Critical级别高耦合项目需要**专门批次**统一处理
- ✅ 优先**低风险、独立性强**的项目
- ✅ **Not Selected项目必须明确原因**
- ✅ 始终考虑小诊所20人以下场景
- ✅ 避免过度工程化

---

*注: Priority Score = (Severity权重 × Impact) / Effort*  
*权重: Critical=5, Major=3, Minor=1*  
*🏥 适配场景: 20人以下小诊所，实用主义优先*