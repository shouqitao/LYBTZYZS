# 架构文档索引

- **维护人**：Thinker（ChatGPT）
- **最后更新**：2025-09-25

本目录收录架构相关资料，包含架构总览、决策记录（ADR）、专题分析与实施指南。建议阅读顺序：总览 → ADR → 最新分析/计划。

## 快速索引
| 文档 | 说明 |
|------|------|
| `overview.md` | 系统整体架构概览、技术栈与关键组件。 |
| **`system-architecture-design.md`** | **系统架构设计文档v3.0** - 完整的架构设计规范 |
| **`functional-modules-design.md`** | **功能模块详细设计v2.0** - 所有模块的详细设计 |
| `ADR-001-cqrs-mediatr-rejection.md` | CQRS + MediatR 拒绝决策。 |
| `ADR-002-technology-roadmap-suggestion.md` | 技术路线与阶段目标建议。 |
| `desktop-architecture-guide.md` | 桌面模块化架构实现指南。 |
| `desktop-refactoring-plan.md` | 桌面架构重构计划。 |
| `Arch-Discussion-Multi-Tenancy-2025-09-23.md` | 多租户讨论记录。 |
| **[modules/](modules/README.md)** | **模块化设计文档集合** - **Server/Client/Shared层详细设计** |

## 衔接其他资料
- **技术标准与规范**：`docs/development/technical-standards.md`
- 最新架构分析报告：`docs/reports/architecture-analysis-2025-09-25.md`
- 架构改进建议：`docs/reports/modification-suggestions-2025-09-25.md`
- 相关任务：参见 `docs/tasks/pending/2025-09-24-all-framework-refactor-task.md`

## 维护规则
1. 新增架构决策须产出 ADR（`ADR-XXX-标题.md`），并在此表格登记。
2. 过期或被取代的指南应在文首标注“历史版本”，必要时移动到 `docs/reports/archive/`。
3. 每次架构评审后，更新“衔接其他资料”中的链接，确保决策链条完整。

# 架构文档（概览）

版本：v1.0  
维护：Claude Code

## 关键入口（证据）
- WebAPI 入口：`src/Server/Services/LYBT.WebAPI/Program.cs:39`（创建 Builder）
- 统一注册：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs:22`（RegisterAllApplicationServices）
- 统一初始化：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedApplicationInitialization.cs:16`（InitializeAllApplicationServices）
- 桌面入口：`src/Client/Desktop/Shell/App.xaml.cs:44`（CreateShell）

## 分层
- Server：Controllers/Services/Core/Modules（EF Core、缓存、鉴权）
- Client：Shell/Core/Infrastructure/Modules（Prism 模块化）
- Shared：Models/Interfaces/Utilities（契约与工具）

## 维护规则
- 入口与装配变更需同步更新此处证据行号
- 架构决策以 ADR 文档为准（architecture/ADR-*.md）
