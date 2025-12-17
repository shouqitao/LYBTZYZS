# Master-Detail模式功能清单对比表

> 文档版本: v1.0
> 创建日期: 2025-12-16
> OpenSpec: refactor-master-detail-layout

## 概述

本文档记录从原ManagementViewModel迁移到MasterDetailViewModel过程中的功能对比，用于确保所有功能完整迁移。

---

## 1. 用户模块 (Users)

| 功能 | 原ManagementVM | 新MasterDetailVM | 状态 |
|------|----------------|------------------|------|
| 新增用户 AddCommand | Y | Y | 已有 |
| 编辑用户 EditCommand | Y | Y | 已有 |
| 查看详情 ViewDetailsCommand | Y | Y (内置) | 已有 |
| 重置密码 ResetPasswordCommand | Y | Y | 已有 |
| 切换状态 ToggleUserStatusCommand | Y | Y | 已有 |
| 删除用户 DeleteCommand | Y | Y | 已有 |
| 审计日志 ShowAuditLogCommand | Y | Y | 已有 |
| 恢复 RestoreCommand | Y | Y | 已有 |
| 导入 ImportCommand | Y | Y | 已有 |
| 导出 ExportCommand | Y | Y | 已有 |
| 下载模板 DownloadTemplateCommand | Y | Y | 已有 |
| 清除筛选 ClearFiltersCommand | Y | Y | 已有 |
| 角色筛选 SelectedRoleFilter | Y | Y | 已有 |
| 状态筛选 SelectedStatusFilter | Y | Y | 已有 |
| 显示非活跃 ShowInactiveUsers | Y | Y | 已有 |

**结论**: 功能完整，无缺失

---

## 2. 患者模块 (Patients)

| 功能 | 原ManagementVM | 新MasterDetailVM | 状态 |
|------|----------------|------------------|------|
| 新增患者 AddCommand | Y | Y | 已有 |
| 编辑患者 EditCommand | Y | Y | 已有 |
| 查看详情 ViewDetailsCommand | Y | Y (内置) | 已有 |
| 审计日志 ShowAuditLogCommand | Y | Y | 已补充 |
| 恢复 RestoreCommand | Y | Y | 已补充 |
| 导入 ImportCommand | Y | Y | 已补充 |
| 导出 ExportCommand | Y | Y | 已补充 |
| 下载模板 DownloadTemplateCommand | Y | Y | 已补充 |

**结论**: 功能已补充完整

---

## 3. 药材模块 (Herbs)

| 功能 | 原ManagementVM | 新MasterDetailVM | 状态 |
|------|----------------|------------------|------|
| 新增药材 AddCommand | Y | Y | 已有 |
| 编辑药材 EditCommand | Y | Y | 已有 |
| 查看详情 ViewDetailsCommand | Y | Y (内置) | 已有 |
| 复制药材 CopyHerbCommand | Y | Y | 已有 |
| 切换状态 ToggleStatusCommand | Y | Y | 已有 |
| 审计日志 ShowAuditLogCommand | Y | Y | 已有 |
| 恢复 RestoreCommand | Y | Y | 已有 |
| 导入 ImportCommand | Y | Y | 已有 |
| 导出 ExportCommand | Y | Y | 已有 |
| 下载模板 DownloadTemplateCommand | Y | Y | 已有 |
| 分类搜索 SearchByCategoryCommand | Y | Y | 已补充 |

**结论**: 功能已补充完整

---

## 4. 验方模块 (Formula)

| 功能 | 原ManagementVM | 新MasterDetailVM | 状态 |
|------|----------------|------------------|------|
| 新增验方 AddCommand | Y | Y | 已有 |
| 编辑验方 EditCommand | Y | Y | 已有 |
| 查看详情 ViewDetailCommand | Y | Y (内置) | 已有 |
| 复制验方 CopyFormulaCommand | Y | Y | 已有 |
| 切换状态 ToggleStatusCommand | Y | Y | 已有 |
| 审计日志 ShowAuditLogCommand | Y | Y | 已有 |
| 恢复 RestoreCommand | Y | Y | 已有 |
| 导入验方 ImportFormulasCommand | Y (开发中) | - | 原本未实现 |
| 导出验方 ExportFormulasCommand | Y (开发中) | - | 原本未实现 |
| 导出模板 ExportTemplateCommand | Y (开发中) | - | 原本未实现 |
| 清除筛选 ClearFiltersCommand | Y | Y | 已补充 |
| 分类搜索 SearchByCategoryCommand | Y | Y | 已补充 |

**结论**: 功能已补充完整（导入/导出原本未实现，暂不补充）

---

## 功能说明

### RestoreCommand (恢复软删除)

**用途**: 恢复被"软删除"的数据

**软删除机制**:
- 删除操作不是真正从数据库删除，而是标记 `IsDeleted=true`
- RestoreCommand 允许管理员恢复这些被软删除的数据

**权限要求**: 仅管理员可用 (`IsAdmin` 检查)

**使用场景**: 误删数据后的恢复操作

---

## 变更记录

| 日期 | 变更内容 | 执行人 |
|------|---------|--------|
| 2025-12-16 | 创建文档，记录功能对比 | Claude Code |
| 2025-12-16 | 补充Patients模块5个缺失功能 | Claude Code |
| 2025-12-16 | 补充Herbs模块1个缺失功能（SearchByCategoryCommand） | Claude Code |
| 2025-12-16 | 补充Formula模块2个缺失功能（ClearFiltersCommand, SearchByCategoryCommand） | Claude Code |
| 2025-12-16 | 全部功能补充完成，编译验证通过 | Claude Code |
