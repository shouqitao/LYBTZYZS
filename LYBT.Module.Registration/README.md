## AGENTS.md — 挂号模块（LYBT.Module.Registration）

### 1. Agent 概述

挂号模块负责患者在诊所的挂号登记工作，包括普通挂号、急诊挂号、直接挂号与排队关联，支撑诊疗工作流的入口环节。

### 2. 核心能力

- 创建挂号记录（含挂号类型、挂号医生、挂号时间等）
- 修改或取消挂号记录
- 支持分页与条件查询挂号列表
- 支持挂号时自动关联排队记录（根据系统设置）

### 3. 输入输出规范

#### 输入

- `RegistrationCreateDto`：创建挂号信息（必含患者ID、医生ID、挂号类型）
- `RegistrationEditDto`：编辑挂号记录
- `RegistrationQueryDto`：查询参数（按患者、医生、状态、日期范围等）

#### 输出

- `RegistrationDto`：挂号详情
- `(IList<RegistrationDto>, int TotalCount)`：分页结果
- `bool`：表示操作是否成功

### 4. 协作与依赖模块

- **患者模块**：提供患者信息，挂号需引用患者ID
- **医生模块**：提供医生信息，挂号需指定医生
- **排队模块**：根据设置，挂号后自动生成排队记录
- **诊疗模块**：挂号后产生诊疗记录
- **日志模块**：记录挂号行为
- **基础设施模块**：用于持久化挂号数据

### 5. 示例场景

#### 普通挂号

```csharp
var dto = new RegistrationCreateDto {
  PatientId = patientId,
  DoctorId = doctorId,
  Type = RegistrationType.Normal
};
bool ok = await _registrationService.AddAsync(dto);
```

#### 查询当日挂号记录

```csharp
var query = new RegistrationQueryDto {
  Date = DateTime.Today,
  DoctorId = doctorId
};
var (list, total) = await _registrationService.SearchAsync(query);
```

### 6. 接口列表

- `Task<(IList<RegistrationDto>, int)> SearchAsync(RegistrationQueryDto dto)`
- `Task<RegistrationDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(RegistrationCreateDto dto)`
- `Task<bool> UpdateAsync(RegistrationEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`

