# 模块级审计交叉验证报告（技术总监 + Mimo Code）

> 日期：2026-08-08
> 独立报告 #1：`structure-audit-module-level-2026-08-08.md`（技术总监，15 页）
> 独立报告 #2：`structure-audit-module-level-mimo-2026-08-08.md`（Mimo，308 行，22 张评分卡）
> 本报告为两份独立分析的交叉验证结论，供产品负责人拍板。

---

## 一、交叉验证结论

### ✅ 双方一致确认

| # | 结论 | 双方证据 |
|---|------|---------|
| 1 | **无模块需要重写**（保留 4 / 修补 16 / 重写 0） | 评分卡一致 |
| 2 | Desktop VM 分层健康（注入 Service 不直连 HTTP） | 技术总监抽查 + Mimo 符号级 |
| 3 | LocalWebAPI 薄宿主健康（A-17 后） | 一致 |
| 4 | Shell 组合根健康 | 一致 |
| 5 | Infrastructure 职责过载（14 类） | 一致（P1） |
| 6 | Service 命名混乱（A-08 遗留未决） | 一致（P2） |
| 7 | MedicalCase 结构分叉（无 CQRS）+ QueryService 538 行 | 一致（P2） |
| 8 | F-01 FeatureToggle 建议删除（14 开关缩至 2 且无有效消费） | Mimo 核实更准（14→2） |

### ⚠️ 交叉纠错（Mimo 抓到技术总监漏掉的，或双方需澄清的）

| # | 纠错 | 详情 | 判定 |
|---|------|------|------|
| **M1（最重要）** | **P0 候选：Server.Patients 违反 ADR-0017** | Mimo 称 Patients 无独立 DbContext 违反 ADR-0017「每模块独立 DbContext」。**技术总监核实：ADR-0017（06-02）确要求全独立，但 03-server.md（08-05 v2.3）已如实文档化当前态 = 5 独立 + 5 复用（Patients 注入 AppDbContext）**。这是「设计态 vs 当前态」的决策澄清项，非新 bug | **降级 P1**：需产品拍板「是否推进 ADR-0017 全独立」（工作量：5 模块各建 DbContext+迁移，大）；或「接受当前态并修订 ADR-0017」 |
| M2 | Desktop 3 个 Mapper 文件零引用 + DTO↔Model 手写重复 2-3 处/模块 | Mimo 符号级确认；技术总监报告只标了 Mapper「待核实」 | 确认 P1：桌面侧 Mapper 统一（F-02 实质内容） |
| M3 | **领域事件空转**：8 个 Created/Deleted 事件全部无订阅者 | Mimo 独有发现（技术总监未查领域事件） | P1：需确认这些事件是「未来扩展预留」还是「应该接线」 |
| M4 | **Shell 真实 bug**：RoleDefinitionBase.cs:17 "AuthModule" vs 实际 "AuthenticationModule"，每次登录触发被吞异常 | Mimo 独有发现 | P1 代码 bug，确认后应修 |
| M5 | **VM→ApiClient 越层 ×3**：AuditLogViewModel/ReportsHomeViewModel/RegistrationListViewModel | Mimo 独有发现（技术总监抽查的 2 个 VM 恰好健康，未覆盖这 3 个） | P1：越层违反 MVVM 分层，应改为走 Service |
| M6 | **A-14 系统性偏离**：8 个 Server 模块无一完全符合「查询走 Service、命令走 MediatR」，写操作普遍留 Service 零验证零审计 | Mimo 独有发现 | P1 或决策项：A-14 声称是有意设计，但「写操作留 Service 绕过验证」可能是偏离而非有意 |

### 📌 F 类 + Tools 最终决策（Mimo 提供，技术总监认可）

| ID | Mimo 决策 | 理由 |
|----|----------|------|
| F-01 | **删除 FeatureToggle** | 14 开关已缩至 2 且无有效消费（YAGNI）|
| F-04 | **已删可关闭** | SyncService 已随清理删除 |
| F-05 | **保留**（9 处活跃调用） | 技术总监未找到独立 PatientMapper.cs 是漏查，Mimo 确认存在且活跃 |
| F-02/F-03/F-07/F-08 | 修补 | Mapper 统一 / LocalData 废弃 / API 版本化 / 日志归档 |
| F-06 | 后置 | 低优先级 |
| Tools | **删 3 留 1** | 保留 PasswordHashGenerator（运维用），删 ApiTester/LoginTester/UserInfoVerifier |

---

## 二、合并定级（终版）

### P1（应修，本次批次）

| # | 项 | 建议 |
|---|-----|------|
| P1-1 | M1 ADR-0017 vs 当前态澄清 | **产品拍板**：推进全独立（大工程）或修订 ADR 接受当前态（推荐后者，5 复用是合理模块化折中）|
| P1-2 | M2 Desktop Mapper 统一（F-02） | 桌面 DTO↔Model 改 Mapperly，删 3 零引用 Mapper |
| P1-3 | M3 领域事件空转 | 确认订阅需求，无则删除事件发布或文档化「预留」|
| P1-4 | M4 RoleDefinitionBase 模块名 bug | 修 "AuthModule"→"AuthenticationModule" |
| P1-5 | M5 VM→ApiClient 越层 ×3 | 3 个 VM 改走 Service 层 |
| P1-6 | M6 A-14 写操作绕过验证 | 核实是否偏离设计，是则补验证/审计 |
| P1-7 | C1 Infrastructure 职责过载 | CardReader 独立 + LocalData 废弃 |
| P1-8 | F-01 删 FeatureToggle + Tools 删 3 | 清理批次 |

### P2（可后议）

Service 命名统一（需产品拍板改名）/ MedicalCase 结构文档化 / Patients Interfaces 目录归位 / 超大 VM 观察项

---

## 三、给产品负责人的结论（业务语言）

1. **结构比预想健康**：无模块需要重写，骨架统一。
2. **本轮新增 4 个 P1 真实问题**（Mimo 抓到）：① Shell 有个模块名写错的 bug（每次登录静默异常）；② 3 个 ViewModel 越层直连 API（绕过分层）；③ 8 个领域事件发了没人听（空转）；④ Desktop 映射还是手写（Server 已统一 Mapperly，桌面没跟上）。
3. **1 个需要你拍板的设计决策**：ADR-0017 要求每模块独立数据库上下文，但当前 5 个模块共用同一个——是花大力气推进全独立（工程量大），还是接受当前态、修订 ADR（推荐，模块化折中合理）。
4. **F 类清理**：删 FeatureToggle 机制 + 删 3 个工具（保留密码工具），LocalData 废弃。

## 四、执行建议

**下一批次（P1 修复）**：M1 拍板后 → M4（bug 修复）→ M2/M5（映射+越层）→ M6（验证审计）→ M3（事件）→ P1-7（Infrastructure）→ F-01+Tools（清理），每项独立验证。
