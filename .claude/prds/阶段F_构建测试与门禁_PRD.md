# 阶段 F：构建、测试与门禁 PRD

## 目标
- 将重构后的桌面端纳入统一的构建/测试/格式化流程；收敛关键分析器警告；稳住门禁。

## 范围
- In Scope：构建、格式化、测试与覆盖率；架构测试；关键分析器（StyleCop 等）配置与修复。
- Out of Scope：引入新测试框架（沿用 xUnit/FluentAssertions/Moq/Verify/NetArchTest）。

## 交付物
- 可复用的命令与脚本；关键模块/服务的单元测试增强；覆盖率报告（无硬性阈值）。

## 验收标准
- 以下命令均成功：
  - `dotnet restore LYBT.All.sln`
  - `dotnet build LYBT.Desktop.sln -c Release --no-restore`
  - `dotnet format LYBT.All.sln`
  - `dotnet test tests -c Release --no-build`
  - `dotnet test tests/Architecture/LYBT.ArchTests.csproj`
  - `dotnet test tests -c Release --collect:"XPlat Code Coverage"`
- 分析器警告趋势下降，新增警告为 0。

## 里程碑
1. 增量单测：为新/改动的 Loading/通知/事件门面补充单测（可使用 Verify 快照）。
2. 架构测试：禁止桌面模块反向依赖 Shell 或跨模块 UI 直接引用；确保仅依赖 Shared 契约。
3. 纳入 CI（如适用）：在现有流水线上追加桌面构建与测试步骤。

## 风险与缓解
- 风险：历史项目警告较多，短期难清零。缓解：列出白名单/抑制原因，逐步消化；优先关闭可安全消除项（XML 注释、CS1998）。

## 依赖
- 现有 tests 目录；`.editorconfig`；CI 环境（如 GitHub Actions/Azure DevOps）。

## 回滚方案
- 流水线问题时，回退新增步骤与校验，保主流程稳定。

## 度量
- 覆盖率报告生成；新增测试用例数；分析器告警减少数量。

## 测试计划
- 单元/架构/快照测试；必要的集成测试（仅桌面可行部分）。

## 受影响文件（示例）
- `tests/*`（新增/修改）
- 相关项目文件中的 `NoWarn` 与分析器配置

