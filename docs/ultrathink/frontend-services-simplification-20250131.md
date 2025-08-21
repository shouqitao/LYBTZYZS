# 前端Desktop Services层精简方案
> 生成时间：2025-01-31
> 重要原则：所有删除操作均为软删除（IsDeleted标记）

## 🗑️ 软删除策略说明

### 数据层软删除实现
```csharp
// 所有实体的删除都通过IsDeleted字段标记
public class BaseEntity
{
    public Guid Id { get; set; }
    public bool IsDeleted { get; set; } = false;  // 软删除标记
    public DateTime? DeletedAt { get; set; }       // 删除时间
    public string? DeletedBy { get; set; }         // 删除操作人
}

// Repository层软删除实现
public async Task<bool> DeleteAsync(Guid id)
{
    var entity = await _context.Set<T>().FindAsync(id);
    if (entity != null)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.DeletedBy = _currentUser.Username;
        
        _context.Update(entity);
        return await _context.SaveChangesAsync() > 0;
    }
    return false;
}

// 查询时自动过滤已删除数据
public IQueryable<T> GetActiveQuery()
{
    return _context.Set<T>().Where(e => !e.IsDeleted);
}
```

## 📦 前端Services层现状分析

### 核心服务（保留）
1. **AuthenticationService** - 身份认证服务
2. **TokenManager** - 令牌管理
3. **UserSessionManager** - 用户会话管理
4. **ApiService** - API调用基础服务
5. **ApiErrorHandler** - API错误处理
6. **MemoryCacheService** - 内存缓存服务
7. **PermissionService** - 权限管理服务

### 业务模块服务（需精简）
1. **PatientModuleService** - 患者模块服务
2. **ConsultationModuleService** - 看诊模块服务
3. **MedicalCaseModuleService** - 病历案例模块服务
4. **PrescriptionsModuleService** - 处方模块服务
5. **HerbModuleService** - 药材模块服务
6. **FormulaModuleService** - 验方模块服务
7. **UserModuleService** - 用户管理模块服务

### 冗余服务（可删除/合并）
1. **SimplifiedAuthenticationService** - 与AuthenticationService重复
2. **PlaceholderServices** - 占位服务，无实际功能
3. **ApiTestService** - 测试服务，生产环境不需要
4. **SimpleDialogService** - 与CommonDialogService重复
5. **PrismDialogService** - 与CommonDialogService重复

## 🔧 前端服务层精简实施

### 1. PatientModuleService精简
```csharp
// src/Client/Desktop/Modules/Patients/Services/PatientModuleService.cs

public class PatientModuleService : IPatientModuleService
{
    // ✅ 保留的核心方法（15个）
    public async Task<ServiceResult<PagedResult<PatientInfo>>> GetPagedAsync(PatientQuery query);
    public async Task<ServiceResult<PatientInfo>> GetByIdAsync(Guid id);
    public async Task<ServiceResult<PatientInfo>> CreateAsync(CreatePatientInfo info);
    public async Task<ServiceResult<PatientInfo>> UpdateAsync(UpdatePatientInfo info);
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id);  // 软删除
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, bool isEnabled);
    public async Task<ServiceResult<List<PatientInfo>>> SearchAsync(string keyword);
    public async Task<ServiceResult<bool>> CheckDuplicateAsync(string idCard, string phone);
    public async Task<ServiceResult<int>> ImportPatientsAsync(List<ImportPatientInfo> patients);
    public async Task<ServiceResult<byte[]>> ExportPatientsAsync(ExportPatientQuery query);
    public async Task<ServiceResult<PatientInfo>> GetByPhoneAsync(string phone);
    public async Task<ServiceResult<PatientInfo>> GetByIdCardAsync(string idCard);
    public async Task<ServiceResult<List<PatientInfo>>> QuickSearchAsync(string keyword);
    public async Task<ServiceResult<bool>> ValidatePatientDataAsync(PatientInfo patient);
    
    // ❌ 删除的方法（28个）- 使用#region标记为废弃
    #region 已废弃功能 - 标签管理
    /*
    public async Task<ServiceResult<List<PatientTagInfo>>> GetPatientTagsAsync(Guid patientId);
    public async Task<ServiceResult<bool>> AddPatientTagAsync(Guid patientId, Guid tagId);
    // ... 其他标签相关方法
    */
    #endregion
    
    #region 已废弃功能 - 档案管理
    /*
    public async Task<ServiceResult<PatientArchiveInfo>> GetArchiveAsync(Guid patientId);
    // ... 其他档案相关方法
    */
    #endregion
    
    #region 已废弃功能 - 统计分析
    /*
    public async Task<ServiceResult<PatientStatisticsInfo>> GetStatisticsAsync();
    // ... 其他统计相关方法
    */
    #endregion
}
```

### 2. ConsultationModuleService精简
```csharp
// src/Client/Desktop/Modules/Consultation/Services/ConsultationModuleService.cs

public class ConsultationModuleService : IConsultationModuleService
{
    // ✅ 保留的核心方法（13个）
    public async Task<ServiceResult<PagedResult<ConsultationInfo>>> GetPagedAsync(ConsultationQuery query);
    public async Task<ServiceResult<ConsultationInfo>> GetByIdAsync(Guid id);
    public async Task<ServiceResult<ConsultationInfo>> CreateAsync(CreateConsultationInfo info);
    public async Task<ServiceResult<ConsultationInfo>> UpdateAsync(UpdateConsultationInfo info);
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id);  // 软删除
    public async Task<ServiceResult<List<ConsultationInfo>>> GetByPatientIdAsync(Guid patientId);
    public async Task<ServiceResult<List<ConsultationInfo>>> GetByDoctorIdAsync(Guid doctorId);
    public async Task<ServiceResult<List<ConsultationInfo>>> GetTodayConsultationsAsync();
    public async Task<ServiceResult<bool>> UpdateDiagnosisAsync(Guid id, string diagnosis);
    public async Task<ServiceResult<List<DiagnosisHistoryInfo>>> GetPatientHistoryAsync(Guid patientId);
    public async Task<ServiceResult<List<ConsultationInfo>>> SearchConsultationsAsync(string keyword);
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, ConsultationStatus status);
    
    // ❌ 删除的方法（3个）
    #region 已废弃功能 - 统计分析
    /*
    public async Task<ServiceResult<ConsultationStatisticsInfo>> GetConsultationStatisticsAsync();
    public async Task<ServiceResult<DoctorPerformanceInfo>> GetDoctorPerformanceAsync(Guid doctorId);
    public async Task<ServiceResult<byte[]>> GenerateConsultationReportAsync(ReportRequest request);
    */
    #endregion
}
```

### 3. MedicalCaseModuleService精简
```csharp
// src/Client/Desktop/Modules/MedicalCase/Services/MedicalCaseModuleService.cs

public class MedicalCaseModuleService : IMedicalCaseModuleService
{
    private readonly IMedicalCaseApi _api;
    private readonly IPrintService _printService;
    
    // ✅ 保留的核心方法（11个）
    public async Task<ServiceResult<PagedResult<MedicalCaseInfo>>> GetPagedAsync(MedicalCaseQuery query);
    public async Task<ServiceResult<MedicalCaseInfo>> GetByIdAsync(Guid id);
    public async Task<ServiceResult<MedicalCaseInfo>> CreateAsync(CreateMedicalCaseInfo info);
    public async Task<ServiceResult<MedicalCaseInfo>> UpdateAsync(UpdateMedicalCaseInfo info);
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id);  // 软删除
    public async Task<ServiceResult<List<MedicalCaseInfo>>> GetByPatientIdAsync(Guid patientId);
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id);  // 完成/归档
    public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason);  // 暂停
    public async Task<ServiceResult<List<MedicalCaseInfo>>> SearchAsync(string patientName);
    
    // 🆕 新增：打印功能（从处方模块迁移）
    public async Task<ServiceResult<byte[]>> PrintMedicalRecordAsync(Guid caseId)
    {
        // 获取完整的病历信息
        var caseResult = await GetByIdAsync(caseId);
        if (!caseResult.IsSuccess)
            return ServiceResult<byte[]>.Failure("获取病历失败");
            
        // 准备打印数据
        var printData = new MedicalRecordPrintInfo
        {
            CaseInfo = caseResult.Data,
            ConsultationInfo = caseResult.Data.Consultation,
            PrescriptionInfo = caseResult.Data.Prescription,
            PatientInfo = caseResult.Data.Patient,
            PrintTime = DateTime.Now
        };
        
        // 生成PDF
        return await _printService.GenerateMedicalRecordPdfAsync(printData);
    }
    
    // ❌ 删除的方法（10个）
    #region 已废弃功能
    /*
    public async Task<ServiceResult<MedicalCaseInfo>> CloneAsync(Guid id);
    public async Task<ServiceResult<int>> BatchDeleteAsync(List<Guid> ids);
    public async Task<ServiceResult<MedicalCaseStatisticsInfo>> GetStatisticsAsync();
    public async Task<ServiceResult<byte[]>> ExportAsync(ExportQuery query);
    // ... 其他废弃方法
    */
    #endregion
}
```

### 4. PrescriptionsModuleService精简
```csharp
// src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsModuleService.cs

public class PrescriptionsModuleService : IPrescriptionsModuleService
{
    // ✅ 保留的核心方法（10个）
    public async Task<ServiceResult<PagedResult<PrescriptionInfo>>> GetPagedAsync(PrescriptionQuery query);
    public async Task<ServiceResult<PrescriptionInfo>> GetByIdAsync(Guid id);
    
    // 创建处方时自动关联适应症
    public async Task<ServiceResult<PrescriptionInfo>> CreateAsync(CreatePrescriptionInfo info)
    {
        // 如果有关联的看诊记录，自动获取诊断作为适应症
        if (info.ConsultationId.HasValue && string.IsNullOrEmpty(info.Indication))
        {
            var consultationResult = await _consultationService.GetByIdAsync(info.ConsultationId.Value);
            if (consultationResult.IsSuccess)
            {
                info.Indication = consultationResult.Data.Diagnosis;
            }
        }
        
        return await _api.CreateAsync(info);
    }
    
    public async Task<ServiceResult<PrescriptionInfo>> UpdateAsync(UpdatePrescriptionInfo info);
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id);  // 软删除
    public async Task<ServiceResult<List<PrescriptionInfo>>> GetByPatientIdAsync(Guid patientId);
    public async Task<ServiceResult<List<PrescriptionInfo>>> GetRecentPrescriptionsAsync(Guid patientId, int count = 5);
    
    // 导入历史处方（原"复制"功能）
    public async Task<ServiceResult<PrescriptionInfo>> ImportPrescriptionAsync(Guid sourcePrescriptionId)
    {
        var sourceResult = await GetByIdAsync(sourcePrescriptionId);
        if (!sourceResult.IsSuccess)
            return ServiceResult<PrescriptionInfo>.Failure("源处方不存在");
            
        // 创建新处方，基于历史处方内容
        var newPrescription = new CreatePrescriptionInfo
        {
            Items = sourceResult.Data.Items.Select(i => new CreatePrescriptionItemInfo
            {
                HerbId = i.HerbId,
                HerbName = i.HerbName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                // 其他字段...
            }).ToList(),
            Remark = $"基于历史处方导入 - {DateTime.Now:yyyy-MM-dd}"
        };
        
        return await CreateAsync(newPrescription);
    }
    
    public async Task<ServiceResult<List<PrescriptionInfo>>> SearchAsync(string keyword);
    
    // ❌ 删除的方法（15个）- 打印功能已迁移到MedicalCase
    #region 已废弃功能
    /*
    public async Task<ServiceResult<byte[]>> PrintPrescriptionAsync(Guid id);  // 迁移到MedicalCase
    public async Task<ServiceResult<bool>> SubmitForApprovalAsync(Guid id);
    public async Task<ServiceResult<bool>> ApproveAsync(Guid id, bool approved, string comment);
    public async Task<ServiceResult<PrescriptionStatisticsInfo>> GetStatisticsAsync();
    // ... 其他废弃方法
    */
    #endregion
}
```

### 5. HerbModuleService精简
```csharp
// src/Client/Desktop/Modules/Herbs/Services/HerbModuleService.cs

public class HerbModuleService : IHerbModuleService
{
    // ✅ 保留的核心方法（18个）
    public async Task<ServiceResult<PagedResult<HerbInfo>>> GetPagedAsync(HerbQuery query);
    public async Task<ServiceResult<HerbInfo>> GetByIdAsync(Guid id);
    public async Task<ServiceResult<HerbInfo>> CreateAsync(CreateHerbInfo info);
    public async Task<ServiceResult<HerbInfo>> UpdateAsync(UpdateHerbInfo info);
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id);  // 软删除
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, bool isEnabled);
    public async Task<ServiceResult<int>> BatchUpdateStatusAsync(List<Guid> ids, bool isEnabled);
    public async Task<ServiceResult<bool>> UpdatePriceAsync(Guid id, decimal newPrice);
    public async Task<ServiceResult<int>> BatchUpdatePricesAsync(Dictionary<Guid, decimal> priceUpdates);
    public async Task<ServiceResult<int>> ImportHerbsAsync(List<ImportHerbInfo> herbs);
    public async Task<ServiceResult<byte[]>> ExportHerbsAsync(ExportHerbQuery query);
    public async Task<ServiceResult<List<HerbInfo>>> SearchAsync(string keyword);
    public async Task<ServiceResult<bool>> CheckDuplicateAsync(string name, string pinyin);
    public async Task<ServiceResult<HerbInfo>> GetByNameAsync(string name);
    public async Task<ServiceResult<List<HerbInfo>>> GetByPinyinAsync(string pinyin);
    public async Task<ServiceResult<List<HerbInfo>>> GetActiveHerbsAsync();  // 缓存30分钟
    public async Task<ServiceResult<bool>> ValidateHerbDataAsync(HerbInfo herb);
    
    // ❌ 删除的方法（22个）
    #region 已废弃功能
    /*
    // 统计分析（8个）
    // 分类管理（6个）
    // 供应商管理（4个）
    // 库存管理（4个）
    */
    #endregion
}
```

### 6. FormulaModuleService精简
```csharp
// src/Client/Desktop/Modules/Formula/Services/FormulaModuleService.cs

public class FormulaModuleService : IFormulaModuleService
{
    // ✅ 保留的核心方法（16个）
    public async Task<ServiceResult<PagedResult<FormulaInfo>>> GetPagedAsync(FormulaQuery query);
    public async Task<ServiceResult<FormulaInfo>> GetByIdAsync(Guid id);
    public async Task<ServiceResult<FormulaInfo>> CreateAsync(CreateFormulaInfo info);
    public async Task<ServiceResult<FormulaInfo>> UpdateAsync(UpdateFormulaInfo info);
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id);  // 软删除
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, bool isEnabled);
    public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, List<Guid> targetDoctorIds);
    public async Task<ServiceResult<List<FormulaInfo>>> GetSharedFormulasAsync();
    public async Task<ServiceResult<FormulaInfo>> CopyFormulaAsync(Guid sourceId, string newName);
    public async Task<ServiceResult<int>> ImportFormulasAsync(List<ImportFormulaInfo> formulas);
    public async Task<ServiceResult<byte[]>> ExportFormulasAsync(ExportFormulaQuery query);
    public async Task<ServiceResult<List<FormulaInfo>>> SearchAsync(string keyword);
    public async Task<ServiceResult<List<FormulaInfo>>> GetByDoctorIdAsync(Guid doctorId);
    public async Task<ServiceResult<List<FormulaInfo>>> GetClassicFormulasAsync();
    public async Task<ServiceResult<bool>> ValidateFormulaAsync(FormulaInfo formula);
    
    // ❌ 删除的方法（15个）
    #region 已废弃功能
    /*
    // 推荐功能（3个）
    // 统计分析（5个）
    // 分类管理（4个）
    // 评价功能（3个）
    */
    #endregion
}
```

## 🗂️ 服务注册简化

```csharp
// src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs

public static IServiceCollection AddDesktopServices(this IServiceCollection services)
{
    // 核心服务
    services.AddSingleton<ITokenManager, TokenManager>();
    services.AddScoped<IAuthenticationService, AuthenticationService>();
    services.AddScoped<IUserSessionManager, UserSessionManager>();
    services.AddScoped<IApiService, ApiService>();
    services.AddSingleton<IMemoryCache, MemoryCache>();
    services.AddSingleton<ICacheService, MemoryCacheService>();
    services.AddScoped<IPermissionService, PermissionService>();
    
    // 业务模块服务
    services.AddScoped<IPatientModuleService, PatientModuleService>();
    services.AddScoped<IConsultationModuleService, ConsultationModuleService>();
    services.AddScoped<IMedicalCaseModuleService, MedicalCaseModuleService>();
    services.AddScoped<IPrescriptionsModuleService, PrescriptionsModuleService>();
    services.AddScoped<IHerbModuleService, HerbModuleService>();
    services.AddScoped<IFormulaModuleService, FormulaModuleService>();
    services.AddScoped<IUserModuleService, UserModuleService>();
    
    // 辅助服务
    services.AddScoped<IPrintService, PrintService>();
    services.AddScoped<ICommonDialogService, CommonDialogService>();
    services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
    
    // ❌ 删除冗余服务注册
    // services.AddScoped<ISimplifiedAuthenticationService, SimplifiedAuthenticationService>();
    // services.AddScoped<IApiTestService, ApiTestService>();
    // services.AddScoped<ISimpleDialogService, SimpleDialogService>();
    
    return services;
}
```

## 🎯 前端服务层优化要点

### 1. 缓存策略
```csharp
// 对变化少的数据进行缓存
public class HerbModuleService
{
    private readonly IMemoryCache _cache;
    
    public async Task<ServiceResult<List<HerbInfo>>> GetActiveHerbsAsync()
    {
        return await _cache.GetOrCreateAsync("active_herbs", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            var result = await _api.GetActiveHerbsAsync();
            return result;
        });
    }
}
```

### 2. 批量操作优化
```csharp
// 批量更新时使用事务
public async Task<ServiceResult<int>> BatchUpdateStatusAsync(List<Guid> ids, bool isEnabled)
{
    if (ids == null || !ids.Any())
        return ServiceResult<int>.Success(0);
        
    // 分批处理，每批100个
    var batches = ids.Chunk(100);
    var totalUpdated = 0;
    
    foreach (var batch in batches)
    {
        var result = await _api.BatchUpdateStatusAsync(batch.ToList(), isEnabled);
        if (result.IsSuccess)
            totalUpdated += result.Data;
    }
    
    return ServiceResult<int>.Success(totalUpdated);
}
```

### 3. 错误处理统一
```csharp
// 使用ApiErrorHandler统一处理
public async Task<ServiceResult<T>> ExecuteApiCallAsync<T>(Func<Task<ApiResponse<T>>> apiCall)
{
    return await ApiErrorHandler.HandleApiResponseAsync(apiCall, 
        operationName: GetOperationName(), 
        maxRetries: 2);
}
```

## 📊 精简效果预估

| 层面 | 精简前 | 精简后 | 减少比例 |
|------|--------|--------|----------|
| 服务类数量 | 75个 | 45个 | -40% |
| API调用方法 | 223个 | 121个 | -46% |
| 代码行数 | ~15000行 | ~8000行 | -47% |
| 内存占用 | ~150MB | ~80MB | -47% |

## ✅ 验收标准

- [ ] 所有删除操作均实现软删除
- [ ] 核心业务功能正常运行
- [ ] 前端服务与后端API对应
- [ ] 缓存机制工作正常
- [ ] 错误处理机制完善

---
*本方案为前端服务层精简的具体实施指南，确保与后端精简同步进行。*