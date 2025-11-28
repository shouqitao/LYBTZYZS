# Proposal: refine-medicalcase-edit-modes

## Summary
重构MedicalCaseWorkspaceView为可复用组件，支持"临床看诊"和"管理编辑"两个大模块复用。

## Problem Statement

当前MedicalCaseWorkspaceView设计为临床看诊专用，导致：

1. **返回逻辑硬编码**: 固定返回PatientSelection，从管理界面进入时无法正确返回
2. **按钮语义混淆**: "保存"和"暂停看诊"功能重叠，用户不清楚区别
3. **模式切换不清晰**: 查看/编辑模式缺乏明确的状态指示和切换机制
4. **复用性差**: 临床和管理两个模块需要分别维护类似的编辑逻辑

## Proposed Solution

### 架构设计: 模块化复用

```
┌─────────────────────────────────────────────────────────────┐
│              MedicalCaseWorkspaceView (共享)                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Header: 动态标题 + 动态返回按钮                       │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ Content: ConsultationPanel + PrescriptionPanel      │   │
│  ├─────────────────────────────────────────────────────┤   │
│  │ Footer: 动态操作按钮组                               │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                          ▲
          ┌───────────────┴───────────────┐
          │                               │
┌─────────┴─────────┐         ┌──────────┴──────────┐
│   临床看诊模块     │         │    管理编辑模块      │
│ (PatientSelection) │         │(MedicalCaseManagement)│
│                   │         │                      │
│ - 返回患者选择     │         │ - 返回医案列表        │
│ - 暂存/完成看诊    │         │ - 查看/修改医案       │
│ - 新建医案流程     │         │ - 历史医案编辑        │
└───────────────────┘         └──────────────────────┘
```

### 1. 引入WorkspaceMode枚举
```csharp
public enum WorkspaceMode
{
    Clinical,    // 临床看诊模式（从PatientSelection进入）
    Management   // 管理编辑模式（从MedicalCaseManagement进入）
}
```

### 2. 动态UI配置

| 元素 | 临床模式 | 管理模式 |
|------|----------|----------|
| 标题 | "看诊中 \| 患者：XXX" | "编辑医案 \| 患者：XXX" |
| 返回按钮 | "返回患者选择" | "返回医案列表" |
| 编辑模式按钮 | [暂存医案] [完成看诊] | [暂存医案] [完成看诊] |
| 只读模式按钮 | [修改医案] | [修改医案] |

### 3. 模式切换逻辑
- **暂存医案**: 保存数据 → 状态设为Draft → 切换到只读模式（留在当前界面）
- **修改医案**: 切换到编辑模式
- **完成看诊**: 保存数据 → 状态设为Completed → 返回来源页面

### 4. 管理界面入口
- **查看详情**: 进入只读模式，可点击"修改医案"进入编辑
- **编辑**: 直接进入编辑模式

## Affected Components
- `MedicalCaseWorkspaceView.xaml` - 动态绑定标题、返回按钮、操作栏
- `MedicalCaseWorkspaceViewModel.cs` - 添加WorkspaceMode、动态属性
- `MedicalCaseManagementViewModel.cs` - 传递模式参数
- `PatientSelectionViewModel.cs` - 传递模式参数
- `medicalcase-lifecycle` spec - 更新要求

## Out of Scope
- 权限控制逻辑（已在LIFECYCLE-007定义）
- 审计日志记录（已在LIFECYCLE-008定义）
- 新建医案流程（保持现有逻辑）
