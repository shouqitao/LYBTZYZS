# Prescription Import Dialogs Specification

## ADDED Requirements

### Requirement: CustomDialogWindowStyle资源

系统SHALL在共享资源中提供`CustomDialogWindowStyle`窗口样式，用于Prism DialogService的对话框显示。

#### Scenario: 对话框使用统一窗口样式

- **WHEN** 任何使用`prism:Dialog.WindowStyle="{StaticResource CustomDialogWindowStyle}"`的对话框被显示
- **THEN** 对话框SHALL使用无边框窗口样式
- **AND** 对话框SHALL居中于父窗口显示
- **AND** 对话框大小SHALL根据内容自适应

### Requirement: 验方导入对话框

系统SHALL提供FormulaImportDialog对话框，允许用户从验方库搜索并选择验方导入药材到当前处方。

#### Scenario: 打开验方导入对话框

- **WHEN** 用户在处方编辑面板点击"经验方查询"按钮
- **THEN** 系统SHALL显示FormulaImportDialog对话框
- **AND** 对话框SHALL自动加载验方列表（通过IFormulaRepository）

#### Scenario: 搜索筛选验方

- **WHEN** 用户在搜索框输入文本
- **THEN** 系统SHALL实时筛选验方列表
- **AND** 筛选条件SHALL包括：验方名称、功效(Effect)、适应症(Indications)
- **AND** 筛选SHALL为模糊匹配（不区分大小写）

#### Scenario: 预览验方药材组成

- **WHEN** 用户选中一个验方
- **THEN** 系统SHALL在预览区域显示该验方的药材组成
- **AND** 显示格式SHALL为：`药材名称 剂量 单位`（逗号分隔）
- **AND** 若验方无药材，SHALL显示"该验方暂无药材组成"

#### Scenario: 确认导入验方

- **WHEN** 用户选中一个有药材的验方并点击"确认导入"
- **THEN** 系统SHALL关闭对话框
- **AND** 返回结果SHALL包含：SelectedFormula（FormulaDto）、SelectedHerbs（List<FormulaHerbItemDto>）
- **AND** 调用方SHALL接收到ButtonResult.OK

#### Scenario: 取消导入

- **WHEN** 用户点击"取消"按钮
- **THEN** 系统SHALL关闭对话框
- **AND** 返回结果SHALL为ButtonResult.Cancel

### Requirement: 历史处方复制对话框

系统SHALL提供HistoryCopyDialog对话框，允许用户从当前患者的历史处方中选择药材复制到当前处方。

#### Scenario: 打开历史处方复制对话框

- **WHEN** 用户在处方编辑面板点击"历史处方查询"按钮
- **THEN** 系统SHALL显示HistoryCopyDialog对话框
- **AND** 对话框SHALL接收PatientId和PatientName参数
- **AND** 对话框SHALL显示当前患者姓名
- **AND** 对话框SHALL自动加载该患者的历史医案（通过IMedicalCaseRepository）

#### Scenario: 筛选历史医案

- **WHEN** 用户在搜索框输入文本
- **THEN** 系统SHALL实时筛选历史医案列表
- **AND** 筛选条件SHALL包括：诊断(Diagnosis)、主诉(ChiefComplaint)、就诊日期
- **AND** 列表SHALL按就诊日期倒序排列（最新在前）
- **AND** 列表SHALL仅显示有处方的医案（PrescriptionId不为空）

#### Scenario: 预览历史处方药材

- **WHEN** 用户选中一个历史医案
- **THEN** 系统SHALL在预览区域显示该医案处方的药材组成
- **AND** 显示格式SHALL为：`药材名称 剂量 单位`（逗号分隔）
- **AND** 若处方无药材，SHALL显示"该历史处方暂无药材记录"

#### Scenario: 确认复制历史处方

- **WHEN** 用户选中一个有药材的医案并点击"确认复制"
- **THEN** 系统SHALL关闭对话框
- **AND** 返回结果SHALL包含：SelectedCase（MedicalCaseDto）、SelectedItems（List<PrescriptionItemDto>）
- **AND** 调用方SHALL接收到ButtonResult.OK

### Requirement: 药材导入处理

系统SHALL提供PrescriptionImportHandler处理导入的药材，包括重复检测和列表更新。

#### Scenario: 导入验方药材

- **WHEN** FormulaImportDialog返回选中的验方和药材
- **THEN** PrescriptionImportHandler SHALL将FormulaHerbItemDto转换为PrescriptionHerbItemViewModel
- **AND** 转换时SHALL保留：HerbId、HerbName、Quantity、Unit
- **AND** 新药材SHALL追加到现有药材列表末尾

#### Scenario: 导入历史处方药材

- **WHEN** HistoryCopyDialog返回选中的处方项
- **THEN** PrescriptionImportHandler SHALL将PrescriptionItemDto转换为PrescriptionHerbItemViewModel
- **AND** 转换时SHALL保留：HerbId、HerbName、Quantity、Unit、UnitPrice
- **AND** 新药材SHALL追加到现有药材列表末尾

#### Scenario: 检测重复药材

- **WHEN** 导入的药材中存在与现有列表相同HerbId的药材
- **THEN** 系统SHALL显示DuplicateHerbAlertDialog提醒用户
- **AND** 对话框SHALL显示重复药材的名称和两侧的剂量
- **AND** 用户可选择"替换剂量"或"保留两者"

#### Scenario: 导入后刷新UI

- **WHEN** 药材成功导入到列表
- **THEN** 系统SHALL刷新药材卡片显示
- **AND** 系统SHALL重新计算单剂价格和总价
- **AND** 系统SHALL确保列表末尾保持一个空白输入框

## UI Design Specifications

### FormulaImportDialog布局

```
+--------------------------------------------------+
| 从验方导入                                        |
+--------------------------------------------------+
| [搜索框：输入验方名称或拼音码搜索...]            |
+--------------------------------------------------+
| 验方名称    | 药材数量 | 分类   | 状态           |
|-------------|----------|--------|----------------|
| 麻黄汤      | 4        | 解表剂 | 启用           |
| 桂枝汤      | 5        | 解表剂 | 启用           |
| ...         | ...      | ...    | ...            |
+--------------------------------------------------+
| 药材组成预览:                                     |
| 麻黄9g, 桂枝6g, 杏仁10g, 甘草3g                  |
+--------------------------------------------------+
| 共 X 个验方              [取消] [确认导入]       |
+--------------------------------------------------+
```

### HistoryCopyDialog布局

```
+--------------------------------------------------+
| 从历史处方复制                                    |
+--------------------------------------------------+
| 当前患者: 张三                                    |
+--------------------------------------------------+
| [搜索框：输入诊断关键词或日期搜索...]            |
+--------------------------------------------------+
| 就诊日期    | 诊断         | 主诉       | 状态   |
|-------------|--------------|------------|--------|
| 2024-12-01  | 风寒感冒     | 发热咳嗽   | 已完成 |
| 2024-11-15  | 气虚乏力     | 疲倦纳差   | 已完成 |
| ...         | ...          | ...        | ...    |
+--------------------------------------------------+
| 处方药材预览:                                     |
| 麻黄9g, 桂枝6g, 杏仁10g, 甘草3g, 生姜3片, 大枣4枚|
+--------------------------------------------------+
| 共 X 条历史处方           [取消] [确认复制]      |
+--------------------------------------------------+
```
