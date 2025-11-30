# 命名规范

> 创建日期: 2025-11-29
> 状态: 规范文档
> 适用范围: LYBTZYZS 项目全栈

## 概述

本文档定义项目中各层次代码的命名规范，确保代码风格一致性和可维护性。

## 通用规范

### 大小写风格

| 风格 | 示例 | 适用场景 |
|------|------|----------|
| PascalCase | `MedicalCase`, `GetPatientById` | 类、方法、属性、公共字段 |
| camelCase | `patientId`, `medicalCaseService` | 私有字段、局部变量、参数 |
| SCREAMING_SNAKE_CASE | `MAX_RETRY_COUNT` | 常量（少用，推荐 PascalCase） |

### 私有字段前缀

```csharp
// 推荐：下划线前缀
private readonly IMedicalCaseService _medicalCaseService;
private readonly ILogger<MedicalCaseController> _logger;

// 不推荐：无前缀或其他前缀
private readonly IMedicalCaseService medicalCaseService;  // 避免
private readonly IMedicalCaseService m_medicalCaseService;  // 避免
```

## 分层命名规范

### 1. Entity 层

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 实体类 | `{业务名称}` (PascalCase) | `MedicalCase`, `Patient`, `Herb` |
| 实体文件 | `{业务名称}Model.cs` | `MedicalCaseModel.cs` |
| 枚举 | `{业务名称}{类型}` | `MedicalCaseStatus`, `UserRole` |
| 导航属性 | 单数或复数（视关系） | `Patient`, `Prescriptions` |

**注意**：Entity 类名不带 `Entity` 后缀，文件名带 `Model` 后缀以区分。

```csharp
// 实体类（LYBT.Entities/MedicalCases/MedicalCaseModel.cs）
public class MedicalCase : BaseEntity
{
    public Patient Patient { get; set; }  // 单数：一对一/多对一
    public ICollection<Prescription> Prescriptions { get; set; }  // 复数：一对多
}
```

### 2. Repository 层

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 接口 | `I{业务名称}Repository` | `IMedicalCaseRepository` |
| 实现 | `{业务名称}Repository` | `MedicalCaseRepository` |
| 方法 | `{动词}{对象}Async` | `GetByIdAsync`, `GetListAsync` |

**常用方法命名**：
```csharp
public interface IMedicalCaseRepository
{
    Task<MedicalCase?> GetByIdAsync(Guid id);
    Task<List<MedicalCase>> GetListAsync(QueryParameters parameters);
    Task<MedicalCase> AddAsync(MedicalCase entity);
    Task UpdateAsync(MedicalCase entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
```

### 3. Service 层

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 接口 | `I{业务名称}Service` | `IMedicalCaseService` |
| 实现 | `{业务名称}Service` | `MedicalCaseService` |
| 方法 | `{动词}{对象}Async` | `CreateMedicalCaseAsync` |

**方法命名约定**：
- `Create{X}Async` - 创建新实体
- `Update{X}Async` - 更新实体
- `Delete{X}Async` - 删除实体（软删除）
- `Get{X}Async` - 获取单个实体
- `Get{X}ListAsync` - 获取列表
- `Set{Property}Async` - 设置特定属性

### 4. Controller 层

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 控制器 | `{业务名称}Controller` | `MedicalCaseController` |
| 路由 | 小写复数 | `/api/v1/medicalcases` |
| Action | HTTP动词对应 | `Get`, `Post`, `Put`, `Delete` |

**RESTful 路由规范**：
```csharp
[Route("api/v1/[controller]")]
public class MedicalCasesController : ControllerBase
{
    [HttpGet]                    // GET /api/v1/medicalcases
    [HttpGet("{id}")]            // GET /api/v1/medicalcases/{id}
    [HttpPost]                   // POST /api/v1/medicalcases
    [HttpPut("{id}")]            // PUT /api/v1/medicalcases/{id}
    [HttpDelete("{id}")]         // DELETE /api/v1/medicalcases/{id}
    [HttpPut("{id}/status")]     // PUT /api/v1/medicalcases/{id}/status
}
```

### 5. DTO 层

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| 查询结果 | `{业务名称}Dto` | `MedicalCaseDto` |
| 创建请求 | `{业务名称}CreateDto` | `MedicalCaseCreateDto` |
| 更新请求 | `{业务名称}UpdateDto` | `MedicalCaseUpdateDto` |
| 输入通用 | `{业务名称}InputDto` | `ConsultationInputDto` |
| 列表项 | `{业务名称}ListItemDto` | `PatientListItemDto` |

### 6. ViewModel 层 (WPF/MVVM)

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| ViewModel | `{业务名称}ViewModel` | `MedicalCaseViewModel` |
| 列表VM | `{业务名称}ListViewModel` | `PatientListViewModel` |
| 详情VM | `{业务名称}DetailViewModel` | `PatientDetailViewModel` |
| Command | `{动作}Command` | `SaveCommand`, `DeleteCommand` |

## 数据库命名规范

### 表名
- 使用 PascalCase 复数形式
- 示例：`MedicalCases`, `Patients`, `Herbs`

### 列名
- 使用 PascalCase
- 外键：`{关联实体}Id`（如 `PatientId`, `DoctorId`）
- 时间戳：`{动作}At`（如 `CreatedAt`, `UpdatedAt`, `DeletedAt`）

### 索引
- 格式：`IX_{表名}_{列名1}[_{列名2}...]`
- 示例：`IX_MedicalCases_PatientId`, `IX_Users_Email`

## 特殊命名规则

### 接口 vs 实现
```csharp
// 接口以 I 开头
public interface IMedicalCaseService { }

// 实现不带 I
public class MedicalCaseService : IMedicalCaseService { }
```

### 异步方法
```csharp
// 所有异步方法以 Async 结尾
public async Task<MedicalCase> GetByIdAsync(Guid id);
public async Task CreateMedicalCaseAsync(CreateDto dto);
```

### 布尔属性
```csharp
// 使用 Is/Has/Can 前缀
public bool IsDeleted { get; set; }
public bool HasPrescription { get; set; }
public bool CanEdit { get; set; }
public bool NeedsPrescription { get; set; }  // 或 Needs 前缀
```

### 集合属性
```csharp
// 使用复数形式
public ICollection<Prescription> Prescriptions { get; set; }
public List<PrescriptionItem> Items { get; set; }
```

## 项目特定约定

### 业务术语统一

| 中文 | 英文 | 说明 |
|------|------|------|
| 医案/病案 | MedicalCase | 聚合根 |
| 辨证/问诊诊断 | Consultation | 仅指诊断部分 |
| 处方 | Prescription | 药方 |
| 药材 | Herb | 中药材 |
| 验方 | Formula | 经验方剂 |
| 患者 | Patient | 病人 |
| 医生 | Doctor | 用户角色之一 |

### 状态枚举命名

```csharp
// 生命周期状态：{实体}Status
public enum MedicalCaseStatus { Draft, Active, Completed, Cancelled }

// 启用禁用：CommonStatus（通用）
public enum CommonStatus { Disabled, Enabled }

// 业务状态：{业务}Status
public enum PrescriptionStatus { Draft, Confirmed, Printed }
```

## 检查清单

新增代码时确认：

- [ ] 类名遵循 PascalCase
- [ ] 私有字段使用下划线前缀
- [ ] 异步方法以 Async 结尾
- [ ] 接口以 I 开头
- [ ] DTO 后缀正确（Dto/CreateDto/UpdateDto）
- [ ] 布尔属性使用 Is/Has/Can/Needs 前缀
- [ ] 集合属性使用复数形式
- [ ] 数据库表名使用复数形式

## 参考资料

- [.NET Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
