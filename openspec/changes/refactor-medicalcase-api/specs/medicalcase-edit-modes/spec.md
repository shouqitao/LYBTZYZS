# medicalcase-edit-modes Spec Delta

## MODIFIED Requirements

### Requirement: EDITMODE-003 暂存医案
系统 **SHALL** 提供"暂存医案"功能，通过API端点保存当前数据并切换到只读状态。

#### Scenario: 点击暂存医案
- **GIVEN** 用户正在编辑医案
- **WHEN** 用户点击"暂存医案"按钮
- **THEN** 系统调用 `PUT /api/v1/medicalcases/{id}/draft`
- **AND** 医案状态设置为Draft
- **AND** 界面切换到只读状态
- **AND** 用户留在当前界面

#### Scenario: 暂存API调用
- **GIVEN** 客户端发起暂存请求
- **WHEN** 调用 `PUT /api/v1/medicalcases/{id}/draft` 并传递ConsultationInputDto
- **THEN** 服务端保存诊断和处方数据
- **AND** 返回更新后的MedicalCase实体

#### Scenario: 暂存按钮Tooltip
- **GIVEN** 用户鼠标悬停在"暂存医案"按钮上
- **WHEN** 显示Tooltip
- **THEN** Tooltip内容为"保存当前进度，可随时点击'修改医案'继续编辑"

---

### Requirement: EDITMODE-009 保存后状态
系统 **SHALL** 在Management模式保存后通过API切换到只读状态并留在当前界面。

#### Scenario: 保存后状态切换
- **GIVEN** 用户在Management模式编辑状态
- **WHEN** 用户点击"保存医案"并调用 `PUT /api/v1/medicalcases/{id}/draft`
- **THEN** API返回成功
- **AND** 界面切换到只读状态
- **AND** 用户留在当前医案界面
- **AND** 右上角显示"编辑医案"按钮

#### Scenario: 保存成功提示
- **GIVEN** 用户点击"保存医案"
- **WHEN** API返回200 OK
- **THEN** 显示成功提示消息
- **AND** 提示内容为"保存成功"

---

## ADDED Requirements

### Requirement: EDITMODE-012 取消医案操作
系统 **SHALL** 提供取消医案功能，允许用户放弃当前未完成的医案。

#### Scenario: 取消按钮显示条件
- **GIVEN** 用户在编辑未完成医案
- **AND** 医案状态为Draft或Active
- **WHEN** 界面渲染
- **THEN** 显示"取消医案"选项（在更多操作菜单中）

#### Scenario: 取消确认对话框
- **GIVEN** 用户点击"取消医案"
- **WHEN** 显示确认对话框
- **THEN** 提示"确定要取消此医案吗？取消后无法恢复。"
- **AND** 显示"确认取消"和"返回"按钮

#### Scenario: 确认取消操作
- **GIVEN** 用户在取消确认对话框
- **WHEN** 用户点击"确认取消"
- **THEN** 系统调用 `PUT /api/v1/medicalcases/{id}/cancel`
- **AND** 取消成功后导航回列表页

#### Scenario: 取消需要审计理由
- **GIVEN** 医案需要审计（非当天本人操作）
- **WHEN** 用户确认取消
- **THEN** 弹出审计理由对话框
- **AND** 必须填写取消原因后才能提交

---

### Requirement: EDITMODE-013 API错误处理
系统 **SHALL** 在API调用失败时提供清晰的错误提示。

#### Scenario: 网络错误处理
- **GIVEN** 用户执行保存操作
- **WHEN** API调用因网络问题失败
- **THEN** 显示错误提示"网络连接失败，请检查网络后重试"
- **AND** 保留用户输入数据

#### Scenario: 权限错误处理
- **GIVEN** 用户尝试操作无权限的医案
- **WHEN** API返回403 Forbidden
- **THEN** 显示错误提示"您没有权限执行此操作"
- **AND** 不修改界面状态

#### Scenario: 业务规则错误处理
- **GIVEN** 用户尝试非法状态转换
- **WHEN** API返回422 UnprocessableEntity
- **THEN** 显示API返回的错误消息
- **AND** 保持当前编辑状态
