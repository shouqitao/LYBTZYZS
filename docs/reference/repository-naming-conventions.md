# Repository 命名规范

**版本**: v1.0.0
**创建日期**: 2025-12-05
**适用范围**: Server端所有Repository接口和实现

---

## 1. 概述

本文档定义了LYBTZYZS项目中Repository层的方法命名规范，确保代码一致性和可维护性。

## 2. 基础接口方法 (IRepository<T>)

所有Repository继承自 `IRepository<T>`，包含11个标准方法：

### 2.1 查询方法 (5个)

| 方法名 | 返回类型 | 说明 |
|--------|----------|------|
| `GetByIdAsync(Guid id)` | `Task<T?>` | 根据ID获取单个实体 |
| `GetAllAsync()` | `Task<IEnumerable<T>>` | 获取所有实体（小数据量场景） |
| `GetPagedAsync(int pageNumber, int pageSize, string? keyword)` | `Task<PagedResult<T>>` | 分页查询 |
| `FindAsync(Expression<Func<T, bool>> predicate)` | `Task<IEnumerable<T>>` | 条件查询 |
| `GetSingleAsync(Expression<Func<T, bool>> predicate)` | `Task<T?>` | 条件查询单个实体 |

### 2.2 写入方法 (4个)

| 方法名 | 返回类型 | 说明 |
|--------|----------|------|
| `AddAsync(T entity)` | `Task<T>` | 新增实体 |
| `UpdateAsync(T entity)` | `Task<T>` | 更新实体 |
| `DeleteAsync(Guid id)` | `Task<bool>` | 删除实体 |
| `AddRangeAsync(IEnumerable<T> entities)` | `Task<IEnumerable<T>>` | 批量新增 |

### 2.3 辅助方法 (2个)

| 方法名 | 返回类型 | 说明 |
|--------|----------|------|
| `CountAsync()` | `Task<int>` | 获取总数 |
| `SaveChangesAsync()` | `Task<int>` | 保存更改 |

## 3. 特定业务方法命名规范

### 3.1 关联查询方法

**规范**: `GetByIdWith{关联实体}Async`

```csharp
// 正确示例
Task<Prescription?> GetByIdWithDetailsAsync(Guid id);      // 包含所有关联
Task<MedicalCase?> GetByIdWithPrescriptionsAsync(Guid id); // 包含处方关联

// 避免使用
Task<Prescription?> GetByIdWithItemsAsync(Guid id);        // Items 语义不清
```

### 3.2 外键查询方法

**规范**: `GetBy{外键实体}IdAsync`

```csharp
// 返回单个实体
Task<Consultation?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

// 返回列表（上下文语义明确时可省略List后缀）
Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);
```

### 3.3 存在性检查方法

**规范**: `{字段名}ExistsAsync`

```csharp
// 正确示例
Task<bool> UsernameExistsAsync(string username);

// 避免使用
Task<bool> IsUsernameExistsAsync(string username);  // Is前缀用于属性，不用于方法
Task<bool> ExistsAsync(string username);            // 语义不够明确
```

### 3.4 批量查询方法

**规范**: `GetBy{条件}Async` 或 `GetByIdsWithDetailsAsync`

```csharp
// 批量ID查询
Task<List<Prescription>> GetByIdsWithItemsAsync(IEnumerable<Guid> prescriptionIds);

// 前缀查询
Task<List<string>> GetPrescriptionNumbersByPrefixAsync(string prefix);
```

### 3.5 分页查询方法

**规范**: `GetPaged{可选修饰}Async`

```csharp
// 基础分页
Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? keyword);

// 包含关联数据的分页
Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string? keyword);
```

## 4. 命名规则总结

| 场景 | 命名模式 | 示例 |
|------|----------|------|
| 根据ID获取 | `GetByIdAsync` | `GetByIdAsync(Guid id)` |
| 获取含关联 | `GetByIdWith{关联}Async` | `GetByIdWithDetailsAsync(Guid id)` |
| 外键查询 | `GetBy{外键}IdAsync` | `GetByMedicalCaseIdAsync(Guid id)` |
| 存在性检查 | `{字段}ExistsAsync` | `UsernameExistsAsync(string)` |
| 批量查询 | `GetByIdsWithDetailsAsync` | `GetByIdsWithItemsAsync(IEnumerable<Guid>)` |
| 分页查询 | `GetPagedWithDetailsAsync` | `GetPagedWithDetailsAsync(...)` |

## 5. Read-only Repository (IReadRepository<T>)

对于聚合根边界内的子实体（如Prescription、Consultation），使用只读Repository：

```csharp
public interface IReadRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, string? keyword = null);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);
}
```

## 6. 变更历史

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2025-12-05 | v1.0.0 | 初始版本，从 OpenSpec server-code-optimization 提取 |
