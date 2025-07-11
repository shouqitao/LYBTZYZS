## AGENTS.md — 病历模块（LYBT.Module.Records）

### 1. Agent 概述

病历模块用于管理患者完整诊疗过程的病历记录，包括主诉、诊断、治疗、用药、挂号信息与共享权限等，是医生回溯患者历史的核心模块。

### 2. 核心能力

- 新增病历记录（含挂号ID、诊疗ID、主诉、诊断、处方等）
- 编辑病历内容
- 删除病历
- 查询/分页病历列表（按患者、医生、日期、共享状态等）
- 病历共享与权限管理（可设置公开/非公开、指定共享对象）

### 3. 输入输出规范

#### 输入

- `RecordCreateDto`：新建病历（含必填患者ID、挂号ID、诊断内容等）
- `RecordEditDto`：编辑病历
- `RecordQueryDto`：搜索/分页参数
- `RecordShareDto`：设置病历共享

#### 输出

- `RecordDto`：病历列表摘要
- `RecordDetailDto`：病历详细内容
- `(IList<RecordDto>, int TotalCount)`：分页结果
- `bool`：操作是否成功

### 4. 协作与依赖模块

- **患者模块**：记录患者基础信息
- **挂号模块**：病历需绑定挂号ID
- **诊疗/处方模块**：病历内含诊疗过程与用药明细
- **系统设置模块**：共享策略、权限配置
- **日志模块**：记录病历增删改操作
- **基础设施模块**：病历信息持久化

### 5. 示例场景

#### 新增病历

```csharp
var dto = new RecordCreateDto {
  PatientId = patientId,
  RegistrationId = regId,
  Diagnosis = "咳嗽",
  ChiefComplaint = "咳嗽一周，痰白"
};
bool ok = await _recordService.AddAsync(dto, doctorId, doctorName);
```

#### 设置病历共享

```csharp
var shareDto = new RecordShareDto {
  RecordId = recordId,
  SharedDoctorIds = new List<Guid> { doctor2Id }
};
bool ok = await _recordService.ShareAsync(shareDto);
```

### 6. 接口列表

- `Task<(IList<RecordDto>, int)> SearchAsync(RecordQueryDto query)`
- `Task<RecordDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(RecordCreateDto dto, Guid operatorId, string operatorName)`
- `Task<bool> UpdateAsync(RecordEditDto dto, Guid operatorId, string operatorName)`
- `Task<bool> DeleteAsync(Guid id)`
- `Task<bool> ShareAsync(RecordShareDto dto)`

