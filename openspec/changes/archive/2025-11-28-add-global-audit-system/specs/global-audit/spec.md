## ADDED Requirements

### Requirement: 统一审计日志基础架构
系统 **SHALL** 提供统一的审计日志基础架构，支持对任意业务实体的变更进行追踪记录。

#### Scenario: 创建操作审计
- **GIVEN** 用户对任意业务实体执行创建操作
- **WHEN** 实体保存成功
- **THEN** 系统记录审计日志
- **AND** 日志包含: EntityType, EntityId, OperationType(Create), OperatorId, OperatorName, OperatorRole, CreatedAt
- **AND** 日志包含实体创建时的完整数据(NewValues JSON)

#### Scenario: 更新操作审计
- **GIVEN** 用户对任意业务实体执行更新操作
- **WHEN** 实体保存成功
- **THEN** 系统记录审计日志
- **AND** 日志包含: EntityType, EntityId, OperationType(Update), OperatorId, OperatorName, OperatorRole, CreatedAt
- **AND** 日志包含变更的字段列表(ChangedFields JSON)
- **AND** 日志包含变更前的值(OldValues JSON)
- **AND** 日志包含变更后的值(NewValues JSON)

#### Scenario: 删除操作审计
- **GIVEN** 用户对任意业务实体执行删除操作
- **WHEN** 实体删除成功(软删除或硬删除)
- **THEN** 系统记录审计日志
- **AND** 日志包含: EntityType, EntityId, OperationType(SoftDelete或Delete), OperatorId, OperatorName, OperatorRole, CreatedAt
- **AND** 日志包含删除前的实体数据(OldValues JSON)

---

### Requirement: 患者(Patient)审计
系统 **SHALL** 记录所有患者信息的创建、修改、删除操作。

#### Scenario: 患者信息变更审计
- **GIVEN** 管理员修改患者基本信息(姓名、性别、年龄、联系方式等)
- **WHEN** 修改保存成功
- **THEN** 系统记录审计日志
- **AND** 日志EntityType为"Patient"
- **AND** 日志包含修改的字段和前后值

#### Scenario: 患者审计日志查看
- **GIVEN** 管理员在患者管理界面选中一个患者
- **WHEN** 管理员点击"变更记录"按钮
- **THEN** 系统显示该患者的所有修改历史
- **AND** 历史按时间倒序排列

---

### Requirement: 处方(Prescription)审计
系统 **SHALL** 记录所有处方的创建、修改、状态变更操作。

#### Scenario: 处方变更审计
- **GIVEN** 医生创建或修改处方
- **WHEN** 处方保存成功
- **THEN** 系统记录审计日志
- **AND** 日志EntityType为"Prescription"
- **AND** 日志包含处方内容变更详情

#### Scenario: 处方审计日志查看
- **GIVEN** 用户在处方管理界面选中一个处方
- **WHEN** 用户点击"变更记录"按钮
- **THEN** 系统显示该处方的所有修改历史

---

### Requirement: 药材(Herb)审计
系统 **SHALL** 记录所有药材信息的创建、修改、删除操作。

#### Scenario: 药材变更审计
- **GIVEN** 管理员修改药材信息(名称、价格、库存等)
- **WHEN** 修改保存成功
- **THEN** 系统记录审计日志
- **AND** 日志EntityType为"Herb"

#### Scenario: 药材审计日志查看
- **GIVEN** 管理员在药材管理界面选中一个药材
- **WHEN** 管理员点击"变更记录"按钮
- **THEN** 系统显示该药材的所有修改历史

---

### Requirement: 方剂(Formula)审计
系统 **SHALL** 记录所有方剂的创建、修改、删除操作。

#### Scenario: 方剂变更审计
- **GIVEN** 医生创建或修改方剂
- **WHEN** 方剂保存成功
- **THEN** 系统记录审计日志
- **AND** 日志EntityType为"Formula"

#### Scenario: 方剂审计日志查看
- **GIVEN** 用户在方剂管理界面选中一个方剂
- **WHEN** 用户点击"变更记录"按钮
- **THEN** 系统显示该方剂的所有修改历史

---

### Requirement: 用户(User)审计
系统 **SHALL** 记录所有用户账号的创建、修改、状态变更操作。

#### Scenario: 用户变更审计
- **GIVEN** 管理员修改用户信息(角色、状态、权限等)
- **WHEN** 修改保存成功
- **THEN** 系统记录审计日志
- **AND** 日志EntityType为"User"
- **AND** 日志不包含敏感信息(如密码哈希)

#### Scenario: 用户审计日志查看
- **GIVEN** 管理员在用户管理界面选中一个用户
- **WHEN** 管理员点击"变更记录"按钮
- **THEN** 系统显示该用户的所有修改历史

---

### Requirement: 诊断记录(Consultation)审计
系统 **SHALL** 记录所有诊断记录的创建、修改操作。

#### Scenario: 诊断记录变更审计
- **GIVEN** 医生创建或修改诊断记录
- **WHEN** 诊断记录保存成功
- **THEN** 系统记录审计日志
- **AND** 日志EntityType为"Consultation"

---

### Requirement: 审计日志API
系统 **SHALL** 为每个支持审计的实体提供审计日志查询API。

#### Scenario: 获取审计日志(分页)
- **GIVEN** 客户端请求实体的审计日志
- **WHEN** 调用 GET /api/{entities}/{id}/audit-logs?page=1&pageSize=20
- **THEN** 返回该实体的审计日志列表
- **AND** 按CreatedAt倒序排列
- **AND** 返回总记录数和分页信息

#### Scenario: 审计日志权限控制
- **GIVEN** 普通用户(非管理员)请求审计日志
- **WHEN** 调用审计日志API
- **THEN** 系统根据用户角色决定是否允许访问
- **AND** Admin和SuperAdmin可访问所有审计日志
- **AND** Doctor只能访问自己操作的审计日志

---

### Requirement: 前端统一审计日志对话框
系统 **SHALL** 提供统一的审计日志查看对话框，支持所有实体类型。

#### Scenario: 打开审计日志对话框
- **GIVEN** 用户在任意管理界面点击"变更记录"按钮
- **WHEN** 对话框打开
- **THEN** 显示该实体的审计日志列表
- **AND** 显示实体标识信息(如患者姓名、处方编号等)
- **AND** 支持分页浏览

#### Scenario: 审计日志详情展示
- **GIVEN** 审计日志对话框显示日志列表
- **WHEN** 用户查看某条日志
- **THEN** 显示操作时间、操作人、操作类型
- **AND** 显示变更字段列表
- **AND** 显示变更前后的值对比
- **AND** 显示修改原因(如有)
