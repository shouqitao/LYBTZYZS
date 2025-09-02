# 🔗 后端API总览 (Backend API Overview)

## 📋 API概述

LYBTZYZS后端采用传统三层架构设计，通过ASP.NET Core Web API暴露RESTful接口，为前端WPF客户端提供数据和业务服务。

**架构状态**: ✅ **传统三层架构稳定运行**
**API状态**: ✅ **8个模块API完全可用**
**认证方式**: JWT Bearer Token
**API版本**: v1.0

## 🏗️ 后端架构设计

### 传统三层架构
```
Controller (控制器层)
    ├── Service (业务服务层)  
    └── Repository (数据访问层)
```

### API控制器体系
- **BaseControllerCore**: 核心基础控制器
- **BaseApiController**: 业务API控制器基类
- **BaseSystemController**: 系统管理控制器基类

## 🎯 API模块列表

### 核心业务模块 (8个)

| 模块 | 控制器 | 基础路径 | 主要功能 | 状态 |
|-----|--------|----------|---------|------|
| **Auth** | AuthController | `/api/v1/auth` | 身份认证、令牌管理 | ✅ 运行中 |
| **Users** | UsersController | `/api/v1/users` | 用户管理、角色权限 | ✅ 运行中 |
| **Patients** | PatientsController | `/api/v1/patients` | 患者档案管理 | ✅ 运行中 |
| **MedicalCase** | MedicalCaseController | `/api/v1/medicalcases` | 医疗案例管理 | ✅ 运行中 |
| **Consultation** | ConsultationController | `/api/v1/consultations` | 看诊诊断管理 | ✅ 运行中 |
| **Prescriptions** | PrescriptionsController | `/api/v1/prescriptions` | 处方管理 | ✅ 运行中 |
| **Herbs** | HerbsController | `/api/v1/herbs` | 中药材管理 | ✅ 运行中 |
| **Formula** | FormulaController | `/api/v1/formulas` | 验方管理 | ✅ 运行中 |

### 系统管理模块 (5个)

| 模块 | 控制器 | 基础路径 | 主要功能 | 状态 |
|-----|--------|----------|---------|------|
| **Health** | HealthController | `/api/v1/health` | 系统健康检查 | ✅ 运行中 |
| **Monitoring** | MonitoringController | `/api/v1/monitoring` | 系统监控 | ✅ 运行中 |
| **Cache** | CacheController | `/api/v1/cache` | 缓存管理 | ✅ 运行中 |
| **Security** | SecurityController | `/api/v1/security` | 安全管理 | ✅ 运行中 |
| **Performance** | PerformanceController | `/api/v1/performance` | 性能监控 | ✅ 运行中 |

## 🔧 API标准规范

### RESTful设计原则

#### HTTP动词使用
- **GET**: 查询资源 (幂等)
- **POST**: 创建资源 (非幂等)
- **PUT**: 更新资源 (幂等)
- **DELETE**: 删除资源 (幂等)

#### URL命名规范
- **基础路径**: `/api/v{version}/{resource}`
- **资源命名**: 使用复数形式 (如 `/users`, `/patients`)
- **子资源**: `/api/v1/patients/{id}/consultations`
- **操作**: 使用HTTP动词而非URL路径动词

### 统一响应格式

#### 成功响应 (ApiResponse<T>)
```json
{
  "success": true,
  "message": "操作成功",
  "data": {
    "id": "uuid",
    "name": "资源名称"
  },
  "timestamp": "2025-09-02T10:30:00Z",
  "requestId": "req-123456"
}
```

#### 错误响应 (ProblemDetails)
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "参数验证失败",
  "status": 400,
  "detail": "用户名不能为空",
  "instance": "/api/v1/users",
  "traceId": "trace-123456"
}
```

### 认证授权

#### JWT认证
- **令牌类型**: Bearer Token
- **过期时间**: 8小时 (Remember Me: 30天)
- **Header格式**: `Authorization: Bearer {token}`

#### 权限控制
- **Admin**: 系统管理员权限
- **Doctor**: 医生权限
- **使用方式**: `[Authorize(Roles = "Admin,Doctor")]`

## 📚 API详细文档

### 1. 认证模块API (Auth)

#### POST /api/v1/auth/login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "sysadmin",
  "password": "Admin@123456",
  "rememberMe": true
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "user": {
      "id": "uuid",
      "username": "sysadmin",
      "role": "Admin"
    },
    "expiresAt": "2025-09-02T18:30:00Z"
  }
}
```

#### POST /api/v1/auth/logout
```http
POST /api/v1/auth/logout
Authorization: Bearer {token}
```

#### POST /api/v1/auth/refresh-token
```http
POST /api/v1/auth/refresh-token
Authorization: Bearer {token}
```

### 2. 用户管理API (Users)

#### GET /api/v1/users
```http
GET /api/v1/users?pageIndex=1&pageSize=10&keyword=admin
Authorization: Bearer {token}
```

#### POST /api/v1/users
```http
POST /api/v1/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "username": "doctor1",
  "password": "Password@123",
  "email": "doctor1@clinic.com",
  "role": "Doctor",
  "isActive": true
}
```

#### PUT /api/v1/users/{id}
```http
PUT /api/v1/users/uuid
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "newemail@clinic.com",
  "isActive": false
}
```

#### DELETE /api/v1/users/{id}
```http
DELETE /api/v1/users/uuid
Authorization: Bearer {token}
```

### 3. 患者管理API (Patients)

#### GET /api/v1/patients
```http
GET /api/v1/patients?pageIndex=1&pageSize=10&keyword=张三
Authorization: Bearer {token}
```

#### POST /api/v1/patients
```http
POST /api/v1/patients
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "张三",
  "gender": "Male",
  "dateOfBirth": "1980-01-01",
  "phone": "13800138000",
  "address": "北京市朝阳区xxx"
}
```

### 4. 医疗案例API (MedicalCase)

#### GET /api/v1/medicalcases
```http
GET /api/v1/medicalcases?patientId=uuid&status=InProgress
Authorization: Bearer {token}
```

#### POST /api/v1/medicalcases
```http
POST /api/v1/medicalcases
Authorization: Bearer {token}
Content-Type: application/json

{
  "patientId": "uuid",
  "chiefComplaint": "头痛",
  "symptoms": "头痛3天，伴有恶心"
}
```

### 5. 系统健康检查API (Health)

#### GET /api/v1/health
```http
GET /api/v1/health
```

**响应**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "Database",
      "status": "Healthy",
      "description": "SQL Server连接正常"
    },
    {
      "name": "Cache",
      "status": "Healthy", 
      "description": "内存缓存运行正常"
    }
  ],
  "totalDuration": "00:00:00.123"
}
```

## 🔗 Swagger文档

### 访问地址
- **开发环境**: https://localhost:7001/swagger
- **生产环境**: https://your-domain/swagger

### Swagger特性
- **交互式文档**: 可直接测试API接口
- **认证支持**: 支持JWT Bearer Token认证
- **模型展示**: 完整的请求响应模型展示
- **错误码说明**: 详细的错误响应说明

## 🛠️ 开发工具

### API测试工具
- **Swagger UI**: 官方交互式文档
- **Postman**: API测试集合
- **curl**: 命令行测试
- **前端集成**: WPF客户端通过Refit调用

### 监控工具
- **健康检查**: `/api/v1/health` 端点监控
- **性能监控**: `/api/v1/performance` 端点
- **日志记录**: Serilog结构化日志

## 📝 版本历史

### v1.0.0 (2025-09-02当前版本)
- ✅ **8个业务模块API完全可用**
- ✅ **传统三层架构稳定运行**  
- ✅ **JWT认证体系完善**
- ✅ **统一响应格式标准**
- ✅ **完整的错误处理机制**
- ✅ **RESTful设计规范遵循**

---

**后端API系统** - 为前端提供稳定可靠的数据服务 🔗