## AGENTS.md — 诊疗模块（LYBT.Module.DiagnosisTreatment）

### 1. Agent 概述

诊疗模块用于记录患者的诊疗过程，包括主诉、现病史、诊断、治疗项目和处方药方，是医生开具治疗方案和中药方剂的核心功能模块。

### 2. 核心能力

- 创建诊疗记录（含诊断、治疗项目、药方）
- 编辑诊疗记录
- 删除诊疗记录
- 获取诊疗记录列表

### 3. 输入输出规范

#### 输入

- `DiagnosisTreatmentCreateDto`：创建诊疗记录（含患者ID、诊断内容、药方组成等）
- `DiagnosisTreatmentEditDto`：编辑诊疗记录

#### 输出

- `DiagnosisTreatmentDto`：诊疗记录概要
- `DiagnosisTreatmentDetailDto`：详细信息（含药方与治疗项目）
- `(IList<DiagnosisTreatmentDto>, int TotalCount)`：分页结果

### 4. 协作与依赖模块

- **患者模块**：关联患者基本资料
- **挂号模块**：诊疗需绑定挂号ID
- **处方模块**：诊疗中生成结构化药方
- **系统设置模块**：使用诊断目录、治疗项目配置
- **日志模块**：记录新增和修改诊疗记录日志
- **基础设施模块**：数据持久化

### 5. 示例场景

#### 创建诊疗记录

```csharp
var dto = new DiagnosisTreatmentCreateDto {
  PatientId = patientId,
  RegistrationId = registrationId,
  Diagnosis = "风寒感冒",
  Treatments = new List<TreatmentItemDto> {
    new TreatmentItemDto { Name = "针灸", Count = 1 }
  },
  Herbs = new List<HerbItemDto> {
    new HerbItemDto { Name = "荆芥", Quantity = 10, Unit = "g", UnitPrice = 1.5M }
  }
};
await _diagnosisService.AddAsync(dto);
```

### 6. 接口列表

- `Task<List<DiagnosisTreatmentDto>> GetListAsync()`
- `Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(DiagnosisTreatmentCreateDto dto)`
- `Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/diagnosis` | 已实现 | `DiagnosisTreatmentController.Add` |
| `PUT /api/diagnosis/{id}` | 已实现 | Id 在 DTO 中传递 |
| `GET /api/diagnosis` | 已实现 | |
| `GET /api/diagnosis/{id}` | 已实现 | |
| `POST /api/diagnosis/importtemplate` | 未实现 | 未找到对应接口 |



## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
