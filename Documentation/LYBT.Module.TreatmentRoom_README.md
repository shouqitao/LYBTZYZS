## AGENTS.md — 诊疗室模块（LYBT.Module.TreatmentRoom）

### 1. Agent 概述

诊疗室模块用于管理诊所的诊疗室信息与辅助治疗任务队列，支持诊疗室资源配置、任务分配、队列监控等。

### 2. 核心能力

- 创建、编辑和删除治疗室记录
- 获取治疗室列表及详情

### 3. 输入输出规范

#### 输入

- `TreatmentRoomCreateDto`：新建诊疗室
- `TreatmentRoomEditDto`：编辑诊疗室信息
- `TreatmentRoomQueryDto`：查询参数
- `TreatmentTaskDto`：创建/更新治疗任务

#### 输出

- `TreatmentRoomDto`：诊疗室列表项
- `TreatmentRoomDetailDto`：诊疗室详情
- `TreatmentTaskDto`：治疗任务详情
- `(IList<TreatmentRoomDto>, int)`：诊疗室分页结果
- `bool`：操作结果

### 4. 协作与依赖模块

- **医生/患者模块**：任务分配涉及医生、患者信息
- **系统设置模块**：治疗项目、诊疗室类型等配置
- **基础设施模块**：诊疗室及任务信息持久化

### 5. 示例场景

#### 新建诊疗室

```csharp
var dto = new TreatmentRoomCreateDto {
  Name = "理疗一室",
  Type = TreatmentRoomType.Physiotherapy
};
bool ok = await _treatmentRoomService.AddAsync(dto);
```

#### 分配治疗任务

```csharp
var task = new TreatmentTaskDto {
  RoomId = roomId,
  PatientId = patientId,
  Project = "拔罐",
  DoctorId = doctorId
};
bool ok = await _treatmentRoomService.AssignTaskAsync(task);
```

### 6. 接口列表

- `Task<List<TreatmentRoomDto>> GetListAsync()`
- `Task<TreatmentRoomDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(TreatmentRoomCreateDto dto)`
- `Task<bool> UpdateAsync(TreatmentRoomEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/treatmentroom` | 已实现 |
| `GET /api/treatmentroom` | 已实现 |
| `DELETE /api/treatmentroom/{id}` | 已实现 |



## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
