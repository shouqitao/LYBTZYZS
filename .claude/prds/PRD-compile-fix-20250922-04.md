name: PRD-compile-fix-20250922-04
status: backlog
description: All 解决方案编译修复（激进替换·第4轮）

# PRD——All 解决方案编译修复（激进替换·第4轮）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：仅保证“可编译”，暂不要求运行）

## 问题（Problem & Context）
- 全包构建失败，Desktop 侧仍有旧字段名未与 Shared 统一命名对齐。
- 代表性错误：
  - Users：`UserSearchDto.CurrentPage/SearchKeyword`
  - Patients：`PatientSearchDto.CurrentPage`
  - Prescriptions：`PrescriptionDto.TotalAmount`

## 需求（Requirements）
- R1：`CurrentPage` → `PageIndex`；`SearchKeyword` → `Keyword`；`TotalAmount` → `TotalPrice`（仅 Desktop 调用点与绑定）。
- R2：全包构建 0 错误。

## 实施清单
- 参见 `docs/ccpm/PRD-compile-fix-20250922-04.md` 中“实施清单”。

## 验收
```bash
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

