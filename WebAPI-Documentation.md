# LYBT 中医诊所管理系统 WebAPI 文档

## 系统概述

LYBT 中医诊所管理系统是基于 ASP.NET Core 8.0 构建的模块化中医诊所管理平台，采用 RESTful API 设计风格，为中医诊所提供完整的业务管理解决方案。

### 技术架构
- **框架版本**: ASP.NET Core 8.0
- **架构模式**: 模块化分层架构
- **API 设计**: RESTful API + 统一响应格式
- **身份验证**: JWT Bearer Token
- **API 版本**: v1.0 (支持版本控制)
- **文档**: Swagger UI 集成

### 核心特性
- 模块化设计，各业务模块独立
- 统一的身份验证和授权机制
- 完整的操作日志审计
- 数据缓存优化
- 软删除策略保护数据安全
- 统一的异常处理和响应格式

## 认证说明

### JWT Token 认证

系统采用 JWT (JSON Web Token) 进行身份验证和授权。

#### 获取 Token
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password"
}
```

#### 响应格式
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": "guid",
      "userName": "admin",
      "realName": "管理员",
      "role": "Admin"
    }
  },
  "statusCode": 200,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

#### 使用 Token
在请求头中携带 Token：
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 用户角色权限

| 角色 | 描述 | 权限范围 |
|------|------|----------|
| Admin | 系统管理员 | 完全访问权限，包括系统配置和用户管理 |
| DiagnosingDoctor | 主治医生 | 诊疗相关功能，患者管理，处方开具 |
| PharmacyStaff | 药剂师 | 药房管理，处方调配，药材管理 |
| CashierStaff | 收费人员 | 费用结算，收费管理 |
| PhysiotherapyStaff | 理疗师 | 治疗室管理，理疗服务 |
| Staff | 挂号人员 | 患者挂号，排队管理 |

## 统一响应格式

### 成功响应
```json
{
  "success": true,
  "message": "操作成功",
  "data": {}, // 响应数据，根据接口而定
  "statusCode": 200,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 错误响应
```json
{
  "success": false,
  "message": "错误描述",
  "data": null,
  "statusCode": 400,
  "timestamp": "2024-01-01T00:00:00Z",
  "errorCode": "ERROR_CODE", // 可选
  "traceId": "trace-id" // 可选，用于日志追踪
}
```

### 分页响应
```json
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [], // 数据列表
    "totalCount": 100,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "statusCode": 200,
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## API 接口文档

### 1. 认证模块 (Auth)

**基础路径**: `/api/v1/auth`

#### 1.1 用户登录
- **接口**: `POST /login`
- **描述**: 用户登录获取访问令牌
- **权限**: 公开访问

**请求参数**:
```json
{
  "username": "string", // 用户名
  "password": "string", // 密码
  "clientIp": "string", // 客户端IP (自动获取)
  "userAgent": "string" // 用户代理 (自动获取)
}
```

#### 1.2 用户登出
- **接口**: `POST /logout`
- **描述**: 用户登出
- **权限**: 需要登录

**请求参数**:
```json
{
  "token": "string" // JWT Token
}
```

#### 1.3 修改管理员密码
- **接口**: `POST /changeSysAdminPassword`
- **描述**: 修改系统管理员密码
- **权限**: 需要登录

**请求参数**:
```json
{
  "currentPassword": "string", // 当前密码
  "newPassword": "string" // 新密码
}
```

### 2. 用户管理模块 (Users)

**基础路径**: `/api/v1/users`

#### 2.1 分页查询用户
- **接口**: `GET /search`
- **描述**: 分页查询用户列表，支持关键词、角色、状态筛选
- **权限**: 需要登录

**查询参数**:
- `keyword`: 搜索关键词 (可选)
- `role`: 用户角色 (可选)
- `status`: 用户状态 (可选)
- `page`: 页码，默认1
- `pageSize`: 页大小，默认20

#### 2.2 新增用户
- **接口**: `POST /add`
- **描述**: 创建新用户，密码设为系统默认值
- **权限**: 需要登录

**请求参数**:
```json
{
  "userName": "string", // 用户名
  "realName": "string", // 真实姓名
  "email": "string", // 邮箱 (可选)
  "phoneNumber": "string", // 手机号 (可选)
  "role": "Admin|DiagnosingDoctor|PharmacyStaff|CashierStaff|PhysiotherapyStaff|Staff"
}
```

#### 2.3 更新用户信息
- **接口**: `PUT /update`
- **描述**: 更新用户信息
- **权限**: 需要登录

#### 2.4 禁用/启用用户
- **接口**: `POST /disable/{id}` / `POST /enable/{id}`
- **描述**: 禁用或启用用户 (软删除)
- **权限**: 需要登录

#### 2.5 重置密码
- **接口**: `POST /resetPassword/{id}`
- **描述**: 管理员重置用户密码为默认值
- **权限**: 需要登录

#### 2.6 修改密码
- **接口**: `POST /changePassword`
- **描述**: 用户修改自己的密码
- **权限**: 需要登录

**请求参数**:
```json
{
  "oldPassword": "string", // 旧密码
  "newPassword": "string" // 新密码
}
```

#### 2.7 修改个人信息
- **接口**: `POST /changeProfile`
- **描述**: 用户修改个人信息
- **权限**: 需要登录

#### 2.8 获取用户详情
- **接口**: `GET /getById/{id}`
- **描述**: 根据ID获取用户详情
- **权限**: 需要登录

#### 2.9 获取启用用户列表
- **接口**: `GET /active`
- **描述**: 获取所有启用的用户列表
- **权限**: 需要登录

#### 2.10 获取角色列表
- **接口**: `GET /getRoles`
- **描述**: 获取所有用户角色选项
- **权限**: 需要登录

### 3. 患者管理模块 (Patients)

**基础路径**: `/api/v1/patients`

#### 3.1 新增患者
- **接口**: `POST /`
- **描述**: 创建新患者档案
- **权限**: 需要登录

**请求参数**:
```json
{
  "name": "string", // 姓名
  "gender": "Male|Female|Unknown", // 性别
  "age": "number", // 年龄
  "phoneNumber": "string", // 电话 (可选)
  "address": "string", // 地址 (可选)
  "idCard": "string", // 身份证号 (可选)
  "allergies": "string", // 过敏史 (可选)
  "medicalHistory": "string" // 病史 (可选)
}
```

#### 3.2 更新患者信息
- **接口**: `PUT /{id}`
- **描述**: 更新患者档案信息
- **权限**: 需要登录

#### 3.3 获取患者详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取患者详情
- **权限**: 需要登录
- **缓存**: 5分钟

#### 3.4 获取所有患者
- **接口**: `GET /`
- **描述**: 获取所有患者列表 (小数据量)
- **权限**: 需要登录
- **缓存**: 5分钟

#### 3.5 分页查询患者
- **接口**: `POST /paged`
- **描述**: 分页条件查询患者
- **权限**: 需要登录

**请求参数**:
```json
{
  "keyword": "string", // 搜索关键词 (可选)
  "gender": "Male|Female|Unknown", // 性别筛选 (可选)
  "page": "number", // 页码，默认1
  "pageSize": "number" // 页大小，默认20
}
```

#### 3.6 搜索患者
- **接口**: `GET /search`
- **描述**: 根据关键词搜索患者
- **权限**: 需要登录

**查询参数**:
- `keyword`: 搜索关键词

#### 3.7 禁用/启用患者
- **接口**: `PATCH /{id}/disable` / `PATCH /{id}/enable`
- **描述**: 禁用或启用患者档案
- **权限**: 需要登录

#### 3.8 批量操作
- **接口**: `PATCH /batch-disable` / `PATCH /batch-enable`
- **描述**: 批量禁用或启用患者档案
- **权限**: 需要登录

#### 3.9 获取启用患者列表
- **接口**: `GET /active`
- **描述**: 获取所有启用的患者列表
- **权限**: 需要登录

#### 3.10 快速创建患者
- **接口**: `POST /quick-create`
- **描述**: 快速创建患者档案 (用于快速看诊场景)
- **权限**: 需要登录

#### 3.11 获取患者病历历史
- **接口**: `GET /{id}/records`
- **描述**: 获取患者的历史病历记录
- **权限**: 需要登录

#### 3.12 导入/导出患者数据
- **接口**: `POST /import` / `GET /export`
- **描述**: 批量导入或导出患者数据
- **权限**: 需要登录

### 4. 医生管理模块 (Doctors)

**基础路径**: `/api/v1/doctors`

#### 4.1 分页查询医生
- **接口**: `POST /paged`
- **描述**: 分页查询医生列表
- **权限**: 需要登录

#### 4.2 搜索医生
- **接口**: `GET /search`
- **描述**: 根据关键词搜索医生
- **权限**: 需要登录
- **缓存**: 5分钟

#### 4.3 获取在职医生列表
- **接口**: `GET /active`
- **描述**: 获取所有在职医生列表
- **权限**: 需要登录
- **缓存**: 10分钟

#### 4.4 获取医生详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取医生详情
- **权限**: 需要登录
- **缓存**: 10分钟

#### 4.5 根据用户ID获取医生
- **接口**: `GET /by-user/{userId}`
- **描述**: 根据用户ID获取医生详情
- **权限**: 需要登录

#### 4.6 新增医生
- **接口**: `POST /`
- **描述**: 创建新医生档案
- **权限**: 需要登录

**请求参数**:
```json
{
  "userId": "guid", // 关联用户ID
  "title": "string", // 职称
  "department": "string", // 科室
  "specialties": "string", // 专长
  "qualification": "string", // 资质证书
  "introduction": "string", // 简介
  "consultationFee": "decimal", // 诊疗费
  "status": "Active|Inactive" // 状态
}
```

#### 4.7 更新医生信息
- **接口**: `PUT /`
- **描述**: 更新医生档案信息
- **权限**: 需要登录

#### 4.8 禁用/启用医生
- **接口**: `PATCH /{id}/disable` / `PATCH /{id}/enable`
- **描述**: 禁用或启用医生档案
- **权限**: 需要登录

#### 4.9 批量操作
- **接口**: `PATCH /batch-disable` / `PATCH /batch-enable`
- **描述**: 批量禁用或启用医生档案
- **权限**: 需要登录

#### 4.10 检查用户关联
- **接口**: `GET /check-user-link/{userId}`
- **描述**: 检查用户是否已关联医生档案
- **权限**: 需要登录

#### 4.11 获取角色列表
- **接口**: `GET /roles`
- **描述**: 获取用户角色枚举列表
- **权限**: 需要登录

### 5. 挂号管理模块 (Registration)

**基础路径**: `/api/v1/registration`

#### 5.1 获取挂号列表
- **接口**: `GET /`
- **描述**: 获取挂号记录列表
- **权限**: 需要登录

#### 5.2 获取挂号详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取挂号详情
- **权限**: 需要登录

#### 5.3 新增挂号
- **接口**: `POST /`
- **描述**: 创建新的挂号记录
- **权限**: 需要登录

**请求参数**:
```json
{
  "patientId": "guid", // 患者ID
  "doctorId": "guid", // 医生ID
  "appointmentDate": "datetime", // 预约时间
  "symptoms": "string", // 症状描述
  "registrationFee": "decimal" // 挂号费
}
```

#### 5.4 编辑挂号
- **接口**: `PUT /`
- **描述**: 更新挂号信息
- **权限**: 需要登录

#### 5.5 删除挂号
- **接口**: `DELETE /{id}`
- **描述**: 删除挂号记录
- **权限**: 需要登录

#### 5.6 取消挂号
- **接口**: `POST /cancel/{id}`
- **描述**: 取消挂号 (软删除)
- **权限**: 需要登录

### 6. 排队管理模块 (Queueing)

**基础路径**: `/api/v1/queueing`

#### 6.1 获取排队列表
- **接口**: `GET /`
- **描述**: 获取当前排队列表
- **权限**: 需要登录

#### 6.2 获取排队详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取排队详情
- **权限**: 需要登录

#### 6.3 新增排队
- **接口**: `POST /`
- **描述**: 添加患者到排队列表
- **权限**: 需要登录

#### 6.4 更新排队信息
- **接口**: `PUT /`
- **描述**: 更新排队信息
- **权限**: 需要登录

#### 6.5 排队状态操作
- **接口**: `POST /complete/{id}` / `POST /hold/{id}` / `POST /cancel/{id}`
- **描述**: 完成排队 / 暂停排队 / 取消排队
- **权限**: 需要登录

### 7. 诊断治疗模块 (DiagnosisTreatment)

**基础路径**: `/api/v1/diagnosistreatment`

#### 7.1 获取诊疗列表
- **接口**: `GET /`
- **描述**: 获取诊疗记录列表
- **权限**: 需要登录

#### 7.2 获取诊疗详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取诊疗详情
- **权限**: 需要登录

#### 7.3 新增诊疗
- **接口**: `POST /`
- **描述**: 创建新的诊疗记录
- **权限**: 需要登录

**请求参数**:
```json
{
  "patientId": "guid", // 患者ID
  "doctorId": "guid", // 医生ID
  "diagnosis": "string", // 诊断结果
  "symptoms": "string", // 症状
  "treatmentPlan": "string", // 治疗方案
  "prescription": "string", // 处方
  "nextVisitDate": "datetime" // 下次复诊时间 (可选)
}
```

#### 7.4 编辑诊疗
- **接口**: `PUT /`
- **描述**: 更新诊疗记录
- **权限**: 需要登录

#### 7.5 删除诊疗
- **接口**: `DELETE /{id}`
- **描述**: 删除诊疗记录
- **权限**: 需要登录

### 8. 处方管理模块 (Prescriptions)

**基础路径**: `/api/v1/prescriptions`

#### 8.1 获取处方列表
- **接口**: `GET /`
- **描述**: 获取处方列表
- **权限**: 需要登录

#### 8.2 获取处方详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取处方详情
- **权限**: 需要登录

#### 8.3 新增处方
- **接口**: `POST /`
- **描述**: 创建新处方
- **权限**: 需要登录

#### 8.4 编辑处方
- **接口**: `PUT /`
- **描述**: 更新处方信息
- **权限**: 需要登录

#### 8.5 删除处方
- **接口**: `DELETE /{id}`
- **描述**: 删除处方
- **权限**: 需要登录

#### 8.6 作废处方
- **接口**: `POST /void/{id}`
- **描述**: 作废处方
- **权限**: 需要登录

### 9. 药材管理模块 (Herbs)

**基础路径**: `/api/herbs`

#### 9.1 获取药材列表
- **接口**: `GET /`
- **描述**: 获取所有药材列表
- **权限**: 需要登录
- **缓存**: 10分钟

#### 9.2 分页查询药材
- **接口**: `POST /paged`
- **描述**: 分页查询药材
- **权限**: 需要登录

#### 9.3 获取药材详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取药材详情
- **权限**: 需要登录

#### 9.4 新增药材
- **接口**: `POST /`
- **描述**: 添加新药材
- **权限**: 需要登录

**请求参数**:
```json
{
  "name": "string", // 药材名称
  "alias": "string", // 别名 (可选)
  "category": "string", // 分类
  "properties": "string", // 性味
  "functions": "string", // 功效
  "dosage": "string", // 用法用量
  "precautions": "string", // 注意事项 (可选)
  "price": "decimal", // 单价
  "unit": "string", // 单位
  "stock": "number", // 库存数量
  "expiryDate": "datetime", // 有效期 (可选)
  "supplier": "string", // 供应商 (可选)
  "status": "Available|OutOfStock|Expired|Discontinued" // 状态
}
```

#### 9.5 编辑药材
- **接口**: `PUT /`
- **描述**: 更新药材信息
- **权限**: 需要登录

#### 9.6 删除药材
- **接口**: `DELETE /{id}`
- **描述**: 删除药材
- **权限**: 需要登录

#### 9.7 更新药材状态
- **接口**: `PATCH /status`
- **描述**: 更新药材状态
- **权限**: 需要登录

#### 9.8 批量更新状态
- **接口**: `PATCH /batch-status`
- **描述**: 批量更新药材状态
- **权限**: 需要登录

#### 9.9 获取可用药材
- **接口**: `GET /available`
- **描述**: 获取可用药材列表
- **权限**: 需要登录
- **缓存**: 15分钟

#### 9.10 获取缺货药材
- **接口**: `GET /out-of-stock`
- **描述**: 获取缺货药材列表
- **权限**: 需要登录

#### 9.11 获取即将过期药材
- **接口**: `GET /expiring`
- **描述**: 获取即将过期的药材列表
- **权限**: 需要登录

**查询参数**:
- `days`: 提前天数，默认30天

#### 9.12 检查过期药材
- **接口**: `POST /check-expired`
- **描述**: 检查并更新过期药材状态
- **权限**: 需要登录

#### 9.13 获取药材统计
- **接口**: `GET /statistics`
- **描述**: 获取药材状态统计信息
- **权限**: 需要登录

#### 9.14 导入/导出药材
- **接口**: `POST /import` / `POST /export`
- **描述**: 批量导入或导出药材数据
- **权限**: 需要登录

### 10. 经验方模板模块 (FormulaTemplates)

**基础路径**: `/api/v1/formulatemplates`

#### 10.1 获取模板列表
- **接口**: `GET /`
- **描述**: 获取所有经验方模板
- **权限**: 需要登录

#### 10.2 获取模板详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取模板详情
- **权限**: 需要登录

#### 10.3 新增模板
- **接口**: `POST /`
- **描述**: 创建新的经验方模板
- **权限**: 需要登录

**请求参数**:
```json
{
  "name": "string", // 模板名称
  "category": "string", // 分类
  "symptoms": "string", // 适应症状
  "composition": [ // 组方
    {
      "herbId": "guid", // 药材ID
      "dosage": "string", // 用量
      "unit": "string" // 单位
    }
  ],
  "preparation": "string", // 制法
  "usage": "string", // 用法
  "precautions": "string", // 注意事项 (可选)
  "source": "string" // 方剂来源 (可选)
}
```

#### 10.4 编辑模板
- **接口**: `PUT /`
- **描述**: 更新模板信息
- **权限**: 需要登录

#### 10.5 删除模板
- **接口**: `DELETE /{id}`
- **描述**: 删除模板
- **权限**: 需要登录

#### 10.6 导入/导出模板
- **接口**: `POST /import` / `POST /export`
- **描述**: 批量导入或导出经验方模板
- **权限**: 需要登录

### 11. 药房管理模块 (Pharmacy)

**基础路径**: `/api/v1/pharmacy`

#### 11.1 获取待抓药列表
- **接口**: `GET /waiting`
- **描述**: 获取待抓药的处方列表
- **权限**: 需要登录

#### 11.2 获取药房单列表
- **接口**: `GET /`
- **描述**: 获取药房单列表
- **权限**: 需要登录

#### 11.3 获取药房单详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取药房单详情
- **权限**: 需要登录

#### 11.4 新增药房单
- **接口**: `POST /`
- **描述**: 创建新的药房单
- **权限**: 需要登录

#### 11.5 编辑药房单
- **接口**: `PUT /`
- **描述**: 更新药房单信息
- **权限**: 需要登录

#### 11.6 删除药房单
- **接口**: `DELETE /{id}`
- **描述**: 删除药房单
- **权限**: 需要登录

#### 11.7 标记已抓药
- **接口**: `POST /{id}/prepared`
- **描述**: 标记处方为已抓药状态
- **权限**: 需要登录

### 12. 费用结算模块 (Billing)

**基础路径**: `/api/v1/billing`

#### 12.1 获取费用列表
- **接口**: `GET /`
- **描述**: 获取费用结算列表
- **权限**: 需要登录

#### 12.2 获取费用详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取费用详情
- **权限**: 需要登录

#### 12.3 新增费用
- **接口**: `POST /`
- **描述**: 创建新的费用记录
- **权限**: 需要登录

**请求参数**:
```json
{
  "patientId": "guid", // 患者ID
  "items": [ // 费用明细
    {
      "itemName": "string", // 项目名称
      "itemType": "Registration|Consultation|Medicine|Treatment", // 项目类型
      "quantity": "number", // 数量
      "unitPrice": "decimal", // 单价
      "totalPrice": "decimal" // 小计
    }
  ],
  "totalAmount": "decimal", // 总金额
  "discountAmount": "decimal", // 优惠金额 (可选)
  "finalAmount": "decimal" // 最终金额
}
```

#### 12.4 编辑费用
- **接口**: `PUT /`
- **描述**: 更新费用信息
- **权限**: 需要登录

#### 12.5 删除费用
- **接口**: `DELETE /{id}`
- **描述**: 删除费用记录
- **权限**: 需要登录

#### 12.6 费用状态操作
- **接口**: 
  - `POST /mark-paid/{id}` - 标记已付款
  - `POST /complete/{id}` - 标记已完成
  - `POST /cancel/{id}` - 取消费用
- **权限**: 需要登录

#### 12.7 退费操作
- **接口**:
  - `POST /request-refund/{id}` - 申请退费
  - `POST /approve-refund/{id}` - 批准退费
  - `POST /reject-refund/{id}` - 拒绝退费
- **权限**: 需要登录

#### 12.8 查询费用
- **接口**:
  - `GET /patient/{patientId}` - 按患者查询
  - `GET /search?keyword={keyword}` - 关键词搜索
  - `GET /status/{status}` - 按状态查询
  - `GET /refundable` - 获取可退费账单
- **权限**: 需要登录

### 13. 病历管理模块 (Records)

**基础路径**: `/api/v1/records`

#### 13.1 获取病历列表
- **接口**: `GET /`
- **描述**: 获取病历列表
- **权限**: 需要登录

#### 13.2 按患者获取病历
- **接口**: `GET /patient/{patientId}`
- **描述**: 获取指定患者的病历列表
- **权限**: 需要登录

#### 13.3 获取病历详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取病历详情
- **权限**: 需要登录

#### 13.4 新增病历
- **接口**: `POST /`
- **描述**: 创建新的病历记录
- **权限**: 需要登录

#### 13.5 编辑病历
- **接口**: `PUT /`
- **描述**: 更新病历信息
- **权限**: 需要登录

#### 13.6 删除病历
- **接口**: `DELETE /{id}`
- **描述**: 删除病历记录
- **权限**: 需要登录

#### 13.7 病历共享
- **接口**: 
  - `POST /share/{id}` - 分享病历给其他医生
  - `POST /unshare/{id}` - 撤销病历分享
  - `GET /shared/{doctorId}` - 获取共享给指定医生的病历
- **权限**: 需要登录

### 14. 治疗室管理模块 (TreatmentRoom)

**基础路径**: `/api/v1/treatmentroom`

#### 14.1 获取治疗室列表
- **接口**: `GET /`
- **描述**: 获取治疗室单列表
- **权限**: 需要登录

#### 14.2 获取治疗室详情
- **接口**: `GET /{id}`
- **描述**: 根据ID获取治疗室详情
- **权限**: 需要登录

#### 14.3 新增治疗室单
- **接口**: `POST /`
- **描述**: 创建新的治疗室单
- **权限**: 需要登录

#### 14.4 编辑治疗室单
- **接口**: `PUT /`
- **描述**: 更新治疗室单信息
- **权限**: 需要登录

#### 14.5 删除治疗室单
- **接口**: `DELETE /{id}`
- **描述**: 删除治疗室单
- **权限**: 需要登录

#### 14.6 按状态查询
- **接口**: `GET /status/{status}`
- **描述**: 按状态获取治疗室单列表
- **权限**: 需要登录

### 15. 数据同步模块 (Sync)

**基础路径**: `/api/v1/sync`

#### 15.1 同步日志管理
- **接口**:
  - `GET /logs` - 获取所有同步日志
  - `GET /logs/last` - 获取最近一次同步信息
  - `GET /logs/paged` - 分页查询同步日志
  - `POST /logs` - 新增同步日志
  - `DELETE /logs/{id}` - 删除同步日志
- **权限**: 需要登录

#### 15.2 同步操作
- **接口**:
  - `GET /connection-status` - 检测中心数据库连接状态
  - `POST /manual-sync` - 手动触发同步
  - `GET /mode` - 获取当前同步模式
  - `POST /mode` - 设置同步模式
- **权限**: 需要登录

#### 15.3 同步任务管理
- **接口**:
  - `GET /tasks` - 获取同步任务列表
  - `GET /tasks/{id}` - 获取同步任务详情
  - `POST /tasks` - 新增同步任务
  - `PUT /tasks` - 更新同步任务
  - `DELETE /tasks/{id}` - 删除同步任务
- **权限**: 需要登录

### 16. 系统配置模块 (UnifiedConfig)

**基础路径**: `/api/unifiedconfig`

#### 16.1 全局设置管理
- **接口**:
  - `GET /global-settings` - 获取全局设置
  - `PUT /global-settings` - 更新全局设置 (仅管理员)
- **权限**: 需要登录

#### 16.2 系统设置管理
- **接口**:
  - `GET /settings/{key}` - 获取指定设置值
  - `POST /settings` - 设置配置值 (仅管理员)
  - `POST /settings/batch` - 批量设置配置 (仅管理员)
  - `GET /settings` - 分页查询设置
  - `GET /settings/group/{group}` - 按分组获取设置
  - `DELETE /settings/{key}` - 删除设置 (仅管理员)
- **权限**: 需要登录

#### 16.3 诊断目录管理
- **接口**:
  - `GET /diagnosis-catalogs` - 获取所有诊断目录
  - `GET /diagnosis-catalogs/paged` - 分页查询诊断目录
  - `GET /diagnosis-catalogs/{id}` - 获取诊断目录详情
  - `POST /diagnosis-catalogs` - 创建诊断目录 (管理员/医生)
  - `PUT /diagnosis-catalogs` - 更新诊断目录 (管理员/医生)
  - `DELETE /diagnosis-catalogs/{id}` - 删除诊断目录 (仅管理员)
- **权限**: 需要登录

#### 16.4 治疗目录管理
- **接口**:
  - `GET /treatment-catalogs` - 获取所有治疗目录
- **权限**: 需要登录

#### 16.5 缓存管理
- **接口**:
  - `POST /cache/refresh-all` - 刷新所有配置缓存 (仅管理员)
  - `POST /cache/refresh-settings` - 刷新设置缓存 (仅管理员)
- **权限**: 需要登录

### 17. 系统日志模块 (UnifiedLogs)

**基础路径**: `/api/unifiedlogs`

#### 17.1 日志查询
- **接口**:
  - `POST /query` - 分页查询日志
  - `GET /{id}` - 根据ID获取日志详情
- **权限**: 需要登录

#### 17.2 日志创建
- **接口**:
  - `POST /` - 创建操作日志
  - `POST /batch` - 批量创建日志
- **权限**: 需要登录

#### 17.3 日志管理
- **接口**:
  - `DELETE /expired` - 删除过期日志 (仅管理员)
- **权限**: 需要登录

#### 17.4 日志统计
- **接口**:
  - `GET /statistics` - 获取日志统计信息
  - `GET /user-statistics/{userId}` - 获取用户操作统计
- **权限**: 需要登录

#### 17.5 日志导出
- **接口**:
  - `POST /export/csv` - 导出日志到CSV
  - `POST /export/excel` - 导出日志到Excel
- **权限**: 需要登录

#### 17.6 用户活动日志
- **接口**:
  - `POST /user-login` - 记录用户登录日志
  - `POST /user-logout` - 记录用户登出日志
- **权限**: 需要登录

## 错误码说明

### HTTP 状态码

| 状态码 | 说明 | 场景 |
|--------|------|------|
| 200 | 成功 | 请求成功处理 |
| 400 | 请求错误 | 参数验证失败、业务逻辑错误 |
| 401 | 未授权 | 未登录或token无效 |
| 403 | 禁止访问 | 权限不足 |
| 404 | 未找到 | 资源不存在 |
| 422 | 验证错误 | 模型验证失败 |
| 500 | 服务器错误 | 系统内部错误 |

### 自定义错误码

| 错误码 | 说明 |
|--------|------|
| VALIDATION_ERROR | 数据验证失败 |
| UNAUTHORIZED | 未授权访问 |
| FORBIDDEN | 禁止访问 |
| NOT_FOUND | 资源未找到 |
| INTERNAL_ERROR | 服务器内部错误 |
| BUSINESS_ERROR | 业务逻辑错误 |

## 开发指南

### 1. 环境要求
- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 或 Visual Studio Code

### 2. 启动开发环境
```bash
# 克隆项目
git clone <repository-url>

# 还原依赖
dotnet restore

# 更新数据库
dotnet ef database update --project LYBT.Module.Users --startup-project LYBT.WebAPI

# 启动项目
cd LYBT.WebAPI
dotnet run
```

### 3. 访问地址
- **API 地址**: https://localhost:5001 或 http://localhost:5000
- **Swagger 文档**: https://localhost:5001/swagger

### 4. 测试账号
系统默认创建管理员账号：
- **用户名**: admin
- **密码**: admin123 (首次登录后请修改)

### 5. 配置说明
主要配置位于 `appsettings.json`:
- `ConnectionStrings`: 数据库连接字符串
- `JwtOptions`: JWT配置
- `UserOptions`: 用户默认设置

## 更新日志

### v1.0.0 (2024-01-01)
- 完成核心业务模块开发
- 实现JWT身份验证
- 添加操作日志审计
- 支持数据缓存优化
- 实现软删除策略

---

**文档版本**: v1.0.0  
**更新时间**: 2024-01-01  
**维护团队**: LYBT开发团队