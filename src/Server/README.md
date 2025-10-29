# Server 入口（后端）

本页作为后端解决方案的入口与导航。详细规范以根 `README.md` 和 docs/ 下专题为准。

## 结构
- Core
 - `LYBT.Entities`：实体与基础模型
 - `LYBT.基础设施（基础设施（Infrastructure））`：数据访问、配置、安全、仓储、Web 基础
- Modules（8 个业务模块）
 - Auth / Users / Patients / MedicalCase / Consultation / Prescriptions / Herbs / Formula
- Services
 - `LYBT.WebAPI`：统一 API 网关，模块注册与对外暴露

## 运行与调试
```bash
# 运行 WebAPI（默认开发环境）
dotnet run --project src/Server/Services/LYBT.WebAPI
# Swagger: https://localhost:7001/swagger/index.html
```

## 模块注册（与文档一致）
- 在 `LYBT.WebAPI` 中通过扩展方法注册模块，例如：
 - `services.AddAuthModule();`
 - `services.AddUsersModuleServices();`
 - `services.AddPatientsModuleServices();`
 - 其余模块：`AddMedicalCaseModule()`、`AddConsultationModule()`、`AddPrescriptionsModule()`、`AddHerbsModule()`、`AddFormulaModule()`

## 路由与版本
- 控制器特性：`[ApiVersion("1")]` + `[Route("api/v{version:apiVersion}/[controller]")]`
- 前端固定 `/api/v1/*` 前缀，与上述约定天然匹配

## 参考
- 架构概览: docs/explanation/architecture/overview.md
- 配置与环境: docs/configuration.md
- 运行手册: docs/runbook.md
- PRD 工作流: 根 README 的“PRD 工作流（CCPM）”小节



## 🎯 项目概述

**LYBT Server 后端解决方案**是凌隐宝堂中医诊所管理系统的核心后端服务，基于ASP.NET Core 8.0构建的微服务架构。提供完整的中医诊所管理功能，支持从患者接诊到处方开具的完整诊疗流程。

**核心职责**：
- 提供RESTful API服务，支持8个核心业务模块（Auth、Users、Patients、MedicalCase、Consultation、Prescriptions、Herbs、Formula）
- 实现JWT身份认证和RBAC权限控制，支持Admin/Doctor双角色体系
- 管理中医诊疗数据（患者档案、医案记录、处方管理、中药材信息等）
- 提供统一的数据访问层和业务逻辑处理，采用Repository + Service模式
- 支持小型诊所（<20人）的高并发访问需求，专为中医诊所场景优化

**架构边界**：
- **上游服务**：为WPF桌面客户端和未来的Web客户端提供统一API服务
- **数据层**：连接SQL Server数据库，通过EF Core管理诊疗业务数据
- **共享协作**：与Shared层紧密协作，确保前后端数据契约的完全一致性

## 📦 项目结构

```
src/Server/
├── Core/                          # 核心基础设施层
│   ├── LYBT.Entities/            # 数据实体模型层
│   │   ├── Users/                # 用户实体（UserModel等）
│   │   ├── Patients/             # 患者实体（PatientModel等）
│   │   ├── Consultation/         # 诊疗实体（ConsultationModel等）
│   │   ├── Prescriptions/        # 处方实体（PrescriptionModel等）
│   │   ├── Herbs/                # 药材实体（HerbModel等）
│   │   ├── Formula/              # 验方实体（FormulaModel等）
│   │   └── Common/               # 公共基类（BaseEntity、审计字段等）
│   └── LYBT.Infrastructure/      # 基础设施实现层
│       ├── Data/                 # 数据访问（AppDbContext、DbContextFactory等）
│       ├── Repositories/         # 通用仓储实现（BaseRepository、OptimizedBaseRepository等）
│       ├── Security/             # 安全服务（JWT、加密、Token存储等）
│       ├── Configuration/        # 配置管理（Options类、ConfigurationService等）
│       ├── Web/                  # Web基础设施（BaseController、错误处理等）
│       └── Migrations/           # EF Core数据库迁移文件
├── Modules/                       # 8个业务模块层
│   ├── LYBT.Module.Auth/         # 身份认证模块（登录、JWT管理、权限验证）
│   ├── LYBT.Module.Users/        # 用户管理模块（用户CRUD、角色管理、密码策略）
│   ├── LYBT.Module.Patients/     # 患者管理模块（患者档案、基础信息、就诊历史）
│   ├── LYBT.Module.MedicalCase/  # 医疗案例模块（诊疗流程管理、状态跟踪）
│   ├── LYBT.Module.Consultation/ # 诊疗模块（中医四诊、辨证论治记录）
│   ├── LYBT.Module.Prescriptions/# 处方管理模块（处方开具、剂量计算、价格预览）
│   ├── LYBT.Module.Herbs/        # 药材管理模块（药材信息、价格维护、拼音检索）
│   └── LYBT.Module.Formula/      # 验方模块（经典方剂、模板管理、方剂分享）
└── Services/                      # 服务层
    └── LYBT.WebAPI/              # 统一API网关服务
        ├── Controllers/          # API控制器（10个控制器，包含业务控制器和系统控制器）
        ├── Extensions/           # 服务注册扩展方法
        ├── Middleware/           # 中间件（认证、异常处理、日志等）
        ├── Configuration/        # 配置文件和选项类
        └── Program.cs            # 应用程序启动入口
```

**各层职责说明**：
- **Core/Entities**: 领域实体定义，包含业务规则和数据结构
- **Core/Infrastructure**: 基础设施实现，提供数据访问、安全、配置等核心服务 
- **Modules**: 业务模块实现，每个模块包含Services、Repositories、Mapping等
- **Services/WebAPI**: 统一API服务，负责请求路由、认证授权、响应格式化

## 🛠 技术栈

### 核心框架
- **.NET 8**: 统一开发平台，支持最新C# 12特性
- **ASP.NET Core 8.0**: Web API框架，支持高性能HTTP服务
- **Entity Framework Core 8.0**: ORM框架，支持Code First和数据库迁移
- **SQL Server**: 关系型数据库，适合结构化诊疗数据存储

### 架构组件 
- **AutoMapper 13.0.1**: 对象映射，实现Entity到DTO的自动转换
- **Serilog 8.0**: 结构化日志记录，支持文件、控制台、数据库多种输出
- **Swashbuckle.AspNetCore 6.9.0**: API文档生成，提供Swagger UI交互界面
- **FluentValidation 12.0.0**: 数据验证框架，支持复杂业务规则验证

### 安全与认证
- **JWT (JSON Web Tokens)**: 无状态认证令牌，支持角色权限控制
- **BCrypt.Net**: 密码哈希算法，确保用户密码安全存储
- **Microsoft.AspNetCore.Authentication.JwtBearer**: JWT认证中间件
- **System.IdentityModel.Tokens.Jwt 8.3.0**: JWT令牌处理库

### 性能与缓存
- **Microsoft.Extensions.Caching.Memory**: 内存缓存，适合小型诊所部署
- **Polly 8.4.1**: 弹性处理库，支持重试、断路器等模式
- **Microsoft.Extensions.Http.Polly**: HTTP客户端弹性策略

### 开发工具
- **StyleCop.Analyzers**: 代码风格检查和规范化
- **coverlet.msbuild**: 代码覆盖率分析工具
- **Microsoft.NET.Test.Sdk**: .NET测试框架支持

## 🚀 快速开始

### 环境要求
- **.NET 8 SDK**: 开发和运行环境
- **SQL Server**: 数据库服务（推荐SQL Server 2019+或LocalDB）
- **Visual Studio 2022**: IDE（推荐，或VS Code + C# Dev Kit）

### 快速启动步骤

1. **克隆和还原依赖**
```bash
# 克隆项目
git clone <repository-url>
cd LYBTZYZS

# 还原所有NuGet包依赖
dotnet restore LYBT.Server.sln
```

2. **数据库配置**
```bash
# 配置数据库连接字符串（appsettings.json或环境变量）
# 默认连接: Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=LYBTDB

# 应用数据库迁移
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI
```

3. **构建和运行**
```bash
# 构建Server解决方案
dotnet build LYBT.Server.sln -c Release --no-restore

# 启动WebAPI服务
dotnet run --project src/Server/Services/LYBT.WebAPI
# 或指定端口: dotnet run --project src/Server/Services/LYBT.WebAPI --urls "https://localhost:7001"

# 访问Swagger文档: https://localhost:7001/swagger
```

4. **测试验证**
```bash
# 运行单元测试
dotnet test LYBT.Server.sln -c Release

# 健康检查
curl https://localhost:7001/health
```

### 默认管理员账户
- **用户名**: sysadmin
- **密码**: LybtAdmin2025@SecurePass!
- **角色**: Admin（系统管理员）

### 开发调试
- **API文档**: https://localhost:7001/swagger/index.html
- **健康检查**: https://localhost:7001/health 
- **缓存状态**: https://localhost:7001/cache/health
- **日志文件**: src/Server/Services/LYBT.WebAPI/logs/

## 🔌 API 接口

### API概览

LYBT Server提供超过90个RESTful API端点，覆盖8个核心业务领域。所有API遵循统一的`ApiResponse<T>`响应格式和版本化路由规则。

**API基础配置**:
- **基础路径**: `/api/v1/`
- **认证方式**: JWT Bearer Token
- **响应格式**: JSON (UTF-8)
- **API版本**: v1 (当前版本)

### 核心业务API

| 控制器 | 路由前缀 | 功能描述 | 主要端点 |
|--------|----------|----------|----------|
| **AuthController** | `/api/v1/auth` | 身份认证与授权 | `POST /login`, `POST /logout`, `POST /refresh-token` |
| **UsersController** | `/api/v1/users` | 用户管理与角色控制 | `GET /`, `POST /`, `PUT /{id}`, `DELETE /{id}` |
| **PatientsController** | `/api/v1/patients` | 患者档案管理 | `GET /search`, `POST /`, `GET /{id}/history` |
| **MedicalCaseController** | `/api/v1/medicalcases` | 医疗案例流程管理 | `POST /start`, `PUT /{id}/status`, `GET /{id}/timeline` |
| **ConsultationController** | `/api/v1/consultations` | 中医诊疗记录管理 | `POST /`, `PUT /{id}`, `GET /{id}/details` |
| **PrescriptionsController** | `/api/v1/prescriptions` | 处方开具与管理 | `POST /`, `GET /{id}/calculate`, `POST /{id}/copy` |
| **HerbsController** | `/api/v1/herbs` | 中药材信息管理 | `GET /search`, `POST /batch`, `GET /{id}/pricing` |
| **FormulasController** | `/api/v1/formulas` | 验方模板管理 | `GET /templates`, `POST /from-prescription`, `GET /{id}/herbs` |

### 系统管理API

| 控制器 | 路由前缀 | 功能描述 |
|--------|----------|----------|
| **HealthController** | `/health` | 系统健康检查 |
| **CacheHealthController** | `/cache/health` | 缓存状态监控 |

### 统一响应格式

```json
{
  "success": true,
  "message": "操作成功",
  "data": { /* 具体业务数据 */ },
  "timestamp": "2025-01-23T10:30:00Z",
  "requestId": "req_123456789"
}
```

### 分页数据格式

```json
{
  "success": true,
  "data": {
    "items": [ /* 数据项数组 */ ],
    "totalCount": 150,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 8,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

### API认证流程

1. **登录认证**: `POST /api/v1/auth/login`
2. **获取Token**: 返回AccessToken和RefreshToken
3. **请求Header**: `Authorization: Bearer {AccessToken}`
4. **Token刷新**: `POST /api/v1/auth/refresh-token`

### 详细API文档

启动服务后访问 **Swagger UI**: https://localhost:7001/swagger/index.html

## 📚 相关文档

### 架构设计
- [架构概览](../../docs/explanation/architecture/overview.md) - 系统整体架构设计说明
- [数据库设计文档](../../docs/database/schema.md) - 数据库表结构和关系设计

### 模块文档
- [模块索引](../../docs/modules/index.md) - 8个业务模块的详细说明
- [LYBT.Entities README](Core/LYBT.Entities/README.md) - 实体模型设计文档
- [LYBT.Infrastructure README](Core/LYBT.Infrastructure/README.md) - 基础设施实现文档
- [LYBT.WebAPI README](Services/LYBT.WebAPI/README.md) - WebAPI服务详细说明

### 业务模块
- [认证模块](Modules/LYBT.Module.Auth/README.md) - JWT认证和权限控制
- [用户管理模块](Modules/LYBT.Module.Users/README.md) - 用户CRUD和角色管理
- [患者管理模块](Modules/LYBT.Module.Patients/README.md) - 患者档案和就诊历史
- [医疗案例模块](Modules/LYBT.Module.MedicalCase/README.md) - 诊疗流程管理
- [诊疗记录模块](Modules/LYBT.Module.Consultation/README.md) - 中医四诊记录
- [处方管理模块](Modules/LYBT.Module.Prescriptions/README.md) - 处方开具和计算
- [药材管理模块](Modules/LYBT.Module.Herbs/README.md) - 中药材信息维护
- [验方管理模块](Modules/LYBT.Module.Formula/README.md) - 方剂模板管理

### 部署运维
- [配置管理文档](../../docs/configuration.md) - 环境配置和参数说明
- [运行手册](../../docs/runbook.md) - 生产环境部署和运维指南
- [安全配置指南](../../docs/security/configuration.md) - JWT、加密、数据保护配置

### 开发指南
- [代码规范](../../docs/development/coding-standards.md) - C#编码规范和最佳实践
- [测试指南](../../docs/development/testing-guide.md) - 单元测试和集成测试规范

### 共享组件
- [Shared层文档](../../src/Shared/README.md) - 前后端共享组件说明
- [DTO设计规范](../../docs/shared-inventory/shared-types.md) - 数据传输对象设计标准
