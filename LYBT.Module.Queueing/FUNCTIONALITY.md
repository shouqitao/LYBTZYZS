# LYBT.Module.Queueing 功能说明文档

## 模块概述

排队模块是中医诊疗系统的患者就诊流程管理核心，负责患者从挂号到就诊的排队叫号全流程管理。本模块实现了智能排队系统，支持不同类型的排队管理（普通、急诊、专家等），提供实时的排队状态跟踪、叫号管理和就诊流程控制，显著提升医院的就诊效率和患者体验。

## 业务价值

- **流程规范化**: 标准化患者就诊排队流程，减少混乱和争议
- **效率提升**: 通过系统化管理提高医生工作效率和患者就诊体验
- **公平就医**: 确保患者按序就诊，维护医疗服务的公平性
- **实时管控**: 提供实时的排队状态监控和异常处理能力
- **数据分析**: 为医院运营管理提供排队和就诊数据支持
- **智能调度**: 支持不同优先级和类型的排队策略优化

## 数据模型

### QueueingModel (排队主实体)

**文件位置**: `LYBT.Module.Queueing/Models/QueueingModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 排队ID（主键） | 自动生成，唯一标识 | 排队记录唯一标识 |
| PatientId | Guid | 患者ID | 必填，关联患者表 | 标识排队的患者 |
| PatientName | string | 患者姓名 | 必填，快速展示用 | 冗余存储便于显示和叫号 |
| DoctorId | Guid | 医生ID | 必填，关联医生表 | 标识患者要就诊的医生 |
| DoctorName | string | 医生姓名 | 必填，快速展示用 | 冗余存储便于排队管理 |
| QueueType | string | 排队类型 | 必填，默认"普通" | 区分普通、急诊、专家等排队类型 |
| QueueTime | DateTime | 排队时间 | 必填，自动设置 | 记录患者进入排队的时间 |
| Status | QueueStatus | 当前状态 | 必填，枚举值 | 跟踪排队的实时状态 |
| Remark | string? | 备注信息 | 可选，最大500字符 | 记录特殊情况和注意事项 |

### QueueStatus (排队状态枚举)

**文件位置**: `LYBT.Common/Enums/Diagnostics/DiagnosticEnums.cs`

**状态说明**:

| 状态值 | 中文名称 | 数值 | 说明 | 业务含义 |
|--------|----------|------|------|----------|
| Waiting | 排队中 | 1 | 患者正在排队等待 | 初始状态，等待医生叫号 |
| InProgress | 就诊中 | 2 | 患者正在就诊 | 医生已叫号，患者正在接受诊疗 |
| Completed | 已完成 | 3 | 就诊完成 | 诊疗结束，患者离开诊室 |
| Skipped | 已跳过 | 4 | 暂时跳过 | 患者暂时不在，跳过继续下一位 |
| Cancelled | 已取消 | -1 | 取消排队 | 患者主动取消或系统取消排队 |

### QueueType (排队类型定义)

**常用类型**:

| 类型名称 | 说明 | 优先级 | 特殊处理 |
|----------|------|--------|----------|
| 普通 | 普通门诊排队 | 正常 | 按时间顺序排队 |
| 急诊 | 急诊患者排队 | 高 | 优先处理，插队到前面 |
| 专家 | 专家门诊排队 | 正常 | 可能有特殊的时间安排 |
| 复诊 | 复诊患者排队 | 中等 | 可能有预约时间限制 |
| VIP | VIP患者排队 | 高 | 特殊服务流程 |

## DTO 数据传输对象

### QueueingCreateDto (新增排队)

**使用场景**: 患者挂号后自动创建或手动加入排队
**特点**: 包含排队所需的基本信息和初始设置

```csharp
- PatientId: 病人ID（必填，string类型）
- DoctorId: 医生ID（必填，string类型）
- QueueType: 排队类型（必填，默认"普通"）
- QueueTime: 排队时间（可选，默认当前时间）
- Remark: 备注说明（可选，特殊情况记录）
```

**验证规则**:
- 患者ID和医生ID必须存在且有效
- 排队类型必须在系统定义的类型范围内
- 同一患者不能在同一医生处重复排队

### QueueingDetailDto (排队详情)

**使用场景**: 查看完整的排队信息和状态
**特点**: 包含所有关联信息，便于管理和监控

```csharp
- Id: 排队ID
- PatientId: 病人ID
- PatientName: 病人姓名
- DoctorId: 医生ID
- DoctorName: 医生姓名
- QueueType: 排队类型
- QueueTime: 排队时间
- Status: 当前状态（字符串表示）
- Remark: 备注信息
```

### QueueingDto (排队列表)

**使用场景**: 排队列表展示和快速浏览
**特点**: 精简信息，适合列表显示和实时更新

### QueueingEditDto (编辑排队)

**使用场景**: 修改排队信息或处理特殊情况
**特点**: 包含ID标识和可修改的字段

```csharp
- Id: 排队ID（必填，用于定位）
- QueueType: 排队类型（可修改）
- Remark: 备注信息（可补充或修改）
```

## 服务层 (IQueueingService & QueueingService)

### 基础排队管理方法

#### GetByIdAsync

```csharp
Task<QueueingDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 获取指定排队记录的详细信息
**业务逻辑**: 
- 根据ID查询排队记录
- 包含患者和医生的关联信息
- 使用AutoMapper进行实体到DTO转换
- 处理数据不存在的情况

**使用场景**: 排队详情查看、状态确认、异常处理

#### GetListAsync

```csharp
Task<List<QueueingDto>> GetListAsync()
```

**功能**: 获取排队记录列表
**业务逻辑**: 
- 查询所有排队记录
- 按排队时间和优先级排序
- 返回当前活跃的排队信息

**使用场景**: 排队大屏显示、医生工作台、管理监控

#### AddAsync

```csharp
Task<bool> AddAsync(QueueingCreateDto dto)
```

**功能**: 创建新的排队记录
**业务逻辑**: 
- 验证患者和医生信息的有效性
- 检查是否存在重复排队
- 生成新的排队ID
- 设置初始状态为排队中
- 记录排队时间

**特殊处理**:
- 急诊患者的优先级处理
- 重复排队检查和提示
- 排队号码的自动分配
- 关联挂号信息的验证

**使用场景**: 患者挂号后排队、手动加入排队、批量排队处理

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(QueueingEditDto dto)
```

**功能**: 更新排队信息和属性
**业务逻辑**: 
- 验证排队记录的存在性
- 更新排队类型和备注信息
- 保持核心信息（患者、医生）不变
- 记录修改历史

**特殊处理**:
- 排队类型变更的影响评估
- 优先级调整的处理
- 状态一致性检查

**使用场景**: 排队类型调整、特殊情况处理、信息修正

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除排队记录
**业务逻辑**: 
- 验证排队记录的存在性
- 检查是否允许删除（状态限制）
- 执行物理删除
- 记录删除操作日志

**安全考虑**:
- 只有特定状态下才能删除
- 需要相应权限验证
- 删除前的确认机制
- 操作审计记录

**使用场景**: 错误记录清理、系统维护、异常处理

### 专业排队功能方法

#### CancelAsync

```csharp
Task<bool> CancelAsync(Guid id)
```

**功能**: 取消患者排队
**业务逻辑**: 
- 验证排队记录的存在性
- 检查当前状态是否允许取消
- 更新状态为已取消
- 处理排队位置的重新排列

**特殊处理**:
- 状态转换验证
- 后续排队的位置调整
- 取消原因记录
- 关联业务的通知

**使用场景**: 患者主动取消、医生暂停接诊、系统自动取消

#### CompleteAsync

```csharp
Task<bool> CompleteAsync(Guid id)
```

**功能**: 标记排队为完成状态
**业务逻辑**: 
- 验证排队记录的存在性
- 检查当前状态是否为就诊中
- 更新状态为已完成
- 触发下一位患者的通知

**特殊处理**:
- 状态转换验证
- 就诊时间的记录
- 下一位患者的自动处理
- 医生状态的更新

**使用场景**: 医生完成诊疗、系统自动完成、管理员操作

#### HoldAsync

```csharp
Task<bool> HoldAsync(Guid id)
```

**功能**: 暂时跳过当前患者
**业务逻辑**: 
- 验证排队记录的存在性
- 更新状态为已跳过
- 调整排队顺序
- 记录跳过原因

**特殊处理**:
- 跳过原因的记录
- 重新排队的时机
- 跳过次数的限制
- 自动恢复机制

**使用场景**: 患者暂时不在、检查未完成、特殊情况处理

## 仓储层 (IQueueingRepository & QueueingRepository)

### 基础数据操作

#### GetByIdAsync

```csharp
Task<QueueingModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取排队实体
**实现细节**: 
- 使用EF Core的FindAsync方法
- 简单快速的单记录查询
- 处理数据不存在的情况

#### GetListAsync

```csharp
Task<List<QueueingModel>> GetListAsync()
```

**功能**: 获取所有排队记录
**实现细节**: 
- 返回完整的排队列表
- 适合小规模数据的全量查询
- 可扩展为支持分页和筛选

#### AddAsync

```csharp
Task<bool> AddAsync(QueueingModel model)
```

**功能**: 新增排队记录到数据库
**实现细节**: 
- 使用EF Core的Add方法
- 单个实体的插入操作
- 返回操作成功状态

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(QueueingModel model)
```

**功能**: 更新排队记录
**实现细节**: 
- 使用EF Core的Update方法
- 全量字段更新策略
- 自动处理实体状态跟踪

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除排队记录
**实现细节**: 
- 先查询再删除的安全模式
- 物理删除策略
- 返回操作结果状态

### 状态管理操作

#### CancelAsync

```csharp
Task<bool> CancelAsync(Guid id)
```

**功能**: 取消排队状态更新
**实现细节**: 
- 查询现有记录
- 更新状态为Cancelled
- 保存变更到数据库

#### CompleteAsync

```csharp
Task<bool> CompleteAsync(Guid id)
```

**功能**: 完成排队状态更新
**实现细节**: 
- 查询现有记录
- 更新状态为Completed
- 事务性状态更新

#### HoldAsync

```csharp
Task<bool> HoldAsync(Guid id)
```

**功能**: 跳过排队状态更新
**实现细节**: 
- 查询现有记录
- 更新状态为Skipped
- 保持数据一致性

## 权限控制策略

### 操作权限

- **查看权限**: 医生可查看自己的排队列表，护士可查看所有排队，患者可查看自己的排队状态
- **创建权限**: 挂号员和系统自动创建，医生可手动加入排队
- **修改权限**: 医生和护士可修改排队信息，患者可取消自己的排队
- **删除权限**: 需要管理员权限，仅限异常情况处理

### 角色分工

- **医生**: 叫号、完成诊疗、跳过患者、查看自己的排队列表
- **护士**: 管理排队秩序、调整排队顺序、处理异常情况
- **挂号员**: 创建排队记录、患者咨询服务
- **管理员**: 系统维护、数据管理、异常处理

### 业务规则

- **排队限制**: 同一患者不能在同一医生处重复排队
- **状态转换**: 严格的状态转换规则，防止非法状态变更
- **时间限制**: 排队有效期限制，超时自动取消

## 日志审计机制

### 操作日志

所有排队相关操作都会记录详细日志：

- **排队创建**: 记录患者、医生、时间、类型等信息
- **状态变更**: 记录状态变更前后值、操作者、变更时间
- **叫号记录**: 记录医生叫号时间、患者响应情况
- **异常处理**: 记录跳过、取消的原因和处理者

### 业务日志

- **排队统计**: 记录每日的排队人数、等待时间、完成情况
- **效率分析**: 记录医生的平均诊疗时间、排队处理效率
- **异常统计**: 记录跳过、取消的频次和原因分析
- **患者体验**: 记录患者等待时间和满意度相关数据

### 审计内容

- 操作时间和操作者信息
- 排队状态的完整变更历史
- 关键业务数据的变更记录
- 异常情况和处理过程

## 集成依赖

### 外部模块依赖

- **LYBT.Module.Registration**: 挂号信息来源和状态同步
- **LYBT.Module.Patients**: 患者基础信息查询和验证
- **LYBT.Module.Doctors**: 医生信息查询和工作状态
- **LYBT.Module.TreatmentRoom**: 诊室状态和治疗安排
- **LYBT.Module.DiagnosisTreatment**: 诊疗流程的状态协调

### 基础服务依赖

- **IUnifiedLogService**: 统一日志服务
- **IMapper**: AutoMapper对象映射服务
- **QueueingDbContext**: 专用数据库上下文
- **ISignalRService**: 实时通信服务（用于叫号通知）

## 使用示例

### 创建排队记录

```csharp
var createDto = new QueueingCreateDto
{
    PatientId = "P123456",
    DoctorId = "D789012",
    QueueType = "急诊",
    QueueTime = DateTime.Now,
    Remark = "患者发热，优先处理"
};

var success = await queueingService.AddAsync(createDto);
if (success)
{
    logger.LogInformation("急诊患者排队创建成功，患者ID: {PatientId}", createDto.PatientId);
}
```

### 获取医生的排队列表

```csharp
// 获取所有排队记录
var allQueues = await queueingService.GetListAsync();

// 筛选特定医生的排队列表
var doctorQueues = allQueues
    .Where(q => q.DoctorId == doctorId && q.Status == "排队中")
    .OrderBy(q => q.QueueType == "急诊" ? 0 : 1) // 急诊优先
    .ThenBy(q => q.QueueTime) // 时间顺序
    .ToList();

// 显示排队信息
foreach (var queue in doctorQueues)
{
    Console.WriteLine($"排队号: {queue.Id}, 患者: {queue.PatientName}, 类型: {queue.QueueType}");
}
```

### 医生叫号处理

```csharp
// 获取下一位患者
public async Task<QueueingDetailDto?> CallNextPatientAsync(Guid doctorId)
{
    var allQueues = await queueingService.GetListAsync();
    var nextPatient = allQueues
        .Where(q => q.DoctorId == doctorId.ToString() && q.Status == "排队中")
        .OrderBy(q => q.QueueType == "急诊" ? 0 : 1)
        .ThenBy(q => q.QueueTime)
        .FirstOrDefault();
    
    if (nextPatient != null)
    {
        // 更新状态为就诊中
        await UpdateQueueStatusAsync(nextPatient.Id, QueueStatus.InProgress);
        
        // 发送叫号通知
        await SendCallNotificationAsync(nextPatient);
        
        return await queueingService.GetByIdAsync(nextPatient.Id);
    }
    
    return null;
}
```

### 完成诊疗流程

```csharp
// 完成当前患者诊疗
public async Task<bool> CompleteCurrentPatientAsync(Guid queueId)
{
    var success = await queueingService.CompleteAsync(queueId);
    
    if (success)
    {
        logger.LogInformation("诊疗完成，排队ID: {QueueId}", queueId);
        
        // 通知下一位患者准备
        await NotifyNextPatientAsync();
        
        return true;
    }
    
    return false;
}
```

### 排队统计分析

```csharp
// 获取医生当日排队统计
public async Task<DoctorQueueStatsDto> GetDoctorQueueStatsAsync(Guid doctorId, DateTime date)
{
    var allQueues = await queueingService.GetListAsync();
    var dayQueues = allQueues.Where(q => 
        q.DoctorId == doctorId.ToString() && 
        q.QueueTime.Date == date.Date).ToList();
    
    return new DoctorQueueStatsDto
    {
        TotalQueues = dayQueues.Count,
        CompletedQueues = dayQueues.Count(q => q.Status == "已完成"),
        CancelledQueues = dayQueues.Count(q => q.Status == "已取消"),
        AverageWaitTime = CalculateAverageWaitTime(dayQueues),
        EmergencyQueues = dayQueues.Count(q => q.QueueType == "急诊")
    };
}
```

### 患者取消排队

```csharp
// 患者主动取消排队
public async Task<bool> PatientCancelQueueAsync(Guid queueId, string reason)
{
    var queueDetail = await queueingService.GetByIdAsync(queueId);
    if (queueDetail == null || queueDetail.Status != "排队中")
    {
        return false;
    }
    
    // 更新备注说明取消原因
    var editDto = new QueueingEditDto
    {
        Id = queueId,
        QueueType = queueDetail.QueueType,
        Remark = $"患者主动取消: {reason}"
    };
    
    await queueingService.UpdateAsync(editDto);
    
    // 取消排队
    var success = await queueingService.CancelAsync(queueId);
    
    if (success)
    {
        logger.LogInformation("患者取消排队，ID: {QueueId}, 原因: {Reason}", queueId, reason);
    }
    
    return success;
}
```

### 实时排队显示屏

```csharp
// 获取排队显示屏数据
public async Task<QueueDisplayDto> GetQueueDisplayDataAsync()
{
    var allQueues = await queueingService.GetListAsync();
    var activeQueues = allQueues.Where(q => q.Status == "排队中" || q.Status == "就诊中").ToList();
    
    var displayData = new QueueDisplayDto
    {
        CurrentPatients = activeQueues
            .Where(q => q.Status == "就诊中")
            .Select(q => new CurrentPatientDto
            {
                DoctorName = q.DoctorName,
                PatientName = MaskPatientName(q.PatientName), // 隐私保护
                StartTime = q.QueueTime
            }).ToList(),
        
        WaitingQueues = activeQueues
            .Where(q => q.Status == "排队中")
            .GroupBy(q => q.DoctorName)
            .Select(g => new DoctorQueueDto
            {
                DoctorName = g.Key,
                WaitingCount = g.Count(),
                NextPatients = g.OrderBy(q => q.QueueTime)
                    .Take(3)
                    .Select(q => MaskPatientName(q.PatientName))
                    .ToList()
            }).ToList(),
        
        UpdateTime = DateTime.Now
    };
    
    return displayData;
}

// 患者姓名隐私保护
private string MaskPatientName(string fullName)
{
    if (string.IsNullOrEmpty(fullName) || fullName.Length <= 1)
        return fullName;
    
    return fullName[0] + new string('*', fullName.Length - 1);
}
```

## 业务扩展建议

### 功能增强

- **智能叫号**: 基于患者位置和预计就诊时间的智能叫号系统
- **预约排队**: 支持预约时间段的排队管理
- **移动叫号**: 患者手机APP接收叫号通知
- **语音播报**: 多语言语音叫号播报系统

### 用户体验

- **等待时间预估**: 基于历史数据预估患者等待时间
- **排队进度**: 实时显示患者在队列中的位置
- **消息通知**: 微信、短信等多渠道叫号通知
- **满意度调查**: 就诊结束后的满意度评价收集

### 数据分析

- **排队效率分析**: 医生工作效率和排队处理能力分析
- **患者流量预测**: 基于历史数据的患者流量预测
- **资源优化**: 医生排班和诊室安排的优化建议
- **异常监控**: 排队异常情况的自动监控和报警