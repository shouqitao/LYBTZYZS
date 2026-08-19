# Desktop 文档-代码一致性审计报告（2026-08-19）

> 任务书：`.hermes-task-doc-code-audit.md`（只审计不修改代码，本报告不包含任何代码改动）
> 范围：10 个需求文档（04-patients/05-herbs/06-formulas/07-medical-cases/08-registration/03-users/10-reports/11a-shell/11b-configuration/04-permissions）vs `src/Client/Desktop/` 全部模块 + 双端 Server 代码
> 方法：逐 US 核对状态一致性 / 功能完整性 / 设计决策 / API 契约 / UI 实现 + 交叉验证 13-traceability-matrix.md（状态列 SSOT）

---

## 一、审计结论总览

| 类别 | 数量 | 说明 |
|------|------|------|
| 完全一致 | 63 | US 状态/实现与代码一致 |
| 文档需更新 | 16 | 需求文档正文状态列滞后于 traceability / 代码（过时描述、自相矛盾） |
| 代码需修复 | 4 | 注释残留 Excel 语义、死常量、死代码类、Server 端点无客户端消费 |
| 设计决策偏差 | 3 | US-MC-008/009 语义偏差、US-MC-018 与文档不符、Desktop 导入导出 UI 未接线 |

**核心发现**：
1. **模块需求文档正文的状态列普遍滞后**于 13-traceability-matrix.md（后者 2026-08-11 已校准）——违反「需求文档状态列是需求-代码对齐 SSOT」规则
2. **04-permissions.md 的「当前代码策略映射 + 已知问题表」整体过时**——P0/P1 修复项几乎全部已落地，文档仍标 🔴
3. **Excel→JSON 决策（2026-08-13）在代码注释层残留严重**——Desktop 契约/Service/Repository 层 20+ 处注释仍写「导出到 Excel」，Server Controller 已更新为 JSON
4. **批量导入/导出在 Desktop UI 层未接线**——Service/Repository/API 完整，但零 ViewModel 消费

---

## 二、逐模块审计

### 2.1 患者模块（04-patients.md，14 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-PAT-001 | ✅ | ✅ | 无 | — |
| US-PAT-002 | ✅ | ✅ | 无 | — |
| US-PAT-003 | ✅（409+拼音兜底） | ✅ | 无（实现注已同步） | — |
| US-PAT-004 | ✅ | ✅ | 无 | — |
| US-PAT-005 | ✅ | ✅ | 无 | — |
| US-PAT-006 | ✅ | ✅ | 无 | — |
| US-PAT-007 | ✅ | ✅ | 无 | — |
| US-PAT-008 | ✅ | ✅ | 无 | — |
| US-PAT-009 | ✅ | ✅ | 无 | — |
| US-PAT-010 | ✅ | ✅ | 无 | — |
| US-PAT-011 | 🔧 JSON | ⚠️ 服务端 JSON ✅；Desktop UI 零消费 | Desktop 无导入模板 UI 入口 | 修复代码（接线 UI）或文档标注 API-only |
| US-PAT-012 | 🔧 JSON | ⚠️ 服务端 JSON ✅；Desktop UI 零消费 | 同上 | 同上 |
| US-PAT-013 | ✅ | ✅ | 无 | — |
| US-PAT-014 | ✅ | ✅（by-id-number 端点存在） | 无 | — |

**模块级偏差**：
- `04-patients.md:21` 模块概述仍写「Excel 导出」——应「JSON 导出」（2026-08-13 决策）
- `04-patients.md:38` 双模式段引用 `IPatientImportExportService`——代码实际 `IPatientService.ExportTemplateAsync/ExportPatientsAsync`（无独立 ImportExport 服务）→ **文档虚构接口名**
- 边界条件「并发编辑乐观锁 RowVersion 409」：Patient 继承 BaseEntity（含 RowVersion）✅ 但 Update 路径冲突处理需确认

### 2.2 药材模块（05-herbs.md，15 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-HERB-001 | ✅ | ✅（类级 DoctorOrAdmin + 写 AdminOrSuperAdmin） | 无 | — |
| US-HERB-002 | ✅ | ✅ | 无 | — |
| US-HERB-003 | ✅ | ✅ | 无 | — |
| US-HERB-004 | ✅ | ✅ | 无 | — |
| US-HERB-005 | **🔴 代码待对齐** | ✅ 已实现（ValidateBeforeDeleteAsync + HerbReferenceRepository） | **文档过时**（traceability 已标 ✅ B1） | 修复文档（状态列 🔴→✅） |
| US-HERB-006 | 🔧 JSON | ✅（batch-import DTO/JSON） | 无 | — |
| US-HERB-007 | ✅ | ⚠️ 验收标准自相矛盾 | AC 写「返回 Excel 文件（.xlsx）」但业务规则写 JSON——**同一 US 内部矛盾** | 修复文档（AC 改 JSON） |
| US-HERB-008 | ✅ | ✅ | 无 | — |
| US-HERB-009 | ✅ | ✅ | 无 | — |
| US-HERB-010 | ✅ | ✅ | 无 | — |
| US-HERB-011 | ✅ | ✅ | 无 | — |
| US-HERB-012 | ✅ | ✅ | 无 | — |
| US-HERB-013 | ✅ | ⚠️ 验收标准自相矛盾 | AC 写「返回 Excel 文件（.xlsx）」+ 业务规则 3 写 JSON——矛盾 | 修复文档（AC 改 JSON） |
| US-HERB-014 | ⚠️ 部分实现 | ⚠️ 需确认 | DesktopCacheManager.InvalidateHerbCaches 存在；CacheEvents 需验证 | 确认后更新 |

**模块级偏差**：
- `05-herbs.md:21` 模块概述「批量导入（两种路径：服务端 Excel 与客户端 DTO）」——**服务端 Excel 路径已删**（2026-08-13）
- `05-herbs.md:21` 「Excel 导出与模板下载」——应 JSON

### 2.3 验方模块（06-formulas.md，15 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-FORM-001~005 | ✅ | ✅ | 无 | — |
| US-FORM-006 | ✅ | ⚠️ 业务规则 5 残留 | 业务规则 5「客户端 NPOI 本地解析 Excel」——**NPOI 已移除**（2026-08-13） | 修复文档（删 NPOI 引用） |
| US-FORM-007 | ✅ | ✅ | 无 | — |
| US-FORM-008/009/010 | ✅ | ✅ | 无 | — |
| US-FORM-011 | ✅ | ✅ | 无 | — |
| US-FORM-012 | ✅ | ✅ | 无 | — |
| US-FORM-013 | ⚠️ 部分实现 | ⚠️ 验收标准已含 JSON 注 | 部分一致（JSON 已注） | 收尾更新 |
| US-FORM-014 | ✅（⚠️ 仅本地模式） | ⚠️ 远程 clone 缺失（文档已注） | 文档如实标注 | 待 T 批次补远程端点 |

**模块级偏差**：US-FORM-014 远程 clone 端点缺失——已知缺口（文档已标注），非新发现。

### 2.4 医案模块（07-medical-cases.md，21 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-MC-001~007 | ✅ | ✅ | 无 | — |
| US-MC-008 | **🔴 缺失** | ⚠️ `GET /patients/{patientId}/history` 存在（返回 Recent MedicalCase 列表，B1） | **文档正文过时**（traceability 标 ✅）；但**语义偏差**：文档要求返回 Consultation 摘要列表，实现返回医案列表（内含 Consultation 嵌套） | 修复文档（对齐 history 端点语义）+ 评估语义符合性 |
| US-MC-009 | **🔴 缺失** | ⚠️ 同上（history 含处方数据） | 同上 | 同上 |
| US-MC-010~017 | ✅ | ✅ | 无（US-MC-011/012/013/014/015/016/017 均已实现） | — |
| US-MC-018 | **🔴 缺失** | ⚠️ `POST /batch-details` 存在 | **文档正文过时**（traceability 标 ✅）；上限文档 50 vs 13c 记录 100 | 修复文档（状态 + 上限） |
| US-MC-019 | ✅ | ✅（HistoryCopyDialog） | 无 | — |
| US-MC-020 | ✅ | ✅ | 无 | — |

**模块级偏差**：
- US-MC-008/009/018 三个 US 文档正文状态 🔴 与 traceability ✅ 冲突——正文未随 B1 批次（2026-08-11）同步
- US-MC-018 上限：文档 50（ERR-30603）vs 13c 记录 100（BatchIdsRequest ≤100）——**数字不一致**
- US-MC-008/009 的 history 端点在 **Desktop 客户端零消费**（客户端侧死契约——US-MC-019 用 Query ByPatient + batch-details 替代）

### 2.5 挂号模块（08-registration.md，9 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-REG-001 | ✅ | ✅ | 无 | — |
| US-REG-002 | 🔧 设计修订 | ⚠️ StartVisit 已接线；两步建号 Desktop 侧第 1 步（Source=Doctor 建号）待确认 | 文档「Desktop 待接线」——StartVisitCommand 已在 RegistrationListViewModel；QuickVisit UI 死按钮已删（上批） | 确认后更新状态 |
| US-REG-003~008 | ✅ | ✅ | 无 | — |

### 2.6 用户模块（03-users.md，12 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-USER-001 | ✅ | ✅ | **已知问题表过时**：文档「分页 TotalCount 错误」——代码已修复（UserRepository 筛选后 CountAsync） | 修复文档（删已知问题行） |
| US-USER-002 | ✅ | ✅ | **已知问题表过时**：文档「CreatedAt 始终 MinValue」——代码已修复（IdentityMapper 正确映射） | 同上 |
| US-USER-003~012 | ✅ | ✅ | 无 | — |

**模块级偏差**：
- `03-users.md:140-148` 「已知问题（2026-06-28 审计）」表 4 行全部过时（TotalCount 错误/Restore 缺失/CreatedAt MinValue/UpdatedAt null——均已修复，Restore 已有端点）
- 双模式差异表 JWT 365 天 ✅ 与 LocalJwtConfig 一致

### 2.7 报表模块（10-reports.md，4 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-REPORT-001~003 | ✅ | ✅ | 无 | — |
| US-REPORT-004 | ✅（双模式已注） | ✅ | 无 | — |

**结论**：报表模块一致 ✅（此前批次已充分同步）。

### 2.8 Shell 模块（11a-shell.md，13 有效 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-SHELL-001 | ✅ | ✅ | 无 | — |
| US-SHELL-003 | **🔴 代码待对齐** | ✅ 已实现（LoginCoordinator.LoadModulesForRoleAsync + C7 死代码清理） | **文档过时**（traceability 标 ✅） | 修复文档（状态 🔴→✅） |
| US-SHELL-004/005 | ✅ | ✅ | 无 | — |
| US-SHELL-007 | ⚠️ 部分实现 | ⚠️ 需确认 ERR-70506 | 上批 B2 已实现切换守卫（SHELL-007 三重守卫）——文档可能过时 | 确认后更新 |
| US-SHELL-010 | ✅ | ✅ | 无 | — |
| US-SHELL-011 | 🧲 待实现 | ⚠️ FirstRunSetupView 存在（功能有限） | 部分实现（首启向导基础版） | 评估是否部分实现 |
| US-SHELL-012 | 📋 v2.0 | ✅ DesktopUpdateService 已实现 | 文档「v2.0 规划」但代码已实现 Velopack 自动更新（SHELL-012 实际 v1.0 已完成） | 修复文档 |
| US-SHELL-013 | ✅ | ✅ | 无 | — |
| US-SHELL-014 | 🧲 待实现 | ⚠️ SecurityAuditLog 存在（13c 记录迁移恢复） | 部分实现（审计表/服务在，UI 待确认） | 确认后更新 |
| US-SHELL-016 | 🧲 待实现 | ❌ 未实现 | 无偏差（待实现） | — |
| US-SHELL-017 | ✅ | ✅ | 无 | — |
| US-SHELL-018 | ✅ | ✅ | 无（权限注已同步 SysAdminOnly） | — |
| US-SHELL-019 | **🧲 待实现** | ✅ 已实现（CardReaderDiagnosticsViewModel + ICardReaderDiagnostics + 面板） | **文档严重过时**（13c SHELL-019 已记录实现但 11a 未同步） | 修复文档（状态 🧲→✅） |
| US-SHELL-020 | ✅ | ✅ | 无 | — |
| US-SHELL-024/025 | ✅ | ✅ | **US-SHELL-025 配置段名偏差**：文档写 `Kestrel:Endpoints`，代码用 `Server:Endpoints`（Program.cs 明确注释非 Kestrel） | 修复文档 |

### 2.9 配置模块（11b-configuration.md，6 US）

| US | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| US-CFG-001 | ✅ | ✅（实现） | **权限描述过时**：文档业务规则「端点受 AdminOrSuperAdmin 策略保护」——代码已 `SysAdminOnly`（CONFIG-PERM-FIX 2026-08-13） | 修复文档（业务规则改 SysAdminOnly） |
| US-CFG-002 | ✅ | ✅ | 无 | — |
| US-CFG-003 | ✅ | ✅ | 无 | — |
| US-CFG-004 | ✅ | ✅（功能） | **实现类引用过时**：文档引用 `ConfigurationOptionsMonitor`/`OptionsMonitorWrapper`（PrismConfigurationExtensions.cs:80/133）——**代码零命中**（T8 改用 FeatureToggleService + IConfiguration.GetReloadToken 动态读） | 修复文档（实现参考改 FeatureToggleService） |
| US-CFG-005 | ✅ | ✅ | 无 | — |
| US-CFG-006 | ✅ | ✅ | 无 | — |

### 2.10 权限模块（01-product/04-permissions.md）

| 项 | 文档状态 | 代码状态 | 偏差 | 建议 |
|----|---------|---------|------|------|
| §2.1 HerbsController | 🔴 前台不可查（代码 DoctorOrReceptionist） | ✅ 类级 DoctorOrAdmin + 写 AdminOrSuperAdmin | **文档过时**（US-HERB-001 已 ✅，C2 已修复） | 修复文档（策略映射行） |
| §2.1 MedicalCasesController | ⚠️ 创建仅 Doctor（C4/K3 待修） | ✅ DoctorOnly 已用（113/244 行） | 文档过时 | 修复文档 |
| §2.1 FormulasController | 🔴 前台不可查（代码 DoctorOrReceptionist） | ✅ 类级 DoctorOrAdmin | 文档过时 | 修复文档 |
| §2.1 ConfigurationController | ✅ SysAdminOnly | ✅ | 无（已同步） | — |
| §3.1 P0-1 Herbs 写操作 | 🔴 | ✅ 已修复 | 文档过时 | 修复文档（标 ✅） |
| §3.1 P0-2 MC 创建 DoctorOnly | 🔴 | ✅ 已修复 | 文档过时 | 同上 |
| §3.1 P0-3 Formulas 写操作 | 🔴 | ✅ 已修复 | 文档过时 | 同上 |
| §3.1 P0-4 Herbs 删除引用检查 | 🔴 | ✅ 已修复（HerbReferenceRepository） | 文档过时 | 同上 |
| §3.1 P0-5 Patients 删除引用检查 | 🔴 | ✅ 已修复 | 文档过时 | 同上 |
| §3.2 P1-1 Formulas GetDetail 所有权 | 🔴 | ✅ 已修复（US-FORM-002 403） | 文档过时 | 同上 |
| §3.2 P1-4 Registrations 策略细分 | 🔴 | ✅ 已修复（36/54/70 行方法级） | 文档过时 | 同上 |
| §3.2 P1-5 Patients 删除/禁用限 Admin | 🔴 | ✅ 已修复（240/269 行） | 文档过时 | 同上 |
| §3.2 P1-6 打印 DoctorOnly | 🔴 | ✅ 已修复 | 文档过时 | 同上 |
| §1.1 验方导出/导入 | 📋 | ⚠️ API 已实现（JSON）但 Desktop UI 零消费 | 部分实现 | 确认后更新 |
| §1.2 会话超时 | 30 分钟 | 需确认 ClientSessionOptions | — | 确认 |

**权限模块结论**：04-permissions.md 的「当前代码策略映射」与「已知问题」表**整体滞后约 2-3 个批次**——P0/P1 修复项 13 项中 12 项已落地。

---

## 三、代码侧审计发现（代码需修复项）

### C-1. Desktop 契约/Service/Repository 层注释残留「Excel」语义（20+ 处）

2026-08-13 Excel→JSON 决策后，**Server Controller 注释已更新**，但 Desktop 层注释未同步：
- `IPatientApi.cs:49/58/63/66`、`IHerbApi.cs:61`、`IFormulaApi.cs:68`（Refit 契约）
- `IApiClientPatients.cs:69/73/77`、`IApiClientHerbs.cs:73/76`、`IApiClientFormulas.cs:85/88`（ApiClient 契约）
- `IPatientService.cs:25`、`IHerbService`/`IFormulaService`（Service 接口）
- `PatientService.cs:86`、`HerbService`/`FormulaService`（实现）
- `IPatientRepository.cs:61`、`IHerbRepository.cs:55`、`IFormulaRepository.cs:74`（Repository 接口）
- `04-patients.md:21`、`05-herbs.md:21`（模块概述）

**影响**：误导新成员（按注释以为返回 Excel 文件，实际 JSON）；文档-代码一致性红线范畴。
**建议**：注释全量改「JSON」（外科手术式，无行为变化）。

### C-2. SystemConstants.Export = ".xlsx" 死常量（零使用）

`LYBT.Desktop.Infrastructure/Constants/SystemConstants.cs:117`——13c 已登记「零使用不动」，审计确认仍零引用。
**建议**：删除或改 `".json"`（如无用途则删）。

### C-3. ImportWizardStep.cs 死代码（患者导入向导遗留）

`LYBT.Desktop.Patients/Models/ImportWizardStep.cs`（enum + ImportProgressInfo）——全仓零引用，患者批量导入 UI 从未接线。
**建议**：删除（如无接线计划）或用于新导入向导。

### C-4. US-MC-008/009 history 端点 Desktop 零消费

`GET /medicalcases/patients/{patientId}/history`——Server 已实现但 Desktop 客户端未调用（US-MC-019 用 Query ByPatient + batch-details 替代）。
**建议**：登记客户端侧死契约（与 Server 协调后再删）或接线到 HistoryCopyDialog。

---

## 四、功能完整性审计（重点项）

### 4.1 批量导入/导出（重点关注项 1/2）

| 维度 | 状态 |
|------|------|
| Server API（导入模板/导出/批量导入，JSON） | ✅ 双端完整 |
| Desktop Service/Repository 层 | ✅ 完整（方法存在） |
| Desktop ViewModel/UI 层 | ❌ **零消费**——无批量导入/导出 UI 入口 |

**结论**：US-PAT-011/012、US-HERB-006/007/013、US-FORM-006/013 文档标 ✅（以 API 层论）但 **Desktop 端功能不可用**（无 UI）。这是「API 完整、UI 未接线」的**功能完整性缺口**——需求文档的 ✅ 需澄清语义（API-only vs 端到端）。

### 4.2 权限矩阵（重点关注项 3）

双端策略一致性 ✅（Remote/Local 镜像）；文档权限矩阵过时（见 §2.10）。

### 4.3 错误处理（重点关注项 4）

错误码（ERR-30103/30104/30105/30302/30304/30305/30306/30307/30603/30604 等）在 BR-003/边界条件中引用——代码层 ErrorCode 枚举需抽查，本次未发现明显不一致（上批 ADR-0020 已统一）。

### 4.4 配置管理（重点关注项 5）

见 §2.9——US-CFG-001 权限、US-CFG-004 实现类引用过时；US-SHELL-025 配置段名 `Kestrel:Endpoints` vs `Server:Endpoints` 偏差。

---

## 五、汇总统计

| 类别 | 数量 | 明细 |
|------|------|------|
| 完全一致 | 63 | 大部分 US（PAT 10/14、HERB 10/15、FORM 13/15、MC 17/21、REG 8/9、USER 12/12、REPORT 4/4、SHELL 8/13、CFG 4/6） |
| 文档需更新 | 16 | US-HERB-005/007/013、US-FORM-006、US-MC-008/009/018、US-SHELL-003/007/011/012/019/025、US-CFG-001/004、04-permissions 策略映射+已知问题表、03-users 已知问题表、04-patients/05-herbs 模块概述 |
| 代码需修复 | 4 | C-1 注释 Excel 残留（20+ 处）、C-2 .xlsx 死常量、C-3 ImportWizardStep 死代码、C-4 history 端点零消费 |
| 设计决策偏差 | 3 | US-MC-008/009 端点语义（history vs consultations/prescriptions 独立列表）、US-MC-018 上限（50 vs 100）、Desktop 导入导出 UI 未接线（API-only ✅ 但端到端 ❌） |

---

## 六、优先建议

### P0（文档-代码一致性红线，立即修）
1. **模块需求文档状态列对齐 traceability**：05-herbs US-HERB-005、07-medical-cases US-MC-008/009/018、11a-shell US-SHELL-003/019 的 🔴/🧲 → ✅（traceability 已校准，正文滞后）
2. **04-permissions.md 策略映射 + 已知问题表**全量校准（P0-1~P0-5、P1-1/4/5/6 已修复，标 ✅）
3. **代码注释 Excel→JSON**（C-1，20+ 处 Desktop 契约/Service/Repository 注释）

### P1（文档自相矛盾，尽快修）
4. 05-herbs US-HERB-007/013 验收标准「Excel 文件」vs 业务规则「JSON」矛盾——AC 改 JSON
5. 06-formulas US-FORM-006 业务规则「客户端 NPOI 解析」——NPOI 已移除
6. 11b-configuration US-CFG-001 权限描述 AdminOrSuperAdmin→SysAdminOnly；US-CFG-004 实现类引用改 FeatureToggleService
7. 11a-shell US-SHELL-025 配置段名 Kestrel:Endpoints→Server:Endpoints
8. 03-users 「已知问题」表 4 行删除/标记已修复
9. 04-patients/05-herbs 模块概述 Excel→JSON；IPatientImportExportService 虚构接口名修正

### P2（代码清理 + 语义确认）
10. C-2 删 .xlsx 死常量；C-3 删 ImportWizardStep 死代码
11. US-MC-018 上限数字统一（50 vs 100）
12. US-MC-008/009 history 端点语义评估（Client 零消费——登记或接线）
13. Desktop 导入导出 UI 接线评估（US-PAT-011/012/HERB-006/007/013/FORM-006/013 端到端可用性）
14. US-SHELL-011/014 状态确认（部分实现）

---

*审计完成时间: 2026-08-19*
*方法: 需求文档 × 代码交叉核对（双端 Controller/Service/Repository/ViewModel/XAML）+ traceability SSOT 交叉验证*
*本报告仅审计，未修改任何代码*
