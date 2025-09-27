# PRD——All 解决方案编译修复（激进替换·第4轮）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：仅保证“可编译”，暂不要求运行）

## 问题（Problem & Context）
- 全包构建失败，Desktop 侧仍有旧字段名未与 Shared 统一命名对齐。
- 构建错误（Release, --no-restore，摘录）：
  - Users 模块
    - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs:100` UserSearchDto 不含 `CurrentPage`
    - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs:102` UserSearchDto 不含 `SearchKeyword`
  - Patients 模块
    - `src/Client/Desktop/Modules/Patients/Services/PatientQueryService.cs:34` PatientSearchDto 不含 `CurrentPage`
  - Prescriptions 模块
    - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsQueryService.cs:184` PrescriptionDto 不含 `TotalAmount`
    - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsModule.cs:92` PrescriptionDto 不含 `TotalAmount`
    - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs:312` PrescriptionDto 不含 `TotalAmount`
    - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs:421` PrescriptionDto 不含 `TotalAmount`

## 目标（Goals）
- 采用“自后向前、直接替换”的方式，统一到 Shared 最新字段命名，确保 `LYBT.All.sln` 编译通过。
- 不新增任何向后兼容别名。

## 非目标（Non-Goals）
- 不调整业务逻辑或 UI 展现（除属性名对齐外）。
- 不新增 Shared 兼容层或临时投影属性。

## 范围（Scope）
- In Scope：Desktop 侧的调用点与 XAML 绑定（.cs/.xaml）。
- Out of Scope：WebAPI/数据库/运行配置与联调。

## 需求（Requirements）
- R1 字段统一（仅 Desktop .cs 调用点）：
  - `UserSearchDto.CurrentPage` → `UserSearchDto.PageIndex`
  - `UserSearchDto.SearchKeyword` → `UserSearchDto.Keyword`
  - `PatientSearchDto.CurrentPage` → `PatientSearchDto.PageIndex`
  - `PrescriptionDto.TotalAmount` → `PrescriptionDto.TotalPrice`
- R2 绑定更新（Desktop .xaml）：
  - 若绑定到 DTO 查询字段，需同步改为 `Keyword`/`PageIndex`；UI 控件自身 `CurrentPage` 字段保留（非 DTO 语义）。
- R3 构建验证：全包构建 0 错误；警告保留。

## 实施清单（本轮）
- Users：
  - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs`（第 100、102 行附近）
  - `src/Client/Desktop/Modules/Users/Views/UserManagementView.xaml`（如有 DTO 绑定字段，需要同步改为 `Keyword`/`PageIndex`）
- Patients：
  - `src/Client/Desktop/Modules/Patients/Services/PatientQueryService.cs:34`（将 `query.CurrentPage` → `query.PageIndex` 或相关使用点统一）
- Prescriptions：
  - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsQueryService.cs:184`
  - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsModule.cs:92`
  - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs:312, 421`
  - 上述位置将 `PrescriptionDto.TotalAmount` → `PrescriptionDto.TotalPrice`

注：Desktop 内部 UI 分页控件/协调器的 `CurrentPage` 为控件自有字段（非 DTO 语义），不在替换范围。

## 成功度量（Success Metrics）
- `dotnet build LYBT.All.sln -c Release --no-restore`：0 错误。

## 验收标准（Acceptance Criteria）
- 还原+构建成功：
  ```bash
  dotnet restore LYBT.All.sln
  dotnet build LYBT.All.sln -c Release --no-restore
  ```
- 未在 Shared 新增兼容别名；替换仅发生在 Desktop 调用点与绑定。

## 里程碑（Milestones）
- 提交 1：完成上述文件的替换并本地构建校验。
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

