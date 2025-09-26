# 凌隐宝堂中医诊所管理系统（LYBTZYZS）

**凌隐宝堂中医诊所管理系统（LYBTZYZS）**是一个基于.NET 8的企业级中医诊所管理解决方案，采用前后端分离架构，专为小型中医诊所（<20人）的诊疗业务场景设计和优化。

## 🎯 系统概览

### 技术架构
- **后端**: ASP.NET Core 8.0 Web API + Entity Framework Core + SQL Server
- **前端**: WPF + Prism.DryIoc MVVM架构 + Refit HTTP客户端  
- **共享**: 统一DTO模型和服务接口定义，确保前后端数据契约一致性

### 核心业务模块（8个）
| 模块 | 功能描述 | 状态 |
|------|----------|------|
| **Auth** | JWT身份认证、RBAC权限控制 | ✅ 完成 |
| **Users** | 用户管理、Admin/Doctor角色体系 | ✅ 完成 |
| **Patients** | 患者档案管理、基础信息维护 | ✅ 完成 |
| **MedicalCase** | 医疗案例管理、诊疗流程控制 | ✅ 完成 |
| **Consultation** | 中医四诊记录、辨证论治 | ✅ 完成 |
| **Prescriptions** | 处方开具、剂量计算、价格预览 | ✅ 完成 |
| **Herbs** | 中药材信息管理、价格维护 | ✅ 完成 |
| **Formula** | 验方模板管理、经典方剂库 | ✅ 完成 |

### 项目规模统计
- **总计**: 28个.NET项目，3个解决方案文件
- **Server端**: 10个项目（2个Core + 8个业务模块）
- **Client端**: 15个项目（基础设施 + 8个业务模块）
- **Shared层**: 3个项目（Models + Interfaces + Utilities）

### 架构分层说明
```
解决方案架构
├── LYBT.All.sln          # 完整解决方案（28个项目）
├── LYBT.Server.sln       # 后端解决方案（10个项目）
└── LYBT.Desktop.sln      # 前端解决方案（15个项目）

Server端架构
├── Core/                 # 核心基础设施
│   ├── LYBT.Entities    # 数据实体模型（8个业务实体）
│   └── LYBT.Infrastructure # 基础设施实现（数据访问、安全、缓存等）
├── Modules/              # 业务模块层
│   └── [8个业务模块]    # 各业务领域的Services、Repositories、Mapping
└── Services/             # 服务层
    └── LYBT.WebAPI       # 统一API网关（10个控制器）

Client端架构  
├── Shell/                # 应用程序启动壳
├── Core/                 # 核心基础设施
├── Services/             # 业务服务和API客户端  
├── Infrastructure/       # UI基础设施和通用组件
├── Workbenches/          # 工作台系统（Admin/Medical）
└── Modules/              # 业务模块UI层
    └── [8个业务模块]    # 各业务领域的Views、ViewModels

Shared层架构
├── LYBT.Shared.Models    # 统一DTO模型和响应格式
├── LYBT.Shared.Interfaces # 服务接口定义  
└── LYBT.Shared.Utilities # 通用工具类和扩展方法
```

本项目基于 .NET 8，包含 ASP.NET Core Web API 后端与 WPF Prism 桌面客户端。目前正在进行桌面端重构：事件体系尚未统一、桌面应用无法成功编译，服务器测试亦存在失败用例。下述内容以当前进展为准，不再使用“生产就绪”等描述。

## 当前状态概览（2025-09-24）
| 项目维度 | 当前结论 |
| --- | --- |
| 桌面端编译 | ❌ 事件重复定义导致编译失败，需统一到 UnifiedEvents.cs |
| 服务器测试 | ⚠️ dotnet test LYBT.Server.sln 失败（Consultation AutoMapper、API 契约尚未修复） |
| 术语一致性 | ⚠️ README / UI / 文档正在统一为“诊疗工作台”等最新称谓 |
| 任务管理 | ✅ Thinker 在 docs/tasks/pending/ 发布任务，完成总结存放于 docs/tasks/completed/ |

## 当前优先级（Thinker）
1. 统一桌面端事件体系，移除重复事件与枚举并修正命名空间。
2. 修复 UnifiedDesignSystem.xaml 转换器引用，确保桌面端资源加载正常。
3. 梳理"诊疗工作台"相关 UI 和文案，替换旧的"看诊"术语。
4. 恢复服务器测试，并规划桌面端关键服务的单元测试。

## Server层查询架构

### 查询层架构概述
Server层采用优化的三层架构，通过ReadRepository模式实现查询和命令分离的基础：

**架构路径**: Controller → QueryService → ReadRepository → Database

**核心特性**：
- **统一缓存策略**：5分钟滑动过期，缓存命中率达83.5%
- **缓存穿透防护**：空值结果缓存1分钟，防止恶意请求
- **性能优化**：缓存场景下查询性能提升93-96%
- **软删除过滤**：全局应用，自动排除已删除记录

### 缓存配置
| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| DefaultCacheDuration | 5分钟 | 正常数据缓存时长 |
| NullCacheDuration | 1分钟 | 空值缓存时长 |
| CacheKeyPrefix | `{EntityName}:readonly:` | 缓存键前缀 |

### 查询层诊断
使用PowerShell诊断脚本监控查询层性能：
```powershell
# 完整诊断
./scripts/QueryLayerDiagnostics.ps1 -CacheStatus -EFTracking -PerformanceSampling

# 特定模块缓存状态
./scripts/QueryLayerDiagnostics.ps1 -Module Users -CacheStatus
```

### 相关文档
- Phase 1重构总结：`docs/tasks/completed/2025-09-24-server-phase1-query-layer-refactor-task-summary.md`
- Phase 2巩固报告：`docs/reports/server-query-layer-phase2-hardening-report.md`
- Phase 3缓存治理：`docs/tasks/completed/2025-09-24-server-phase3-cache-governance-task-summary.md`
- 诊断脚本：`scripts/QueryLayerDiagnostics.ps1`

### 缓存诊断脚本使用

Query Layer诊断脚本支持缓存状态监控和性能分析：

```powershell
# 使用真实API获取缓存数据（推荐）
.\scripts\QueryLayerDiagnostics.ps1 -CacheStatus -UseRealApi -Token "your-jwt-token"

# API失败时使用离线回退
.\scripts\QueryLayerDiagnostics.ps1 -CacheStatus -UseRealApi -OfflineFallback -Token "your-jwt-token"

# 详细调试模式
.\scripts\QueryLayerDiagnostics.ps1 -CacheStatus -UseRealApi -Verbose -Token "your-jwt-token"
```

**前置条件**：
- 服务运行在 http://localhost:5001（或通过-BaseUrl指定）
- 拥有Admin角色的JWT Token
- 服务已更新到Phase 3版本（包含CacheHealthController）

**故障排查**：
- HTTP 401：Token无效或过期，重新获取JWT Token
- HTTP 403：Token权限不足，需要Admin角色
- 连接失败：检查服务运行状态和端口配置
- API不存在：确认服务已更新到最新版本

## 项目结构
`
src/
├── Server/                  # ASP.NET Core Web API
├── Client/Desktop/          # WPF Prism 客户端
├── Shared/                  # DTO、接口、工具库
docs/
 └── tasks/
      ├── pending/           # Thinker 发布的任务
      └── completed/         # 任务完成总结
`

## 构建与运行（当前建议）
`powershell
# 还原
dotnet restore LYBT.All.sln

# 构建（桌面端待修复前，可先构建 Server / Shared）
dotnet build LYBT.Server.sln -c Release --no-restore
# dotnet build LYBT.Desktop.sln -c Release --no-restore  # 需先统一事件体系

# 运行 WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI
`

## 测试现状
`powershell
# 服务器侧（当前失败，优先修复）
dotnet test LYBT.Server.sln -c Release

# 桌面端测试：尚未建立自动化测试基线
`

主要失败点：
- Consultation AutoMapper 映射字段与模型不一致。
- API 契约测试期望响应结构与实际返回不匹配。

## 术语与角色
- **诊疗工作台**：旧称“看诊工作台”，后续统一使用新称谓。
- **系统工作台**：管理员功能入口。
- Thinker（ChatGPT）负责架构规划、任务发布与文档维护；Coder（Claude Code）专注编码实现。

## 任务目录
- docs/tasks/pending/：Thinker 发布的任务说明。
- docs/tasks/completed/：任务完成后的总结报告（建议追加 -summary.md 后缀）。

## 后续计划
- 完成事件归一 → 修复桌面端编译 → 更新桌面端文案 → 恢复测试 → 补齐桌面关键服务单测。
- README 将随着任务推进持续更新，确保信息与代码实现一致。
