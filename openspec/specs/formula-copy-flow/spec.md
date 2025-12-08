# formula-copy-flow Specification

## Purpose
TBD - created by archiving change implement-formula-copy-flow. Update Purpose after archive.
## Requirements
### Requirement: COPY-001 验方所有权判断
FormulaDetailViewModel SHALL 提供 `IsOwnFormula` 属性判断当前用户是否为验方创建者。

#### Scenario: 判断自己创建的验方
- **GIVEN** 当前登录用户ID为 `user-123`
- **AND** 查看的验方 CreatedBy 为 `user-123`
- **WHEN** 检查 IsOwnFormula 属性
- **THEN** 返回 true

#### Scenario: 判断他人创建的验方
- **GIVEN** 当前登录用户ID为 `user-123`
- **AND** 查看的验方 CreatedBy 为 `admin-456`
- **WHEN** 检查 IsOwnFormula 属性
- **THEN** 返回 false

### Requirement: COPY-002 编辑权限控制
FormulaDetailView SHALL 根据验方所有权控制编辑按钮可见性。

#### Scenario: 自己的验方显示编辑按钮
- **GIVEN** IsOwnFormula 为 true
- **WHEN** 在查看模式打开验方详情
- **THEN** 显示"编辑"按钮

#### Scenario: 他人的验方隐藏编辑按钮
- **GIVEN** IsOwnFormula 为 false
- **WHEN** 在查看模式打开验方详情
- **THEN** 隐藏"编辑"按钮

### Requirement: COPY-003 复制按钮显示
FormulaDetailView SHALL 在查看模式下显示"复制为我的验方"按钮。

#### Scenario: 查看模式显示复制按钮
- **GIVEN** 用户在查看模式打开任意验方
- **WHEN** 页面加载完成
- **THEN** 显示"复制为我的验方"按钮

#### Scenario: 编辑模式隐藏复制按钮
- **GIVEN** 用户在编辑模式
- **WHEN** 页面显示
- **THEN** 隐藏"复制为我的验方"按钮

### Requirement: COPY-004 复制流程导航
点击"复制为我的验方"按钮 SHALL 导航到新建验方界面并预填充数据。

#### Scenario: 执行复制导航
- **GIVEN** 用户正在查看验方 "感冒方"
- **WHEN** 点击"复制为我的验方"按钮
- **THEN** 导航到 FormulaDetailView
- **AND** 传递 `CopyFromFormula` 参数
- **AND** 设置 `ReadOnly` 为 false

### Requirement: COPY-005 复制数据预填充
导航到新建界面时 SHALL 从源验方预填充所有字段。

#### Scenario: 预填充验方基本信息
- **GIVEN** 源验方名称为 "感冒方"
- **WHEN** 加载复制的验方数据
- **THEN** 名称设置为 "感冒方(副本)"
- **AND** Id 设置为 Empty (Guid.Empty)
- **AND** IsShared 设置为 false
- **AND** 其他字段复制源验方值

#### Scenario: 预填充药材列表
- **GIVEN** 源验方包含 5 味药材
- **WHEN** 加载复制的验方数据
- **THEN** HerbItems 包含相同的 5 味药材
- **AND** 每味药材的用量和加工方法保持不变

### Requirement: COPY-006 保存复制的验方
保存复制的验方 SHALL 作为新建处理，创建新记录。

#### Scenario: 保存创建新验方
- **GIVEN** 用户通过复制创建的验方（Id 为 Empty）
- **WHEN** 点击保存按钮
- **THEN** 调用 Create API 创建新验方
- **AND** 新验方 CreatedBy 为当前用户
- **AND** 不影响源验方数据

