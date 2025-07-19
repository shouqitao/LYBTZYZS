## AGENTS.md — 医生模块（LYBT.Module.Doctors）

### 1. Agent 概述

医生模块用于管理医生档案信息，包括姓名、性别、职称、联系方式、执业状态、挂号权限等，支持医生信息增删改查。

### 2. 核心能力

- 新增和编辑医生档案
- 禁用/启用医生账号及批量操作
- 分页查询医生列表
- 按在职状态筛选

### 3. 输入输出规范

#### 输入

- `DoctorCreateDto`：新增医生
- `DoctorEditDto`：修改医生信息
- `DoctorQueryDto`：分页/条件查询

#### 输出

- `DoctorDto`：医生列表项
- `DoctorDetailDto`：医生详细资料
- `(IList<DoctorDto>, int TotalCount)`：分页结果
- `bool`：操作成功与否

### 4. 协作与依赖模块

- **用户模块**：医生账号与用户表可关联
- **挂号/诊疗模块**：医生信息用于挂号、诊疗、排队等业务
- **基础设施模块**：持久化医生信息
- **日志模块**：医生信息变更写入操作日志

### 5. 示例场景

#### 新增医生

```csharp
var dto = new DoctorCreateDto {
  Name = "王主任",
  Gender = Gender.Male,
  Title = DoctorTitle.ChiefPhysician,
  PhoneNumber = "13312345678"
};
bool ok = await _doctorService.AddAsync(dto);
```

#### 禁用医生账号

```csharp
await _doctorService.DisableAsync(doctorId);
```

### 6. 接口列表

- `Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query)`
- `Task<List<DoctorDto>> SearchAsync(string keyword)`
- `Task<DoctorDetailDto?> GetByIdAsync(Guid id)`
- `Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId)`
- `Task<bool> AddAsync(DoctorDetailDto dto)`
- `Task<bool> UpdateAsync(DoctorDetailDto dto)`
- `Task<bool> DisableAsync(Guid id)`
- `Task<bool> EnableAsync(Guid id)`
- `Task<int> BatchDisableAsync(List<Guid> ids)`
- `Task<int> BatchEnableAsync(List<Guid> ids)`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/doctors` | 未实现 | 实际路径 `POST /api/Doctors/add` |
| `PUT /api/doctors/{id}` | 未实现 | 实际路径 `PUT /api/Doctors/update` |
| `GET /api/doctors` | 未实现 | 实际路径 `GET /api/Doctors/search` |
| `DELETE /api/doctors/{id}` | 未实现 | 提供 `disable/{id}` 与 `enable/{id}` |



## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```
