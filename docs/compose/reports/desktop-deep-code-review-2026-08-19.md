# Desktop 深度代码审查报告（2026-08-19）

> 任务书：`.hermes-task-desktop-deep-review.md`（本报告完成时已删除）
> 范围：基于最新扁平化需求文档 + 追溯矩阵 WebAPI/Desktop 分列 + 本批次新增代码（批量导入/导出 UI，commit `f5e0b4236`）对 Desktop 代码进行深度审查
> 性质：**只审查不修改代码**——本报告不包含任何代码改动；修复项以「七、修复任务书」交付

---

## 一、审查结论总览

| 类别 | 数量 | 严重度 | 摘要 |
|------|------|--------|------|
| 功能破损（双模式） | 2 | **P1** | 药材「导出」双模式 404（Remote 路由名错位 + Local 无端点）；药材「模板下载」Local 模式 404（Local 无端点） |
| 模板/导入 DTO 形状不符 | 1 | **P1** | 验方模板示例（Herbs 为字符串）与导入 DTO（List\<对象\>）不一致；模板 Fields 含 DTO 不存在的 Category |
| 权限口径不一致 | 1 | **P2** | 需求文档要求 Admin 级，代码实际 DoctorOrAdmin（类级继承），模块间也不一致 |
| 导出文件结构与文档不符 | 1 | **P2** | 导出保存的是 ApiResponse 包装体而非「JSON 数组」；导出→导入回灌（迁移场景）不可行 |
| 导出筛选参数错位/缺明细 | 2 | **P2** | 验方导出客户端传 `category` 服务端读 `keyword`；导出不含药材组成明细（US-FORM-013 验收不满足） |
| 分类搜索断裂（既有） | 1 | **P2** | `SearchText="分类:xxx"` 当 keyword 传服务端永不命中；导出继承同一缺陷 |
| UI 打磨 | 5 | **P3** | 导入无 busy 门控 / ImportJsonOptions 三处重复 / 导入前无大小预检 / SaveFileDialog 无 DefaultExt / 导出提示字节数 |
| 架构合规 | 0 | — | 分层链、DI、Mapperly、无越层 **全部合规**（架构守卫 87/87） |
| 安全 | 0 | — | 文件对话框/JSON 反序列化/10000 上限/敏感字段脱敏 **基本良好**（客户端预检缺失记 P3） |
| 文档一致性 | 6 | P1×1 + P2×5 | 追溯矩阵 HERB-007/013 Desktop ✅ **失真**；需求文档 Excel 残留/状态列滞后/虚构接口/AllowAnonymous 矛盾；架构文档双端表述失实 |

**核心结论**：本批次 UI 接线（命令/XAML/错误处理/风格）质量良好且构建 0/0、架构 87/87 已验证；但**「接线完成」不等于「功能可用」**——药材导出/模板在双模式下路由断裂，追溯矩阵在未真机验证的情况下将 HERB-007/013 乐观标注为 Desktop ✅（违反「状态列是需求-代码对齐 SSOT」纪律）。修复方向：先过需求（权限口径、导出文件结构、验方模板 DTO 对齐），再改代码 + 补双端路由守卫测试。

---

## 二、本批次新增代码审查（commit f5e0b4236）

> 改动文件：PatientMasterDetailViewModel / HerbMasterDetailViewModel / FormulaMasterDetailViewModel（各 +3 命令）、Patient/Herb/FormulaMasterDetailControl.xaml（工具栏按钮）、UserMasterDetailControl.xaml（删死绑定）、追溯矩阵 v1.9、接线报告 desktop-batch-import-export-2026-08-19.md

### 2.1 ViewModel 命令实现 ✅

三个 VM 各新增 3 命令，模式统一且实现正确：

| 命令 | 实现 | 评价 |
|------|------|------|
| `DownloadImportTemplateAsync` | Service.ExportTemplateAsync → SaveFileDialog → WriteAllBytesAsync | ✅ 正确 |
| `ImportXxxAsync` | OpenFileDialog → ReadAllTextAsync → Deserialize → 空数据校验 → 确认框 → BatchImportAsync → 结果框 → 缓存失效 + RefreshAsync | ✅ 正确（含 JsonException 单独捕获） |
| `ExportXxxAsync` | Service.ExportXxxAsync → SaveFileDialog → WriteAllBytesAsync | ✅ 正确（患者/药材带 SearchText 筛选） |

- RelayCommand 生成器产物齐全（3 VM 共 9 个 .g.cs 均生成）→ 命令名与 XAML 绑定一致 ✓
- 服务调用全部经 Service 接口（IPatientService/IHerbService/IFormulaService），契约层方法签名（ExportTemplateAsync/BatchImportAsync/ExportPatientsAsync/ExportHerbsAsync/ExportFormulasAsync）与 Repository/IApiClient 链完整 ✓
- 结果对话框正确消费 DTO 字段：患者/药材（SuccessCount/FailureCount/SkippedCount + Strategy）、验方（SuccessCount/FailureCount/MatchedHerbsCount——验方 DTO 无 SkippedCount 故消息一致）✓

### 2.2 XAML 绑定 ✅

- 三个 Control 工具栏 `ExportCommand` 绑定（ExportPatientsCommand/ExportHerbsCommand/ExportFormulasCommand）+ AdditionalContent「模板/导入」按钮全部命中真实命令（DataGridToolbar 的 ExportCommand 空值自动隐藏按钮）✓
- `UserMasterDetailControl.xaml` 删除 `ExportCommand="{Binding ExportCommand}"` 死绑定——User 无导出 API（US-USER-012 仅批量删除/启用/禁用，无导出需求；IUserService 零导出方法实证）→ 处理正确 ✓

### 2.3 错误处理 ✅（详见安全性 §四）

### 2.4 双模式兼容 ❌ **关键发现（P1 × 2）**

本批次把 Service/Repository 层既有 API 接线到 UI，暴露了**契约-端点路由错位**（潜伏缺陷转用户可见）：

**F1 药材「导出」双模式 404（P1）**

| 层 | 位置 | 路径 |
|----|------|------|
| Desktop 契约（Remote Refit） | `IHerbApi.cs:64` | `GET /api/v1/herbs/export` |
| Desktop 契约（Local） | `HerbsHttpApiClient.cs:48` | `GET /api/v1/herbs/export` |
| Remote 服务端实际 | `CatalogController.cs:160` | `GET /api/v1/herbs/**export-all**`（无 /export） |
| LocalWebAPI 实际 | `CatalogController.cs`（药材区 48-306） | **无任何药材 export 端点** |

→ 药材「导出」在 Remote 模式 404（路由名错位）、Local 模式 404（端点不存在）。`export-all` 全仓无 Desktop 消费点（死端点）。

**F2 药材「下载模板」Local 模式 404（P1）**

- Remote `GET /api/v1/herbs/import-template`（CatalogController.cs:74）存在 ✓
- LocalWebAPI 药材区**无 import-template 端点** → 离线模式「下载模板」404

**测试盲区根因**：`ImportExportJsonTests`（远程）只断言 `HerbImportTemplate`/`HerbExportAll` 方法存在，**不断言客户端 Refit 路由与服务端对齐**；`LocalImportExportJsonTests`（本地 4 用例）只覆盖患者/验方，**未覆盖药材** → 双端路由错位零守卫。

**F6 验方导出筛选参数错位（P2，潜伏）**：`IFormulaApi:71`/`FormulasHttpApiClient.cs:52` 传 `?category=`，双端服务端 action 读 `keyword`（Remote :570、Local :418）→ 传参永不生效。当前 UI 传 null 无碍，但一旦按筛选导出即失效。

患者/验方双模式端点经核对**全部对齐**：`/patients/import-template|export|batch-import`、`/formulas/import-template|export|batch-import` 双端存在且路径一致 ✓。

### 2.5 UI 风格一致性 ✅

- 三个模块统一 `MaterialDesignOutlinedButton` + `PackIcon`（FileDocumentOutline/Import），与既有编辑按钮一致 ✓
- 模板/导入按钮放 AdditionalContent，导出走 DataGridToolbar 内建 ExportCommand——三模块布局一致 ✓
- 用户模块死绑定删除后工具栏自动隐藏导出按钮（NullToVis）✓

---

## 三、架构合规性审查 ✅（全部合规）

蓝图 §3.5 分层链逐环节验证：

```
View(XAML) ← binding ← ViewModel ← IService ← Repository ← IApiClient{Module} ← SwitchingApiClient ←(Remote: Refit | Local: HttpClient → LocalWebAPI)
```

| 检查项 | 结果 |
|--------|------|
| VM 注入面 | 3 VM 仅注入 IService + StatusHandler/CacheManager/Mapper/Editor 子 VM——**无 IApiClient 直注**（DP10 合规）✓ |
| Service → Repository → IApiClient 链 | 患者/药材/验方三模块批量导入/导出全链路逐层转发，签名一致 ✓ |
| SwitchingApiClient | `HttpClientApiClient`（Local）/`RefitApiClient`（Remote）按 URL 切换，Repository 无感知 ✓ |
| DI 注册 | `CatalogModule.cs:33-34`（IHerbService/IFormulaService）、`PatientsModule.cs:44`（IPatientService）✓ |
| Mapperly | 新命令无手写映射（byte[]/DTO 直传）；既有 Mapperly 映射未受影响 ✓ |
| 验证 | `dotnet build LYBTZYZS.sln --no-incremental`：**0 错误 0 警告**（本次实测）；架构守卫 **87/87**（本次实测）✓ |

---

## 四、安全性审查

### 4.1 文件对话框 ✅

- 导入/导出路径全部来自 `OpenFileDialog`/`SaveFileDialog` 用户显式选择，无外部输入拼接路径 → 无路径遍历注入风险 ✓
- 唯一打磨点：`SaveFileDialog` 未设 `DefaultExt=".json"`（AddExtension 默认 true 但 DefaultExt 空）→ 用户可能保存无扩展名文件（P3，F13）

### 4.2 JSON 反序列化 ✅（1 条加固建议）

- `JsonSerializerOptions`：`PropertyNameCaseInsensitive + JsonStringEnumConverter`——无多态类型、无远程类型解析，System.Text.Json 默认安全（MaxDepth 64）✓
- 非法枚举字符串/结构错误 → JsonException → 单独捕获提示「文件格式错误，请使用下载的 JSON 模板」✓
- **加固建议（P3，F12）**：`File.ReadAllTextAsync` 先全量读入再反序列化，导入前无文件大小/行数预检——超大文件先吃内存，且服务器 10000 上限在 POST 后才拦截。建议读前查文件大小 + 解析后 `Count > 10000` 提前提示。

### 4.3 大文件处理（批量导入上限 10000）✅ 服务端已实现

- 患者：`BatchImportPatientsCommandHandler.cs:27` `MAX_IMPORT_SIZE = 10000` ✓
- 药材：`BatchImportHerbsCommandHandler.cs:27` 同 ✓
- 验方：`BatchImportFormulasCommandHandler.cs:25` 同 ✓
- 客户端确认框显示实际条数，空文件前置拦截 ✓

### 4.4 敏感字段 ✅

- 患者导出经 `SensitiveDataJsonConverterFactory` 管道自动脱敏（US-PAT-012「敏感字段按脱敏规则导出」）✓

---

## 五、文档一致性审查

### 5.1 追溯矩阵状态列准确性 ❌

| 行 | 矩阵 v1.9 标注 | 代码事实 | 判定 |
|----|---------------|---------|------|
| US-PAT-011/012 | Desktop ✅ | 端点双端齐全 + UI 接线，**但无真机验证**；导出文件为包装体（F5） | ⚠️ 接线事实 ✅，功能可用性未经真机验证 |
| US-HERB-006 | Desktop ✅ | `POST /herbs/batch-import` 双端存在 + UI 接线 | ✅ 基本准确 |
| **US-HERB-007/013** | Desktop ✅（「export-all 端点双端」） | **导出双模式 404（F1）、Local 模板 404（F2）、export-all 无 Desktop 消费** | **❌ 失真**——「双端」不成立 |
| US-FORM-006/013 | Desktop ✅ | 双端端点齐全 + UI 接线；但模板示例与 DTO 不符（F3）、导出筛选参数错位（F6）、导出无明细（F7） | ⚠️ 接线 ✅，模板/筛选功能未达验收 |

**纪律问题**：f5e0b4236 将 HERB-007/013 ⚠️→✅ 属未真机验证的乐观标注，与接线报告「双模式兼容 ✅」断言一并失实（接线报告 desktop-batch-import-export-2026-08-19.md 未真机验证 herb 路由）。违反「状态列是需求-代码对齐 SSOT」规则（skill 3b）。

### 5.2 需求文档正文滞后/自相矛盾

| 位置 | 问题 |
|------|------|
| `05-herbs.md` US-HERB-007（:222）/US-HERB-013（:416-417）验收标准 | 仍写「返回 Excel 文件（.xlsx）」「标准 Excel 模板」——与同文档业务规则 1（JSON，2026-08-13）**自相矛盾**；状态 ✅ 但验收标准未同步 |
| `05-herbs.md:240/:424`、`04-patients.md:38` | 引用不存在的接口 `IHerbImportExportService` / `IPatientImportExportService`（代码零命中） |
| `04-patients.md` US-PAT-011/012 状态 | 仍「🔧 设计修订（Excel→JSON）」——未同步为已实现 |
| `06-formulas.md` US-FORM-013 | 状态仍「⚠️ 部分实现」（矩阵已 ✅）；业务规则 4「模板允许匿名访问（AllowAnonymous）」代码**无**（双端均继承类级 [Authorize]） |
| `06-formulas.md` US-FORM-013 验收「每行验方包含药材组成详情」 | 导出返回 FormulaListDto（仅 HerbCount）→ 验收不满足（F7） |
| Desktop 契约注释 | `IPatientApi.cs`/`IHerbApi.cs`/`IFormulaApi.cs` 导出方法注释仍写「导出数据到Excel」「Excel模板文件流」——Excel→JSON 决策未传播（已知项，doc-code-audit 已列） |

### 5.3 权限口径不一致（P2，F4）

- 需求文档：US-HERB-007/013「角色: 管理员（AdminOrSuperAdmin 策略）」「写操作仅 Admin，已落地 C2」；US-PAT-011/012「管理员（Admin 及以上）」
- 代码事实：
  - Remote/Local `CatalogController` 类级 `DoctorOrAdmin`，herb `batch-import`/`import-template`/`export-all` **无显式收紧** → Doctor 可导入/导出药材/验方
  - 患者 `import-template`/`export` 继承类级 `DoctorOrAdminOrReceptionist`（含前台/挂号角色）
  - 患者 `batch-import` 显式 `AdminOrSuperAdmin`（:343）——**模块间不一致**
- `04-permissions.md`「验方导出/导入」标 📋（待定）——需产品决策对齐后再改代码+文档

### 5.4 架构文档需更新项

| 文档 | 问题 |
|------|------|
| `13c-current-status.md` #112 | 「import-template/export-all **双端**改 JSON」——「双端」不成立（LocalWebAPI 无药材 export 端点） |
| `04-api-reference/04-herbs.md` | 声称服务端提供 import-template/export-all——仅 Remote 成立，Local 缺失 |
| `docs/03-architecture/localwebapi/api-endpoints.md` | 旧路径 `/api/herbs/*`（无版本前缀）整体过时，且所列 `/api/herbs/export`、`/api/herbs/import-template` 在当前 LocalWebAPI 均不存在 |
| `docs/compose/reports/desktop-doc-code-audit-2026-08-19.md` | **未入库**（git 未跟踪）——其「Desktop UI 零消费」结论已被 f5e0b4236 推翻，需更新/归档；连同 `.commandcode/` 目录一并清理 |

### 5.5 导出文件结构（P2，F5）

- 导出端点返回 `ApiResponse<T>` 包装（`{success,message,data:[...],timestamp}`），Desktop `ReadAsByteArrayAsync` 保存原样字节 → 文件不是需求文档声称的「JSON 数组」；且与导入 DTO（`{patients:[...],strategy}`）不兼容 → US-PAT-012「迁移」场景导出→导入**回灌不可行**。需产品决策：裸 JSON 数组 vs 包装体 + 回灌兼容格式。

---

## 六、验证证据（本次独立实测）

| 项 | 结果 |
|----|------|
| `dotnet build LYBTZYZS.sln --no-incremental` | ✅ 0 错误 0 警告（5m38s） |
| `dotnet test tests/LYBT.Tests.Architecture/` | ✅ 87/87 通过 |
| Desktop E2E（Integration） | ⏸ 未运行——需运行中 WebAPI + LocalDB（C-01 已知环境项）；`ExportHerbs_WithKeyword_ReturnsFileResponse`（HerbTests.cs:262）按路由分析在现有 Remote 代码上应 404，修复后需回归 |
| 静态路由核对 | 患者/验方双端 9 端点全部对齐；药材 export/import-template 断裂（F1/F2） |

---

## 七、修复任务书（P1 可直接派发；P2 需先过需求）

### T1（P1）药材导出路由对齐（双模式）
- **决策点**：矩阵/需求语义 US-HERB-007=全量导出（export-all）、US-HERB-013=筛选导出+模板。Desktop 单个「导出」按钮 = 筛选导出 → 建议 **Remote 补 `GET /api/v1/herbs/export`（keyword 筛选，对齐患者）+ LocalWebAPI 补 `GET /api/v1/herbs/export` + `GET /api/v1/herbs/import-template`**；或统一改为消费 `export-all` 并明确全量语义。二选一经产品确认。
- **验收**：双模式导出/模板真机 200；`LocalImportExportJsonTests` 补药材 2 用例；新增契约-端点路由对齐守卫（Refit 路径 ↔ 服务端 Route 反射比对）。

### T2（P1）验方模板 DTO 对齐（US-FORM-006/013）
- 模板 Example.Herbs 改 `[{HerbName,Dosage,Unit}]` JSON 对象数组（或按 FormulaImportItemDto 实际形状出示例）；模板 Fields 删 Category 或 FormulaImportItemDto 补 Category 属性（先过需求）。
- **验收**：照抄模板示例可成功导入；模板字段与 batch-import DTO 完全一致。

### T3（P2）权限口径对齐（F4）
- 产品决策：药材/验方 batch-import、模板、导出 = AdminOrSuperAdmin 还是 DoctorOrAdmin（04-permissions.md「验方导出/导入」📋 待定）。
- 决策后：双端控制器补/收方法级 [Authorize] + 权限矩阵/需求文档同步。

### T4（P2）导出文件结构 + 回灌兼容（F5）
- 产品决策：导出裸 JSON 数组（改 Desktop 解包 `data` 再保存）或保留包装体；如需「迁移回灌」，导出格式与 `{patients:[...],strategy}` 对齐（含策略默认值）。
- 患者敏感字段脱敏语义在两种格式下均保留。

### T5（P2）验方导出筛选参数对齐（F6）+ 明细补全（F7）
- 客户端 `category` ↔ 服务端 `keyword` 二选一对齐；US-FORM-013「每行含药材组成详情」需导出 DTO 补 Herbs 明细（先过需求，涉及 FormulaListDto 或新建导出 DTO）。

### T6（P3）UI 打磨
- 导入命令补 `CanExecute = !IsBusy` 门控；`ImportJsonOptions` 抽公共助手（3 VM 去重）；导入前文件大小/行数预检（>10000 提前提示）；SaveFileDialog 补 `DefaultExt=".json"`。

### T7 文档批次（随各代码批次同步）
- 需求文档：US-PAT-011/012 状态列 → ✅；US-HERB-007/013 验收标准去 Excel 残留；US-FORM-013 状态/AllowAnonymous/明细验收对齐；删虚构接口引用（IHerbImportExportService/IPatientImportExportService）。
- 追溯矩阵：HERB-007/013 Desktop 修正（修复后 ✅，修复前 ⚠️）；HERB-007 端点行号 :82→:160。
- 架构文档：13c #112「双端」表述、04-api-reference/04-herbs.md 双端清单、localwebapi/api-endpoints.md 整体刷新、Desktop 契约注释 Excel 语义清理。
- 仓库卫生：desktop-doc-code-audit-2026-08-19.md 未入库文件更新/归档；.commandcode/ 未跟踪目录清理。

---

*审查时间: 2026-08-19*
*约束遵守: 只审查不修改代码 ✅ / 证据独立实测（build 0/0 + 架构 87/87）✅ / 修复项登记任务书 ✅*
