# 项目指导思想（Guiding Philosophy）

本指南是整个凌隐宝堂项目的“作战手册”。它明确我们的价值观、技术边界与日常开发的通用做法，确保多人协作时“目标一致、路径清晰、节奏稳定”。

## 一、核心价值观
- 稳定优先：在保证现有功能稳定的前提下演进，避免一次性大改。
- 最小变更：重构以“可读、可测、可维护”为目标，控制变更面，严格验证回归。
- 分层清晰：UI/应用层（Desktop）只做表现与编排；数据契约集中在 Shared；服务逻辑在 Server。
- 统一约束：统一的构建、测试、风格、版本与依赖管理，消除“隐性分叉”。
- 可回溯：以文档与架构测试守门，变更可解释、可追踪、可回滚。

## 二、架构与边界
- 分层结构
  - Server：WebAPI | 业务模块（Auth/Users/Patients/MedicalCase/Consultation/Prescriptions/Herbs/Formula）
  - Desktop：Shell/Core/Infrastructure/Services/Workbenches/Modules（Prism.DryIoc 8.1.97）
  - Shared：Models（DTO/枚举）| Interfaces（业务接口/Refit API）| Utilities
- 关键约束
  - DTO 的唯一来源是 Shared（禁止在 Desktop/Server 侧再造“影子 DTO”）。
  - Desktop 不得依赖 Entities/Infrastructure/WebAPI 宿主（以 NetArchTest 守门）。
  - API 路由统一 `/api/v1/*`，采用 API Versioning 标注。
  - 统一序列化为 System.Text.Json；Refit 使用 SystemTextJsonContentSerializer。

## 三、Desktop 现代化（不加新功能前提下的重构）
- 目标与原则
  - 不新增业务功能，只做架构与体验一致性的收敛。
  - 渐进式替换，保持现有 XAML 绑定名与交互逻辑不变。
- 基类与命令收敛
  - 从 `NewBaseListViewModel<T>` 迁移到 `ModernManagementViewModel<T>`。
  - 基类统一提供：检索/增删改/查看明细/刷新/分页命令与状态；页面可按需添加 `First/Last` 兼容命令。
- 资源与样式统一
  - 转换器/颜色/控件样式集中在 `Shell/Resources/UnifiedDesignSystem.xaml`。
  - 删除/避免页面/模块级重复转换器（如 BooleanToVisibilityConverter），缺失的全局转换器补齐（如 EmptyStringToVisibilityConverter）。
- 事件与导航
  - 精简 EventAggregator 事件通道，仅保留必要导航/数据刷新/工作流步骤事件。
  - 工作流模型仅保留 `WorkflowStep` 枚举；其余冗余类型移除。
- 命名与模型
  - UI 专用模型用 `*Item/*ViewState/*Info` 命名，避免 `*Dto` 以免与跨层契约混淆。
  - 统一角色/状态等枚举来源于 Shared；逐步移除 Legacy 枚举与映射逻辑。
- 分阶段路线（示例）
  - P0 低风险：统一转换器与资源，删除未用事件与影子 DTO（已启动）。
  - P1 体系化：全量迁移使用 `NewBaseListViewModel` 的 VM，角色/状态收敛到 Shared，裁剪事件。
  - P2 体验优化：统一控件模板与主题，补充必要 XML 注释，评估 Prism 升级路径。

## 四、构建、测试与守门
- 构建/运行
  - 还原：`dotnet restore LYBT.All.sln`
  - 构建：`dotnet build LYBT.All.sln -c Release --no-restore`
  - 运行 API：`dotnet run --project src/Server/Services/LYBT.WebAPI`
  - 运行 Desktop（调试）：启动 `LYBT.Desktop.Shell`（WPF/Prism）
- 代码风格
  - C# 4 空格缩进；UTF‑8、CRLF；`System.*` using 置顶；花括号换行；命名遵循：类型/成员 PascalCase、接口前缀 I、私有字段 `_camelCase`、异步方法 `*Async`。
  - 启用 StyleCop.Analyzers；修复警告或给出合理抑制。
- 测试
  - 单元/集成：xUnit、FluentAssertions、Moq、Verify、NetArchTest；Coverlet 收集覆盖率。
  - 命令：`dotnet test tests -c Release --no-build`；覆盖率：`--collect:"XPlat Code Coverage"`。
  - 架构测试：确保 Desktop 不依赖 Entities/Infrastructure/WebAPI，Server 控制器路由合规等。

## 五、提交与评审
- 提交：Conventional Commits（例：`refactor(desktop): unify converters …`），变更原子、信息完整。
- PR 要求：通过全部测试与架构测试；必要的文档/截屏随改随更；变更说明清晰可回放。
- 质量闸门：禁止引入被禁框架；固定 SDK/包版本由 `global.json`/`Directory.Packages.props` 统一管理。

## 六、安全与配置
- 禁止提交密钥；使用本地 `appsettings.Development.json` 或环境变量。
- EF Core 优先隐式事务；显式事务最小化与小范围；配置文件经 `docs/configuration.md` 管理与解释。

## 七、文档组织与贡献
- 文档导航：见 `docs/index.md`；本页为“指导思想”，其余为分册（架构/开发/测试/交付/安全/模块）。
- 文档约定：与代码同生命周期，功能/接口/风格变更必须同步更新文档。
- 贡献方式：PR 附带对应文档变更，遵守现有结构与命名；评审人可按此页价值观进行一致性审查。

> 做正确的事（边界/约束/一致性） + 把事做对（最小变更/可验证/可回溯），是我们交付高质量医疗信息系统的“底层逻辑”。

