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
dotnet restore LYBTZYZS.sln

# 3. 编译
dotnet build LYBTZYZS.sln

# 4. 运行服务端
cd src/Server/Services/LYBT.WebAPI
dotnet run

# 5. 运行 Desktop 客户端 (需要 Visual Studio)
# 打开 LYBTZYZS.sln -> 设置 LYBT.Desktop.Shell 为启动项目 -> F5
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
    LYBT.Tests.Server/          # Server 全量测试 (net8.0, Testing Trophy, 零 mock)
    LYBT.Tests.Desktop/         # Desktop 全量测试 (net8.0-windows, 最小 WPF mock)
    LYBT.Tests.Architecture/    # 架构防护测试 (net8.0, 含 AntiMockRules)
  docs/                         # 项目文档
```

## 运行模式

| 模式 | 数据库 | API | 场景 |
|------|--------|-----|------|
| **远程模式** | SQL Server (HTTP API) | LYBT.WebAPI (IIS/Kestrel) | 生产环境、多终端协作 |
| **本地模式** | SQL Server LocalDB | LocalWebAPI (嵌入式 Kestrel) | 离线诊疗、单机使用 |

架构: WPF → SwitchingApiClient → (Remote WebAPI 或 LocalWebAPI on Kestrel) → Service → DB

切换方式: Desktop 客户端设置页面手动切换。

## 测试

```bash
# 运行全部测试
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"

# 分项目运行
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
dotnet test tests/LYBT.Tests.Architecture/
```

## 文档导航

| 文档 | 内容 |
|------|------|
| [环境搭建](setup.md) | 详细环境配置步骤 |
| [编码规范](code-standards.md) | 命名、模式、规范 |
| [设计模式](patterns.md) | Repository/Service/ViewModel 速查 |
| [测试指南](testing.md) | 测试策略、项目结构、编写规范 |
| [开发工作流](workflow.md) | 日常工作流、分支策略 |
| [性能基线](performance-baseline.md) | 性能指标与基线 |
| [密码安全管理](security-password-management.md) | 密码策略与安全规范 |
| [Postman vs .NET 测试策略](postman-vs-dotnet-testing-strategy.md) | 测试工具对比 |
| [UAT 测试计划](uat-test-plan.md) | 用户验收测试计划 |
| [配置迁移指南](configuration-migration-guide.md) | 配置文件迁移说明 |
| [OpenSpec 追踪指南](openspec-tracking-guide.md) | OpenSpec 变更追踪 |
| [开发标准](standards/) | 编码与测试标准文档 |
| [运维文档](../06-operations/) | 部署、配置、监控 |
| [STD-01 命名规范](standards/STD-01-Naming.md) | 命名标准 |
| [STD-02 异常处理](standards/STD-02-Exception.md) | 异常处理标准 |
| [STD-03 日志规范](standards/STD-03-Logging.md) | 日志标准 |
| [STD-04 兼容代码](standards/STD-04-CompatCode.md) | 兼容代码标记 |
| [STD-05 AAA 测试](standards/STD-05-AAA-Test.md) | 测试编写标准 |

---

## 常见问题

**Q: 编译报错 "TargetFramework 'net8.0-windows' is not supported"**
A: Desktop 相关项目需要 Windows SDK 工作负载。在 Visual Studio Installer 中勾选 ".NET 桌面开发" 工作负载，或使用 `dotnet workload install` 安装。Linux/macOS 下只能编译 Server 项目。

**Q: 不装 SQL Server 能开发吗？**
A: 可以。Desktop 客户端支持本地模式 (SQL Server LocalDB + 嵌入式 LocalWebAPI)，无需独立 SQL Server 实例。Server 端开发需要 SQL Server，集成测试使用真实 SQL Server + Respawn (不是 SQLite InMemory)。

**Q: `dotnet test` 部分项目失败**
A: 常见原因:
1. Desktop 测试项目 (`LYBT.Tests.Desktop`) 需要 `net8.0-windows`，Linux/macOS 无法运行
2. Server 测试需要本地 SQL Server 实例 (Respawn 数据重置)
3. 使用 `--filter` 参数分项目运行可定位问题

**Q: 远程模式和本地模式如何切换？**
A: Desktop 客户端设置页面手动切换。本地模式使用 SQL Server LocalDB + 嵌入式 LocalWebAPI (Kestrel)，与远程模式共享 Service/Repository 层，切换后无需额外配置。

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-02-22 | v1.1 | 新增常见问题 (FAQ) 章节 |
| 2026-03-04 | v1.2 | Testing Trophy 重构: 测试项目 5->3, 更新命令 |

## 开发笔记
