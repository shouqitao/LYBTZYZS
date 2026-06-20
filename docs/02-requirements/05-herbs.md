# 药材管理 (Herb Management)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 模块概述

药材管理模块负责维护中医处方中使用的所有中药材基础数据。本系统对药材采用「记录模式」（Record-Only）：仅管理药材的名称、性味归经、功效等基础信息，**不涉及库存管理**（无入库/出库/库存量）。药材是验方（Formula）与处方（Prescription）的核心组成单元，其数据质量直接影响诊疗准确性。

模块核心包括：药材分页查询（支持拼音首字母搜索）、详情查看、创建、更新、删除（软删除 + 引用检查）、批量导入（两种路径：服务端 Excel 与客户端 DTO）、导出全部、单个/批量引用检查、启用/禁用、恢复软删除、批量操作（启用/禁用/删除）、Excel 导出与模板下载。删除被处方引用的药材会被拒绝（返回 422）。

## 业务规则

1. **记录模式（Record-Only）**：系统仅管理药材基础信息，不管理库存。无库存量、入库、出库概念。
2. **拼音搜索**：`PinyinAbbreviation` 存储药材名称的拼音首字母（如 "dg" → "当归"），支持快速拼音检索。
3. **两种导入路径**：
   - `ImportFromExcelAsync`：服务端解析 Excel（EPPlus），适合大批量首次导入
   - `BatchImportAsync`：接收客户端已解析的 DTO 列表，适合客户端预处理后导入
4. **删除引用检查**：被处方（Prescription）引用的药材不可删除，返回 422。
5. **重复处理策略（`DuplicateStrategy` 枚举）**：
   - `Skip`：跳过重复项
   - `Update`：更新已存在的重复项
   - `Error`：遇到重复即报错终止
6. **批量上限**：单次批量导入最多 10000 条记录。
7. **分类筛选**：药材可按 `Category`（如解表药、清热药）筛选。
8. **输出缓存**：`OutputCache` 策略 `HerbsCache` 缓存高频读取的药材数据。

## 双模式差异

药材管理在远程与本地模式下行为完全一致，均通过统一的 `IHerbService` / `IHerbImportExportService` 服务层实现。本地 WebAPI 复用全部服务端模块。

## 用户故事

### US-HERB-001: 分页查询药材列表

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 分页查询药材列表并支持拼音与分类筛选，**以便** 快速定位所需药材。

**验收标准**:
- [ ] 支持分页参数（pageIndex、pageSize）
- [ ] 支持按名称、拼音首字母（PinyinAbbreviation）筛选
- [ ] 支持按 Category（分类）筛选
- [ ] 返回总数与分页数据
- [ ] 仅 Doctor 及以上角色可访问

**业务规则**:
1. 端点受 `DoctorOrReceptionist` 策略保护（医生与管理员可访问，前台不可）
2. 拼音搜索基于 `PinyinAbbreviation` 字段（如 "dg" 匹配 "当归"）
3. 结果受 OutputCache 缓存（`HerbsCache` 策略）提升查询性能

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:37` (HttpGet list), `IHerbService`, OutputCache `HerbsCache`

---

### US-HERB-002: 查看药材详情

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 查看指定药材的完整信息，**以便** 开方前核实药材的性味归经与功效。

**验收标准**:
- [ ] 通过药材 ID 查询
- [ ] 返回完整药材信息（名称、性味、归经、功效、分类等）
- [ ] 不存在的 ID 返回 404
- [ ] 仅 Doctor 及以上角色可访问

**业务规则**:
1. 端点受 `DoctorOrReceptionist` 策略保护
2. 药材详情同样受 OutputCache 缓存

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:57` (HttpGet `{id}`), `IHerbService`

---

### US-HERB-003: 创建药材

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 创建新的药材记录，**以便** 扩充系统药材库。

**验收标准**:
- [ ] 接受名称、性味、归经、功效、分类等字段
- [ ] 自动生成 PinyinAbbreviation
- [ ] 名称唯一校验（重复返回 409）
- [ ] 创建成功返回新药材（含 ID）

**业务规则**:
1. 拼音由服务自动生成，无需客户端提供
2. 新药材默认 `IsEnabled=true`、`IsDeleted=false`
3. 端点受 `DoctorOrReceptionist` 策略保护（医生与管理员均可创建）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:75` (HttpPost create), `IHerbService`

---

### US-HERB-004: 更新药材

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
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
3. 端点受 `DoctorOrReceptionist` 策略保护

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:96` (HttpPut `{id}`), `IHerbService`

---

### US-HERB-005: 删除药材（软删除，引用检查）

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 软删除药材（保留审计数据），**以便** 清理无效药材同时保留历史。

**验收标准**:
- [ ] 通过 ID 软删除（设置 IsDeleted=true）
- [ ] 删除前检查是否被处方（Prescription）引用
- [ ] 被引用时返回 422，拒绝删除
- [ ] 删除后列表查询自动排除
- [ ] 不存在的 ID 返回 404

**业务规则**:
1. 引用检查：查询 Prescription 表是否存在引用该药材的处方项
2. 被引用的药材不可删除（保护已开处方完整性），返回 422
3. 软删除通过全局查询过滤器自动隐藏

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:121` (HttpDelete `{id}`), `IHerbService`

---

### US-HERB-006: 批量导入药材（Skip/Update/Error 策略）

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 批量导入药材并指定重复处理策略，**以便** 高效建立或扩充药材库。

**验收标准**:
- [ ] 支持两种导入路径：Excel 文件上传 或 DTO 列表提交
- [ ] 接受 `DuplicateStrategy`（Skip/Update/Error）
- [ ] 单次导入上限 10000 条
- [ ] 返回导入结果（成功数、跳过数、失败明细）
- [ ] 自动生成拼音

**业务规则**:
1. 两种导入路径：
   - `ImportFromExcelAsync`：服务端用 EPPlus 解析 Excel
   - `BatchImportAsync`：接收客户端已解析的 DTO 列表
2. `DuplicateStrategy` 处理重名药材：
   - `Skip`：跳过，保留原记录
   - `Update`：用新数据覆盖原记录
   - `Error`：遇到重复立即报错终止
3. 单次上限 10000 条，超出拒绝
4. 导入时自动生成 PinyinAbbreviation

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:148` (HttpPost `batch-import`), `IHerbImportExportService.ImportFromExcelAsync` / `BatchImportAsync`

---

### US-HERB-007: 导出全部药材

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 导出全部药材数据为 Excel，**以便** 数据备份、迁移或外部审核。

**验收标准**:
- [ ] 返回 Excel 文件（.xlsx）
- [ ] 导出全部启用且未删除的药材
- [ ] 包含所有基础字段（名称、性味、归经、功效、分类、拼音）
- [ ] 大数据量导出不影响主业务性能

**业务规则**:
1. 导出由 `IHerbImportExportService` 执行（EPPlus）
2. 端点受 `DoctorOrReceptionist` 策略保护
3. 与 US-HERB-013 的导出端点不同：本端点导出全部，US-HERB-013 支持筛选导出 + 模板下载

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:186` (HttpGet `export-all`), `IHerbImportExportService`

---

### US-HERB-008: 单个引用检查

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 在删除前检查药材是否被处方引用，**以便** 预判删除是否可行。

**验收标准**:
- [ ] 通过药材 ID 查询引用状态
- [ ] 返回是否被引用（HasReference）及引用计数
- [ ] 引用明细（可选）：关联的处方 ID 列表
- [ ] 不存在的 ID 返回 404

**业务规则**:
1. 引用检查查询 Prescription 表中引用该药材的处方项数量
2. 此端点允许 Doctor 与 Admin 查询（开方者需了解药材状态）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:206` (HttpGet `{id}/check-reference`), `IHerbService`

---

### US-HERB-009: 批量引用检查

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 批量检查多个药材的引用状态，**以便** 批量删除前快速识别可删除项。

**验收标准**:
- [ ] 接受药材 ID 列表
- [ ] 返回每个药材的引用状态与计数
- [ ] 高效查询（避免 N+1）
- [ ] 不存在的 ID 标记为无效

**业务规则**:
1. 批量引用检查通过单次聚合查询实现（避免逐项 N+1）
2. 用于批量删除前的预检

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:226` (HttpPost `batch-check-reference`), `IHerbService`

---

### US-HERB-010: 启用/禁用药材

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
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

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:264` (HttpPost `{id}/toggle-status`), `IHerbService`

---

### US-HERB-011: 恢复软删除药材

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 恢复被软删除的药材，**以便** 在误删或重新启用时还原记录。

**验收标准**:
- [ ] 通过 ID 恢复（设置 IsDeleted=false）
- [ ] 恢复后药材在列表与开方界面可见
- [ ] 使用 `IgnoreQueryFilters` 查询已软删除记录
- [ ] 恢复后默认启用状态

**业务规则**:
1. 恢复操作需 `IgnoreQueryFilters()` 绕过全局软删除过滤器定位记录
2. 恢复仅还原药材记录本身，不还原关联处方（处方删除独立）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:288` (HttpPost `{id}/restore`), `IHerbService`

---

### US-HERB-012: 批量操作（启用/禁用/删除）

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 批量启用、禁用或删除多个药材，**以便** 高效管理大批量药材变更。

**验收标准**:
- [ ] 提供批量启用（batch-enable）、批量禁用（batch-disable）、批量删除（batch-delete）端点
- [ ] 接受药材 ID 列表
- [ ] 批量删除逐项执行引用检查，被引用的跳过并记录
- [ ] 返回成功与失败（含原因）的明细

**业务规则**:
1. 批量操作采用逐项处理（非原子），允许部分失败
2. 批量删除每项均执行引用检查（同 US-HERB-005 规则）
3. 端点受 `DoctorOrReceptionist` 策略保护

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:314` (batch-enable), `HerbsController.cs:338` (batch-disable), `HerbsController.cs:359` (batch-delete), `IHerbService`

---

### US-HERB-013: 导出 Excel + 下载模板

**角色**: 医生/管理员（DoctorOrReceptionist 策略）
**优先级**: Should
**状态**: ✅ 已实现

**作为** 医生或管理员，**我想要** 按条件导出药材 Excel 与下载导入模板，**以便** 数据备份与规范批量导入。

**验收标准**:
- [ ] 导出端点支持按筛选条件导出（非全量）
- [ ] 模板端点返回标准 Excel 模板（含列头、说明、示例）
- [ ] 返回 Excel 文件（.xlsx）
- [ ] 模板标注必填字段与可选字段

**业务规则**:
1. 与 US-HERB-007 区分：本端点支持筛选导出 + 提供模板；US-HERB-007 为全量导出
2. 模板字段与 US-HERB-006 导入端点期望的 DTO 一致
3. 导出与模板均由 `IHerbImportExportService` 生成

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `HerbsController.cs:386` (HttpGet `export`), `HerbsController.cs:398` (HttpGet `import-template`), `IHerbImportExportService`
