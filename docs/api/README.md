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
- 本地 Swagger: `https://localhost:7001/swagger/index.html`
- 导出 OpenAPI（离线对照）
  ```bash
  # 运行 WebAPI 后，导出 v1 OpenAPI 文档
  curl -k https://localhost:7001/swagger/v1/swagger.json -o docs/api/openapi.v1.json
  ```

