# 凌隐宝堂中医诊所管理系统（LYBTZYZS）

<div align="center">
  
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)
[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/shouqitao/LYBTZYZS)
[![Documentation](https://img.shields.io/badge/Docs-Latest-blue)](docs/)

**面向中医诊所的企业级管理解决方案**

[快速开始](#快速开始) • [架构设计](docs/architecture/system-architecture-design.md) • [开发指南](docs/development/development-guide.md) • [API文档](docs/api/)

</div>

## 📋 项目概览

凌隐宝堂中医诊所管理系统是一个专为中医诊所设计的综合管理平台，采用 .NET 8 + WPF + EF Core 技术栈，提供从患者档案、诊疗记录到处方管理的完整解决方案。

### 核心特性

- 🏥 **患者档案管理** - 完整的患者信息管理，支持Excel批量导入
- 📝 **诊疗工作台** - 四诊合参，中医特色诊疗流程
- 💊 **智能处方系统** - 四种录入方式，支持方剂模板
- 🌿 **药材库管理** - 完整药材字典，拼音码快速检索
- 📊 **数据统计分析** - 经营分析，处方统计
- 🔐 **安全认证** - 双轨认证架构，超级管理员物理隔离，JWT+RefreshToken机制
- 💾 **三级缓存策略** - 客户端/API/数据库分层缓存

## 🏗️ 系统架构

```
┌─────────────────────────────────────────────────┐
│             WPF Desktop Client                   │
│         (Prism.DryIoc + MVVM)                   │
└─────────────────┬───────────────────────────────┘
                  │ HTTPS/REST
┌─────────────────▼───────────────────────────────┐
│          ASP.NET Core Web API                    │
│     (JWT Auth + Service Layer)                   │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│         Entity Framework Core                    │
│    (Repository + Unit of Work)                   │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│           SQL Server Database                    │
└─────────────────────────────────────────────────┘
```

### 技术栈

| 层次 | 技术选型 | 版本 |
|------|----------|------|
| **前端** | WPF + Prism.DryIoc | .NET 8 |
| **后端** | ASP.NET Core Web API | 8.0 |
| **ORM** | Entity Framework Core | 8.0 |
| **数据库** | SQL Server | 2019+ |
| **认证** | JWT + RefreshToken | - |
| **缓存** | MemoryCache | 内置 |
| **日志** | Serilog | 8.0 |
| **测试** | MSTest + Moq + FluentAssertions | 3.0 |

## 📦 项目结构

```
LYBTZYZS/
├── 📁 src/
│   ├── 📁 Server/                     # 服务器端代码
│   │   ├── 📁 Core/                   # 核心层
│   │   │   ├── LYBT.Entities/         # 实体模型（聚合根：MedicalCase）
│   │   │   └── LYBT.Infrastructure/   # 基础设施（DbContext、缓存、安全）
│   │   ├── 📁 Modules/                # 业务模块（8个）
│   │   │   ├── LYBT.Module.Auth/      # 认证授权模块
│   │   │   ├── LYBT.Module.Patients/  # 患者管理模块
│   │   │   ├── LYBT.Module.MedicalCase/# 病历管理模块（聚合根）
│   │   │   ├── LYBT.Module.Consultation/# 诊疗管理模块
│   │   │   ├── LYBT.Module.Prescriptions/# 处方管理模块
│   │   │   ├── LYBT.Module.Herbs/     # 药材管理模块
│   │   │   ├── LYBT.Module.Formula/   # 方剂管理模块
│   │   │   └── LYBT.Module.Users/     # 用户管理模块
│   │   └── 📁 Services/
│   │       └── LYBT.WebAPI/           # Web API服务（统一入口）
│   ├── 📁 Client/                     # 客户端代码
│   │   └── 📁 Desktop/                # WPF桌面客户端（Issue #815 Core_New架构）
│   │       ├── 📁 Core_New/           # 三层基础架构
│   │       │   ├── LYBT.Desktop.Infrastructure/  # 基础设施层（Commands, Events, Interfaces, Themes）
│   │       │   ├── LYBT.Desktop.Models/          # 模型层（ViewModels基类, Mapping, Validation）
│   │       │   └── LYBT.Desktop.Services/        # 服务层（Business, Repositories, Http, Navigation等）
│   │       ├── 📁 Modules/            # 业务模块层（8个模块）
│   │       │   ├── LYBT.Desktop.Auth/            # 认证模块
│   │       │   ├── LYBT.Desktop.Patients/        # 患者管理
│   │       │   ├── LYBT.Desktop.MedicalCase/     # 病历管理
│   │       │   ├── LYBT.Desktop.Consultation/    # 诊疗管理
│   │       │   ├── LYBT.Desktop.Prescriptions/   # 处方管理
│   │       │   ├── LYBT.Desktop.Herbs/           # 药材管理
│   │       │   ├── LYBT.Desktop.Formula/         # 方剂管理
│   │       │   └── LYBT.Desktop.Users/           # 用户管理
│   │       ├── 📁 Workstations/       # 工作台层（聚合层）
│   │       │   ├── LYBT.Desktop.ClinicalWorkstation/  # 诊疗工作台
│   │       │   └── LYBT.Desktop.AdminWorkstation/     # 管理工作台
│   │       └── 📁 Shell/              # 启动层
│   │           └── LYBT.Desktop.Shell/           # 主程序壳、DI注册、启动引导
│   └── 📁 Shared/                     # 共享代码
│       ├── LYBT.Shared.Models/        # DTO和契约模型
│       ├── LYBT.Shared.Interfaces/    # 服务接口定义
│       └── LYBT.Shared.Utilities/     # 工具类库
├── 📁 tests/                          # 测试项目
│   ├── UnitTests/                     # 单元测试
│   └── IntegrationTests/              # 集成测试
├── 📁 docs/                           # 文档
│   ├── architecture/                  # 架构设计文档
│   ├── development/                   # 开发规范文档
│   └── requirements/                  # 需求文档
└── 📁 scripts/                        # 部署脚本
```

## 🚀 快速开始

### 环境要求

- Windows 10/11 或 Windows Server 2019+
- .NET 8.0 SDK 或更高版本
- SQL Server 2019 或更高版本
- Visual Studio 2022 (17.4+) 或 VS Code

### 安装步骤

1. **克隆代码库**
```powershell
git clone https://github.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS
```

2. **还原NuGet包**
```powershell
dotnet restore LYBT.All.sln
```

3. **配置数据库连接**

编辑 `src/Server/Services/LYBT.WebAPI/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

4. **初始化数据库**
```powershell
cd src/Server/Services/LYBT.WebAPI
dotnet ef database update
```

5. **运行项目**

启动Web API（终端1）:
```powershell
cd src/Server/Services/LYBT.WebAPI
dotnet run --launch-profile https
```

启动桌面客户端（终端2）:
```powershell
cd src/Client/Desktop/LYBT.Desktop.Shell
dotnet run
```

### 默认账号

- **管理员**: admin / Admin123@SecurePass!
- **医生**: doctor / Doctor123@SecurePass!

## 🔧 开发指南

### 编译项目

```powershell
# 完整编译
dotnet build LYBT.All.sln -c Release

# 分别编译
dotnet build LYBT.Server.sln -c Release
dotnet build LYBT.Desktop.sln -c Release
```

### 运行测试

```powershell
# 运行所有测试
dotnet test LYBT.All.sln

# 带覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 代码格式化

```powershell
# 格式化代码
dotnet format LYBT.All.sln
```

## 📊 核心功能模块

### 1. 病历管理（MedicalCase - 聚合根）

- 一病历一诊断，一病历至多一处方
- 当天可改，过期锁定业务规则
- 管理员可编辑所有病历

### 2. 诊疗管理（Consultation）

- 四诊合参：望闻问切
- 中医诊断：辨证论治
- 医嘱建议：用药指导

### 3. 处方管理（Prescriptions）

四种录入方式：
- 📝 表格编辑 - 传统表格输入
- ⚡ 快速录入 - 拼音码搜索
- 📋 方剂导入 - 从模板导入
- 📑 历史复制 - 从历史处方复制

### 4. 药材管理（Herbs）

- 完整药材字典（2000+药材）
- 拼音码生成与检索
- 价格实时维护
- 库存预警提醒

### 5. 患者档案（Patients）

- 基础信息管理
- 病历历史查询
- Excel批量导入
- 就诊统计分析

## 🔒 安全特性

### 双轨认证架构
- **超级管理员隔离**: AdminSecrets表物理隔离，用户名配置驱动
- **普通用户认证**: Users表标准认证流程
- **用户名保护**: 保留用户名列表，防止冲突

### 认证机制
- **JWT认证**: AccessToken有效期2小时
- **RefreshToken**: 有效期7天，支持撤销
- **密码加密**: BCrypt哈希算法
- **隐藏端点**: `/api/v1/auth/admin/login` 超级管理员专用

### 权限管控
- **角色权限**: Admin/Doctor角色体系
- **审计日志**: 完整操作记录
- **登录保护**: 速率限制防暴力破解

## 🚫 技术约束

为保持系统简洁高效，本项目**明确禁止**使用以下技术：

- ❌ CQRS/MediatR（过度工程）
- ❌ 微服务架构（单体足够）
- ❌ Redis（MemoryCache足够）
- ❌ 消息队列（同步处理足够）
- ❌ Docker/K8s（传统部署足够）
- ❌ GraphQL（RESTful足够）

## 📈 性能指标

- 并发用户：<10人
- 日处方量：20-100张
- 响应时间：<200ms（缓存命中）
- 数据规模：<10万条记录

## 📚 文档资源

- [系统架构设计](docs/architecture/system-architecture-design.md)
- [功能模块设计](docs/architecture/functional-modules-design.md)
- [技术标准规范](docs/development/technical-standards.md)
- [开发指南](docs/development/development-guide.md)
- [API文档](docs/api/)
- [需求文档](docs/requirements/)

## 🤝 贡献指南

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'feat: 添加某某功能 - Issue #123'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

### 提交规范

```
feat(模块): 功能描述 - Issue #编号
fix(模块): 缺陷修复 - Issue #编号
docs: 文档更新 - Issue #编号
refactor: 代码重构 - Issue #编号
test: 测试相关 - Issue #编号
```

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情

## 👥 团队

- **架构设计**: 技术架构组
- **开发团队**: 1-3人小型团队
- **维护方式**: GitHub Issues驱动

## 📞 联系方式

- **GitHub Issues**: [创建Issue](https://github.com/shouqitao/LYBTZYZS/issues)
- **技术讨论**: 通过Issue进行技术讨论

---

<div align="center">
  
**凌隐宝堂中医诊所管理系统** - 专注中医，服务健康

Copyright © 2025 LYBT. All rights reserved.

</div>