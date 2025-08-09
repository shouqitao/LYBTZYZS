# 凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-253%20passing-brightgreen.svg)](tests/)
[![Coverage](https://img.shields.io/badge/coverage-2.76%25-red.svg)](coverage-report/)

基于 .NET 8 的企业级中医诊所诊疗系统，采用模块化架构设计，支持患者管理、诊疗记录、药材管理、处方开具等完整的中医诊所业务流程。

**项目特点**：
- 🏥 **纯中医系统**：专为中医诊所设计，支持中医四诊、验方管理
- 🧱 **模块化架构**：8个核心模块，清晰职责，易扩展
- ✅ **高质量代码**：253+单元测试，持续集成，UltraThink重构
- 🔒 **企业级安全**：JWT认证，基于角色的权限控制
- ⚡ **Solution级架构标准化**：统一接口，标准化实现

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

### 8个核心模块（架构标准化完成 ✅）

| 模块 | 功能描述 | 状态 |
|-----|---------|------|
| **Auth** | 身份认证和授权、JWT管理 | ✅ 完成 |
| **Users** | 用户管理（包含医生功能） | ✅ 完成 |
| **Patients** | 患者档案管理和基础接待 | ✅ 完成 |
| **Herbs** | 中药材管理（仅处方用药） | ✅ 完成 |
| **Formula** | 验方管理（经典处方模板） | ✅ 完成 |
| **Consultation** | 看诊管理（中医四诊） | ✅ 完成 |
| **MedicalCase** | 医疗案例（诊疗流程聚合根） | ✅ 完成 |
| **Prescriptions** | 处方管理和智能建议 | ✅ 完成 |

**核心诊疗流程**：
```
患者接待(Patients) → 看诊(Consultation) → 开方(Prescriptions)
         ↑                    ↓
      医疗案例(MedicalCase)贯穿全程
```

**模块特色**：
- **Herbs模块**：专注药材信息管理，支持处方开具，不涉及库存
- **Formula模块**：经典验方库+个人验方，可直接应用到处方
- **MedicalCase模块**：整合了病历记录，作为诊疗聚合根
- **Users模块**：合并了医生功能，统一用户管理

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

## 🧪 测试状态（2025-08-08）

### 单元测试完成情况

**Repository层测试**（✅ 完成）：
- 97个测试用例全部通过
- UserRepository: 31个测试
- PatientRepository: 38个测试 
- HerbRepository: 28个测试

**Service层测试**（🚧 进行中）：
- UserService: 68个测试用例（✅ 完成）
- PatientService: 88个测试用例（✅ 完成）
- HerbService: 45个测试用例（🚧 开发中）
- 总计156个Service层测试已完成
- 下一步：AuthService、ConsultationService单元测试

**测试覆盖率**：从2.30%提升至2.76%，目标60%

**持续集成**：GitHub Actions自动化测试，Python脚本构建

### 技术亮点

- ✅ **AutoMapper 15.0.1**：正确配置ILoggerFactory参数
- ✅ **xUnit + FluentAssertions**：清晰易读的测试断言
- ✅ **Moq框架**：完整的依赖Mock配置
- ✅ **InMemory数据库**：快速单元测试执行
- ✅ **Bogus数据生成**：一致的测试数据生成

## 📝 数据库

系统使用统一的数据库架构，所有模块共享一个`AppDbContext`。

**架构特点**：
- 统一数据访问：所有模块共享`AppDbContext`（在Infrastructure中）
- 模块化设计：每个业务模块独立但共享数据上下文
- 整洁架构：严格分离关注点
- 异步优先：数据库操作使用async/await

**主要命令**：

```bash
# 添加迁移 - 必须使用Infrastructure项目
dotnet ef migrations add MigrationName --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 运行所有测试
dotnet test

# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage" --results-directory coverage-report
```

## 🎯 开发路线图

### 当前阶段：测试完善（Q3 2025）

- ✅ Repository层单元测试（97个测试）
- 🚧 Service层单元测试（目标：300+测试）
- ⏳ Controller层单元测试
- ⏳ 集成测试
- ⏳ 代码覆盖率60%+

### 下一阶段：功能完善（Q4 2025）

- ⏳ 缓存机制实现
- ⏳ API版本管理
- ⏳ 性能优化
- ⏳ 部署文档

### 未来规划

- 📱 移动端应用
- 🌐 Web端界面
- ☁️ 云部署支持
- 📊 数据分析模块

## 🤝 贡献

欢迎贡献代码！请查看 [贡献指南](docs/development/CONTRIBUTING.md) 了解如何参与项目开发。

## 📄 许可证

本项目采用 MIT 许可证。详情请查看 [LICENSE](LICENSE) 文件。

## 📞 联系方式

如有问题或建议，请提交 [Issue](https://github.com/shouqitao/LYBTZYZS/issues)。

---

**凌隐宝堂中医诊所诊疗系统** - 让中医诊疗更智能高效 ✨