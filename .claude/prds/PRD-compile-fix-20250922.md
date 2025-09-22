name: PRD-compile-fix-20250922
status: backlog
description: All 解决方案编译修复（激进替换）

# PRD——All 解决方案编译修复（激进替换）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：仅保证“可编译”，暂不要求运行）

## 问题（Problem & Context）
- 全包构建失败，错误集中在 Desktop 模块继续引用旧字段名：
  - 查询分页：旧 `CurrentPage`、`SearchKeyword`，现已统一为 `PageIndex`、`Keyword`（`PagedQueryBaseDto` 及其派生 `*SearchDto/*QueryDto`）。
  - 处方金额：Desktop 引用 `PrescriptionDto.TotalAmount`，现模型使用 `TotalPrice`（计算属性）。
- 代表性错误：
  - CS1061: PagedQueryBaseDto 未包含 `CurrentPage`/`SearchKeyword`
  - CS0117: UserSearchDto 未包含 `CurrentPage`/`SearchKeyword`
  - CS1061: PrescriptionDto 未包含 `TotalAmount`

## 目标（Goals）
- 采用“自后向前、直接替换”的激进方式，统一到 Shared 最新字段命名，确保 `LYBT.All.sln` 编译通过。
- 不新增任何向后兼容别名。

## 非目标（Non-Goals）
- 不调整业务逻辑或 UI 展现（除属性名对齐外）。
- 不新增 Shared 兼容层或临时投影属性。

## 范围（Scope）
- In Scope：Desktop 侧的调用点与 XAML 绑定（.cs/.xaml）。
- Out of Scope：WebAPI/数据库/运行配置与联调。

## 需求（Requirements）
- R1 字段统一（代码替换）：
  - 将所有 `*.SearchDto/*.QueryDto/PagedQueryBaseDto` 相关的 `CurrentPage` → `PageIndex`，`SearchKeyword` → `Keyword`。
  - 将所有对 `PrescriptionDto.TotalAmount` 的访问替换为 `PrescriptionDto.TotalPrice`。
- R2 绑定更新（XAML）：
  - 与上述字段一致的绑定路径同步替换。
- R3 验证与提交：
  - 全包构建 0 错误；保留警告。

## 实施清单（基于静态扫描与构建错误）
- Users 模块：
  - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs`：`UserSearchDto.CurrentPage/SearchKeyword` → `PageIndex/Keyword`
  - `src/Client/Desktop/Modules/Users/Views/UserManagementView.xaml`：绑定 `CurrentPage/SearchKeyword` → `PageIndex/Keyword`
- Patients 模块：
  - `src/Client/Desktop/Modules/Patients/Services/PatientQueryService.cs`：`PatientSearchDto.CurrentPage` → `PageIndex`
- MedicalCase 模块：
  - `src/Client/Desktop/Modules/MedicalCase/Services/MedicalCaseQueryService.cs`：`PagedQueryBaseDto.CurrentPage/SearchKeyword` → `PageIndex/Keyword`
  - `src/Client/Desktop/Modules/MedicalCase/ViewModels/MedicalCaseManagementViewModel.cs`：同上
- Prescriptions 模块：
  - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsQueryService.cs`：`PrescriptionDto.TotalAmount` → `TotalPrice`
  - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs`：`TotalAmount` → `TotalPrice`
  - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsModule.cs`：`TotalAmount` → `TotalPrice`

注：控件/协调器中用于 UI 分页的自有 `CurrentPage/SearchKeyword` 字段（非 DTO 语义）不在替换范围。

## 成功度量（Success Metrics）
- `dotnet build LYBT.All.sln -c Release --no-restore`：0 错误。

## 验收标准（Acceptance Criteria）
- 还原+构建成功：
  ```bash
  dotnet restore LYBT.All.sln
  dotnet build LYBT.All.sln -c Release --no-restore
  ```
- 未引入 Shared 兼容别名；所有替换发生在 Desktop 侧调用点与绑定。

## 里程碑（Milestones）
- 提交 1：批量替换 Desktop 代码与 XAML。
- 提交 2：复核并编译验证（0 错误）。

## 风险与缓解（Risks & Mitigations）
- 风险：替换范围较广引入遗漏。
  - 缓解：全局正则扫描 + 编译器错误驱动迭代直到 0 错误。
- 风险：少量 UI 层字段名与 DTO 同名但语义不同。
  - 缓解：逐文件确认语义，保留 UI 自有字段名，不替换。

## 测试计划（Testing）
- 编译级验证（仅）：
  ```bash
  dotnet restore LYBT.All.sln
  dotnet build LYBT.All.sln -c Release --no-restore
  ```

## 交付物（Deliverables）
- 可编译通过的 `LYBT.All.sln`
- 本 PRD 文档（存档于 `docs/ccpm/` 与 `.claude/prds/`）

