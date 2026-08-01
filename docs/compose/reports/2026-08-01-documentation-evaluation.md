# 凌隐宝堂文档体系评估报告

> **评估日期**: 2026-08-01
> **评估范围**: `docs/` 全量 (351 个 .md 文件, 4.3MB)
> **评估方法**: 3 路并行 subagent 按域深读 + 主评估人交叉抽查代码验证
> **基准对比**: 与上一份文档审计报告 (2026-06-28) 对比，标注已修复/仍存在/新发现
> **评估性质**: 只读评估，不修改任何文件

---

## 一、执行摘要

文档体系整体健康度 **B-**（较上次的 D 级有显著提升）。上次 (2026-06-28) 审计发现的系统性失同步问题已做了一轮集中修复：端口铁律已统一为 5300，US 总数已收敛为 141，Sync 幽灵模块已加 v2.0 声明，用户端点补齐了 restore/batch-enable/batch-disable。

但文档体系仍存在 **中等量级的失同步**：少数核心数字（AccessToken 有效期、ADR 计数、compose 文件数）跨文档矛盾，若干代码改名未同步到文档，一份含明文密码的测试环境文档未清理，`compose/` 目录（208 文件）的 README 统计数严重滞后。

### 健康度总览

| 分区 | 文件数 | 评分 | 趋势 | 核心问题 |
|------|--------|------|------|----------|
| 01-product | 4 | 5/5 | ↑ | 优秀，无重大问题 |
| 02-requirements | 18 | 4/5 | ↑ | US 总数已收敛； Receptionist 药材访问权限跨文件描述不清 |
| 03-architecture | 54 | 3/5 | ↑ | ADR 计数三处不一致 (14/18/实际17)；SaveAsDraft 改名未同步；6 个草稿状态优化文档 |
| 04-api-reference | 14 | 4/5 | ↑ | 端点已基本对齐代码；DeployController 缺文档；策略数少计 1 |
| 05-development | 27 | 4/5 | ↑ | 良好，SQLite 误称已修正 |
| 06-operations | 15 | 3/5 | ↑ | **含明文 SA 密码**；数据目录路径两文档不一致 |
| compose | 208 | 2/5 | → | README 统计数严重滞后（specs 17→64）；零治理索引；大量已完工计划未归档 |
| plans (deprecated) | 6 | 2/5 | → | 已标记废弃但文件残留 |
| training | 1 | 4/5 | → | 单文件，内容完整但仅覆盖一个功能 |
| **总体** | **351** | **B-** | **↑** | 主文档集稳定，compose 目录需治理 |

---

## 二、🔴 关键问题（必须修复）

### 2.1 🔴 明文密码泄露 — `06-operations/deployment-test-environment.md` + `03-webapi-deployment-summary.md`

**严重度**: P0 安全（本评估亲自核实 3 处明文凭证）

| 位置 | 泄露内容 | 严重度 |
|------|----------|--------|
| `deployment-test-environment.md:21` | SSH 密码 `123456` 明文 | 高 |
| `deployment-test-environment.md:33` | SA 密码 `Shou@850528` 明文 | **极高** |
| `03-webapi-deployment-summary.md:102` | **生产 JWT SecretKey 明文** | **极高**（可伪造任意用户 Token） |

**建议**: 
1. 立即将 3 处明文替换为占位符 `<REDACTED>`
2. 生产 JWT SecretKey 已泄露，应轮换（该 SecretKey 当前可能仍用于生产）
3. 评估是否将含密钥文件移出 git 历史（git filter-branch / BFG），或至少加入 `.gitignore`

### 2.2 🔴 AccessToken 有效期跨文档矛盾（且文档描述与代码实现不符）

**代码真相**（本评估亲自验证）:
- JWT 实际签发有效期从配置读取：`JwtService.cs:110` `CurrentOptions.AccessTokenExpirationMinutes`
- `appsettings.json` 开发默认 **480 分钟**，`appsettings.Production.json` 生产 **30 分钟**
- `LoginCommandHandler.cs:151` 的 `tokenExpireMinutes = 60` 仅用于 `LoginResponse.ExpiresAt` 展示字段，不代表 JWT 实际有效期

**文档错误**:
| 位置 | 描述 | 实际 |
|------|------|------|
| `02-auth.md:9` | 「60 分钟硬编码」 | ❌ 从配置读取，开发 480/生产 30 |
| `01-auth.md:9` | 「AccessToken 有效期 60 分钟」 | ❌ 同上 |
| `10-variables-secrets.md:11` | 「代码硬编码 60 分钟，未在 appsettings 暴露」 | ❌ 已在 appsettings 暴露 |
| `11-variables-value-ranges.md:10` | 同上错误描述 | ❌ |
| `00-architecture-summary.md:50` | 「30min」 | ❌ 开发 480/生产 30 |
| `02-configuration.md:140` | 开发 480/生产 30 | ✅ 正确 |
| `12-nfr.md:156` | 60 分钟 | ❌ |

**建议**: 以「JWT 有效期从配置读取，开发默认 480 / 生产 30」为唯一真相，修正 5 处错误描述。

### 2.3 🔴 ADR 计数三处不一致

| 位置 | 声称 | 实际文件 |
|------|------|---------|
| `03-architecture/README.md:50` | 「ADR-0001~0014，共 14 条」 | 17 个 .md 文件 |
| `docs/AGENTS.md:52` | 「18 ADR (0001~0018)」 | 17 个（缺 0016） |
| `docs/README.md:52` | 「14 条 ADR」 | 17 个 |

实际：编号 0001-0015、0017-0018（跳号缺 0016），共 **17 个 ADR 文件**。

**建议**: 统一为「17 条 (ADR-0001~0015, 0017~0018; 0016 预留)」，或补建 ADR-0016，或在 README 注明跳号原因。

### 2.4 🔴 compose/ README 统计数严重滞后

`docs/compose/README.md:12-14`（标注 2026-06-28 统计）:

| 子目录 | README 声称 | 实际 | 偏差 |
|--------|------------|------|------|
| specs 活跃 | 17 | 64 | +47 |
| specs 归档 | 22 | 22 | 0 |
| plans 活跃 | 9 | 54 | +45 |
| plans 归档 | 39 | 41 | +2 |
| reports | 4 | 25 | +21 |

**根因**: README 统计数自 2026-06-28 后未更新，一个多月来新增了大量 specs/plans/reports 但无人维护索引。

**建议**: 重跑统计，或改为脚本自动生成（避免人工维护滞后）。同时考虑大量已完工的 plans 是否应批量归档。

---

## 三、🟡 中等问题

### 3.1 医案聚合根方法改名未同步

| 位置 | 描述 | 实际 |
|------|------|------|
| `03-architecture/03-server.md:68` | `SaveAsDraft()` | 方法已改名 `Suspend()` |
| `ADR-0001:50` 变更记录 | 正确记录了 2026-02-21 的改名 | ✅ |

ADR-0001 的变更记录正确，但 `03-server.md` 仍用旧名。

### 3.2 数据库文件路径两文档不一致

| 位置 | 路径 |
|------|------|
| `06-operations/01-deployment.md:211` | `%LOCALAPPDATA%\LYBTZYZS\data\lybt-local.mdf` |
| `06-operations/07-backup-recovery.md:14` | `%APPDATA%\LYBT\data\lybt-local.mdf` |

差异：`LOCALAPPDATA` vs `APPDATA`，`LYBTZYZS` vs `LYBT`。应统一为代码实际路径。

### 3.3 PolicyConstants 策略数描述不一致

API README 与架构文档多处称「共 4 项策略」，实际 `PolicyConstants.cs` 定义了 **5 个策略**（含 `DoctorOrAdminOrReceptionist`），文档遗漏。而该策略实际已被 Patients/Registrations/MedicalCases 使用。另：`12-permissions-matrix.md` 未提及此策略，`03-patients.md` 标注「代码当前为 DoctorOrAdmin」也已过时（代码已升级为 `DoctorOrAdminOrReceptionist`）。

### 3.4 Receptionist 药材访问权限跨文件不一致

- `01-product/02-personas.md:158` 权限矩阵标注 Receptionist 对药材「✗（前台不涉及药材）」
- `02-requirements/05-herbs.md:51,118` US-HERB-003 写「端点受 DoctorOrReceptionist 策略保护（Receptionist 可查询）」
- 代码 `HerbsController.cs:23` 使用 `[Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]` — 包含 Receptionist 查询

**结论**: `05-herbs.md` 与权威矩阵/Personas 矛盾。需统一（建议以代码为准：Receptionist 可查询，不可编辑）。

### 3.5 DeployController 缺 API 文档

12 个 Server Controller 中 `DeployController`（2 端点 upload/restart，`AdminOrSuperAdmin` 权限）是唯一无对应 `04-api-reference/*.md` 文件的控制器。如果该 Controller 不面向外部 API（内部部署用），应在 API README 中注明。

### 3.6 架构文档滞后于 MediatR CQRS 迁移

- `03-server.md:5` 仍描述「Controller → Service → Repository → DbContext」经典三层架构
- 但代码已迁移 MediatR CQRS（ADR-0017 Modular Monolith CQRS），Controller 直接用 `ISender` 调 CommandHandler，无直接 Service 调用
- `00-architecture-summary.md` 桌面模块列表仍含 "Sync"（v2.0 未实现）
- `03-server.md:68` 仍写 `SaveAsDraft()`（实际已改名 `Suspend()`）

**这是核心架构文档未跟进实际迁移的证据**，建议优先修正。

### 3.7 数据库名称不一致

| 位置 | 名称 |
|------|------|
| 代码 `appsettings.json:27` | `Database=LYBTDB` |
| `07-backup-recovery.md`、`09-deployment-rollback.md:12`、`deployment-test-environment.md:31`、`04-windows-deployment.md:217` | `LYBTDB_Dev` |
| `08-monitoring-alerting.md:206` | `DB_ID('LYBTDB')` |

**建议**: 生产/测试环境用 `LYBTDB_Dev`、代码默认 `LYBTDB`（可能由 ConnectionStrings 覆盖），文档需说明这一环境差异。

### 3.8 连接池配置不一致

代码 `appsettings.json:27` `Max Pool Size=20`，但 `02-configuration.md:220` 写 `MaxConnections=100, MinConnections=5`；`12-nfr.md:56` NFR-DATA-004 写 20。文档与代码/NFR 不一致。

### 3.9 Desktop 分发方式描述滞后

`01-deployment.md:207` 写「ClickOnce 或 MSI 分发」，但 `11a-shell.md` US-SHELL-010 明确写 Velopack 打包，`02-personas.md` 亦提及 Velopack。需求文档已领先于运维文档。

### 3.10 `13-error-handling-flow.md` 与 `06-error-handling.md` 内容重叠

前者已明确标注过期但仍留在主目录，与后者（权威）内容全部重叠。建议移至 `archive/` 或删除。

### 3.11 compose/plans 大量已完工计划未归档

`compose/plans/` 活跃目录有 54 个文件，`plans/archive/` 有 41 个。抽查发现多个 2026-06 月的计划已对应已合并的 commit，应归档但未归档。例如:
- `2026-06-27-*.md` 系列（7 个）多为早期 Shell/UI 计划，git 历史显示对应工作已完成
- `2026-07-14-*.md` 系列（7 个）Shell 优化的多个 plan

**综合估算**: 活跃区实际约 145 份（specs 64 + plans 54 + reports 25 + reviews 1），README 声明仅约 30 份活跃——**约 115 份应归档未归档**，构成首要「文档债」。

**建议**: 按合并状态批量归档，减少活跃目录的噪声。

### 3.12 compose/reports 命名违规

`compose/README.md` 命名规约要求 `YYYY-MM-DD-<slug>.md`，但 reports/ 活跃区 **8/25 不合规**（缺日期前缀），如 `architecture-analysis-2026-07-19.md`、`desktop-architecture-cleanup.md`、`frontend-architecture-optimization.md` 等。archive/ 子目录 100% 合规，说明归档时执行到位、活跃区执行不均。此外 specs/ 有 1 份缺日期（`controller-inheritance-simplification.md`）。

---

## 四、🟢 轻微问题

### 4.1 `docs/plans/` 废弃目录残留文件

`docs/plans/README.md` 已明确标注「⚠️ 本目录已弃用」，但 `archive/` 子目录仍保留 5 个历史文件。README 指向 `compose/`，但残留文件可能误导新成员。

**建议**: 确认这些文件已被 compose/reports 取代后，可考虑 git 历史保留、当前删除。

### 4.2 `scan-output/CLASS_INVENTORY.md` 过时

该文件声称 `LYBT.Desktop.Receptionist (3 files)` 作为独立项目，但实际 Receptionist 是 `LYBT.Desktop.Clinical/Receptionist/` 子目录，非独立 csproj。文件头标注「Generated 2026-06-29」，已过时。

### 4.3 `docs/README.md` 总文件数偏低

`docs/README.md:17` 写「~110 个核心文档 + 90+ compose 工作流产物」，实际核心文档 (01-06) 已达 116 个，compose 已达 208 个。建议改为「~116 + 208 compose」。

### 4.4 6 份架构优化文档处于草稿状态

`03-architecture/` 下 6 个 `*-optimization.md` 文件（`client-desktop-architecture-optimization.md` 等）部分标注「状态: 草稿」，与同级的主架构文档格式风格不一致。如果内容已被主文档吸收，可考虑归档；仍有效则建议完善并去掉草稿标记。

### 4.5 wpftmp 临时文件残留

6 个 `*_wpftmp.csproj` 文件残留在 Desktop 模块目录（`.gitignore` 已覆盖但 git 仍跟踪）。这是上次审计已指出的问题，仍未清理。

---

## 五、已修复问题（对比 2026-06-28 审计）

上次审计报告 (`2026-06-28-docs-deep-audit.md`) 指出的 6 类严重问题，修复情况如下:

| # | 上次问题 | 状态 | 说明 |
|---|---------|------|------|
| 1 | 端口铁律错误 (5100 vs 5300) | ✅ 已修复 | 根 AGENTS.md、06-operations/README 已统一为 5300 |
| 2 | API 幽灵端点 (Sync + Users/Patients/MC) | ✅ 已修复 | Sync 加了 v2.0 声明；restore/batch-enable 端点已补齐到代码 |
| 3 | 权限策略错配 (Registration) | ⚠️ 未确认 | 本评估未深入验证 Registration 权限标注 |
| 4 | 架构文档幻影内容 (RefreshToken 等) | ⚠️ 部分修复 | `13-error-handling-flow.md` 加了过期警告；ADR-0008 仍提到 RefreshToken 但 ADR 本身记录历史决策 |
| 5 | US 总数三重矛盾 (128/136/138) | ✅ 已修复 | 统一为 141 |
| 6 | 配置三文档与 appsettings 失配 | ⚠️ 未确认 | 本评估未深入验证配置键名 |

**结论**: 上次审计的主要问题已修复约 60%，剩余 40% 需进一步确认或修复。

---

## 六、亮点（做得好的方面）

| 方面 | 评价 |
|------|------|
| **文档分层清晰** | 01-product → 06-operations 六层递进，每层 README 索引完整 |
| **v2.0 标注规范** | Sync、Patients 导入等延期功能均用「v2.0 规划」明确标注 |
| **ADR 规范** | 17 条 ADR 基本遵守 Context/Decision/Consequences 三段式，含变更记录 |
| **术语铁律** | `03-glossary.md` 的三条铁律 (Consultation/MedicalCase/Formula) 强制约束全项目 |
| **「文档权威」原则** | 文档作为目标态，代码差异标注 `⚠️ 代码待对齐` 或 `🧲 v1.0 待实现` |
| **上次审计响应快** | 一个月内修复了 D 级报告中的多数 P0 问题 |
| **training 文档** | 医案工作区 UX 培训指南面向临床医师，结构清晰，718 行内容完整 |
| **02-requirements 追溯矩阵** | `13-traceability-matrix.md` 覆盖 141 US → ADR/Flow/API/实现，工程化程度高 |

---

## 七、优先级建议

| 优先级 | 问题 | 工作量 | 影响 |
|--------|------|--------|------|
| **P0** | 清理 3 处明文凭证（SA 密码 + SSH 密码 + JWT SecretKey），考虑轮换 SecretKey | 小 | **安全风险** |
| **P0** | 修正 AccessToken 有效期描述（5 处：02-auth/01-auth/10-variables/11-variables/00-summary → 配置读取 480/30） | 小 | 事实错误 |
| **P0** | 统一 ADR 计数 (14/18 → 17) | 小 | 跨文档矛盾 |
| **P1** | 更新 `compose/README.md` 统计数（偏差 3-6 倍） | 小 | 索引失真 |
| **P1** | 修正 `03-server.md` CQRS 架构滞后 + SaveAsDraft→Suspend | 小 | 架构文档失真 |
| **P1** | 统一数据库文件路径 (APPDATA/LOCALAPPDATA) + DB 名称 (LYBTDB/LYBTDB_Dev) | 小 | 跨文件矛盾 |
| **P1** | 修正 PolicyConstants 策略数（4→5，补 DoctorOrAdminOrReceptionist） | 小 | 完整性 |
| **P1** | 统一 Receptionist 药材访问权限描述（以代码为准） | 小 | 跨分区矛盾 |
| **P1** | 修正连接池配置 (100→20) + Desktop 分发方式 (ClickOnce→Velopack) | 小 | 配置事实错误 |
| **P2** | compose 活跃区批量归档（约 115 份） | 中 | 目录治理 |
| **P2** | 修复 compose/reports 8 份命名违规 + specs 1 份 | 极小 | 命名规约 |
| **P2** | 清理 6 个 wpftmp 临时文件 | 极小 | 仓库整洁 |
| **P2** | 确认 `DeployController` 是否需 API 文档 | 小 | 完整性 |
| **P2** | 归档 `13-error-handling-flow.md` 到 archive/ | 极小 | 减少误导 |
| **P3** | 评估 6 个草稿状态优化文档去留 | 中 | 架构文档整洁 |
| **P3** | 05-development 测试文档 4 份 (05/12/13/14) 分工模糊 | 中 | 可读性 |

---

## 八、方法论说明

本次评估采用 **3 路并行 subagent 分区深读 + 主评估人交叉抽查**:

1. **Subagent A**: 01-product + 02-requirements + 06-operations (37 files, 528K)
2. **Subagent B**: 03-architecture + 04-api-reference (68 files, 724K)
3. **Subagent C**: 05-development + compose + plans + training (242 files, 3M)

每个 subagent 通读分区内所有文件，对关键事实（端口、版本号、API 路径、策略名）用 `read_file`/`search_files` 抽查代码验证。主评估人同步做主题交叉验证（ADR 编号、AccessToken 有效期、Receptionist 模块位置等），最终汇总成本报告。

**局限**: 未逐行核对所有 351 个文件，部分发现基于抽查；未深入验证权限策略标注与代码 `[Authorize]` 属性的逐条对齐；未验证配置键名与 `appsettings.json` 的完整对账。这些可在后续专项审计中补齐。

---

*报告生成于 2026-08-01 by AI Agent (技术总监角色)*
*评估范围: docs/ 全量 + 代码交叉验证*
*对比基准: `docs/compose/reports/2026-06-28-docs-deep-audit.md`*