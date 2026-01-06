# Proposal: 看诊工作台布局重构 V2

**Change ID**: `refactor-medicalcase-workspace`
**Type**: UI Refactoring + Layout Optimization
**Priority**: P1 (Current Sprint)
**Status**: In Progress
**Author**: Claude Code
**Created**: 2025-12-25
**Updated**: 2026-01-04
**Target Version**: v1.1.0+

---

## 1. Executive Summary

### 1.1 问题陈述

当前医生工作台的看诊流程存在以下问题：

| 问题 | 影响 | 严重程度 |
|------|------|----------|
| 诊断和处方分离为两个面板 | 界面割裂，操作繁琐 | 高 |
| 诊断区占用过多空间(35%-50%) | 仅4个字段却占用大量空间 | 高 |
| 待诊队列在患者选择界面 | 看诊时无法快速切换患者 | 中 |
| 左侧患者卡片信息冗余 | 已有患者信息区域，重复展示 | 低 |

### 1.2 提案目标 (V2更新)

1. **统一医案表单**: 诊断和处方合并为连续表单，不再分割面板
2. **左侧整合**: 患者信息(上) + 待诊队列(下)
3. **诊断精简**: 仅3行布局(现病史、舌诊+脉诊、中医诊断)
4. **处方增强**: 使用HerbListControl，占主要空间

### 1.3 技术方案 (V2更新)

| 组件 | 当前(V1) | 变更后(V2) | 说明 |
|------|----------|------------|------|
| 看诊布局 | 左25%(患者卡片) + 右75%(诊断35%:处方65%) | 左25%(患者+待诊) + 右75%(统一表单) | 统一表单 |
| 诊断区 | 独立面板，2x2网格 | 表单顶部3行布局 | 精简 |
| 处方区 | 独立面板 | 表单主体区域 | HerbListControl |
| 待诊队列 | 患者选择界面 | 左侧底部 | 快速切换 |

### 1.4 预期收益

| 收益 | 量化指标 |
|------|----------|
| 界面简洁 | 诊断区从50%降至约15%高度 |
| 操作流畅 | 表单连续，无需切换面板 |
| 快速切换 | 看诊界面可直接选择待诊患者 |
| 处方空间 | 处方区占主界面约85% |

---

## 2. Current State Analysis

### 2.1 当前布局 (V1实现后)

**MedicalCaseWorkspaceView (看诊界面)**:
```
+------------------------------------------------------------------+
|                    医生工作台 - 看诊                              |
+------------------------------------------------------------------+
| 左25%                      |        右75%                         |
| +------------------------+ | +------------------------------------+
| | PatientInfoCard        | | |          诊断区 35%               |
| |                        | | | 现病史 | 舌诊                     |
| |                        | | | 脉诊   | 中医诊断                 |
| +------------------------+ | +------------------------------------+
|                            | |          处方区 65%               |
|                            | | [经验方查询] [历史医案] [清空]    |
|                            | | 药材列表...                       |
+------------------------------------------------------------------+
```

**问题**: 诊断和处方分割为两个独立面板，界面割裂

### 2.2 当前代码结构

| 文件 | 行数 | 职责 |
|------|------|------|
| PatientSelectionView.xaml | 320+ | 待诊队列+患者搜索+患者列表 |
| PatientSelectionViewModel.cs | 581 | 所有逻辑混合 |
| MedicalCaseWorkspaceView.xaml | 450+ | 看诊主界面 |

### 2.3 现有功能清单(必须保留)

| 功能 | 入口 | 实现位置 | 状态 |
|------|------|----------|------|
| 经验方查询 | 处方区标题栏按钮 | `PrescriptionPanelViewModel.OpenFormulaImportDialogCommand` | 保留 |
| 历史医案查询 | 处方区标题栏按钮 | `PrescriptionPanelViewModel.OpenHistoryCopyDialogCommand` | 保留 |
| 待诊队列管理 | 患者选择左侧 | `PendingQueueManager` | 保留 |
| 患者搜索 | 患者选择右侧 | `PatientSelectionViewModel` | 控件化 |
| 导航跳转 | 选中患者后 | `PatientSelectedEvent` | 保留 |

---

## 3. Proposed Architecture (V2)

### 3.1 新布局方案

**MedicalCaseWorkspaceView (V2重构后)**:
```
+------------------------------------------------------------------+
|                    医生工作台 - 看诊                              |
+------------------------------------------------------------------+
| 左侧 25% (Min 280px)       |        右侧 75% (自适应)             |
| +------------------------+ | +------------------------------------+
| | PatientInfoCard        | | |    [经验方] [历史处方] [清空] ←右上|
| | - 姓名/性别/年龄       | | +------------------------------------+
| | - 挂号时间             | | | 诊断区 (固定高度约120px)          |
| | - [查看历史]           | | | Row1: [现病史...................]  |
| +------------------------+ | | Row2: [舌诊.....] [脉诊........]  |
| | 待诊队列 (可折叠)      | | | Row3: [中医诊断*...............]  |
| | - 王某 等待中          | | +------------------------------------+
| | - 李某 等待中          | | | 处方区 (占剩余全部空间)           |
| | - 张某 (挂起)          | | | HerbListControl (4列网格)         |
| | [刷新]                 | | |   药材1  药材2  药材3  药材4      |
| +------------------------+ | +------------------------------------+
|                            | | 共X味 | 付数/用法/单价 信息区      |
+------------------------------------------------------------------+
```

**关键变化 (V2)**:
1. **诊断区精简**: 从独立面板变为表单顶部区域，固定高度约120px
2. **处方区扩展**: 占主界面约85%空间
3. **待诊队列移入**: 从患者选择界面移到看诊界面左侧
4. **统一表单**: 诊断+处方连续排列，无分割线

### 3.2 四大核心控件 (V2)

#### 概览

| 控件 | 位置 | 状态 | 用途 |
|------|------|------|------|
| PatientInfoCardControl | `Infrastructure/Controls/` | 已有 | 患者信息展示 |
| PendingQueueControl | `Infrastructure/Controls/` | 已有 | 待诊队列列表 |
| **MedicalCaseEditControl** | `MedicalCase/Controls/` | **新建** | 医案编辑表单 |
| **MedicalCaseViewControl** | `MedicalCase/Controls/` | **新建** | 医案只读预览 |

#### 3.2.1 MedicalCaseEditControl (新建)

**位置**: `LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`

**职责**: 统一的医案编辑表单，包含诊断区+处方区

**布局**:
```
+--------------------------------------------------+
|        [经验方] [历史处方] [清空]  ←右上角工具栏  |
+--------------------------------------------------+
| 诊断区 (固定高度约120px)                          |
| Row1: [现病史...............................]    |
| Row2: [舌诊..........] [脉诊...............]    |
| Row3: [中医诊断*...........................]    |
+--------------------------------------------------+
| 处方区 (占剩余空间)                               |
| +----------------------------------------------+ |
| | HerbListControl (4列网格)                    | |
| |   药材1  药材2  药材3  药材4                 | |
| |   药材5  药材6  ...                         | |
| +----------------------------------------------+ |
| | 共X味 | 付数/用法/单价信息区                  | |
+--------------------------------------------------+
```

**DependencyProperty**:
```csharp
public static readonly DependencyProperty ConsultationProperty;      // 诊断数据
public static readonly DependencyProperty HerbItemsProperty;         // 药材列表
public static readonly DependencyProperty AllHerbsProperty;          // 全部药材(自动补全)
public static readonly DependencyProperty PrescriptionInfoProperty;  // 处方信息(付数/用法)
// Commands
public static readonly DependencyProperty ImportFormulaCommandProperty;   // 导入经验方
public static readonly DependencyProperty ImportHistoryCommandProperty;   // 导入历史医案
public static readonly DependencyProperty ClearAllCommandProperty;        // 清空处方
```

#### 3.2.2 MedicalCaseViewControl (新建)

**位置**: `LYBT.Desktop.MedicalCase/Controls/MedicalCaseViewControl.xaml`

**职责**: 医案只读预览，用于详情查看

**布局**:
```
+--------------------------------------------------+
| 诊断信息                                          |
| 现病史: XXXXXXX                                  |
| 舌诊: XX  脉诊: XX                               |
| 中医诊断: XXXXXXX                                |
+--------------------------------------------------+
| 处方内容                                          |
| HerbListControl (IsEditMode=False)              |
|   药材1 10g  药材2 15g  药材3 12g  ...          |
+--------------------------------------------------+
| 付数: X付  用法: XXX                             |
+--------------------------------------------------+
```

**DependencyProperty**:
```csharp
public static readonly DependencyProperty MedicalCaseProperty;   // 医案数据
public static readonly DependencyProperty ShowPrintButtonProperty;  // 显示打印按钮
public static readonly DependencyProperty PrintCommandProperty;     // 打印命令
```

#### 3.2.3 HerbListControl (已有，Herbs模块)

**位置**: `LYBT.Desktop.Herbs/Controls/HerbList/HerbListControl.xaml`

**使用方式**:
```xml
<!-- 编辑模式 -->
<herbList:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4" />

<!-- 只读模式 -->
<herbList:HerbListControl
    HerbItems="{Binding HerbItems}"
    IsEditMode="False"
    Columns="4" />
```

### 3.3 诊断区布局规范 (V2新增)

**布局**: 3行固定高度区域

| 行 | 字段 | 宽度 | 高度 |
|----|------|------|------|
| Row1 | 现病史 | 100% | 40px |
| Row2 | 舌诊 + 脉诊 | 各50% | 40px |
| Row3 | 中医诊断* | 100% | 40px |

**总高度**: 约120px (含边距)

```xml
<StackPanel>
    <!-- Row1: 现病史 -->
    <Grid Height="40">
        <TextBlock Text="现病史" Width="60"/>
        <TextBox Text="{Binding Consultation.History}" Margin="60,0,0,0"/>
    </Grid>

    <!-- Row2: 舌诊 + 脉诊 -->
    <Grid Height="40">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <StackPanel Grid.Column="0" Orientation="Horizontal">
            <TextBlock Text="舌诊" Width="40"/>
            <TextBox Text="{Binding Consultation.TongueDiagnosis}"/>
        </StackPanel>
        <StackPanel Grid.Column="1" Orientation="Horizontal">
            <TextBlock Text="脉诊" Width="40"/>
            <TextBox Text="{Binding Consultation.PulseDiagnosis}"/>
        </StackPanel>
    </Grid>

    <!-- Row3: 中医诊断(必填) -->
    <Grid Height="40">
        <TextBlock Text="中医诊断*" Width="70"/>
        <TextBox Text="{Binding Consultation.Diagnosis}" Margin="70,0,0,0"/>
    </Grid>
</StackPanel>
```

---

## 4. Flow Design (V2更新)

### 4.1 简化流程

**V2变化**: 待诊队列移入看诊界面，减少界面切换

```
+------------------+          +------------------------------------------+
| 患者选择界面     |  选择    |        看诊界面(V2)                       |
| PatientSelection |  ====>   | MedicalCaseWorkspace                     |
+------------------+          +------------------------------------------+
| - 待诊队列       |          | 左25%:                                   |
| - 患者搜索       |          |   - 患者信息卡片 (上)                     |
| - 患者列表       |          |   - 待诊队列 (下) <-- 新增               |
+------------------+          | 右75%: 统一医案表单                       |
                              |   - 诊断(顶部3行)                         |
                              |   - 处方(HerbListControl)                 |
                              +------------------------------------------+
```

### 4.2 看诊界面内切换患者

**V2新增**: 可在看诊界面左下角待诊队列直接切换患者

1. 点击待诊队列中的患者
2. 当前医案自动暂存
3. 加载新患者医案

---

## 5. 控件清理 (V2新增)

### 5.1 需要删除的无用控件

| 控件 | 位置 | 删除原因 |
|------|------|----------|
| `MedicalCaseDetailViewModel.cs` | MedicalCase/ViewModels/ | 已被MedicalCaseWorkspaceViewModel替代 |
| `ConsultationFormViewModel.cs` | Consultation/ViewModels/ | 诊断字段精简后不再需要独立ViewModel |
| `DuplicateHerbAlertDialog` | MedicalCase/Views/ | 合并到HerbListControl内部处理 |
| `DuplicateHerbAlertDialogViewModel.cs` | MedicalCase/ViewModels/ | 配套删除 |
| `HistoryPrescriptionSelectionDialog` | MedicalCase/Views/ | 重复功能，保留HistoryCopyDialog |
| `HistoryPrescriptionSelectionDialogViewModel.cs` | MedicalCase/ViewModels/ | 配套删除 |
| `AuditLogDialog/AuditReasonDialog` | MedicalCase/Dialogs/ | 审计功能延迟到v2.0 |
| `HerbDetailViewModel.cs` | Herbs/ViewModels/ | 已被HerbMasterDetailControl替代 |
| `PatientDetailViewModel.cs` | Patients/ViewModels/ | 已被PatientMasterDetailControl替代 |
| `PatientDetailView.xaml` | Patients/Views/ | 配套删除 |
| `QuickCreatePatientDialog` | Patients/Views/ | 功能合并到PatientEditControl |
| `UserDetailViewModel.cs` | Users/ViewModels/ | 已被UserMasterDetailControl替代 |
| `FormulaDetailViewModel.cs` | Formula/ViewModels/ | 已被FormulaMasterDetailControl替代 |
| `FormulaValidationViewModel.cs` | Formula/ViewModels/ | 验证逻辑移入Service层 |
| `EditFormulaDialog` | Formula/Views/ | 已被FormulaEditControl替代 |

### 5.2 需要删除的基础控件(Infrastructure)

| 控件 | 删除原因 |
|------|----------|
| `HerbCardControl` | 已被HerbItemControl替代 |
| `HerbListView` | 已被HerbListControl替代 |
| `HerbListEditor` | 已被HerbListControl替代 |
| `HerbItem/` 目录(Infrastructure) | 已迁移到Herbs模块 |
| `HerbList/` 目录(Infrastructure) | 已迁移到Herbs模块 |
| `HerbItemDto.cs`(Infrastructure) | 已迁移到Herbs模块 |

### 5.3 清理验证

- [ ] 删除后编译通过
- [ ] 无悬挂引用
- [ ] 相关功能正常工作

---

## 6. Risk Assessment

### 5.1 风险矩阵

| 风险 | 可能性 | 影响 | 风险等级 | 缓解措施 |
|------|--------|------|----------|----------|
| 控件拆分导致绑定失效 | 中 | 中 | **中** | 充分测试DependencyProperty |
| 布局在特殊分辨率异常 | 中 | 中 | **中** | 多分辨率测试 |
| 经验方/历史医案按钮丢失 | 低 | 高 | **中** | 保留PrescriptionPanelViewModel现有命令 |
| 功能回归 | 低 | 高 | **中** | 渐进式迁移，每阶段验证 |

### 5.2 验收标准

- [ ] 3个控件可独立使用，DependencyProperty绑定正常
- [ ] 看诊界面布局: 左25%患者卡片 + 右75%诊断处方
- [ ] 诊断区4字段: 现病史、舌诊、脉诊、中医诊断(必填)
- [ ] **经验方查询功能正常** (FormulaImportDialog)
- [ ] **历史医案查询功能正常** (HistoryCopyDialog)
- [ ] 响应式布局在1280px/1600px断点正常切换
- [ ] 完整流程测试: 患者选择 -> 看诊 -> 保存
- [ ] PatientSelectionViewModel行数 < 400行
- [ ] 编译通过，无警告

---

## 7. Affected Files (V2更新)

### 需新建

| 文件 | 说明 |
|------|------|
| `MedicalCase/Controls/MedicalCaseEditControl.xaml(.cs)` | **医案编辑控件** |
| `MedicalCase/Controls/MedicalCaseViewControl.xaml(.cs)` | **医案查看控件** |

### 需修改

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `MedicalCaseWorkspaceView.xaml` | 重构 | 使用4个控件组合 |
| `MedicalCaseWorkspaceViewModel.cs` | 简化 | 集成待诊队列 |
| `MedicalCaseMasterDetailControl.xaml` | 修改 | 使用新View/Edit控件 |

### 需删除

| 文件 | 删除原因 |
|------|----------|
| `MedicalCaseDetailViewModel.cs` | 已被控件替代 |
| `ConsultationFormViewModel.cs` | 诊断精简 |
| `DuplicateHerbAlertDialog.xaml(.cs)` | 功能合并 |
| `HistoryPrescriptionSelectionDialog.xaml(.cs)` | 功能重复 |
| `AuditLogDialog.xaml(.cs)` | 延迟v2.0 |
| `AuditReasonDialog.xaml(.cs)` | 延迟v2.0 |
| `HerbDetailViewModel.cs` | 已被控件替代 |
| `PatientDetailViewModel.cs` | 已被控件替代 |
| `PatientDetailView.xaml(.cs)` | 已被控件替代 |
| `QuickCreatePatientDialog.xaml(.cs)` | 功能合并 |
| `UserDetailViewModel.cs` | 已被控件替代 |
| `FormulaDetailViewModel.cs` | 已被控件替代 |
| `FormulaValidationViewModel.cs` | 逻辑迁移 |
| `EditFormulaDialog.xaml(.cs)` | 已被控件替代 |
| `Infrastructure/Controls/HerbCardControl.*` | 已迁移 |
| `Infrastructure/Controls/HerbListView.*` | 已迁移 |
| `Infrastructure/Controls/HerbListEditor.*` | 已迁移 |
| `Infrastructure/Controls/HerbItem/*` | 已迁移 |
| `Infrastructure/Controls/HerbList/*` | 已迁移 |
| `Infrastructure/Models/HerbItemDto.cs` | 已迁移 |

### 保持不变

| 文件 | 说明 |
|------|------|
| `FormulaImportDialog.xaml` | 经验方导入对话框 |
| `HistoryCopyDialog.xaml` | 历史医案复制对话框 |
| `PrescriptionPanelViewModel.cs` | 处方面板(含经验方/历史命令) |
| `PendingQueueManager.cs` | 待诊队列管理器 |
| `PatientInfoCardControl` | 已有控件 |
| `PendingQueueControl` | 已有控件 |
| `HerbListControl` | Herbs模块已有 |

---

## 7. Spec Deltas

本提案将修改以下spec:

### 7.1 medicalcase-ui-layout

**变更类型**: MODIFIED

**变更内容**:
- 主分栏比例从50:50改为25:75
- 右侧内部比例从无分栏改为35:65(诊断:处方)
- 新增左侧PatientInfoCard区域规范
- 新增响应式断点规范

---

## 8. Approval

**提案状态**: 待审批

**审批要求**:
- [ ] 布局设计评审通过
- [ ] 控件接口设计确认
- [ ] 实施计划确认

**下一步**: 用户确认后进入apply阶段

---

## 9. Implementation Record (2025-12-30)

### 9.1 控件实现

| 控件 | 文件位置 | 状态 | 特性 |
|------|----------|------|------|
| PatientInfoCardControl | `LYBT.Desktop.Infrastructure/Controls/` | 已完成 | Full/Compact/Minimal模式，历史按钮，就诊次数 |
| PatientSearchControl | `LYBT.Desktop.Infrastructure/Controls/` | 已完成 | 搜索框，患者列表，分页，新建按钮 |
| PendingQueueControl | `LYBT.Desktop.Infrastructure/Controls/` | 已完成 | 三种状态颜色，空状态提示，刷新功能 |

### 9.2 支持文件

| 文件 | 说明 |
|------|------|
| `PatientCardDisplayMode.cs` | Full/Compact/Minimal枚举 |
| `PatientCardDisplayModeToVisibilityConverter.cs` | 显示模式转换器 |
| `PatientDisplayModel.cs` | 患者显示数据模型 |

### 9.3 布局重构

**MedicalCaseWorkspaceView.xaml**:
- 主布局: 左25%(固定300px) + 右75%(自适应)
- 左侧: PatientInfoCardControl + PendingQueueControl
- 右侧: 诊断区35% + 处方区65%
- 统一间距16px

### 9.4 验证状态

- [x] 3个控件已创建，DependencyProperty绑定正常
- [x] 看诊界面布局: 左25%患者卡片 + 右75%诊断处方
- [x] 经验方查询功能保留 (FormulaImportDialog)
- [x] 历史医案查询功能保留 (HistoryCopyDialog)
- [x] 编译通过 (0错误, 0警告)
- [ ] 手动测试验证 (待用户验证)

---

**文档版本**: v2.0
**最后更新**: 2025-12-30
