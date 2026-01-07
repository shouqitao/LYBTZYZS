# OpenSpec Proposal: standardize-service-naming

**Change ID**: standardize-service-naming
**Status**: applied
**Priority**: Medium
**Estimated Effort**: 3h (实际: 15min)
**Created**: 2025-12-30
**Applied**: 2025-12-30

---

## 1. Problem Statement

### 1.1 Current Situation

Desktop层服务命名存在混乱：

| 后缀 | 数量 | 示例 |
|------|------|------|
| Service | 20+ | ApiService, AuthenticationService, HerbService |
| Handler | 8+ | TokenRefreshHandler, LoggingHttpHandler, HerbCommandHandler |
| Manager | 5+ | TokenManager, MenuManager, LoadingStateManager |
| Coordinator | 2 | MedicalCaseWorkspaceCoordinator |

### 1.2 Problems

1. **语义混乱**: Service/Handler/Manager概念边界不清
2. **命名不一致**: 相似功能使用不同后缀
3. **查找困难**: 不知道某功能应该找哪个后缀的类

---

## 2. Solution

### 2.1 Naming Convention (命名规范)

| 后缀 | 适用场景 | 特征 |
|------|----------|------|
| **Service** | 业务逻辑服务 | 无状态、提供业务操作、返回Result |
| **Handler** | HTTP处理/事件处理 | 拦截器、中间件、事件订阅者 |
| **Manager** | 状态管理 | 有状态、管理生命周期、单例 |
| **Coordinator** | 复杂编排 | 协调多个Service/Manager、工作流 |
| **Repository** | 数据访问 | CRUD操作、API调用封装 |

### 2.2 Migration Plan

#### Phase 1: 保持现有命名（最小改动）
当前代码基本符合规范，只需要：
1. 文档化命名规范
2. 新增代码遵循规范
3. 明显违规的逐步迁移

#### Phase 2: 规范违例清理

| 当前名称 | 问题 | 建议 |
|---------|------|------|
| HerbCommandHandler | 不是HTTP/事件Handler | 保留（已有迁移到Service的趋势） |
| LoadingStateManager | 符合规范 | 保留 |
| TokenManager | 符合规范 | 保留 |

### 2.3 Documentation

创建 `docs/desktop-naming-conventions.md` 文档化规范：

```markdown
# Desktop Layer Naming Conventions

## Service Classes

### Service
- 业务逻辑服务
- 无状态设计
- 依赖Repository进行数据访问
- 返回统一Result类型

### Handler
- HTTP消息处理器（继承DelegatingHandler）
- 事件处理器（EventAggregator订阅）
- 命令处理器（逐步迁移为Service）

### Manager
- 状态管理类
- 生命周期管理
- 通常为单例

### Coordinator
- 复杂业务流程编排
- 协调多个Service/Manager
- 维护工作流状态

### Repository
- 数据访问抽象
- API调用封装
- 返回DTO/Entity
```

---

## 3. Implementation

### 3.1 Tasks

1. [x] 创建命名规范文档 → `docs/development/desktop-naming-conventions.md`
2. [x] 审查现有命名违例 → 现有代码基本符合规范
3. [ ] 更新CLAUDE.md添加命名规范引用 (可选，规范已在docs目录)
4. [ ] （可选）重命名明显违规的类 → 无需重命名

### 3.2 Scope Control

此提案聚焦于**文档化和规范制定**，不进行大规模重命名。

理由：
- 重命名影响范围大
- 现有代码基本符合规范
- 优先保证新代码遵循规范

---

## 4. Validation

### 4.1 Acceptance Criteria

- [ ] 命名规范文档已创建
- [ ] CLAUDE.md已更新
- [ ] 新增代码遵循规范（PR Review检查点）

---

## 5. Dependencies

- None (独立提案)

---

## 6. Timeline

| Phase | Task | Duration |
|-------|------|----------|
| 1 | 创建命名规范文档 | 1h |
| 2 | 审查现有代码 | 1h |
| 3 | 更新项目文档 | 30min |
| 4 | Review和调整 | 30min |
