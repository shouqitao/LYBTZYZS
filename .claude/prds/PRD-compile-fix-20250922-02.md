name: PRD-compile-fix-20250922-02
status: backlog
description: All 解决方案编译修复（激进替换·第2轮）

# PRD——All 解决方案编译修复（激进替换·第2轮）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：仅保证“可编译”，暂不要求运行）

## 问题（Problem & Context）
- 全包构建失败，Desktop 侧继续引用旧字段名，未与 Shared 模型的统一命名对齐。
- 构建摘要（Release, --no-restore）：
  - Users 模块
    - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs:100` UserSearchDto 不含 `CurrentPage`
    - `src/Client/Desktop/Modules/Users/ViewModels/UserManagementViewModel.cs:102` UserSearchDto 不含 `SearchKeyword`
  - Prescriptions 模块
    - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsQueryService.cs:184` PrescriptionDto 不含 `TotalAmount`
    - `src/Client/Desktop/Modules/Prescriptions/Services/PrescriptionsModule.cs:92` PrescriptionDto 不含 `TotalAmount`
    - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs:312` PrescriptionDto 不含 `TotalAmount`
    - `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionManagementViewModel.cs:421` PrescriptionDto 不含 `TotalAmount`

## 目标（Goals）
- 采用“自后向前、直接替换”的激进方式，统一到 Shared 最新字段命名，确保 `LYBT.All.sln` 编译通过。
- 不新增任何向后兼容别名。

## 需求（Requirements）
- R1：`CurrentPage` → `PageIndex`；`SearchKeyword` → `Keyword`；`TotalAmount` → `TotalPrice`（仅 Desktop 调用点与绑定）。
- R2：全包构建 0 错误。

## 实施清单
- 参见 docs/ccpm/PRD-compile-fix-20250922-02.md 中“实施清单”。

## 验收
```bash
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

