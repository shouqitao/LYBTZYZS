## AGENTS.md — 患者模块（LYBT.Module.Patients）

### 1. Agent 概述

患者模块负责管理患者的基础信息，包括新增、修改、查询、删除患者记录，并支持快速模糊搜索、分页浏览、身份证读取等辅助功能，是整个诊疗系统的核心入口之一。

### 2. 核心能力

- 添加新患者（支持手动输入或身份证读取）
- 编辑患者信息
- 删除患者记录
- 根据姓名、拼音码、身份证等快速模糊搜索
- 分页查询患者列表
- 支持按条件筛选和排序

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

- `Task<(IList<PatientDto>, int)> SearchAsync(PatientQueryDto query)`
- `Task<PatientDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(PatientCreateDto dto)`
- `Task<bool> UpdateAsync(PatientEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`

