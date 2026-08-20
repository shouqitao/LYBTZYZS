# 开发指南

> **用户速览**：给开发者看的。从 01-setup 开始，5 分钟跑起来。
>
> **权威文档在哪**：编码规范 → [02-code-standards.md](02-code-standards.md)；测试指南 → [04-testing.md](04-testing.md)（原 10-testing-standards 已合并）。完整查询指南见 [docs/README.md](../README.md#ai-查询指南)。

## 前置条件

| 工具 | 版本 | 用途 |
|------|------|------|
| .NET SDK | 8.0.400+ | 编译运行 |
| Visual Studio 2022 | 17.8+ | IDE（含 WPF 工作负载） |
| SQL Server | 2019+ | 远程模式数据库 |
| Git | 2.30+ | 版本控制 |

## 5 分钟快速开始

```bash
git clone <repo-url> && cd LYBTZYZS
dotnet restore LYBTZYZS.sln
dotnet build LYBTZYZS.sln
cd src/Server/Services/LYBT.WebAPI && dotnet run
```

## 项目结构

```
LYBTZYZS/
  src/
    Client/Desktop/         # WPF 客户端
    Server/
      Core/                 # Entities, Infrastructure
      Modules/              # 业务模块
      Services/LYBT.WebAPI/ # ASP.NET Core
    Shared/                 # 共享模型
  tests/
    LYBT.Tests.Server/      # Server 测试
    LYBT.Tests.Desktop/     # Desktop 测试
    LYBT.Tests.Architecture/# 架构防护测试
```

## 文档索引

| # | 文档 | 一句话说明 |
|---|------|-----------|
| 01 | [环境搭建](01-setup.md) | 详细配置步骤 |
| 02 | [编码规范](02-code-standards.md) | 命名、模式、规范 |
| 03 | [设计模式](03-patterns.md) | Repository/Service/ViewModel 速查 |
| 04 | [测试指南](04-testing.md) | 测试策略、编写规范 |
| 05 | [密码安全](05-security-password-management.md) | 密码策略 |
| 06 | [配置迁移](06-configuration-migration-guide.md) | 配置文件迁移 |
| 07 | [性能基线](07-performance-baseline.md) | 性能指标 |
| 08 | [UAT 计划](08-uat-test-plan.md) | 用户验收测试 |
| ~~09~~ | ~~Postman vs .NET~~ | ~~已归档至 compose/archive/~~ |
| 13 | [迁移策略](13-migration-strategy.md) | EF Core 迁移与回滚 |

### 子目录

| 目录 | 内容 |
|------|------|
| [standards/](standards/README.md) | 开发标准（STD-01~06） |

## 常见问题

**Q: 编译报错 "net8.0-windows is not supported"**
A: 需要 Windows SDK 工作负载。Visual Studio Installer 勾选「.NET 桌面开发」。

**Q: 不装 SQL Server 能开发吗？**
A: Desktop 支持本地模式（LocalDB + 嵌入式 LocalWebAPI），无需独立 SQL Server。

**Q: 远程和本地怎么切换？**
A: Desktop 设置页面手动切换。本地用 LocalDB，远程用 SQL Server。
