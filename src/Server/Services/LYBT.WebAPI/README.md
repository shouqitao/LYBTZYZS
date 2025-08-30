# LYBT.WebAPI

> **凌隐宝堂中医诊所管理系统 - Web API 服务**  
> 基于 ASP.NET Core 8.0 的企业级中医诊所管理 REST API

## 🎯 项目概述

LYBT.WebAPI 是系统的核心后端服务，集成8个业务模块并通过统一的 RESTful API 对外提供服务。采用UltraThink三层架构，支持中医诊所完整诊疗流程。

**🏆 质量状态**: ✅ **零编译警告** | ✅ **生产就绪** | ✅ **A+代码质量**

## 🏗️ 技术架构

- **框架**: ASP.NET Core 8.0 Web API
- **数据访问**: Entity Framework Core 8.0.17 + SQL Server  
- **认证授权**: JWT Bearer Token + RBAC权限控制
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **API文档**: Swagger/OpenAPI 自动生成
- **缓存**: IMemoryCache 智能缓存系统

## 🧱 集成业务模块

| 模块 | 控制器 | 功能描述 |
|------|--------|----------|
| **Auth** | AuthController | JWT认证、登录登出、会话管理 |
| **Users** | UsersController | 用户管理、角色分配、密码管理 |
| **Patients** | PatientsController | 患者档案、病历管理 |
| **MedicalCase** | MedicalCaseController | 医疗案例、诊疗流程管理 |
| **Consultation** | ConsultationController | 中医四诊、辨证论治 |
| **Prescriptions** | PrescriptionsController | 处方管理、智能配伍 |
| **Herbs** | HerbsController | 中药材管理 |
| **Formula** | FormulasController | 验方模板管理 |

## 🚀 快速开始

### 环境要求
- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 或 VS Code

### 启动步骤
```bash
# 1. 还原依赖
dotnet restore

# 2. 更新数据库
dotnet ef database update --project ../../Core/LYBT.Infrastructure

# 3. 启动服务
dotnet run --urls "https://localhost:7001"
```

### 访问API
- **API地址**: https://localhost:7001
- **Swagger文档**: https://localhost:7001/swagger  
- **默认登录**: sysadmin / Admin@123456

## 🔐 认证授权

### JWT配置
- **Token有效期**: 8小时 (Remember Me: 30天)
- **算法**: HS256
- **角色**: Admin, Doctor

### 使用方式
```bash
# 1. 获取Token
POST /api/v1/auth/login
{
  "username": "sysadmin",
  "password": "Admin@123456"
}

# 2. 使用Token
Authorization: Bearer <your-jwt-token>
```

## 📊 健康检查

系统提供8个健康检查端点，监控各项系统指标：

- `/health/database` - 数据库连接状态
- `/health/cache` - 缓存系统状态  
- `/health/memory` - 内存使用情况
- `/health/disk` - 磁盘空间状态

## 🧪 测试

```bash
# 运行单元测试
dotnet test

# 运行API集成测试
cd ../../../tests/api
python api_test_automation.py
```

## ⚙️ 配置说明

### 主要配置文件
- `appsettings.json` - 基础配置
- `appsettings.Development.json` - 开发环境配置
- `appsettings.Production.json` - 生产环境配置

### 关键配置项
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client"
  }
}
```

## 📈 性能优化

- **连接池**: Max=20, Min=2 (适合小型诊所)
- **智能缓存**: 常用数据10分钟内存缓存
- **批量操作**: EF Core ExecuteUpdate优化
- **异步优先**: 全部API使用async/await模式

## 🎯 UltraThink质量标准

### 编译质量保证 ✅
- ✅ **零编译警告**: 符合.NET 8最佳实践
- ✅ **现代化API**: 使用最新Microsoft.Data.SqlClient
- ✅ **异步规范**: 严格遵循C#异步编程模式
- ✅ **平台兼容**: Windows特定代码正确标记

### 代码质量等级: A+
- **CS1998修复**: 移除无效async关键字
- **ASP0019修复**: HTTP头操作使用最佳实践  
- **CS0618修复**: 升级到最新Microsoft包
- **CA1416修复**: 添加平台支持属性标记

---

> 📌 **更多信息**: 参考项目根目录 [CLAUDE.md](../../../../CLAUDE.md) 了解完整开发规范
