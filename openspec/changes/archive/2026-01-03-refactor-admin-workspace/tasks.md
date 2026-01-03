# Tasks: refactor-admin-workspace

## 概述

将5个MasterDetailView重构为Control模式，实现"View在角色台，Control在业务模块"的架构统一。

**重构策略**: 渐进式重构（重命名 → 移动 → 更新命名空间 → 创建薄包装View）

---

## Phase A: 业务模块Control重构

### A-1: Herbs模块

- [x] A-1.1 重命名 `HerbMasterDetailView.xaml` → `HerbMasterDetailControl.xaml`
- [x] A-1.2 重命名 `HerbMasterDetailView.xaml.cs` → `HerbMasterDetailControl.xaml.cs`
- [x] A-1.3 移动文件到 Controls/ 目录
- [x] A-1.4 更新命名空间为 `LYBT.Desktop.Herbs.Controls`
- [x] A-1.5 更新 x:Class 和类名
- [x] A-1.6 更新 HerbsModule.cs 注册
- [x] A-1.7 编译验证

### A-2: Formula模块

- [x] A-2.1 重命名 `FormulaMasterDetailView.xaml` → `FormulaMasterDetailControl.xaml`
- [x] A-2.2 重命名 `FormulaMasterDetailView.xaml.cs` → `FormulaMasterDetailControl.xaml.cs`
- [x] A-2.3 移动文件到 Controls/ 目录
- [x] A-2.4 更新命名空间为 `LYBT.Desktop.Formula.Controls`
- [x] A-2.5 更新 x:Class 和类名
- [x] A-2.6 更新 FormulaModule.cs 注册
- [x] A-2.7 编译验证

### A-3: Patients模块

- [x] A-3.1 重命名 `PatientMasterDetailView.xaml` → `PatientMasterDetailControl.xaml`
- [x] A-3.2 重命名 `PatientMasterDetailView.xaml.cs` → `PatientMasterDetailControl.xaml.cs`
- [x] A-3.3 移动文件到 Controls/ 目录
- [x] A-3.4 更新命名空间为 `LYBT.Desktop.Patients.Controls`
- [x] A-3.5 更新 x:Class 和类名
- [x] A-3.6 更新 PatientsModule.cs 注册
- [x] A-3.7 编译验证

### A-4: MedicalCase模块

- [x] A-4.1 重命名 `MedicalCaseMasterDetailView.xaml` → `MedicalCaseMasterDetailControl.xaml`
- [x] A-4.2 重命名 `MedicalCaseMasterDetailView.xaml.cs` → `MedicalCaseMasterDetailControl.xaml.cs`
- [x] A-4.3 移动文件到 Controls/ 目录
- [x] A-4.4 更新命名空间为 `LYBT.Desktop.MedicalCase.Controls`
- [x] A-4.5 更新 x:Class 和类名
- [x] A-4.6 更新 MedicalCaseModule.cs 注册
- [x] A-4.7 编译验证

### A-5: Users模块

- [x] A-5.1 重命名 `UserMasterDetailView.xaml` → `UserMasterDetailControl.xaml`
- [x] A-5.2 重命名 `UserMasterDetailView.xaml.cs` → `UserMasterDetailControl.xaml.cs`
- [x] A-5.3 移动文件到 Controls/ 目录
- [x] A-5.4 更新命名空间为 `LYBT.Desktop.Users.Controls`
- [x] A-5.5 更新 x:Class 和类名
- [x] A-5.6 更新 UsersModule.cs 注册
- [x] A-5.7 编译验证

---

## Phase B: 角色台View创建

### B-1: Admin角色台

- [x] B-1.1 创建 `Admin/Views/HerbManagementView.xaml` (使用HerbMasterDetailControl)
- [x] B-1.2 创建 `Admin/Views/FormulaManagementView.xaml` (使用FormulaMasterDetailControl)
- [x] B-1.3 创建 `Admin/Views/PatientManagementView.xaml` (使用PatientMasterDetailControl)
- [x] B-1.4 创建 `Admin/Views/MedicalCaseManagementView.xaml` (使用MedicalCaseMasterDetailControl)
- [x] B-1.5 创建 `Admin/Views/UserManagementView.xaml` (使用UserMasterDetailControl)
- [x] B-1.6 更新 AdminModule.cs 注册新Views
- [x] B-1.7 更新 AdminHomeViewModel 导航目标
- [x] B-1.8 编译验证

### B-2: Clinical角色台

- [x] B-2.1 创建 `Clinical/Views/HerbReferenceView.xaml` (使用HerbMasterDetailControl)
- [x] B-2.2 创建 `Clinical/Views/FormulaReferenceView.xaml` (使用FormulaMasterDetailControl)
- [x] B-2.3 创建 `Clinical/Views/PatientHistoryView.xaml` (使用PatientMasterDetailControl)
- [x] B-2.4 创建 `Clinical/Views/MedicalCaseArchiveView.xaml` (使用MedicalCaseMasterDetailControl)
- [x] B-2.5 更新 ClinicalModule.cs 注册新Views
- [x] B-2.6 更新 ClinicalHomeViewModel 导航目标
- [x] B-2.7 编译验证

---

## Phase C: 清理与验证

- [x] C-1 删除业务模块Views/下的旧MasterDetailView文件
- [x] C-2 清理业务模块空的Views目录或仅保留Dialogs
- [x] C-3 完整编译验证 (0 errors, 7 warnings - 均为预先存在的过时API警告)
- [ ] C-4 运行相关单元测试
- [ ] C-5 手动测试Admin管理功能导航
- [ ] C-6 手动测试Clinical参考功能导航

---

## Task Dependencies

```
Phase A-1 ──┬──> Phase B-1.1 ──┐
Phase A-2 ──┼──> Phase B-1.2   │
Phase A-3 ──┼──> Phase B-1.3   ├──> Phase C
Phase A-4 ──┼──> Phase B-1.4   │
Phase A-5 ──┴──> Phase B-1.5 ──┘
            │
            ├──> Phase B-2.1 ──┐
            ├──> Phase B-2.2   │
            ├──> Phase B-2.3   ├──> Phase C
            └──> Phase B-2.4 ──┘
```

**说明**:
- Phase A各子阶段可并行执行
- Phase B依赖对应的Phase A完成
- Phase C需要等待所有Phase A和B完成

---

## Validation Checklist

**每个模块完成后验证**:
- [x] 编译成功 (0 errors)
- [x] 命名空间正确更新
- [x] 模块注册正确
- [x] Control可被正常引用

**Phase B完成后验证**:
- [x] 导航可用
- [ ] 页面正常显示（需手动测试）

**最终验证**:
- [x] 所有MasterDetailView已转为Control
- [x] Admin角色台5个ManagementView正常注册
- [x] Clinical角色台4个ReferenceView正常注册
- [x] 业务模块无MasterDetailView
- [ ] 全部功能无回归（需手动测试）

---

## 薄包装View模板

```xml
<UserControl x:Class="LYBT.Desktop.Admin.Views.HerbManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:herbControls="clr-namespace:LYBT.Desktop.Herbs.Controls;assembly=LYBT.Desktop.Herbs">

    <!-- 管理员药材管理视图 - 使用Herbs模块的MasterDetailControl -->

    <Grid>
        <!-- 可选：角色特定的顶部工具栏 -->

        <!-- 核心内容：引用业务模块Control -->
        <herbControls:HerbMasterDetailControl />
    </Grid>
</UserControl>
```

---

## 估计工作量

| Phase | 任务数 | 预估时间 | 实际状态 |
|-------|--------|----------|----------|
| A-1 ~ A-5 | 35 | 2-3小时 | 已完成 |
| B-1 | 8 | 1小时 | 已完成 |
| B-2 | 7 | 1小时 | 已完成 |
| C | 6 | 0.5小时 | 编译验证完成 |
| **总计** | **56** | **4-5小时** | **代码变更完成** |
