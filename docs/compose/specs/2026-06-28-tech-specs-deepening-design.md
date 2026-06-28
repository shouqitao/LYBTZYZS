# 技术规格深化与约束设计

> **日期**: 2026-06-28
> **范围**: 14 ADR 审计 + 5 项跨文档约束补全
> **状态**: 待审

## [S1] 问题

技术规格文档已形成（14 ADR + 03-architecture/ 31 文件），但存在以下问题：
- 5 个 ADR 缺少明确约束（版本锁定、性能边界、验收标准）
- 5 项跨文档约束未建立（性能 SLO、错误处理交叉引用、并发策略、API 版本控制、缓存约束）
- 部分 ADR 与代码实际状态不一致

## [S2] ADR 深化方案

### ADR-0003（测试策略）—— 补充约束

| 需补充项 | 内容 | 理由 |
|----------|------|------|
| 测试数量下限 | Server ≥1100, Desktop ≥700, Architecture ≥70 | 防止测试退化 |
| 覆盖率目标 | 每个 US 至少 1 个集成测试 | 可追溯性 |
| 回归测试门禁 | 新 PR 必须包含至少 1 个对应 US 的测试 | 质量守门 |

### ADR-0006（ViewModel 分解）—— 补充验证

| 需补充项 | 内容 |
|----------|------|
| 代码现状对照 | 列出当前所有 >500 行的 ViewModel，验证是否已分解 |
| 组件清单 | 列出已实现的 DataManager/CommandHandler/Validator |
| 新 ViewModel 入口检查 | 说明如何在 PR review 中检查合规性 |

### ADR-0007（ViewModel 组合）—— 补充边界

| 需补充项 | 内容 |
|----------|------|
| 禁止项 | 明确禁止在 CoreViewModelBase 中添加业务逻辑 |
| IMasterDetailServices 使用规范 | 何时用组合、何时用继承的决策树 |

### ADR-0008（Token 安全）—— 补充实现状态

| 需补充项 | 内容 |
|----------|------|
| 当前实现状态 | 标注哪些已实现、哪些待补回（D3 B+） |
| 补回优先级 | v1.0 补回项 vs v2.0 项的明确划分 |
| 依赖关系 | Token 安全依赖哪些其他 ADR（如 0004 用户上下文） |

### ADR-0001（聚合根）—— 补充关联 US 完整性

| 需补充项 | 内容 |
|----------|------|
| 遗漏 US | 检查是否有 MedicalCase 相关 US 未在关联列表中 |
| 演进触发条件量化 | 将"500行 Service"改为更具体的指标 |

## [S3] 跨文档约束补全

### 约束 1: 性能 SLO（PRD → Architecture 映射）

PRD 01-prd.md 定义了性能指标，但 03-architecture/ 缺少对应的架构约束。

**补全方案**: 在 `00-architecture-summary.md` 增加「性能约束」节：

```markdown
## 性能约束

| 指标 | SLO | 架构约束 |
|------|-----|----------|
| API 简单查询 | <500ms P95 | EF Core 查询优化 + 连接池 ≥10 连接 |
| API 列表查询 | <1s P95 | 分页必须，禁止全表扫描 |
| API 聚合保存 | <2s P95 | MedicalCase 聚合原子写入 + 索引覆盖 |
| Desktop 启动 | <5s | 模块懒加载 + 启动管线并行化 |
| Desktop 页面切换 | <1s | Region 预加载 + 缓存 |
```

### 约束 2: 错误处理交叉引用

`06-error-handling.md` 与 ADR 无关联。需在以下 ADR 补充错误处理规范：

| ADR | 需补充内容 |
|-----|-----------|
| 0001 聚合根 | MedicalCase 操作异常应抛出 BusinessException，非 ApiException |
| 0004 用户上下文 | GetOperator() 失败时的错误处理策略 |
| 0009 双模式 | 本地模式异常处理与远程模式的差异 |
| 0010 LocalWebAPI | 跨层调用异常如何传播 |

**补全方案**: 在 `06-error-handling.md` 末尾增加「与 ADR 的关联」节。

### 约束 3: 并发控制策略

`04-data-model.md` 的 BaseEntity 有 RowVersion 字段，但无文档化并发策略。

**补全方案**: 在 `04-data-model.md` 增加「并发控制」节：
- 所有写操作必须校验 RowVersion（乐观锁）
- 冲突时抛出 ConflictException
- MedicalCase 聚合根：单活动医案约束 + 乐观锁双重保护

### 约束 4: API 版本控制

当前使用 URL Path Versioning (`/api/v1/`)，但无 ADR 记录此决策。

**补全方案**: 新建 ADR-0015 记录 API 版本控制策略，或在 `01-system-overview.md` 补充说明。

### 约束 5: 缓存策略

`07-configuration.md` 提到内存缓存但无约束。

**补全方案**: 在 `07-configuration.md` 补充：
- 缓存粒度：仅对只读查询启用（患者列表、药材列表、验方列表）
- 缓存失效：写操作后主动清除相关缓存
- 本地模式：不使用缓存（单用户场景）

## [S4] 实施计划

| 批次 | 任务 | 优先级 |
|------|------|--------|
| 1 | ADR-0003 补充测试约束 | P0 |
| 2 | ADR-0001 补充关联 US + ADR-0008 实现状态 | P0 |
| 3 | 性能 SLO 约束（00-architecture-summary.md） | P1 |
| 4 | 错误处理交叉引用（06-error-handling.md） | P1 |
| 5 | 并发控制策略（04-data-model.md） | P1 |
| 6 | API 版本控制（新建 ADR-0015） | P2 |
| 7 | 缓存策略（07-configuration.md） | P2 |
| 8 | ADR-0006/0007 代码验证对照 | P2 |
