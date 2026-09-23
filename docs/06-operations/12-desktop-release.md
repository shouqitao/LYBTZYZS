# Desktop 发布流程（Velopack）

> 版本: v1.0 ｜ 日期: 2026-09-16 ｜ 关联: US-SHELL-010（打包）/ US-SHELL-012（自动更新）、ADR-0021、`docs/06-operations/01-deployment.md`
> 打包脚本: `scripts/velopack-pack.ps1` ｜ 更新源配置: `DesktopUpdate:*`（`Shell/appsettings.Production.json`）

---

## 1. 产物与组成

`scripts/velopack-pack.ps1` 在 `dist/releases/` 产出：

| 文件 | 用途 | 必需 |
| --- | --- | --- |
| `LYBTZYZS-win-Setup.exe` | 安装器（自包含运行时，终端用户免装 .NET） | ✅ 新装 |
| `LYBTZYZS-<ver>-full.nupkg` | 完整包（更新源产物） | ✅ 更新 |
| `LYBTZYZS-<ver>-delta.nupkg` | **增量包**（输出目录存在更早版本时自动生成） | 更新（推荐） |
| `releases.win.json` | **馈源清单**（客户端枚举可用版本/资产） | ✅ 更新 |
| `RELEASES` | 旧版 Squirrel 兼容文件 | 否（保留兼容） |
| `LYBTZYZS-win-Portable.zip` | 免安装便携包 | 否 |
| `SHA256SUMS.txt` | 产物校验清单 | 否（运维核对用） |

`assets.win.json` 为 Velopack 的本地资产记录，非分发给客户端的文件。

## 2. 前置

```powershell
dotnet tool install -g vpk      # Velopack CLI（本项目验证版本 1.2.0）
```

## 3. 打包

```powershell
# 版本取自 Directory.Build.props 的 VersionPrefix
pwsh scripts/velopack-pack.ps1

# 显式版本
pwsh scripts/velopack-pack.ps1 -Version 1.0.1

# 额外产出 machine-wide 引导 msi
pwsh scripts/velopack-pack.ps1 -Version 1.0.1 -Msi
```

要点：

- **版本号单源**：`-Version` 会同时以 `-p:Version`/`-p:InformationalVersion` 盖章到程序集，
  保证「安装包版本 == 应用内日志/关于页版本」。省略时取 `Directory.Build.props`
  的 `<VersionPrefix>`（当前 `1.0.0`）。
- **不要用 `-Clean`**：`vpk` 依据输出目录中已存在的更早版本生成增量包；清空目录等于重发基线，
  客户端下次只能下载全量包（约 118 MB，而非约 1.5 MB）。
- **刻意不使用 `PublishSingleFile`**：单文件会把整个应用压成一个大文件，
  按文件差分的增量包会退化为全量包。脚本显式设置 `-p:PublishSingleFile=false`。
- 脚本末尾会做**馈源自检**（`releases.<channel>.json` 必须存在且含本次版本），失败即报错退出。

## 4. 发布（更新源）

> **渠道现状（2026-09-23）**：仓库主远端 `origin` = GitHub（SSH），Gitee 为镜像。
> 因此**公网分发主渠道为方式 C（GitHub Releases）**；方式 B（Gitee）保留为**可选镜像渠道**；
> 方式 A（自建静态目录）仍是内网/离线诊所与默认配置（`SourceKind=Server`）。

### 方式 A：自建静态目录（默认，`SourceKind=Server`）

把 `dist/releases/` **整体**同步到 `DesktopUpdate:FeedUrl` 指向的目录（HTTP 可目录访问），例如
`http://<host>:5000/releases/`。前提：WebAPI 已启用 `/releases/` 静态服务
（`DesktopUpdate:Enabled` + `ReleasesPath`）。

### 方式 C：GitHub Releases（`SourceKind=GitHub`，主渠道）

GitHub 仓库：`https://github.com/shouqitao/LYBTZYZS`。

1. 为版本打 tag（如 `v1.0.1`）并创建 Release（`gh release create v1.0.1 ...` 或网页操作）。
2. **必须上传** `releases.<channel>.json`（默认 `releases.win.json`）为 Release 资产——
   Velopack 的 git 源**不**直接读取 `*-full.nupkg`，而是先取该清单资产，
   再依清单里的 `FileName` 逐个下载包。缺该资产时客户端日志会报
   `Could not find asset called 'releases.win.json'`，表现为「检查不到更新」。
3. 同时上传清单中列出的 `*.nupkg`（`-full` 与 `-delta`）。
4. 客户端配置：

```json
"DesktopUpdate": {
  "Enabled": true,
  "SourceKind": "GitHub",
  "GitHubRepoUrl": "https://github.com/shouqitao/LYBTZYZS",
  "GitHubToken": "",
  "GitHubPrerelease": false
}
```

> **API 与字段差异（`GitHubReleaseSource`）**：GitHub REST API 主机是 **`api.github.com`**
> （`https://api.github.com/repos/{owner}/{repo}/releases`），**不是** `{host}/api/...`——
> 与 Gitee 的 `{host}/api/v5/` 拼法不同，两者不可互换；列表分页 GitHub 用 `per_page`/`page`
> （Gitee 走 `page`/`limit`）。资产下载地址两者**同名字段** `assets[].browser_download_url`
> （GitHub 为 `https://github.com/{owner}/{repo}/releases/download/{tag}/{name}`），
> 而 `assets[].url` 是 API 资源地址、**不用于下载**；`tag_name` / `prerelease` / `assets[]`
> 三个字段名与 Gitee 一致（Gitee OpenAPI v5 刻意对齐 GitHub 命名）。
>
> **私有仓库注意**：私有仓库需配 `GitHubToken`（PAT）；令牌随客户端分发等于公开令牌——
> 因此**公网分发请使用公开仓库**（公开仓库匿名可读，受 GitHub 匿名速率限制 60 次/小时/IP，
> 客户端检查间隔默认 60 分钟，不会触及上限）。

### 方式 B：Gitee Releases（可选镜像渠道，`SourceKind=Gitee`）

Gitee 仓库：`https://gitee.com/shouqitao/LYBTZYZS`。

1. 为版本打 tag（如 `v1.0.1`）并创建 Release。
2. **必须上传** `releases.<channel>.json`（默认 `releases.win.json`）为 Release 资产——
   Velopack 的 git 源**不**直接读取 `*-full.nupkg`，而是先取该清单资产，
   再依清单里的 `FileName` 逐个下载包。缺该资产时客户端日志会报
   `Could not find asset called 'releases.win.json'`，表现为「检查不到更新」。
3. 同时上传清单中列出的 `*.nupkg`（`-full` 与 `-delta`）。
4. 客户端配置：

```json
"DesktopUpdate": {
  "Enabled": true,
  "SourceKind": "Gitee",
  "GiteeRepoUrl": "https://gitee.com/shouqitao/LYBTZYZS",
  "GiteeAccessToken": "",
  "GiteePrerelease": false
}
```

> **私有仓库注意**：Gitee OpenAPI 对私有仓库的 `/releases` 要求 `access_token`
> （否则返回 `Not Found Project`）。而把令牌随客户端分发等于公开令牌——
> 因此**公网分发建议使用公开仓库，或继续走方式 A 的自建静态更新源**。

## 5. 客户端更新流程

1. **进程入口**（`App.OnStartup` 首步）：`VelopackApp.Build().Run()`
   —— 处理 `--veloapp-*`（安装/更新重启/卸载）生命周期，并注册 Velopack 定位器。
   必须在**单实例互斥量之前**执行：更新/卸载时安装器会拉起第二个进程，
   若先抢互斥量会被立刻关闭，导致安装器等待超时。
2. **启动后台检查**（`DesktopUpdateStartupStep`，Order 400，非必需步骤）：检查 → 提示 → 下载 → 提示重启。
3. **应用更新**（`DesktopUpdateService.ApplyUpdateAndRestartAsync`）：由 Velopack
   以 `--veloapp-updated` 重启进程完成替换。

配置（`Shell/appsettings.Production.json`）：

```json
"DesktopUpdate": {
  "Enabled": true,
  "CheckIntervalMinutes": 60,
  "SourceKind": "Server",
  "FeedUrl": "http://<host>:5000/releases/",
  "GitHubRepoUrl": "https://github.com/shouqitao/LYBTZYZS",
  "GitHubToken": "",
  "GitHubPrerelease": false,
  "GiteeRepoUrl": "https://gitee.com/shouqitao/LYBTZYZS",
  "GiteeAccessToken": "",
  "GiteePrerelease": false
}
```

> `SourceKind` 默认 `Server`（方式 A）；切到公网 Release 渠道时改为 `GitHub`（主，方式 C）
> 或 `Gitee`（镜像，方式 B）——三个渠道的字段可同时保留在配置里，只由 `SourceKind` 决定生效项。

开发态 `appsettings.json` 默认 `Enabled: false`（不触网）。
未通过 Velopack 安装（开发直接运行、绿色解压）时更新能力自动降级为「不可用」并记 Warning，不影响启动。

## 6. 用户数据与「更新不丢数据」

**安装目录由 Velopack 独占管理**（每用户安装根 = `%LOCALAPPDATA%\{packId}` 即
`%LOCALAPPDATA%\LYBTZYZS`）——文件不得放在这里。实际数据位置：

| 数据 | 位置 | 更新时 |
| --- | --- | --- |
| **业务数据库** | SQL Server LocalDB 实例目录（`LYBTDB_Local`） | **不受影响**（不在安装目录） |
| 用户连接设置 | `%LOCALAPPDATA%\LYBT\Desktop\user-settings.json` | 保留 |
| 数据库备份 | `%LOCALAPPDATA%\LYBT\Desktop\Backup\` | 保留 |
| 凭据/照片/系统设置/首启标记 | `%LOCALAPPDATA%\LYBT\Desktop\` | 保留 |
| 程序文件 | `%LOCALAPPDATA%\LYBTZYZS\` | 被更新替换 |

> 2026-09-16 修正：连接设置与数据库备份原位于 `%LOCALAPPDATA%\LYBTZYZS\`
> （**与安装根重合**），经 Velopack 安装后处于被更新/卸载清理的目录内；
> 已迁移到 `%LOCALAPPDATA%\LYBT\Desktop\`，并带**一次性自动迁移**
> （新位置不存在且旧位置存在时移动文件，幂等，失败不阻塞启动）。

## 7. 回滚

- 客户端：Velopack `UpdateOptions.AllowVersionDowngrade`（当前未开启）或重新分发低版本安装包。
- 服务端：见 `08-deployment-rollback.md`。
- 版本回退后建议同时回退更新源目录，避免客户端再次被提示升级。

## 8. 体积说明

自包含 WPF + 内嵌 ASP.NET Core 的完整包约 118 MB（压缩后），安装器约 124 MB。组成主要为
.NET 运行时（`PresentationFramework`/`System.Private.CoreLib`/`coreclr` 等）、
`MaterialDesignThemes.Wpf` + 字体（LatoFont 约 12 MB）、QuestPDF（打印）、ClosedXML（Excel 导入导出）。
WPF **不支持 `PublishTrimmed`**，运行时程序集无法安全裁剪；因此发版体积的杠杆是
**增量包**（实测 1.0.0 → 1.0.1 的 delta 为 1.5 MB，约为全量的 1/78）。

## 9. 故障排查

| 现象 | 原因 | 处置 |
| --- | --- | --- |
| `NETSDK1152` 发布输出文件冲突 | 两个项目产出同名 `appsettings.json`（已修复：LocalWebAPI 的独立宿主配置改名 `appsettings.localwebapi.json` 且不参与发布） | 确认无新增同名配置文件 |
| `No VelopackLocator has been set` | 未调用 `VelopackApp.Build().Run()`（开发态直接运行属预期） | 经安装器安装后运行；日志按 Warning 记录 |
| 客户端「检查不到更新」 | Gitee Release 未上传 `releases.win.json`；或静态目录未同步 `releases.<channel>.json` | 按 §4 补齐清单资产 |
| 更新包体积接近全量 | 输出目录被 `-Clean` 清空，或曾用 `PublishSingleFile` | 保留历史包目录，勿开单文件 |
| 私有仓库报 `Not Found Project` | 缺 `GiteeAccessToken` | 配令牌或改用公开仓库/自建源 |

## 10. CI（可选，当前未启用）

打包需要 Windows + `vpk` + 约 120 MB 产物，适合在发布机手动执行（本流程）。
如需自动化，Gitee Go（`.workflow/`）或其它 Windows Runner 上执行：

```powershell
dotnet tool install -g vpk
pwsh scripts/velopack-pack.ps1 -Version $env:RELEASE_VERSION
# 随后把 dist/releases 同步到更新源（方式 A）、用 gh CLI 上传 GitHub Release 资产（方式 C，主渠道）
# 或用 Gitee OpenAPI 上传 Release 资产（方式 B，镜像渠道）
```

> 当前仓库未配置 CI（发布机器手动执行）；Gitee Go 的凭据/审批链未在本环境验证，
> 故仅记录为可选项而非默认路径。
