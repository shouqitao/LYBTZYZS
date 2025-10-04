# LYBT.WebAPI

> **凌隐宝堂中医诊所管理系统 - Web API 核心服务** 
> 基于ASP.NET Core 8.0的中医诊所管理REST API | 专为小型诊所(<20人)优化 
> **服务状态**: ✅ **生产就绪** | 🎆 **优化完成** | **编译通过**

## 🎯 项目概述

LYBT.WebAPI是系统的核心后端服务，作为统一API网关集成8个业务模块，通过RESTful API对外提供完整的中医诊所管理功能。采用分层架构设计，支持从患者接诊到处方开具的完整诊疗流程，专为小型中医诊所场景优化。

## 📦 项目结构

此项目是后端服务的总入口，其核心结构由各业务模块的`Controller`组成。

```
LYBT.WebAPI/
├── Controllers/         # 托管所有业务模块的API控制器
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── PatientsController.cs
│   └── ... (其他7个模块的控制器)
├── Program.cs           # 应用程序启动、服务注册和中间件配置
└── appsettings.json     # 应用程序配置文件
```

## 🛠 技术栈

- **Web Framework**: ASP.NET Core 8.0 (Minimal API + MVC控制器)
- **数据访问**: Entity Framework Core 8 + SQL Server
- **认证授权**: JWT Bearer Token
- **API文档**: Swashbuckle (Swagger/OpenAPI)
- **日志记录**: Serilog (结构化日志)
- **健康检查**: ASP.NET Core Health Checks

## 🚀 快速开始

### 开发环境启动
```bash
# 1. 克隆项目并进入WebAPI目录
cd src/Server/Services/LYBT.WebAPI

# 2. 配置环境变量 (复制.env.example到.env并配置)
# 在项目根目录执行: copy .env.example .env

# 3. 还原NuGet包依赖
dotnet restore

# 4. 更新数据库到最新迁移
dotnet ef database update --project ../../Core/LYBT.Infrastructure

# 5. 启动API服务 (HTTPS端口7001)
dotnet run --urls "https://localhost:7001;http://localhost:5001"

# 6. 访问Swagger API文档
# 浏览器访问: https://localhost:7001/swagger
```

## 🔌 API 接口

此项目是后端所有RESTful API的提供者，集成了8个业务模块，共计超过90个API端点。所有API都遵循统一的`ApiResponse<T>`返回格式。

**API文档 (Swagger)**: 启动服务后，可通过 `https://localhost:7001/swagger` 访问交互式API文档。

### 关键业务API示例

- **用户认证**: `POST /api/v1/auth/login`
- **患者管理**: `GET /api/v1/patients`
- **诊疗流程**: `POST /api/v1/medicalcases`

---

*（详细的API列表、安全体系、监控、部署等信息请参考本文档后续章节。）*