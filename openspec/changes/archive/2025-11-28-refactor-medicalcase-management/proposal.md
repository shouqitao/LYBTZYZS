# Change: 重构医案管理界面 - 职责分离与编辑权限

## Why

当前医案管理界面(MedicalCaseManagementView)存在职责混淆问题：
1. 包含"新建案例"按钮，违反单一职责原则
2. 缺乏编辑功能，管理员无法修改历史医案
3. 没有审计日志，无法追踪修改历史

重构目标：
- 管理界面专注于"查看和管理"，移除创建入口
- 增加编辑功能，支持管理员修改历史医案
- 建立完整的审计日志机制

## What Changes

### UI变更
- **移除** `MedicalCaseManagementView.xaml` 中的"新建案例"按钮
- **新增** "编辑"按钮，支持管理员编辑医案（含历史医案）
- **新增** 审计日志查看功能

### 编辑权限矩阵
| 角色 | Draft | Active | Completed | 说明 |
|------|-------|--------|-----------|------|
| Doctor(创建者) | 可编辑 | 可编辑 | 不可编辑 | 只能编辑自己未完成的医案 |
| Doctor(非创建者) | 不可编辑 | 不可编辑 | 不可编辑 | 不能修改他人医案 |
| Admin/SuperAdmin | 可编辑 | 可编辑 | **可编辑** | 可编辑所有医案含历史 |

### 审计日志
- 所有编辑操作自动记录修改人、时间、修改内容
- 支持记录修改原因（可选）
- 管理界面可查看审计历史

### 职责边界
- **管理界面**(Admin): 查看、搜索、筛选、**编辑**、状态管理、审计查看
- **临床界面**(Doctor): 创建新医案、诊断、处方、完成看诊

## Impact

### Affected specs
- `medicalcase-lifecycle`: 新增编辑权限和审计日志规范

### Affected code

**前端**:
- `MedicalCaseManagementView.xaml` - 移除新建按钮，添加编辑按钮
- `MedicalCaseManagementViewModel.cs` - 移除AddCommand，添加EditCommand
- `MedicalCaseWorkspaceViewModel.cs` - 添加EditMode支持

**后端**:
- 新增 `MedicalCaseAuditLog` 实体
- 新增 `IMedicalCaseAuditService` 接口和实现
- 修改 `MedicalCaseService.UpdateAsync` 集成审计
- 修改 `MedicalCaseController` 添加权限检查

**数据库**:
- 新增 `MedicalCaseAuditLogs` 表

### User impact
- 管理员可以编辑所有医案（包括历史医案）
- 医生只能编辑自己未完成的医案
- 所有修改都有审计记录

### Risk assessment
- **风险等级**: 中等（涉及权限和审计）
- **向后兼容**: 是（新增功能，不影响现有数据）
- **回滚策略**: Git revert + 数据库迁移回滚
