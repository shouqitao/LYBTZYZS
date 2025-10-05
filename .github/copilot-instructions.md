# GitHub Copilot 代码审查指令

本文件定义 GitHub Copilot 在代码审查中的行为与标准，确保与 Claude Code Review 保持一致。

---

## 审查重点

### 1. 架构合规性（最高优先级）

**检查项**:
- ✅ 严格遵守 **Record-Only 系统约束**（仅 CRUD + 历史查询）
- ❌ **禁止使用**:
  - Docker, Kubernetes
  - Redis
  - CQRS, MediatR
  - Event-Driven Architecture
  - 微服务架构
  - GraphQL, gRPC
  - 消息队列（RabbitMQ/Kafka）
- ✅ 符合 `.ai/rules.json` 和 `_governance/architecture.md` 定义的技术标准

### 2. C# / .NET 8 最佳实践

**检查项**:
- 命名规范:
  - `PascalCase` - 类型、公开成员
  - `_camelCase` - 私有字段
  - `UPPER_SNAKE_CASE` - 常量
- 异步约定:
  - 涉及 I/O 必须 `async/await`
  - 异步方法名以 `Async` 结尾
- 依赖注入:
  - 仅使用构造函数注入
  - 禁止 `ServiceLocator` 或 `Container.Resolve()`
- 资源管理:
  - 正确使用 `IDisposable` 和 `using`
- Nullable 引用类型:
  - 正确处理可空性注解

### 3. WPF / Prism MVVM 规范

**检查项**:
- ViewModel:
  - 必须继承 `BindableBase` 或实现 `INotifyPropertyChanged`
- 导航:
  - 使用 Prism `IRegionManager`
  - 禁止直接 `new Window()`
- 对话框:
  - 使用 Prism `IDialogService`
  - 禁止 `MessageBox.Show()`
- 事件聚合:
  - 使用 `IEventAggregator`
- 命令:
  - 使用 `DelegateCommand` 或 `DelegateCommand<T>`

### 4. 代码质量

**检查项**:
- 单个文件 ≤500 行
- 方法圈复杂度 ≤10
- 避免重复代码（DRY 原则）
- 清晰的注释（复杂逻辑必须注释）
- 魔法数字提取为常量

### 5. 安全性

**检查项**:
- SQL 注入防护（参数化查询）
- 敏感信息不得硬编码
- 输入验证与清理
- 正确的异常处理（不泄露堆栈信息）

### 6. 性能

**检查项**:
- 避免 N+1 查询（EF Core）
- 合理使用缓存（`IMemoryCache`）
- 大集合操作使用 LINQ 延迟执行
- WPF UI 操作在 UI 线程，耗时操作异步化

### 7. 测试覆盖

**检查项**:
- 核心逻辑必须有单元测试
- 测试命名清晰（Given_When_Then 模式）
- 边界条件与异常场景测试

### 8. 文档同步

**检查项**:
- 架构变更需更新 `docs/architecture/` 文档
- API 变更需更新 `docs/api/` 文档
- 新增功能需更新模块 README

---

## 审查输出格式

建议使用以下格式提供反馈：

```markdown
💡 Copilot 建议:

### 代码改进
- [建议描述]

### 潜在问题
- [问题描述]

### 最佳实践
- [实践建议]
```

---

## 严重程度标注

- 🔴 **严重问题**: 违反架构约束、安全风险、必须修复
- 🟡 **建议问题**: 违反最佳实践、性能问题、建议修复
- 🟢 **优化建议**: 代码改进机会、可选修复

---

## 与 Claude Code Review 的协同

- **Claude Code**: 全面代码质量检查（架构合规、安全性、性能）
- **Copilot**: 补充性建议（代码简洁性、潜在问题、.NET 新特性使用）

两者共同确保代码质量，Copilot 应关注 Claude 可能遗漏的细节改进机会。

---

**相关文档**:
- [代码审查指南](../docs/development/code-review-guidelines.md)
- [开发标准](../docs/development/standards.md)
- [分支保护配置](../docs/development/branch-protection-setup.md)

---

**最后更新**: 2025-10-05
**维护人**: Claude Code + GitHub Copilot
