# API 总览

## 版本策略与路由
- 版本: v1（ASP.NET API Versioning）
- 路由模板: `api/v{version:apiVersion}/[controller]`
- 前端调用: 固定 `/api/v1/*`（大小写不敏感，前端统一小写）

## 模块与控制器 ⭐v4.0对齐架构

### Server端三层架构模块
- **认证模块**: AuthController、UsersController
- **患者管理**: PatientsController
- **病历管理**: MedicalCaseController（聚合根）
- **诊疗管理**: ConsultationController
- **处方管理**: PrescriptionsController
- **药材管理**: HerbsController
- **方剂管理**: FormulasController

### 系统监控
- HealthController（已提供）
- Monitoring/Security/Cache/Performance 控制器规划中

## 统一序列化
- System.Text.Json（前后端一致）；Refit 使用 `SystemTextJsonContentSerializer`

## 文档与调试

### Swagger 访问
- **开发环境**: http://localhost:5001/swagger/index.html
- **生产环境**: https://localhost:7001/swagger/index.html

### OpenAPI 导出
```bash
# 运行 WebAPI 后，导出 v1 OpenAPI 文档
# 开发环境
curl http://localhost:5001/swagger/v1/swagger.json -o docs/api/openapi.v1.json

# 生产环境（需要证书）
curl -k https://localhost:7001/swagger/v1/swagger.json -o docs/api/openapi.v1.json
```

## 认证与授权 ⭐v4.0双轨认证架构

### 双轨认证设计
- **超级管理员**: AdminSecrets表物理隔离，专用登录端点
- **普通用户**: Users表标准认证流程

### JWT Bearer Token
- **获取 Token**: POST `/api/v1/auth/login`
- **超级管理员登录**: POST `/api/v1/auth/admin/login`（隐藏端点）
- **刷新 Token**: POST `/api/v1/auth/refresh`
- **Token 有效期**: AccessToken 2小时，RefreshToken 7天

### 请求头格式
```http
Authorization: Bearer <your-jwt-token>
```

## 核心接口列表

### 认证模块 (Auth) ⭐v4.0双轨认证
- `POST /api/v1/auth/login` - 普通用户登录
- `POST /api/v1/auth/admin/login` - 超级管理员登录（隐藏端点）
- `POST /api/v1/auth/refresh` - 刷新令牌
- `POST /api/v1/auth/logout` - 用户登出

### 用户管理 (Users)
- `GET /api/v1/users` - 获取用户列表
- `GET /api/v1/users/{id}` - 获取用户详情
- `POST /api/v1/users` - 创建用户
- `PUT /api/v1/users/{id}` - 更新用户
- `DELETE /api/v1/users/{id}` - 删除用户

### 患者管理 (Patients)
- `GET /api/v1/patients` - 获取患者列表
- `GET /api/v1/patients/{id}` - 获取患者详情
- `POST /api/v1/patients` - 创建患者
- `PUT /api/v1/patients/{id}` - 更新患者

### 健康检查 (Health)
- `GET /api/v1/health` - 系统健康状态
- `GET /api/v1/health/db` - 数据库连接状态


