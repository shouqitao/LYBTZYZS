# 凌隐宝堂中医诊所系统 API 接口规范 v2.0

## 更新说明

**版本**: 2.0  
**更新日期**: 2025年8月7日  
**主要变更**:
1. 统一采用 RESTful 风格
2. 移除重复接口
3. 统一状态切换接口
4. 优化响应格式

## 基础信息

### API 基础路径
```
https://localhost:7001/api
```

### 认证方式
```
Authorization: Bearer {token}
```

### 响应格式

#### 成功响应
```json
{
  "data": { ... },
  "success": true,
  "message": "操作成功"
}
```

#### 错误响应 (ProblemDetails)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "具体错误信息",
  "instance": "/api/users",
  "errors": {
    "fieldName": ["错误信息1", "错误信息2"]
  }
}
```

#### 分页响应
```json
{
  "items": [...],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## 核心模块 API

### 1. 用户管理 (Users)

#### 1.1 获取用户列表
```http
GET /api/users?page=1&pageSize=10&keyword=admin&roleId=xxx&isActive=true
```

**参数说明**:
- `page`: 页码，默认 1
- `pageSize`: 每页数量，默认 10
- `keyword`: 搜索关键词（用户名/姓名/手机号）
- `roleId`: 角色ID筛选
- `isActive`: 状态筛选

**响应示例**:
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "username": "zhangsan",
      "realName": "张三",
      "phoneNumber": "13800138000",
      "email": "zhangsan@example.com",
      "roleId": "xxx",
      "roleName": "医生",
      "isActive": true,
      "createTime": "2025-08-07T10:00:00"
    }
  ],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 10
}
```

#### 1.2 获取单个用户
```http
GET /api/users/{id}
```

#### 1.3 创建用户
```http
POST /api/users
Content-Type: application/json

{
  "username": "lisi",
  "realName": "李四",
  "phoneNumber": "13900139000",
  "email": "lisi@example.com",
  "roleId": "xxx"
}
```

**响应**: 返回创建的用户对象

#### 1.4 更新用户
```http
PUT /api/users/{id}
Content-Type: application/json

{
  "id": "xxx",
  "realName": "李四更新",
  "phoneNumber": "13900139001",
  "email": "lisi_new@example.com",
  "roleId": "xxx"
}
```

#### 1.5 删除用户
```http
DELETE /api/users/{id}
```

**响应**:
```json
{
  "message": "用户删除成功"
}
```

#### 1.6 切换用户状态 🆕
```http
POST /api/users/{id}/toggle-status
```

**响应**: 返回更新后的用户对象

#### 1.7 重置密码
```http
POST /api/users/{id}/reset-password
```

**响应**:
```json
{
  "message": "密码重置成功"
}
```

#### 已移除的接口 ❌
- ~~GET /api/users/paged~~ → 使用 GET /api/users
- ~~POST /api/users/add~~ → 使用 POST /api/users
- ~~PUT /api/users/update~~ → 使用 PUT /api/users/{id}
- ~~GET /api/users/get/{id}~~ → 使用 GET /api/users/{id}
- ~~POST /api/users/{id}/enable~~ → 使用 POST /api/users/{id}/toggle-status
- ~~POST /api/users/{id}/disable~~ → 使用 POST /api/users/{id}/toggle-status

### 2. 患者管理 (Patients)

#### 2.1 获取患者列表
```http
GET /api/patients?page=1&pageSize=10&keyword=张&isActive=true
```

**参数说明**:
- `keyword`: 搜索关键词（姓名/电话/患者编号）
- `isActive`: 状态筛选

#### 2.2 获取单个患者
```http
GET /api/patients/{id}
```

#### 2.3 创建患者
```http
POST /api/patients
Content-Type: application/json

{
  "name": "张三",
  "gender": "男",
  "age": 35,
  "phoneNumber": "13800138000",
  "identityCard": "110101198801011234",
  "address": "北京市朝阳区",
  "emergencyContact": "张四",
  "emergencyPhone": "13900139000"
}
```

#### 2.4 更新患者
```http
PUT /api/patients/{id}
```

#### 2.5 删除患者
```http
DELETE /api/patients/{id}
```

#### 2.6 切换患者状态 🆕
```http
POST /api/patients/{id}/toggle-status
```

#### 已移除的接口 ❌
- ~~GET /api/patients/search~~ → 使用 GET /api/patients
- ~~POST /api/patients/batch-import~~ (未实现)
- ~~POST /api/patients/batch-delete~~ (未实现)
- ~~POST /api/patients/{id}/archive~~ → 使用 toggle-status

### 3. 药材管理 (Herbs)

#### 3.1 获取药材列表
```http
GET /api/herbs?page=1&pageSize=10&keyword=人参&minPrice=10&maxPrice=100&lowStock=true&isActive=true
```

**参数说明**:
- `keyword`: 搜索关键词（药材名/产地）
- `minPrice`: 最低价格
- `maxPrice`: 最高价格
- `lowStock`: 是否只显示低库存
- `isActive`: 状态筛选

#### 3.2 获取单个药材
```http
GET /api/herbs/{id}
```

#### 3.3 创建药材
```http
POST /api/herbs
Content-Type: application/json

{
  "name": "人参",
  "origin": "东北",
  "specification": "10g/包",
  "unit": "克",
  "unitPrice": 50.00,
  "stockQuantity": 100,
  "category": "补益类",
  "description": "补气固脱，健脾益肺"
}
```

#### 3.4 更新药材
```http
PUT /api/herbs/{id}
```

#### 3.5 删除药材（软删除）
```http
DELETE /api/herbs/{id}
```

#### 3.6 切换药材状态 🆕
```http
POST /api/herbs/{id}/toggle-status
```

#### 3.7 更新库存
```http
PUT /api/herbs/{id}/stock
Content-Type: application/json

{
  "quantity": 50,
  "operation": "add"  // add 或 subtract
}
```

#### 已移除的接口 ❌
- ~~GET /api/herbs/paged~~ → 使用 GET /api/herbs
- ~~GET /api/herbs/active~~ → 使用 GET /api/herbs?isActive=true
- ~~POST /api/herbs/add~~ → 使用 POST /api/herbs

### 4. 处方管理 (Prescriptions)

#### 4.1 获取处方列表
```http
GET /api/prescriptions?page=1&pageSize=10&keyword=感冒&startDate=2025-08-01&endDate=2025-08-07&patientId=xxx&doctorId=xxx&status=Completed
```

**参数说明**:
- `keyword`: 搜索关键词（诊断/患者名/医生名）
- `startDate`: 开始日期
- `endDate`: 结束日期
- `patientId`: 患者ID
- `doctorId`: 医生ID
- `status`: 处方状态 (Draft/Completed/Cancelled)

#### 4.2 获取处方详情
```http
GET /api/prescriptions/{id}
```

**响应示例**:
```json
{
  "id": "xxx",
  "patientId": "xxx",
  "patientName": "张三",
  "doctorId": "xxx",
  "doctorName": "李医生",
  "diagnosis": "风寒感冒",
  "items": [
    {
      "herbId": "xxx",
      "herbName": "金银花",
      "quantity": 10,
      "unit": "g",
      "price": 5.00,
      "subtotal": 50.00
    }
  ],
  "totalPrice": 150.00,
  "status": "Completed",
  "remark": "一日三次，饭后服用",
  "createTime": "2025-08-07T10:00:00"
}
```

#### 4.3 创建处方
```http
POST /api/prescriptions
Content-Type: application/json

{
  "patientId": "xxx",
  "doctorId": "xxx",
  "diagnosis": "风寒感冒",
  "items": [
    {
      "herbId": "xxx",
      "quantity": 10,
      "price": 5.00
    }
  ],
  "remark": "一日三次，饭后服用"
}
```

#### 4.4 更新处方
```http
PUT /api/prescriptions/{id}
```

#### 4.5 删除处方
```http
DELETE /api/prescriptions/{id}
```

#### 4.6 完成处方
```http
POST /api/prescriptions/{id}/complete
```

#### 4.7 取消处方
```http
POST /api/prescriptions/{id}/cancel
Content-Type: application/json

{
  "reason": "患者取消"
}
```

#### 已移除的接口 ❌
- ~~GET /api/prescriptions/paged~~ → 使用 GET /api/prescriptions

### 5. 看诊管理 (Consultation)

#### 5.1 获取看诊记录列表
```http
GET /api/consultation?page=1&pageSize=10&doctorId=xxx&patientId=xxx&status=InProgress&date=2025-08-07
```

#### 5.2 获取看诊详情
```http
GET /api/consultation/{id}
```

#### 5.3 开始看诊
```http
POST /api/consultation/start
Content-Type: application/json

{
  "registrationId": "xxx",
  "doctorId": "xxx",
  "patientId": "xxx"
}
```

#### 5.4 更新看诊信息
```http
PUT /api/consultation/{id}
Content-Type: application/json

{
  "chiefComplaint": "头痛发热三天",
  "presentIllness": "三天前受凉后出现头痛...",
  "tcmDiagnosis": {
    "inspection": "面色苍白，舌质淡红",
    "auscultation": "声音低微",
    "inquiry": "畏寒怕冷，食欲不振",
    "palpation": "脉浮紧"
  },
  "westernDiagnosis": "上呼吸道感染",
  "treatmentPlan": "疏风散寒，解表退热"
}
```

#### 5.5 完成看诊
```http
POST /api/consultation/{id}/complete
```

## 通用约定

### HTTP 状态码
- `200 OK`: 成功
- `201 Created`: 创建成功
- `400 Bad Request`: 请求参数错误
- `401 Unauthorized`: 未认证
- `403 Forbidden`: 无权限
- `404 Not Found`: 资源不存在
- `409 Conflict`: 冲突（如重复数据）
- `500 Internal Server Error`: 服务器错误

### 分页参数
- `page`: 页码，从 1 开始，默认 1
- `pageSize`: 每页数量，默认 10，最大 100

### 时间格式
- ISO 8601 格式: `2025-08-07T10:00:00`
- 日期格式: `2025-08-07`

### 软删除策略
所有删除操作均为软删除，不会物理删除数据：
- DELETE 请求实际上是将 `IsDeleted` 设置为 `true`
- 可以通过 `toggle-status` 接口恢复

### 状态切换统一接口
所有模块的启用/禁用操作统一使用 `toggle-status` 接口：
- POST /api/{module}/{id}/toggle-status
- 自动切换 `IsActive` 状态
- 返回更新后的完整对象

## 版本历史

### v2.0 (2025-08-07)
- 统一 RESTful 风格
- 移除重复接口
- 添加 toggle-status 统一接口
- 优化响应格式

### v1.0 (2025-01-01)
- 初始版本