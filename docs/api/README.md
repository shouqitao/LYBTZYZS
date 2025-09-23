# API 总览

## 版本策略与路由
- 版本: v1（ASP.NET API Versioning）
- 路由模板: `api/v{version:apiVersion}/[controller]`
- 前端调用: 固定 `/api/v1/*`（大小写不敏感，前端统一小写）

## 模块与控制器
- AuthController、UsersController、PatientsController、MedicalCaseController、ConsultationController、PrescriptionsController、HerbsController、FormulasController
- 系统健康: HealthController（已提供）；Monitoring/Security/Cache/Performance 控制器规划中

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

## 认证与授权

### JWT Bearer Token
- **获取 Token**: POST `/api/v1/auth/login`
- **刷新 Token**: POST `/api/v1/auth/refresh`
- **Token 有效期**: 8小时（Remember Me: 30天）

### 请求头格式
```http
Authorization: Bearer <your-jwt-token>
```

## 核心接口列表

### 认证模块 (Auth)
- `POST /api/v1/auth/login` - 用户登录
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


