# rename-reference-to-management

## 概述

将Clinical角色台中的"Reference"视图统一重命名为"Management"视图，反映医生对自己创建的数据具有管理权限的实际情况。

## 背景

当前Clinical角色台使用"Reference"命名（如`FormulaReferenceView`）暗示只读参考功能，但实际业务需求是：
- **诊所共享数据**: 医生只读参考
- **医生自创数据**: 医生可完整管理（增删改查）

命名与实际权限不符，造成理解混淆。

## 变更内容

### 视图重命名

| 当前命名 | 新命名 | 模块 |
|----------|--------|------|
| `FormulaReferenceView` | `FormulaManagementView` | LYBT.Desktop.Clinical |
| `HerbReferenceView` | `HerbManagementView` | LYBT.Desktop.Clinical |
| `PatientHistoryView` | `PatientManagementView` | LYBT.Desktop.Clinical |
| `MedicalCaseArchiveView` | `MedicalCaseManagementView` | LYBT.Desktop.Clinical |

### 权限控制设计

权限控制在业务层实现，而非视图层：

```
┌─────────────────────────────────────────────────────────┐
│  MasterDetailControl / ViewModel                        │
├─────────────────────────────────────────────────────────┤
│  查询筛选:                                              │
│  - Admin: 查看所有数据                                   │
│  - Clinical: 查看诊所共享 + 自己创建的                    │
├─────────────────────────────────────────────────────────┤
│  操作权限:                                              │
│  - 诊所共享: 仅Admin可编辑                               │
│  - 个人创建: 创建者可编辑                                │
└─────────────────────────────────────────────────────────┘
```

## 影响范围

- `LYBT.Desktop.Clinical/Views/` - 4个View文件重命名
- `LYBT.Desktop.Clinical/ClinicalModule.cs` - 更新导航注册
- `LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs` - 更新导航目标

## 非范围

- 权限控制逻辑实现（后续Issue处理）
- Admin模块视图（保持不变）
- 业务模块Control（保持不变，复用）

## 验收标准

- [ ] 4个Reference视图重命名为Management
- [ ] ClinicalModule导航注册更新
- [ ] ClinicalHomeViewModel导航方法更新
- [ ] 编译通过，无错误
- [ ] 运行时导航正常
