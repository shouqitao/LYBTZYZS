# LYBT.Module.Pharmacy 功能说明文档

## 模块概述

药房模块是中医诊疗系统的药物调配核心，负责处方的调药、配药、发药等关键环节管理。本模块实现了从处方接收到药物发放的完整工作流程，支持药房工作人员的日常操作管理，确保药物调配的准确性、及时性和可追溯性。通过任务状态跟踪和操作记录，提供完整的药房作业管理体系。

## 业务价值

- **工作流程管理**: 规范化药房从接收处方到发放药物的完整流程
- **任务分配优化**: 支持药房工作任务的合理分配和进度跟踪
- **质量控制**: 通过操作记录和状态管理确保调药质量和安全
- **效率提升**: 减少人工错误，提高药房工作效率和准确性
- **库存关联**: 与药材库存系统联动，确保药物可用性
- **追溯管理**: 完整的药物调配记录便于质量追溯和责任界定

## 数据模型

### PharmacyModel (药房任务主实体)

**文件位置**: `LYBT.Module.Pharmacy/Models/PharmacyModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 药房任务ID（主键） | 自动生成，唯一标识 | 药房任务记录唯一标识 |
| TaskId | Guid | 治疗任务ID | 必填，关联治疗任务 | 建立与治疗流程的关联 |
| PrescriptionId | Guid | 处方ID | 必填，关联处方记录 | 确定需要调配的处方 |
| PatientId | Guid | 患者ID | 必填，关联患者信息 | 标识药物的目标患者 |
| Herbs | List&lt;HerbModel&gt; | 药材清单 | 必填，处方药材列表 | 具体需要调配的药材信息 |
| NeedDecoction | bool | 是否需要代煎 | 布尔值，默认false | 确定是否提供代煎服务 |
| Status | TreatmentTaskStatus | 任务状态 | 必填，枚举值 | 跟踪药房任务执行状态 |
| CreateTime | DateTime | 创建时间 | 必填，自动设置 | 任务创建时间记录 |
| DoctorId | Guid | 开方医生ID | 必填，关联医生信息 | 明确处方责任医生 |
| OperatorId | Guid | 药房操作员ID | 必填，关联操作人员 | 记录具体执行人员 |
| DispenseTime | DateTime | 调药时间 | 操作时设置 | 记录实际调药完成时间 |
| Remark | string? | 备注说明 | 可选，最大256字符 | 记录特殊情况和注意事项 |

### TreatmentTaskStatus (任务状态枚举)

**使用场景**: 跟踪药房任务的执行状态
**状态值说明**:

| 状态值 | 中文名称 | 说明 | 业务含义 |
|--------|----------|------|----------|
| Pending | 待处理 | 处方已接收，等待调药 | 药房任务队列中的初始状态 |
| InProgress | 进行中 | 正在调药配药过程中 | 药房人员正在执行调药任务 |
| Completed | 已完成 | 调药完成，等待患者取药 | 药品已准备好，可以发放 |
| Cancelled | 已取消 | 任务被取消或作废 | 因各种原因无法执行的任务 |

## DTO 数据传输对象

### PharmacyCreateDto (新增药房任务)

**使用场景**: 从处方系统接收新的调药任务
**特点**: 包含调药所需的基本信息和初始状态设置

```csharp
- PrescriptionId: 处方ID（必填，Guid类型）
- OperatorId: 药房操作员ID（必填，当前登录用户）
- DispenseTime: 抓药时间（可选，默认当前时间）
- Status: 药房状态（可选，默认为待处理）
- Remark: 备注说明（可选，特殊情况记录）
```

**验证规则**:
- 处方ID必须存在且有效
- 操作员ID必须是有效的药房工作人员
- 状态值必须在枚举范围内

### PharmacyDetailDto (药房任务详情)

**使用场景**: 查看完整的药房任务信息和关联数据
**特点**: 包含患者姓名、操作员姓名等关联信息

```csharp
- Id: 药房单ID
- PrescriptionId: 处方ID
- PatientName: 病人姓名（关联查询）
- OperatorName: 药房操作员姓名（关联查询）
- DispenseTime: 抓药时间
- Status: 药房状态（PharmacyStatus枚举）
- Remark: 备注说明
```

### PharmacyDto (药房任务列表)

**使用场景**: 药房任务列表展示和快速检索
**特点**: 精简信息，适合列表显示和状态查看

```csharp
- Id: 药房单ID
- PatientName: 病人姓名
- Status: 药房状态（整型表示）
- DispenseTime: 抓药时间
```

### PharmacyEditDto (编辑药房任务)

**使用场景**: 更新药房任务状态和操作信息
**特点**: 包含ID标识和可修改的关键字段

```csharp
- Id: 药房单ID（必填，用于定位）
- OperatorId: 药房操作员ID（必填，可更换操作人员）
- DispenseTime: 抓药时间（可修改）
- Status: 药房状态（可更新任务进度）
- Remark: 备注说明（可补充操作说明）
```

## 服务层 (IPharmacyService & PharmacyService)

### 基础任务管理方法

#### GetByIdAsync

```csharp
Task<PharmacyDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 获取指定药房任务的详细信息
**业务逻辑**: 
- 根据ID查询药房任务记录
- 包含关联的患者和操作员信息
- 使用AutoMapper进行实体到DTO转换
- 处理数据不存在的情况

**使用场景**: 任务详情查看、操作确认页面、异常处理

#### GetListAsync

```csharp
Task<List<PharmacyDto>> GetListAsync()
```

**功能**: 获取所有药房任务列表
**业务逻辑**: 
- 查询所有药房任务记录
- 按创建时间或状态排序
- 返回精简的列表信息

**使用场景**: 药房工作台、任务总览、管理监控

#### AddAsync

```csharp
Task<bool> AddAsync(PharmacyCreateDto pharmacyCreateDto)
```

**功能**: 创建新的药房调药任务
**业务逻辑**: 
- 验证处方ID的有效性
- 生成新的药房任务ID
- 设置初始状态为待处理
- 记录任务创建时间
- 建立与处方的关联关系

**特殊处理**:
- 自动提取处方中的药材信息
- 检查药材库存可用性
- 设置默认的调药时间
- 创建任务队列记录

**使用场景**: 处方系统推送、手动创建任务、批量任务生成

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(PharmacyEditDto pharmacyEditDto)
```

**功能**: 更新药房任务信息和状态
**业务逻辑**: 
- 验证任务的存在性
- 更新操作员和调药时间
- 修改任务状态和备注
- 记录操作历史

**特殊处理**:
- 状态变更的业务规则验证
- 操作权限检查
- 时间戳的更新管理
- 变更日志记录

**使用场景**: 任务状态更新、操作员变更、异常情况处理

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除指定的药房任务
**业务逻辑**: 
- 验证任务的存在性
- 检查任务状态是否允许删除
- 执行软删除或硬删除
- 记录删除操作日志

**安全考虑**:
- 只有待处理状态的任务可以删除
- 需要管理员权限确认
- 删除前的依赖关系检查
- 操作审计记录

**使用场景**: 错误任务清理、处方取消、系统维护

### 专业药房功能方法

#### GetWaitingListAsync

```csharp
Task<List<PharmacyDto>> GetWaitingListAsync()
```

**功能**: 获取所有待处理的调药任务
**业务逻辑**: 
- 查询状态为Pending的任务
- 按优先级和创建时间排序
- 返回药房工作队列

**特殊处理**:
- 优先级算法（如急诊、VIP患者）
- 任务分组（如普通药、特殊药材）
- 工作量评估

**使用场景**: 药房工作台、任务分配、工作量统计

#### MarkAsPreparedAsync

```csharp
Task<bool> MarkAsPreparedAsync(Guid id)
```

**功能**: 将指定任务标记为调药完成
**业务逻辑**: 
- 验证任务的存在性和当前状态
- 更新状态为已完成
- 记录完成时间
- 触发后续流程（如通知患者取药）

**特殊处理**:
- 状态转换验证
- 库存扣减处理
- 完成时间记录
- 后续流程触发

**使用场景**: 调药完成确认、状态流转、质量控制

## 仓储层 (IPharmacyRepository & PharmacyRepository)

### 基础数据操作

#### GetByIdAsync

```csharp
Task<PharmacyModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取药房任务实体
**实现细节**: 
- 使用EF Core的FindAsync方法
- 支持Include加载关联数据（药材、患者等）
- 处理数据不存在的情况

#### GetListAsync

```csharp
Task<List<PharmacyModel>> GetListAsync()
```

**功能**: 获取所有药房任务记录
**实现细节**: 
- 返回完整的任务列表
- 包含关联的药材和操作信息
- 支持排序和筛选扩展

#### AddAsync

```csharp
Task<bool> AddAsync(PharmacyModel pharmacyModel)
```

**功能**: 新增药房任务到数据库
**实现细节**: 
- 使用EF Core的Add方法
- 级联保存关联的药材数据
- 事务性操作确保数据一致性

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(PharmacyModel pharmacyModel)
```

**功能**: 更新药房任务信息
**实现细节**: 
- 使用EF Core的Update方法
- 处理实体状态跟踪
- 更新关联的子实体数据

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除药房任务记录
**实现细节**: 
- 先查询再删除的安全模式
- 级联删除关联数据
- 返回操作结果状态

### 专业查询方法

#### GetByStatusAsync

```csharp
Task<List<PharmacyModel>> GetByStatusAsync(TreatmentTaskStatus status)
```

**功能**: 根据状态查询药房任务列表
**实现细节**: 
- 使用状态过滤条件
- 支持多状态组合查询
- 结果按时间排序

**使用场景**: 工作队列查询、状态统计、任务分配

## 权限控制策略

### 操作权限

- **查看权限**: 药房工作人员可查看所有任务，医生可查看自己开具的处方任务
- **创建权限**: 系统自动创建或药房管理员手动创建
- **修改权限**: 药房工作人员可更新自己负责的任务状态
- **删除权限**: 需要药房主管权限，且只能删除待处理状态的任务

### 角色分工

- **药房主管**: 全部任务的查看、分配、管理权限
- **药房调剂师**: 负责药材调配和任务状态更新
- **药房发药员**: 负责药物发放和完成确认
- **系统管理员**: 系统维护和异常处理权限

### 数据安全

- **任务隔离**: 不同药房工作人员只能操作分配给自己的任务
- **状态控制**: 严格的状态转换规则，防止非法状态修改
- **操作审计**: 记录所有关键操作的执行者和时间

## 日志审计机制

### 操作日志

所有药房相关操作都会记录详细日志：

- **任务创建**: 记录处方来源、创建者、初始状态
- **状态变更**: 记录状态变更前后值、操作者、变更原因
- **任务完成**: 记录完成时间、操作者、质量检查结果
- **异常处理**: 记录异常情况、处理措施、责任人

### 质量日志

- **调药过程**: 记录调药的详细步骤和检查点
- **质量检查**: 记录药材质量检查和审核结果
- **发药确认**: 记录药物发放的确认和患者签收
- **退药处理**: 记录退药原因和处理流程

### 审计内容

- 操作时间和操作者信息
- 任务状态的完整变更历史
- 关键业务数据的变更记录
- 异常情况和处理结果

## 集成依赖

### 外部模块依赖

- **LYBT.Module.Prescriptions**: 处方数据来源和状态同步
- **LYBT.Module.Herbs**: 药材基础数据和库存信息
- **LYBT.Module.Patients**: 患者基础信息查询
- **LYBT.Module.Doctors**: 医生信息和处方验证
- **LYBT.Module.TreatmentRoom**: 治疗任务状态协调

### 基础服务依赖

- **IUnifiedLogService**: 统一日志服务
- **IMapper**: AutoMapper对象映射服务
- **PharmacyDbContext**: 专用数据库上下文
- **ICacheService**: 缓存服务（用于频繁查询的任务列表）

## 使用示例

### 创建药房调药任务

```csharp
var createDto = new PharmacyCreateDto
{
    PrescriptionId = prescriptionId,
    OperatorId = currentUserId,
    DispenseTime = DateTime.Now,
    Status = 0, // 待处理状态
    Remark = "急诊患者，优先处理"
};

var success = await pharmacyService.AddAsync(createDto);
if (success)
{
    logger.LogInformation("药房任务创建成功，处方ID: {PrescriptionId}", createDto.PrescriptionId);
}
```

### 查询待处理任务列表

```csharp
// 获取所有待处理任务
var waitingTasks = await pharmacyService.GetWaitingListAsync();

// 按优先级排序（急诊优先）
var prioritizedTasks = waitingTasks
    .OrderBy(t => t.Remark?.Contains("急诊") == true ? 0 : 1)
    .ThenBy(t => t.DispenseTime)
    .ToList();

foreach (var task in prioritizedTasks)
{
    Console.WriteLine($"患者: {task.PatientName}, 时间: {task.DispenseTime:HH:mm}");
}
```

### 更新任务状态

```csharp
var editDto = new PharmacyEditDto
{
    Id = taskId,
    OperatorId = currentUserId,
    DispenseTime = DateTime.Now,
    Status = TreatmentTaskStatus.InProgress,
    Remark = "开始调药，预计30分钟完成"
};

var updateSuccess = await pharmacyService.UpdateAsync(editDto);
if (updateSuccess)
{
    logger.LogInformation("任务状态更新为进行中，ID: {TaskId}", editDto.Id);
}
```

### 完成调药任务

```csharp
// 标记任务为已完成
var completeSuccess = await pharmacyService.MarkAsPreparedAsync(taskId);

if (completeSuccess)
{
    // 可以触发后续流程，如通知患者取药
    await NotifyPatientForPickupAsync(taskId);
    
    logger.LogInformation("调药任务完成，ID: {TaskId}", taskId);
}
```

### 药房工作台查询

```csharp
// 获取当前操作员的任务统计
public async Task<PharmacyWorkbenchDto> GetWorkbenchDataAsync(Guid operatorId)
{
    var allTasks = await pharmacyService.GetListAsync();
    var myTasks = allTasks.Where(t => t.OperatorId == operatorId).ToList();
    
    return new PharmacyWorkbenchDto
    {
        TotalTasks = myTasks.Count,
        PendingTasks = myTasks.Count(t => t.Status == 0),
        InProgressTasks = myTasks.Count(t => t.Status == 1),
        CompletedTasks = myTasks.Count(t => t.Status == 2),
        TodayCompletedCount = myTasks.Count(t => 
            t.Status == 2 && t.DispenseTime.Date == DateTime.Today)
    };
}
```

### 质量控制检查

```csharp
// 任务完成前的质量检查
public async Task<bool> QualityCheckAsync(Guid taskId)
{
    var taskDetail = await pharmacyService.GetByIdAsync(taskId);
    if (taskDetail == null) return false;
    
    // 检查药材准备完整性
    var prescription = await prescriptionService.GetByIdAsync(taskDetail.PrescriptionId);
    var requiredHerbs = prescription.HerbItems;
    var preparedHerbs = await GetPreparedHerbsAsync(taskId);
    
    // 验证所有药材都已准备
    var allPrepared = requiredHerbs.All(required => 
        preparedHerbs.Any(prepared => 
            prepared.HerbId == required.HerbId && 
            prepared.Amount >= required.Amount));
    
    if (allPrepared)
    {
        logger.LogInformation("质量检查通过，任务ID: {TaskId}", taskId);
        return true;
    }
    else
    {
        logger.LogWarning("质量检查不通过，任务ID: {TaskId}", taskId);
        return false;
    }
}
```

### 药房效率统计

```csharp
// 药房工作效率统计
public async Task<PharmacyEfficiencyStatsDto> GetEfficiencyStatsAsync(DateTime startDate, DateTime endDate)
{
    var allTasks = await pharmacyService.GetListAsync();
    var periodTasks = allTasks.Where(t => 
        t.CreateTime >= startDate && t.CreateTime <= endDate).ToList();
    
    var completedTasks = periodTasks.Where(t => t.Status == 2).ToList();
    
    return new PharmacyEfficiencyStatsDto
    {
        TotalTasks = periodTasks.Count,
        CompletedTasks = completedTasks.Count,
        AverageProcessingTime = completedTasks.Any() ? 
            completedTasks.Average(t => (t.DispenseTime - t.CreateTime).TotalMinutes) : 0,
        CompletionRate = periodTasks.Count > 0 ? 
            (double)completedTasks.Count / periodTasks.Count * 100 : 0
    };
}
```

## 业务扩展建议

### 功能增强

- **智能分配**: 基于工作负载和专长的任务智能分配系统
- **预计时间**: 基于历史数据的调药时间预估功能
- **批量处理**: 支持相似处方的批量调药处理
- **移动端支持**: 药房人员移动端应用，提高操作便利性

### 质量管理

- **视频监控**: 关键调药过程的视频记录和回放
- **条码扫描**: 药材条码扫描确保准确性
- **双人复核**: 重要药材的双人复核机制
- **质量追溯**: 完整的质量问题追溯体系

### 效率优化

- **工作流优化**: 基于数据分析的工作流程持续优化
- **库存预警**: 与库存系统联动的智能预警机制
- **设备集成**: 与自动化调药设备的系统集成
- **数据驱动**: 基于操作数据的效率提升建议