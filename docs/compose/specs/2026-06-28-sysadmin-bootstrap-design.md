# Sysadmin 开局方案（专家建议）

> **角色**：AI 开发专家给出技术方案与推荐，用户在业务结果上确认。
> **范围**：sysadmin 主导的「部署 → 首次初始化 → 持续升级」全链路。
> **依据**：Velopack 官方文档（context7 查证）+ 本项目特性（双模式/角色级联/诊所配置/审计发现）。
> **日期**：2026-06-28

---

## [S1] 核心结论（先看这个）

「开局」是**三个阶段**，不是一个动作。每个阶段我有明确技术推荐：

| 阶段 | 我的推荐（技术由我定） | 关键理由 |
|------|----------------------|---------|
| **① 部署** | Desktop 用 **Velopack** 打包成单文件 `Setup.exe`（自带 .NET 运行时，免管理员权限，装到用户目录）；WebAPI 单独服务器部署（带 SQL Server） | Velopack 是 Squirrel 维护模式的现代继任者，专为 .NET 桌面自动更新设计；免 admin 权限对小诊所运维友好 |
| **② 首次初始化** | sysadmin 首登强制走「初始化向导」5 步，**不完成则不能用系统** | 业界标准（参考各类 SaaS/桌面应用 onboarding）；防止"默认密码裸奔" |
| **③ 持续升级** | Desktop 用 Velopack 自动更新（启动检查→提示→一键升级重启）；WebAPI 手动升级+EF 迁移（带文档） | Desktop 自动更新成熟可靠；WebAPI 涉及数据库迁移，自动升级风险高，半自动更稳 |

---

## [S2] 阶段 ① 部署（详细方案）

### 部署拓扑——这是**业务问题，需要你确认**

本项目有两种部署形态，决定方案差异很大：

| 形态 | 描述 | 适合 |
|------|------|------|
| **A. 单机本地模式** | 一台机器跑 Desktop（内嵌 LocalWebAPI + LocalDB），不设独立服务器 | 单医生小诊所、义诊笔记本 |
| **B. 服务器+客户端** | 一台服务器跑 WebAPI + SQL Server；N 台工作站跑 Desktop 连远程 | 多医生多终端诊所（NFR：1-3 医生、1-5 终端） |

**当前代码两种都支持**（dual-mode 架构已就绪）。但部署方案要对准主要形态。

### Desktop 部署（两种形态都要）——Velopack

```
开发者：vpk pack → 产出 Setup.exe（自包含 .NET 运行时，~150MB）
用户：双击 Setup.exe → 装到 %LocalAppData%\LYBT（无需管理员）→ 桌面快捷方式
```

**为什么选 Velopack 而非 Squirrel/AutoUpdater.NET/MSIX**：
- **Squirrel.Windows**：已进入维护模式，作者推荐迁移到 Velopack
- **AutoUpdater.NET**：只做更新不做安装打包，需另配安装器
- **MSIX**：需签名证书+Microsoft Store 倾向，对小诊所过重
- **Velopack**：安装+更新一体；增量更新（delta）省带宽；支持静默安装 `Setup.exe --silent`；跨平台；活跃维护

### WebAPI 部署（仅形态 B）——服务器侧

WebAPI 是 ASP.NET Core 服务，**不适合 Velopack**（那是桌面工具）。推荐：
- 发布为 **自包含 EXE + systemd/Windows Service** 或 IIS 托管
- 配 **SQL Server**（远程库 `LYBTDB_Dev`）
- 提供一份《服务器部署手册》+ PowerShell 部署脚本

**我的建议**：服务器部署保持"文档+脚本"半自动，不上自动更新（数据库迁移风险）。

---

## [S3] 阶段 ② 首次初始化向导（核心）

### 触发条件

Desktop 首次启动（检测到 `IsFirstRun=true`，或 sysadmin `LastLoginAt==null`）→ **强制进入向导，无法跳过**。

### 向导 5 步（强制顺序）

```
Step 1: 改密          sysadmin 默认密码登录后立即强制改密（ForceChangeOnFirstLogin=true）
                     ↓
Step 2: 诊所信息       填名称/科室/地址/电话/许可证号 → 写入 ClinicSettings
                     （这些驱动处方打印标题区，必须有）
                     ↓
Step 3: 模式选择       形态A: 默认本地模式（跳过）
                     形态B: 填远程 WebAPI 地址 → 测试连通 → 保存
                     ↓
Step 4: 创建首个 admin sysadmin 设定 admin 用户名 + 临时密码（或系统生成强密码提示）
                     → 创建 admin 账号（IsSysAdmin=false, Role=Admin）
                     ↓
Step 5: 完成交权       提示"请以 admin 登录继续配置用户/药材" → 注销 sysadmin
```

### 关键设计原则

1. **不可跳过**：向导未完成，sysadmin 无法进入主界面（防止裸奔）
2. **种子只建 sysadmin**：`IdentitySeedData` 改为**只种子 sysadmin**（现状多种子了 admin，要删）——符合你的级联框架
3. **默认密码不硬编码**：安装时生成随机强初始密码，**显示一次**让 sysadmin 记下，首次登录即改（消除当前 `SysAdmin@2026!` 全代码硬编码的安全风险——审计 S8 发现）
4. **ClinicSettings 进向导**：不再藏在 appsettings，由向导写入（驱动打印标题，必须有）
5. **JWT 密钥自动生成**：首次启动生成随机密钥写回配置（当前 `appsettings.json:22` 明文硬编码 JWT Secret，安全风险）

### 与现状的差异（要改的）

| 现状 | 改为 |
|------|------|
| `IdentitySeedData` 种子 sysadmin+admin | 只种子 sysadmin |
| `ForceChangeOnFirstLogin: false` | `true` |
| 默认密码 `SysAdmin@2026!` 全代码硬编码 | 安装时随机生成，首登强制改 |
| `FirstRunSetup` 只配连接 | 扩为 5 步向导（改密/诊所/模式/建admin/交权） |
| JWT Secret 明文在 appsettings | 首次生成随机密钥 |
| `SeedTool` CLI（PRD 提但不存在） | 用 sysadmin 内的"密码重置"UI 替代，或保留 CLI 作为灾难恢复 |

---

## [S4] 阶段 ③ 持续升级

### Desktop 自动更新（Velopack）

```
Desktop 启动 → UpdateManager.CheckForUpdates() → 有新版？
   ├─ 是 → 提示用户"发现新版本 vX.X，是否升级？" 
   │       → 用户同意 → 后台下载（增量 delta）→ 提示重启
   │       → 用户拒绝 → 记下，下次启动再问（不强制）
   └─ 否 → 正常启动
重启时 → Velopack 自动应用更新 → 启动新版
```

**更新源（Update Feed）放哪**——三个选项，我推荐 B：

| 选项 | 描述 | 适合 |
|------|------|------|
| A. 静态文件服务器 | Velopack 产出的 release 文件放一个 HTTP 目录 | 简单，但要单独架服务器 |
| **B. WebAPI 提供更新源** | 远程 WebAPI 加一个 `/updates` 端点供 Velopack 轮询（或一个静态目录） | **复用现有服务器，零额外基建** |
| C. Gitee Releases | 更新包传 Gitee 仓库的 Releases | 免费，但 Gitee 不保证稳定 CDN |

**强制 vs 自愿更新**——我推荐：
- **普通更新**：自愿（提示，用户决定何时重启）
- **安全更新**（标记为 critical）：强制倒计时升级（防止漏洞版本长期运行）
- **sysadmin 可配置**：在 sysadmin 设置里控"是否允许跳过更新""自动安装无提示"

### WebAPI 升级（半自动）

- 不自动更新（数据库迁移风险）
- 流程：sysadmin 下载新版 → 停服务 → `dotnet ef database update` → 替换文件 → 启服务
- 提供《服务器升级手册》+ 升级脚本
- **EF 迁移必须向前兼容**（旧 Desktop 能连新 WebAPI 一段过渡期）

---

## [S5] 整合到本项目的落地清单

这些建议落地时影响的项目位置（设计阶段细化）：

| 改动 | 位置 |
|------|------|
| Velopack 集成 | Desktop Shell 启动管线（`App.xaml.cs` OnStartup 加 `VelopackApp.Build().Run()`） |
| 初始化向导 | 扩展现有 `FirstRunSetupViewModel`（Auth 模块）为 5 步；或新建 `InitializationWizard` |
| 种子只 sysadmin | `IdentitySeedData.cs` 删 admin 种子 |
| 强制改密 | `appsettings.json` `ForceChangeOnFirstLogin:true` + 登录流程强制 |
| 随机初始密码 | 安装时（Velopack first-run hook）生成，写配置 |
| ClinicSettings 向导 | 新建模态 + 写 `clinic-settings.json`（已有文件） |
| JWT 密钥生成 | 首次启动生成写配置 |
| Desktop 更新检查 | Shell 启动管线加 `UpdateManager.CheckForUpdates()` |
| WebAPI 更新源 | WebAPI 加静态目录或 `/updates` 端点 |

---

## [S6] 需要你确认的业务问题（非技术）

技术方案我已定。只有这几个**业务/运营**问题需要你拍板：

1. **部署拓扑**：你的诊所是**单机本地模式**为主，还是**服务器+多客户端**？（决定 WebAPI 部署要不要重点做）
2. **更新策略**：普通更新**自愿**还是**强制**？安全更新是否**强制倒计时**？
3. **更新源**：放 **WebAPI 服务器**（推荐）还是 **Gitee Releases**（免费但不保证稳定）？
4. **sysadmin 密码灾难恢复**：用 **sysadmin 内的密码重置 UI**（推荐，自助）还是保留 **CLI 工具**（需命令行能力）？

这 4 个是业务/运营决策。技术实现（Velopack 怎么集成、向导怎么写）是我的活，你不用管。
