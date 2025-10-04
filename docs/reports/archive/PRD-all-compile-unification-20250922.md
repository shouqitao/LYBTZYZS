# PRD——All 解决方案编译统一（Phase 1：仅编译通过）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：仅保证“可编译”，暂不要求运行）

## 背景（Problem & Context）
- All 方案编译失败，Desktop 侧仍使用历史字段名，未与 Shared 模型统一命名对齐。
- 代表性错误（Release, --no-restore）：
  - Users：UserSearchDto 缺少 `CurrentPage`、`SearchKeyword`（`UserManagementViewModel.cs:100/102`）
  - Patients：PatientSearchDto 缺少 `CurrentPage`（`PatientQueryService.cs:34`）
  - Prescriptions：PrescriptionDto 缺少 `TotalAmount`（`PrescriptionsQueryService.cs:184`、`PrescriptionsModule.cs:92`、`PrescriptionManagementViewModel.cs:312/421`）

## 目标（Goals）
- 采用“自后向前、直接替换”的激进方式，统一到 Shared 最新字段命名，确保 `LYBT.All.sln` 编译通过。
- 不新增任何向后兼容别名。

## 非目标（Non-Goals）
- 不调整业务逻辑或 UI 展现（除属性名对齐外）。
- 不新增 Shared 兼容层或临时投影属性。

## 需求（Requirements）
- R1（Desktop 代码替换）：
  - `*.SearchDto/*.QueryDto/PagedQueryBaseDto` 相关：`CurrentPage` → `PageIndex`；`SearchKeyword` → `Keyword`
  - 处方 DTO 使用：`PrescriptionDto.TotalAmount` → `PrescriptionDto.TotalPrice`
- R2（Desktop 绑定校对）：
  - 若 XAML 绑定到 DTO 查询字段，需同步改为 `Keyword`/`PageIndex`（控件自有 `CurrentPage` 字段保留）。
- R3（验证）：
  - 全包构建 0 错误（警告暂不处理）。

## 实施清单（首批文件）
- Users：
  - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs`（第 100、102 行附近）
  - `src/Client/Desktop/Modules/Users/Views/UserManagementView.xaml`（如绑定 DTO 查询字段需同步）
- Patients：
  - `src/Client/Desktop/Modules/Patients/Services/PatientQueryService.cs:34`（日志/引用统一 `PageIndex`）
- Prescriptions：
  - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsQueryService.cs:184`
  - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsModule.cs:92`
  - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs:312, 421`
  - 上述位置统一 `TotalAmount` → `TotalPrice`

## 成功度量（Success Metrics）
- `dotnet build LYBT.All.sln -c Release --no-restore`：0 错误。

## 验收标准（Acceptance Criteria）
- 还原 + 构建成功：
  ```bash
  dotnet restore LYBT.All.sln
  dotnet build LYBT.All.sln -c Release --no-restore
  ```
- 未在 Shared 新增兼容别名；替换仅发生在 Desktop 调用点与绑定。

## 里程碑（Milestones）
- 提交 1：完成上述文件替换并本地构建校验。
- 提交 2：若仍有遗漏，按编译器错误清单逐步补齐至 0 错误。

## 风险与缓解（Risks & Mitigations）
- 风险：替换遗漏或误伤 UI 自有字段。
  - 缓解：按编译错误逐步收敛，确认仅替换 DTO 语义字段；UI 控件字段名保留。

## 测试计划（Testing）
- 编译级验证（仅）：
  ```bash
  dotnet restore LYBT.All.sln
  dotnet build LYBT.All.sln -c Release --no-restore
  ```

## 交付物（Deliverables）
- 可编译通过的 `LYBT.All.sln`
- 本 PRD 文档（存档于 `docs/ccpm/` 与 `.claude/prds/`）

