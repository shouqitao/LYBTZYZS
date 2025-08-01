# 凌隐宝堂中医诊所管理系统 (LYBT)

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

中医诊所现代化管理系统，采用模块化架构设计，支持患者管理、诊疗记录、药材管理、处方开具等完整的中医诊所业务流程。

## 🏗️ 项目架构

本项目采用标准的企业级多项目多Solution架构：

```
LYBTZYZS/
├── src/                         # 源代码
│   ├── Backend/                 # 后端服务 (.NET 8 Web API)
│   │   ├── Core/               # 核心库
│   │   ├── Modules/            # 业务模块
│   │   └── Services/           # 服务层
│   └── Frontend/               # 前端应用
│       └── Desktop/            # WPF桌面客户端
├── docs/                       # 项目文档
├── scripts/                    # 构建和部署脚本
└── tests/                      # 测试项目
```

## 🚀 快速开始

### 环境要求

- .NET 8.0 SDK
- SQL Server 2019+
- Visual Studio 2022 或 VS Code

### 后端服务

1. **克隆项目**
   
   ```bash
   git clone https://github.com/shouqitao/LYBTZYZS.git
   cd LYBTZYZS
   ```

2. **构建后端**
   
   ```bash
   cd src/Backend
   dotnet build LYBT.Backend.sln
   ```

3. **初始化数据库**
   
   ```bash
   cd Services/LYBT.WebAPI
   dotnet ef database update --project ../../Core/LYBT.Infrastructure
   ```

4. **运行WebAPI**
   
   ```bash
   dotnet run
   ```
   
   API将在 `https://localhost:5001` 启动，Swagger文档可访问根路径。

### 前端客户端

1. **构建前端**
   
   ```bash
   cd src/Frontend
   dotnet build LYBT.Client.sln
   ```

2. **运行桌面客户端**
   
   ```bash
   cd Desktop/Shell
   dotnet run
   ```
   
   **默认登录凭据**: 用户名 `sysadmin`, 密码 `123456`

## 📚 核心功能

### 后端模块

- **认证授权** - JWT身份验证和基于角色的访问控制
- **患者管理** - 患者档案、病历记录管理
- **医生管理** - 医生信息、排班管理
- **挂号就诊** - 预约挂号、排队叫号
- **诊疗治疗** - 诊断记录、治疗方案
- **处方管理** - 中药处方开具和管理
- **药材管理** - 中药材库存和信息管理
- **验方模板** - 常用处方模板管理
- **药房管理** - 药品调配和发放
- **收费结算** - 费用计算和支付管理
- **病历档案** - 电子病历存储和查询
- **治疗室管理** - 治疗设备和房间管理
- **数据同步** - 多端数据同步服务

### 前端模块

- **用户认证** - 登录界面和权限管理
- **系统管理** - 用户、药材、模板管理
- **前台接待** - 挂号、收费等前台业务
- **医生工作** - 诊疗、开方等医生业务
- **收银管理** - 费用结算和支付处理

## 🔧 开发指南

详细的开发文档请查看：

- [架构说明](docs/architecture/)
- [API文档](docs/api/)
- [开发指南](docs/development/)
- [用户手册](docs/user-guide/)

## 📝 数据库

系统使用统一的数据库架构，所有模块共享一个`AppDbContext`。

主要命令：

```bash
# 添加迁移
dotnet ef migrations add MigrationName --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

## 🤝 贡献

欢迎贡献代码！请查看 [贡献指南](docs/development/CONTRIBUTING.md) 了解如何参与项目开发。

## 📄 许可证

本项目采用 MIT 许可证。详情请查看 [LICENSE](LICENSE) 文件。

## 📞 联系方式

如有问题或建议，请提交 [Issue](https://github.com/shouqitao/LYBTZYZS/issues)。

---

**凌隐宝堂中医诊所管理系统** - 让中医诊所管理更简单高效 ✨