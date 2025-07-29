# LYBT.Module.Records 功能说明文档

## 模块概述

病历模块是中医诊疗系统的核心数据管理中心，负责完整医疗记录的创建、存储、管理和共享。本模块实现了结构化的电子病历系统，支持中医特色的辨证论治记录、药材组方、治疗方案等专业内容，并提供医生间的病历共享协作功能。通过标准化的病历管理，保障医疗质量并为临床决策提供历史数据支持。

## 业务价值

- **完整记录**: 全面记录患者从主诉到治疗的完整医疗过程
- **中医特色**: 支持中医特色的辨证论治、药材组方等专业内容
- **知识共享**: 医生间的病历共享促进医疗协作和经验交流
- **历史追溯**: 完整的病历历史便于疾病发展跟踪和治疗效果评估
- **质量管控**: 规范化的病历记录提升医疗质量和安全性
- **数据价值**: 结构化数据为医学研究和临床决策提供支持

## 数据模型

### RecordModel (病历主实体)

**文件位置**: `LYBT.Module.Records/Models/RecordModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 主键ID | 自动生成，唯一标识 | 病历记录唯一标识 |
| RecordId | Guid | 记录ID | 可能与Id重复，历史兼容 | 病历业务标识 |
| PatientId | Guid | 患者ID | 必填，关联患者表 | 建立与患者的关联 |
| DoctorId | Guid | 医生ID | 必填，关联医生表 | 记录主治医生信息 |
| ChiefComplaint | string | 主诉 | 必填，详细描述主要症状 | 患者主要症状和就诊原因 |
| DiagnosisText | string | 诊断文本 | 必填，诊断结论描述 | 医生的诊断结论 |
| Diagnosis | string | 诊断 | 必填，标准化诊断名称 | 规范化的诊断信息 |
| PresentIllness | string? | 现病史 | 可选，详细病史描述 | 疾病发展过程记录 |
| TreatmentAdvice | string? | 治疗建议 | 可选，治疗方案建议 | 医生的治疗指导意见 |
| PrescriptionSummary | string | 处方摘要 | 自动生成，处方简要信息 | 快速了解用药情况 |
| TreatmentSummary | string | 治疗摘要 | 自动生成，治疗方案摘要 | 治疗项目的简要说明 |
| PrescriptionId | Guid? | 处方ID | 可选，关联处方记录 | 建立与处方的关联 |
| DiagnosisResults | List&lt;string&gt; | 辨证结果列表 | 中医特色，辨证论治结果 | 中医诊断的辨证分析 |
| HerbalFormula | List&lt;HerbItemModel&gt;? | 药材组成 | 可选，药方组成详情 | 详细的药材配方信息 |
| TreatmentPlans | List&lt;TreatmentItemModel&gt;? | 辅助治疗方案 | 可选，非药物治疗方案 | 针灸、推拿等治疗项目 |
| IsShared | bool | 是否共享 | 默认false | 控制病历的共享状态 |
| SharedToDoctorIds | List&lt;string&gt; | 共享给医生ID列表 | 空列表表示未共享 | 指定可查看的医生范围 |
| CreatedBy | string? | 创建医生ID | 自动设置，记录创建者 | 病历责任医生标识 |
| CreatedTime | DateTime | 创建时间 | 自动设置，记录创建时间 | 病历档案创建时间 |
| VisitTime | DateTime | 就诊时间 | 必填，实际就诊时间 | 患者实际就诊的时间 |
| RecordTime | DateTime | 病历记录时间 | 可手动设置，病历记录时间 | 医生记录病历的时间 |

### 辨证论治特色字段

中医病历的特色体现在以下字段的结构化设计：

| 字段名 | 中医含义 | 数据结构 | 使用说明 |
|--------|----------|----------|----------|
| DiagnosisResults | 辨证结果 | List&lt;string&gt; | 记录"气虚血瘀"、"肝郁脾虚"等中医辨证结果 |
| HerbalFormula | 方药组成 | 复杂对象列表 | 详细记录每味药材的用量、用法 |
| TreatmentPlans | 治疗方案 | 复杂对象列表 | 针灸、推拿、拔罐等辅助治疗 |

## DTO 数据传输对象

### RecordCreateDto (新增病历)

**使用场景**: 医生完成诊疗后创建完整病历记录
**特点**: 包含完整的诊疗信息和中医特色内容

```csharp
- PatientId: 病人ID（必填，string类型）
- RegistrationId: 挂号ID（必填，关联挂号记录）
- Diagnosis: 诊断内容（必填，主要诊断结论）
- ChiefComplaint: 主诉（可选，患者主要症状）
- PresentIllness: 现病史（可选，疾病发展历程）
- TreatmentAdvice: 诊疗建议（可选，后续治疗指导）
- DiagnosisResults: 辩证结果列表（中医特色）
- HerbalFormula: 药材组成（可选，详细药方）
- TreatmentPlans: 辅助治疗方案（可选，非药物治疗）
- IsShared: 是否共享（默认false）
- SharedToDoctorIds: 共享给医生ID列表
- CreatedBy: 创建医生ID（自动设置）
- CreatedTime: 创建时间（自动设置）
- PrescriptionId: 开方信息（可选，关联处方）
- RecordTime: 病历创建时间（可手动设置）
```

**验证规则**:
- 患者ID和挂号ID必须存在且有效
- 诊断内容不能为空
- 药材组成必须符合处方规范
- 共享医生ID必须是有效的医生

### RecordDetailDto (病历详情)

**使用场景**: 查看完整的病历信息，包含所有关联数据
**特点**: 包含患者姓名等关联信息，便于完整展示

```csharp
- Id: 病历ID
- PatientId: 病人ID
- PatientName: 病人姓名（关联查询）
- RegistrationId: 挂号ID
- Diagnosis: 诊断内容
- ChiefComplaint: 主诉
- PresentIllness: 现病史
- TreatmentAdvice: 诊疗建议
- PrescriptionId: 开方信息
- DiagnosisResults: 辩证结果列表
- HerbalFormula: 药材组成
- TreatmentPlans: 辅助治疗方案
- IsShared: 是否共享
- SharedToDoctorIds: 共享给医生ID列表
- CreatedBy: 创建医生ID
- CreatedTime: 创建时间
- RecordTime: 病历创建/修改时间
```

### RecordDto (病历列表)

**使用场景**: 病历列表展示和快速检索
**特点**: 精简信息，适合列表显示和搜索

### RecordEditDto (编辑病历)

**使用场景**: 修改现有病历记录的内容
**特点**: 包含ID标识和所有可修改的字段

## 服务层 (IRecordService & RecordService)

### 基础病历管理方法

#### GetByIdAsync

```csharp
Task<RecordDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 获取指定病历的详细信息
**业务逻辑**: 
- 根据ID查询病历记录
- 包含完整的辨证、药方、治疗信息
- 使用AutoMapper进行实体到DTO转换
- 处理数据不存在的情况

**使用场景**: 病历详情查看、编辑前数据加载、病历共享查看

#### GetListAsync

```csharp
Task<List<RecordDto>> GetListAsync()
```

**功能**: 获取病历记录列表
**业务逻辑**: 
- 查询所有病历记录
- 按记录时间倒序排列
- 返回精简的列表信息

**使用场景**: 病历管理列表、统计分析、数据导出

#### AddAsync

```csharp
Task<bool> AddAsync(RecordCreateDto recordCreateDto, Guid operatorId, string operatorName)
```

**功能**: 创建新的病历记录
**业务逻辑**: 
- 验证输入数据的完整性和有效性
- 生成新的病历ID
- 设置创建者和创建时间
- 处理药材组方和治疗方案的关联
- 记录详细的操作日志

**特殊处理**:
- 自动生成处方摘要和治疗摘要
- 验证药材配方的合理性
- 检查治疗方案的可行性
- 建立与挂号记录的关联

**审计日志**: 记录病历创建的完整信息，包括操作者、时间、完整内容

**使用场景**: 诊疗结束后病历归档、批量病历导入、病历模板应用

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(RecordEditDto recordEditDto, Guid operatorId, string operatorName)
```

**功能**: 更新现有病历记录
**业务逻辑**: 
- 验证病历记录的存在性
- 保留变更前的完整数据
- 更新可修改的字段内容
- 保持关键信息（患者、医生）不变
- 记录详细的变更日志

**特殊处理**:
- 增量更新策略，保留未修改字段
- 药材配方的版本控制
- 治疗方案的调整记录
- 共享状态的变更处理

**审计日志**: 记录修改前后的完整对比，便于变更追溯

**使用场景**: 病历修正、补充诊疗信息、治疗方案调整

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 删除指定的病历记录
**业务逻辑**: 
- 验证病历记录的存在性
- 检查是否存在关联的处方或其他记录
- 记录删除前的完整信息
- 执行物理删除操作
- 记录删除操作的审计日志

**安全考虑**:
- 需要管理员权限或病历创建者权限
- 删除前的二次确认机制
- 关联数据的依赖检查
- 完整的删除记录审计

**使用场景**: 错误记录清理、隐私数据处理、系统维护

### 专业病历功能方法

#### GetByPatientIdAsync

```csharp
Task<List<RecordDto>> GetByPatientIdAsync(Guid patientId)
```

**功能**: 获取指定患者的所有病历记录
**业务逻辑**: 
- 查询患者的完整病历历史
- 按记录时间倒序排列
- 包含共享给当前医生的其他医生记录

**特殊处理**:
- 权限控制，确保医生只能查看有权限的病历
- 时间排序便于了解疾病发展历程
- 共享病历的标识显示

**使用场景**: 患者病史查询、疾病发展跟踪、治疗效果评估

#### MarkAsSharedAsync

```csharp
Task<bool> MarkAsSharedAsync(Guid id, List<string> doctorIds)
```

**功能**: 标记病历为共享状态并指定共享对象
**业务逻辑**: 
- 验证病历的存在性和共享权限
- 设置共享状态为true
- 更新共享医生列表
- 通知被共享的医生

**特殊处理**:
- 验证共享医生ID的有效性
- 处理重复共享的情况
- 权限检查，确保只有病历创建者可以共享
- 共享记录的日志记录

**使用场景**: 疑难病例会诊、医生协作、经验分享

#### RevokeSharingAsync

```csharp
Task<bool> RevokeSharingAsync(Guid id)
```

**功能**: 撤销病历的共享状态
**业务逻辑**: 
- 验证病历的存在性和操作权限
- 设置共享状态为false
- 清空共享医生列表
- 通知之前可访问的医生

**使用场景**: 隐私保护、共享权限回收、病历状态管理

#### GetSharedRecordsAsync

```csharp
Task<List<RecordDto>> GetSharedRecordsAsync(Guid doctorId)
```

**功能**: 获取共享给指定医生的病历列表
**业务逻辑**: 
- 查询所有共享状态的病历
- 筛选包含指定医生ID的记录
- 按时间排序返回结果

**使用场景**: 医生查看共享病历、会诊准备、学习参考

## 仓储层 (IRecordRepository & RecordRepository)

### 基础数据操作

#### GetByIdAsync

```csharp
Task<RecordModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取病历实体
**实现细节**: 
- 使用EF Core的FindAsync方法
- 简单高效的单记录查询
- 包含完整的病历信息

#### GetListAsync

```csharp
Task<List<RecordModel>> GetListAsync()
```

**功能**: 获取所有病历记录
**实现细节**: 
- 返回完整的病历列表
- 适合管理员查看和系统统计
- 可扩展为支持分页查询

#### AddAsync

```csharp
Task<bool> AddAsync(RecordModel recordModel)
```

**功能**: 新增病历记录到数据库
**实现细节**: 
- 使用EF Core的Add方法
- 级联保存关联的药材和治疗数据
- 事务性操作确保数据一致性

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(RecordModel recordModel)
```

**功能**: 更新病历记录
**实现细节**: 
- 使用EF Core的Update方法
- 全量更新策略
- 自动处理实体状态跟踪

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除病历记录
**实现细节**: 
- 先查询再删除的安全模式
- 物理删除策略
- 返回操作结果状态

### 专业查询方法

#### GetListByPatientIdAsync

```csharp
Task<List<RecordModel>> GetListByPatientIdAsync(Guid patientId)
```

**功能**: 根据患者ID查询病历列表
**实现细节**: 
- 使用患者ID进行过滤
- 按记录时间倒序排列
- 支持异步查询优化

#### GetSharedRecordsAsync

```csharp
Task<List<RecordModel>> GetSharedRecordsAsync(Guid doctorId)
```

**功能**: 查询共享给指定医生的病历
**实现细节**: 
- 先查询所有共享状态的病历
- 在内存中筛选包含指定医生的记录
- 可优化为数据库层面的查询

## 权限控制策略

### 操作权限

- **查看权限**: 医生可查看自己创建的病历和共享给自己的病历，管理员可查看所有病历
- **创建权限**: 只有执业医生可以创建病历记录
- **修改权限**: 病历创建者可修改自己的病历，限定时间内可修改
- **删除权限**: 需要管理员权限或病历创建者权限，且有严格的审核流程

### 共享权限

- **共享控制**: 只有病历创建者可以设置共享状态和共享对象
- **查看范围**: 被共享的医生只能查看，不能修改或再次共享
- **撤销权限**: 病历创建者可随时撤销共享权限
- **审计追踪**: 所有共享操作都有完整的审计记录

### 数据安全

- **隐私保护**: 病历数据按最高级别的隐私标准保护
- **访问控制**: 严格的基于角色的访问控制
- **操作审计**: 所有关键操作都有详细的审计日志

## 日志审计机制

### 操作日志

所有病历相关操作都会记录详细日志：

- **病历创建**: 记录创建者、患者、完整病历内容
- **病历修改**: 记录修改前后的完整对比数据
- **病历删除**: 记录删除者、删除时间、完整病历信息
- **共享操作**: 记录共享设置、被共享医生、操作时间

### 业务日志

- **诊疗质量**: 记录病历的完整性和规范性检查结果
- **医生协作**: 记录病历共享和协作的详细过程
- **系统使用**: 记录病历系统的使用频次和效率数据
- **异常处理**: 记录异常情况和处理过程

### 审计内容

- 操作时间和操作者信息
- 病历数据的完整变更历史
- 关键业务数据的变更记录
- 权限操作和共享记录

## 集成依赖

### 外部模块依赖

- **LYBT.Module.Patients**: 患者基础信息查询和验证
- **LYBT.Module.Doctors**: 医生信息查询和权限验证
- **LYBT.Module.Registration**: 挂号信息关联和验证
- **LYBT.Module.DiagnosisTreatment**: 诊疗记录的数据来源
- **LYBT.Module.Prescriptions**: 处方信息的关联管理
- **LYBT.Module.Herbs**: 药材基础数据查询

### 基础服务依赖

- **IUnifiedLogService**: 统一日志服务（重点依赖）
- **IMapper**: AutoMapper对象映射服务
- **RecordDbContext**: 专用数据库上下文
- **IAuthorizationService**: 权限验证服务

## 使用示例

### 创建完整病历记录

```csharp
var createDto = new RecordCreateDto
{
    PatientId = "P123456",
    RegistrationId = registrationId,
    Diagnosis = "肝郁脾虚证",
    ChiefComplaint = "胸胁胀痛，腹胀纳差，情志不舒",
    PresentIllness = "患者2月前因工作压力大开始出现胸胁胀痛，伴腹胀纳差，情绪低落，时有叹息",
    TreatmentAdvice = "疏肝理气，健脾和胃。服药期间避免情绪激动，饮食清淡",
    DiagnosisResults = new List<string> { "肝郁", "脾虚", "气滞" },
    HerbalFormula = new List<HerbModel>
    {
        new HerbModel { Name = "柴胡", Price = 12.00m },
        new HerbModel { Name = "白芍", Price = 15.00m },
        new HerbModel { Name = "白术", Price = 18.00m },
        new HerbModel { Name = "茯苓", Price = 10.00m }
    },
    TreatmentPlans = new List<TreatmentItemModel>
    {
        new TreatmentItemModel { Name = "针灸", Count = 3, Price = 80.00m },
        new TreatmentItemModel { Name = "推拿", Count = 2, Price = 60.00m }
    },
    IsShared = false,
    PrescriptionId = prescriptionId
};

var success = await recordService.AddAsync(createDto, currentUserId, currentUserName);
if (success)
{
    logger.LogInformation("病历创建成功，患者ID: {PatientId}", createDto.PatientId);
}
```

### 查询患者病历历史

```csharp
// 获取患者的完整病历历史
var patientId = Guid.Parse("patient-guid-here");
var patientRecords = await recordService.GetByPatientIdAsync(patientId);

// 按时间分组显示病历
var recordsByYear = patientRecords
    .GroupBy(r => r.RecordTime.Year)
    .OrderByDescending(g => g.Key)
    .ToList();

foreach (var yearGroup in recordsByYear)
{
    Console.WriteLine($"==== {yearGroup.Key}年病历 ====");
    foreach (var record in yearGroup.OrderByDescending(r => r.RecordTime))
    {
        Console.WriteLine($"{record.RecordTime:MM-dd}: {record.Diagnosis}");
        Console.WriteLine($"主诉: {record.ChiefComplaint}");
        Console.WriteLine($"辨证: {string.Join("、", record.DiagnosisResults)}");
        Console.WriteLine();
    }
}
```

### 病历共享管理

```csharp
// 共享病历给其他医生
public async Task<bool> ShareRecordToColleaguesAsync(Guid recordId, List<string> doctorIds, string reason)
{
    // 验证共享权限
    var record = await recordService.GetByIdAsync(recordId);
    if (record == null || record.CreatedBy != currentUserId.ToString())
    {
        return false;
    }
    
    // 执行共享
    var success = await recordService.MarkAsSharedAsync(recordId, doctorIds);
    
    if (success)
    {
        // 记录共享日志
        logger.LogInformation("病历共享成功，病历ID: {RecordId}, 共享给: {DoctorIds}, 原因: {Reason}", 
            recordId, string.Join(",", doctorIds), reason);
        
        // 发送通知给被共享的医生
        await NotifySharedDoctorsAsync(doctorIds, record, reason);
    }
    
    return success;
}

// 查看共享给我的病历
public async Task<List<RecordDto>> GetMySharedRecordsAsync()
{
    var sharedRecords = await recordService.GetSharedRecordsAsync(currentUserId);
    
    // 按共享时间排序
    return sharedRecords.OrderByDescending(r => r.RecordTime).ToList();
}
```

### 病历内容搜索

```csharp
// 根据诊断内容搜索病历
public async Task<List<RecordDto>> SearchRecordsByDiagnosisAsync(string diagnosisKeyword)
{
    var allRecords = await recordService.GetListAsync();
    
    // 搜索诊断内容包含关键词的病历
    var matchedRecords = allRecords.Where(r => 
        r.Diagnosis.Contains(diagnosisKeyword) || 
        r.DiagnosisResults.Any(dr => dr.Contains(diagnosisKeyword)))
        .ToList();
    
    return matchedRecords;
}

// 根据药材搜索相关病历
public async Task<List<RecordDto>> SearchRecordsByHerbAsync(string herbName)
{
    var allRecords = await recordService.GetListAsync();
    
    var matchedRecords = allRecords.Where(r => 
        r.HerbalFormula != null && 
        r.HerbalFormula.Any(h => h.Name.Contains(herbName)))
        .ToList();
    
    return matchedRecords;
}
```

### 病历统计分析

```csharp
// 获取医生的病历统计
public async Task<DoctorRecordStatsDto> GetDoctorRecordStatsAsync(Guid doctorId, DateTime startDate, DateTime endDate)
{
    var allRecords = await recordService.GetListAsync();
    var doctorRecords = allRecords.Where(r => 
        r.CreatedBy == doctorId.ToString() && 
        r.RecordTime >= startDate && 
        r.RecordTime <= endDate).ToList();
    
    return new DoctorRecordStatsDto
    {
        TotalRecords = doctorRecords.Count,
        UniquePatients = doctorRecords.Select(r => r.PatientId).Distinct().Count(),
        CommonDiagnoses = doctorRecords
            .SelectMany(r => r.DiagnosisResults)
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new DiagnosisStatsDto { Name = g.Key, Count = g.Count() })
            .ToList(),
        SharedRecords = doctorRecords.Count(r => r.IsShared),
        AverageRecordLength = doctorRecords.Average(r => 
            (r.ChiefComplaint?.Length ?? 0) + 
            (r.PresentIllness?.Length ?? 0) + 
            (r.TreatmentAdvice?.Length ?? 0))
    };
}
```

### 病历质量检查

```csharp
// 检查病历完整性
public async Task<RecordQualityCheckDto> CheckRecordQualityAsync(Guid recordId)
{
    var record = await recordService.GetByIdAsync(recordId);
    if (record == null) return null;
    
    var qualityCheck = new RecordQualityCheckDto
    {
        RecordId = recordId,
        IsComplete = true,
        Issues = new List<string>()
    };
    
    // 检查必填项
    if (string.IsNullOrEmpty(record.ChiefComplaint))
    {
        qualityCheck.IsComplete = false;
        qualityCheck.Issues.Add("缺少主诉信息");
    }
    
    if (string.IsNullOrEmpty(record.Diagnosis))
    {
        qualityCheck.IsComplete = false;
        qualityCheck.Issues.Add("缺少诊断信息");
    }
    
    // 检查中医特色内容
    if (record.DiagnosisResults == null || !record.DiagnosisResults.Any())
    {
        qualityCheck.Issues.Add("建议添加辨证结果");
    }
    
    if (record.HerbalFormula == null || !record.HerbalFormula.Any())
    {
        qualityCheck.Issues.Add("建议添加药材组方");
    }
    
    // 检查内容合理性
    if (!string.IsNullOrEmpty(record.TreatmentAdvice) && record.TreatmentAdvice.Length < 10)
    {
        qualityCheck.Issues.Add("治疗建议过于简单，建议详细说明");
    }
    
    qualityCheck.QualityScore = CalculateQualityScore(record);
    
    return qualityCheck;
}

private int CalculateQualityScore(RecordDetailDto record)
{
    int score = 0;
    
    // 基础信息完整性 (40分)
    if (!string.IsNullOrEmpty(record.ChiefComplaint)) score += 10;
    if (!string.IsNullOrEmpty(record.Diagnosis)) score += 10;
    if (!string.IsNullOrEmpty(record.PresentIllness)) score += 10;
    if (!string.IsNullOrEmpty(record.TreatmentAdvice)) score += 10;
    
    // 中医特色内容 (40分)
    if (record.DiagnosisResults?.Any() == true) score += 15;
    if (record.HerbalFormula?.Any() == true) score += 15;
    if (record.TreatmentPlans?.Any() == true) score += 10;
    
    // 内容详细程度 (20分)
    if (record.ChiefComplaint?.Length > 20) score += 5;
    if (record.PresentIllness?.Length > 50) score += 5;
    if (record.TreatmentAdvice?.Length > 30) score += 5;
    if (record.DiagnosisResults?.Count > 1) score += 5;
    
    return score;
}
```

## 业务扩展建议

### 功能增强

- **病历模板**: 支持常见疾病的病历模板，提高录入效率
- **智能提示**: 基于历史病历数据的智能诊断和用药提示
- **语音录入**: 支持语音转文字的病历录入方式
- **图片附件**: 支持舌象、脉象等图片的附件管理

### 协作功能

- **会诊支持**: 多医生协作的病历会诊功能
- **版本控制**: 病历修改的版本管理和回滚功能
- **评论系统**: 医生间对病历的评论和讨论功能
- **学习案例**: 优秀病历的案例库和学习功能

### 数据分析

- **疗效分析**: 基于病历数据的疗效统计和分析
- **用药分析**: 药材使用频次和配伍分析
- **诊断趋势**: 疾病诊断的时间趋势分析
- **质量控制**: 病历质量的自动评估和改进建议