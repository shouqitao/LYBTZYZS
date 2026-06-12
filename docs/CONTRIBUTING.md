# 贡献指南

---

## 提交规范

### 提交消息格式

```
<type>(<scope>): <description>
```

**类型**:

| 类型 | 用途 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug修复 |
| `refactor` | 重构（不改功能） |
| `test` | 测试相关 |
| `docs` | 文档 |
| `chore` | 构建、依赖、CI |
| `style` | 格式（不影响逻辑） |

**示例**:
```
feat(medicalcase): add prescription completeness checker
fix(patient): resolve duplicate import skip logic
refactor(herbs): align batch-import from Stream to DTO pattern
docs: add developer onboarding guide
```

---

## 分支策略

```
main          ← 稳定分支，PR合并目标
├── feat/xxx  ← 新功能
├── fix/xxx   ← Bug修复
└── refactor/xxx ← 重构
```

- 从 `main` 创建分支
- 一个分支一个关注点
- 完成后创建 PR

---

## PR 规范

### 创建PR前

```bash
dotnet build LYBTZYZS.sln          # 0 errors
dotnet test LYBTZYZS.sln           # 全部通过
gitnexus_detect_changes()           # 确认变更范围
```

### PR标题

与提交消息格式一致：`<type>(<scope>): <description>`

### PR描述模板

```markdown
## 变更内容
- 做了什么
- 为什么做

## 测试
- [ ] 新增/更新测试
- [ ] dotnet build 通过
- [ ] dotnet test 通过

## 影响范围
- 涉及模块
- 是否有破坏性变更
```

---

## 代码规范

### 硬性规则

| 规则 | 说明 |
|------|------|
| 三层架构 | Controller → Service → Repository → DbContext |
| Service禁止注入DbContext | 必须通过 Repository 接口 |
| 跨模块禁止引用 | Server模块间 / Desktop模块间禁止直接引用 |
| API统一响应 | 所有Controller返回 `ApiResponse<T>` |
| 软删除 | `IsDeleted` 全局过滤器 |
| Mapperly映射 | 编译时源生成，禁止反射映射 |

### 编码风格

- 遵循 `.editorconfig` 规则
- Nullable reference types: enabled
- C# 12 / .NET 8
- 分析器警告视为错误

### 命名约定

| 类型 | 约定 | 示例 |
|------|------|------|
| 实体 | PascalCase, 单数 | `Herb`, `MedicalCase` |
| DTO | PascalCase + Dto后缀 | `HerbListDto`, `HerbInputDto` |
| 接口 | I前缀 | `IHerbRepository`, `IHerbService` |
| Repository | internal class | `HerbRepository` |
| Service | public class | `HerbService` |
| Controller | 复数 + Controller | `HerbsController` |
| 模块 | Module后缀 | `HerbsModule` |

---

## 测试要求

### 测试策略

- **集成优先**: 真实数据库，零Mock
- Server测试: 真实SQL Server + Respawn清理
- Desktop测试: SQLite InMemory
- 架构测试: 守卫架构规则

### 覆盖要求

| 场景 | 最低覆盖 |
|------|---------|
| 新功能 | 必须有测试 |
| Bug修复 | 必须有回归测试 |
| 重构 | 原有测试必须继续通过 |

### 运行测试

```bash
dotnet test tests/LYBT.Tests.Server/        # 服务端
dotnet test tests/LYBT.Tests.Desktop/       # 桌面
dotnet test tests/LYBT.Tests.Architecture/  # 架构守卫
```

---

## 工作流

详见 [`05-development/02-workflow.md`](05-development/02-workflow.md)（OpenSpec + Superpowers + GSD 工具链）。

---

## 相关文档

| 文档 | 说明 |
|------|------|
| [ONBOARDING.md](ONBOARDING.md) | 新人引导 |
| [DEVELOPER-GUIDE.md](DEVELOPER-GUIDE.md) | 开发者总入口 |
| [05-development/](05-development/) | 编码标准、开发流程 |
| [07-concepts/](07-concepts/) | 技术概念索引 |

---

## 变更记录
| 日期 | 变更 |
|------|------|
| 2026-06-12 | 初始版本 |
