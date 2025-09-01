# API接口对接标准 - 基于实际代码分析

## 文档概述

本文档基于**2025-09-01实际代码分析**结果，定义了凌隐宝堂中医诊所系统API接口对接的统一标准和TODO标记替换实施方案。

**发现统计**: 共识别85个TODO标记需要替换为真实API调用，分布在6个核心服务中。

---

## 🎯 API接口对接核心原则

### 1. 统一API客户端管理模式

#### 当前问题分析
```csharp
// ❌ 当前分散的API通信方式 - 发现在多个CoreService中重复出现
// TODO: 将API通信移至公共模块 - 统一API客户端管理
var result = await _consultationApi.StartConsultationAsync(startDto);
var result = await _formulaApi.CreateFormulaAsync(createDto);
var result = await _medicalCaseApi.CreateMedicalCaseAsync(createDto);
```

#### 标准解决方案
```csharp
// ✅ 统一API客户端管理器
public interface IUnifiedApiClientManager
{
    IConsultationApi ConsultationApi { get; }
    IFormulaApi FormulaApi { get; }
    IMedicalCaseApi MedicalCaseApi { get; }
    IPrescriptionApi PrescriptionApi { get; }
    IPatientApi PatientApi { get; }
    IUserApi UserApi { get; }
    IHerbApi HerbApi { get; }
}

// 标准CoreService实现模式
public class ConsultationCoreService
{
    private readonly IUnifiedApiClientManager _apiManager;
    
    public async Task<ServiceResult<T>> MethodAsync<T>()
    {
        try 
        {
            var result = await _apiManager.ConsultationApi.MethodAsync();
            return ServiceResult<T>.Success(result);
        }
        catch (ApiException apiEx)
        {
            return ServiceResult<T>.Failure($"API调用失败: {apiEx.Message}");
        }
        catch (Exception ex)
        {
            return ServiceResult<T>.Failure($"系统异常: {ex.Message}");
        }
    }
}
```

### 2. 统一异常处理标准

#### 标准异常处理模板
```csharp
public static class ApiExceptionHandler
{
    public static ServiceResult<T> HandleApiException<T>(Exception ex, string operation, object? context = null)
    {
        return ex switch
        {
            ApiException apiEx when apiEx.StatusCode == HttpStatusCode.NotFound 
                => ServiceResult<T>.Failure($"{operation}失败：资源不存在"),
            ApiException apiEx when apiEx.StatusCode == HttpStatusCode.BadRequest 
                => ServiceResult<T>.Failure($"{operation}失败：请求参数错误 - {apiEx.Content}"),
            ApiException apiEx when apiEx.StatusCode == HttpStatusCode.Unauthorized 
                => ServiceResult<T>.Failure($"{operation}失败：认证失败，请重新登录"),
            ApiException apiEx when apiEx.StatusCode == HttpStatusCode.Forbidden 
                => ServiceResult<T>.Failure($"{operation}失败：权限不足"),
            HttpRequestException httpEx 
                => ServiceResult<T>.Failure($"{operation}失败：网络连接错误 - {httpEx.Message}"),
            TaskCanceledException timeoutEx 
                => ServiceResult<T>.Failure($"{operation}失败：请求超时"),
            _ => ServiceResult<T>.Failure($"{operation}失败：系统异常 - {ex.Message}")
        };
    }
}
```

---

## 📊 TODO标记分析统计

### 按服务模块分布

| 服务模块 | TODO数量 | 主要类型 | 优先级 |
|---------|---------|---------|--------|
| **MedicalCaseCoreService** | 20个 | API通信、业务逻辑、基础设施 | 🔴 高 |
| **FormulaCoreService** | 15个 | API通信、权限检查、配伍逻辑 | 🔴 高 |
| **PrescriptionsCoreService** | 13个 | API通信、模块间通信 | 🔴 高 |
| **ConsultationCoreService** | 8个 | API通信统一化 | 🟡 中 |
| **AuthCoreService** | 4个 | Token管理、验证逻辑 | 🟡 中 |
| **PatientCoreService** | 1个 | 状态切换API | 🟢 低 |

### 按功能类型分布

#### 1. API通信标准化 (42个TODO)
**位置分布**:
- ConsultationCoreService: 8个
- FormulaCoreService: 13个  
- MedicalCaseCoreService: 8个
- PrescriptionsCoreService: 13个

**标准模式**:
```csharp
// ❌ 当前分散状态
// TODO: 将API通信移至公共模块 - 统一API客户端管理

// ✅ 目标统一状态
private readonly IUnifiedApiClientManager _apiManager;
var result = await _apiManager.ConsultationApi.StartConsultationAsync(startDto);
```

#### 2. 业务逻辑实现 (25个TODO)
**核心待实现功能**:
- **配伍禁忌检查** (FormulaCoreService): 中药配伍安全检查
- **权限检查逻辑** (FormulaCoreService): 用户操作权限验证  
- **患者医生关联验证** (MedicalCaseCoreService): 诊疗权限检查
- **缓存统计逻辑** (MedicalCaseCoreService): 性能优化缓存
- **数据格式化逻辑** (MedicalCaseCoreService): 数据标准化处理

#### 3. 基础设施完善 (18个TODO)
**系统级功能**:
- **操作日志记录** (MedicalCaseCoreService): 审计日志
- **事件通知机制** (MedicalCaseCoreService): 系统通知
- **健康检查逻辑** (多个服务): 系统监控
- **系统配置获取** (多个服务): 配置管理
- **Token刷新机制** (AuthCoreService): 身份认证

---

## 🚀 TODO替换实施计划

### Phase 1: API通信统一化 (42个TODO) - 2周 ✅ **基础设施已完成 [2025-09-01]**

#### 1.1 创建统一API客户端管理器 ✅ **[已完成 2025-09-01]**
```csharp
// ✅ 已实现: src/Client/Desktop/Infrastructure/Api/IUnifiedApiClientManager.cs
public interface IUnifiedApiClientManager
{
    IConsultationApi ConsultationApi { get; }
    IFormulaApi FormulaApi { get; }
    IMedicalCaseApi MedicalCaseApi { get; }
    IPrescriptionApi PrescriptionApi { get; }
    IPatientApi PatientApi { get; }
    IUserApi UserApi { get; }
    IHerbApi HerbApi { get; }
    IAuthApi AuthApi { get; }
}

// ✅ 已实现: UnifiedApiClientManager完整实现
// ✅ 已集成: Prism DI容器注册完成
// ✅ 编译质量: Infrastructure项目0错误0警告
```

**✅ 完成状态**:
- ✅ 接口定义完成 - `IUnifiedApiClientManager`
- ✅ 实现类完成 - `UnifiedApiClientManager`
- ✅ DI注册完成 - Prism容器集成
- ✅ API客户端初始化 - Refit类型安全REST客户端
- ✅ 编译验证通过 - 0错误0警告
- 🔄 **待完成**: DTO缺失问题修复后即可开始API调用替换

#### 1.2 替换ConsultationCoreService (8个TODO)
**待替换方法**:
- `StartConsultationAsync` - 开始看诊
- `UpdateConsultationAsync` - 更新诊断
- `DeleteConsultationAsync` - 删除看诊记录
- `GetConsultationByIdAsync` - 获取看诊详情
- `GetConsultationListAsync` - 分页查询
- `CompleteConsultationAsync` - 完成看诊
- `CancelConsultationAsync` - 取消看诊
- `GetStatisticsAsync` - 获取统计

#### 1.3 替换FormulaCoreService (13个TODO)
**待替换方法**:
- `CreateFormulaAsync` - 创建验方
- `UpdateFormulaAsync` - 更新验方
- `DeleteFormulaAsync` - 删除验方
- `GetFormulaByIdAsync` - 获取验方详情
- `GetFormulasAsync` - 获取验方列表
- `GetPagedFormulasAsync` - 分页查询验方
- `SearchFormulasAsync` - 搜索验方
- `UpdateFormulaStatusAsync` - 更新状态
- `BatchOperateFormulasAsync` - 批量操作
- `GetFormulaStatisticsAsync` - 获取统计
- `ImportFormulasAsync` - 导入验方
- `ExportFormulasAsync` - 导出验方

### Phase 2: 核心业务逻辑实现 (25个TODO) - 3周

#### 2.1 配伍禁忌检查系统 (FormulaCoreService)
```csharp
// TODO: 实现配伍禁忌检查逻辑
public async Task<ServiceResult<ContraindicationCheckResult>> CheckContraindicationsAsync(List<HerbDto> herbs)
{
    // 1. 调用配伍禁忌API
    var apiResult = await _apiManager.HerbApi.CheckContraindicationsAsync(herbs);
    
    // 2. 分析配伍关系
    var conflicts = AnalyzeHerbConflicts(apiResult.Data);
    
    // 3. 返回检查结果
    return ServiceResult<ContraindicationCheckResult>.Success(conflicts);
}
```

#### 2.2 权限检查系统 (FormulaCoreService)
```csharp
// TODO: 实现具体的权限检查逻辑
public async Task<ServiceResult<bool>> CheckPermissionAsync(Guid userId, string operation, object? resource = null)
{
    var permissionResult = await _apiManager.UserApi.CheckUserPermissionAsync(userId, operation);
    return permissionResult;
}
```

#### 2.3 患者医生关联验证 (MedicalCaseCoreService)
```csharp
// TODO: 实现患者和医生关联验证逻辑
public async Task<ServiceResult<bool>> ValidatePatientDoctorRelationAsync(Guid patientId, Guid doctorId)
{
    // 检查医生是否有权限诊治该患者
    var result = await _apiManager.UserApi.CheckDoctorPatientPermissionAsync(doctorId, patientId);
    return result;
}
```

### Phase 3: 基础设施完善 (18个TODO) - 2周

#### 3.1 操作日志记录系统
```csharp
// TODO: 实现操作日志记录到数据库或外部系统
public async Task LogOperationAsync(string operation, object data, Guid userId)
{
    var logEntry = new OperationLogDto
    {
        Operation = operation,
        Data = JsonSerializer.Serialize(data),
        UserId = userId,
        Timestamp = DateTime.Now
    };
    
    await _apiManager.SystemApi.CreateOperationLogAsync(logEntry);
}
```

#### 3.2 事件通知机制
```csharp
// TODO: 实现事件通知机制，例如SignalR推送、邮件通知等
public async Task SendNotificationAsync(NotificationDto notification)
{
    await _apiManager.SystemApi.SendNotificationAsync(notification);
}
```

#### 3.3 系统健康检查
```csharp
// TODO: 实现健康检查逻辑，检查API连接、缓存状态等
public async Task<ServiceResult<HealthCheckResult>> CheckHealthAsync()
{
    var healthResult = await _apiManager.SystemApi.GetHealthStatusAsync();
    return healthResult;
}
```

---

## 📝 实施检查清单

### API通信统一化检查清单
- [ ] **创建IUnifiedApiClientManager接口和实现**
- [ ] **更新所有CoreService构造函数注入**
- [ ] **替换ConsultationCoreService的8个TODO**
- [ ] **替换FormulaCoreService的13个TODO**  
- [ ] **替换MedicalCaseCoreService的8个TODO**
- [ ] **替换PrescriptionsCoreService的13个TODO**
- [ ] **更新依赖注入配置**
- [ ] **编写API客户端管理器单元测试**

### 业务逻辑实现检查清单
- [ ] **实现配伍禁忌检查API和逻辑**
- [ ] **实现权限检查API和逻辑**
- [ ] **实现患者医生关联验证**
- [ ] **实现缓存统计逻辑**
- [ ] **实现数据格式化处理**
- [ ] **实现Token刷新机制**
- [ ] **完善用户名密码验证规则**

### 基础设施完善检查清单
- [ ] **实现操作日志记录系统**
- [ ] **实现事件通知机制**
- [ ] **实现系统健康检查**
- [ ] **实现系统配置获取**
- [ ] **实现模块间通信验证**

---

## 🔧 开发工具和脚本

### TODO替换进度检查脚本
```powershell
# scripts/check-todo-progress.ps1
$todoPattern = "// TODO"
$sourceFiles = Get-ChildItem -Path "src/Client/Desktop" -Recurse -Include "*.cs"

$totalTodos = 0
$serviceStats = @{}

foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw
    $todos = [regex]::Matches($content, $todoPattern)
    
    if ($todos.Count -gt 0) {
        $serviceName = $file.Name
        $serviceStats[$serviceName] = $todos.Count
        $totalTodos += $todos.Count
    }
}

Write-Host "=== TODO替换进度统计 ===" -ForegroundColor Green
Write-Host "总计TODO标记: $totalTodos" -ForegroundColor Yellow

foreach ($service in $serviceStats.GetEnumerator() | Sort-Object Value -Descending) {
    Write-Host "$($service.Key): $($service.Value)个" -ForegroundColor White
}
```

### API调用验证脚本
```powershell
# scripts/validate-api-calls.ps1
$mockPatterns = @(
    "return.*模拟",
    "return.*mock", 
    "return.*fake",
    "// TODO.*API",
    "await Task\.CompletedTask"
)

foreach ($pattern in $mockPatterns) {
    Write-Host "=== 检查模式: $pattern ===" -ForegroundColor Yellow
    Select-String -Path "src/Client/Desktop/**/*.cs" -Pattern $pattern
}
```

---

## 📚 相关文档

- [交付标准规范](DELIVERY_STANDARDS.md) - 交付阶段总体要求
- [开发规范](../开发规范.md) - 代码开发标准  
- [UltraThink三层架构](../architecture/ultrathink-three-layer-architecture.md) - 架构设计标准
- [前后端契约规范](../前后端契约规范.md) - 接口契约定义

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**基于代码**: UltraThink三层架构重构完成后实际状态  
**适用范围**: 凌隐宝堂中医诊所系统交付阶段API接口对接