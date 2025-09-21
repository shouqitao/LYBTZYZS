# PRD——LYBT.Desktop.sln 桌面解决方案“快速修复清单”落地（CCPM）

- 文档日期：2025-09-21
- 项目经理：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.Desktop.sln`、`src/Client/Desktop/*`、`src/Shared/*`、`Directory.Packages.props`、`Directory.Build.props`

## 背景（Problem & Context）
- 构建失败：
  - 缺少 `Microsoft.Extensions.ObjectPool` 包，`ObjectPool<T>`/`IPooledObjectPolicy<T>` 无法解析（`src/Client/Desktop/Core/ObjectPool/ObjectPoolService.cs`）。
  - 命名冲突：命名空间 `LYBT.Desktop.Core.ObjectPool` 与类型名 `ObjectPool<T>` 同名，触发“命名空间不能与类型参数一起使用”。
- 还原告警：`Directory.Packages.props` 中 `coverlet.collector` 定义重复（NU1506）。
- 输出目录不一致：多个项目将 `<DocumentationFile>` 指向项目内 `bin/...`，与统一产物 `BIN/` 策略不符。
- JSON 序列化混用：后端与基础设施偏向 System.Text.Json，但仍保留 `Refit.Newtonsoft.Json` 与个别 `Newtonsoft.Json` 依赖。
- 配置冗余：`LYBT.Desktop.Services` 启用 `UseWindowsForms`，无实际需要；`Directory.Build.props` 中 `PrismVersion` 未被使用且与实际版本不一致。
- WPF 资源包含方式存在重复/不一致风险：Shell 通过通配 `<Resource Include="..\\Themes\\*.xaml">` 收集 XAML，可能与 SDK 默认 Page 规则冲突。

## 目标（Goals）
- 修复构建错误与还原告警，使 `LYBT.Desktop.sln` 在 Debug/Release 下可稳定构建。
- 与仓库规范对齐：
  - 统一产物输出到 `BIN/`；
  - 中央包管理无重复；
  - JSON 序列化统一为 System.Text.Json；
  - 路由常量风格一致（小写 `/api/v1/*`）。
- 降低技术债，不改变现有业务功能与对外契约。

## 非目标（Non-Goals）
- 不引入新功能或 UI 可见变化。
- 不修改后端 API 契约及版本策略。
- 不进行大规模代码风格/目录重构（仅限必要的命名与依赖修复）。

## 用户场景（User Stories）
- 作为开发者，我需要在本地无报错地还原并构建桌面解决方案，以便启动与调试 WPF 客户端。
- 作为CI维护者，我需要统一的输出目录和无冗余依赖，降低流水线复杂度和不稳定性。
- 作为架构负责人，我需要前后端在序列化/路由等关键点保持一致，减少集成问题。

## 范围与边界（Scope）
- In Scope：
  - 包引用与命名冲突修复；
  - 重复包版本定义清理；
  - XML 文档输出路径统一为 `$(OutputPath)$(AssemblyName).xml` 或默认；
  - JSON 栈统一（去除 `Refit.Newtonsoft.Json`）；
  - Shell 资源包含方式优化（避免与 SDK 默认规则冲突）；
  - 路由常量小写化；
  - 清理未用/不一致属性（`UseWindowsForms`、`PrismVersion`）。
- Out of Scope：
  - 业务逻辑改动；
  - 大范围 UI/主题重构；
  - 新的质量门禁与分析器规则（本次不加严）。

## 需求明细（Requirements）
- R1 构建修复（必须）
  - 在 `LYBT.Desktop.Core.csproj` 添加 `Microsoft.Extensions.ObjectPool`。
  - 解决命名冲突：
    - 方案A（推荐）：命名空间由 `LYBT.Desktop.Core.ObjectPool` 改为 `LYBT.Desktop.Core.Pooling`；
    - 方案B：引入别名 `using MEOP = Microsoft.Extensions.ObjectPool;` 并使用完全限定名。
  - 删除 `LYBT.Desktop.Services.csproj` 中 `<UseWindowsForms>true</UseWindowsForms>`。
  - 清理 `Directory.Packages.props` 中重复的 `coverlet.collector` 定义，仅保留一处。
- R2 一致性治理（应做）
  - 移除或统一 `<DocumentationFile>` 至 `$(OutputPath)$(AssemblyName).xml`，与 `BIN/` 对齐（Core/Shell/Workbench.Core/Modules.Users 等）。
  - 移除 `Refit.Newtonsoft.Json` 依赖，保持 `UnifiedApiClientManager` 的 System.Text.Json 配置；保留最小必要的 `Newtonsoft.Json` 使用点（如确有兼容场景）。
  - `ApiEndpoints` 常量改为小写路径片段（如 `"api/v1/users"`）。
  - Shell 资源：避免大范围通配 `<Resource Include=*.xaml>`；遵循 SDK 默认 WPF Page 规则或通过 `MergedDictionaries` 精确引用。
- R3 清理与文档（可做）
  - 移除 `Directory.Build.props` 中未使用的 `PrismVersion` 或与实际版本对齐。
  -（可选）统一 `LYBT.Desktop.sln` 中 Shared 项目类型 GUID 为 SDK 风格，和 `LYBT.All.sln` 一致。
  - 更新文档（本 PRD、变更记录、序列化统一说明、资源组织约定）。

## 成功指标（Success Metrics）
- `dotnet restore LYBT.Desktop.sln`：无 NU1506 及同类重复包警告。
- `dotnet build LYBT.Desktop.sln -c Release --no-restore`：0 错误；无缺包/命名冲突。
- 产物与 XML 文档输出位于根 `BIN/`，路径由 `$(OutputPath)` 控制。
- 运行时 API 调用依旧正常（System.Text.Json 序列化兼容）。

## 验收标准（Acceptance Criteria）
- 构建：Debug/Release 均通过；上述命令均返回成功。
- 依赖：`Directory.Packages.props` 无重复定义；桌面项目 PackageReference 不含显式版本号（遵循中央管理）。
- JSON：移除 `Refit.Newtonsoft.Json` 后，编译通过；`UnifiedApiClientManager` 仍以 System.Text.Json 工作。
- 资源：Shell 编译打包成功，主题/资源字典可正常加载。
- 路由常量：小写化后编译通过，调用后端 `/api/v1/*` 正常。

## 里程碑与实施步骤（Milestones）
- 提交 1（构建修复）
  - 添加 `Microsoft.Extensions.ObjectPool`；
  - 命名空间重命名/或别名修复；
  - 删除 `UseWindowsForms`；
  - 去重 `coverlet.collector`。
- 提交 2（一致性治理）
  - 统一/移除 `<DocumentationFile>`；
  - 移除 `Refit.Newtonsoft.Json`；
  - `ApiEndpoints` 小写；
  - 调整 Shell 资源包含策略。
- 提交 3（清理与文档）
  - 清理 `PrismVersion` 未用属性；
  -（可选）统一 Shared 项目 GUID；
  - 更新/归档变更说明与对齐文档。

## 风险与缓解（Risks & Mitigations）
- 命名空间更名影响面：
  - 缓解：IDE 批量重命名 + 全量编译 + 搜索 `LYBT.Desktop.Core.ObjectPool` 校验。
- JSON 栈统一带来兼容差异：
  - 缓解：若发现问题，在基础设施层局部保留 `Newtonsoft.Json`，不外泄至业务层。
- 资源包含策略调整导致资源缺失：
  - 缓解：以运行时验证和视图冒烟测试确认主题/资源加载。

## 依赖与前置（Dependencies & Preconditions）
- 按 `global.json` 固定使用 .NET SDK `9.0.305`。
- 中央包管理：`Directory.Packages.props`。
- 输出目录策略：`Directory.Build.props` 统一至 `BIN/`。

## 回滚策略（Rollback）
- 以提交粒度逐步回滚。
- 若 JSON 统一引发问题，临时恢复 `Refit.Newtonsoft.Json`，并限制使用范围至基础设施层。

## 测试计划（Testing）
- 构建与静态验证：`dotnet restore`、`dotnet build -c Release`。
- 本地冒烟：启动 Shell（若后端可用），验证首页加载与基础资源。
- API 冒烟：用户列表/健康检查回路（`/api/v1/health` 或 `/api/v1/users`）。
- 影响面扫描：搜索命名空间引用、验证资源与序列化路径。

## 产出物（Deliverables）
- 可稳定构建的 `LYBT.Desktop.sln`。
- 统一产物到 `BIN/` 的配置与 XML 文档输出。
- 移除冗余依赖后的项目文件。
- 更新文档：本 PRD、`docs/reports/LYBT.Desktop.sln-analysis.md` 的跟进结论与变更记录。

