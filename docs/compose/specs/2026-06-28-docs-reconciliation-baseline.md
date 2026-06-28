# 文档对齐决策基线（v1.0 范围冻结）

> **日期**：2026-06-28
> **状态**：✅ 已冻结 — 文档体系一致性修复的唯一依据
> **来源**：`prd-code-reconciliation`（D1-D10）+ `scenario-functional-map`（Sync/N1）+ `shell-requirements-round1` + 用户逐项确认（SHELL 扩展 / N1 / 权限原则）
> **配套**：审查报告 `docs/compose/reports/2026-06-28-docs-deep-audit.md`
> **原则**：**文档是设计权威**，保留目标态；代码与文档不符 → 文档不动，记为「代码待对齐」修复任务。

---

## 1. v1.0 功能范围决策（D1-D10，已冻结）

| 簇 | 主题 | 决策 |
|---|------|------|
| D1 | 医案审计日志（MC-017） | **A 补回 v1.0**（实体+Service+/audit-logs 端点） |
| D2 | 打印保护/回写（PRINT-004 + IsPrinted/PrintVersion/PrintCount/LastPrintedAt） | **A 补回 v1.0** |
| D3 | Auth 安全模型 | **B+**：Token 族旋转+限流+登出撤销+审计日志补 v1.0；重放检测 v2.0 |
| D4 | Restore 软删除恢复（Users/Patients/Herbs/Formulas） | **A 补回 v1.0**（基础设施已就绪） |
| D5 | 引用检查 BR-DEL-001（Patients 单删 / Herbs） | **A 必做** |
| D6 | Excel 导入导出 | **Desktop Excel + API JSON**：Herbs 补 Excel；Patients 导入标 v2.0 |
| D7 | 权限策略错配 | **A 按文档(PRD)修代码**（见 §3） |
| D8 | P0 数据/安全 Bug（7 个） | **A 全修** |
| D9 | 历史聚合查询（MC-008/009） | **A 补回 v1.0** |
| D10 | 字段级加密 | **拉回 v1.0** |

## 2. Sync 模块 = v2.0

- `scenario-functional-map` S3 明确 Sync 延期 v2.0。
- **N1 决策（用户 2026-06-28）**：**v1.0 远程库与本地库数据孤立，不互通**。本地模式定位为"远程故障应急降级"，断网期录入的数据事后手动补录或可丢。
- 文档处理：`01-vision.md`/`01-prd.md` 把 Sync 移出 v1.0（改"9 模块"，删 Sync 行）；清除 4 处死链（`07-medical-cases.md`、`02-desktop.md`×2、`04-api-reference/09-sync.md`）；`09-sync.md` 标注「v2.0 规划，v1.0 不实现」；`05-dual-mode.md` 明确写"v1.0 双模式数据孤立，同步属 v2.0"。

## 3. 权限语义 = 文档权威（D7）

代码现状与 PRD 不符，**修代码而非改文档**。API 文档保留目标策略，加「⚠️ 代码当前为 X，待对齐」标注：

| 模块 | 文档目标（权威） | 代码现状（待修） |
|------|------------------|------------------|
| 挂号（创建/取消） | `DoctorOrReceptionist` | `DoctorOrAdmin` |
| 患者 | `DoctorOrReceptionist` | `DoctorOrAdmin` |
| 药材 | `DoctorOrReceptionist` | `DoctorOrAdmin` |
| 医案创建 | `Doctor-only` | `DoctorOrAdmin` |

## 4. SHELL 扩展 US 判定（用户 2026-06-28 逐项确认）

| US | 标题 | 归属 |
|----|------|------|
| SHELL-010 | Desktop 安装（Velopack） | **v1.0** |
| SHELL-011 | 首次初始化向导 | **v1.0** |
| SHELL-012 | Desktop 自动更新 | **v2.0** |
| SHELL-013 | 数据库备份恢复 | **v1.0**（含 015 合并：状态展示+手动备份按钮并入） |
| SHELL-014 | 安全审计日志查看 | **v1.0** |
| SHELL-015 | 备份状态与手动备份 | **撤销，并入 013** |
| SHELL-016 | 配置导出/导入 | **v1.0**（3 台客户端分发场景） |
| SHELL-017 | 生产环境安全门控 | **v1.0**（服务端已实现） |
| SHELL-018 | sysadmin 配置中心 | **v1.0** |
| SHELL-019 | 读卡器诊断测试工具 | **v1.0**（前台建档入口需自检） |

## 5. US 总数

- **v1.0 = 141**：AUTH13 + USER12 + PAT13 + HERB13 + FORM13 + **MC19** + **REG8** + PRINT4 + **REPORT3** + **Platform43**
- **Platform v1.0 = 43**：原声明 35（Shell5+CFG4+ERR8+LOG7+SYS9+CARD2）+ 隐身 v1.0 的 8（010/011/013/014/016/017/018/019）
- v2.0：SHELL-012（1）+ Sync 整模块
- 所有声明 128（`02-requirements/README`、`01-vision`、`01-prd`）与 136（根 README、docs/AGENTS）之处，统一为 **141**。

### 范围变更记录（恢复权威性）

本基线冻结时（2026-06-28）v1.0 = **136**（MC18 + REG7 + PRINT4 + Platform43，9 模块，不含 REPORT）。冻结后三项新增经正式设计决策补入 v1.0，但未回写本节数字，属治理疏漏。现统一修正为 **141**，溯源如下：

| 增项 | US | 决策来源 | 补入模块 | 数量 |
|------|----|---------|---------|:---:|
| 复用上次处方微调 | US-MC-019 | D6（Excel 导入导出决策的处方复用分支） | 医案管理 | +1（MC 18→19） |
| 医生工作台实时推送 | US-REG-008 | R10（挂号 spec，SignalR Hub） | 挂号管理 | +1（REG 7→8） |
| 报表管理（收入/就诊/药材排行） | US-REPORT-001/002/003 | A7（报表清单设计落地） | 报表管理（新模块） | +3（新模块行） |

> 三项均「五要素齐全」（US 编号 + ADR/Flow/访谈点 + 决策记录 + 追溯矩阵行 + 实现参考）。baseline 的 136 为补入前冻结快照，141 为补入后当前权威值。模块数 9→10（REPORT 独立成模块，对应 `10-reports.md`）。

## 6. 技术事实（代码为真相源）

| 项 | 真相 | 证据 |
|----|------|------|
| LocalWebAPI 嵌入端口 | **5300** | `EmbeddedLocalWebApiService.cs:17` + `appsettings.json:47`（OfflineMode:LocalApiBaseUrl） |
| LocalWebAPI 独立调试端口 | 5290 | `LocalWebAPI/Program.cs:5`（仅独立运行，嵌入模式不生效） |
| 远程 WebAPI | 5000 | `appsettings.json:3`（ApiClient:BaseUrl） |
| `/health/database` | **存在**（仅 Server） | `UnifiedMiddlewareConfiguration.cs:154`（MapHealthChecks 中间件）；LocalWebAPI 无 |
| health 控制器端点 | `/health`、`/health/ping`、`/health/details`（[Authorize]） | `HealthController.cs`（Server + LocalWebAPI 各一份） |
| 技术栈版本 | 以 `Directory.Packages.props` 为准 | Prism 8.1.97 / BCrypt 4.1.0 / FluentValidation 12.1.1 / EF Core 8.0.26 / MDIX 5.3.2 / Mapperly 4.3.1 |
| Desktop 测试库 | **SQL Server LocalDB**（非 SQLite InMemory） | `tests/LYBT.Tests.Desktop` 全部 `UseSqlServer("(localdb)\MSSQLLocalDB...")` |
| 配置真值 | 以 `appsettings.json` 为准 | Jwt:Issuer=`LYBT.WebAPI`；连接串 key=`ConnectionStrings:DefaultConnection`；Session 节=`Session:TimeoutMinutes`（Shell 用 `ClientSession:InactivityTimeoutMinutes=30`） |
| ApiResponse 字段 | `success/message/data/errors/timestamp/requestId`（**无 code**） | `ApiResponse.cs` |
| WebAPI 部署 | **有公网部署需求** | 用户 2026-06-28 确认；印证 D3 B+ 安全方案 |

## 7. 文档处理原则（修复时遵循）

1. **文档 = 设计权威**：保留目标态描述，不为迁就代码而改业务语义。
2. **代码未实现但属 v1.0 的功能**（D1/D2/D4/D9 补回项 + 幽灵端点 + 幻影实体 RefreshToken/AuditLog/PrintLog）：文档**保留设计**，加标注 `🚧 v1.0 待实现`，不删除。
3. **代码与文档不符**（权限错配 D7、端口历史误记）：文档按目标态/真值写，权限处加 `⚠️ 代码当前为 X，待对齐（D7）`。
4. **v2.0 项**（Sync、SHELL-012、重放检测、Patients 导入）：明确标注 `v2.0 规划`，从 v1.0 范围移除。
5. **撤销项**（SHELL-015）：并入 013，独立 US 删除，总览计数调整。
6. **数字一致性**：US 总数=141、Platform=43、ADR=12、端口=5300，全仓统一。

## 8. 修复批次（执行序）

| 批 | 范围 | 确定性 |
|----|------|--------|
| A | 技术事实对齐：端口 5300、版本号、Desktop 测试库 LocalDB、配置三文档对齐 appsettings、ApiResponse 字段、health 端点澄清 | 高（代码真值） |
| B | 范围/计数对齐：US=141、Platform=43、Sync v2.0、SHELL 判定落地、015 并入 013、N1 数据孤立描述 | 高（决策已冻） |
| C | 目标态保留 + 待对齐标注：权限标注、幽灵端点标待实现、幻影实体标待实现、ADR-0008 标注 | 中（遵循原则） |
| D | 索引与治理：docs/AGENTS.md 刷新、各 README 导航补全、compose/README 新建、docs/plans 弃用、CONTRIBUTING 修正 | 高 |
| E | 次要清理：编号格式、版本同步、措辞、行号、变更记录 | 低 |
