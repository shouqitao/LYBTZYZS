# PRD——LYBT.Desktop.sln 桌面解决方案“快速修复清单”（CCPM）

- 文档日期：2025-09-21
- 项目经理：ccpm（Claude Code Project Manager）
- 作用范围：`LYBT.Desktop.sln`、`src/Client/Desktop/*`、`src/Shared/*`、`Directory.Packages.props`、`Directory.Build.props`

## 背景（Problem & Context）
- 构建失败与还原告警：缺少 `Microsoft.Extensions.ObjectPool`、命名冲突（命名空间与类型同名）、`coverlet.collector` 重复定义
- 产物与文档路径不统一：多个项目 `<DocumentationFile>` 指向项目内 `bin/...`，与统一 BIN/ 不一致
- JSON 栈混用：实际已用 System.Text.Json，但仍保留 `Refit.Newtonsoft.Json`；存在行为差异风险
- 其它一致性问题：`UseWindowsForms` 无必要、`PrismVersion` 未用、Shell 资源包含方式可能与 WPF 默认处理冲突

## 目标（Goals）
- 修复构建/告警，使 `LYBT.Desktop.sln` 在 Debug/Release 稳定构建
- 与仓库规范对齐：统一 BIN/ 输出、集中包管理、System.Text.Json、`/api/v1/*` 小写路由
- 降低技术债，不改变对外功能

## 非目标（Non-Goals）
- 不引入新功能或 UI 改动
- 不修改后端 API 契约与版本策略

## 用户故事（User Stories）
- 作为开发者，我需要在本地无报错地构建与运行桌面解决方案
- 作为 CI 维护者，我需要统一目录与依赖，降低流水线复杂度

## 范围（Scope）
- In Scope：
  - 构建修复；重复包定义清理；XML 文档输出改为 `$(OutputPath)$(AssemblyName).xml`
  - JSON 栈统一（移除 `Refit.Newtonsoft.Json`）；Shell 资源包含方式与 WPF 默认一致；路由常量小写
  - 清理无用/不一致属性（`UseWindowsForms`、`PrismVersion`）
- Out of Scope：
  - 业务逻辑/界面重构；新质量门禁与分析器规则收严

## 需求明细（Requirements）
- R1 构建修复（必须）
  - Core 引入 `Microsoft.Extensions.ObjectPool`；命名冲突通过命名空间更名或类型别名解决
  - 取消 `UseWindowsForms`；清理 `coverlet.collector` 重复定义
- R2 一致性治理（应做）
  - 统一/移除 `<DocumentationFile>` 为 `$(OutputPath)$(AssemblyName).xml`
  - 移除 `Refit.Newtonsoft.Json`，保持 System.Text.Json；路由常量统一小写；Shell 资源包含方式依赖 WPF 默认规则
- R3 清理与文档（可做）
  - 清理未用 `PrismVersion`；（可选）统一 Shared 项目类型 GUID；补充变更说明

## 成功指标（Success Metrics）
- `dotnet restore LYBT.Desktop.sln` 无重复包告警（NU1506/NU1504）
- `dotnet build -c Release --no-restore` 0 错误；无缺包/命名冲突
- XML 文档随 `$(OutputPath)` 产出到 BIN/
- 运行时 API 调用正常，内容序列化统一 System.Text.Json

## 验收标准（Acceptance Criteria）
- Debug/Release 构建通过；上述命令返回成功
- 依赖：`Directory.Packages.props` 无重复定义；项目 PackageReference 不含显式版本号
- JSON 统一：移除 `Refit.Newtonsoft.Json` 后编译/运行通过；`UnifiedApiClientManager` 使用 System.Text.Json
- 资源：Shell 编译打包成功；主题/资源字典正确加载
- 实现必须严格遵循本 PRD 的要求与范围。任何偏差须先更新 PRD 并获批准

## 里程碑与实施步骤（Milestones）
- 提交 1（构建修复）：包引用、命名修复、UseWindowsForms、重复包清理
- 提交 2（一致性）：XML 文档输出、移除 Refit.Newtonsoft.Json、路由小写、资源包含方式
- 提交 3（清理与文档）：清理 PrismVersion、（可选）统一 GUID、更新变更说明

## 风险与缓解（Risks & Mitigations）
- 命名空间更名影响面 → IDE 批量重命名 + 全量编译 + 搜索校验
- JSON 统一引发兼容差异 → 若发现问题，仅在基础设施层局部保留 Newtonsoft（不外泄到业务）
- 资源包含方式调整导致资源缺失 → 运行时验证与冒烟测试

## 依赖与前置（Dependencies & Preconditions）
- 按 `global.json` 固定 .NET SDK 9.0.305；采用 `Directory.Packages.props` 管理依赖；BIN/ 输出策略生效

## 回滚策略（Rollback）
- 按提交粒度回滚；若 JSON 统一引发问题，临时恢复 `Refit.Newtonsoft.Json`，并限制使用范围至基础设施层

## 测试（Testing）
- 构建与静态验证：`dotnet restore`、`dotnet build -c Release`
- 冒烟测试：启动 Shell（若后端可用），验证首页与基础资源
- API 冒烟：`/api/v1/health`、`/api/v1/users` 等
- 影响面扫描：搜索命名空间引用与资源路径

## 产出物（Deliverables）
- 可稳定构建的 `LYBT.Desktop.sln`
- 统一 BIN/ 输出策略与 XML 文档产出
- 移除冗余依赖后的项目文件
- 完成总结文档（Summary）：docs/prds-summary/PRD-desktop-sln-quick-fix-20250921-SUMMARY.md（包含变更摘要、验证与测试、更新 README 列表与链接、风险与后续）
