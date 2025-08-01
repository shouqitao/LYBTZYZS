# LYBTZYZS 中医诊所管理系统 WebAPI 接口文档

## 文档信息

- **项目名称**: LYBTZYZS：凌隐宝堂中医诊所
- **版本**: v1.0
- **生成日期**: 2025-08-01
- **文档类型**: WebAPI接口完整清单

## 项目概述

LYBTZYZS是一套完整的中医诊所管理系统，采用前后端分离架构：

- **后端**: .NET 8.0 WebAPI，提供RESTful接口服务
- **前端**: WPF桌面应用，基于Prism框架
- **数据库**: 支持SQL Server、LocalDB、SQLite
- **认证**: JWT Bearer Token认证机制

## API架构特点

### 🔧 技术规范

- **API版本控制**: 统一使用 v1.0 版本
- **路由格式**: `api/v{version:apiVersion}/[controller]`
- **认证方式**: JWT Bearer Token (除健康检查接口外)
- **响应格式**: 统一使用 `ApiResponse<T>` 封装
- **软删除策略**: 关键实体采用禁用/启用而非直接删除

### 🏗️ 系统特色

- **权限分级控制**: 基于用户角色的访问控制
- **完整缓存机制**: 查询接口普遍支持内存缓存
- **批量操作支持**: 提供批量禁用/启用等操作
- **导入导出功能**: 支持数据的批量导入导出
- **健康检查**: 完整的系统状态监控

---

## API控制器详细列表

### 1. AuthController - 认证控制器

**基础路由**: `api/v1.0/Auth`

| HTTP方法 | 路由                        | 方法名                    | 描述           | 参数类型                      | 返回类型                       |
| ------ | ------------------------- | ---------------------- | ------------ | ------------------------- | -------------------------- |
| POST   | `/login`                  | Login                  | 用户登录（简化版本）   | LoginRequestDto           | ApiResponse<LoginResponse> |
| POST   | `/logout`                 | Logout                 | 用户登出         | LogoutRequestDto          | ApiResponse<object>        |
| POST   | `/changeSysAdminPassword` | ChangeSysAdminPassword | 修改sysadmin密码 | ChangeSysAdminPasswordDto | ApiResponse<object>        |
| POST   | `/testLogin`              | TestLogin              | 健康检查登录（调试用）  | LoginRequestDto           | ApiResponse<object>        |
| POST   | `/mockLogin`              | MockLogin              | 模拟登录（前端测试用）  | LoginRequestDto           | ApiResponse<LoginResponse> |

**特点说明**:

- `/login`、`/testLogin`、`/mockLogin` 允许匿名访问
- 支持sysadmin特殊用户登录处理
- 提供模拟登录接口用于开发测试

---

### 2. DoctorsController - 医生管理控制器

**基础路由**: `api/v1.0/Doctors`

| HTTP方法 | 路由                          | 方法名           | 描述            | 参数类型              | 返回类型                                   |
| ------ | --------------------------- | ------------- | ------------- | ----------------- | -------------------------------------- |
| POST   | `/paged`                    | GetPaged      | 分页查询医生列表      | DoctorQueryDto    | ApiResponse<PagedResultDto<DoctorDto>> |
| GET    | `/search`                   | Search        | 搜索医生          | string keyword    | ApiResponse<List<DoctorDto>>           |
| GET    | `/active`                   | GetActiveList | 获取在职医生列表      | -                 | ApiResponse<List<DoctorDto>>           |
| GET    | `/{id}`                     | GetById       | 根据ID获取医生详情    | Guid id           | ApiResponse<DoctorDetailDto>           |
| GET    | `/by-user/{userId}`         | GetByUserId   | 根据用户ID获取医生详情  | Guid userId       | ApiResponse<DoctorDetailDto>           |
| POST   | `/`                         | Add           | 新增医生          | DoctorDetailDto   | ApiResponse<bool>                      |
| PUT    | `/`                         | Update        | 更新医生信息        | DoctorDetailDto   | ApiResponse<bool>                      |
| PATCH  | `/{id}/disable`             | Disable       | 禁用医生（软删除）     | Guid id           | ApiResponse<bool>                      |
| PATCH  | `/{id}/enable`              | Enable        | 启用医生          | Guid id           | ApiResponse<bool>                      |
| PATCH  | `/batch-disable`            | BatchDisable  | 批量禁用医生        | DoctorBatchIdsDto | ApiResponse<int>                       |
| PATCH  | `/batch-enable`             | BatchEnable   | 批量启用医生        | DoctorBatchIdsDto | ApiResponse<int>                       |
| GET    | `/check-user-link/{userId}` | CheckUserLink | 检查用户是否已关联医生档案 | Guid userId       | ApiResponse<bool>                      |
| GET    | `/roles`                    | GetRoles      | 获取用户角色枚举列表    | -                 | ApiResponse<object>                    |

**特点说明**:

- 实现软删除策略，不提供真实的删除接口
- 具有完整的缓存机制
- 支持权限控制（禁用的医生仅管理员可查询）

---

### 3. HerbsController - 药材管理控制器

**基础路由**: `api/v1.0/Herbs`

| HTTP方法 | 路由                 | 方法名               | 描述         | 参数类型                     | 返回类型                                      |
| ------ | ------------------ | ----------------- | ---------- | ------------------------ | ----------------------------------------- |
| GET    | `/`                | GetList           | 获取药材列表     | -                        | ApiResponse<List<HerbDto>>                |
| POST   | `/paged`           | GetPaged          | 分页查询药材     | HerbPagedQueryDto        | ApiResponse<PagedResultDto<HerbDto>>      |
| GET    | `/{id}`            | GetById           | 获取药材详情     | Guid id                  | ApiResponse<HerbDetailDto>                |
| POST   | `/`                | Add               | 新增药材       | HerbCreateDto            | ApiResponse<object>                       |
| PUT    | `/`                | Update            | 编辑药材       | HerbEditDto              | ApiResponse<object>                       |
| DELETE | `/{id}`            | Delete            | 删除药材       | Guid id                  | ApiResponse<object>                       |
| POST   | `/import`          | Import            | 批量导入药材     | List<HerbImportDto>      | ActionResult                              |
| POST   | `/export`          | Export            | 导出药材数据     | -                        | ActionResult<List<HerbDetailDto>>         |
| PATCH  | `/status`          | UpdateStatus      | 更新药材状态     | HerbStatusUpdateDto      | ActionResult                              |
| PATCH  | `/batch-status`    | BatchUpdateStatus | 批量更新药材状态   | HerbBatchStatusUpdateDto | ActionResult                              |
| GET    | `/status/{status}` | GetByStatus       | 根据状态获取药材列表 | HerbStatus status        | ActionResult<List<HerbDto>>               |
| GET    | `/available`       | GetAvailable      | 获取可用药材列表   | -                        | ApiResponse<List<HerbDto>>                |
| GET    | `/out-of-stock`    | GetOutOfStock     | 获取缺货药材列表   | -                        | ActionResult<List<HerbDto>>               |
| GET    | `/expiring`        | GetExpiring       | 获取即将过期药材列表 | int days                 | ActionResult<List<HerbDto>>               |
| POST   | `/check-expired`   | CheckExpired      | 检查并更新过期药材  | -                        | ActionResult                              |
| GET    | `/statistics`      | GetStatistics     | 获取药材状态统计   | -                        | ActionResult<Dictionary<HerbStatus, int>> |

**特点说明**:

- 支持完整的CRUD操作
- 提供导入导出功能
- 支持状态管理和库存预警
- 具有缓存机制

---

### 4. PatientsController - 患者管理控制器

**基础路由**: `api/v1.0/Patients`

| HTTP方法 | 路由                | 方法名               | 描述          | 参数类型                      | 返回类型                                          |
| ------ | ----------------- | ----------------- | ----------- | ------------------------- | --------------------------------------------- |
| POST   | `/`               | Add               | 新增病人        | PatientDetailDto          | IActionResult                                 |
| PUT    | `/{id}`           | Edit              | 编辑病人        | Guid id, PatientDetailDto | IActionResult                                 |
| PATCH  | `/{id}/enable`    | Enable            | 启用患者档案      | Guid id                   | IActionResult                                 |
| PATCH  | `/{id}/disable`   | Disable           | 禁用患者档案（软删除） | Guid id                   | IActionResult                                 |
| GET    | `/{id}`           | GetById           | 获取病人详情      | Guid id                   | ApiResponse<PatientDetailDto>                 |
| GET    | `/`               | GetAll            | 获取全部病人      | -                         | ApiResponse<List<PatientDetailDto>>           |
| POST   | `/paged`          | GetPaged          | 分页条件查询      | PatientPagedQueryDto      | ApiResponse<PagedResultDto<PatientDetailDto>> |
| PATCH  | `/batch-disable`  | BatchDisable      | 批量禁用患者档案    | PatientBatchIdsDto        | IActionResult                                 |
| PATCH  | `/batch-enable`   | BatchEnable       | 批量启用患者档案    | PatientBatchIdsDto        | IActionResult                                 |
| GET    | `/search`         | Search            | 搜索患者档案      | string keyword            | ApiResponse<List<PatientDetailDto>>           |
| POST   | `/import`         | Import            | 导入患者档案数据    | List<PatientDetailDto>    | IActionResult                                 |
| GET    | `/export`         | Export            | 导出患者档案数据    | -                         | ApiResponse<List<PatientDetailDto>>           |
| GET    | `/{id}/records`   | GetHistory        | 获取患者档案历史病历  | Guid id                   | ApiResponse<List<RecordDto>>                  |
| GET    | `/active`         | GetActivePatients | 获取启用的患者档案列表 | -                         | ApiResponse<List<PatientDetailDto>>           |
| POST   | `/find-or-create` | FindOrCreate      | 查询或创建患者档案   | PatientDetailDto          | ApiResponse<PatientDetailDto>                 |

**特点说明**:

- 实现软删除策略，不提供删除接口
- 支持权限控制（禁用的患者档案仅管理员可查询）
- 提供导入导出功能
- 具有缓存机制

---

### 5. BillingController - 费用结算控制器

**基础路由**: `api/v1.0/Billing`

| HTTP方法 | 路由                     | 方法名                | 描述           | 参数类型                   | 返回类型                          |
| ------ | ---------------------- | ------------------ | ------------ | ---------------------- | ----------------------------- |
| GET    | `/`                    | GetList            | 获取费用结算列表     | -                      | ApiResponse<List<BillingDto>> |
| GET    | `/{id}`                | GetById            | 获取费用结算详情     | Guid id                | ApiResponse<BillingDetailDto> |
| POST   | `/`                    | Add                | 新增费用结算       | BillingCreateDto       | ApiResponse<object>           |
| PUT    | `/`                    | Update             | 编辑费用结算       | BillingEditDto         | ApiResponse<object>           |
| DELETE | `/{id}`                | Delete             | 删除费用结算       | Guid id                | ApiResponse<object>           |
| POST   | `/mark-paid/{id}`      | MarkAsPaid         | 标记为已付款       | Guid id                | ApiResponse<object>           |
| POST   | `/complete/{id}`       | MarkAsCompleted    | 标记为已完成       | Guid id                | ApiResponse<object>           |
| POST   | `/request-refund/{id}` | RequestRefund      | 申请退款         | Guid id, string reason | ApiResponse<object>           |
| POST   | `/approve-refund/{id}` | ApproveRefund      | 批准退款         | Guid id                | ApiResponse<object>           |
| POST   | `/reject-refund/{id}`  | RejectRefund       | 拒绝退款         | Guid id                | ApiResponse<object>           |
| POST   | `/cancel/{id}`         | Cancel             | 取消费用结算       | Guid id                | ApiResponse<object>           |
| GET    | `/patient/{patientId}` | GetByPatientId     | 根据患者ID获取费用结算 | Guid patientId         | ApiResponse<List<BillingDto>> |
| GET    | `/search`              | Search             | 搜索费用结算       | string keyword         | ApiResponse<List<BillingDto>> |
| GET    | `/refundable`          | GetRefundableBills | 获取可退款费用结算    | -                      | ApiResponse<List<BillingDto>> |
| GET    | `/status/{status}`     | GetByStatus        | 根据状态获取费用结算   | BillingStatus status   | ApiResponse<List<BillingDto>> |

**特点说明**:

- 支持完整的费用结算流程管理
- 提供退款流程支持
- 具有缓存机制

---

### 6. UsersController - 用户管理控制器

**基础路由**: `api/v1.0/Users`

| HTTP方法 | 路由                    | 方法名            | 描述         | 参数类型              | 返回类型          |
| ------ | --------------------- | -------------- | ---------- | ----------------- | ------------- |
| GET    | `/search`             | Search         | 分页查找用户     | UserQueryDto      | IActionResult |
| POST   | `/add`                | Add            | 新增用户       | UserCreateDto     | IActionResult |
| PUT    | `/update`             | Update         | 编辑用户       | UserDetailDto     | IActionResult |
| POST   | `/disable/{id}`       | Disable        | 禁用用户（软删除）  | Guid id           | IActionResult |
| POST   | `/enable/{id}`        | Enable         | 启用用户       | Guid id           | IActionResult |
| POST   | `/batchDisable`       | BatchDisable   | 批量禁用用户     | UserBatchIdsDto   | IActionResult |
| POST   | `/batchEnable`        | BatchEnable    | 批量启用用户     | UserBatchIdsDto   | IActionResult |
| POST   | `/resetPassword/{id}` | ResetPassword  | 管理员重置密码    | Guid id           | IActionResult |
| POST   | `/changePassword`     | ChangePassword | 用户修改密码     | ChangePasswordDto | IActionResult |
| POST   | `/changeProfile`      | ChangeProfile  | 用户修改个人信息   | ChangeProfileDto  | IActionResult |
| GET    | `/getRoles`           | GetRoles       | 获取所有角色     | -                 | IActionResult |
| GET    | `/getById/{id}`       | GetById        | 根据Id获取用户详情 | Guid id           | IActionResult |
| GET    | `/active`             | GetActiveUsers | 获取启用的用户列表  | -                 | IActionResult |

**特点说明**:

- 实现软删除策略，不提供删除接口
- 支持权限控制（禁用的用户仅管理员可查询）
- 提供完整的用户管理功能

---

### 7. HealthController - 健康检查控制器

**基础路由**: `api/Health`

| HTTP方法 | 路由          | 方法名               | 描述      | 参数类型 | 返回类型          |
| ------ | ----------- | ----------------- | ------- | ---- | ------------- |
| GET    | `/`         | Get               | 基本健康检查  | -    | string        |
| GET    | `/database` | CheckDatabase     | 数据库健康检查 | -    | IActionResult |
| GET    | `/detailed` | GetDetailedStatus | 详细系统状态  | -    | IActionResult |

**特点说明**:

- 不需要认证（AllowAnonymous）
- 提供系统和数据库状态监控

---

### 8. DiagnosisTreatmentController - 诊疗控制器

**基础路由**: `api/v1.0/DiagnosisTreatment`

| HTTP方法 | 路由      | 方法名     | 描述     | 参数类型                        | 返回类型                        |
| ------ | ------- | ------- | ------ | --------------------------- | --------------------------- |
| GET    | `/`     | GetList | 获取诊疗列表 | -                           | List<DiagnosisTreatmentDto> |
| GET    | `/{id}` | GetById | 获取诊疗详情 | Guid id                     | DiagnosisTreatmentDetailDto |
| POST   | `/`     | Add     | 新增诊疗   | DiagnosisTreatmentCreateDto | ActionResult                |
| PUT    | `/`     | Update  | 编辑诊疗   | DiagnosisTreatmentEditDto   | ActionResult                |
| DELETE | `/{id}` | Delete  | 删除诊疗   | Guid id                     | ActionResult                |

---

### 9. FormulaTemplatesController - 经验方模板控制器

**基础路由**: `api/v1.0/FormulaTemplate`

| HTTP方法 | 路由        | 方法名     | 描述       | 参数类型                           | 返回类型                           |
| ------ | --------- | ------- | -------- | ------------------------------ | ------------------------------ |
| GET    | `/`       | GetList | 获取所有模板列表 | -                              | List<FormulaTemplateDto>       |
| GET    | `/{id}`   | GetById | 获取模板详情   | Guid id                        | FormulaTemplateDetailDto       |
| POST   | `/`       | Add     | 新增模板     | FormulaTemplateCreateDto       | ApiSuccessResponse             |
| PUT    | `/`       | Update  | 编辑模板     | FormulaTemplateEditDto         | ApiSuccessResponse             |
| DELETE | `/{id}`   | Delete  | 删除模板     | Guid id                        | ApiSuccessResponse             |
| POST   | `/import` | Import  | 批量导入模板   | List<FormulaTemplateImportDto> | object                         |
| POST   | `/export` | Export  | 导出模板数据   | -                              | List<FormulaTemplateDetailDto> |

---

### 10. PharmacyController - 药房控制器

**基础路由**: `api/v1.0/Pharmacy`

| HTTP方法 | 路由               | 方法名            | 描述         | 参数类型              | 返回类型              |
| ------ | ---------------- | -------------- | ---------- | ----------------- | ----------------- |
| GET    | `/waiting`       | GetWaitingList | 获取待抓药的处方列表 | -                 | List<PharmacyDto> |
| GET    | `/`              | GetList        | 获取药房单列表    | -                 | List<PharmacyDto> |
| GET    | `/{id}`          | GetById        | 获取药房单详情    | Guid id           | PharmacyDetailDto |
| POST   | `/`              | Add            | 新增药房单      | PharmacyCreateDto | ActionResult      |
| PUT    | `/`              | Update         | 编辑药房单      | PharmacyEditDto   | ActionResult      |
| DELETE | `/{id}`          | Delete         | 删除药房单      | Guid id           | ActionResult      |
| POST   | `/{id}/prepared` | MarkAsPrepared | 标记处方为已抓药   | Guid id           | ActionResult      |

---

### 11. PrescriptionsController - 处方管理控制器

**基础路由**: `api/v1.0/Prescriptions`

| HTTP方法 | 路由           | 方法名     | 描述     | 参数类型                  | 返回类型                  |
| ------ | ------------ | ------- | ------ | --------------------- | --------------------- |
| GET    | `/`          | GetList | 获取处方列表 | -                     | List<PrescriptionDto> |
| GET    | `/{id}`      | GetById | 获取处方详情 | string id             | PrescriptionDetailDto |
| POST   | `/`          | Add     | 新增处方   | PrescriptionCreateDto | ActionResult          |
| PUT    | `/`          | Update  | 编辑处方   | PrescriptionEditDto   | ActionResult          |
| DELETE | `/{id}`      | Delete  | 删除处方   | string id             | ActionResult          |
| POST   | `/void/{id}` | Cancel  | 取消处方   | string id             | ActionResult          |

---

### 12. QueueingController - 排队管理控制器

**基础路由**: `api/v1.0/Queueing`

| HTTP方法 | 路由               | 方法名      | 描述     | 参数类型              | 返回类型              |
| ------ | ---------------- | -------- | ------ | ----------------- | ----------------- |
| GET    | `/`              | GetList  | 获取排队列表 | -                 | List<QueueingDto> |
| GET    | `/{id}`          | GetById  | 获取排队详情 | Guid id           | QueueingDetailDto |
| POST   | `/`              | Add      | 新增排队   | QueueingCreateDto | ActionResult      |
| PUT    | `/`              | Update   | 编辑排队   | QueueingEditDto   | ActionResult      |
| DELETE | `/{id}`          | Delete   | 删除排队   | Guid id           | ActionResult      |
| POST   | `/cancel/{id}`   | Cancel   | 取消排队   | Guid id           | ActionResult      |
| POST   | `/complete/{id}` | Complete | 完成排队   | Guid id           | ActionResult      |
| POST   | `/hold/{id}`     | Hold     | 暂停排队   | Guid id           | ActionResult      |

---

### 13. RecordsController - 病历控制器

**基础路由**: `api/v1.0/Record`

| HTTP方法 | 路由                     | 方法名           | 描述         | 参数类型                            | 返回类型            |
| ------ | ---------------------- | ------------- | ---------- | ------------------------------- | --------------- |
| GET    | `/`                    | GetList       | 获取病历列表     | -                               | List<RecordDto> |
| GET    | `/patient/{patientId}` | GetByPatient  | 根据患者ID获取病历 | Guid patientId                  | List<RecordDto> |
| GET    | `/{id}`                | GetById       | 获取病历详情     | Guid id                         | RecordDetailDto |
| POST   | `/`                    | Add           | 新增病历       | RecordCreateDto                 | ActionResult    |
| PUT    | `/`                    | Update        | 编辑病历       | RecordEditDto                   | ActionResult    |
| DELETE | `/{id}`                | Delete        | 删除病历       | Guid id                         | ActionResult    |
| POST   | `/share/{id}`          | MarkAsShared  | 标记病历为共享    | Guid id, List<string> doctorIds | ActionResult    |
| POST   | `/unshare/{id}`        | RevokeSharing | 撤销病历共享     | Guid id                         | ActionResult    |
| GET    | `/shared/{doctorId}`   | GetShared     | 获取共享给医生的病历 | Guid doctorId                   | List<RecordDto> |

---

### 14. RegistrationController - 挂号管理控制器

**基础路由**: `api/v1.0/Registration`

| HTTP方法 | 路由             | 方法名     | 描述        | 参数类型                  | 返回类型                  |
| ------ | -------------- | ------- | --------- | --------------------- | --------------------- |
| GET    | `/`            | GetList | 获取挂号列表    | -                     | List<RegistrationDto> |
| GET    | `/{id}`        | GetById | 获取挂号详情    | Guid id               | RegistrationDetailDto |
| POST   | `/`            | Add     | 新增挂号      | RegistrationCreateDto | ActionResult          |
| PUT    | `/`            | Update  | 编辑挂号      | RegistrationEditDto   | ActionResult          |
| DELETE | `/{id}`        | Delete  | 删除挂号      | Guid id               | ActionResult          |
| POST   | `/cancel/{id}` | Cancel  | 取消挂号（软删除） | Guid id               | ActionResult          |

---

### 15. SyncController - 数据同步控制器

**基础路由**: `api/v1.0/Sync`

| HTTP方法 | 路由                   | 方法名             | 描述          | 参数类型                   | 返回类型              |
| ------ | -------------------- | --------------- | ----------- | ---------------------- | ----------------- |
| GET    | `/logs`              | GetLogList      | 获取所有同步日志    | -                      | List<SyncLogDto>  |
| GET    | `/logs/last`         | GetLastLog      | 获取最近一次同步信息  | -                      | SyncLogDto?       |
| GET    | `/logs/paged`        | GetLogPaged     | 分页查询同步日志    | int page, int pageSize | List<SyncLogDto>  |
| POST   | `/logs`              | AddLog          | 新增同步日志      | SyncLogCreateDto       | ActionResult      |
| DELETE | `/logs/{id}`         | DeleteLog       | 删除同步日志      | string id              | ActionResult      |
| GET    | `/connection-status` | CheckConnection | 检测中心数据库连接状态 | -                      | bool              |
| POST   | `/manual-sync`       | ManualSync      | 手动触发同步      | -                      | ActionResult      |
| GET    | `/mode`              | GetSyncMode     | 获取当前同步模式    | -                      | SyncMode          |
| POST   | `/mode`              | SetSyncMode     | 设置同步模式      | SyncMode mode          | ActionResult      |
| GET    | `/tasks`             | GetTaskList     | 获取同步任务列表    | -                      | List<SyncTaskDto> |
| GET    | `/tasks/{id}`        | GetTaskDetail   | 获取同步任务详情    | Guid id                | SyncTaskDetailDto |
| POST   | `/tasks`             | AddTask         | 新增同步任务      | SyncTaskCreateDto      | ActionResult      |
| PUT    | `/tasks`             | UpdateTask      | 更新同步任务      | SyncTaskEditDto        | ActionResult      |
| DELETE | `/tasks/{id}`        | DeleteTask      | 删除同步任务      | Guid id                | ActionResult      |

---

### 16. TreatmentRoomController - 治疗室控制器

**基础路由**: `api/v1.0/TreatmentRoom`

| HTTP方法 | 路由                 | 方法名         | 描述        | 参数类型                   | 返回类型                   |
| ------ | ------------------ | ----------- | --------- | ---------------------- | ---------------------- |
| GET    | `/`                | GetList     | 获取治疗室单列表  | -                      | List<TreatmentRoomDto> |
| GET    | `/{id}`            | GetById     | 获取治疗室单详情  | Guid id                | TreatmentRoomDetailDto |
| POST   | `/`                | Add         | 新增治疗室单    | TreatmentRoomCreateDto | ActionResult           |
| PUT    | `/`                | Update      | 编辑治疗室单    | TreatmentRoomEditDto   | ActionResult           |
| DELETE | `/{id}`            | Delete      | 删除治疗室单    | Guid id                | ActionResult           |
| GET    | `/status/{status}` | GetByStatus | 根据状态获取治疗室 | string status          | List<TreatmentRoomDto> |

---

### 17. UnifiedConfigController - 统一配置管理控制器

**基础路由**: `api/UnifiedConfig`

| HTTP方法 | 路由                          | 方法名                       | 描述         | 参数类型                                                          | 返回类型                             |
| ------ | --------------------------- | ------------------------- | ---------- | ------------------------------------------------------------- | -------------------------------- |
| GET    | `/global-settings`          | GetGlobalSettings         | 获取全局设置     | -                                                             | GlobalSettingsDto                |
| PUT    | `/global-settings`          | UpdateGlobalSettings      | 更新全局设置     | GlobalSettingsDto                                             | ActionResult                     |
| GET    | `/settings/{key}`           | GetSetting                | 获取设置值      | string key, string? defaultValue                              | string                           |
| POST   | `/settings`                 | SetSetting                | 设置配置值      | SetSettingRequest                                             | ActionResult                     |
| POST   | `/settings/batch`           | SetSettings               | 批量设置配置值    | Dictionary<string, object>                                    | ActionResult                     |
| GET    | `/settings`                 | GetSettings               | 分页查询设置     | string? group, string? keyword, int pageIndex, int pageSize   | PagedResult<SettingsDto>         |
| GET    | `/settings/group/{group}`   | GetSettingsByGroup        | 根据分组获取所有设置 | string group                                                  | Dictionary<string, string>       |
| DELETE | `/settings/{key}`           | DeleteSetting             | 删除设置       | string key                                                    | ActionResult                     |
| GET    | `/diagnosis-catalogs`       | GetDiagnosisCatalogs      | 获取所有诊断目录   | -                                                             | List<DiagnosisCatalogDto>        |
| GET    | `/diagnosis-catalogs/paged` | GetDiagnosisCatalogsPaged | 分页查询诊断目录   | string? keyword, bool? isEnabled, int pageIndex, int pageSize | PagedResult<DiagnosisCatalogDto> |
| GET    | `/diagnosis-catalogs/{id}`  | GetDiagnosisCatalog       | 根据ID获取诊断目录 | Guid id                                                       | DiagnosisCatalogDto              |
| POST   | `/diagnosis-catalogs`       | CreateDiagnosisCatalog    | 创建诊断目录     | DiagnosisCatalogDto                                           | ActionResult                     |
| PUT    | `/diagnosis-catalogs`       | UpdateDiagnosisCatalog    | 更新诊断目录     | DiagnosisCatalogDto                                           | ActionResult                     |
| DELETE | `/diagnosis-catalogs/{id}`  | DeleteDiagnosisCatalog    | 删除诊断目录     | Guid id                                                       | ActionResult                     |
| GET    | `/treatment-catalogs`       | GetTreatmentCatalogs      | 获取所有治疗目录   | -                                                             | List<TreatmentCatalogDto>        |
| POST   | `/cache/refresh-all`        | RefreshAllCache           | 刷新所有配置缓存   | -                                                             | ActionResult                     |
| POST   | `/cache/refresh-settings`   | RefreshSettingCache       | 刷新设置缓存     | -                                                             | ActionResult                     |

**特点说明**:

- 管理员功能需要特殊权限（Admin角色）
- 提供完整的系统配置管理

---

### 18. UnifiedLogsController - 统一日志管理控制器

**基础路由**: `api/UnifiedLogs`

| HTTP方法 | 路由                          | 方法名                     | 描述         | 参数类型                                              | 返回类型                       |
| ------ | --------------------------- | ----------------------- | ---------- | ------------------------------------------------- | -------------------------- |
| POST   | `/query`                    | GetLogs                 | 分页查询日志     | LogQueryDto                                       | PagedResult<LogDto>        |
| GET    | `/{id}`                     | GetLog                  | 根据ID获取日志详情 | Guid id                                           | LogDto                     |
| POST   | `/`                         | CreateLog               | 创建操作日志     | LogCreateDto                                      | ActionResult               |
| POST   | `/batch`                    | CreateLogs              | 批量创建日志     | List<LogCreateDto>                                | ActionResult               |
| DELETE | `/expired`                  | DeleteExpiredLogs       | 删除过期日志     | DateTime beforeDate                               | ActionResult               |
| GET    | `/statistics`               | GetLogStatistics        | 获取日志统计信息   | DateTime startDate, DateTime endDate              | Dictionary<string, object> |
| GET    | `/user-statistics/{userId}` | GetUserActionStatistics | 获取用户操作统计   | Guid userId, DateTime startDate, DateTime endDate | Dictionary<string, object> |
| POST   | `/export/csv`               | ExportLogsToCsv         | 导出日志到CSV   | LogQueryDto                                       | ActionResult               |
| POST   | `/export/excel`             | ExportLogsToExcel       | 导出日志到Excel | LogQueryDto                                       | ActionResult               |
| POST   | `/user-login`               | LogUserLogin            | 记录用户登录日志   | UserLoginLogRequest                               | ActionResult               |
| POST   | `/user-logout`              | LogUserLogout           | 记录用户登出日志   | UserLogoutLogRequest                              | ActionResult               |

**特点说明**:

- 提供完整的日志管理功能
- 支持日志导出（CSV和Excel格式）
- 管理员功能需要特殊权限

---

## 📊 系统统计信息

### 接口数量统计

- **总控制器数**: 18个业务控制器
- **总接口数**: 约170+个API接口
- **核心业务模块**: 13个
- **系统管理模块**: 5个

### 业务模块分布

| 模块类型 | 控制器数量 | 主要功能            |
| ---- | ----- | --------------- |
| 核心业务 | 13个   | 用户、医生、患者、药材、处方等 |
| 系统管理 | 3个    | 配置、日志、健康检查      |
| 数据同步 | 1个    | 多数据源同步          |
| 认证授权 | 1个    | 用户认证和授权         |

### 功能特性覆盖

- ✅ **用户认证**: JWT令牌认证机制
- ✅ **权限控制**: 基于角色的访问控制
- ✅ **软删除**: 关键实体的软删除策略
- ✅ **缓存支持**: 查询接口的内存缓存
- ✅ **批量操作**: 批量禁用/启用功能
- ✅ **导入导出**: 数据的批量导入导出
- ✅ **日志审计**: 完整的操作日志记录
- ✅ **健康检查**: 系统状态监控
- ✅ **配置管理**: 动态系统配置
- ✅ **数据同步**: 多数据源同步功能

---

## 🚀 快速开始

### 基础环境要求

- .NET 8.0 SDK
- SQL Server / LocalDB / SQLite
- Visual Studio 2022 或 VS Code

### 启动WebAPI服务

```bash
# 进入WebAPI项目目录
cd src/Backend/Services/LYBT.WebAPI

# 运行服务
dotnet run
```

### 访问接口文档

- **Swagger UI**: `https://localhost:5001/swagger`
- **健康检查**: `https://localhost:5001/api/health`
- **API版本**: `v1.0`

### 认证方式

```http
Authorization: Bearer <JWT_TOKEN>
```

### 默认登录信息

- **用户名**: `sysadmin`
- **密码**: `123456`

---

## 📝 注意事项

### 开发建议

1. **统一响应格式**: 所有接口都应使用 `ApiResponse<T>` 格式
2. **错误处理**: 实现全局异常处理中间件
3. **参数验证**: 使用模型验证特性进行参数校验
4. **缓存策略**: 合理使用内存缓存提升性能
5. **日志记录**: 记录关键操作和异常信息

### 安全注意

1. **JWT令牌**: 注意令牌的过期时间和刷新机制
2. **权限控制**: 严格按照用户角色控制接口访问
3. **参数校验**: 对所有输入参数进行严格校验
4. **敏感信息**: 避免在日志中记录敏感信息

### 性能优化

1. **分页查询**: 大数据量查询务必使用分页
2. **缓存使用**: 频繁查询的数据使用缓存
3. **数据库优化**: 合理使用索引和查询优化
4. **批量操作**: 使用批量接口而非循环调用

---

## 📞 技术支持

### 文档更新

本文档将随着API接口的更新而持续维护，确保与实际代码保持同步。

### 问题反馈

如果发现接口文档与实际实现不符，请及时反馈给开发团队。

---

**文档版本**: v1.0  
**最后更新**: 2025-08-01  
**维护团队**: LYBTZYZS开发组