# API总览文档

**版本**：v5.0 对齐架构版  
**更新时间**：2025-10-15  
**维护团队**：API开发组  

## 🎯 API体系导航

凌隐宝堂中医诊所管理系统提供完整的RESTful API，支持8个核心业务模块，采用双轨认证机制，确保系统安全性和可靠性。

### 📋 API架构概览

| 模块 | 控制器 | 端点数量 | 主要功能 | 认证要求 |
|------|--------|----------|----------|----------|
| **认证模块** | AuthController | 6个 | 登录、注册、令牌管理 | 公开 |
| **用户模块** | UsersController | 8个 | 用户管理、角色权限 | 需认证 |
| **患者模块** | PatientsController | 10个 | 患者信息管理 | 需认证 |
| **医案模块** | MedicalCasesController | 12个 | 医案流程管理 | 需认证 |
| **诊疗模块** | ConsultationsController | 11个 | 诊疗记录管理 | 需认证 |
| **处方模块** | PrescriptionsController | 14个 | 处方管理计算 | 需认证 |
| **药材模块** | HerbsController | 9个 | 药材字典管理 | 需认证 |
| **验方模块** | FormulasController | 11个 | 验方模板管理 | 需认证 |

## 🔐 认证与授权

### 1. 双轨认证机制
系统采用双轨认证机制，支持普通用户认证和超级管理员认证：

#### 普通用户认证
- **认证方式**: 邮箱 + 密码
- **令牌类型**: JWT (JSON Web Token)
- **令牌有效期**: 
  - AccessToken: 2小时
  - RefreshToken: 7天
- **用户来源**: Users表

#### 超级管理员认证
- **认证方式**: 秘密密钥
- **物理隔离**: AdminSecrets表
- **权限范围**: 全系统权限
- **特殊权限**: 绕过常规权限检查

### 2. 令牌获取与使用
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "doctor@lybt.com",
  "password": "password123"
}
```

**成功响应**:
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcy1pcy1hLXJlZnJlc2gtdG9rZW4=",
    "user": {
      "id": 1,
      "email": "doctor@lybt.com",
      "name": "张医生",
      "role": "Doctor",
      "permissions": ["PatientManage", "MedicalCaseManage", "ConsultationManage", "PrescriptionManage"]
    }
  },
  "code": 200,
  "timestamp": "2025-10-15T10:30:00Z"
}
```

### 3. API请求格式
```http
GET /api/patients?pageIndex=1&pageSize=20&keyword=张三
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

### 4. 权限控制
- **基于角色的访问控制 (RBAC)**
- **细粒度权限控制**
- **资源级别权限检查**
- **API端点权限装饰器**

## 📊 API响应格式

### 1. 统一响应结构
所有API响应都遵循统一的格式：

```json
{
  "success": true,
  "message": "操作成功",
  "data": {},
  "code": 200,
  "timestamp": "2025-10-15T10:30:00Z",
  "errors": []
}
```

### 2. 成功响应
```json
{
  "success": true,
  "message": "获取患者列表成功",
  "data": {
    "data": [
      {
        "id": 1,
        "name": "张三",
        "gender": "男",
        "birthDate": "1990-05-15",
        "phone": "138****8000",
        "age": 35,
        "createdAt": "2025-10-15T10:30:00Z"
      }
    ],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "code": 200,
  "timestamp": "2025-10-15T10:30:00Z"
}
```

### 3. 错误响应
```json
{
  "success": false,
  "message": "验证失败",
  "data": null,
  "code": 422,
  "timestamp": "2025-10-15T10:30:00Z",
  "errors": [
    "患者姓名不能为空",
    "手机号格式不正确"
  ]
}
```

### 4. 分页响应
```json
{
  "success": true,
  "message": "获取数据成功",
  "data": {
    "data": [],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 100,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "code": 200,
  "timestamp": "2025-10-15T10:30:00Z"
}
```

## 🔧 核心API端点

### 1. 认证API (/api/auth)

#### 用户登录
```http
POST /api/auth/login
```
**请求体**:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

#### 刷新令牌
```http
POST /api/auth/refresh
```
**请求体**:
```json
{
  "accessToken": "old-access-token",
  "refreshToken": "old-refresh-token"
}
```

#### 用户登出
```http
POST /api/auth/logout
Authorization: Bearer {access-token}
```

#### 获取当前用户信息
```http
GET /api/auth/me
Authorization: Bearer {access-token}
```

#### 修改密码
```http
POST /api/auth/change-password
Authorization: Bearer {access-token}
```
**请求体**:
```json
{
  "currentPassword": "old-password",
  "newPassword": "new-password"
}
```

### 2. 患者管理API (/api/patients)

#### 获取患者列表
```http
GET /api/patients?pageIndex=1&pageSize=20&keyword=张三
Authorization: Bearer {access-token}
```

#### 获取患者详情
```http
GET /api/patients/{id}
Authorization: Bearer {access-token}
```

#### 创建患者
```http
POST /api/patients
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "name": "张三",
  "gender": "男",
  "birthDate": "1990-05-15",
  "phone": "13800138000",
  "address": "北京市朝阳区",
  "idCard": "110101199005153456",
  "medicalHistory": "无特殊病史",
  "allergies": "青霉素过敏",
  "notes": "患者备注"
}
```

#### 更新患者信息
```http
PUT /api/patients/{id}
Authorization: Bearer {access-token}
Content-Type: application/json
```

#### 删除患者
```http
DELETE /api/patients/{id}
Authorization: Bearer {access-token}
```

#### 搜索患者
```http
GET /api/patients/search?keyword=张三
Authorization: Bearer {access-token}
```

#### 检查手机号是否存在
```http
GET /api/patients/check-phone?phone=13800138000&excludeId=1
Authorization: Bearer {access-token}
```

#### 导出患者数据
```http
GET /api/patients/export?keyword=张三
Authorization: Bearer {access-token}
```

#### 导入患者数据
```http
POST /api/patients/import
Authorization: Bearer {access-token}
Content-Type: multipart/form-data
```

### 3. 医案管理API (/api/medical-cases)

#### 获取医案列表
```http
GET /api/medical-cases?pageIndex=1&pageSize=20&patientId=1&status=InProgress
Authorization: Bearer {access-token}
```

#### 创建医案
```http
POST /api/medical-cases
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "patientId": 1,
  "chiefComplaint": "头痛、失眠",
  "presentIllness": "患者头痛伴失眠1月余",
  "pastHistory": "既往体健",
  "familyHistory": "无特殊家族史",
  "personalHistory": "无不良嗜好",
  "diagnosis": "肝阳上亢",
  "syndromeDifferentiation": "肝阳上亢证",
  "treatmentPrinciple": "平肝潜阳，安神定志",
  "prognosis": "预后良好"
}
```

#### 更新医案状态
```http
PUT /api/medical-cases/{id}/status
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "status": "Completed",
  "notes": "治疗完成，症状改善"
}
```

### 4. 诊疗管理API (/api/consultations)

#### 获取诊疗记录
```http
GET /api/consultations?patientId=1&pageIndex=1&pageSize=20
Authorization: Bearer {access-token}
```

#### 创建诊疗记录
```http
POST /api/consultations
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "patientId": 1,
  "medicalCaseId": 1,
  "consultationType": "FollowUp",
  "fourExaminations": {
    "inspection": "面色红润，舌质红，苔薄黄",
    "auscultation": "语音清晰，呼吸平稳",
    "inquiry": "头痛、失眠、口苦",
    "palpation": "脉弦数"
  },
  "diagnosis": "肝阳上亢",
  "prescription": "天麻钩藤饮加减",
  "advice": "清淡饮食，规律作息"
}
```

### 5. 处方管理API (/api/prescriptions)

#### 获取处方列表
```http
GET /api/prescriptions?patientId=1&status=Confirmed&pageIndex=1&pageSize=20
Authorization: Bearer {access-token}
```

#### 创建处方
```http
POST /api/prescriptions
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "patientId": 1,
  "consultationId": 1,
  "prescriptionType": "Herbal",
  "herbs": [
    {
      "herbId": 1,
      "name": "天麻",
      "dosage": 12,
      "unit": "g",
      "price": 2.50
    },
    {
      "herbId": 2,
      "name": "钩藤",
      "dosage": 15,
      "unit": "g",
      "price": 1.80
    }
  ],
  "usage": "每日1剂，水煎分2次服用",
  "days": 7,
  "totalAmount": 30.10,
  "notes": "随症加减"
}
```

#### 计算处方价格
```http
POST /api/prescriptions/calculate
Authorization: Bearer {access-token}
Content-Type: application/json
```

### 6. 药材管理API (/api/herbs)

#### 获取药材列表
```http
GET /api/herbs?pageIndex=1&pageSize=20&keyword=天麻
Authorization: Bearer {access-token}
```

#### 获取药材详情
```http
GET /api/herbs/{id}
Authorization: Bearer {access-token}
```

#### 创建药材
```http
POST /api/herbs
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "name": "天麻",
  "pinyin": "tian ma",
  "category": "平肝息风药",
  "properties": "甘、平",
  "meridians": "肝经",
  "functions": "平肝息风，定惊止痉",
  "indications": "头痛眩晕，惊厥抽搐",
  "usageDosage": "3-10g",
  "contraindications": "血虚、阴虚者慎用",
  "price": 2.50,
  "unit": "g",
  "stock": 1000,
  "minStock": 100,
  "supplier": "北京药材公司",
  "notes": "质优药材"
}
```

#### 搜索药材
```http
GET /api/herbs/search?keyword=天麻
Authorization: Bearer {access-token}
```

#### 获取药材库存
```http
GET /api/herbs/{id}/stock
Authorization: Bearer {access-token}
```

#### 更新药材库存
```http
PUT /api/herbs/{id}/stock
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "stock": 1200,
  "operation": "Increase",
  "reason": "新购入库"
}
```

### 7. 验方管理API (/api/formulas)

#### 获取验方列表
```http
GET /api/formulas?pageIndex=1&pageSize=20&category=安神剂
Authorization: Bearer {access-token}
```

#### 创建验方
```http
POST /api/formulas
Authorization: Bearer {access-token}
Content-Type: application/json
```
**请求体**:
```json
{
  "name": "天麻钩藤饮",
  "pinyin": "tian ma gou teng yin",
  "category": "安神剂",
  "source": "中医内科杂病证治新义",
  "composition": [
    {
      "herbId": 1,
      "herbName": "天麻",
      "dosage": 9,
      "unit": "g"
    },
    {
      "herbId": 2,
      "herbName": "钩藤",
      "dosage": 12,
      "unit": "g"
    }
  ],
  "functions": "平肝息风，清热活血，补益肝肾",
  "indications": "肝阳上亢，头痛头晕，失眠多梦",
  "usage": "水煎服，每日1剂，分2次服用",
  "modifications": "血虚甚者加当归、熟地；失眠重者加炒枣仁、远志",
  "efficacyAnalysis": "方中天麻、钩藤平肝息风为主药",
  "clinicalApplication": "现代常用于高血压、神经性头痛等",
  "notes": "孕妇忌用"
}
```

## 🔍 错误处理

### 1. HTTP状态码
- **200 OK**: 请求成功
- **201 Created**: 资源创建成功
- **400 Bad Request**: 请求参数错误
- **401 Unauthorized**: 未授权访问
- **403 Forbidden**: 权限不足
- **404 Not Found**: 资源不存在
- **422 Unprocessable Entity**: 数据验证失败
- **500 Internal Server Error**: 服务器内部错误

### 2. 错误响应格式
```json
{
  "success": false,
  "message": "错误描述",
  "data": null,
  "code": 400,
  "timestamp": "2025-10-15T10:30:00Z",
  "errors": [
    "具体错误信息1",
    "具体错误信息2"
  ]
}
```

### 3. 常见错误码
| 错误码 | 描述 | 解决方案 |
|--------|------|----------|
| 1001 | 用户名或密码错误 | 检查登录凭据 |
| 1002 | 令牌已过期 | 使用刷新令牌获取新令牌 |
| 1003 | 权限不足 | 联系管理员分配权限 |
| 2001 | 患者不存在 | 检查患者ID |
| 2002 | 手机号已存在 | 使用其他手机号 |
| 3001 | 药材库存不足 | 补充药材库存 |
| 3002 | 处方价格计算错误 | 检查药材价格和数量 |

## 🚀 API使用指南

### 1. 环境配置
- **开发环境**: `https://localhost:5001/api`
- **测试环境**: `https://test.lybt.com/api`
- **生产环境**: `https://api.lybt.com/api`

### 2. 请求限制
- **频率限制**: 每分钟100次请求
- **数据限制**: 单次请求最大10MB
- **超时设置**: 30秒超时

### 3. 版本控制
- **当前版本**: v1.0
- **版本策略**: URL路径版本控制
- **向后兼容**: 保持至少两个版本的兼容性

### 4. 数据格式
- **请求格式**: JSON
- **响应格式**: JSON
- **日期格式**: ISO 8601 (YYYY-MM-DDTHH:mm:ssZ)
- **编码格式**: UTF-8

## 📚 SDK与工具

### 1. 官方SDK
- **C# SDK**: LYBT.API.Client
- **JavaScript SDK**: lybt-api-js
- **Python SDK**: lybt-api-py

### 2. 开发工具
- **Swagger UI**: `/swagger`
- **API文档**: `/docs`
- **健康检查**: `/health`
- **系统信息**: `/info`

### 3. 测试工具
- **Postman集合**: 导入API测试集合
- **自动化测试**: 完整的API测试套件
- **性能测试**: 负载和压力测试

## 🔗 相关文档

- **[架构总览](../architecture/README.md)** - 三层对齐架构设计原理
- **[Server端架构](../architecture/server/README.md)** - 服务端三层架构实现
- **[模块文档](../modules/README.md)** - 8个业务模块详细说明
- **[开发指南总览](../development/README.md)** - 开发规范和流程指导

---

**文档维护**：API开发组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核