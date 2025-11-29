# medicalcase-edit-modes Specification

## Purpose
TBD - created by archiving change refine-medicalcase-edit-modes. Update Purpose after archive.
## Requirements
### Requirement: EDITMODE-001 工作区模式
系统 **SHALL** 支持MedicalCaseWorkspaceView在两种工作区模式下运行：Clinical(临床看诊)和Management(管理编辑)。

#### Scenario: 临床看诊模式
- **Given** 医生从PatientSelectionView选择患者并开始看诊
- **When** 系统导航到MedicalCaseWorkspaceView
- **Then** 工作区模式设置为Clinical
- **And** 界面标题显示"看诊中 | 患者：XXX"
- **And** 返回按钮显示"返回患者选择"

#### Scenario: 管理编辑模式
- **Given** 用户从MedicalCaseManagementView点击编辑或查看
- **When** 系统导航到MedicalCaseWorkspaceView
- **Then** 工作区模式设置为Management
- **And** 界面标题显示"编辑医案 | 患者：XXX"
- **And** 返回按钮显示"返回医案列表"

---

### Requirement: EDITMODE-002 编辑状态
系统 **SHALL** 支持MedicalCaseWorkspaceView在两种编辑状态下运行：Editing(编辑中)和ReadOnly(只读)。

#### Scenario: 编辑状态界面 - Clinical模式
- **Given** 用户在Clinical模式进入编辑状态
- **When** 界面渲染
- **Then** 所有表单字段可编辑
- **And** 底部操作栏显示: [暂存医案] [打印处方笺] [完成看诊]

#### Scenario: 编辑状态界面 - Management模式
- **Given** 用户在Management模式进入编辑状态
- **When** 界面渲染
- **Then** 所有表单字段可编辑
- **And** 底部操作栏显示: [打印处方笺] [保存医案]
- **And** [保存医案]按钮位于最右侧

#### Scenario: 只读状态界面 - Management模式
- **Given** 用户在Management模式进入只读状态
- **When** 界面渲染
- **Then** 所有表单字段只读
- **And** 右上角显示: [编辑医案]按钮
- **And** 底部操作栏仅显示: [打印处方笺]

#### Scenario: 只读状态界面 - Clinical模式
- **Given** 用户在Clinical模式进入只读状态
- **When** 界面渲染
- **Then** 所有表单字段只读
- **And** 底部操作栏显示: [修改医案](有权限时) [打印处方笺]

---

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

### Requirement: EDITMODE-004 修改医案
系统 **SHALL** 提供"修改医案/编辑医案"功能，切换到编辑状态（需权限检查）。

#### Scenario: Management模式编辑按钮位置
- **Given** 用户在Management模式只读状态
- **And** 用户有编辑权限
- **When** 界面渲染
- **Then** 右上角显示"编辑医案"按钮
- **And** 底部操作栏不显示编辑按钮

#### Scenario: Clinical模式编辑按钮位置
- **Given** 用户在Clinical模式只读状态
- **And** 用户有编辑权限
- **When** 界面渲染
- **Then** 底部操作栏显示"修改医案"按钮

#### Scenario: 点击编辑医案(Management)
- **Given** 用户在Management模式只读状态
- **And** 用户有编辑权限
- **When** 用户点击右上角"编辑医案"按钮
- **Then** 界面切换到编辑状态
- **And** 显示"保存医案"按钮

#### Scenario: 修改按钮Tooltip
- **Given** 用户鼠标悬停在"编辑医案"或"修改医案"按钮上
- **When** 显示Tooltip
- **Then** Tooltip内容为"进入编辑模式，可修改诊断和处方内容"

#### Scenario: 无权限时隐藏编辑按钮
- **Given** 用户查看他人创建的已完成医案
- **And** 用户不是管理员
- **When** 界面加载完成
- **Then** 不显示"编辑医案"或"修改医案"按钮

---

### Requirement: EDITMODE-005 动态返回导航
系统 **SHALL** 根据工作区模式返回到正确的来源页面。

#### Scenario: 临床模式返回
- **Given** 用户在Clinical模式下
- **When** 用户点击返回按钮
- **Then** 系统导航到PatientSelectionView

#### Scenario: 管理模式返回
- **Given** 用户在Management模式下
- **When** 用户点击返回按钮
- **Then** 系统导航到MedicalCaseManagementView

---

### Requirement: EDITMODE-006 管理界面入口
系统 **SHALL** 在医案管理界面提供明确区分的"查看详情"和"编辑"入口。

#### Scenario: 查看详情入口
- **Given** 用户在MedicalCaseManagementView
- **When** 用户点击"查看详情"按钮
- **Then** 系统导航到MedicalCaseWorkspaceView
- **And** 工作区模式为Management
- **And** 编辑状态为ReadOnly（只读）

#### Scenario: 编辑入口
- **Given** 用户在MedicalCaseManagementView
- **And** 用户有编辑权限
- **When** 用户点击"编辑"按钮
- **Then** 系统导航到MedicalCaseWorkspaceView
- **And** 工作区模式为Management
- **And** 编辑状态为Editing（可编辑）

---

### Requirement: EDITMODE-007 完成看诊后返回
系统 **SHALL** 在完成看诊后返回到来源页面。

#### Scenario: 临床模式完成看诊
- **Given** 用户在Clinical模式下完成看诊
- **When** 医案状态设置为Completed
- **Then** 系统自动导航到PatientSelectionView

#### Scenario: 管理模式完成看诊
- **Given** 用户在Management模式下完成看诊
- **When** 医案状态设置为Completed
- **Then** 系统自动导航到MedicalCaseManagementView

---

### Requirement: EDITMODE-008 编辑返回确认
系统 **SHALL** 在Management编辑模式下返回时提示用户处理未保存的修改。

#### Scenario: 只读模式返回
- **Given** 用户在Management模式只读状态
- **When** 用户点击"返回医案列表"
- **Then** 系统直接导航到MedicalCaseManagementView
- **And** 不显示任何确认对话框

#### Scenario: 编辑模式返回确认
- **Given** 用户在Management模式编辑状态
- **When** 用户点击"返回医案列表"
- **Then** 系统显示确认对话框
- **And** 对话框包含三个选项："保存修改"、"放弃修改"、"取消"

#### Scenario: 选择保存修改
- **Given** 用户在返回确认对话框
- **When** 用户点击"保存修改"
- **Then** 系统执行保存操作（含审计检查）
- **And** 保存成功后导航到MedicalCaseManagementView

#### Scenario: 选择放弃修改
- **Given** 用户在返回确认对话框
- **When** 用户点击"放弃修改"
- **Then** 系统不保存任何数据
- **And** 直接导航到MedicalCaseManagementView

#### Scenario: 选择取消
- **Given** 用户在返回确认对话框
- **When** 用户点击"取消"
- **Then** 对话框关闭
- **And** 用户留在当前编辑界面

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

### Requirement: EDITMODE-010 审计判断
系统 **SHALL** 根据医案状态、修改人和时间间隔智能判断是否需要填写修改原因。

#### Scenario: 当天本人修改进行中医案
- **Given** 当前用户是医案创建医生
- **And** 医案创建日期是今天
- **And** 医案状态为Draft或Active
- **When** 用户保存医案
- **Then** 不要求填写修改原因

#### Scenario: 修改已完成医案
- **Given** 医案状态为Completed
- **When** 任何用户保存修改
- **Then** 必须填写修改原因

#### Scenario: 隔天修改医案
- **Given** 医案创建日期早于今天
- **When** 任何用户保存修改
- **Then** 必须填写修改原因

#### Scenario: 非本人修改医案
- **Given** 当前用户不是医案创建医生
- **When** 用户保存修改
- **Then** 必须填写修改原因

---

### Requirement: EDITMODE-011 审计理由对话框
系统 **SHALL** 提供审计理由对话框，在需要审计时强制用户填写修改原因。

#### Scenario: 显示审计对话框
- **Given** 用户点击保存且需要审计
- **When** 系统检测到需要填写修改原因
- **Then** 弹出审计理由对话框
- **And** 对话框包含多行文本输入框
- **And** 对话框包含常用原因选项

#### Scenario: 常用原因选项
- **Given** 审计理由对话框显示
- **When** 用户查看常用原因
- **Then** 显示以下选项：
  - 补充遗漏信息
  - 更正录入错误
  - 患者要求修改
  - 医嘱调整

#### Scenario: 选择常用原因
- **Given** 用户在审计理由对话框
- **When** 用户选择一个常用原因
- **Then** 该原因自动填充到文本输入框

#### Scenario: 确认按钮状态
- **Given** 审计理由对话框显示
- **When** 文本输入框为空
- **Then** "确认保存"按钮禁用
- **When** 文本输入框有内容
- **Then** "确认保存"按钮启用

#### Scenario: 提交审计理由
- **Given** 用户填写了修改原因
- **When** 用户点击"确认保存"
- **Then** 系统保存医案数据
- **And** 系统记录审计日志（含修改原因）
- **And** 对话框关闭

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

