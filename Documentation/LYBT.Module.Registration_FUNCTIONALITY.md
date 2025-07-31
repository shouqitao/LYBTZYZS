# LYBT.Module.Registration 功能说明文档

## 模块概述

挂号管理模块负责医院挂号业务的完整流程管理，包括挂号预约、号源管理、挂号状态跟踪、医患关联等功能。本模块与患者、医生模块深度集成，支持多种挂号类型、状态流转和历史记录管理，为医院门诊业务提供基础支撑。

## 数据模型

### RegistrationModel (挂号实体)

**文件位置**: `Models/RegistrationModel.cs`

| 字段名              | 类型                | 说明         | 验证规则             |
| ---------------- | ----------------- | ---------- | ---------------- |
| Id               | Guid              | 挂号唯一标识（主键） | 必填               |
| PatientId        | Guid              | 患者ID       | 必填，外键关联患者表       |
| PatientName      | string            | 患者姓名       | 用于列表显示，冗余字段      |
| DoctorId         | Guid              | 医生ID       | 必填，外键关联医生表       |
| DoctorName       | string            | 医生姓名       | 用于列表显示，冗余字段      |
| RegistrationType | RegistrationType  | 挂号类型       | 枚举值，默认Normal     |
| IsFromDoctor     | bool              | 是否医生直接挂号   | 默认false，标识挂号来源   |
| Status           | RegistrationStatus | 挂号状态       | 枚举值，默认Registered |
| RegistrationTime | DateTime          | 挂号时间       | 默认当前时间           |
| Remark           | string            | 备注信息       | 可选字段             |

### 枚举类型

#### RegistrationType (挂号类型)
- `Normal (1)`: 普通号 - 常规门诊挂号
- `Expert (2)`: 专家号 - 专家门诊挂号
- `Emergency (3)`: 急诊号 - 急诊科挂号
- `FollowUp (4)`: 复诊号 - 复诊患者挂号

#### RegistrationStatus (挂号状态)
- `Registered (1)`: 已挂号 - 挂号成功，等待就诊
- `Visited (2)`: 已就诊 - 患者已就诊
- `Completed (3)`: 已完成 - 就诊流程完成
- `Cancelled (-1)`: 已取消 - 挂号被取消
- `Expired (-2)`: 过期 - 挂号过期失效

## DTO 数据传输对象

### RegistrationDto (挂号列表展示)

**使用场景**: 挂号列表展示、简单挂号信息返回
**特点**: 包含显示友好的字符串格式信息

```csharp
- Id: 挂号ID
- PatientName: 患者姓名
- DoctorName: 医生姓名
- RegistrationType: 挂号类型（字符串描述）
- RegistrationTime: 挂号时间
- Status: 状态（字符串描述）
```

### RegistrationDetailDto (挂号详情)

**使用场景**: 挂号详情展示、完整挂号信息查看
**特点**: 包含挂号完整信息

```csharp
- Id: 挂号ID
- PatientId: 患者ID（字符串格式）
- DoctorId: 医生ID（字符串格式）
- PatientName: 患者姓名
- RegistrationType: 挂号类型（字符串描述）
- RegistrationTime: 挂号时间
- Status: 状态（字符串描述）
- Remark: 备注信息
```

### RegistrationCreateDto (挂号创建)

**使用场景**: 新建挂号记录
**特点**: 包含数据验证规则

```csharp
- PatientId: 患者ID（必填，字符串格式）
- DoctorId: 医生ID（必填，字符串格式）
- RegistrationType: 挂号类型（必填，默认"普通"）
- RegistrationTime: 挂号时间（默认当前时间）
- Remark: 备注信息（可选）
```

### RegistrationEditDto (挂号编辑)

**使用场景**: 编辑挂号信息
**特点**: 继承自RegistrationCreateDto，包含ID字段

```csharp
- Id: 挂号ID（必填，标识更新目标）
- 其他字段同RegistrationCreateDto
```

## 服务层 (IRegistrationService & RegistrationService)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<RegistrationDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取挂号详情
**使用场景**: 挂号详情页面、编辑前数据加载

#### GetListAsync

```csharp
Task<List<RegistrationDto>> GetListAsync()
```

**功能**: 获取所有挂号列表
**特点**: 返回简化的挂号信息
**使用场景**: 挂号管理页面的列表展示

#### AddAsync

```csharp
Task<bool> AddAsync(RegistrationCreateDto dto)
```

**功能**: 创建新挂号记录
**业务逻辑**: 
- 患者ID和医生ID必填验证
- 患者和医生存在性验证
- 挂号时间有效性验证
- 重复挂号检查（同一患者同一医生同一天）
- 医生排班和号源检查

**使用场景**: 前台挂号、在线预约挂号

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(RegistrationEditDto dto)
```

**功能**: 更新挂号信息
**业务逻辑**: 
- 挂号ID验证
- 状态检查（只有部分状态可编辑）
- 数据变更验证
- 操作日志记录

**使用场景**: 挂号信息修改、状态调整

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除挂号记录
**注意**: 物理删除，不推荐使用
**建议**: 使用CancelAsync代替
**使用场景**: 特殊情况下的数据清理

#### CancelAsync

```csharp
Task<bool> CancelAsync(Guid id)
```

**功能**: 取消挂号
**业务逻辑**: 
- 状态检查（只有已挂号状态可取消）
- 更新状态为Cancelled
- 号源释放
- 操作日志记录

**使用场景**: 患者取消挂号、医生停诊处理

### 扩展业务方法 (建议扩展)

#### GetByPatientIdAsync

```csharp
Task<List<RegistrationDto>> GetByPatientIdAsync(Guid patientId, int days = 30)
```

**功能**: 获取患者指定时间内的挂号记录
**参数**: days - 查询天数（默认30天）
**使用场景**: 患者挂号历史查询

#### GetByDoctorIdAsync

```csharp
Task<List<RegistrationDto>> GetByDoctorIdAsync(Guid doctorId, DateTime date)
```

**功能**: 获取医生指定日期的挂号列表
**使用场景**: 医生门诊安排查看

#### GetByStatusAsync

```csharp
Task<List<RegistrationDto>> GetByStatusAsync(RegistrationStatus status)
```

**功能**: 根据状态获取挂号列表
**使用场景**: 特定状态挂号查询

#### GetPagedAsync

```csharp
Task<PagedResultDto<RegistrationDto>> GetPagedAsync(RegistrationQueryDto query)
```

**功能**: 分页条件查询挂号列表
**查询条件**: 
- 患者姓名关键词搜索
- 医生筛选
- 挂号类型筛选
- 状态筛选
- 时间范围筛选

**使用场景**: 挂号管理页面的分页列表

#### CheckInAsync

```csharp
Task<bool> CheckInAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 患者签到（从已挂号变为已就诊）
**业务逻辑**: 
- 状态检查（只有已挂号状态可签到）
- 更新状态为Visited
- 创建就诊记录
- 操作日志记录

**使用场景**: 患者到院签到

#### CompleteAsync

```csharp
Task<bool> CompleteAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 完成就诊（从已就诊变为已完成）
**业务逻辑**: 
- 状态检查（只有已就诊状态可完成）
- 更新状态为Completed
- 完成时间记录
- 操作日志记录

**使用场景**: 医生完成诊疗

#### GetTodayRegistrationsAsync

```csharp
Task<List<RegistrationDto>> GetTodayRegistrationsAsync(Guid doctorId)
```

**功能**: 获取医生今日挂号列表
**使用场景**: 医生工作台、当日门诊管理

#### GetAvailableSlotsAsync

```csharp
Task<List<TimeSlot>> GetAvailableSlotsAsync(Guid doctorId, DateTime date)
```

**功能**: 获取医生指定日期的可用时段
**业务逻辑**: 
- 医生排班查询
- 已挂号时段排除
- 可用号源计算

**使用场景**: 挂号预约时段选择

### 统计分析方法 (建议扩展)

#### GetStatisticsAsync

```csharp
Task<RegistrationStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate)
```

**功能**: 获取挂号统计信息
**统计内容**: 
- 各状态挂号数量
- 医生挂号量排名
- 挂号类型分布
- 日均挂号量趋势

**使用场景**: 数据分析、运营报表

## 仓储层 (IRegistrationRepository & RegistrationRepository)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<RegistrationModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取挂号实体
**使用场景**: 服务层调用的底层数据操作

#### GetListAsync

```csharp
Task<List<RegistrationModel>> GetListAsync()
```

**功能**: 获取所有挂号实体列表
**使用场景**: 批量操作、全量查询

#### AddAsync

```csharp
Task<bool> AddAsync(RegistrationModel model)
```

**功能**: 新增挂号记录
**使用场景**: 创建新挂号

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(RegistrationModel model)
```

**功能**: 更新挂号信息
**使用场景**: 挂号信息修改

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除挂号记录
**注意**: 物理删除，不推荐使用
**使用场景**: 特殊情况下的数据清理

#### CancelAsync

```csharp
Task<bool> CancelAsync(Guid id)
```

**功能**: 取消挂号
**实现**: 更新状态为Cancelled，不删除数据
**使用场景**: 挂号取消操作

### 扩展查询方法 (建议扩展)

#### GetByPatientIdAsync

```csharp
Task<List<RegistrationModel>> GetByPatientIdAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null)
```

**功能**: 根据患者ID查询挂号记录
**使用场景**: 患者挂号历史查询

#### GetByDoctorIdAsync

```csharp
Task<List<RegistrationModel>> GetByDoctorIdAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
```

**功能**: 根据医生ID查询挂号记录
**使用场景**: 医生门诊记录查询

#### GetByStatusAsync

```csharp
Task<List<RegistrationModel>> GetByStatusAsync(RegistrationStatus status)
```

**功能**: 根据状态查询挂号记录
**使用场景**: 状态筛选查询

#### GetByDateRangeAsync

```csharp
Task<List<RegistrationModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
```

**功能**: 根据时间范围查询挂号记录
**使用场景**: 时间段统计查询

#### GetPagedAsync

```csharp
Task<(List<RegistrationModel> list, int total)> GetPagedAsync(RegistrationQueryDto query)
```

**功能**: 分页查询挂号记录
**查询条件**: 多条件组合查询
**使用场景**: 分页列表展示

#### CheckDuplicateAsync

```csharp
Task<bool> CheckDuplicateAsync(Guid patientId, Guid doctorId, DateTime date)
```

**功能**: 检查重复挂号
**业务逻辑**: 同一患者同一医生同一天是否已挂号
**使用场景**: 挂号前重复检查

## 权限控制策略

### 角色级别权限

- **前台人员**: 可挂号、查看、修改、取消挂号
- **医生**: 可查看自己的挂号列表，可完成就诊流程
- **护士**: 可查看挂号列表，可协助患者签到
- **管理员**: 可查看和操作所有挂号记录
- **患者**: 可查看自己的挂号记录

### 操作权限

- **创建挂号**: 前台人员、在线系统
- **修改挂号**: 前台人员、管理员
- **取消挂号**: 前台人员、管理员、患者本人
- **签到操作**: 前台人员、护士、医生
- **完成就诊**: 医生本人、管理员

### 数据访问控制

- 医生只能访问自己的挂号记录
- 患者只能访问自己的挂号记录
- 前台人员可访问当日挂号记录
- 管理员可访问所有挂号记录

## 业务规则

### 挂号状态流转

```
Registered → Visited → Completed
     ↓         ↓
 Cancelled  Cancelled
     ↓
  Expired
```

**状态流转规则**:
- 已挂号可以签到、取消、过期
- 已就诊可以完成、取消，不能再编辑
- 已完成、已取消、过期为终态，不能再变更

### 业务约束

- **时间限制**: 挂号时间不能早于当前时间
- **重复检查**: 同一患者同一医生同一天不能重复挂号
- **号源限制**: 医生每日挂号数量有上限
- **状态限制**: 只有特定状态可以进行特定操作

### 数据完整性

- **关联验证**: 患者和医生必须存在且有效
- **时间验证**: 挂号时间必须在合理范围内
- **类型验证**: 挂号类型必须有效
- **状态验证**: 状态流转必须符合业务规则

## 集成依赖

### 模块依赖

- **LYBT.Module.Patients**: 患者模块（患者信息验证）
- **LYBT.Module.Doctors**: 医生模块（医生信息验证）
- **LYBT.Module.Queueing**: 排队模块（挂号后排队）
- **LYBT.Infrastructure**: 基础设施（日志、缓存、配置）

### 外部集成

- **排班系统**: 医生排班和号源管理
- **收费系统**: 挂号费用计算和支付
- **通知系统**: 挂号成功、取消等通知
- **门诊系统**: 就诊流程集成

## 使用示例

### 患者挂号

```csharp
var createDto = new RegistrationCreateDto {
    PatientId = "550e8400-e29b-41d4-a716-446655440000",
    DoctorId = "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
    RegistrationType = "专家",
    RegistrationTime = DateTime.Today.AddHours(9), // 明日上午9点
    Remark = "患者要求专家号"
};

var result = await registrationService.AddAsync(createDto);
```

### 患者签到

```csharp
// 患者到院签到
var checkInResult = await registrationService.CheckInAsync(registrationId, frontDeskId, "前台小王");
```

### 完成就诊

```csharp
// 医生完成诊疗
var completeResult = await registrationService.CompleteAsync(registrationId, doctorId, "张医生");
```

### 查询医生今日挂号

```csharp
var todayRegistrations = await registrationService.GetTodayRegistrationsAsync(doctorId);

foreach (var reg in todayRegistrations) {
    Console.WriteLine($"患者: {reg.PatientName}, 类型: {reg.RegistrationType}, 状态: {reg.Status}");
}
```

### 分页查询挂号记录

```csharp
var query = new RegistrationQueryDto {
    PatientName = "张",
    DoctorId = doctorId,
    Status = RegistrationStatus.Registered,
    StartDate = DateTime.Today,
    EndDate = DateTime.Today.AddDays(1),
    Page = 1,
    PageSize = 20
};

var pagedResult = await registrationService.GetPagedAsync(query);
```

### 取消挂号

```csharp
var cancelResult = await registrationService.CancelAsync(registrationId);
if (cancelResult) {
    // 发送取消通知
    await notificationService.SendCancellationNotice(registrationId);
}
```

### 获取可用时段

```csharp
var availableSlots = await registrationService.GetAvailableSlotsAsync(doctorId, DateTime.Today.AddDays(1));

foreach (var slot in availableSlots) {
    Console.WriteLine($"时段: {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm}, 剩余: {slot.AvailableCount}");
}
```

### 挂号统计

```csharp
var statistics = await registrationService.GetStatisticsAsync(
    DateTime.Today.AddDays(-30), 
    DateTime.Today
);

Console.WriteLine($"本月挂号总数: {statistics.TotalCount}");
Console.WriteLine($"完成就诊数: {statistics.CompletedCount}");
Console.WriteLine($"取消挂号数: {statistics.CancelledCount}");
Console.WriteLine($"日均挂号量: {statistics.DailyAverage:F1}");
```

## 扩展建议

### 功能扩展

- **预约挂号**: 支持预约未来时间的挂号
- **号源管理**: 医生号源配置和动态调整
- **挂号费管理**: 不同类型挂号费用设置
- **排班集成**: 与医生排班系统深度集成
- **候诊管理**: 挂号后的候诊队列管理

### 技术优化

- **缓存策略**: 号源信息缓存，提高查询性能
- **并发控制**: 挂号时的并发锁定机制
- **消息队列**: 挂号状态变更事件通知
- **实时更新**: WebSocket实时更新挂号状态
- **移动支持**: 移动端挂号和查询功能

### 集成增强

- **微信挂号**: 微信小程序挂号集成
- **支付集成**: 在线支付挂号费功能
- **短信通知**: 挂号成功、提醒、取消等短信通知
- **身份验证**: 身份证、医保卡等身份验证
- **数据同步**: 与HIS系统的数据同步

### 业务优化

- **智能推荐**: 根据病史推荐合适的医生
- **时段优化**: 基于历史数据的时段分配优化
- **预警机制**: 挂号异常和拥堵预警
- **服务评价**: 挂号和就诊服务质量评价
- **数据分析**: 挂号数据的深度分析和报告