# TODO标记替换实施计划

## 文档概述

本文档提供**85个TODO标记**的系统化替换实施方案，基于2025-09-01实际代码分析结果制定。

**实施原则**: 遵循文档驱动开发，先完善API接口文档，再实施代码替换。

---

## 🎯 替换实施总体策略

### 替换优先级矩阵

| 优先级 | 类型 | 数量 | 实施周期 | 风险等级 |
|-------|------|------|---------|---------|
| 🔴 **P0-关键** | API通信统一化 | 42个 | 2周 | 高 |
| 🟡 **P1-重要** | 核心业务逻辑 | 25个 | 3周 | 中 |
| 🟢 **P2-一般** | 基础设施完善 | 18个 | 2周 | 低 |

### 实施阶段规划

#### 第一阶段：API通信统一化 (2周) ✅ **基础设施已完成 [2025-09-01]**
- **目标**: 建立统一API客户端管理器，替换42个API通信TODO
- **交付物**: 统一API客户端管理器 + 所有CoreService API调用标准化
- **成功标准**: 所有"// TODO: 将API通信移至公共模块"标记全部清除

**✅ 已完成核心基础设施 (2025-09-01)**:
- ✅ `IUnifiedApiClientManager`接口实现 - 8个业务模块API统一管理
- ✅ `UnifiedApiClientManager`实现完成 - Refit类型安全REST客户端
- ✅ Prism DI集成完成 - 替代原有8个独立API客户端注册
- ✅ 编译质量达标 - Infrastructure项目0错误0警告
- 🔄 **下一步**: 修复模块DTO缺失问题，完成API调用替换

#### 第二阶段：核心业务逻辑实现 (3周)  
- **目标**: 实现核心业务功能，替换25个业务逻辑TODO
- **交付物**: 配伍禁忌检查、权限验证、数据验证等业务功能
- **成功标准**: 所有业务功能TODO实现并通过单元测试

#### 第三阶段：基础设施完善 (2周)
- **目标**: 完善系统基础设施，替换18个基础设施TODO  
- **交付物**: 日志记录、通知机制、健康检查等系统功能
- **成功标准**: 系统监控和运维功能完整

---

## 📊 详细替换清单

### AuthCoreService - 4个TODO

#### TODO-AUTH-001: Token刷新机制
**位置**: `AuthCoreService.cs:92`
```csharp
// TODO: 实现Token刷新API调用
// 这里需要根据后端API实现Token刷新机制
```

**实施方案**:
```csharp
public async Task<ServiceResult<TokenRefreshResult>> RefreshTokenAsync(string refreshToken)
{
    try
    {
        var refreshRequest = new TokenRefreshRequest { RefreshToken = refreshToken };
        var result = await _apiManager.AuthApi.RefreshTokenAsync(refreshRequest);
        
        // 更新本地Token存储
        await _tokenManager.UpdateTokenAsync(result.AccessToken, result.RefreshToken);
        
        return ServiceResult<TokenRefreshResult>.Success(result);
    }
    catch (ApiException apiEx)
    {
        return ApiExceptionHandler.HandleApiException<TokenRefreshResult>(apiEx, "Token刷新");
    }
}
```

**依赖API**: `POST /api/v1/auth/refresh-token`

#### TODO-AUTH-002: Token格式验证
**位置**: `AuthCoreService.cs:156`
```csharp
// TODO: 实现Token格式验证（JWT格式检查等）
```

**实施方案**:
```csharp
public ServiceResult<bool> ValidateTokenFormat(string token)
{
    try
    {
        // JWT格式基本验证
        var tokenParts = token.Split('.');
        if (tokenParts.Length != 3)
            return ServiceResult<bool>.Failure("Token格式错误：不是有效的JWT格式");
            
        // Base64解码验证
        foreach (var part in tokenParts.Take(2))
        {
            try { Convert.FromBase64String(part + "=="); }
            catch { return ServiceResult<bool>.Failure("Token格式错误：Base64编码无效"); }
        }
        
        return ServiceResult<bool>.Success(true);
    }
    catch (Exception ex)
    {
        return ServiceResult<bool>.Failure($"Token验证失败：{ex.Message}");
    }
}
```

#### TODO-AUTH-003: 用户名格式验证
**位置**: `AuthCoreService.cs:203`
```csharp
// TODO: 添加更多用户名格式验证规则
```

**实施方案**:
```csharp
public ServiceResult ValidateUsername(string username)
{
    if (string.IsNullOrWhiteSpace(username))
        return ServiceResult.Failure("用户名不能为空");
        
    if (username.Length < 3 || username.Length > 20)
        return ServiceResult.Failure("用户名长度必须在3-20个字符之间");
        
    if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        return ServiceResult.Failure("用户名只能包含字母、数字和下划线");
        
    if (char.IsDigit(username[0]))
        return ServiceResult.Failure("用户名不能以数字开头");
        
    return ServiceResult.Success();
}
```

#### TODO-AUTH-004: 密码强度验证
**位置**: `AuthCoreService.cs:220`
```csharp
// TODO: 添加更多密码强度验证规则
```

**实施方案**:
```csharp
public ServiceResult ValidatePasswordStrength(string password)
{
    if (string.IsNullOrWhiteSpace(password))
        return ServiceResult.Failure("密码不能为空");
        
    if (password.Length < 8 || password.Length > 50)
        return ServiceResult.Failure("密码长度必须在8-50个字符之间");
        
    var hasUpper = password.Any(char.IsUpper);
    var hasLower = password.Any(char.IsLower);  
    var hasDigit = password.Any(char.IsDigit);
    var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
    
    int strengthScore = 0;
    if (hasUpper) strengthScore++;
    if (hasLower) strengthScore++;
    if (hasDigit) strengthScore++;
    if (hasSpecial) strengthScore++;
    
    if (strengthScore < 3)
        return ServiceResult.Failure("密码强度不足：必须包含大写字母、小写字母、数字、特殊字符中的至少3种");
        
    return ServiceResult.Success();
}
```

### ConsultationCoreService - 8个TODO

#### 统一API通信模式
**当前状态**: 8个方法都有相同的TODO标记
```csharp
// TODO: 将API通信移至公共模块 - 统一API客户端管理
```

**标准替换模式**:
```csharp
// 原始方法
public async Task<ServiceResult<ConsultationDto>> StartConsultationAsync(ConsultationStartDto startDto)
{
    // TODO: 将API通信移至公共模块 - 统一API客户端管理
    var result = await _consultationApi.StartConsultationAsync(startDto);
    return ServiceResult<ConsultationDto>.Success(result);
}

// 替换后的标准模式
public async Task<ServiceResult<ConsultationDto>> StartConsultationAsync(ConsultationStartDto startDto)
{
    try
    {
        var result = await _apiManager.ConsultationApi.StartConsultationAsync(startDto);
        return ServiceResult<ConsultationDto>.Success(result, "开始看诊成功");
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<ConsultationDto>(ex, "开始看诊", startDto);
    }
}
```

**需要替换的8个方法**:
1. `StartConsultationAsync` - 开始看诊
2. `UpdateConsultationAsync` - 更新诊断  
3. `DeleteConsultationAsync` - 删除看诊记录
4. `GetConsultationByIdAsync` - 获取看诊详情
5. `GetConsultationListAsync` - 分页查询
6. `CompleteConsultationAsync` - 完成看诊
7. `CancelConsultationAsync` - 取消看诊
8. `GetStatisticsAsync` - 获取统计

### FormulaCoreService - 15个TODO

#### API通信统一化 (12个TODO)
**替换模式**: 与ConsultationCoreService相同的统一API客户端管理模式

**方法列表**:
1. `CreateFormulaAsync` - 创建验方
2. `UpdateFormulaAsync` - 更新验方
3. `DeleteFormulaAsync` - 删除验方
4. `GetFormulaByIdAsync` - 获取验方详情
5. `GetFormulasAsync` - 获取验方列表
6. `GetPagedFormulasAsync` - 分页查询
7. `SearchFormulasAsync` - 搜索验方
8. `UpdateFormulaStatusAsync` - 更新状态
9. `BatchOperateFormulasAsync` - 批量操作
10. `GetFormulaStatisticsAsync` - 获取统计
11. `ImportFormulasAsync` - 导入验方
12. `ExportFormulasAsync` - 导出验方

#### 业务逻辑实现 (2个TODO)

**TODO-FORMULA-013: 配伍禁忌检查**
**位置**: `FormulaCoreService.cs:591`
```csharp
// TODO: 实现配伍禁忌检查逻辑
// 这里应该实现具体的中药配伍禁忌检查逻辑
// 目前返回基础验证通过
```

**实施方案**:
```csharp
public async Task<ServiceResult<ContraindicationCheckResult>> CheckFormulaContraindicationsAsync(List<Guid> herbIds)
{
    try
    {
        // 1. 调用配伍禁忌检查API
        var request = new ContraindicationCheckRequest { HerbIds = herbIds };
        var apiResult = await _apiManager.HerbApi.CheckContraindicationsAsync(request);
        
        if (!apiResult.IsSuccess)
            return ServiceResult<ContraindicationCheckResult>.Failure("配伍禁忌检查失败");
            
        // 2. 分析配伍关系
        var result = new ContraindicationCheckResult
        {
            HasConflicts = apiResult.Data.Conflicts.Any(),
            ConflictDetails = apiResult.Data.Conflicts,
            SafetyLevel = CalculateSafetyLevel(apiResult.Data.Conflicts),
            Recommendations = GenerateRecommendations(apiResult.Data.Conflicts)
        };
        
        return ServiceResult<ContraindicationCheckResult>.Success(result);
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<ContraindicationCheckResult>(ex, "配伍禁忌检查", herbIds);
    }
}

private SafetyLevel CalculateSafetyLevel(List<HerbConflict> conflicts)
{
    if (!conflicts.Any()) return SafetyLevel.Safe;
    if (conflicts.Any(c => c.Severity == ConflictSeverity.Critical)) return SafetyLevel.Dangerous;
    if (conflicts.Any(c => c.Severity == ConflictSeverity.Major)) return SafetyLevel.Caution;
    return SafetyLevel.Minor;
}
```

**依赖API**: `POST /api/v1/herbs/check-contraindications`

**TODO-FORMULA-014: 权限检查逻辑**
**位置**: `FormulaCoreService.cs:862`
```csharp
// TODO: 实现具体的权限检查逻辑
// 目前返回基础检查
```

**实施方案**:
```csharp
public async Task<ServiceResult<bool>> CheckFormulaPermissionAsync(Guid userId, FormulaOperation operation, Guid? formulaId = null)
{
    try
    {
        var request = new PermissionCheckRequest
        {
            UserId = userId,
            Resource = "Formula",
            Operation = operation.ToString(),
            ResourceId = formulaId
        };
        
        var result = await _apiManager.UserApi.CheckPermissionAsync(request);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "权限检查", new { userId, operation, formulaId });
    }
}
```

### MedicalCaseCoreService - 20个TODO

#### API通信统一化 (8个TODO)
**替换模式**: 统一API客户端管理模式

**方法列表**:
1. `CreateMedicalCaseAsync` - 创建医案
2. `UpdateMedicalCaseAsync` - 更新医案
3. `DeleteMedicalCaseAsync` - 删除医案
4. `GetMedicalCaseByIdAsync` - 获取医案详情
5. `GetPagedMedicalCasesAsync` - 分页查询
6. `UpdateMedicalCaseStatusAsync` - 更新状态

#### 业务逻辑实现 (12个TODO)

**TODO-MEDICAL-007: 患者医生关联验证**
```csharp
public async Task<ServiceResult<bool>> ValidatePatientDoctorRelationAsync(Guid patientId, Guid doctorId)
{
    try
    {
        var request = new PatientDoctorValidationRequest
        {
            PatientId = patientId,
            DoctorId = doctorId
        };
        
        var result = await _apiManager.UserApi.ValidatePatientDoctorRelationAsync(request);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "患者医生关联验证", new { patientId, doctorId });
    }
}
```

**TODO-MEDICAL-008: 缓存统计逻辑**
```csharp
public async Task<ServiceResult<MedicalCaseCacheStatisticsDto>> GetCacheStatisticsAsync()
{
    try
    {
        var cacheHits = _cache.Get<int>("medical_case_cache_hits") ?? 0;
        var cacheMisses = _cache.Get<int>("medical_case_cache_misses") ?? 0;
        var totalRequests = cacheHits + cacheMisses;
        
        var statistics = new MedicalCaseCacheStatisticsDto
        {
            CacheHits = cacheHits,
            CacheMisses = cacheMisses,
            HitRate = totalRequests > 0 ? (double)cacheHits / totalRequests : 0,
            TotalCachedItems = GetCachedItemsCount(),
            LastClearTime = _cache.Get<DateTime?>("last_clear_time")
        };
        
        return ServiceResult<MedicalCaseCacheStatisticsDto>.Success(statistics);
    }
    catch (Exception ex)
    {
        return ServiceResult<MedicalCaseCacheStatisticsDto>.Failure($"获取缓存统计失败：{ex.Message}");
    }
}
```

### PrescriptionsCoreService - 13个TODO

#### API通信统一化 (8个TODO)
**替换模式**: 统一API客户端管理模式

#### 模块间通信验证 (5个TODO)
**TODO-PRESCRIPTION-009到013**: 患者、医生、药材、医案存在性验证

```csharp
// 统一的模块间验证模式
public async Task<ServiceResult<bool>> ValidatePatientExistsAsync(Guid patientId)
{
    try
    {
        var result = await _apiManager.PatientApi.ExistsAsync(patientId);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "患者存在性验证", patientId);
    }
}

public async Task<ServiceResult<bool>> ValidateDoctorExistsAsync(Guid doctorId)
{
    try
    {
        var result = await _apiManager.UserApi.DoctorExistsAsync(doctorId);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "医生存在性验证", doctorId);
    }
}

public async Task<ServiceResult<bool>> ValidateHerbExistsAsync(Guid herbId)
{
    try
    {
        var result = await _apiManager.HerbApi.ExistsAsync(herbId);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "药材存在性验证", herbId);
    }
}

public async Task<ServiceResult<bool>> ValidateMedicalCaseExistsAsync(Guid medicalCaseId)
{
    try
    {
        var result = await _apiManager.MedicalCaseApi.ExistsAsync(medicalCaseId);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "医案存在性验证", medicalCaseId);
    }
}
```

### PatientCoreService - 1个TODO

#### TODO-PATIENT-001: 患者状态切换API
**位置**: `PatientCoreService.cs:224`
```csharp
// TODO: 实现患者状态切换API调用
// 目前PatientApi可能没有这个方法，需要后端支持
```

**实施方案**:
```csharp
public async Task<ServiceResult<bool>> UpdatePatientStatusAsync(Guid patientId, PatientStatus status)
{
    try
    {
        var request = new PatientStatusUpdateRequest
        {
            PatientId = patientId,
            NewStatus = status,
            UpdateTime = DateTime.Now
        };
        
        var result = await _apiManager.PatientApi.UpdateStatusAsync(request);
        return result;
    }
    catch (Exception ex)
    {
        return ApiExceptionHandler.HandleApiException<bool>(ex, "更新患者状态", new { patientId, status });
    }
}
```

**依赖API**: `PUT /api/v1/patients/{id}/status`

---

## 🛠️ 实施工具和检查脚本

### 1. TODO进度跟踪脚本
```powershell
# scripts/todo-progress-tracker.ps1
param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceFilter = "*"
)

$sourceRoot = "src/Client/Desktop/Modules"
$services = @{
    "AuthCoreService" = @{ Total = 4; Priority = "Medium" }
    "ConsultationCoreService" = @{ Total = 8; Priority = "Medium" }
    "FormulaCoreService" = @{ Total = 15; Priority = "High" }
    "MedicalCaseCoreService" = @{ Total = 20; Priority = "High" }
    "PatientCoreService" = @{ Total = 1; Priority = "Low" }
    "PrescriptionsCoreService" = @{ Total = 13; Priority = "High" }
}

Write-Host "=== TODO替换进度跟踪 ===" -ForegroundColor Green
Write-Host "检查时间: $(Get-Date)" -ForegroundColor Gray

$totalRemaining = 0
foreach ($serviceName in $services.Keys) {
    if ($serviceName -like $ServiceFilter) {
        $filePath = "$sourceRoot/**/Services/$serviceName.cs"
        $todoCount = 0
        
        if (Test-Path $filePath) {
            $content = Get-Content $filePath -Raw -ErrorAction SilentlyContinue
            if ($content) {
                $todoCount = ([regex]::Matches($content, "// TODO")).Count
            }
        }
        
        $service = $services[$serviceName]
        $completed = $service.Total - $todoCount
        $progress = if ($service.Total -gt 0) { [math]::Round(($completed / $service.Total) * 100, 1) } else { 100 }
        
        $priorityColor = switch ($service.Priority) {
            "High" { "Red" }
            "Medium" { "Yellow" }  
            "Low" { "Green" }
        }
        
        Write-Host "$serviceName [$($service.Priority)]:" -ForegroundColor $priorityColor -NoNewline
        Write-Host " $completed/$($service.Total) 完成 ($progress%)" -ForegroundColor White
        
        if ($todoCount -gt 0) {
            Write-Host "  剩余TODO: $todoCount 个" -ForegroundColor Gray
        }
        
        $totalRemaining += $todoCount
    }
}

Write-Host "`n总计剩余TODO: $totalRemaining 个" -ForegroundColor $(if ($totalRemaining -eq 0) { "Green" } else { "Red" })
```

### 2. API依赖检查脚本
```powershell
# scripts/api-dependency-checker.ps1
$apiDependencies = @{
    "AuthCoreService" = @("POST /api/v1/auth/refresh-token")
    "ConsultationCoreService" = @(
        "POST /api/v1/consultations",
        "PUT /api/v1/consultations/{id}",
        "DELETE /api/v1/consultations/{id}",
        "GET /api/v1/consultations/{id}",
        "GET /api/v1/consultations",
        "PUT /api/v1/consultations/{id}/complete",
        "PUT /api/v1/consultations/{id}/cancel",
        "GET /api/v1/consultations/statistics"
    )
    "FormulaCoreService" = @(
        "POST /api/v1/herbs/check-contraindications",
        "POST /api/v1/users/check-permission"
    )
    "PatientCoreService" = @("PUT /api/v1/patients/{id}/status")
}

Write-Host "=== API依赖检查 ===" -ForegroundColor Green

foreach ($service in $apiDependencies.Keys) {
    Write-Host "`n$service 依赖的API:" -ForegroundColor Yellow
    foreach ($api in $apiDependencies[$service]) {
        Write-Host "  - $api" -ForegroundColor White
    }
}

Write-Host "`n请确保以上API端点已在后端实现" -ForegroundColor Red
```

### 3. 替换完成验证脚本
```powershell
# scripts/validate-replacement-completion.ps1
$patterns = @{
    "未完成的TODO" = "// TODO"
    "临时实现代码" = @(
        "await Task\.CompletedTask",
        "return.*模拟", 
        "return.*mock",
        "临时实现"
    )
    "异常处理缺失" = @(
        "(?<!try\s*{[^}]*)}(?!\s*catch)",
        "catch\s*\([^}]*\}\s*(?!finally)"
    )
}

Write-Host "=== 替换完成验证 ===" -ForegroundColor Green

$totalIssues = 0
$sourceFiles = Get-ChildItem "src/Client/Desktop/Modules/**/*CoreService.cs" -Recurse

foreach ($file in $sourceFiles) {
    $fileIssues = 0
    $content = Get-Content $file.FullName -Raw
    
    foreach ($category in $patterns.Keys) {
        $patternList = if ($patterns[$category] -is [array]) { $patterns[$category] } else { @($patterns[$category]) }
        
        foreach ($pattern in $patternList) {
            $matches = [regex]::Matches($content, $pattern)
            if ($matches.Count -gt 0) {
                if ($fileIssues -eq 0) {
                    Write-Host "`n文件: $($file.Name)" -ForegroundColor Yellow
                }
                Write-Host "  $category`: $($matches.Count) 处" -ForegroundColor Red
                $fileIssues += $matches.Count
            }
        }
    }
    
    $totalIssues += $fileIssues
}

if ($totalIssues -eq 0) {
    Write-Host "`n✅ 所有TODO替换已完成，未发现问题" -ForegroundColor Green
} else {
    Write-Host "`n❌ 发现 $totalIssues 个问题需要解决" -ForegroundColor Red
}
```

---

## 📅 实施时间表

### 第1-2周：API通信统一化
| 天数 | 任务 | 交付物 |
|-----|------|--------|
| Day 1-2 | 创建统一API客户端管理器 | IUnifiedApiClientManager接口和实现 |
| Day 3-4 | 替换ConsultationCoreService | 8个TODO替换完成 |
| Day 5-6 | 替换FormulaCoreService API部分 | 12个API通信TODO替换完成 |
| Day 7-8 | 替换MedicalCaseCoreService API部分 | 8个API通信TODO替换完成 |
| Day 9-10 | 替换PrescriptionsCoreService API部分 | 8个API通信TODO替换完成 |

### 第3-5周：核心业务逻辑实现
| 天数 | 任务 | 交付物 |
|-----|------|--------|
| Day 11-13 | 实现配伍禁忌检查系统 | 完整的中药配伍检查功能 |
| Day 14-16 | 实现权限检查系统 | 统一的权限验证机制 |
| Day 17-19 | 实现患者医生关联验证 | 诊疗权限验证功能 |
| Day 20-21 | 实现缓存统计逻辑 | 缓存性能监控功能 |

### 第6-7周：基础设施完善
| 天数 | 任务 | 交付物 |
|-----|------|--------|
| Day 22-24 | 实现操作日志记录系统 | 完整的审计日志功能 |
| Day 25-26 | 实现事件通知机制 | 系统通知推送功能 |
| Day 27-28 | 实现健康检查和系统监控 | 完整的系统监控功能 |

---

## ✅ 验收标准

### 代码质量标准
- [ ] **所有85个TODO标记全部清除**
- [ ] **新增代码通过静态代码分析**
- [ ] **异常处理覆盖率100%**
- [ ] **单元测试覆盖率 ≥ 90%**

### 功能完整性标准
- [ ] **所有API调用实现统一异常处理**
- [ ] **所有业务逻辑实现完整验证**
- [ ] **所有模块间通信正常工作**
- [ ] **系统监控和日志记录正常**

### 交付标准
- [ ] **编译零警告零错误**
- [ ] **集成测试全部通过**  
- [ ] **API文档100%同步**
- [ ] **部署脚本验证通过**

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**实施开始**: 待定  
**预计完成**: 实施开始后7周  
**维护负责人**: UltraThink架构团队