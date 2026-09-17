# Architecture
> 版本: v1.1 | 日期: 2026-09-17

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
```

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

> **唯一权威**：当前 Build/测试/已知问题见 [13c-current-status.md](13c-current-status.md)。本节仅列架构级长期假设，不复制问题清单。

1. **Token 本地有效期** — 本地模式 JWT 有效期较长、撤销机制有限（内网风险低，见 [09-security-architecture.md](09-security-architecture.md)）
2. **Sync 延期** — v1.0 本地→远程数据同步推迟至 v2.0，本地为数据孤岛（见 [17-sync-protocol.md](17-sync-protocol.md)）
3. **公网 HTTPS** — 部署若暴露公网需 HTTPS（见 [00-governance/tech-debt.md](../00-governance/tech-debt.md) TD-004）

## Related Documents

- `01-product/01-vision.md` — 产品愿景
- `02-requirements/01-prd.md` — 产品需求文档
- `03-architecture/01-system-overview.md` — 系统架构详解
- `03-architecture/09-security-architecture.md` — 安全架构
- `03-architecture/11-business-flows.md` — 关键业务流程
- `03-architecture/12-permissions-matrix.md` — 权限矩阵
- `06-operations/09-variables-secrets.md` — 配置与密钥
- `05-development/04-testing.md` — 测试策略与覆盖
- `02-requirements/11a-shell.md` — Shell/平台壳（历史 Shell Phase2 设计已归档）
- `02-requirements/13-traceability-matrix.md` — 需求追溯矩阵
- `03-architecture/decisions/README.md` — ADR 索引

---

*文档版本: v1.1 | 最后更新: 2026-09-17 | 可读性审查：修复代码围栏、风险列表指向 13c、清理过程注释*
