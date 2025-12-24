# Desktop Architecture Spec Deltas

## ADDED Requirements

### Requirement: SVC-001 标准Service接口规范

Desktop层业务服务 **MUST** 遵循标准Service命名和接口规范。

**接口命名规则**:
- 接口名: `I{Entity}Service` (如 `IHerbService`, `IPatientService`)
- 实现名: `{Entity}Service` (如 `HerbService`, `PatientService`)
- 文件位置: `Services/` 目录

**方法签名规范**:
- 查询方法: 返回 `Task<T?>` 或 `Task<PagedResult<T>>`
- 命令方法: 返回 `Task<(bool Success, T? Data, string? Error)>` 元组

#### Scenario: 简单实体Service

**Given** 一个简单业务实体（如Herb、Formula、User）
**When** 创建对应的Service类
**Then** 必须遵循以下规范：
- 类名为 `{Entity}Service`
- 实现 `I{Entity}Service` 接口
- 位于 `Services/` 目录
- 包含标准CRUD方法

---

### Requirement: SVC-002 聚合根Service规范

管理聚合根及其子实体的Service **MUST** 遵循聚合根Service规范。

**状态管理**:
- 必须提供 `Current` 属性访问当前聚合根
- 必须提供 `HasChanges` 属性检测变更
- 必须提供 `InitializeAsync` / `ReloadAsync` 生命周期方法

**子实体操作**:
- 通过聚合根Service统一管理子实体
- 子实体更新使用 `Update{SubEntity}(Action<T>)` 模式

#### Scenario: 聚合根Service

**Given** 一个DDD聚合根实体（如MedicalCase）
**When** 创建对应的Service类
**Then** 必须遵循以下规范：
- 类名为 `{AggregateRoot}Service`
- 提供 `Current` / `HasChanges` 状态属性
- 提供子实体访问属性（如 `CurrentConsultation`）
- 提供子实体更新方法

---

### Requirement: SVC-003 专用Handler规范

专用领域逻辑处理器 **MUST** 保留Handler命名，用于不适合放入通用Service的特定操作。

**适用场景**:
- 导航协调逻辑
- 生命周期管理
- 导入/导出操作
- 跨服务协调

**命名规则**:
- 类名: `{Domain}{Operation}Handler` (如 `MedicalCaseNavigationHandler`)
- 无需接口（通常直接注入使用）

#### Scenario: 专用Handler

**Given** 一个特定领域操作（如导航、导入）
**When** 该操作不适合放入通用Service
**Then** 创建专用Handler：
- 类名为 `{Domain}{Operation}Handler`
- 位于 `Services/` 目录
- 职责单一，专注特定操作

---

## REMOVED Requirements

- **CMD-001**: CommandHandler模式 - 废弃，统一使用Service命名
- **AGG-001**: AggregateService模式 - 废弃，简化为Service命名
- **STATE-001**: StateManager模式 - 废弃，统一使用Service命名
