## AGENTS.md — 患者模块（LYBT.Module.Patients）

### 1. Agent 概述

患者模块负责管理患者的基础信息，包括新增、修改、查询、删除患者记录，并支持快速模糊搜索、分页浏览、身份证读取等辅助功能，是整个诊疗系统的核心入口之一。

### 2. 核心能力

- 添加、编辑和删除患者记录
- 根据关键词快速搜索患者
- 分页查询患者列表
- 启用/禁用患者及批量禁用
- 授权患者给指定医生
- 批量导入与导出患者数据

### 3. 输入输出规范

#### 输入

- `PatientCreateDto`：新增患者（含姓名、性别、年龄、电话、地址等）
- `PatientEditDto`：修改患者信息
- `PatientQueryDto`：模糊搜索与分页参数

#### 输出

- `PatientDto`：患者基本信息
- `(IList<PatientDto>, int TotalCount)`：分页结果
- `bool`：操作成功与否

### 4. 协作与依赖模块

- **挂号模块**：挂号前需从患者模块选择或创建患者
- **病历模块**：病历中关联患者 ID
- **诊疗模块**：一条诊疗记录需关联患者基本信息
- **通用模块**：使用枚举类型（如性别）与通用分页返回结构
- **基础设施模块**：通过仓储方式持久化患者信息到数据库

### 5. 示例场景

#### 新增患者

```csharp
var dto = new PatientCreateDto {
  Name = "张三",
  Gender = Gender.Male,
  Age = 35,
  PhoneNumber = "1234567890",
  Address = "广州市天河区"
};
bool result = await _patientService.AddAsync(dto);
```

#### 搜索患者

```csharp
var query = new PatientQueryDto {
  Keyword = "zs",
  PageIndex = 1,
  PageSize = 10
};
var (list, total) = await _patientService.SearchAsync(query);
```

### 6. 接口列表

- `Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query)`
- `Task<PatientDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(PatientDetailDto dto, Guid operatorId, string operatorName)`
- `Task<bool> UpdateAsync(PatientDetailDto dto, Guid operatorId, string operatorName)`
- `Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)`
- `Task<int> BatchDeleteAsync(List<string> ids, Guid operatorId, string operatorName)`
- `Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName)`
- `Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName)`
- `Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName)`
- `Task<List<PatientDetailDto>> SearchAsync(string keyword)`
- `Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)`
- `Task<int> ImportAsync(List<PatientDetailDto> dtos, Guid operatorId, string operatorName)`
- `Task<List<PatientDetailDto>> ExportAsync()`
- `Task<List<RecordDto>> GetHistoryRecordsAsync(Guid patientId)`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/patients` | 未实现 | 实际路径 `POST /api/Patients/add` |
| `PUT /api/patients/{id}` | 未实现 | 实际路径 `PUT /api/Patients/edit` |
| `GET /api/patients` | 未实现 | 提供 `GET /api/Patients/all`、`/paged` 等 |
| `DELETE /api/patients/{id}` | 未实现 | 提供 `batchDelete` 与 `disable/{id}` |
| `POST /api/patients/readidcard` | 未实现 | 代码中未找到对应实现 |



## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
