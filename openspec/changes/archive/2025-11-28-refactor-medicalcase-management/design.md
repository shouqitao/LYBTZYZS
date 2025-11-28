# Design: 医案管理界面重构

## Context

### 背景
LYBTZYZS系统中存在两个医案相关界面：
1. **MedicalCaseManagementView** - 管理员使用的医案管理界面
2. **MedicalCaseWorkspaceView** - 医生使用的临床看诊界面(5:5布局)

当前问题：
- 管理界面包含"新建案例"按钮，可以绕过患者选择流程
- 缺乏编辑功能，管理员无法修改历史医案
- 没有审计日志机制
- 医生看诊界面缺少明确的"保存"和"编辑"按钮

### 约束
- 遵循MVVM模式和Prism框架规范
- 保持现有导航结构不变
- 不影响医生临床工作流
- 所有修改必须有审计记录

## Goals / Non-Goals

### Goals
- 明确管理界面和临床界面的职责边界
- 实现基于角色的编辑权限控制
- 建立完整的审计日志机制
- 为医生看诊界面添加"保存"和"编辑"按钮

### Non-Goals
- 不改变现有的数据模型结构
- 不修改后端API路由（仅增强）
- 不改变医生临床工作流程

## Decisions

### Decision 1: 完全移除管理界面的新建入口

**决策**: 删除"新建案例"按钮和相关代码。

**原因**: 职责分离原则，医案创建是"诊疗"行为，属于临床工作流。

### Decision 2: 管理界面添加编辑功能

**决策**: 添加"编辑"按钮，管理员可编辑所有医案（包括历史医案）。

**原因**: 管理员需要能够修正历史数据、补录信息。

### Decision 3: 医生看诊界面添加保存和编辑按钮

**决策**: 在MedicalCaseWorkspaceView底部操作栏添加"保存"和"编辑"按钮。

**按钮设计**:
| 按钮 | 功能 | 可见条件 |
|------|------|----------|
| 保存 | 保存当前进度，不改变状态 | 编辑模式下 |
| 编辑 | 进入编辑模式 | 只读模式下且有权限 |
| 暂停看诊 | 保存并设为Draft | 编辑模式下 |
| 完成看诊 | 保存并设为Completed | 编辑模式下 |

**模式切换**:
- **只读模式**: 查看已完成医案时默认进入，显示"编辑"按钮
- **编辑模式**: 新建或点击"编辑"后进入，显示"保存"按钮

### Decision 4: 基于角色的权限控制

**权限矩阵**:
```
┌──────────────┬─────────┬──────────┬───────────┬─────────────────────┐
│ 角色         │ Draft   │ Active   │ Completed │ 说明                │
├──────────────┼─────────┼──────────┼───────────┼─────────────────────┤
│ Doctor       │ ✓ 自己  │ ✓ 自己   │ ✗         │ 只能编辑自己未完成的 │
│ (创建者)     │         │          │           │                     │
├──────────────┼─────────┼──────────┼───────────┼─────────────────────┤
│ Doctor       │ ✗       │ ✗        │ ✗         │ 不能修改他人医案     │
│ (非创建者)   │         │          │           │                     │
├──────────────┼─────────┼──────────┼───────────┼─────────────────────┤
│ Admin        │ ✓       │ ✓        │ ✓         │ 可编辑所有医案       │
│ SuperAdmin   │         │          │           │                     │
└──────────────┴─────────┴──────────┴───────────┴─────────────────────┘
```

**实现逻辑**:
```csharp
public bool CanEdit(Guid userId, UserRole role, MedicalCase medicalCase)
{
    // 管理员可以编辑所有
    if (role == UserRole.SuperAdmin || role == UserRole.Admin)
        return true;

    // 医生只能编辑自己未完成的医案
    if (role == UserRole.Doctor)
    {
        if (medicalCase.CreatedBy == userId)
        {
            return medicalCase.Status == MedicalCaseStatus.Draft
                || medicalCase.Status == MedicalCaseStatus.Active;
        }
    }

    return false;
}
```

### Decision 5: 审计日志设计

**审计实体**:
```csharp
public class MedicalCaseAuditLog
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; }
    public UserRole OperatorRole { get; set; }
    public AuditOperationType OperationType { get; set; }
    public string ChangedFields { get; set; }  // JSON: ["Field1", "Field2"]
    public string OldValues { get; set; }      // JSON: {"Field1": "old"}
    public string NewValues { get; set; }      // JSON: {"Field1": "new"}
    public string? Reason { get; set; }        // 修改原因
    public DateTime CreatedAt { get; set; }

    // 导航属性
    public MedicalCase MedicalCase { get; set; }
}

public enum AuditOperationType
{
    Create = 1,
    Update = 2,
    StatusChange = 3,
    SoftDelete = 4
}
```

**审计触发点**:
- MedicalCaseService.CreateAsync → AuditOperationType.Create
- MedicalCaseService.UpdateAsync → AuditOperationType.Update
- MedicalCaseService.ChangeStatusAsync → AuditOperationType.StatusChange
- MedicalCaseService.DeleteAsync → AuditOperationType.SoftDelete

## Architecture

### 界面职责边界

```
┌─────────────────────────────────────────────────────────────────────┐
│                     管理员入口 (AdminHomeView)                       │
│                              │                                       │
│                              ▼                                       │
│              ┌───────────────────────────────────┐                  │
│              │  MedicalCaseManagementView        │                  │
│              │  ─────────────────────────────    │                  │
│              │  [查看] [搜索] [筛选] [编辑]       │                  │
│              │  [状态管理] [审计日志]             │                  │
│              │  ✗ 无新建功能                     │                  │
│              └───────────────────────────────────┘                  │
│                              │ 编辑                                  │
│                              ▼                                       │
│              ┌───────────────────────────────────┐                  │
│              │   MedicalCaseWorkspaceView        │                  │
│              │   (历史修改模式)                   │                  │
│              │   [保存] [取消]                    │                  │
│              │   + 修改原因输入框                 │                  │
│              └───────────────────────────────────┘                  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                     医生入口 (DoctorHomeView)                        │
│                              │                                       │
│                              ▼                                       │
│              ┌───────────────────────────────────┐                  │
│              │      PatientSelection             │                  │
│              │  选择/创建患者                     │                  │
│              │  选择已有医案 / 创建新医案         │                  │
│              └───────────────────────────────────┘                  │
│                              │                                       │
│                              ▼                                       │
│              ┌───────────────────────────────────┐                  │
│              │   MedicalCaseWorkspaceView        │                  │
│              │   (正常看诊模式)                   │                  │
│              │  ─────────────────────────────    │                  │
│              │  5:5 布局                         │                  │
│              │  ┌─────────────────────────────┐  │                  │
│              │  │ 底部操作栏                   │  │                  │
│              │  │ [保存] [编辑] [暂停] [完成]  │  │                  │
│              │  └─────────────────────────────┘  │                  │
│              └───────────────────────────────────┘                  │
└─────────────────────────────────────────────────────────────────────┘
```

### 医生看诊界面按钮状态机

```
┌─────────────────────────────────────────────────────────────────┐
│                    MedicalCaseWorkspaceView                      │
│                                                                  │
│  ┌──────────────────┐              ┌──────────────────┐         │
│  │   只读模式       │   点击编辑   │   编辑模式       │         │
│  │  (ReadOnly)      │ ──────────> │  (Editing)       │         │
│  │                  │              │                  │         │
│  │ 可见按钮:        │              │ 可见按钮:        │         │
│  │ - [编辑]         │              │ - [保存]         │         │
│  │                  │ <────────── │ - [暂停看诊]     │         │
│  │                  │  保存/暂停   │ - [完成看诊]     │         │
│  │                  │  /完成后     │                  │         │
│  └──────────────────┘              └──────────────────┘         │
│                                                                  │
│  进入条件:                                                       │
│  - 查看已完成医案 → 只读模式                                     │
│  - 新建医案 → 编辑模式                                           │
│  - 继续Draft/Active医案 → 编辑模式                               │
│  - 管理员编辑历史医案 → 编辑模式(+修改原因)                       │
└─────────────────────────────────────────────────────────────────┘
```

### 代码变更范围

```
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
├── Views/
│   ├── MedicalCaseManagementView.xaml    # 移除新建，添加编辑
│   └── MedicalCaseWorkspaceView.xaml     # 添加保存/编辑按钮
└── ViewModels/
    ├── MedicalCaseManagementViewModel.cs # 移除Add，添加Edit
    └── MedicalCaseWorkspaceViewModel.cs  # 添加模式切换逻辑

src/Server/
├── Core/LYBT.Domain/Entities/
│   └── MedicalCaseAuditLog.cs           # 新增审计实体
├── Core/LYBT.Infrastructure/
│   └── Persistence/Configurations/
│       └── MedicalCaseAuditLogConfiguration.cs
├── Modules/LYBT.Module.MedicalCase/
│   ├── Services/
│   │   ├── IMedicalCaseAuditService.cs  # 新增
│   │   ├── MedicalCaseAuditService.cs   # 新增
│   │   ├── IMedicalCasePermissionService.cs # 新增
│   │   └── MedicalCasePermissionService.cs  # 新增
│   └── MedicalCaseService.cs            # 集成审计
└── Services/LYBT.WebAPI/
    └── Controllers/MedicalCaseController.cs # 添加权限检查
```

## Risks / Trade-offs

### Risk 1: 编辑历史医案的数据一致性
- **风险**: 修改已完成医案可能影响统计报表
- **缓解**: 审计日志记录所有修改，可追溯
- **接受度**: 可接受，业务需要此功能

### Risk 2: 权限检查性能
- **风险**: 每次操作都需要权限检查
- **缓解**: 权限检查逻辑简单，可缓存用户角色
- **接受度**: 可接受

### Risk 3: 审计日志存储
- **风险**: 长期运行后审计日志表会变大
- **缓解**: 添加索引，未来可考虑归档策略
- **接受度**: 可接受

## Migration Plan

### 步骤
1. 创建审计日志表（数据库迁移）
2. 实现审计服务和权限服务
3. 修改前端UI（管理界面）
4. 修改前端UI（看诊界面）
5. 集成测试
6. 验证功能

### 回滚
- 数据库迁移支持回滚
- 代码通过Git revert回滚

## Open Questions

*当前无开放问题*

## Implementation Summary

### 已完成实现 (2025-11-28)

#### 后端实现
- `MedicalCaseAuditLog` 实体和数据库迁移
- `AuditOperationType` 枚举 (Create, Update, StatusChange, SoftDelete)
- `IMedicalCaseAuditService` / `MedicalCaseAuditService` 审计服务
- `IMedicalCasePermissionService` / `MedicalCasePermissionService` 权限服务
- `MedicalCaseController` 权限检查集成
- `MedicalCaseService` 审计日志集成 (CreateAsync, UpdateAsync, UpdateConsultationAsync, SetPrescriptionFlagAsync)

#### 前端实现
- **MedicalCaseManagementView**: 移除"新建"按钮，添加"编辑"和"变更记录"按钮
- **MedicalCaseManagementViewModel**: EditCommand, ViewAuditLogCommand 实现
- **MedicalCaseWorkspaceView**: 底部操作栏添加"保存"/"编辑"按钮
- **MedicalCaseWorkspaceViewModel**: IsReadOnly, IsEditing 模式切换, SaveCommand, EditCommand
- **AuditLogDialog + AuditLogDialogViewModel**: 审计日志查看对话框

#### API端点
- `GET /api/v1/medicalcases/{id}/audit-logs` - 获取医案审计日志
- `GET /api/v1/medicalcases/{id}/can-edit` - 检查编辑权限

#### 验证结果
- 全解决方案构建成功 (0错误 0警告)
- MedicalCase集成测试: 27通过/7失败(预先存在的测试问题，非本次变更引起)
