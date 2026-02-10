# 开发指南

> 新开发者上手指南 -- 从零到运行项目

## 前置条件

| 工具 | 版本要求 | 用途 |
|------|----------|------|
| .NET SDK | 8.0.406+ | 编译运行 |
| Visual Studio 2022 | 17.8+ | IDE (含 WPF 工作负载) |
| SQL Server | 2019+ | 远程模式数据库 |
| Git | 2.30+ | 版本控制 |

## 5 分钟快速开始

```bash
# 1. 克隆项目
git clone <repo-url>
cd LYBTZYZS

# 2. 恢复依赖
dotnet restore LYBT.All.sln

# 3. 编译
dotnet build LYBT.All.sln

# 4. 运行服务端
cd src/Server/Services/LYBT.WebAPI
dotnet run

# 5. 运行 Desktop 客户端 (需要 Visual Studio)
# 打开 LYBT.All.sln -> 设置 LYBT.Desktop.Shell 为启动项目 -> F5
```

## 项目结构

```
LYBTZYZS/
  src/
    Client/Desktop/             # WPF Desktop 客户端
      Core/                     # Foundation, Infrastructure, Models
      Modules/                  # 业务模块 (MedicalCase, Patients, Herbs...)
      LYBT.Desktop.Shell/       # 启动项目 (Prism Shell)
    Server/
      Core/                     # Entities, Infrastructure, Shared.Models
      Modules/                  # 业务模块 (Users, Patients, Herbs, Formula, MedicalCase, Sync)
      Services/LYBT.WebAPI/     # ASP.NET Core WebAPI
    Shared/                     # 共享模型和工具
  tests/
    LYBT.Tests.Unit/            # Server 单元测试 (net8.0)
    LYBT.Tests.Desktop.Unit/    # Desktop 单元测试 (net8.0-windows)
    LYBT.Tests.Server.Integration/  # Server 集成测试 (net8.0)
    LYBT.Tests.Desktop.Integration/ # Desktop 集成测试 (net8.0)
    LYBT.Tests.Architecture/    # 架构约束测试 (net8.0)
  docs/                         # 项目文档
```

## 运行模式

| 模式 | 数据库 | API | 场景 |
|------|--------|-----|------|
| **远程模式** | SQL Server (HTTP API) | LYBT.WebAPI | 生产环境、多终端协作 |
| **本地模式** | SQLite (本地文件) | 无 | 离线诊疗、单机使用 |

切换方式: Desktop 客户端设置页面手动切换。

## 测试

```bash
# 运行全部测试
dotnet test LYBT.All.sln --filter "FullyQualifiedName~LYBT.Tests"

# 分项目运行
dotnet test tests/LYBT.Tests.Unit/
dotnet test tests/LYBT.Tests.Desktop.Unit/
dotnet test tests/LYBT.Tests.Architecture/
dotnet test tests/LYBT.Tests.Server.Integration/
dotnet test tests/LYBT.Tests.Desktop.Integration/
```

## 文档导航

| 文档 | 内容 |
|------|------|
| [环境搭建](setup.md) | 详细环境配置步骤 |
| [编码规范](code-standards.md) | 命名、模式、规范 |
| [设计模式](patterns.md) | Repository/Service/ViewModel 速查 |
| [测试指南](testing.md) | 测试策略、项目结构、编写规范 |
| [运维文档](../06-operations/) | 部署、配置、监控 |

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
