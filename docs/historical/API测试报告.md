# LYBT中医诊所管理系统 - API测试报告

## 测试概览
- **测试时间**: 2025-08-01 22:34:00
- **服务器地址**: http://192.168.190.243:5000
- **总接口数**: 41
- **成功数量**: 14
- **失败数量**: 27
- **成功率**: 34.1%

## 详细测试结果

| 模块 | 接口路径 | 方法 | 状态码 | 结果 | 描述 |
|------|----------|------|---------|------|------|
| 健康检查 | /api/Health | GET | 200 | ✅ 成功 | 基本健康检查 |
| 健康检查 | /api/Health/database | GET | 200 | ✅ 成功 | 数据库健康检查 |
| 健康检查 | /api/Health/detailed | GET | 200 | ✅ 成功 | 详细系统状态 |
| 认证 | /api/v1/Auth/login | POST | 200 | ✅ 成功 | 用户登录测试 |
| 认证 | /api/v1/Auth/logout | POST | 200 | ✅ 成功 | 用户登出 |
| 认证 | /api/v1/Auth/RefreshToken | POST | 200 | ✅ 成功 | 刷新token |
| 用户 | /api/v1/Users | GET | 500 | ❌ 失败 | 获取用户列表 |
| 用户 | /api/v1/Users/paged | POST | 500 | ❌ 失败 | 分页查询用户 |
| 用户 | /api/v1/Users/getRoles | GET | 200 | ✅ 成功 | 获取所有角色 |
| 用户 | /api/v1/Users/active | GET | 500 | ❌ 失败 | 获取启用用户列表 |
| 患者 | /api/v1/Patients | GET | 500 | ❌ 失败 | 获取患者列表 |
| 患者 | /api/v1/Patients/paged | POST | 500 | ❌ 失败 | 分页查询患者 |
| 患者 | /api/v1/Patients/active | GET | 500 | ❌ 失败 | 获取启用患者列表 |
| 医生 | /api/v1/Doctors | GET | 405 | ❌ 失败 | 获取医生列表 |
| 医生 | /api/v1/Doctors/paged | POST | 400 | ❌ 失败 | 分页查询医生 |
| 医生 | /api/v1/Doctors/active | GET | 405 | ❌ 失败 | 获取启用医生列表 |
| 药材 | /api/v1/Herbs | GET | 500 | ❌ 失败 | 获取药材列表 |
| 药材 | /api/v1/Herbs/paged | POST | 500 | ❌ 失败 | 分页查询药材 |
| 药材 | /api/v1/Herbs/active | GET | 500 | ❌ 失败 | 获取启用药材列表 |
| 挂号 | /api/v1/Registration | GET | 200 | ✅ 成功 | 获取挂号列表 |
| 挂号 | /api/v1/Registration/paged | POST | 500 | ❌ 失败 | 分页查询挂号 |
| 处方 | /api/v1/Prescriptions | GET | 200 | ✅ 成功 | 获取处方列表 |
| 处方 | /api/v1/Prescriptions/paged | POST | 500 | ❌ 失败 | 分页查询处方 |
| 诊断治疗 | /api/v1/DiagnosisTreatment | GET | 200 | ✅ 成功 | 获取诊断治疗列表 |
| 诊断治疗 | /api/v1/DiagnosisTreatment/paged | POST | 500 | ❌ 失败 | 分页查询诊断治疗 |
| 药房 | /api/v1/Pharmacy | GET | 200 | ✅ 成功 | 获取药房列表 |
| 药房 | /api/v1/Pharmacy/paged | POST | 500 | ❌ 失败 | 分页查询药房 |
| 费用结算 | /api/v1/Billing | GET | 200 | ✅ 成功 | 获取费用结算列表 |
| 费用结算 | /api/v1/Billing/paged | POST | 500 | ❌ 失败 | 分页查询费用结算 |
| 排队 | /api/v1/Queueing | GET | 200 | ✅ 成功 | 获取排队列表 |
| 排队 | /api/v1/Queueing/paged | POST | 500 | ❌ 失败 | 分页查询排队 |
| 病历 | /api/v1/Records | GET | 404 | ❌ 失败 | 获取病历列表 |
| 病历 | /api/v1/Records/paged | POST | 404 | ❌ 失败 | 分页查询病历 |
| 方剂模板 | /api/v1/FormulaTemplates | GET | 404 | ❌ 失败 | 获取方剂模板列表 |
| 方剂模板 | /api/v1/FormulaTemplates/paged | POST | 404 | ❌ 失败 | 分页查询方剂模板 |
| 治疗室 | /api/v1/TreatmentRoom | GET | 200 | ✅ 成功 | 获取治疗室列表 |
| 治疗室 | /api/v1/TreatmentRoom/paged | POST | 500 | ❌ 失败 | 分页查询治疗室 |
| 统一配置 | /api/v1/UnifiedConfig | GET | 404 | ❌ 失败 | 获取统一配置 |
| 统一日志 | /api/v1/UnifiedLogs | GET | 404 | ❌ 失败 | 获取统一日志 |
| 统一日志 | /api/v1/UnifiedLogs/paged | POST | 404 | ❌ 失败 | 分页查询统一日志 |
| 数据同步 | /api/v1/Sync | GET | 404 | ❌ 失败 | 获取数据同步状态 |

## 按模块统计

- **健康检查**: 3/3 (100.0% 成功)
- **认证**: 3/3 (100.0% 成功)
- **用户**: 1/4 (25.0% 成功)
- **患者**: 0/3 (0.0% 成功)
- **医生**: 0/3 (0.0% 成功)
- **药材**: 0/3 (0.0% 成功)
- **挂号**: 1/2 (50.0% 成功)
- **处方**: 1/2 (50.0% 成功)
- **诊断治疗**: 1/2 (50.0% 成功)
- **药房**: 1/2 (50.0% 成功)
- **费用结算**: 1/2 (50.0% 成功)
- **排队**: 1/2 (50.0% 成功)
- **病历**: 0/2 (0.0% 成功)
- **方剂模板**: 0/2 (0.0% 成功)
- **治疗室**: 1/2 (50.0% 成功)
- **统一配置**: 0/1 (0.0% 成功)
- **统一日志**: 0/2 (0.0% 成功)
- **数据同步**: 0/1 (0.0% 成功)

## 主要问题分析

### 1. 服务器内部错误 (500) - 高优先级
以下接口出现500错误，需要立即修复：
- 用户模块：/api/v1/Users, /api/v1/Users/paged, /api/v1/Users/active
- 患者模块：所有接口 (3个)
- 药材模块：所有接口 (3个)
- 所有分页查询接口 (8个)

### 2. 接口不存在 (404) - 中优先级
以下接口路径不存在，需要确认实现：
- 病历模块：/api/v1/Records (2个接口)
- 方剂模板模块：/api/v1/FormulaTemplates (2个接口)
- 统一配置模块：/api/v1/UnifiedConfig
- 统一日志模块：/api/v1/UnifiedLogs (2个接口)
- 数据同步模块：/api/v1/Sync

### 3. HTTP方法错误 (405) - 低优先级
医生模块的GET请求不被允许，需要检查路由配置。

### 4. 参数验证错误 (400) - 低优先级
医生模块的分页查询参数有问题。

---
*报告生成时间: 2025-08-01 22:34:00*

## 测试结果汇总

| 模块名 | 接口路径 | 方法 | 测试结果 | 备注 |
|--------|----------|------|----------|------|
| Auth | /Auth/Login | POST | 成功 |  |
| Auth | /Auth/Logout | POST | 成功 |  |
| Auth | /Auth/RefreshToken | POST | 失败 | 状态码: 404 |
| Auth | /Auth/ChangePassword | POST | 失败 | 状态码: 404 |
| Users | /Users | GET | 失败 | 状态码: 404 |
| Users | /Users/{id} | GET | 失败 | 状态码: 404 |
| Users | /Users | POST | 失败 | 状态码: 404 |
| Users | /Users/{id} | PUT | 失败 | 状态码: 404 |
| Users | /Users/{id} | DELETE | 失败 | 状态码: 404 |
| Patients | /Patients | GET | 成功 |  |
| Patients | /Patients/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| Patients | /Patients | POST | 失败 | {'dto': ['The dto field is required.'], '$.gender': ['The JSON value could not be converted to LYBT.Shared.Models.Enums.Gender. Path: $.gender | LineNumber: 0 | BytePositionInLine: 55.']} |
| Patients | /Patients/{id} | PUT | 失败 | {'id': ["The value '1' is not valid."], 'dto': ['The dto field is required.'], '$.id': ['The JSON value could not be converted to System.Guid. Path: $.id | LineNumber: 0 | BytePositionInLine: 8.']} |
| Patients | /Patients/{id} | DELETE | 失败 | 状态码: 405 |
| Doctors | /Doctors | GET | 失败 | 状态码: 405 |
| Doctors | /Doctors/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| Doctors | /Doctors | POST | 失败 | {'dto': ['The dto field is required.'], '$.gender': ['The JSON value could not be converted to LYBT.Shared.Models.Enums.Gender. Path: $.gender | LineNumber: 0 | BytePositionInLine: 55.']} |
| Doctors | /Doctors/{id} | PUT | 失败 | 状态码: 405 |
| Doctors | /Doctors/{id} | DELETE | 失败 | 状态码: 405 |
| Registration | /Registration | GET | 成功 |  |
| Registration | /Registration/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| Registration | /Registration | POST | 失败 | {'dto': ['The dto field is required.'], '$.patientId': ['The JSON value could not be converted to System.String. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']} |
| Registration | /Registration/{id} | PUT | 失败 | 状态码: 405 |
| Registration | /Registration/{id} | DELETE | 失败 | {'id': ["The value '999' is not valid."]} |
| Queueing | /Queueing | GET | 成功 |  |
| Queueing | /Queueing/Current | GET | 失败 | {'id': ["The value 'Current' is not valid."]} |
| Queueing | /Queueing/Next | POST | 失败 | 状态码: 405 |
| Queueing | /Queueing/Call/{id} | POST | 失败 | 状态码: 404 |
| Queueing | /Queueing/Skip/{id} | POST | 失败 | 状态码: 404 |
| DiagnosisTreatment | /DiagnosisTreatment | GET | 成功 |  |
| DiagnosisTreatment | /DiagnosisTreatment/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| DiagnosisTreatment | /DiagnosisTreatment | POST | 失败 | {'dto': ['The dto field is required.'], '$.patientId': ['The JSON value could not be converted to System.Guid. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']} |
| DiagnosisTreatment | /DiagnosisTreatment/{id} | PUT | 失败 | 状态码: 405 |
| DiagnosisTreatment | /DiagnosisTreatment/{id} | DELETE | 失败 | {'id': ["The value '999' is not valid."]} |
| Prescriptions | /Prescriptions | GET | 成功 |  |
| Prescriptions | /Prescriptions/{id} | GET | 失败 | 状态码: 404 |
| Prescriptions | /Prescriptions | POST | 失败 | {'dto': ['The dto field is required.'], '$.patientId': ['The JSON value could not be converted to System.Guid. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']} |
| Prescriptions | /Prescriptions/{id} | PUT | 失败 | 状态码: 405 |
| Prescriptions | /Prescriptions/{id} | DELETE | 失败 | 状态码: 404 |
| Herbs | /Herbs | GET | 成功 |  |
| Herbs | /Herbs/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| Herbs | /Herbs | POST | 成功 |  |
| Herbs | /Herbs/{id} | PUT | 失败 | 状态码: 405 |
| Herbs | /Herbs/{id} | DELETE | 失败 | {'id': ["The value '999' is not valid."]} |
| FormulaTemplates | /FormulaTemplates | GET | 失败 | 状态码: 404 |
| FormulaTemplates | /FormulaTemplates/{id} | GET | 失败 | 状态码: 404 |
| FormulaTemplates | /FormulaTemplates | POST | 失败 | 状态码: 404 |
| FormulaTemplates | /FormulaTemplates/{id} | PUT | 失败 | 状态码: 404 |
| FormulaTemplates | /FormulaTemplates/{id} | DELETE | 失败 | 状态码: 404 |
| Pharmacy | /Pharmacy/Dispensing | GET | 失败 | {'id': ["The value 'Dispensing' is not valid."]} |
| Pharmacy | /Pharmacy/Dispensing/{id} | GET | 失败 | 状态码: 404 |
| Pharmacy | /Pharmacy/Dispense | POST | 失败 | 状态码: 405 |
| Pharmacy | /Pharmacy/Return | POST | 失败 | 状态码: 405 |
| Billing | /Billing | GET | 成功 |  |
| Billing | /Billing/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| Billing | /Billing | POST | 失败 | {'billingCreateDto': ['The billingCreateDto field is required.'], '$.patientId': ['The JSON value could not be converted to System.Guid. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']} |
| Billing | /Billing/Pay/{id} | POST | 失败 | 状态码: 404 |
| Billing | /Billing/Refund/{id} | POST | 失败 | 状态码: 404 |
| Records | /Records | GET | 失败 | 状态码: 404 |
| Records | /Records/{id} | GET | 失败 | 状态码: 404 |
| Records | /Records/Patient/{patientId} | GET | 失败 | 状态码: 404 |
| Records | /Records | POST | 失败 | 状态码: 404 |
| Records | /Records/{id} | DELETE | 失败 | 状态码: 404 |
| TreatmentRoom | /TreatmentRoom | GET | 成功 |  |
| TreatmentRoom | /TreatmentRoom/{id} | GET | 失败 | {'id': ["The value '1' is not valid."]} |
| TreatmentRoom | /TreatmentRoom | POST | 失败 | {'treatmentRoomCreateDto': ['The treatmentRoomCreateDto field is required.'], '$.status': ['The JSON value could not be converted to System.Int32. Path: $.status | LineNumber: 0 | BytePositionInLine: 107.']} |
| TreatmentRoom | /TreatmentRoom/{id} | PUT | 失败 | 状态码: 405 |
| TreatmentRoom | /TreatmentRoom/{id} | DELETE | 失败 | {'id': ["The value '999' is not valid."]} |
| Sync | /Sync/Status | GET | 失败 | 状态码: 404 |
| Sync | /Sync/Start | POST | 失败 | 状态码: 404 |
| Sync | /Sync/History | GET | 失败 | 状态码: 404 |
| UnifiedConfig | /UnifiedConfig | GET | 失败 | 状态码: 404 |
| UnifiedConfig | /UnifiedConfig/{key} | GET | 失败 | 状态码: 404 |
| UnifiedConfig | /UnifiedConfig | POST | 失败 | 状态码: 404 |
| UnifiedConfig | /UnifiedConfig/{key} | PUT | 失败 | 状态码: 404 |
| UnifiedConfig | /UnifiedConfig/{key} | DELETE | 失败 | 状态码: 404 |
| UnifiedLogs | /UnifiedLogs | GET | 失败 | 状态码: 404 |
| UnifiedLogs | /UnifiedLogs/{id} | GET | 失败 | 状态码: 404 |
| UnifiedLogs | /UnifiedLogs/Export | GET | 失败 | 状态码: 404 |
| Health | /Health | GET | 失败 | 状态码: 404 |
| Health | /Health/Database | GET | 失败 | 状态码: 404 |

## 统计信息

- 总接口数: 81
- 成功数: 11
- 失败数: 70
- 成功率: 13.58%

## 失败接口汇总

- **Auth** - POST /Auth/RefreshToken: 状态码: 404
- **Auth** - POST /Auth/ChangePassword: 状态码: 404
- **Users** - GET /Users: 状态码: 404
- **Users** - GET /Users/{id}: 状态码: 404
- **Users** - POST /Users: 状态码: 404
- **Users** - PUT /Users/{id}: 状态码: 404
- **Users** - DELETE /Users/{id}: 状态码: 404
- **Patients** - GET /Patients/{id}: {'id': ["The value '1' is not valid."]}
- **Patients** - POST /Patients: {'dto': ['The dto field is required.'], '$.gender': ['The JSON value could not be converted to LYBT.Shared.Models.Enums.Gender. Path: $.gender | LineNumber: 0 | BytePositionInLine: 55.']}
- **Patients** - PUT /Patients/{id}: {'id': ["The value '1' is not valid."], 'dto': ['The dto field is required.'], '$.id': ['The JSON value could not be converted to System.Guid. Path: $.id | LineNumber: 0 | BytePositionInLine: 8.']}
- **Patients** - DELETE /Patients/{id}: 状态码: 405
- **Doctors** - GET /Doctors: 状态码: 405
- **Doctors** - GET /Doctors/{id}: {'id': ["The value '1' is not valid."]}
- **Doctors** - POST /Doctors: {'dto': ['The dto field is required.'], '$.gender': ['The JSON value could not be converted to LYBT.Shared.Models.Enums.Gender. Path: $.gender | LineNumber: 0 | BytePositionInLine: 55.']}
- **Doctors** - PUT /Doctors/{id}: 状态码: 405
- **Doctors** - DELETE /Doctors/{id}: 状态码: 405
- **Registration** - GET /Registration/{id}: {'id': ["The value '1' is not valid."]}
- **Registration** - POST /Registration: {'dto': ['The dto field is required.'], '$.patientId': ['The JSON value could not be converted to System.String. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']}
- **Registration** - PUT /Registration/{id}: 状态码: 405
- **Registration** - DELETE /Registration/{id}: {'id': ["The value '999' is not valid."]}
- **Queueing** - GET /Queueing/Current: {'id': ["The value 'Current' is not valid."]}
- **Queueing** - POST /Queueing/Next: 状态码: 405
- **Queueing** - POST /Queueing/Call/{id}: 状态码: 404
- **Queueing** - POST /Queueing/Skip/{id}: 状态码: 404
- **DiagnosisTreatment** - GET /DiagnosisTreatment/{id}: {'id': ["The value '1' is not valid."]}
- **DiagnosisTreatment** - POST /DiagnosisTreatment: {'dto': ['The dto field is required.'], '$.patientId': ['The JSON value could not be converted to System.Guid. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']}
- **DiagnosisTreatment** - PUT /DiagnosisTreatment/{id}: 状态码: 405
- **DiagnosisTreatment** - DELETE /DiagnosisTreatment/{id}: {'id': ["The value '999' is not valid."]}
- **Prescriptions** - GET /Prescriptions/{id}: 状态码: 404
- **Prescriptions** - POST /Prescriptions: {'dto': ['The dto field is required.'], '$.patientId': ['The JSON value could not be converted to System.Guid. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']}
- **Prescriptions** - PUT /Prescriptions/{id}: 状态码: 405
- **Prescriptions** - DELETE /Prescriptions/{id}: 状态码: 404
- **Herbs** - GET /Herbs/{id}: {'id': ["The value '1' is not valid."]}
- **Herbs** - PUT /Herbs/{id}: 状态码: 405
- **Herbs** - DELETE /Herbs/{id}: {'id': ["The value '999' is not valid."]}
- **FormulaTemplates** - GET /FormulaTemplates: 状态码: 404
- **FormulaTemplates** - GET /FormulaTemplates/{id}: 状态码: 404
- **FormulaTemplates** - POST /FormulaTemplates: 状态码: 404
- **FormulaTemplates** - PUT /FormulaTemplates/{id}: 状态码: 404
- **FormulaTemplates** - DELETE /FormulaTemplates/{id}: 状态码: 404
- **Pharmacy** - GET /Pharmacy/Dispensing: {'id': ["The value 'Dispensing' is not valid."]}
- **Pharmacy** - GET /Pharmacy/Dispensing/{id}: 状态码: 404
- **Pharmacy** - POST /Pharmacy/Dispense: 状态码: 405
- **Pharmacy** - POST /Pharmacy/Return: 状态码: 405
- **Billing** - GET /Billing/{id}: {'id': ["The value '1' is not valid."]}
- **Billing** - POST /Billing: {'billingCreateDto': ['The billingCreateDto field is required.'], '$.patientId': ['The JSON value could not be converted to System.Guid. Path: $.patientId | LineNumber: 0 | BytePositionInLine: 15.']}
- **Billing** - POST /Billing/Pay/{id}: 状态码: 404
- **Billing** - POST /Billing/Refund/{id}: 状态码: 404
- **Records** - GET /Records: 状态码: 404
- **Records** - GET /Records/{id}: 状态码: 404
- **Records** - GET /Records/Patient/{patientId}: 状态码: 404
- **Records** - POST /Records: 状态码: 404
- **Records** - DELETE /Records/{id}: 状态码: 404
- **TreatmentRoom** - GET /TreatmentRoom/{id}: {'id': ["The value '1' is not valid."]}
- **TreatmentRoom** - POST /TreatmentRoom: {'treatmentRoomCreateDto': ['The treatmentRoomCreateDto field is required.'], '$.status': ['The JSON value could not be converted to System.Int32. Path: $.status | LineNumber: 0 | BytePositionInLine: 107.']}
- **TreatmentRoom** - PUT /TreatmentRoom/{id}: 状态码: 405
- **TreatmentRoom** - DELETE /TreatmentRoom/{id}: {'id': ["The value '999' is not valid."]}
- **Sync** - GET /Sync/Status: 状态码: 404
- **Sync** - POST /Sync/Start: 状态码: 404
- **Sync** - GET /Sync/History: 状态码: 404
- **UnifiedConfig** - GET /UnifiedConfig: 状态码: 404
- **UnifiedConfig** - GET /UnifiedConfig/{key}: 状态码: 404
- **UnifiedConfig** - POST /UnifiedConfig: 状态码: 404
- **UnifiedConfig** - PUT /UnifiedConfig/{key}: 状态码: 404
- **UnifiedConfig** - DELETE /UnifiedConfig/{key}: 状态码: 404
- **UnifiedLogs** - GET /UnifiedLogs: 状态码: 404
- **UnifiedLogs** - GET /UnifiedLogs/{id}: 状态码: 404
- **UnifiedLogs** - GET /UnifiedLogs/Export: 状态码: 404
- **Health** - GET /Health: 状态码: 404
- **Health** - GET /Health/Database: 状态码: 404
