# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

凌隐宝堂中医诊所诊疗系统 (LYBTZYZS) - 基于 .NET 8 的企业级中医诊所管理系统，采用 Web API 后端 + WPF 桌面前端架构。

## 常用开发命令

### 快速启动

```bash
# 交互式开发管理器（推荐）
scripts\dev-manager.bat

# 快速启动开发服务器
scripts\start-dev.bat

# 手动启动（开发时通常使用 Visual Studio）
dotnet run --project src/Backend/Services/LYBT.WebAPI
```

### 构建命令

```bash
# 构建解决方案
dotnet build LYBT.Backend.sln    # 后端
dotnet build LYBT.Desktop.sln    # 前端
dotnet build LYBT.All.sln        # 完整方案

# 发布生产版本
scripts\publish-production.bat
```

### 数据库管理

```bash
# 交互式数据库管理器
scripts\database-manager.bat

# 添加迁移 - 必须使用 Infrastructure 项目
dotnet ef migrations add [迁移名称] --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 测试

```bash
# 运行所有测试
dotnet test

# API 自动化测试
cd tests/api
python api_test_automation.py
```

## 高层架构

### 整体技术栈
- **后端**: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0.17, SQL Server
- **前端**: WPF (.NET 8), Prism.DryIoc 9.0.537, Refit
- **认证**: JWT Bearer Token
- **API文档**: Swagger/Swashbuckle 9.0.1

### 项目结构

```
src/
├── Backend/
│   ├── Core/
│   │   ├── LYBT.Infrastructure/     # 统一 AppDbContext，所有迁移在此
│   │   └── LYBT.Models/            # 领域模型
│   ├── Modules/                    # 15个业务模块
│   └── Services/LYBT.WebAPI/       # Web API 入口
├── Frontend/Desktop/               # WPF 客户端
└── Shared/                        # 前后端共享模型
```

### 关键架构特点

1. **统一数据访问**: 所有模块共享 `AppDbContext`（在 Infrastructure 中）
2. **模块化设计**: 每个业务模块独立但共享数据上下文
3. **整洁架构**: 严格分离关注点
4. **API 响应包装**: 所有响应包装在 `ApiResponse<T>` 中
5. **依赖注入**: 构造函数注入模式
6. **异步优先**: 数据库操作使用 async/await

### 业务模块列表

1. **Auth** - 身份认证和授权
2. **Users** - 用户管理  
3. **Patients** - 患者档案
4. **Doctors** - 医生管理
5. **Registration** - 挂号预约
6. **Consultation** - 看诊管理（核心模块，支持中医四诊）
7. **MedicalCase** - 医疗案例（统一管理整个诊疗流程）
8. **Prescriptions** - 处方管理
9. **Herbs** - 中药材管理
10. **FormulaTemplates** - 验方模板
11. **Pharmacy** - 药房管理
12. **Billing** - 收费结算
13. **Records** - 病历档案
14. **Queueing** - 排队叫号（工作流协调器）
15. **TreatmentRoom** - 治疗室管理
16. **Sync** - 数据同步
17. **DiagnosisTreatment** - 诊断治疗（保留兼容）

## 核心工作流

### 诊疗流程（以看诊为主线）

```
挂号(Registration) → 排队(Queueing) → 看诊(Consultation) → 
缴费(Billing) → 药房/理疗室(Pharmacy/TreatmentRoom)
```

- **MedicalCase** 贯穿整个流程，统一管理患者的诊疗案例
- **Queueing** 作为工作流协调器，管理各环节的排队叫号
- **Consultation** 是核心，支持中医四诊（望闻问切）和现代医学检查

## 开发约定

### 必须遵循的规则

1. **数据库迁移**: 只能在 `LYBT.Infrastructure` 项目中添加
2. **数据访问**: 使用统一的 `AppDbContext`
3. **API 响应格式**: 遵循 [API响应标准](docs/API响应标准.md)
   - POST 方法返回 `Ok(createdObject)`
   - PUT/DELETE 方法返回 `Ok(new { message = "xxx" })`
   - 错误响应使用 `ProblemDetails`
4. **对象映射**: 使用 AutoMapper
5. **模块模式**: 新模块遵循现有模块结构（Interfaces/Services/Repositories/Mapping）

### 环境配置

- **数据库**: SQL Server (localhost/LYBTDB)
- **API端口**: https://localhost:7001
- **默认登录**: sysadmin / Admin@123456
- **JWT过期**: 8小时（Remember Me: 30天）
- **默认密码配置**:
  - 普通用户: `ChangeMe123` (UserOptions.DefaultUserPassword)
  - 管理员: `Admin@123456` (SysAdminOptions.DefaultPassword)
  - 详见: [默认密码文档](docs/development/DEFAULT_PASSWORDS.md)

### 开发流程

1. 使用 Visual Studio 手动运行项目（根据 CLAUDE.local.md）
2. API 文档访问: https://localhost:7001/swagger
3. 使用 scripts/ 目录的批处理文件执行常见任务
4. 数据库在首次运行时自动初始化

## 术语说明

- **Pharmacy**: 药房
- **Prescriptions**: 处方
- **FormulaTemplate**: 验方

## 项目特定指令

- 显示和回答都用中文
- 本项目数据库为 SQL Server（不是 LocalDB）
- 开发时手动用 VS 执行运行操作

## 文档管理

- 文档同时保留一份中文版，一份英文版。

## 开发规范文档

- [开发规范](docs/开发规范.md) - 完整的开发规范指南
- [前后端契约规范](docs/前后端契约规范.md) - 前后端接口约定
- [API响应标准](docs/API响应标准.md) - API 响应格式规范

## 脚本管理

- 脚本都用Python脚本