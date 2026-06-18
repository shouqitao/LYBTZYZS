# 看诊工作流优化 — 设计规格

> 日期: 2026-06-18
> 子项目: B (工作流优化)

## [S1] 问题

当前看诊流程需要 3 步导航：`ClinicalHome（卡片页）→ PatientSelectionView → MedicalCaseWorkspaceView`。每次看诊都要经过卡片导航页，多余且缓慢。

MedicalCaseWorkspaceView 使用左右 50/50 分屏，诊断和处方区域空间不足。看诊时无法看到患者历史就诊信息。

## [S2] 目标

1. 将看诊入口从 3 步减少到 1 步——登录后直接进入看诊工作台
2. 患者列表常驻看诊页面左侧，无需返回选择
3. 看诊工作区显示患者历史信息
4. 其他功能（药材/验方/挂号/医案）通过 Sidebar 切换，全宽显示

## [S3] 方案 — A/C 混合：看诊页面自带患者列表

### 布局

```
┌──────────┬───────────┬───────────────────────────┐
│ Sidebar  │ 患者列表   │  看诊工作区                 │
│ (常驻)   │ (看诊区内)  │                           │
│          │           │  患者信息栏（历史+当前）      │
│ ▶ 看诊    │ 🔍 搜索    │                           │
│   药材    │ ● 张三 60  │  ┌─诊断──────┐ ┌─处方─────┐│
│   验方    │ ○ 李四 45  │  │ 现病史     │ │ 药材列表  ││
│   挂号    │ ○ 王五 30  │  │ 舌脉       │ │ 剂量     ││
│   医案    │           │  │ 中医诊断   │ │ 用法     ││
│ ─────    │ [+ 新患者]  │  └──────────┘ └─────────┘│
│   设置    │           │  [保存草稿] [完成] [打印]    │
│   账户    │           │                              │
└──────────┴───────────┴───────────────────────────┘
```

### 导航行为

| Sidebar 项 | 右侧 ContentRegion 显示 | 布局 |
|-----------|----------------------|------|
| **看诊**（默认） | ClinicalWorkspaceView | 三栏：Sidebar + 患者列表 + 工作区 |
| 药材 | HerbManagementView | 两栏：Sidebar + 全宽管理 |
| 验方 | FormulaManagementView | 两栏 |
| 挂号 | RegistrationListView | 两栏 |
| 医案 | MedicalCaseManagementView | 两栏 |
| 设置 | SystemSettingsView | 两栏 |

### 登录后默认行为

- 医生登录后直接导航到"看诊"（ClinicalWorkspaceView），不再经过 ClinicalHomeView 卡片页
- ClinicalHomeView 保留但不再是默认入口（可通过设置改为卡片模式）

## [S4] ClinicalWorkspaceView 组件结构

### 新建文件

```
Roles/LYBT.Desktop.Clinical/
  Views/
    ClinicalWorkspaceView.xaml         ← 新建：复合视图容器
    ClinicalWorkspaceView.xaml.cs
  ViewModels/
    ClinicalWorkspaceViewModel.cs      ← 新建：协调患者列表和工作区
```

### ClinicalWorkspaceView XAML 结构

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300" />   <!-- 患者列表列 -->
        <ColumnDefinition Width="Auto" />  <!-- 分割线 -->
        <ColumnDefinition Width="*" />     <!-- 工作区 -->
    </Grid.ColumnDefinitions>

    <!-- 左列：患者列表 + 搜索 -->
    <patients:PatientSelectionControl Grid.Column="0"
        SelectedPatient="{Binding SelectedPatient, Mode=TwoWay}"
        PatientDoubleClicked="..." />

    <GridSplitter Grid.Column="1" Width="4" />

    <!-- 右列：看诊工作区 -->
    <medicalCase:MedicalCaseEditControl Grid.Column="2"
        Consultation="{Binding CurrentConsultation}"
        Prescription="{Binding CurrentPrescription}"
        PatientInfo="{Binding SelectedPatient}" />
</Grid>
```

### ClinicalWorkspaceViewModel 职责

1. 管理 `SelectedPatient`（绑定到 PatientSelectionControl）
2. 当 `SelectedPatient` 变化时，创建/加载对应的 MedicalCase
3. 将 MedicalCase 数据绑定到 MedicalCaseEditControl
4. 提供"保存草稿"、"完成"、"打印"命令
5. 显示患者历史信息（最近 N 次就诊摘要）

### 患者历史信息区

在 MedicalCaseEditControl 顶部增加一个折叠面板：

```
┌─ 张三 男 60岁 | 就诊3次 | 最后: 2026-06-10 ──────────┐
│ [▼ 历史记录]                                          │
│   2026-06-10: 感冒风寒 → 麻黄汤加减 (7剂)             │
│   2026-05-15: 脾胃虚弱 → 四君子汤 (5剂)              │
│   2026-04-01: 失眠 → 酸枣仁汤 (10剂)                 │
└──────────────────────────────────────────────────────┘
```

数据来源：通过 `IMedicalCaseRepository.GetByPatientIdAsync(patientId)` 获取历史列表。

## [S5] 患者列表交互

### 搜索
- 顶部搜索框，输入姓名/拼音/身份证号即时过滤
- 复用现有 `PatientSelectionControl` 组件

### 排序
- 默认按最近就诊时间降序（最近来看过的在最前面）
- 可切换为按姓名排序

### 标记
- 有未完成医案的患者显示橙色圆点
- 正在候诊（有挂号记录）的患者显示蓝色标记

### 新建患者
- 列表底部 `[+ 新患者]` 按钮
- 点击弹出 PatientEditDialog（已有），创建后自动选中

## [S6] 看诊工作区优化

### 诊断区 + 处方区分栏

保留左右分栏但优化比例：
- 诊断区：40%（足够录入四诊信息）
- 处方区：60%（处方需要更多空间展示药材列表）

或者使用 Tab 切换（诊断/处方来回切换），让每个区域占满全宽。

### 快捷操作
- `Ctrl+S`：保存草稿
- `Ctrl+Enter`：完成看诊
- `Ctrl+P`：打印处方
- `Esc`：返回患者列表

## [S7] 修改范围

### 新建
- `Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` + `.cs`
- `Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs`

### 修改
- `Roles/LYBT.Desktop.Clinical/ClinicalModule.cs` — 注册 ClinicalWorkspaceView 导航
- `Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` — 添加 `ClinicalWorkspace` 常量
- `Shell/Services/NavigationCoordinator.cs` — 医生登录后默认导航到 ClinicalWorkspace
- `Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` — 顶部增加患者历史信息折叠面板
- `Core/LYBT.Desktop.Infrastructure/Controls/SidebarControl.xaml` — "看诊"项导航目标改为 ClinicalWorkspace

### 不变
- PatientSelectionControl（复用现有组件）
- MedicalCaseEditControl 核心逻辑（仅增加历史信息面板）
- ClinicalHomeView（保留，不再是默认入口）
- SidebarControl 整体结构

## [S8] 不在范围内

- 处方录入的交互优化（子项目 C 中处理）
- 性能优化（子项目 D）
- 打印模板修改
