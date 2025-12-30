## ADDED Requirements

### Requirement: PatientViewControl双列布局

系统 SHALL 使用2x3 Grid布局展示患者信息卡片，避免垂直滚动条。

#### Scenario: 双列卡片布局
- **WHEN** 显示患者详情
- **THEN** 使用2列3行Grid布局
- **AND** 左列显示：基本信息、健康信息、紧急联系人
- **AND** 右列显示：身份信息、联系信息、就诊统计
- **AND** 不使用ScrollViewer包装

#### Scenario: 紧凑间距
- **WHEN** 显示患者信息卡片
- **THEN** 列间距为16px
- **AND** 行间距为12px
- **AND** 整体布局在标准屏幕分辨率下无需滚动

### Requirement: PatientSelection编辑模式

患者选择界面 SHALL 支持inline编辑模式，统一用户体验。

#### Scenario: 新建患者进入编辑模式
- **WHEN** 用户点击"新增"按钮
- **THEN** 右侧Detail区域切换到编辑模式
- **AND** 显示PatientEditControl
- **AND** 显示保存/取消按钮

#### Scenario: 查看模式显示
- **WHEN** 选中已有患者且未进入编辑
- **THEN** 显示PatientViewControl
- **AND** 显示"编辑"和"开始诊断"按钮

#### Scenario: 保存新患者
- **WHEN** 用户填写患者信息并点击"保存"
- **THEN** 调用API创建患者
- **AND** 刷新患者列表
- **AND** 选中新创建的患者
- **AND** 退出编辑模式

#### Scenario: 取消编辑
- **WHEN** 用户点击"取消"按钮
- **THEN** 放弃未保存的更改
- **AND** 退出编辑模式
- **AND** 恢复之前的选中状态
