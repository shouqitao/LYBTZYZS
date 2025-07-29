# LYBT.Module.TreatmentRoom 功能说明文档

## 模块概述

治疗室模块是中医诊疗系统的治疗执行管理核心，负责各种中医治疗项目的执行管理和跟踪。本模块主要管理针灸、推拿、拔罐、艾灸等非药物治疗项目的执行过程，包括治疗计划制定、执行进度跟踪、治疗效果记录等功能，为中医综合治疗提供完整的管理支持。

## 业务价值

- **治疗规范化**: 标准化治疗项目的执行流程和管理
- **进度跟踪**: 实时跟踪治疗计划的执行进度和完成情况
- **资源调度**: 优化治疗师和治疗室的资源分配
- **质量控制**: 确保治疗项目的执行质量和安全性
- **数据记录**: 完整记录治疗过程和效果，支持疗效分析
- **协同管理**: 与诊疗、药房等模块协同，提供综合治疗方案

## 数据模型

### TreatmentRoomModel (治疗室任务实体)

**文件位置**: `LYBT.Module.TreatmentRoom/Models/TreatmentRoomModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 主键ID | 自动生成，唯一标识 | 治疗任务唯一标识 |
| ExecutionId | string | 执行计划ID | 必填，最大64字符 | 关联具体的执行计划 |
| PlanId | string | 治疗方案ID | 必填，最大64字符 | 关联整体治疗方案 |
| PatientId | string | 患者ID | 必填，最大64字符 | 标识接受治疗的患者 |
| TreatmentType | string | 治疗类型 | 必填，最大32字符 | 区分不同的治疗类型 |
| ExecutedCount | int | 已执行次数 | 非负整数 | 记录已完成的治疗次数 |
| TotalCount | int | 总治疗次数 | 正整数 | 计划的总治疗次数 |
| Status | string | 任务状态 | 必填，最大32字符 | 跟踪治疗任务的执行状态 |
| Executor | string | 执行者 | 可选，最大64字符 | 记录实际执行治疗的人员 |
| LastExecuteTime | DateTime | 最后执行时间 | 必填 | 记录最近一次治疗的时间 |
| DoctorId | string | 医生ID | 可选，最大64字符 | 制定治疗方案的医生 |
| StartTime | DateTime | 开始时间 | 必填 | 治疗计划的开始时间 |
| TreatmentItem | string | 治疗项目 | 可选，最大64字符 | 具体的治疗项目名称 |
| EndTime | DateTime | 结束时间 | 必填 | 治疗计划的结束时间 |
| Remark | string? | 备注信息 | 可选，最大256字符 | 记录特殊情况和注意事项 |
| Count | int | 治疗次数 | 非负整数 | 当前安排的治疗次数 |

### 治疗状态定义

| 状态值 | 中文名称 | 说明 | 业务含义 |
|--------|----------|------|----------|
| 0 | 待治疗 | 治疗任务已安排，等待执行 | 初始状态，等待治疗师开始执行 |
| 1 | 治疗中 | 治疗正在进行中 | 治疗师正在执行治疗项目 |
| 2 | 已完成 | 治疗任务已完成 | 所有计划的治疗次数已执行完毕 |
| 3 | 已暂停 | 治疗暂时中断 | 因特殊情况暂停，可恢复执行 |
| -1 | 已取消 | 治疗任务被取消 | 因各种原因取消治疗计划 |

### 治疗类型分类

| 治疗类型 | 说明 | 特点 | 常见项目 |
|----------|------|------|----------|
| 针灸治疗 | 使用针具进行的治疗 | 需要专业针灸师，时间相对固定 | 毫针、电针、耳针 |
| 推拿治疗 | 手法推拿按摩治疗 | 需要专业推拿师，时间灵活 | 全身推拿、局部按摩 |
| 理疗治疗 | 物理因子治疗 | 使用设备，可批量治疗 | 艾灸、拔罐、红外线 |
| 康复治疗 | 功能恢复训练 | 需要康复师指导，周期较长 | 功能训练、器械训练 |

## DTO 数据传输对象

### TreatmentRoomCreateDto (新增治疗任务)

**使用场景**: 医生开具治疗处方后创建治疗任务
**特点**: 包含治疗的基本信息和初始设置

```csharp
- PatientId: 病人ID（必填，string类型）
- PatientName: 病人姓名（便于显示）
- DoctorId: 医生ID（必填，开具处方的医生）
- TreatmentItem: 诊疗项目（必填，具体治疗项目）
- Count: 治疗次数（必填，范围1到最大值）
- Status: 治疗状态（可选，默认0待治疗）
- StartTime: 治疗开始时间（可选，默认当前时间）
- Remark: 备注信息（可选，特殊说明）
```

**验证规则**:
- 患者ID和医生ID必须存在且有效
- 治疗次数必须大于0
- 治疗项目必须在系统支持的项目范围内
- 开始时间不能早于当前时间

### TreatmentRoomDetailDto (治疗任务详情)

**使用场景**: 查看完整的治疗任务信息，包含关联的患者和医生信息
**特点**: 包含所有治疗相关的详细信息

```csharp
- Id: 治疗室单ID
- PatientId: 病人ID
- PatientName: 病人姓名（关联查询）
- DoctorId: 医生ID
- DoctorName: 医生姓名（关联查询）
- TreatmentItem: 诊疗项目
- Count: 治疗次数
- Status: 治疗状态
- StartTime: 治疗开始时间
- EndTime: 治疗结束时间（可选）
- Remark: 备注信息
```

### TreatmentRoomDto (治疗任务列表)

**使用场景**: 治疗任务列表展示和快速浏览
**特点**: 精简信息，适合列表显示和状态筛选

### TreatmentRoomEditDto (编辑治疗任务)

**使用场景**: 更新治疗任务的执行状态和信息
**特点**: 包含ID标识和可修改的关键字段

```csharp
- Id: 治疗室单ID（必填）
- TreatmentItem: 诊疗项目（可修改）
- Count: 治疗次数（可调整）
- Status: 治疗状态（可更新）
- EndTime: 治疗结束时间（完成时设置）
- Remark: 备注信息（可补充）
```

## 服务层 (ITreatmentRoomService & TreatmentRoomService)

### 基础治疗管理方法

#### GetByIdAsync

```csharp
Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 获取指定治疗任务的详细信息
**业务逻辑**: 
- 根据ID查询治疗任务记录
- 包含患者和医生的关联信息
- 使用AutoMapper进行实体到DTO转换
- 处理数据不存在的情况

**使用场景**: 治疗详情查看、执行前信息确认、进度跟踪

#### GetListAsync

```csharp
Task<List<TreatmentRoomDto>> GetListAsync()
```

**功能**: 获取所有治疗任务列表
**业务逻辑**: 
- 查询所有治疗任务记录
- 按开始时间或优先级排序
- 返回治疗任务的基本信息

**使用场景**: 治疗工作台、任务总览、资源调度

#### AddAsync

```csharp
Task<bool> AddAsync(TreatmentRoomCreateDto treatmentRoomCreateDto)
```

**功能**: 创建新的治疗任务
**业务逻辑**: 
- 验证输入数据的完整性和有效性
- 生成新的治疗任务ID
- 设置治疗开始时间
- 建立与患者和医生的关联
- 初始化任务状态

**特殊处理**:
- 自动生成GUID格式的任务ID
- 设置开始时间为当前时间
- 验证治疗项目的可执行性
- 检查患者的治疗冲突

**使用场景**: 医生开具治疗处方、治疗计划制定、批量任务创建

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(TreatmentRoomEditDto treatmentRoomEditDto)
```

**功能**: 更新治疗任务信息和状态
**业务逻辑**: 
- 验证治疗任务的存在性
- 更新治疗项目和次数
- 修改任务状态和完成时间
- 更新备注信息
- 记录执行历史

**特殊处理**:
- 状态转换的业务规则验证
- 完成时间的自动设置
- 治疗进度的计算更新
- 任务变更历史记录

**使用场景**: 治疗执行更新、状态变更、信息修正

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除指定的治疗任务
**业务逻辑**: 
- 验证治疗任务的存在性
- 检查任务是否可以删除
- 执行删除操作
- 记录删除日志

**安全考虑**:
- 正在执行的任务不能删除
- 已完成的任务谨慎删除
- 需要相应权限验证
- 删除前确认机制

**使用场景**: 错误任务清理、计划调整、系统维护

### 专业治疗功能方法

#### GetByStatusAsync

```csharp
Task<List<TreatmentRoomDto>> GetByStatusAsync(string status)
```

**功能**: 根据状态查询治疗任务列表
**业务逻辑**: 
- 使用状态过滤条件查询
- 支持多状态组合查询
- 按时间或优先级排序
- 返回筛选后的任务列表

**使用场景**: 待治疗任务查询、已完成任务统计、任务进度监控

## 仓储层 (ITreatmentRoomRepository & TreatmentRoomRepository)

### 基础数据操作

#### GetByIdAsync

```csharp
Task<TreatmentRoomModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取治疗任务实体
**实现细节**: 
- 使用EF Core的FindAsync方法
- 简单高效的单记录查询
- 处理数据不存在的情况

#### GetListAsync

```csharp
Task<List<TreatmentRoomModel>> GetListAsync()
```

**功能**: 获取所有治疗任务记录
**实现细节**: 
- 返回完整的任务列表
- 适合管理和统计查询
- 可扩展为支持分页

#### AddAsync

```csharp
Task<bool> AddAsync(TreatmentRoomModel treatmentRoomModel)
```

**功能**: 新增治疗任务到数据库
**实现细节**: 
- 使用EF Core的Add方法
- 单个实体插入操作
- 返回操作成功状态

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(TreatmentRoomModel treatmentRoomModel)
```

**功能**: 更新治疗任务记录
**实现细节**: 
- 使用EF Core的Update方法
- 全量字段更新策略
- 自动处理实体状态跟踪

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除治疗任务记录
**实现细节**: 
- 先查询再删除的安全模式
- 物理删除策略
- 返回操作结果状态

### 专业查询方法

#### GetByStatusAsync

```csharp
Task<List<TreatmentRoomModel>> GetByStatusAsync(string status)
```

**功能**: 根据状态查询治疗任务
**实现细节**: 
- 使用Where条件过滤
- 高效的状态筛选查询
- 支持状态值的精确匹配

## 权限控制策略

### 操作权限

- **查看权限**: 治疗师可查看分配给自己的任务，医生可查看自己开具的治疗，管理员可查看所有任务
- **创建权限**: 医生可创建治疗任务，护士可协助创建
- **执行权限**: 只有具备相应资质的治疗师可执行对应的治疗项目
- **修改权限**: 治疗师可更新执行状态，医生可调整治疗方案

### 资质管理

- **针灸师资质**: 只有针灸师可执行针灸相关治疗
- **推拿师资质**: 只有推拿师可执行推拿相关治疗
- **理疗师资质**: 具备理疗资质的人员可执行理疗项目
- **医生监督**: 重要治疗项目需要医生监督执行

### 安全控制

- **患者安全**: 确保治疗的安全性和适宜性
- **设备安全**: 治疗设备的安全使用和维护
- **环境安全**: 治疗环境的卫生和安全要求

## 日志审计机制

### 执行日志

所有治疗相关操作都会记录详细日志：

- **任务创建**: 记录治疗任务的创建者、时间、内容
- **执行记录**: 记录每次治疗的执行者、时间、效果
- **状态变更**: 记录任务状态的变更历史和原因
- **异常情况**: 记录治疗过程中的异常和处理

### 质量日志

- **治疗效果**: 记录治疗效果评估和患者反馈
- **执行质量**: 记录治疗执行的质量检查结果
- **安全事件**: 记录治疗过程中的安全事件
- **设备使用**: 记录治疗设备的使用和维护情况

### 审计内容

- 操作时间和操作者信息
- 治疗任务的完整执行历史
- 关键数据的变更记录
- 异常情况和处理过程

## 集成依赖

### 外部模块依赖

- **LYBT.Module.DiagnosisTreatment**: 治疗方案来源和关联
- **LYBT.Module.Patients**: 患者基础信息查询
- **LYBT.Module.Doctors**: 医生信息和权限验证
- **LYBT.Module.Registration**: 挂号信息关联
- **LYBT.Module.Queueing**: 治疗队列管理

### 基础服务依赖

- **IMapper**: AutoMapper对象映射服务
- **TreatmentRoomDbContext**: 专用数据库上下文
- **IAuthorizationService**: 权限验证服务
- **INotificationService**: 通知服务（治疗提醒）

## 使用示例

### 创建治疗任务

```csharp
var createDto = new TreatmentRoomCreateDto
{
    PatientId = "P123456",
    PatientName = "张三",
    DoctorId = "D789012",
    TreatmentItem = "针灸治疗",
    Count = 10,
    Status = 0, // 待治疗
    StartTime = DateTime.Now,
    Remark = "腰痛针灸治疗，每日一次"
};

var success = await treatmentService.AddAsync(createDto);
if (success)
{
    logger.LogInformation("治疗任务创建成功，患者: {PatientName}, 项目: {TreatmentItem}", 
        createDto.PatientName, createDto.TreatmentItem);
}
```

### 查询待治疗任务

```csharp
// 获取所有待治疗的任务
var pendingTasks = await treatmentService.GetByStatusAsync("0");

// 按治疗类型分组
var tasksByType = pendingTasks
    .GroupBy(t => t.TreatmentItem)
    .ToList();

foreach (var group in tasksByType)
{
    Console.WriteLine($"=== {group.Key} ===");
    foreach (var task in group.OrderBy(t => t.StartTime))
    {
        Console.WriteLine($"患者: {task.PatientName}, 次数: {task.Count}, 时间: {task.StartTime:HH:mm}");
    }
}
```

### 执行治疗并更新状态

```csharp
// 开始治疗
public async Task<bool> StartTreatmentAsync(Guid taskId, string executorId)
{
    var task = await treatmentService.GetByIdAsync(taskId);
    if (task == null || task.Status != 0) // 不是待治疗状态
    {
        return false;
    }
    
    var editDto = new TreatmentRoomEditDto
    {
        Id = taskId,
        TreatmentItem = task.TreatmentItem,
        Count = task.Count,
        Status = 1, // 治疗中
        EndTime = null, // 尚未结束
        Remark = $"{task.Remark} - 开始执行，执行者: {executorId}"
    };
    
    var success = await treatmentService.UpdateAsync(editDto);
    if (success)
    {
        logger.LogInformation("开始治疗，任务ID: {TaskId}, 执行者: {ExecutorId}", taskId, executorId);
    }
    
    return success;
}

// 完成治疗
public async Task<bool> CompleteTreatmentAsync(Guid taskId, string effect)
{
    var editDto = new TreatmentRoomEditDto
    {
        Id = taskId,
        Status = 2, // 已完成
        EndTime = DateTime.Now,
        Remark = $"治疗完成，效果: {effect}"
    };
    
    return await treatmentService.UpdateAsync(editDto);
}
```

### 治疗进度统计

```csharp
// 获取患者的治疗进度
public async Task<PatientTreatmentProgressDto> GetPatientProgressAsync(string patientId)
{
    var allTasks = await treatmentService.GetListAsync();
    var patientTasks = allTasks.Where(t => t.PatientId == patientId).ToList();
    
    return new PatientTreatmentProgressDto
    {
        PatientId = patientId,
        TotalTasks = patientTasks.Count,
        CompletedTasks = patientTasks.Count(t => t.Status == 2),
        PendingTasks = patientTasks.Count(t => t.Status == 0),
        InProgressTasks = patientTasks.Count(t => t.Status == 1),
        TreatmentTypes = patientTasks
            .GroupBy(t => t.TreatmentItem)
            .Select(g => new TreatmentTypeProgressDto
            {
                TypeName = g.Key,
                TotalCount = g.Sum(t => t.Count),
                CompletedCount = g.Where(t => t.Status == 2).Sum(t => t.Count)
            }).ToList()
    };
}
```

### 治疗师工作安排

```csharp
// 获取治疗师的工作任务
public async Task<List<TreatmentRoomDto>> GetTherapistTasksAsync(string therapistId, DateTime date)
{
    var allTasks = await treatmentService.GetListAsync();
    
    // 筛选指定日期和治疗师的任务
    var therapistTasks = allTasks.Where(t => 
        t.Executor == therapistId && 
        t.StartTime.Date == date.Date)
        .OrderBy(t => t.StartTime)
        .ToList();
    
    return therapistTasks;
}

// 分配治疗师
public async Task<bool> AssignTherapistAsync(Guid taskId, string therapistId)
{
    var task = await treatmentService.GetByIdAsync(taskId);
    if (task == null) return false;
    
    var editDto = new TreatmentRoomEditDto
    {
        Id = taskId,
        TreatmentItem = task.TreatmentItem,
        Count = task.Count,
        Status = task.Status,
        EndTime = task.EndTime,
        Remark = $"{task.Remark} - 分配治疗师: {therapistId}"
    };
    
    return await treatmentService.UpdateAsync(editDto);
}
```

### 治疗效果跟踪

```csharp
// 记录治疗效果
public async Task<bool> RecordTreatmentEffectAsync(Guid taskId, TreatmentEffectDto effect)
{
    var task = await treatmentService.GetByIdAsync(taskId);
    if (task == null) return false;
    
    var effectRecord = new
    {
        TaskId = taskId,
        PatientId = task.PatientId,
        TreatmentItem = task.TreatmentItem,
        EffectScore = effect.Score, // 1-10分
        PatientFeedback = effect.Feedback,
        TherapistObservation = effect.Observation,
        RecordTime = DateTime.Now
    };
    
    // 更新任务备注，记录效果
    var editDto = new TreatmentRoomEditDto
    {
        Id = taskId,
        TreatmentItem = task.TreatmentItem,
        Count = task.Count,
        Status = task.Status,
        EndTime = task.EndTime,
        Remark = $"{task.Remark} - 效果评分: {effect.Score}/10, {effect.Feedback}"
    };
    
    return await treatmentService.UpdateAsync(editDto);
}
```

### 治疗资源管理

```csharp
// 获取治疗室使用情况
public async Task<TreatmentRoomUsageStatsDto> GetRoomUsageStatsAsync(DateTime date)
{
    var allTasks = await treatmentService.GetListAsync();
    var dayTasks = allTasks.Where(t => t.StartTime.Date == date.Date).ToList();
    
    return new TreatmentRoomUsageStatsDto
    {
        Date = date,
        TotalTasks = dayTasks.Count,
        CompletedTasks = dayTasks.Count(t => t.Status == 2),
        AcupunctureCount = dayTasks.Count(t => t.TreatmentItem.Contains("针灸")),
        MassageCount = dayTasks.Count(t => t.TreatmentItem.Contains("推拿")),
        PhysiotherapyCount = dayTasks.Count(t => t.TreatmentItem.Contains("理疗")),
        BusiestHour = dayTasks
            .GroupBy(t => t.StartTime.Hour)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? 0
    };
}
```

## 业务扩展建议

### 功能增强

- **智能调度**: 基于治疗师技能和设备可用性的智能任务分配
- **预约管理**: 支持患者预约特定时间的治疗项目
- **设备管理**: 治疗设备的使用、维护和预约管理
- **移动应用**: 治疗师移动端应用，便于实时更新状态

### 质量管理

- **标准化流程**: 建立各类治疗项目的标准化执行流程
- **质量评估**: 治疗质量的量化评估和持续改进
- **技能培训**: 基于执行数据的治疗师技能培训管理
- **安全监控**: 治疗安全事件的监控和预警系统

### 数据分析

- **疗效分析**: 基于治疗数据的疗效统计和分析
- **资源优化**: 治疗资源配置的优化建议
- **成本分析**: 治疗成本的精确计算和控制
- **患者满意度**: 治疗满意度调查和分析系统