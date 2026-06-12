# 开发者 Onboarding 指南

> 从 clone 到第一个 PR，预计 30-45 分钟。

---

## 1. 环境准备 (10 min)

### 1.1 必要工具

| 工具 | 版本 | 验证 |
|------|------|------|
| .NET SDK | 8.0.406+ (`global.json` 锁定) | `dotnet --version` |
| Visual Studio 2022 | 17.8+ | — |
| SQL Server | 2019+ 或 Express LocalDB | — |
| Git | 2.30+ | `git --version` |

**VS 工作负载**：ASP.NET and web development + .NET desktop development (含 WPF)

### 1.2 Clone & Restore

```bash
git clone <repo-url> LYBTZYZS
cd LYBTZYZS
dotnet restore LYBTZYZS.sln
```

> 包版本由 `Directory.Packages.props` 统一管理，各 `.csproj` 不声明版本号。

---

## 2. 理解项目 (5 min)

### 2.1 系统架构

```
Desktop (WPF + Prism MVVM)
    │ HTTP/REST (嵌入式 Kestrel)
WebAPI (ASP.NET Core 8)
    │ EF Core 8
SQL Server / LocalDB
```

- **三层架构**: Controller → Service → Repository → DbContext
- **DDD**: MedicalCase 是唯一聚合根，Consultation + Prescription 为内部实体
- **双模式**: 远程模式 (SQL Server) + 本地模式 (LocalDB)

### 2.2 核心术语

| 术语 | 含义 | 不是 |
|------|------|------|
| Consultation | 中医诊断 | "问诊"/"就诊" |
| MedicalCase | 医案 | "病历" |
| Formula | 验方/经验方 | "公式" |

### 2.3 项目结构

```
LYBTZYZS/
├── src/
│   ├── Server/                    # 后端
│   │   ├── Core/LYBT.Entities/    # 实体
│   │   ├── Core/LYBT.Infrastructure/  # DbContext, Repositories
│   │   ├── Modules/LYBT.Module.*/     # 7个业务模块
│   │   └── Services/LYBT.WebAPI/      # API入口
│   ├── Client/Desktop/            # 桌面客户端
│   │   ├── Shell/                 # App入口, Prism启动
│   │   ├── Modules/LYBT.Desktop.*/  # 8个桌面模块
│   │   └── Roles/                 # 3个角色工作区
│   └── Shared/                    # 共享库
│       ├── LYBT.Shared.Models/   # DTOs/Contracts
│       ├── LYBT.Shared.ExceptionHandling/
│       └── LYBT.Shared.Logging/
├── tests/
│   ├── LYBT.Tests.Server/        # 1185 tests (真实SQL Server)
│   ├── LYBT.Tests.Desktop/       # 760 tests (SQLite InMemory)
│   └── LYBT.Tests.Architecture/  # 76 tests (架构守卫)
├── docs/                          # 文档
└── scripts/                       # 构建/运维脚本
```

---

## 3. 构建与运行 (5 min)

### 3.1 构建

```bash
dotnet build LYBTZYZS.sln
```

### 3.2 数据库

**远程模式** — 创建数据库，配置连接字符串：

```sql
CREATE DATABASE LYBTDB;
```

```json
// src/Server/Services/LYBT.WebAPI/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

> 迁移在应用启动时自动执行 (`EnsureCreatedInDevelopment: true`)

**本地模式** — 无需配置，Desktop 自动创建 `%APPDATA%\LYBT\data\lybt-local.db`

### 3.3 启动服务端

```bash
dotnet run --project src/Server/Services/LYBT.WebAPI
# 验证: https://localhost:5001/api/v1/health
```

### 3.4 启动桌面客户端

1. VS 打开 `LYBTZYZS.sln`
2. 设置 `LYBT.Desktop.Shell` 为启动项目
3. F5 运行
4. 默认管理员: `sysadmin` / 密码见 `appsettings.json > DefaultPasswords`

---

## 4. 运行测试 (5 min)

```bash
dotnet test tests/LYBT.Tests.Server/        # ~1185 tests
dotnet test tests/LYBT.Tests.Desktop/       # ~760 tests (需Windows)
dotnet test tests/LYBT.Tests.Architecture/  # ~76 tests
```

> **测试策略**: 集成优先，零Mock。Server测试用真实SQL Server + Respawn清理。

详见 → [`07-concepts/testing-strategy.md`](07-concepts/testing-strategy.md)

---

## 5. 代码规范速查 (5 min)

### 5.1 架构规则

| 规则 | 说明 |
|------|------|
| 跨模块禁止 | Server模块间 / Desktop模块间禁止直接引用 |
| Service禁止注入DbContext | 必须通过 Repository 接口 |
| 软删除 | `IsDeleted` 全局过滤器，查询软删记录用 `IgnoreQueryFilters()` |
| API响应统一 | 所有Controller返回 `ApiResponse<T>` |

### 5.2 编码风格

- 语言版本: C# 12 (`Directory.Build.props`)
- Nullable: enabled
- 命名: 遵循 `.editorconfig` 规则
- 分析器: 项目内置 Roslyn Analyzer

### 5.3 常见陷阱

| 陷阱 | 解决方案 |
|------|---------|
| `FindAsync` 查不到软删记录 | 用 `IgnoreQueryFilters()` |
| Desktop测试在非Windows失败 | 需要 `net8.0-windows` 目标框架 |
| `HasPrescription` 不一致 | Mapper必须显式设置，不依赖计算属性 |
| Edit工具相同字符串报错 | 确保 oldString ≠ newString |

详见 → [`07-concepts/development/common-pitfalls.md`](07-concepts/development/common-pitfalls.md)

---

## 6. 第一个PR (10 min)

### 6.1 开发流程

```
创建分支 → 改代码 → build → test → commit → push → PR
```

### 6.2 示例：修改一个API端点

```bash
# 1. 创建分支
git checkout -b fix/my-first-change

# 2. 修改代码 (示例：调整分页默认值)
#    编辑 src/Server/Modules/LYBT.Module.Patient/Services/PatientService.cs

# 3. 构建
dotnet build LYBTZYZS.sln

# 4. 运行相关测试
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~Patient"

# 5. 提交
git add src/Server/Modules/LYBT.Module.Patient/
git commit -m "fix: adjust patient pagination default page size"

# 6. 推送并创建PR
git push -u origin fix/my-first-change
gh pr create --title "fix: adjust patient pagination default" --body "描述改动"
```

---

## 7. 导航地图

完成 Onboarding 后，按需深入阅读：

| 想了解 | 去哪里 |
|--------|--------|
| 产品功能全貌 | [`01-product/`](01-product/) |
| 需求规格 (PRD) | [`02-requirements/`](02-requirements/) |
| 架构设计与ADR | [`03-architecture/`](03-architecture/) |
| API端点文档 | [`04-api-reference/`](04-api-reference/) |
| 编码标准与流程 | [`05-development/`](05-development/) |
| 部署与运维 | [`06-operations/`](06-operations/) |
| 技术概念索引 | [`07-concepts/`](07-concepts/) |
| 开发者总入口 | [`DEVELOPER-GUIDE.md`](DEVELOPER-GUIDE.md) |

---

## 变更记录
| 日期 | 变更 |
|------|------|
| 2026-06-12 | 初始版本 |
