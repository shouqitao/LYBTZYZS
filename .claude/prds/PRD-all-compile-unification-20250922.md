name: PRD-all-compile-unification-20250922
status: backlog
description: All 解决方案编译统一（Phase 1：仅编译通过）

# PRD——All 解决方案编译统一（Phase 1：仅编译通过）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：仅保证“可编译”，暂不要求运行）

## 背景（Problem & Context）
- Desktop 侧仍使用历史字段名，未与 Shared 模型统一命名对齐，导致 All 构建失败。

## 需求（Requirements）
- R1：`CurrentPage` → `PageIndex`；`SearchKeyword` → `Keyword`；`TotalAmount` → `TotalPrice`（仅 Desktop 调用点与绑定）。
- R2：全包构建 0 错误。

## 实施清单（示例）
- Users：`UserManagementViewModel.cs`（100/102）
- Patients：`PatientQueryService.cs:34`
- Prescriptions：`PrescriptionsQueryService.cs:184`、`PrescriptionsModule.cs:92`、`PrescriptionManagementViewModel.cs:312/421`

## 验收（Acceptance）
```bash
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

