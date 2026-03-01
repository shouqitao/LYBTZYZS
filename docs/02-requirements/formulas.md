# 验方管理 需求规格

## 概述

验方管理模块负责经验方模板的创建、编辑、药材组成管理和共享。支持延迟绑定 (导入时药材名称未关联系统药材库)、验方验证工作流、批量导入导出。验方是处方的模板来源，可在开具处方时导入复用。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部验方 |
| Admin | CRUD 全部验方 |
| Doctor | CRUD 自己的验方 + 查看共享验方 (只读) |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。资源级权限: Doctor 查看自己创建的 + IsShared=true 的验方。

---

## 功能清单

> **[已修订 2026-02-21]** Name 字段最大长度从 100 修订为 200，实体定义 200 更宽松无害
> 原因: 代码实体 Name=200，PRD/DTO=100 不一致，对齐实体定义  |  参考: FORM-14

### FR-FORM-001: 创建验方

- **描述**: 创建新的经验方模板
- **业务规则**:
  1. 名称必填，1-100 字符
  2. 功效选填，最长 500 字符 (以 Server 端字段定义为准)
  3. 用法选填，最长 500 字符 (以 Server 端字段定义为准)
  4. 药材组成至少 1 味
  5. 默认类型为 Experience (经验方)
  6. 初始 ValidationStatus=Draft
  7. 记录 UserId 和 CreatedBy (用于所有权判断)
- **远程模式**: POST `/api/v1/formulas`，返回 FormulaDetailDto (200)
- **本地模式**: DataSource 本地存储
- **验收标准**:
  - [ ] 药材列表为空 -> 返回 400 验证失败
  - [ ] 创建成功 -> ValidationStatus=Draft

### FR-FORM-002: 查看验方列表

- **描述**: 分页查看验方列表，支持关键词和分类筛选
- **业务规则**:
  1. 支持按名称搜索 (keyword)
  2. 支持按分类筛选 (category)
  3. Admin 返回全部验方
  4. Doctor 返回自己创建的 + 共享的验方
  5. 列表包含 HerbCount 和 TotalPrice
  6. 分页参数验证: page >= 1, pageSize 1-100 (见 [nfr.md](nfr.md) NFR-API-001)
- **远程模式**: GET `/api/v1/formulas?keyword=&category=&page=&pageSize=`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] Doctor 查询 -> 仅返回 CreatedBy=自己 或 IsShared=true 的验方
  - [ ] Admin 查询 -> 返回全部验方

### FR-FORM-003: 查看验方详情

- **描述**: 获取验方完整信息，包含药材组成列表
- **业务规则**:
  1. 返回 FormulaDetailDto 含完整 Herbs 列表
  2. 包含每味药材的验证状态 (IsValidated)
- **远程模式**: GET `/api/v1/formulas/{id}`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 有效ID -> 返回 FormulaDetailDto + Herbs 列表 (含 IsValidated)

### FR-FORM-004: 更新验方

- **描述**: 修改验方信息和药材组成
- **业务规则**:
  1. 统一所有权检查 (Doctor 只能编辑自己的)
  2. 药材组成采用粗粒度替换策略: 完整替换 Herbs 集合
  3. 药材组成至少 1 味
- **远程模式**: PUT `/api/v1/formulas/{id}`
- **本地模式**: 本地更新
- **验收标准**:
  - [ ] Doctor 编辑 CreatedBy!=自己的验方 -> 返回 403
  - [ ] 更新 Herbs 列表 -> 原有 Herbs 全部替换为新列表

### FR-FORM-005: 删除验方

- **描述**: 软删除验方
- **业务规则**:
  1. 统一所有权检查
  2. 软删除，数据保留
  3. 支持批量删除
- **远程模式**: DELETE `/api/v1/formulas/{id}`，批量: POST `/api/v1/formulas/batch-delete`
- **本地模式**: 本地软删除
- **验收标准**:
  - [ ] Doctor 删除 CreatedBy!=自己的验方 -> 返回 403

### FR-FORM-006: 启用/禁用验方

- **描述**: 切换验方启用/禁用状态
- **业务规则**:
  1. 统一所有权检查
  2. 禁用后开方时不可导入 (验方导入对话框过滤 Status=Enabled)
  3. 支持批量启用/禁用
  4. **处方导入对话框仅展示 ValidationStatus=Validated 且 Status=Enabled 的验方** (MC-D08，见 [medical-cases.md](medical-cases.md) FR-MC-016)
- **远程模式**: POST `/api/v1/formulas/{id}/toggle-status`，批量: POST `/api/v1/formulas/batch-enable` 或 `/batch-disable`
- **本地模式**: 本地状态切换
- **验收标准**:
  - [ ] 验方 Status=Disabled -> 开方时验方导入列表不显示

### FR-FORM-007: 恢复已删除验方

- **描述**: 恢复软删除的验方
- **业务规则**:
  1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
- **远程模式**: POST `/api/v1/formulas/{id}/restore`
- **本地模式**: 本地恢复
- **验收标准**:
  - [ ] 恢复成功 -> 验方重新出现在默认列表查询中

### FR-FORM-008: 共享验方

- **描述**: 将验方标记为共享，其他 Doctor 可查看
- **业务规则**:
  1. IsShared=true 的验方对所有 Doctor 可见
  2. 共享验方对 Doctor 只读
  3. Admin 可编辑任何共享验方
- **远程模式**: 通过 Update 修改 IsShared 字段
- **本地模式**: 本地标记
- **验收标准**:
  - [ ] IsShared=true -> 其他 Doctor 列表查询可见
  - [ ] Doctor 编辑他人共享验方 -> 返回 403

### FR-FORM-009: 延迟绑定

- **描述**: 支持验方药材与系统药材库的延迟绑定
- **业务规则**:
  1. FormulaHerbItem.HerbId 可为空 (未绑定状态)
  2. OriginalHerbName 保存原始药材名称 (从旧系统导入)
  3. IsValidated=false 表示未验证
  4. 手动绑定: 通过 validate 端点将药材关联到系统药材库
  5. 绑定后 IsValidated=true, HerbId 填充
  6. 当所有药材都已验证时，验方 ValidationStatus 自动变为 Validated
- **远程模式**: POST `/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate`，请求体含 selectedHerbId
- **本地模式**: 本地验证
- **验收标准**:
  - [ ] HerbId=null -> 显示 OriginalHerbName
  - [ ] validate 成功 -> IsValidated=true, HerbId 填充
  - [ ] 所有 HerbItem.IsValidated=true -> Formula.ValidationStatus=Validated

### FR-FORM-010: 获取待验证验方

- **描述**: 获取所有包含未验证药材的验方列表
- **业务规则**:
  1. 返回 ValidationStatus=Draft 的验方
  2. 用于管理界面批量处理未验证数据
- **远程模式**: GET `/api/v1/formulas/pending-validation`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 查询 -> 仅返回 ValidationStatus=Draft 的验方

### FR-FORM-011: 批量导入

- **描述**: 通过 JSON 格式批量导入验方 (含药材组成)
- **业务规则**:
  1. 每个验方包含名称、功效、用法和药材列表
  2. 导入的药材默认 IsValidated=false
  3. 返回成功列表和失败详情 (含匹配/未匹配药材数)
- **远程模式**: POST `/api/v1/formulas/batch-import`
- **本地模式**: 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext
- **验收标准**:
  - [ ] 导入完成 -> 返回 successCount/failureCount/matchedCount/unmatchedCount

### FR-FORM-012: 导出验方

- **描述**: 将验方数据导出为 Excel
- **业务规则**:
  1. 支持按分类筛选导出
- **远程模式**: GET `/api/v1/formulas/export?category=`
- **本地模式**: 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件
- **验收标准**:
  - [ ] 导出 Excel -> 每行验方包含药材组成详情

### FR-FORM-013: 下载导入模板

- **描述**: 下载验方导入 Excel 模板
- **业务规则**:
  1. 允许匿名访问
- **远程模式**: GET `/api/v1/formulas/import-template` (AllowAnonymous)
- **本地模式**: 内置模板
- **验收标准**:
  - [ ] GET 请求 -> 返回 .xlsx 模板文件

---

## 数据模型

### Formula (验方实体)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 验方ID |
| Name | string(200) | Required | 验方名称 |
| Effect | string(500)? | - | 功效 |
| Indication | string(1000)? | - | 主治 |
| Usage | string(500)? | - | 用法 |
| Property | string(300)? | - | 性味归经 |
| Category | string(50)? | - | 方剂分类 |
| FormulaType | FormulaType | Default: Experience | 方剂类型 (Classic/Experience) |
| Status | CommonStatus | Default: Enabled | 状态 |
| IsShared | bool | Default: false | 是否共享 |
| ValidationStatus | FormulaValidationStatus | Default: Draft | 验证状态 (Draft/Validated) |
| UserId | Guid? | - | 创建用户ID |
| Remark | string(500)? | - | 备注 |
| Herbs | ICollection | 导航属性 | 药材组成列表 |

### FormulaHerbItem (验方药材项)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 项ID |
| FormulaId | Guid | FK | 所属验方ID |
| HerbId | Guid? | FK, 可空 | 药材ID (延迟绑定) |
| OriginalHerbName | string(100)? | - | 原始药材名称 |
| IsValidated | bool | Default: false | 是否已验证绑定 |
| HerbName | string(100) | Required | 药材名称 |
| Dosage | int | Required | 剂量 (数值部分，单位由 Unit 指定) |
| Unit | string(16) | Required | 单位 (克/g/ml/条/粒 等) |
| ProcessingMethod | string(100)? | - | 炮制方法 |
| DecocteMethod | DecocteMethod | Default: Normal | 煎法 (定义见 [medical-cases.md](medical-cases.md) DecocteMethod 枚举) |
| Usage | string(200)? | - | 用法 |
| Remark | string(200)? | - | 备注 |

> 两个实体均继承 BaseEntity

---

## 错误码

> Service 层采用 Result 模式统一返回。错误码分区: 6xxxx，编号体系: MCCEE (M=模块6, CC=子类别, EE=序号)。所有权检查: Admin/SuperAdmin 可操作全部，Doctor 仅可操作自己创建的验方。

### 核心错误 (601xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-60101 | FormulaNotFound | 404 | 验方不存在 | GetById/Update/Delete/ToggleStatus/Restore 时 ID 无效或已被软删除 |
| ERR-60102 | FormulaIdInvalid | 400 | 验方ID不能为空 | 传入 Guid.Empty |
| ERR-60103 | FormulaNoPermission | 403 | 您没有权限操作此验方，只能操作自己创建的数据 | Doctor 编辑/删除/切换状态他人创建的验方 |
| ERR-60104 | FormulaCreateFailed | 200 | 新增验方失败 | Service 返回 Failure |
| ERR-60105 | FormulaUpdateFailed | 200 | 更新验方失败 | Service 返回 Failure |
| ERR-60106 | FormulaDeleteFailed | 404 | 验方不存在 | Repository.DeleteAsync() 返回 false |
| ERR-60107 | FormulaNotDeleted | 200 | 该验方未被删除，无需恢复 | 恢复未软删除的验方 |
| ERR-60108 | FormulaInvalidPagination | 400 | 页码和页大小参数无效（页码>0，页大小1-100） | 分页参数校验失败 |

### 药材验证错误 (FR-FORM-009, 602xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-60201 | HerbItemIdInvalid | 400 | {paramName}不能为空 | formulaId/herbItemId/selectedHerbId 为 Guid.Empty |
| ERR-60202 | HerbItemNotFound | 200 | 药材项不存在 | formula.Herbs 中未找到 herbItemId |
| ERR-60203 | HerbItemAlreadyValidated | 200 | 该药材已校验，无需重复操作 | herbItem.IsValidated == true |
| ERR-60204 | SystemHerbNotFound | 200 | 所选药材不存在 | 跨模块查询 GetHerbBasicInfoAsync 返回 null |
| ERR-60205 | PendingValidationListFailed | 200 | 获取待校验验方列表失败 | 查询异常 |

### 批量操作错误 (603xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-60301 | FormulaBatchEmpty | 400 | 请至少选择一个方剂 | 批量删除/启用/禁用时 ID 列表为空 |
| ERR-60302 | FormulaBatchImportEmpty | 400 | 导入数据不能为空 | BatchImport 请求体为空 |
| ERR-60303 | FormulaBatchItemNotFound | 200 | 方剂不存在 | 批量删除/状态切换时单项不存在 |
| ERR-60304 | FormulaBatchItemError | 200 | 删除操作失败 / 状态更新失败 | 数据库异常，使用安全消息 |

### 导入行级错误 (FR-FORM-011)

| 失败原因 | 类型 | 触发条件 |
|----------|------|----------|
| 验方名称不能为空 | 验证失败 | 名称字段为空 |
| 数据处理异常 | 技术异常 | 行级 try-catch 捕获 |

**药材匹配机制**: 导入时通过 ICrossModuleService.GetHerbByNameOrPinyinAsync() 匹配系统药材，匹配失败则 HerbId=null、IsValidated=false，保存供后续手动绑定。

> **[Sprint 4 已实现]** Formula DataSource 扩展: IFormulaDataSource 新增 BatchImportAsync/GetPendingValidationAsync/GetAllForExportAsync/ValidateHerbBindingsAsync 方法，Local/Remote 双模式实现 (T4-X2-19~22)

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下导入导出的支持方式 | FR-FORM-011 ~ 013 | 已确定: 支持。客户端 NPOI 本地处理，不依赖 API |
| 2 | 验方复制到处方时的价格计算规则 | FR-FORM-008 | 已确定: 根据 HerbId 查药材库当前价格。FormulaHerbItem 不含价格字段，价格始终以药材库为准 |
| MC-D08 | 处方导入对话框的验方过滤 | FR-FORM-006 + FR-MC-016 | 已确定: 仅展示 ValidationStatus=Validated 且 Status=Enabled 的验方。Draft 验方需先完成药材绑定验证 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 FormulasController + Formula 实体 + formula-copy-flow spec 提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含核心错误 8 个 + 药材验证 5 个 + 批量/导入错误 6 个场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果] 格式，增加具体参数和返回值描述 |
| 2026-02-17 | v1.3 | Round 9: FR-FORM-006 补充处方导入过滤规则 (Validated + Enabled)，新增决策 MC-D08 |
| 2026-02-17 | v1.4 | PRD审查修复: C1-FR-FORM-001 Effect/Usage改为选填(以Server端为准)，长度统一500 |
| 2026-02-18 | v1.5 | 错误码全量分配: 3 个子类别 (601xx~603xx) 共 17 个错误码，统一 ERR-MCCEE 格式 + 枚举名 |
| 2026-02-21 | v1.6 | PRD vs Code 偏差分析修订: 1 项修订 (FORM-14 Name字段最大长度) |
| 2026-02-26 | v1.7 | **Sprint 4 已实现标记**: IFormulaDataSource 扩展 BatchImportAsync/GetPendingValidationAsync/GetAllForExportAsync/ValidateHerbBindingsAsync (T4-X2-19~22) |
| 2026-02-28 | v1.8 | **PRD 偏差修复**: Create 端点返回码明确标注 200 (PRD-11) |
