# 架构概览

## 分层与解决方案
- Server（后端 Web API + 业务模块）
  - Core: `LYBT.Entities`（实体）、`LYBT.Infrastructure`（数据/配置/安全/仓储）
  - Modules: 8 个业务模块（Auth/Users/Patients/MedicalCase/Consultation/Prescriptions/Herbs/Formula）
  - Services: `LYBT.WebAPI`（统一 API 网关与服务注册）
- Client/Desktop（WPF 客户端）
  - Shell（应用外壳）、Core（基础设施/服务）、Infrastructure（HTTP/Refit/Polly）、Services（业务服务）、Workbenches（工作台）、Modules（8 个业务模块）
- Shared（前后端共享）
  - `LYBT.Shared.Models`（DTO/枚举/异常）
  - `LYBT.Shared.Interfaces`（业务服务接口/Refit API 接口）
  - `LYBT.Shared.Utilities`（通用工具与扩展）

## 关键约定
- API 路由: `/api/v1/*`；控制器采用 `[ApiVersion("1")]` 与 `[Route("api/v{version:apiVersion}/[controller]")]`
- 序列化: 前后端统一 System.Text.Json；Refit 使用 `SystemTextJsonContentSerializer`
- 输出目录: 统一 `BIN/`（`Directory.Build.props`）
- 依赖: `Directory.Packages.props` 集中管理版本

## 端到端调用路径
1. WPF ViewModel → 前端业务服务（Users/Patients...）
2. 通过 Infrastructure 生成的 Refit 客户端调用 API（System.Text.Json）
3. WebAPI 控制器（v1） → 对应业务模块服务（Query/Business）
4. Infrastructure（EF Core/缓存）→ 数据读写
5. 返回统一响应（ApiResponse<T> 或业务结果）

