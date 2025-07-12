## AGENTS.md — 排队模块（LYBT.Module.Queueing）

### 1. Agent 概述

排队模块负责管理患者的候诊排队信息，主要用于控制医生就诊顺序，配合挂号流程生成和调整候诊队列，是门诊调度的重要组成部分。

### 2. 核心能力

- 创建排队记录
- 获取全部排队列表
- 取消排队记录或标记完成
- 删除排队记录

### 3. 输入输出规范

#### 输入

- `QueueingCreateDto`：创建排队（需含挂号ID、患者ID、医生ID）
- `QueueingEditDto`：编辑排队记录
- `QueueingQueryDto`：查询条件（按医生、患者、时间等）

#### 输出

- `QueueingDto`：排队信息详情
- `(IList<QueueingDto>, int TotalCount)`：分页结果
- `bool`：操作是否成功

### 4. 协作与依赖模块

- **挂号模块**：挂号成功后自动生成排队记录
- **诊疗模块**：完成就诊后移除对应排队记录
- **基础设施模块**：用于访问排队数据表
- **系统设置模块**：可配置是否开启自动排队、排队优先级等规则

### 5. 示例场景

#### 创建排队

```csharp
var dto = new QueueingCreateDto {
  RegistrationId = regId,
  PatientId = patientId,
  DoctorId = doctorId,
  Type = QueueType.Normal
};
bool ok = await _queueingService.AddAsync(dto);
```

#### 查询排队

```csharp
var list = await _queueingService.GetListAsync();
```

### 6. 接口列表

- `Task<List<QueueingDto>> GetListAsync()`
- `Task<QueueingDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(QueueingCreateDto dto)`
- `Task<bool> UpdateAsync(QueueingEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`
- `Task<bool> CancelAsync(Guid id)`

