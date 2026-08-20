# 药材管理 (Herb Management)

> 药材管理模块负责维护中医处方中使用的所有中药材基础数据，对药材采用「记录模式」（Record-Only）：仅管理药材的名称、性味归经、功效等基础信息，**不涉及库存管理**。药材是验方（Formula）与处方（Prescription）的核心组成单元。
>
> **权限权威**：完整权限矩阵（GET 前台不可查、写操作 Admin+、恢复 Admin）以 [04-permissions.md](../01-product/04-permissions.md) 为准，本模块 US 中策略名为摘要。
>
> | 我是… | 我能… |
> | ------- | ------- |
> | 医生 | 查看药材列表、搜药材（开方选药） |
> | 管理员 | 全部操作：增删改、批量导入、启用/禁用、调价 |
> | 前台 | ❌ 不涉及药材（2026-08-03 决策） |

---

## US-HERB-001: 分页查询药材列表

**角色**: 医生/管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 诊所工作人员，**我想要** 分页查询药材列表并支持拼音与分类筛选，**以便** 快速定位所需药材。

**验收标准**:

- [ ] 支持分页参数（pageIndex、pageSize）
- [ ] 支持按名称、拼音首字母（PinyinAbbreviation）筛选
- [ ] 支持按 Category（分类）筛选
- [ ] 返回总数与分页数据
- [ ] Doctor/Admin/SuperAdmin 可查看药材列表（只读，前台不可）；Admin+ 可创建/编辑药材

**业务规则**:

1. GET 端点策略：Doctor+Admin（前台不可查，2026-08-03 决策）；Create/Update 已用 `AdminOrSuperAdmin`（代码已修复 C2，2026-08-04 核实）
2. 拼音搜索基于 `PinyinAbbreviation` 字段（如 "dg" 匹配 "当归"）
3. 结果受 OutputCache 缓存（`HerbsCache` 策略）提升查询性能


**实现参考**: `CatalogController.cs` (HttpGet list), `IHerbService`, OutputCache `HerbsCache`

---

## US-HERB-002: 查看药材详情

**角色**: 管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 查看指定药材的完整信息，**以便** 开方前核实药材的性味归经与功效。

**验收标准**:

- [ ] 通过药材 ID 查询
- [ ] 返回完整药材信息（名称、性味、归经、功效、分类等）
- [ ] 不存在的 ID 返回 404
- [ ] 仅 Doctor 及以上角色可访问

**业务规则**:

1. GET 端点受 `DoctorOrAdmin` 策略保护（前台不可查；已落地）
2. 药材详情同样受 OutputCache 缓存


**实现参考**: `CatalogController.cs` (HttpGet `{id}`), `IHerbService`

---

## US-HERB-003: 创建药材

**角色**: 管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 创建新的药材记录，**以便** 扩充系统药材库。

**验收标准**:

- [ ] 接受名称、性味、归经、功效、分类等字段
- [ ] 自动生成 PinyinAbbreviation
- [ ] 名称唯一校验（重复返回 409）
- [ ] 创建成功返回新药材（含 ID）

**业务规则**:

1. 拼音由服务自动生成，无需客户端提供
2. 新药材默认 `IsEnabled=true`、`IsDeleted=false`
3. 端点受 `AdminOrSuperAdmin` 策略保护（写操作仅 Admin，已落地 C2）


**实现参考**: `CatalogController.cs` (HttpPost create), `IHerbService`

---

## US-HERB-004: 更新药材

**角色**: 管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 更新药材信息，**以便** 修正错误或补充最新药理数据。

**验收标准**:

- [ ] 接受可修改字段
- [ ] 名称变更触发拼音重新生成
- [ ] 名称变更后仍需唯一
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 更新时重新生成 PinyinAbbreviation（名称可能变更）
2. 名称唯一约束同样适用于更新
3. 端点受 `AdminOrSuperAdmin` 策略保护（写操作仅 Admin，已落地 C2）


**实现参考**: `CatalogController.cs` (HttpPut `{id}`), `IHerbService`

---

## US-HERB-005: 删除药材（软删除，引用检查）

**角色**: 管理员
**优先级**: Must
**状态**: ✅ 已实现（D5 引用检查，2026-08-04 代码批次 C3）

**作为** 医生或管理员，**我想要** 软删除药材（保留审计数据），**以便** 清理无效药材同时保留历史。

**验收标准**:

- [ ] 通过 ID 软删除（设置 IsDeleted=true）
- [ ] 删除前检查是否被处方（Prescription）引用
- [ ] 被引用时返回 422，拒绝删除
- [ ] 删除后列表查询自动排除
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 引用检查：查询 PrescriptionItem 表（PrescriptionItem.HerbId）是否存在引用该药材的处方项
2. 被引用的药材不可删除（保护已开处方完整性），返回 422
3. 软删除通过全局查询过滤器自动隐藏


**实现参考**: `CatalogController.cs` (HttpDelete `{id}`), `IHerbService`

---

## US-HERB-006: 批量导入药材（Skip/Update/Error 策略）

**角色**: 管理员
**优先级**: Must
**状态**: 🔧 设计修订（2026-08-13：移除服务端 Excel 解析路径——后端统一 JSON/DTO，保持通用性）

**作为** 医生或管理员，**我想要** 批量导入药材并指定重复处理策略，**以便** 高效建立或扩充药材库。

**验收标准**:

- [ ] 支持 DTO 列表提交（JSON 数组——客户端可先做 Excel 解析转 JSON）
- [ ] 接受 `DuplicateStrategy`（Skip/Update/Error）
- [ ] 单次导入上限 10000 条
- [ ] 返回导入结果（成功数、跳过数、失败明细）
- [ ] 自动生成拼音

**业务规则**:

1. 导入路径：`BatchImportAsync` 接收客户端已解析的 DTO 列表（JSON）——**唯一路径**（2026-08-13 移除服务端 Excel 解析）
2. `DuplicateStrategy` 处理重名药材：`Skip` 跳过保留原记录 / `Update` 用新数据覆盖 / `Error` 遇到重复立即报错终止
3. 单次上限 10000 条，超出拒绝
4. 导入时自动生成 PinyinAbbreviation
5. **重复处理策略（模块规则）**：`DuplicateStrategy` 枚举（Skip/Update/Error）
6. **批量上限（模块规则）**：单次批量导入最多 10000 条记录


**实现参考**: `CatalogController.cs` (HttpPost `batch-import`——DTO/JSON 唯一导入路径)

---

## US-HERB-007: 导出全部药材

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现（T4: export-all 端点双端）

**作为** 医生或管理员，**我想要** 导出全部药材数据为 JSON，**以便** 数据备份、迁移或外部审核。

**验收标准**:

- [ ] 返回 JSON 数组（`application/json`，2026-08-13：Excel→JSON）
- [ ] 导出全部启用且未删除的药材
- [ ] 包含所有基础字段（名称、性味、归经、功效、分类、拼音）
- [ ] 大数据量导出不影响主业务性能

**业务规则**:

1. 导出由服务层执行（JSON 数组——2026-08-13：Excel→JSON，后端不涉及 Excel 格式）
2. 端点受 `AdminOrSuperAdmin` 策略保护（写操作仅 Admin，已落地 C2）
3. 与 US-HERB-013 的导出端点不同：本端点导出全部，US-HERB-013 支持筛选导出 + 模板下载


**实现参考**: `CatalogController.cs` (HttpGet `export-all`)

---

## US-HERB-008: 单个引用检查

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现（CheckHerbReference 处方+验方双计数）

**作为** 医生或管理员，**我想要** 在删除前检查药材是否被处方引用，**以便** 预判删除是否可行。

**验收标准**:

- [ ] 通过药材 ID 查询引用状态
- [ ] 返回是否被引用（HasReference）及引用计数
- [ ] 引用明细（可选）：关联的处方 ID 列表
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 引用检查查询 Prescription 表中引用该药材的处方项数量
2. 此端点允许 Doctor 与 Admin 查询（开方者需了解药材状态）


**实现参考**: `CatalogController.cs` (HttpGet `{id}/check-reference`), `IHerbService`

---

## US-HERB-009: 批量引用检查

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现（BatchCheckReference 聚合计数）

**作为** 医生或管理员，**我想要** 批量检查多个药材的引用状态，**以便** 批量删除前快速识别可删除项。

**验收标准**:

- [ ] 接受药材 ID 列表
- [ ] 返回每个药材的引用状态与计数
- [ ] 高效查询（避免 N+1）
- [ ] 不存在的 ID 标记为无效

**业务规则**:

1. 批量引用检查通过单次聚合查询实现（避免逐项 N+1）
2. 用于批量删除前的预检


**实现参考**: `CatalogController.cs` (HttpPost `batch-check-reference`), `IHerbService`

---

## US-HERB-010: 启用/禁用药材

**角色**: 管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 启用或禁用药材，**以便** 暂时屏蔽不使用或有质量问题的药材。

**验收标准**:

- [ ] 切换药材的 IsEnabled 状态
- [ ] 禁用后药材在开方界面不可选
- [ ] 禁用不影响已存在的处方（处方仍保留该药材）
- [ ] 不存在的 ID 返回 404

**业务规则**:

1. 禁用与软删除语义不同：禁用保留记录但对开方界面隐藏；软删除则视为已移除
2. 禁用不影响历史处方（已开具的处方完整性受保护）

**边界条件**:

- 药材被禁用后，已开具的历史处方仍可正常查看和打印（药材数据快照，不随当前状态变化）
- 禁用的药材不出现在新建处方的药材选择列表中（前台不可查药材）


**实现参考**: `CatalogController.cs` (HttpPost `{id}/toggle-status`), `IHerbService`

---

## US-HERB-011: 恢复软删除药材

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现（Restore 泛型命令）

**作为** 医生或管理员，**我想要** 恢复被软删除的药材，**以便** 在误删或重新启用时还原记录。

**验收标准**:

- [ ] 通过 ID 恢复（设置 IsDeleted=false）
- [ ] 恢复后药材在列表与开方界面可见
- [ ] 使用 `IgnoreQueryFilters` 查询已软删除记录
- [ ] 恢复后默认启用状态

**业务规则**:

1. 恢复操作需 `IgnoreQueryFilters()` 绕过全局软删除过滤器定位记录
2. 恢复仅还原药材记录本身，不还原关联处方（处方删除独立）


**实现参考**: `CatalogController.cs` (HttpPost `{id}/restore`), `IHerbService`

---

## US-HERB-012: 批量操作（启用/禁用/删除）

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现（批量删除引用检查）

**作为** 医生或管理员，**我想要** 批量启用、禁用或删除多个药材，**以便** 高效管理大批量药材变更。

**验收标准**:

- [ ] 提供批量启用（batch-enable）、批量禁用（batch-disable）、批量删除（batch-delete）端点
- [ ] 接受药材 ID 列表
- [ ] 批量删除逐项执行引用检查，被引用的跳过并记录
- [ ] 返回成功与失败（含原因）的明细

**业务规则**:

1. 批量操作采用逐项处理（非原子），允许部分失败
2. 批量删除每项均执行引用检查（同 US-HERB-005 规则）
3. 端点受 `AdminOrSuperAdmin` 策略保护（写操作仅 Admin，已落地 C2）


**实现参考**: `CatalogController.cs` (batch-enable), `CatalogController.cs` (batch-disable), `CatalogController.cs` (batch-delete), `IHerbService`

---

## US-HERB-013: 导出 JSON + 下载模板

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现（T4: export/import-template 端点双端；2026-08-19 P1 修复：Local 补 export/import-template/export-all 端点 + 模板示例与 DTO 对齐）

**作为** 医生或管理员，**我想要** 按条件导出药材 JSON 与下载导入模板，**以便** 数据备份与规范批量导入。

**验收标准**:

- [ ] 导出端点支持按筛选条件导出（非全量）
- [ ] 模板端点返回 JSON 模板（字段说明 + 示例 + 必填标注，2026-08-13 起非 Excel）
- [ ] 返回 JSON 数组（`application/json`）
- [ ] 模板标注必填字段与可选字段

**业务规则**:

1. 与 US-HERB-007 区分：本端点支持筛选导出 + 提供模板；US-HERB-007 为全量导出
2. 模板字段与 US-HERB-006 导入端点期望的 DTO 一致
3. 导出与模板由 `IHerbService` 生成


**实现参考**: `CatalogController.cs` (HttpGet `export`), `CatalogController.cs` (HttpGet `import-template`)

---

## US-HERB-014: Desktop 前端药材缓存（开方/验方选择用）

**角色**: 医生/管理员
**优先级**: Must
**状态**: ✅ 已实现（`DesktopCacheManager.InvalidateHerbCaches` + 需求文档 2026-08-13 补充定义）

**作为** 医生，**我想要** 在开方/验方编辑时快速选择药材且不重复请求服务器，**以便** 编辑体验流畅、减少网络往返。

**验收标准**:

- [ ] 首次进入开方/验方编辑 → 查询药材（`GET /api/v1/herbs`）并缓存
- [ ] 同一会话内再次进入 → 使用缓存（不重复请求）
- [ ] 药材 CRUD 操作后 → 缓存失效（下次进入重新查询）
- [ ] 提交验方/处方时后端引用校验兜底（缓存过期药材 → 422 提示刷新）

**业务规则**:

1. 缓存键：`GET:/api/v1/herbs`（前缀匹配，`DesktopCacheManager`）
2. 失效事件：`CacheEvents.InvalidatedEvent`（Domain=Herbs，Reason=HerbCRUD）——药材增删改/批量操作后发布
3. 缓存范围：**只缓存查询结果（药材目录）**；验方/处方提交时的药材组合是**请求体快照**，不依赖缓存
4. 价格一致性：开方时 `PrescriptionItem` 价格快照取自查询结果，历史处方不受后续调价影响（A2 决策）
5. 缓存不适用场景：验方详情/处方详情（实时数据，不缓存）


**实现参考**: `DesktopCacheManager.cs`（`InvalidateHerbCaches`）、`FormulaEditorViewModel.cs`（`EditHerbItems`）、`PrescriptionEditorViewModel.cs`

---

## 模块级业务规则（横切）

**记录模式（Record-Only）**：系统仅管理药材基础信息，不管理库存。无库存量、入库、出库概念。

**分类筛选**：药材可按 `Category`（如解表药、清热药）筛选。

**调价审计（A2 决策）**：药材单价调整操作写入审计日志（操作人/时间/旧价→新价），并入 D1 医案审计日志体系（[07-medical-cases.md](07-medical-cases.md) US-MC-017），**不单独建调价历史实体**。历史处方的价格由 `PrescriptionItem` 快照隔离（保存开方时的 UnitPrice/Amount），不受后续调价影响；新开方调用药材最新单价。

**剂量单位（D13 决策）**：处方中药材剂量单位默认 **g（克）**；v1.0 **不支持**单位换算（如钱 / g 转换）。`PrescriptionItem.Unit` 为自由文本字段，Doctor 录入时可自定（如"粒"、"片"、"ml"），系统不做换算只原样存储与打印。

**导入路径（2026-08-13 修订）**：`BatchImportAsync` 接收客户端已解析的 DTO 列表（JSON），适合客户端预处理后导入——**唯一路径**（服务端 Excel 解析已移除）。

**双模式**：药材管理在远程与本地模式下行为完全一致，均通过统一的 `IHerbService` 服务层实现。本地 WebAPI 复用全部服务端模块。

**边界条件**：验方导入处方时，已禁用药材自动跳过并提示"以下药材已停用，已跳过: xxx"（MC-D09，详见 [07-medical-cases.md](07-medical-cases.md)）。

---

## 变更记录

| 日期 | 变更 |
| ------ | ------ |
| 2026-06-25 | 修正引用检查实体名（PrescriptionItem）、补充边界条件验收标准 |
| 2026-06-25 | 修正药材查询权限描述：Receptionist 可查看（只读），与 DoctorOrReceptionist 策略一致 |
| 2026-06-28 | 补调价审计（A2：并入 D1 审计 + PrescriptionItem 快照）与剂量单位（D13：默认 g 不换算）业务规则 |
