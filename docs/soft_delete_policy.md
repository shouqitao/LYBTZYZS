# 各模块功能概览与禁用策略

本文档总结了 LYBT 智能医疗业务管理系统各模块的功能实现情况，并统一阐述「禁用替代删除」的设计原则。所有记录在系统中均应保留，只通过状态字段标记失效或禁用，以便后续审计追溯。

## 模块功能实现对比

| 模块                      | 主要功能              | 删除策略符合性                             |
| ----------------------- | ----------------- | ----------------------------------- |
| 患者（Patients）            | 新增、编辑、查询、导入导出等。   | 部分符合：提供禁用状态，但仍有物理删除接口，应改造为软删除。      |
| 医生（Doctors）             | 医生账户信息维护，状态管理等。   | 符合：通过状态字段控制，无物理删除。                  |
| 账户/用户（Users）            | 新增、更新、搜索，批量启用禁用等。 | 符合：仅通过状态标记禁用，无删除接口。                 |
| 挂号（Registration）        | 创建挂号、编辑、查询等。      | 不符合：存在删除接口，应改为取消挂号并更新状态。            |
| 排队（Queueing）            | 队列维护与状态更新。        | 不符合：提供删除接口，建议改为取消状态。                |
| 诊疗（DiagnosisTreatment）  | 诊疗记录新增、编辑、查询。     | 不符合：不应物理删除，应通过作废标记处理。               |
| 处方（Prescriptions）       | 处方开具与查询。          | 不符合：需用处方状态表示作废，避免删除。                |
| 经验方模板（FormulaTemplates） | 模板新增、编辑、查询。       | 不符合：删除应改为禁用或作废。                     |
| 药房（Pharmacy）            | 抓药任务管理。           | 部分符合：存在删除接口，建议用状态取消。                |
| 费用结算（Billing）           | 账单生成、支付、退款。       | 部分符合：Cancel/Refund 已软删除，但仍有删除接口应移除。 |
| 日志（Logs）                | 记录与查询系统操作。        | 符合：默认保留全部日志，无删除。                    |
| 系统设置（Settings）          | 全局参数维护。           | 符合：多为更新，不涉及删除。                      |
| 同步（Sync）                | 与外部系统同步任务。        | 符合：任务以状态追踪，无删除需求。                   |
| 诊室任务（TreatmentRoom）     | 诊室与队列分配。          | 待确认：应通过状态更新处理取消。                    |

## 待开发功能清单

按照优先级整理需完善或新增的功能，首要任务是统一软删除逻辑，其余依次实施。

1. **全面软删除改造**：将所有存在物理删除的接口改为更新状态字段（Disabled/Cancelled等）。
2. **挂号/队列取消联动**：新增取消挂号接口，并同步更新排队状态。
3. **日志查询与审计界面**：实现日志分页查询、条件搜索等接口，供管理员审计。
4. **医生资料变更审核**：医生提交信息修改需经管理员审批。
5. **患者管理 API**：补充患者相关的 REST 接口并遵循软删除策略。
6. **处方状态流转**：引入 PrescriptionStatus，支持作废处方并与药房联动。
7. **系统同步任务扩展**：完善同步调度与日志监控，支持手动触发。
8. **界面及权限完善**：前端适配禁用记录显示，优化角色权限控制。

## Codex 指令与接口概览

以下列表汇总各模块主要 API/服务方法的调用方式、参数及返回结构，供开发和自动化脚本参考。

### 用户模块（Users）

- `GET /api/Users/search`：分页搜索用户。
- `POST /api/Users/add`：新增用户。
- `PUT /api/Users/update`：编辑用户信息。
- `POST /api/Users/disable/{id}`：禁用用户。
- `POST /api/Users/enable/{id}`：启用用户。
- `POST /api/Users/batchDisable`：批量禁用用户。
- `POST /api/Users/batchEnable`：批量启用用户。
- `POST /api/Users/resetPassword/{id}`：管理员重置密码。
- `POST /api/Users/changePassword`：用户修改密码。
- `POST /api/Users/changeProfile`：修改个人资料。
- `GET /api/Users/getRoles`：获取角色列表。
- `GET /api/Users/getById/{id}`：获取用户详情。

### 患者模块（Patients）

- `GET /api/Patients`：获取患者列表。
- `GET /api/Patients/{id}`：获取患者详情。
- `GET /api/Patients/search`：搜索患者。
- `POST /api/Patients`：新增患者。
- `PUT /api/Patients`：编辑患者。
- `POST /api/Patients/disable/{id}`：禁用患者档案。
- `POST /api/Patients/enable/{id}`：启用患者档案。
- `POST /api/Patients/batchDisable`：批量禁用患者。
- `POST /api/Patients/import`：批量导入患者。
- `GET /api/Patients/export`：导出患者数据。

### 挂号与排队模块

- `GET /api/Registration`：获取挂号列表。
- `GET /api/Registration/{id}`：获取挂号详情。
- `POST /api/Registration`：新增挂号并加入队列。
- `PUT /api/Registration`：编辑挂号信息。
- `POST /api/Registration/cancel/{id}`：取消挂号（软删除）。
- `GET /api/Queueing`：获取排队列表。
- `POST /api/Queueing/cancel/{id}`：取消排队记录。

### 诊疗与处方模块

- `GET /api/DiagnosisTreatment`：获取诊疗列表。
- `GET /api/DiagnosisTreatment/{id}`：诊疗详情。
- `POST /api/DiagnosisTreatment`：新增诊疗记录。
- `PUT /api/DiagnosisTreatment`：编辑诊疗记录。
- `POST /api/DiagnosisTreatment/void/{id}`：作废诊疗记录（规划）。
- `GET /api/Prescriptions`：获取处方列表。
- `GET /api/Prescriptions/{id}`：处方详情。
- `POST /api/Prescriptions`：新增处方。
- `PUT /api/Prescriptions`：编辑处方。
- `POST /api/Prescriptions/void/{id}`：作废处方。

### 药房与费用模块

- `GET /api/Pharmacy/waiting`：待抓药处方列表。
- `POST /api/Pharmacy/{id}/prepared`：标记处方已抓药。
- `GET /api/Billing`：账单列表。
- `GET /api/Billing/{id}`：账单详情。
- `POST /api/Billing`：新增账单。
- `PUT /api/Billing`：编辑账单。
- `POST /api/Billing/mark-paid/{id}`：标记已支付。
- `POST /api/Billing/request-refund/{id}`：申请退款。
- `POST /api/Billing/cancel/{id}`：取消账单（软删除）。

### 日志与系统设置

- `GET /api/Logs`：分页获取日志。
- `GET /api/Logs/{id}`：日志详情。
- `GET /api/Logs/search`：搜索日志。
- `GET /api/Settings`：获取系统设置。
- `PUT /api/Settings`：更新系统设置。

本文件所列接口均需结合身份认证与角色权限使用，实际返回值结构详见各模块 DTO 定义。
