# Status vs IsDeleted 概念区分

> 创建日期: 2025-11-29
> 状态: 规范文档

## 概述

本文档澄清项目中两个易混淆的概念：`CommonStatus`（启用/禁用）和 `IsDeleted`（软删除），并定义它们的正确使用场景。

## 核心概念

### 1. IsDeleted（软删除）

**定义**：记录被逻辑删除，用户认为已删除，但数据保留在数据库中。

**特征**：
- 用户操作：点击"删除"按钮
- 用户感知：记录已不存在
- 数据状态：数据库保留，标记 `IsDeleted = true`
- 恢复方式：管理员可恢复，或用于审计追溯
- 查询过滤：默认全局过滤，不显示已删除记录

**实现位置**：`BaseEntity` 基类
```csharp
public abstract class BaseEntity : IAuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

### 2. CommonStatus（启用/禁用）

**定义**：记录的可用状态控制，用户知道记录存在但当前不可用。

**特征**：
- 用户操作：点击"禁用/启用"按钮
- 用户感知：记录存在，但被标记为"已禁用"
- 数据状态：`Status = CommonStatus.Disabled`
- 恢复方式：随时可切换启用/禁用
- 查询过滤：可选过滤，列表可显示禁用记录（灰色/标记）

**枚举定义**：
```csharp
public enum CommonStatus
{
    [Description("禁用")]
    Disabled = 0,

    [Description("启用")]
    Enabled = 1
}
```

## 业务场景对比

| 场景 | IsDeleted（软删除） | Status（禁用） |
|------|-------------------|---------------|
| 用户离职 | 不适用 | 禁用账号，保留历史记录 |
| 药材停用 | 不适用 | 禁用药材，不出现在选择列表 |
| 删除错误患者 | 软删除 | 不适用 |
| 验方暂停使用 | 不适用 | 禁用验方 |
| 删除测试数据 | 软删除 | 不适用 |

## 项目中的Entity分析

### 使用 CommonStatus 的Entity

| Entity | 字段 | 业务含义 | 是否合理 |
|--------|------|---------|---------|
| `UserModel` | Status | 用户账号启用/禁用 | 合理 |
| `PatientModel` | Status | 患者启用/禁用 | 合理 |
| `HerbModel` | Status | 药材启用/禁用 | 合理 |
| `FormulaModel` | Status | 验方启用/禁用 | 合理 |
| `ConsultationModel` | Status | 问诊状态 | **待审视** |
| `AuthSessionModel` | Status | 会话有效性 | 合理 |

### 已移除 Status 的Entity

| Entity | 原字段 | 移除原因 | 替代方案 |
|--------|--------|---------|---------|
| `MedicalCase` | Status | 与 CaseStatus 功能重叠 | 使用 CaseStatus + IsDeleted |

**MedicalCase 分析**：
- `MedicalCaseStatus` 枚举描述业务流程：Draft(暂存) → Active(进行中) → Completed(已完成) / Cancelled(已取消)
- 这是生命周期状态，不是启用/禁用概念
- 软删除使用继承自 `BaseEntity` 的 `IsDeleted` 字段
- 因此移除 `CommonStatus Status` 是合理的

## 设计原则

### 何时使用 IsDeleted

1. 用户执行"删除"操作
2. 记录不应再出现在任何列表中
3. 需要保留数据用于审计或恢复
4. 记录有关联数据，硬删除会破坏引用完整性

### 何时使用 Status

1. 记录需要临时不可用但不删除
2. 用户需要看到记录存在但被禁用
3. 状态可能频繁切换（启用↔禁用）
4. 禁用记录在某些场景下仍需显示（如历史查看）

### 两者可以共存

```csharp
public class HerbModel : BaseEntity
{
    // 软删除 - 来自 BaseEntity
    public bool IsDeleted { get; set; }

    // 启用/禁用 - 业务状态
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}
```

**查询逻辑**：
```csharp
// 标准查询：排除已删除，只显示启用
query.Where(h => !h.IsDeleted && h.Status == CommonStatus.Enabled)

// 管理查询：排除已删除，显示全部状态
query.Where(h => !h.IsDeleted)

// 审计查询：显示全部（包括已删除）
query.IgnoreQueryFilters()
```

## UI 表现

### IsDeleted
- 记录不显示在列表中
- 无法通过常规搜索找到
- 仅管理员可在"回收站"或审计日志中查看

### Status = Disabled
- 记录显示在列表中，但有视觉标记（灰色、标签等）
- 可通过筛选器显示/隐藏
- 在选择器中不可选（如选择药材时）

## 代码示例

### 正确的用户禁用流程
```csharp
// 禁用用户 - 保留记录，禁止登录
public async Task DisableUserAsync(Guid userId)
{
    var user = await _repository.GetByIdAsync(userId);
    user.Status = CommonStatus.Disabled;
    await _repository.UpdateAsync(user);
}
```

### 正确的用户删除流程
```csharp
// 删除用户 - 软删除，用于错误数据清理
public async Task DeleteUserAsync(Guid userId)
{
    var user = await _repository.GetByIdAsync(userId);
    user.IsDeleted = true;
    user.DeletedAt = DateTime.UtcNow;
    await _repository.UpdateAsync(user);
}
```

## 行业最佳实践

根据业界通用做法：

1. **软删除(Soft Delete)** 是一种数据持久化策略，用于防止永久删除记录
2. **启用/禁用状态** 是一种业务状态管理，用于控制记录的可用性
3. 两者是**正交的概念**，不应相互替代
4. 对于有生命周期的实体，可以将"归档"作为生命周期的一部分

## 结论

**Status（启用/禁用）和 IsDeleted（软删除）是两个不同的业务概念，不应用 IsDeleted 替代 Status。**

- MedicalCase 移除 Status 是因为它有专门的 `CaseStatus` 枚举管理生命周期
- 其他模块（User、Patient、Herb、Formula 等）保留 Status 字段是合理的

## 参考资料

- [Soft Delete in EF Core](https://www.milanjovanovic.tech/blog/implementing-soft-delete-with-ef-core)
- [JetBrains - Soft Delete Strategy](https://blog.jetbrains.com/dotnet/2023/06/14/how-to-implement-a-soft-delete-strategy-with-entity-framework-core/)
- [Cultured Systems - Avoiding Soft Delete Anti-pattern](https://www.cultured.systems/2024/04/24/Soft-delete/)
