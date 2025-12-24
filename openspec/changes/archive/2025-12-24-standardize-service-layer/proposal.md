# OpenSpec Proposal: 标准化Desktop Service层架构

**Change ID**: standardize-service-layer
**Created**: 2025-12-24
**Updated**: 2025-12-24
**Status**: Draft
**Priority**: P1
**Phase**: Post-Release Refactoring
**Spec Deltas**: desktop-architecture (SVC-001 ~ SVC-003)
**Depends On**: unify-desktop-command-handler (已完成)

---

## 问题背景

### 当前架构问题

经过与业界最佳实践对比评估，当前Desktop层数据处理架构存在以下问题：

#### 1. 模式过多，认知负担重

当前有4种模式：
- **CommandHandler**: 无状态CRUD操作
- **AggregateService**: 有状态聚合根管理
- **StateManager**: 有状态简单实体管理
- **Handler**: 专用处理器

标准MVVM架构只需要：**ViewModel → Service → Repository**

#### 2. 划分标准不符合业界规范

| 当前划分标准 | 业界标准 |
|-------------|----------|
| 按"有状态/无状态"划分 | 按"读/写职责"划分 (CQRS) |
| CommandHandler名称暗示CQRS | 实际是CRUD封装，非真正CQRS |
| 按实体复杂度划分 | 按业务领域边界划分 |

#### 3. 命名不符合标准

| 当前命名 | 问题 |
|----------|------|
| `HerbCommandHandler` | "CommandHandler"暗示CQRS模式，但实际不是 |
| `MedicalCaseAggregateService` | "Aggregate"是DDD术语，Desktop层不应使用 |
| `PatientStateManager` | "StateManager"不是标准术语 |

#### 4. Patient模块职责混乱

```
PatientCommandHandler
├── CRUD方法 (CreateAsync, UpdateAsync, DeleteAsync...)
├── UI命令 (SaveCommand, EditCommand, DeleteCommand...)  ← 应在ViewModel
└── 依赖StateManager

PatientStateManager
├── 状态属性 (CurrentPatient, HasChanges...)
└── CRUD方法 (SaveAsync, DeleteAsync...)  ← 与CommandHandler重叠
```

**双层包装问题**：`CommandHandler.ExecuteSaveAsync()` → `StateManager.SaveAsync()` → `Repository.CreateAsync()`

### 业界最佳实践参考

**Microsoft MVVM文档**:
> "ViewModel serves as an integration point with other services such as database-access code"

**StackOverflow高票回答**:
> "你也可以通过将大Services分解成小Services来获得同样的好处"

**Microsoft Azure架构中心**:
> "CQRS is just a small pattern... 适用于复杂领域，不要过度使用"

---

## 目标

### 核心目标

1. **统一为标准Service模式**: 符合业界MVVM最佳实践
2. **简化认知负担**: 4种模式 → 2种模式 (Service + Handler)
3. **命名规范化**: 使用标准术语
4. **消除职责重叠**: 每个模块只有一个Service

### 成功标准

- [ ] 所有CommandHandler重命名为Service
- [ ] AggregateService简化命名为Service
- [ ] Patient模块合并重构（删除StateManager和CommandHandler，新建PatientService）
- [ ] UI命令移至ViewModel
- [ ] 专用Handler保留
- [ ] 编译通过，测试通过

---

## 重构方案

### 目标架构

```
┌─────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  UI Commands: SaveCommand, EditCommand, DeleteCommand │  │
│  │  (从Service层移入，ViewModel自行定义)                   │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                           │
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                    Standard Services                     ││
│  │  (统一命名: {Entity}Service)                              ││
│  │                                                          ││
│  │  HerbService | FormulaService | PatientService           ││
│  │  UserService | ConsultationService | MedicalCaseService  ││
│  └─────────────────────────────────────────────────────────┘│
│                                                              │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              Specialized Handlers                        ││
│  │  (保留: 专用领域逻辑)                                     ││
│  │                                                          ││
│  │  NavigationHandler | LifecycleHandler | ImportHandler    ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Repository Layer                          │
│  IXxxRepository → API Client                                 │
└─────────────────────────────────────────────────────────────┘
```

### 模式简化

| 原模式 | 新模式 | 操作 |
|--------|--------|------|
| CommandHandler | **Service** | 重命名 |
| AggregateService | **Service** | 重命名 |
| StateManager | **Service** | 合并删除 |
| Handler | **Handler** | 保留 |

---

## 详细变更计划

### Phase 1-3: 简单模块重命名

| 模块 | 原类名 | 新类名 | 操作 |
|------|--------|--------|------|
| Herbs | `HerbCommandHandler` | `HerbService` | 重命名 |
| Formula | `FormulaCommandHandler` | `FormulaService` | 重命名 |
| Consultation | `ConsultationCommandHandler` | `ConsultationService` | 重命名 |

### Phase 4: Patient模块合并重构

| 原类 | 操作 | 原因 |
|------|------|------|
| `PatientCommandHandler` | **删除** | 混合Service和ViewModel职责 |
| `PatientStateManager` | **删除** | 与CommandHandler职责重叠 |
| - | **新建PatientService** | 整合CRUD + 状态管理 |

**合并逻辑**:
- CRUD操作 ← CommandHandler
- 状态属性 ← StateManager
- UI命令 → 移至ViewModel

### Phase 5: Users模块重命名

| 原类名 | 新类名 | 操作 |
|--------|--------|------|
| `UserCommandHandler` | `UserService` | 重命名 |

### Phase 6: MedicalCase模块简化

| 原类名 | 新类名 | 操作 |
|--------|--------|------|
| `MedicalCaseAggregateService` | `MedicalCaseService` | 重命名 |
| `MedicalCaseCommandHandler` | - | 合并到MedicalCaseService后删除 |

### 保留不变

| 类 | 保留原因 |
|----|----------|
| `MedicalCaseNavigationHandler` | 专用导航逻辑 |
| `MedicalCaseLifecycleHandler` | 专用生命周期管理 |
| `PrescriptionSaveHandler` | 专用保存逻辑 |
| `PrescriptionItemHandler` | 专用处方项操作 |
| `PrescriptionImportHandler` | 专用导入逻辑 |
| `UnfinishedCaseHandler` | 专用未完成病例处理 |

---

## 接口设计规范

### 标准Service接口

```csharp
public interface IHerbService
{
    // 查询
    Task<HerbDto?> GetByIdAsync(int id);
    Task<PagedResult<HerbDto>> GetPagedAsync(HerbQueryDto query);

    // 命令 - 统一返回元组
    Task<(bool Success, HerbDto? Data, string? Error)> CreateAsync(HerbInputDto input);
    Task<(bool Success, HerbDto? Data, string? Error)> UpdateAsync(int id, HerbInputDto input);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
```

### 聚合根Service接口

```csharp
public interface IMedicalCaseService
{
    // 状态属性
    MedicalCaseDetailDto? Current { get; }
    bool HasChanges { get; }

    // 生命周期
    Task InitializeAsync(int medicalCaseId);
    Task ReloadAsync();

    // 聚合根操作
    Task<(bool Success, string? Error)> SaveAsync();
    Task<(bool Success, MedicalCaseDetailDto? Data, string? Error)> CreateAsync(...);
}
```

---

## 影响范围

### 文件变更统计

| 类别 | 数量 | 说明 |
|------|------|------|
| 删除文件 | 4 | PatientCommandHandler, PatientStateManager, MedicalCaseCommandHandler及其接口 |
| 重命名文件 | 10 | CommandHandler/AggregateService → Service |
| 新建文件 | 2 | PatientService + IPatientService |
| 修改文件 | ~25 | 模块注册 + ViewModel引用 |

### 不影响的部分

- 业务逻辑不变
- API契约不变
- Repository层不变
- Server端不变

---

## 实施计划

| Phase | 模块 | Tasks | 核心操作 |
|-------|------|-------|----------|
| 1 | Herbs | 4 | 重命名CommandHandler → Service |
| 2 | Formula | 4 | 重命名CommandHandler → Service |
| 3 | Consultation | 4 | 重命名CommandHandler → Service |
| 4 | Patients | 8 | **合并重构** (删除2类，新建1类) |
| 5 | Users | 4 | 重命名CommandHandler → Service |
| 6 | MedicalCase | 8 | 简化命名 + 合并CommandHandler |
| 7 | 验证与文档 | 8 | 编译测试 + 文档更新 |

**总计**: 7 Phases, 40 Tasks

详见: `tasks.md`

---

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 大量引用更新 | 高 | 使用IDE重构工具，分模块执行 |
| Patient模块合并复杂 | 高 | 先分析职责，明确合并策略 |
| 遗漏引用 | 中 | 编译验证 + 全局搜索 |
| 功能回归 | 中 | 保持业务逻辑不变，仅重构结构 |

---

## 与enhance-dataflow-logging的关系

本提案完成后，日志提案简化为：

| 层级 | 日志前缀 |
|------|----------|
| Service | [SVC] |
| Handler | [HDL] |

原来的[CMD]、[AGG]、[STATE]全部统一为[SVC]。

**建议执行顺序**: 先完成本提案 → 再执行日志覆盖提案

---

## 参考文档

- [Microsoft MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm)
- [Prism Library Documentation](https://prismlibrary.com/docs/)
- [CQRS Pattern - Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [WPF Best Practices 2024](https://blog.postsharp.net/wpf-best-practices-2024)

---

## 审批

- [ ] 技术方案评审
- [ ] 用户确认
- [ ] 开始实施
