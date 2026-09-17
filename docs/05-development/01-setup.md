# 环境搭建
> 版本: v1.0 | 日期: 2026-08-20

## 获取代码

```bash
git clone https://gitee.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS
git checkout master
```

---

## 必要工具

### 1. .NET SDK

- **版本**: 8.0.400+ (`global.json` 锁定 `8.0.400` + `rollForward: latestPatch`，实际接受 8.0.4xx 补丁版本)
- **下载**: https://dotnet.microsoft.com/download/dotnet/8.0
- **验证**: `dotnet --version` 输出 `8.0.4xx`

### 2. Visual Studio 2022

- **版本**: 17.8+
- **工作负载**:
  - ASP.NET and web development
  - .NET desktop development (含 WPF)
- **推荐扩展**: EditorConfig Language Service

### 3. SQL Server

- **版本**: SQL Server 2019+ 或 SQL Server Express (LocalDB)
- **远程模式必需**: 仅远程模式需要独立 SQL Server
- **本地模式**: 使用 SQL Server LocalDB + 嵌入式 LocalWebAPI (Kestrel)，无需安装独立数据库
- **LocalDB 验证**: `sqllocaldb info` 查看已安装的 LocalDB 实例，`sqllocaldb start MSSQLLocalDB` 启动

### 4. Git

- **版本**: 2.30+
- **配置**: `git config core.autocrlf true` (Windows)

---

## 数据库配置

### 远程模式 (SQL Server)

1. 创建数据库:

```sql
CREATE DATABASE LYBTDB_Dev;
```

2. 配置连接字符串 (`src/Server/Services/LYBT.WebAPI/appsettings.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB_Dev;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

3. 数据库迁移会在应用启动时自动执行 (`DatabaseInitializationService` 调用 `MigrateAsync()`，幂等迁移 + 失败重试；InMemory 测试场景才用 `EnsureCreatedAsync`)。

### 本地模式 (SQL Server LocalDB)

- Desktop 客户端启动时自动启动嵌入式 LocalWebAPI (Kestrel)，使用 LocalDB 作为数据库
- 数据存储位置: `%APPDATA%\LYBT\data\` (LocalDB MDF 文件)
- LocalDB 随 Visual Studio / SQL Server Express 安装，无需额外配置

> ⚠️ **首次运行必须注入默认密码**（2026-09-13 修复，见 13c #135）：
> 嵌入式 LocalWebAPI 首启会执行种子 `IdentitySeedData.SeedRolesAndAdminAsync`（路径：`src/Server/Modules/LYBT.Module.Identity/Services/IdentitySeedData.cs`，建 4 角色 + sysadmin）。
> 仓库内 `Shell/appsettings.json` 的 `DefaultPasswords` 为占位符 `__REPLACE__`，**不满足密码策略**，
> 种子会 fail-fast 并打印可执行错误（`[SEED] 创建用户 sysadmin 失败：…请检查默认密码配置…`）。
> 本地调试请先注入合规密码（≥8 位，含大写/小写/数字/特殊字符）再启动客户端：

```powershell
$env:DefaultPasswords__SysAdminPassword = 'SeedTest@2026!'
$env:DefaultPasswords__NewUserPassword  = 'SeedTest@2026!'
# 然后启动 Desktop（或从该终端 dotnet run）
```

> 未注入时本地模式不可用（登录无账号），属**预期的安全行为**（禁止明文/默认密码回退——K4 加固）；
> 远程 WebAPI 同源种子逻辑，生产由环境变量 `DefaultPasswords__SysAdminPassword` 注入。

### 端口说明

| 端口 | 用途 | 说明 |
|------|------|------|
| 5000 | WebAPI HTTP | 远程模式服务端 |
| 5001 | WebAPI HTTPS | 远程模式服务端 (SSL) |
| 5300 | LocalWebAPI | 嵌入式本地模式 (Desktop 启动) |

---

## 依赖管理

项目使用 **Central Package Management** (`Directory.Packages.props`)。

所有 NuGet 包版本在根目录 `Directory.Packages.props` 统一管理，各 `.csproj` 只声明包名不声明版本。

核心依赖 (版本由 `Directory.Packages.props` 统一管理，以下为当前锁定值):

| 包 | 版本 | 用途 |
|-----|------|------|
| Microsoft.EntityFrameworkCore | 8.0.26 | ORM |
| Prism.DryIoc / Prism.Wpf / Prism.Core | 8.1.97 | WPF MVVM + DI |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM 源生成器 (`[ObservableProperty]` / `[RelayCommand]`) |
| MaterialDesignThemes | 5.3.2 | WPF UI (MDIX) |
| Asp.Versioning.Mvc | 8.1.1 | API 版本控制 |
| Serilog | 4.3.1 | 结构化日志 |
| Riok.Mapperly | 4.3.1 | 编译时对象映射 (非 AutoMapper) |
| FluentValidation | 12.1.1 | 输入验证 |
| ClosedXML | 0.102.3 | Excel 导入导出（Desktop 前端，B-12——服务端不涉及 Excel） |
| Refit | 8.0.0 | HTTP 客户端 (Desktop) |

> 历史：EPPlus 7.7.3 / NPOI 2.8.0（Excel，2026-08-04 C-04 + 2026-08-13 #112 移除——后端改 JSON 契约）、BCrypt.Net-Next 4.1.0（密码哈希，A-27 移除——哈希统一走 Identity PBKDF2）均不再引用。

Desktop-only 依赖:

| 包 | 版本 | 用途 |
|-----|------|------|
| NSubstitute | 5.x | 测试 Mock 框架 (仅 Desktop 测试使用) |

---

## 首次运行

### 服务端

```bash
cd src/Server/Services/LYBT.WebAPI
dotnet run
# 访问 https://localhost:5001/api/v1/health 验证
```

默认账号（启动仅自动 seed **sysadmin**）:

| 用户名 | 密码 | 角色 |
|--------|------|------|
| `sysadmin` | 来自 `DefaultPasswords__SysAdminPassword`（环境变量/配置注入，不固化在文档） | 系统运维 (IsSysAdmin=true) |

> `admin` 等业务账号**不由种子自动创建**，由 sysadmin 登录后手动创建。密码可在 `appsettings.json` > `DefaultPasswords` 或环境变量中配置。

### 客户端

1. Visual Studio 打开 `LYBTZYZS.sln`
2. 设置 `LYBT.Desktop.Shell` 为启动项目
3. F5 运行
4. 默认连接 **本地模式**（`http://localhost:5300`，嵌入式 LocalWebAPI）；远程模式需改为远程 WebAPI 地址（如 `http://localhost:5000`）并先启动服务端

---

## 常见问题

| 问题 | 解决方案 |
|------|----------|
| `dotnet build` 失败: 找不到 SDK | 检查 `global.json` 版本匹配 |
| WPF 项目编译失败 | 确认已安装 ".NET desktop development" 工作负载 |
| 数据库连接失败 | 检查 SQL Server 服务运行状态和连接字符串 |
| Desktop 启动白屏 | 检查 WebAPI 是否运行 (远程模式需要) |
| 测试运行失败 | `dotnet restore` 后重试；Desktop 测试需 Windows |
| 端口被占用 (5000/5001/5300) | `netstat -ano | findstr :5000` 查找占用进程并终止 |
| LocalDB 未运行 | `sqllocaldb start MSSQLLocalDB` 或重启 Visual Studio |
| Desktop 模块加载失败 | 确认所有 Desktop 项目编译成功，检查 `dotnet build` 输出 |
| `EnsureCreatedAsync` 后表为空 | 正常行为: `EnsureCreatedAsync` 跳过 migrations，直接建表 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
| 2026-06-25 | v1.1 | 补充 Git clone URL、默认密码、端口说明、LocalDB 验证、常见问题 |
