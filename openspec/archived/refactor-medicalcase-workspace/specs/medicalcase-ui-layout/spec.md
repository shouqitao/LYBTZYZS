# medicalcase-ui-layout Spec Delta

**Change ID**: refactor-medicalcase-workspace
**Target Spec**: medicalcase-ui-layout
**变更类型**: MODIFIED

---

## MODIFIED Requirements

### Requirement: UI-LAYOUT-002 主内容区分栏
系统 **SHALL** 将主内容区分为左侧患者信息区(25%)和右侧诊断处方区(75%)，右侧内部再分为诊断区(35%)和处方区(65%)。

#### Scenario: 新分栏比例
- **Given** 用户在医案看诊界面
- **When** 查看主内容区
- **Then** 左侧患者信息区占25%宽度(MinWidth=300px, MaxWidth=400px)
- **And** 右侧诊断处方区占75%宽度
- **And** 右侧内部诊断区占35%高度
- **And** 右侧内部处方区占65%高度
- **And** 各区域之间有12px间距

#### Scenario: 患者信息卡片显示
- **Given** 用户已通过患者选择界面选择患者
- **When** 进入看诊界面
- **Then** 左侧显示PatientInfoCardControl控件
- **And** 显示患者姓名、性别、年龄
- **And** 显示就诊次数和挂号时间
- **And** 提供"查看历史"按钮

---

### Requirement: UI-LAYOUT-004 诊断面板布局
系统 **SHALL** 采用2x2网格布局显示诊断字段(4个核心字段: 现病史、舌诊、脉诊、中医诊断)，占右侧区域35%高度。

#### Scenario: 诊断区4字段布局
- **Given** 用户在诊断区
- **When** 填写诊断信息
- **Then** 诊断区采用2x2网格布局
- **And** 第一行: 现病史 + 舌诊
- **And** 第二行: 脉诊 + 中医诊断*
- **And** 中医诊断为必填字段(带红色*标记)

---

### Requirement: UI-LAYOUT-005 处方面板布局
系统 **SHALL** 在处方区标题栏提供"经验方查询"和"历史医案"按钮，药材列表采用DataGrid表格布局，占右侧区域65%高度。

#### Scenario: 处方区标题栏按钮
- **Given** 用户在处方区
- **When** 需要导入经验方或历史处方
- **Then** 标题栏显示"经验方查询"按钮 (Command: OpenFormulaImportDialogCommand)
- **And** 标题栏显示"历史医案"按钮 (Command: OpenHistoryCopyDialogCommand)
- **And** 标题栏显示"清空"按钮

#### Scenario: 药材列表布局
- **Given** 用户已添加药材到处方
- **When** 查看药材列表区域
- **Then** 药材以DataGrid表格形式显示
- **And** 列包含: 药名、剂量、单位、煎法、删除
- **And** 支持拼音自动补全输入

---

## ADDED Requirements

### Requirement: UI-LAYOUT-009 响应式布局断点

系统 **SHALL** 支持响应式布局，根据窗口宽度自动调整布局模式。

#### Scenario: 完整模式 (>=1600px)
- **Given** 用户窗口宽度大于等于1600px
- **When** 查看看诊界面
- **Then** 左侧患者信息区显示完整模式(25%宽度)
- **And** 显示所有患者信息字段
- **And** 显示"查看历史"按钮

#### Scenario: 折叠模式 (1280-1600px)
- **Given** 用户窗口宽度在1280-1600px之间
- **When** 查看看诊界面
- **Then** 左侧患者信息区收窄至200px固定宽度
- **And** 患者卡片显示紧凑模式(仅姓名+性别+年龄)
- **And** 隐藏次要信息

#### Scenario: 最小模式 (<1280px)
- **Given** 用户窗口宽度小于1280px
- **When** 查看看诊界面
- **Then** 左侧患者信息区可选择性折叠或变为顶部下拉
- **And** 主内容区获得更多空间

---

### Requirement: UI-LAYOUT-010 可复用控件规范

系统 **SHALL** 将患者相关UI提取为可复用的UserControl控件。

#### Scenario: PatientInfoCardControl复用
- **Given** 系统需要在多个界面显示患者信息
- **When** 使用PatientInfoCardControl控件
- **Then** 控件通过DependencyProperty接收Patient数据
- **And** 支持Full/Compact/Minimal三种显示模式
- **And** 可配置是否显示历史按钮和就诊次数

#### Scenario: 控件跨模块复用
- **Given** PatientInfoCardControl定义在LYBT.Desktop.Shared项目
- **When** MedicalCase模块需要显示患者卡片
- **Then** 通过NuGet引用或项目引用使用控件
- **And** 通过DependencyProperty绑定数据和命令

---

## 变更影响

### 受影响的文件

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| MedicalCaseWorkspaceView.xaml | 重构 | 布局从50:50改为25:75 |
| MedicalCaseWorkspaceViewModel.cs | 修改 | 添加CurrentPatient属性 |
| PatientInfoCardControl.xaml | 新增 | 患者信息卡片控件 |

### 向后兼容性

- **PrescriptionPanelViewModel**: 保持不变，经验方/历史命令继续工作
- **FormulaImportDialog**: 保持不变
- **HistoryCopyDialog**: 保持不变
- **PatientSelectedEvent**: 保持不变

---

**最后更新**: 2025-12-25
