# CLAUDE.md

本文件用于指导 Claude Code（claude.ai/code）在本仓库内开展开发工作，请务必遵循以下约定。

## 项目简介
- **项目名称**：凌隐宝堂中医诊所管理系统（LYBTZYZS）
- **总体定位**：面向中医诊所的企业级 .NET 8 解决方案，前端采用 WPF + Prism.DryIoc，后端采用 ASP.NET Core Web API + EF Core，核心契约与工具位于 `src/Shared`。

## 当前状态速览（2025-09-24）
| 项目维度 | 当前结论 |
| --- | --- |
| 编译情况 | ❌ Desktop 端存在事件重复定义，暂无法通过编译 |
| 事件体系 | ⚠️ 多套事件/枚举并存，必须统一到 `UnifiedEvents.cs` |
| 测试现状 | ⚠️ 服务器侧 `dotnet test` 失败；桌面端尚未建立自动化测试基线 |
| 术语一致性 | ⚠️ README、UI 与文档需统一使用“诊疗工作台”等最新术语 |

## 当前最高优先级任务
1. **事件体系统一**：清理 `Core/Events` 目录下所有重复事件与枚举，仅保留权威定义，并统一使用 `StatusMessageType`。
2. **修复资源引用**：检查 `UnifiedDesignSystem.xaml` 中转换器命名空间，确保 `StringToVisibilityConverter` 所在程序集已被 Shell 正确加载。
3. **术语与结构调整**：将“看诊”相关命名改为“诊疗”，梳理 `ConsultationWorkbenchMainView` 的职责，更新 UI 文案及 README。
4. **测试恢复计划**：在完成编译修复后，先解决服务器端失败用例，再为桌面端关键服务（如 `SessionManager`、`UnifiedEventHandler`）补齐首批单元测试。

> 未完成以上事项前，请勿开始新的功能开发。

## 技术栈与架构
### 前端（WPF + Prism.DryIoc）
- 采用 **UltraThink 双层架构**：Module（委托层）+ QueryService（查询）+ BusinessService（业务）。
- 通过角色驱动的工作台（系统工作台 / 诊疗工作台）实现按需加载与导航。
- ViewModel 必须通过接口注入服务，禁止直接解析容器或依赖具体模块实现。

### 后端（ASP.NET Core Web API）
- 延续 **控制器 → 服务 → 仓储** 的三层模式。
- 所有数据访问均使用 `LYBT.Infrastructure` 中的统一 `AppDbContext`。

### 共享层
- DTO、接口、工具位于 `src/Shared`，禁止在前后端重复定义数据结构或服务接口。

## 常用命令（PowerShell）
```powershell
# 还原 / 构建
dotnet restore LYBT.All.sln
dotnet build LYBT.Server.sln -c Release --no-restore
dotnet build LYBT.Desktop.sln -c Release --no-restore

# 运行 WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI

# 代码格式化
dotnet format LYBT.All.sln

# 测试（修复失败后执行）
dotnet test LYBT.Server.sln -c Release
```

## 开发规范要点
- **语言统一**：所有代码注释、终端输出、提交信息均使用中文。
- **依赖注入**：采用构造函数注入接口；禁止在 ViewModel 中使用 `Container.Resolve` 或 `ServiceLocator`。
- **异步规范**：涉及 I/O 的操作必须使用 async/await，避免同步阻塞。
- **文件体量**：建议单文件不超过 500 行，逻辑复杂时应拆分模块。
- **命名约定**：类型与公有成员 PascalCase，私有字段 `_camelCase`，异步方法以 `Async` 结尾。
`n## 工具与效率提升

## 任务交付流程
- Thinker 发布的全部开发任务固定存放在 `docs/tasks/pending/`，文件命名建议为 `YYYY-MM-DD-任务名称.md`，包含背景、目标、验收点。
- Claude Code 在启动任务前，应确认对应任务文件并可在本地记录进展；若任务信息不完整，需先向 Thinker 反馈补充。
- 任务完成后，必须在 `docs/tasks/completed/` 中以同名文件追加 `-summary.md`（或在原任务文件中新增“完成情况”段），总结实现内容、测试结果、遗留风险与后续建议。
- 若任务涉及 README 或其他文档调整，请在总结中明确指出已更新的文件列表，方便 Thinker 审核。

## 测试与质量策略
- 当前桌面端缺少自动化测试，服务器端测试仍有失败用例。
- 优先补齐以下测试：
  - `SessionManager`：验证登录、诊疗状态切换及事件发布。
  - `UnifiedEventHandler`：验证状态消息、错误事件发布逻辑。
  - 关键导航服务与 ViewModel 命令逻辑。
- 推荐技术栈：xUnit、FluentAssertions、Moq、Bogus。
- 阶段目标：在修复失败用例后，将关键模块覆盖率提升至 **≥30%**，再逐步迈向 60%。

## 文档维护要求
1. README.md 由 Thinker 负责维护；Coder 专注代码实现。如发现 README 存在偏差，Thinker 必须优先更新或发布补充任务。
2. 每次调整架构、术语或关键流程时，必须同步更新 `README.md` 及相关 `docs/requirements/*` 文件。
3. `docs/reports/prism-8x-desktop-refactor-plan-2025-09-24.md` 应按 Phase A/B/C/D 的进展实时维护。
4. 本文件若新增约定或排除项，也需同步在 README 中体现。

## 常见陷阱
- 保留多套事件/枚举导致命名冲突与编译失败。
- 在 ViewModel 中直接访问容器或具体实现，破坏可测试性。
- 忽略 Shell 对资源字典的引用，造成转换器解析失败。
- 术语未同步更新，导致 README、UI、代码描述不一致。

## 默认环境信息
- **数据库**：SQL Server（推荐实例：`localhost/LYBTDB`）。
- **API 开发地址**：`http://localhost:5001`。
- **默认账号**：`sysadmin / LybtAdmin2025@SecurePass!`。
- **JWT 配置**：默认有效期 8 小时，记住我模式 30 天。

## Git 提交规范
```text
格式：<类型>(范围): <主题>
常用类型：
- feat：新增功能
- fix：缺陷修复
- refactor：重构（无功能变化）
- docs：文档更新
- test：测试相关变化
- chore：构建、脚本或依赖调整
```

## 语言与沟通要求
- Claude Code 及所有协同工具输出必须为中文。
- 对外表述统一使用“诊疗工作台”等最新术语，避免使用“看诊”旧称呼。
- 在提交代码前，请逐项核对“当前最高优先级任务”是否已完成；如未完成，请先处理阻断项。

请在开始任何编码工作前再次阅读本文件，并确保工作成果与上述要求保持一致。如发现内容与最新需求不符，请立即反馈并更新文档。




