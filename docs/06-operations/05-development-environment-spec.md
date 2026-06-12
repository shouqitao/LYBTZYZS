# LYBTZYZS 开发环境规范

> 版本: 1.0 | 创建: 2026-04-22 | 维护: 观澜
> 本文档定义 LYBTZYZS 项目的开发环境标准，后续开发以本文为准。

---

## 1. 架构总览

```
┌─────────────────────────────────────────────────────────┐
│  Ubuntu (player-virtual-machine)                        │
│  代码仓库 + Git + OpenCode 编码 + 调度                   │
│  ~/repos/LYBTZYZS/                                      │
└──────────┬──────────────────┬───────────────────────────┘
           │ scp/sync         │ scp/sync
           ▼                  ▼
┌──────────────────┐  ┌──────────────────────────────────┐
│ 192.168.190.248  │  │ 192.168.190.6                    │
│ WIN-URSB5I68VL5  │  │ DESKTOP-JT5FULA                 │
│ Windows Server   │  │ Windows 10 IoT LTSC             │
│ ─ WebAPI 服务器  │  │ ─ WPF 桌面客户端测试             │
│ ─ schtasks 托管  │  │ ─ dotnet build/run               │
└──────────────────┘  └──────────────────────────────────┘
```

---

## 2. 机器清单

### 2.1 本机 — Ubuntu（开发机）

| 项目 | 值 |
|------|-----|
| 主机名 | `player-virtual-machine` |
| 角色 | 代码仓库、Git 操作、OpenCode 调度 |
| .NET SDK | **未安装**（纯调度，不编译） |
| 代码路径 | `~/repos/LYBTZYZS/` |
| Git remote | `origin → git@github.com:shouqitao/LYBTZYZS.git` |

### 2.2 服务器 — 192.168.190.248

| 项目 | 值 |
|------|-----|
| 主机名 | `WIN-URSB5I68VL5` |
| 系统 | Windows Server 2012 R2 |
| 角色 | WebAPI 服务器 |
| SSH | `player`（密钥认证） |
| .NET SDK | 8.0.420 |
| 运行时 | AspNetCore 8.0.26 / NETCore 8.0.26 / WindowsDesktop 8.0.26 |
| 项目路径 | `C:\LYBTZYZS\` |
| 监听 | `0.0.0.0:5000` |
| 托管方式 | **schtasks**（任务名 `LYBT-API`，开机启动，SYSTEM） |
| 健康检查 | `curl http://127.0.0.1:5000/health` → `Healthy` |
| 数据库 | `LYBTDB` on localhost（Windows Authentication） |
| 配置 | `C:\LYBTZYZS\appsettings.Production.json` |
| 启动脚本 | `C:\LYBTZYZS\start-service.bat` |

> ⚠️ SC 服务在 Server 2012 R2 上会触发 1053 超时（.NET 8 启动慢），**禁止使用 `sc.exe`**，统一用 `schtasks /sc onstart`。

### 2.3 桌面 — 192.168.190.6

| 项目 | 值 |
|------|-----|
| 主机名 | `DESKTOP-JT5FULA` |
| 系统 | Windows 10 IoT LTSC |
| 角色 | WPF 桌面客户端开发测试 |
| SSH | `<see credentials manager>` |
| .NET SDK | 8.0.420 |
| MSBuild | 17.11.48（SDK 内置，**不需 VS Build Tools**） |
| 运行时 | AspNetCore 8.0.26 / NETCore 8.0.26 / WindowsDesktop 8.0.26 |
| Git | 2.48.1.windows.1 |
| 源码路径 | `C:\LYBTZYZS\` |
| 部署路径 | `C:\LYBTZYZS\publish\` |
| 网络 | 可访问 `192.168.190.248:5000`，**无外网** |

> ⚠️ 6 号机无外网，NuGet 包需从内网源获取或预先缓存。VS Build Tools 不需要，SDK 自带 MSBuild + WPF Targeting Pack。

---

## 3. 开发工作流

### 3.1 角色分工

| 角色 | 职责 | 工具 |
|------|------|------|
| **观澜** | 拆解任务、写 plan、调度 OpenCode、验证结果、部署 | mimo-v2-omni |
| **OpenCode** | 代码编写、重构、批量修改、code review | GLM5.1 / Qwen 3.6+ / GH Copilot |

### 3.2 任务分派原则

| 场景 | 执行者 |
|------|--------|
| 单文件修改、配置调整、小 fix | 观澜直接做 |
| 多文件重构、新模块开发、批量改 | OpenCode |
| Code review / 交叉验证 | OpenCode |
| 部署、测试验证 | 观澜 |

### 3.3 标准流程

```
1. 牧川下达任务
2. 观澜拆解 → 写 plan（复杂任务）
3. 派 OpenCode 执行编码
4. 观澜同步到两台机器构建
   ├── 192.168.190.248: dotnet build → 部署 → curl /health
   └── 192.168.190.6:   dotnet build → 验证 exe 生成
5. 通过 → commit + push
```

---

## 4. 构建与部署标准

### 4.1 本机（Ubuntu）— 编排

```bash
# 同步源码到目标机器
cd ~/repos/LYBTZYZS
git archive HEAD | gzip > /tmp/lybtzyzs.tar.gz
sshpass -p '<see credentials manager>' ssh player@192.168.190.6 'mkdir C:\LYBTZYZS 2>nul'
sshpass -p '<see credentials manager>' scp /tmp/lybtzyzs.tar.gz player@192.168.190.6:C:\Temp\
sshpass -p '<see credentials manager>' ssh player@192.168.190.6 'tar -xzf C:\Temp\lybtzyzs.tar.gz -C C:\LYBTZYZS'
```

### 4.2 服务器（192.168.190.248）— WebAPI

```bash
# 构建
ssh player@192.168.190.248 'cd C:\LYBTZYZS && dotnet build LYBTZYZS.sln'

# 部署 Server 项目（源码路径）
ssh player@192.168.190.248 'cd C:\LYBTZYZS && dotnet publish src\Server\Services\LYBT.WebAPI\LYBT.WebAPI.csproj -c Release -o C:\LYBTZYZS\publish'

# 重启（通过 scheduled task）
ssh player@192.168.190.248 'schtasks /end /tn LYBT-API && schtasks /run /tn LYBT-API'

# 健康检查
curl http://192.168.190.248:5000/health
```

### 4.3 桌面（192.168.190.6）— WPF

```bash
# 构建 Shell 项目（主入口）
sshpass -p '<see credentials manager>' ssh player@192.168.190.6 'cd C:\LYBTZYZS && dotnet build src\Client\Desktop\Shell\LYBT.Desktop.Shell.csproj'

# 验证输出
sshpass -p '<see credentials manager>' ssh player@192.168.190.6 'dir C:\LYBTZYZS\src\Client\Desktop\Shell\bin\Debug\net8.0-windows\LYBT.Desktop.Shell.exe'
```

---

## 5. 质量标准

| 项目 | 标准 |
|------|------|
| 编译 | **0 errors + 0 warnings** 才可 push |
| 测试 | 核心路径通过后 push |
| Commit | 说明"为什么"，非"做了什么" |
| Push | 两台机器都验证通过后才 push |
| 分支 | `main` 受保护，开发用 feature 分支 |

---

## 6. 约束与禁忌

| 约束 | 说明 |
|------|------|
| ❌ 不用 `sc.exe` | Server 2012 R2 上 .NET 8 服务会 1053 超时 |
| ❌ 不用自解释程序 | 2012 R2 兼容性差，统一 Framework-Dependent |
| ❌ 不在 6 号机下载大文件 | 无外网，安装包先在 Ubuntu 下载再 scp |
| ❌ 不混淆两个项目 | LYBTZYZS 与数字档案系统完全隔离 |
| ❌ 不在本机编译 | 本机无 .NET SDK，纯调度角色 |
| ⚠️ SCP 路径 | Windows 路径含反斜杠，用单引号包裹 |
| ⚠️ base64 中转 | 文件名含中文时，用 base64 编码传输防乱码 |

---

## 7. 文件传输模式

```
Ubuntu → 248 (Server):  scp (密钥认证, 直连)
Ubuntu → 6   (Desktop):  sshpass + scp (密码认证)
Windows 路径:             单引号包裹 + 反斜杠
中文路径/文件名:          base64 编码后传输解码
大文件(>100MB):          先在 Ubuntu 下载, 再 scp
```

---

## 8. 快速参考

```
项目根:        C:\LYBTZYZS\ (两台 Windows)
Server 入口:   src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj
Desktop 入口:  src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
启动脚本:      start-service.bat (Server)
配置文件:      appsettings.Production.json (Server)
健康端点:      http://192.168.190.248:5000/health
Server 部署:   schtasks /tn LYBT-API
```
