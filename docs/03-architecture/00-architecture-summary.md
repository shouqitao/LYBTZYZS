# Architecture
> 版本: v1.0 | 日期: 2026-08-20

## Product Overview

凌隐宝堂中医诊所管理系统（LYBTZYZS）— 面向小型中医诊所的桌面端管理系统，支持远程/本地双模式，覆盖患者管理、中医诊断（望闻问切）、处方开具、药材管理、验方管理等核心业务流程。

**核心假设**：
- 诊所规模 1-5 人（1 医生 + 1-2 前台 + 0-1 管理员 + 1 运维）
- 公有云部署（WebAPI）+ 本地离线（LocalDB）双模式
- 中医诊疗流程：挂号→就诊→诊断→开方→打印→取药

## Tech Stack

| 层 | 技术 |
|---|------|
| Desktop | .NET 8, WPF, Prism 8.1.97 (DryIoc), CommunityToolkit.Mvvm, MaterialDesignThemes.XAML |
| Server | ASP.NET Core 8, EF Core 8, SQL Server |
| Local | SQL Server LocalDB (嵌入式 LocalWebAPI) |
| Auth | JWT (运行时=Identity PBKDF2 哈希；BCrypt 仅残留工具类 PasswordHelper)，🧲 Token Family 旋转（D3 B+，v1.0 补回；重放检测 v2.0） |
| 打印 | QuestPDF, WPF FixedDocument |
| 映射 | Riok.Mapperly (编译期) |
| HTTP | Refit (Desktop→Server) |
| 日志 | Serilog (两阶段引导) |

## Architecture Layers

```text
┌─────────────────────────────────────────────────┐
│  Desktop (WPF/Prism)                            │
│  ┌──────────┬────────────────────────────────┐  │
│  │  Shell   │  Modules (Identity/Patients/Catalog│  │
│  │          │  /MedicalCase/Registra-            │  │
│  │          │  tion/Users/Sync)                   │  │
│  └──────────┴────────────────────────────────┘  │
│                    ↓ Refit                       │
├─────────────────────────────────────────────────┤
│  Server (ASP.NET Core)                          │
│  Controllers → Services → Repository → DbContext│
│                    ↓ EF Core                     │
├─────────────────────────────────────────────────┤
│  Database (SQL Server / LocalDB)                │
└─────────────────────────────────────────────────┘
```text

**依赖方向**: Shell → Roles → Modules → Infrastructure → Foundation → Contracts

> **LocalWebAPI 特例（P07 白名单，见 ADR-0010/0023）**：`LYBT.LocalWebAPI` 为 P07 模块间零引用的**唯一例外**，允许直接引用 `LYBT.Entities`/`LYBT.Infrastructure` 及 6 个 Server 模块（`Identity/Catalog/Patients/MedicalCases/Registrations/Reports`）以实现统一服务层（双模式行为 100% 复用）。其余 Server 模块间仍零引用，架构测试显式豁免 LocalWebAPI。

## Auth/Session Flow

1. Desktop → `POST /api/v1/auth/login` → Server 验证凭据
2. Server 返回 JWT AccessToken + RefreshToken（族旋转），详见 [[02-auth]]
3. Desktop 存储 Token (内存，进程退出自动清除)
4. 后续请求携带 `Authorization: Bearer <token>`
5. Token 过期前自动刷新 (TokenRefreshHandler)
6. 登出 → 🧲 撤销 Token 族（D3 B+ v1.0 补回）

## Trust Boundaries

| 边界 | 说明 |
|------|------|
| Desktop ↔ Server | JWT 认证，HTTPS (生产) |
| Server ↔ DB | EF Core 连接，服务账号 |
| Desktop ↔ LocalDB | 本地模式，无认证 |
| 用户 ↔ Desktop | 本地登录，密码验证 |

## 性能约束

| 指标 | SLO (P95) | 架构约束 |
|------|-----------|----------|
| API 简单查询 | < 500ms | EF Core 查询优化，连接池 ≥10 连接 |
| API 列表查询 | < 1s | 分页必须（20 条/页），禁止全表扫描 |
| API 聚合保存 | < 2s | MedicalCase 聚合原子写入，索引覆盖关键查询 |
| Desktop 启动 | < 5s | 模块懒加载 + 启动管线并行化 |
| Desktop 页面切换 | < 1s | Region 预加载 + 本地缓存 |
| SignalR 推送延迟 | < 500ms | WebSocket 优先，降级 SSE/LongPolling |

## Known Risks / Assumptions

1. **C1 双轨模块加载** — LoginCoordinator 硬编码旁路已确认为 bug，待修复（见 Shell Phase2 Design）
2. **Token 本地 1 年** — 本地模式 JWT 有效期 1 年，无撤销机制（内网风险低）
3. **Sync 延期** — v1.0 本地→远程数据同步被延期到 v2.0，本地为数据孤岛
4. **审计日志缺失** — 医疗审计日志实体已删，待补回（D1 决策）

## Related Documents

- `01-product/01-vision.md` — 产品愿景
- `02-requirements/01-prd.md` — 产品需求文档
- `03-architecture/01-system-overview.md` — 系统架构详解
- `03-architecture/09-security-architecture.md` — 安全架构
- `03-architecture/11-business-flows.md` — 关键业务流程
- `03-architecture/12-permissions-matrix.md` — 权限矩阵
- `06-operations/09-variables-secrets.md` — 配置与密钥
- `05-development/13-test-coverage-map.md` — 测试覆盖地图
- ~~`docs/compose/specs/2026-06-28-shell-phase2-design.md`~~（已归档）— Shell Phase2 设计，见 [11a-shell.md](../02-requirements/11a-shell.md)
- ~~`docs/compose/specs/2026-06-28-prd-code-reconciliation.md`~~（已归档）— PRD-代码对账，见 [13-traceability-matrix.md](../02-requirements/13-traceability-matrix.md)
