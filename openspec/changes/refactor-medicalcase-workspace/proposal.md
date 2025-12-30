# Proposal: 看诊工作台布局重构与控件化

**Change ID**: `refactor-medicalcase-workspace`
**Type**: UI Refactoring + Component Extraction
**Priority**: P2 (Post-Release)
**Status**: Applied
**Author**: Claude Code
**Created**: 2025-12-25
**Updated**: 2025-12-30
**Applied**: 2025-12-30
**Target Version**: v1.1.0+

---

## 1. Executive Summary

### 1.1 问题陈述

当前医生工作台的看诊流程存在以下问题：

| 问题 | 影响 | 严重程度 |
|------|------|----------|
| 诊断内容减少后布局比例不合理 | 诊断区50%空间利用率低，处方区拥挤 | 中 |
| 患者选择逻辑无法复用 | 前台挂号需要重复实现患者搜索 | 中 |
| 待诊队列与患者搜索分离不清晰 | PatientSelectionView职责过重(581行) | 中 |
| 患者信息展示不统一 | 不同场景下患者卡片样式不一致 | 低 |

### 1.2 提案目标

1. **重新设计看诊界面布局**: 左25%患者卡片 + 右75%(诊断35%+处方65%)
2. **提取可复用控件**: PatientInfoCardControl, PatientSearchControl, PendingQueueControl
3. **保留原有两步流程**: 患者选择 -> 看诊
4. **确保功能完整**: 经验方查询、历史医案查询等功能不遗漏

### 1.3 技术方案

| 组件 | 当前 | 变更后 | 说明 |
|------|------|--------|------|
| 看诊布局 | 50:50 | 25:75(35:65) | 适应诊断内容减少 |
| 患者信息 | 内联代码 | PatientInfoCardControl | UserControl + DependencyProperty |
| 患者搜索 | PatientSelectionView内嵌 | PatientSearchControl | 可复用控件 |
| 待诊队列 | PatientSelectionView内嵌 | PendingQueueControl | 可复用控件 |

### 1.4 预期收益

| 收益 | 量化指标 |
|------|----------|
| 控件复用 | 3个控件可在前台挂号等场景复用 |
| 代码简化 | PatientSelectionViewModel从581行降至<400行 |
| 布局优化 | 处方区空间增加30%，更符合实际使用 |
| 一致性 | 患者信息展示统一风格 |

---

## 2. Current State Analysis

### 2.1 当前布局

**MedicalCaseWorkspaceView (看诊界面)**:
```
+------------------------------------------------------------------+
|                    医生工作台 - 看诊                              |
+------------------------------------------------------------------+
|         诊断区 50%          |         处方区 50%                  |
| +------------------------+ | +------------------------------------+
| | 现病史                 | | | [经验方查询] [历史医案] [清空]    |
| | 舌诊                   | | +------------------------------------+
| | 脉诊                   | | | 药材列表                          |
| | 中医诊断               | | | ...                               |
| +------------------------+ | +------------------------------------+
+------------------------------------------------------------------+
```

**问题**: 诊断内容已大幅减少(仅4字段)，50%空间浪费严重

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

## 3. Proposed Architecture

### 3.1 新布局方案

**MedicalCaseWorkspaceView (重构后)**:
```
+------------------------------------------------------------------+
|                    医生工作台 - 看诊                              |
+------------------------------------------------------------------+
| 左侧 25% (Min 300px)       |        右侧 75% (自适应)            |
| +------------------------+ | +------------------------------------+
| |                        | | |          诊断区 35%               |
| |  PatientInfoCardControl| | | +----------------------------------+
| |                        | | | | 现病史 | 舌诊 | 脉诊 | 中医诊断 |
| |  - 姓名/性别/年龄      | | | +----------------------------------+
| |  - 挂号时间            | | |                                    |
| |  - 就诊次数            | | |          处方区 65%               |
| |  - [查看历史] 按钮     | | | +----------------------------------+
| |                        | | | | [经验方查询] [历史医案] [清空]  |
| +------------------------+ | | +----------------------------------+
|                            | | | 药材列表 (拼音自动补全)         |
|                            | | +----------------------------------+
+------------------------------------------------------------------+
```

### 3.2 控件化设计

#### 3.2.1 PatientInfoCardControl (新建)

**位置**: `LYBT.Desktop.Shared/Controls/PatientInfoCardControl.xaml`

**DependencyProperty**:
```csharp
public static readonly DependencyProperty PatientProperty;            // 患者数据
public static readonly DependencyProperty DisplayModeProperty;        // Full/Compact/Minimal
public static readonly DependencyProperty ShowHistoryButtonProperty;  // 历史按钮
public static readonly DependencyProperty HistoryCommandProperty;     // 历史命令
public static readonly DependencyProperty ShowVisitCountProperty;     // 显示就诊次数
```

**复用场景**: 看诊工作台、患者选择详情区、医案详情

#### 3.2.2 PatientSearchControl (提取)

**位置**: `LYBT.Desktop.Shared/Controls/PatientSearchControl.xaml`

**DependencyProperty**:
```csharp
public static readonly DependencyProperty SearchKeywordProperty;      // 搜索关键词
public static readonly DependencyProperty PatientsProperty;           // 患者列表
public static readonly DependencyProperty SelectedPatientProperty;    // 选中患者
public static readonly DependencyProperty SearchCommandProperty;      // 搜索命令
public static readonly DependencyProperty PatientSelectedCommandProperty; // 选中回调
public static readonly DependencyProperty ShowCreateButtonProperty;   // 显示新建按钮
public static readonly DependencyProperty ShowPaginationProperty;     // 显示分页
```

**复用场景**: 患者选择界面、前台挂号界面

#### 3.2.3 PendingQueueControl (提取)

**位置**: `LYBT.Desktop.Shared/Controls/PendingQueueControl.xaml`

**DependencyProperty**:
```csharp
public static readonly DependencyProperty PendingQueueProperty;       // 队列数据
public static readonly DependencyProperty SelectedItemProperty;       // 选中项
public static readonly DependencyProperty RefreshCommandProperty;     // 刷新命令
public static readonly DependencyProperty SelectCommandProperty;      // 选择命令
public static readonly DependencyProperty IsCompactModeProperty;      // 紧凑模式
```

**复用场景**: 患者选择界面、前台叫号管理

### 3.3 响应式设计

| 屏幕宽度 | 布局模式 |
|---------|---------|
| >= 1600px | 完整模式：左25%+右75%，所有信息展示 |
| 1280-1600px | 折叠模式：左侧收窄至200px，患者卡片精简 |
| < 1280px | 下拉模式：左侧变为顶部下拉选择器 |

---

## 4. Flow Design (流程设计)

### 4.1 保留原有两步流程

```
+------------------+          +----------------------------------+
| 患者选择界面     |  选择    |        看诊界面                   |
| PatientSelection |  ====>   | MedicalCaseWorkspace             |
+------------------+          +----------------------------------+
| - 待诊队列       |          | 左25%: 患者信息卡片              |
| - 患者搜索       |          | 右75%: 诊断(35%) + 处方(65%)     |
| - 患者列表       |          |                                  |
+------------------+          +----------------------------------+
```

### 4.2 导航事件保留

- `PatientSelectedEvent`: 患者选择完成后触发
- `NavigationParameters`: 传递PatientId

---

## 5. Risk Assessment

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

## 6. Affected Files

### 需修改

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `MedicalCaseWorkspaceView.xaml` | 重构 | 布局调整为25:75 |
| `MedicalCaseWorkspaceViewModel.cs` | 修改 | 集成PatientInfoCard |
| `PatientSelectionView.xaml` | 重构 | 使用提取的控件 |
| `PatientSelectionViewModel.cs` | 简化 | 目标<400行 |

### 需新建

| 文件 | 说明 |
|------|------|
| `LYBT.Desktop.Shared/Controls/PatientInfoCardControl.xaml(.cs)` | 患者信息卡片 |
| `LYBT.Desktop.Shared/Controls/PatientSearchControl.xaml(.cs)` | 患者搜索控件 |
| `LYBT.Desktop.Shared/Controls/PendingQueueControl.xaml(.cs)` | 待诊队列控件 |

### 保持不变

| 文件 | 说明 |
|------|------|
| `FormulaImportDialog.xaml` | 经验方导入对话框 |
| `HistoryCopyDialog.xaml` | 历史医案复制对话框 |
| `PrescriptionPanelViewModel.cs` | 处方面板(含经验方/历史命令) |
| `PendingQueueManager.cs` | 待诊队列管理器 |

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
