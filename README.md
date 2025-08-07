# 凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

中医诊所现代化诊疗系统，采用模块化架构设计，支持患者管理、诊疗记录、药材管理、处方开具等完整的中医诊所业务流程。

## 📖 完整文档

详细文档请访问 [文档中心](./docs/README.md)

## 🏗️ 项目架构

本项目采用标准的企业级架构，支持多平台扩展：

```
LYBTZYZS/
├── LYBT.All.sln                # 总解决方案（包含所有项目）
├── src/                        # 源代码
│   ├── Backend/                # 后端服务 (.NET 8 Web API)
│   │   ├── LYBT.Backend.sln    # 后端独立解决方案
│   │   ├── Core/               # 核心库
│   │   ├── Modules/            # 业务模块
│   │   └── Services/           # 服务层
│   ├── Frontend/               # 前端应用
│   │   ├── Desktop/            # WPF桌面客户端
│   │   │   └── LYBT.Desktop.sln # 桌面端独立解决方案
│   │   ├── Mobile/             # 移动端（预留）
│   │   ├── Web/                # Web端（预留）
│   │   └── CrossPlatform/      # 跨平台（预留）
│   └── Shared/                 # 共享项目
├── docs/                       # 完整文档库
├── scripts/                    # 自动化脚本
└── BIN/                        # 统一输出目录
```

## 🚀 快速开始

请参考 [快速开始指南](./docs/development/getting-started.md)

### 开发环境

- Visual Studio 2022 (17.0+)
- .NET 8.0 SDK
- SQL Server 2019+ 或 LocalDB
- Git

### 一键启动

```bash
# 使用开发管理器
scripts\dev-manager.bat

# 或直接启动
scripts\start-dev.bat
```

**默认登录凭据**: 

- 用户名：`sysadmin`
- 密码：`Admin@123456`

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

## 🔧 开发资源

- [架构文档](docs/architecture/) - 系统架构设计
- [开发指南](docs/development/) - 开发环境配置和指南
- [开发规范](docs/standards/) - 编码规范和最佳实践
- [API文档](docs/api/) - 接口文档和测试
- [用户手册](docs/user-guides/) - 各角色使用说明

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

**凌隐宝堂中医诊所诊疗系统** - 让中医诊疗更智能高效 ✨