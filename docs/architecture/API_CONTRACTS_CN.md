# API 契约规范

## 目录

1. [概述](#概述)
2. [API设计原则](#api设计原则)
3. [URL路由规范](#url路由规范)
4. [HTTP方法规范](#http方法规范)
5. [请求规范](#请求规范)
6. [响应规范](#响应规范)
7. [认证与授权](#认证与授权)
8. [错误处理](#错误处理)
9. [API版本控制](#api版本控制)
10. [业务模块API](#业务模块api)
11. [测试指南](#测试指南)

## 概述

本文档定义了凌隐宝堂中医诊所管理系统的API契约规范。所有API设计和实现都应遵循本文档中的约定，以确保接口的一致性、可用性和可维护性。

### 基本信息

- **基础URL**: `https://localhost:7001/api/v1`
- **协议**: HTTPS（生产环境必须）
- **数据格式**: JSON
- **字符编码**: UTF-8
- **API版本**: v1.0

## API设计原则

### RESTful原则

1. **面向资源**: URL代表资源，使用名词而非动词
2. **统一接口**: 使用标准HTTP方法表示操作
3. **无状态**: 每个请求包含所有必要信息
4. **分层系统**: 客户端无需知道是否直接连接到服务器
5. **可缓存**: 响应应明确标识是否可缓存

### 命名约定

- **URL路径**: 小写字母，使用连字符分隔单词
- **查询参数**: camelCase（驼峰命名）
- **JSON属性**: camelCase（驼峰命名）
- **控制器名称**: 复数形式（如patients、doctors）

## URL路由规范

### 基础路由模式

```
/api/v{version}/[controller]
```

### 标准路由示例

| 操作 | HTTP方法 | URL模式 | 说明 |
|-----|---------|---------|------|
| 获取列表 | GET | `/api/v1/patients` | 获取患者列表 |
| 获取详情 | GET | `/api/v1/patients/{id}` | 获取指定患者 |
| 创建资源 | POST | `/api/v1/patients` | 创建新患者 |
| 更新资源 | PUT | `/api/v1/patients/{id}` | 更新患者信息 |
| 删除资源 | DELETE | `/api/v1/patients/{id}` | 删除患者（软删除） |

### 业务路由示例

| 操作 | HTTP方法 | URL模式 | 说明 |
|-----|---------|---------|------|
| 分页查询 | POST | `/api/v1/patients/paged` | 分页查询患者 |
| 批量启用 | POST | `/api/v1/patients/batch-enable` | 批量启用患者 |
| 批量禁用 | POST | `/api/v1/patients/batch-disable` | 批量禁用患者 |
| 搜索 | GET | `/api/v1/patients/search` | 搜索患者 |
| 导入 | POST | `/api/v1/patients/import` | 导入患者数据 |
| 导出 | GET | `/api/v1/patients/export` | 导出患者数据 |

## HTTP方法规范

### 方法语义

| 方法 | 语义 | 幂等性 | 安全性 | 请求体 | 响应体 |
|-----|------|--------|--------|--------|--------|
| GET | 获取资源 | 是 | 是 | 否 | 是 |
| POST | 创建资源 | 否 | 否 | 是 | 是 |
| PUT | 完整更新 | 是 | 否 | 是 | 是 |
| PATCH | 部分更新 | 是 | 否 | 是 | 是 |
| DELETE | 删除资源 | 是 | 否 | 否 | 否 |

### 使用规范

1. **GET**: 仅用于获取数据，不应有副作用
2. **POST**: 用于创建新资源或非幂等操作
3. **PUT**: 用于完整资源替换
4. **PATCH**: 用于部分资源更新
5. **DELETE**: 用于删除资源（本系统使用软删除）

## 请求规范

### 请求头

必需的请求头：

```http
Content-Type: application/json
Accept: application/json
Authorization: Bearer {token}
```

可选的请求头：

```http
Accept-Language: zh-CN
X-Request-ID: {uuid}
```

### 请求参数

#### 查询参数

用于过滤、排序、分页：

```
GET /api/v1/patients?pageNumber=1&pageSize=20&searchTerm=张&orderBy=name
```

#### 路径参数

用于标识特定资源：

```
GET /api/v1/patients/{id}
PUT /api/v1/patients/{id}
```

#### 请求体

创建或更新资源时使用JSON格式：

```json
{
  "name": "张三",
  "idNumber": "110101199001011234",
  "phoneNumber": "13800138000",
  "gender": 1,
  "birthDate": "1990-01-01"
}
```

### 分页请求

统一的分页请求格式：

```json
{
  "pageNumber": 1,
  "pageSize": 20,
  "searchTerm": "张",
  "orderBy": "name",
  "isDescending": false,
  "filters": {
    "isActive": true,
    "gender": 1
  }
}
```

## 响应规范

### 成功响应

#### 单个资源

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "张三",
  "phoneNumber": "13800138000",
  "createdAt": "2024-01-01T08:00:00Z"
}
```

#### 资源列表

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "张三"
  },
  {
    "id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
    "name": "李四"
  }
]
```

#### 分页响应

```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "张三"
    }
  ],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 20,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### 操作响应

```json
{
  "message": "操作成功",
  "affectedRows": 5
}
```

### 响应状态码

| 状态码 | 含义 | 使用场景 |
|-------|------|----------|
| 200 | 成功 | 成功获取或更新资源 |
| 201 | 已创建 | 成功创建资源 |
| 204 | 无内容 | 成功删除资源 |
| 400 | 错误请求 | 请求参数错误 |
| 401 | 未授权 | 未认证 |
| 403 | 禁止访问 | 无权限 |
| 404 | 未找到 | 资源不存在 |
| 409 | 冲突 | 资源冲突 |
| 422 | 无法处理 | 业务逻辑错误 |
| 500 | 服务器错误 | 服务器内部错误 |

## 认证与授权

### JWT认证

#### 登录请求

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "sysadmin",
  "password": "Admin@123456",
  "rememberMe": false
}
```

#### 登录响应

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 28800,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "sysadmin",
    "realName": "系统管理员",
    "role": "Admin"
  }
}
```

### 请求认证

所有需要认证的请求必须在请求头中包含JWT Token：

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token配置

- **默认过期时间**: 8小时（480分钟）
- **记住我过期时间**: 30天（43200分钟）
- **时钟偏差**: 5分钟（300秒）

## 错误处理

### 错误响应格式

使用RFC 7807 Problem Details标准：

```json
{
  "type": "https://example.com/probs/out-of-credit",
  "title": "余额不足",
  "status": 403,
  "detail": "您的当前余额为30，但此操作需要50。",
  "instance": "/account/12345/withdraw"
}
```

### 验证错误

```json
{
  "title": "验证失败",
  "status": 400,
  "errors": {
    "name": ["姓名不能为空", "姓名不能超过50个字符"],
    "phoneNumber": ["手机号格式不正确"]
  }
}
```

### 业务错误

```json
{
  "title": "业务错误",
  "status": 422,
  "detail": "该患者已有未完成的挂号"
}
```

## API版本控制

### 版本策略

- **版本位置**: 在URL路径中（/api/v1/...）
- **版本格式**: v{major}
- **向后兼容**: 小版本更新保持向后兼容
- **弃用通知**: API弃用前3个月通知

### 版本协商

支持多种版本传递方式：

1. **URL路径**（推荐）：
   ```
   /api/v1/patients
   ```

2. **查询参数**：
   ```
   /api/patients?api-version=1.0
   ```

3. **请求头**：
   ```
   X-API-Version: 1.0
   ```

## 业务模块API

### 1. 认证模块 (Auth)

#### 登录
```http
POST /api/v1/auth/login
```

请求体：
```json
{
  "username": "string",
  "password": "string",
  "rememberMe": false
}
```

#### 登出
```http
POST /api/v1/auth/logout
```

#### 刷新Token
```http
POST /api/v1/auth/refresh
```

#### 修改密码
```http
POST /api/v1/auth/change-password
```

请求体：
```json
{
  "oldPassword": "string",
  "newPassword": "string"
}
```

### 2. 患者管理 (Patients)

#### 获取患者列表
```http
GET /api/v1/patients?pageNumber=1&pageSize=20
```

#### 获取患者详情
```http
GET /api/v1/patients/{id}
```

#### 创建患者
```http
POST /api/v1/patients
```

请求体：
```json
{
  "name": "张三",
  "idNumber": "110101199001011234",
  "phoneNumber": "13800138000",
  "gender": 1,
  "birthDate": "1990-01-01",
  "address": "北京市朝阳区",
  "emergencyContact": "李四",
  "emergencyPhone": "13900139000"
}
```

#### 更新患者
```http
PUT /api/v1/patients/{id}
```

#### 删除患者
```http
DELETE /api/v1/patients/{id}
```

### 3. 挂号管理 (Registration)

#### 创建挂号
```http
POST /api/v1/registrations
```

请求体：
```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440000",
  "doctorId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "registrationType": 1,
  "appointmentTime": "2024-01-20T09:00:00",
  "remark": "初诊"
}
```

#### 取消挂号
```http
POST /api/v1/registrations/{id}/cancel
```

请求体：
```json
{
  "reason": "患者有急事"
}
```

#### 完成就诊
```http
POST /api/v1/registrations/{id}/complete
```

### 4. 处方管理 (Prescriptions)

#### 创建处方
```http
POST /api/v1/prescriptions
```

请求体：
```json
{
  "patientId": "550e8400-e29b-41d4-a716-446655440000",
  "recordId": "7ba7b810-9dad-11d1-80b4-00c04fd430c8",
  "items": [
    {
      "herbId": "8ba7b810-9dad-11d1-80b4-00c04fd430c8",
      "dosage": 10,
      "unit": "g",
      "usage": "水煎服"
    }
  ],
  "totalDays": 7,
  "dailyTimes": 2,
  "instructions": "饭后服用"
}
```

#### 审核处方
```http
POST /api/v1/prescriptions/{id}/approve
```

请求体：
```json
{
  "approved": true,
  "comment": "审核通过"
}
```

### 5. 药材管理 (Herbs)

#### 获取药材列表
```http
GET /api/v1/herbs?category=1&isActive=true
```

#### 更新库存
```http
PATCH /api/v1/herbs/{id}/stock
```

请求体：
```json
{
  "quantity": 100,
  "operation": "add",
  "reason": "采购入库"
}
```

#### 更新价格
```http
PATCH /api/v1/herbs/{id}/price
```

请求体：
```json
{
  "newPrice": 25.50,
  "effectiveDate": "2024-02-01"
}
```

### 6. 费用结算 (Billing)

#### 生成账单
```http
POST /api/v1/billing/generate
```

请求体：
```json
{
  "registrationId": "550e8400-e29b-41d4-a716-446655440000",
  "prescriptionId": "6ba7b810-9dad-11d1-80b4-00c04fd430c8"
}
```

#### 支付账单
```http
POST /api/v1/billing/{id}/pay
```

请求体：
```json
{
  "paymentMethod": "cash",
  "amount": 280.50,
  "remark": "现金支付"
}
```

#### 退款
```http
POST /api/v1/billing/{id}/refund
```

请求体：
```json
{
  "refundAmount": 100.00,
  "reason": "部分药材缺货"
}
```

## 测试指南

### 使用Swagger UI

1. 访问 `https://localhost:7001/swagger`
2. 点击"Authorize"按钮
3. 输入JWT Token（格式：Bearer {token}）
4. 选择要测试的API端点
5. 填写请求参数
6. 点击"Execute"发送请求

### 使用Postman

#### 环境配置

```json
{
  "baseUrl": "https://localhost:7001/api/v1",
  "token": "{{jwt_token}}"
}
```

#### 认证配置

1. 在Authorization标签中选择"Bearer Token"
2. 设置Token值为 `{{token}}`

#### 测试脚本示例

登录并保存token：

```javascript
// Tests标签
pm.test("状态码为200", function () {
    pm.response.to.have.status(200);
});

pm.test("保存token", function () {
    var jsonData = pm.response.json();
    pm.environment.set("token", jsonData.token);
});
```

### 使用cURL

#### 登录请求

```bash
curl -X POST https://localhost:7001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"Admin@123456"}'
```

#### 带认证的请求

```bash
curl -X GET https://localhost:7001/api/v1/patients \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 使用REST Client (VS Code)

创建 `.http` 文件：

```http
@baseUrl = https://localhost:7001/api/v1
@token = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

### 登录
POST {{baseUrl}}/auth/login
Content-Type: application/json

{
  "username": "sysadmin",
  "password": "Admin@123456"
}

### 获取患者列表
GET {{baseUrl}}/patients
Authorization: Bearer {{token}}
```

## 最佳实践

### 请求设计

1. **幂等性**: PUT、DELETE、PATCH操作应该是幂等的
2. **资源定位**: 使用ID而非其他属性来定位资源
3. **批量操作**: 提供批量接口以减少请求次数
4. **查询优化**: 支持字段过滤以减少数据传输

### 响应设计

1. **最小化**: 只返回必要的数据
2. **一致性**: 同类型资源返回相同字段
3. **时间格式**: 统一使用ISO 8601格式
4. **空值处理**: 明确区分null和空字符串

### 错误处理

1. **详细信息**: 提供足够的错误信息用于调试
2. **错误代码**: 使用一致的错误代码体系
3. **国际化**: 支持多语言错误消息
4. **安全性**: 不暴露敏感的系统信息

### 性能优化

1. **分页**: 大数据集必须分页
2. **缓存**: 合理使用HTTP缓存头
3. **压缩**: 启用GZIP压缩
4. **异步**: 长时间操作使用异步模式

### 安全规范

1. **HTTPS**: 生产环境强制使用HTTPS
2. **认证**: 所有敏感操作需要认证
3. **授权**: 实现细粒度的权限控制
4. **验证**: 严格验证所有输入参数
5. **审计**: 记录所有重要操作

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2024-01-01 | 初始版本 |

## 联系信息

- **API支持**: api-support@lybt.com
- **技术文档**: https://docs.lybt.com/api
- **问题反馈**: https://github.com/lybt/api/issues