# 编码规范

## 命名规范

### C# 命名约定

| 类型 | 风格 | 示例 |
|------|------|------|
| 类/接口 | PascalCase | `PatientService`, `IHerbRepository` |
| 方法 | PascalCase | `GetByIdAsync`, `CreateAsync` |
| 属性 | PascalCase | `PatientName`, `CreatedAt` |
| 私有字段 | _camelCase | `_service`, `_logger` |
| 参数 | camelCase | `patientId`, `pageSize` |
| 常量 | PascalCase | `MaxPageSize`, `DefaultTimeout` |
| 枚举值 | PascalCase | `UserRole.Doctor` |

### 特定命名规则

| 层 | 后缀规范 | 示例 |
|----|----------|------|
| Controller | `{Entity}Controller` | `PatientsController` |
| Service | `I{Entity}Service` / `{Entity}Service` | `IPatientService` |
| Repository | `I{Entity}Repository` / `{Entity}Repository` | `PatientRepository` |
| ViewModel | `{Feature}ViewModel` | `PatientListViewModel` |
| Entity | 无后缀 | `Patient`, `MedicalCase` |
| DTO | `{Entity}{Action}Dto` | `PatientDetailDto`, `PatientInputDto` |

### 异步方法

所有异步方法必须以 `Async` 结尾:

```csharp
// 正确
Task<Patient> GetByIdAsync(Guid id);
Task CreateAsync(PatientInputDto dto);

// 错误
Task<Patient> GetById(Guid id);
```

---

## 架构规范

### 三层依赖方向

```
Controller → Service → Repository → DbContext
     ↓           ↓          ↓
   DTO         Entity     Entity
```

**禁止**: Controller 直接访问 Repository 或 DbContext。

### DDD 聚合根规则

- `MedicalCase` 是唯一聚合根
- `Consultation` 和 `Prescription` 是内部子实体
- 子实体不能独立创建/删除，必须通过聚合根操作
- `Consultation` 与 `MedicalCase` 共享主键 (1:1)
- `Prescription` 通过 `MedicalCaseId` 关联 (1:0..1)

### MVVM 模式 (Desktop)

```
View (XAML) ← 数据绑定 → ViewModel → Repository → API/DataSource
```

- View 不包含业务逻辑
- ViewModel 通过 DI 注入 Repository
- Repository 封装 API 调用或 DataSource 访问
- 使用 Prism `BindableBase` 和 `DelegateCommand`

---

## 编码原则

### 软删除

所有业务实体使用软删除:

```csharp
entity.IsDeleted = true;
entity.DeletedAt = DateTime.UtcNow;
```

EF Core 全局查询过滤器自动排除 `IsDeleted = true` 的记录。

**注意**: `FindAsync` 在实体不在 ChangeTracker 中时会应用全局过滤器，需要用 `IgnoreQueryFilters()` 查询软删除记录。

### 异常处理

- Controller 不写 try-catch，由全局异常处理器 (`BusinessExceptionHandler` + `SystemExceptionHandler`) 统一处理
- Service 层抛出 `BusinessException` 表示业务规则违反
- 使用 `Result<T>` 模式传递操作结果

### API 响应

统一使用 `ApiResponse<T>` 包装:

```csharp
// 成功
return Success(data, "操作成功");

// 业务失败 (200 + success=false)
return BusinessFail("验证失败");

// 参数错误 (400)
return ValidationFail("参数无效");

// 未找到 (404)
return NotFound("资源不存在");
```

### 所有权检查

非管理员只能操作自己创建的资源:

```csharp
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _service.GetByIdAsync, "验方");
if (ownershipError != null) return ownershipError;
```

---

## 术语规范

| 英文术语 | 中文含义 | 错误用法 |
|----------|----------|----------|
| Consultation | 诊断 | "问诊"、"就诊" |
| MedicalCase | 医案 | "病历" |
| Formula | 验方/经验方 | "方剂" |
| Prescription | 处方 | - |
| Herb | 药材 | "中药" |
| Patient | 患者 | "病人" |

---

## 变更分级

| 级别 | 说明 | 执行方式 |
|------|------|----------|
| 局部优化 | 单模块内调整 | 直接执行 |
| 跨模块优化 | 影响 2-3 个模块 | 说明影响范围后执行 |
| 架构重构 | 核心架构变更 | 需确认方案 |
| 技术栈变更 | 引入/替换框架 | 必须审批 |

---

## 兼容代码规范

兼容代码是临时措施:

```csharp
// OpenSpec: {change-id} - 兼容设计，待{目标提案}完成后移除
```

- 必须添加上述注释标记
- 重构完成后必须创建清理提案
- 禁止无限期保留兼容代码

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
