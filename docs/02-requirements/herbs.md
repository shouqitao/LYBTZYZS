# 药材管理 需求规格

## 概述

药材管理模块负责中药材库的维护，包括药材基本信息、分类、价格、启用/禁用状态管理。支持 Excel 和 JSON 批量导入导出，以及药材被处方引用的检查。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部药材 |
| Admin | CRUD 全部药材 |
| Doctor | 查看药材、创建药材 |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。Update/Delete/ToggleStatus 包含所有权检查。

---

## 功能清单

> **[已修订 2026-02-21]** Price 最小值从 >0 修订为 >=0.01，避免极小金额无实际意义
> 原因: 代码实现 >0.01 更合理，PRD 对齐  |  参考: HERB-05

> **[已修订 2026-02-21]** Price 最大值从 999999.99 修订为 100000，10 万上限更务实
> 原因: 代码实现 100000 上限更符合实际业务场景  |  参考: HERB-06

### FR-HERB-001: 创建药材

- **描述**: 在药材库中创建新的中药材记录
- **业务规则**:
  1. 名称必填，1-100 字符
  2. 单位必填，默认"克"
  3. 单价必须大于 0，最大 999999.99
  4. 自动生成拼音码 (PinYinCode) 用于快速搜索
  5. 默认状态为 Enabled
- **远程模式**: POST `/api/v1/herbs`，返回 HerbDetailDto
- **本地模式**: DataSource 本地存储
- **验收标准**:
  - [ ] 创建成功 -> 拼音码自动生成
  - [ ] Price=0 -> 返回 400 验证失败

### FR-HERB-002: 查看药材列表

- **描述**: 分页查看药材列表，支持关键词和分类筛选
- **业务规则**:
  1. 支持按名称和拼音码搜索 (keyword)
  2. 支持按分类筛选 (category)
  3. 默认分页: page=1, pageSize=20
  4. 列表缓存: OutputCache("HerbsCache")
  5. 默认按名称升序排列
  6. 分页参数验证: page >= 1, pageSize 1-100 (见 [nfr.md](nfr.md) NFR-API-001)
- **远程模式**: GET `/api/v1/herbs?keyword=&category=&page=&pageSize=`
- **本地模式**: 本地 SQLite 查询
- **验收标准**:
  - [ ] category="补血药" -> 仅返回补血药分类
  - [ ] keyword="DG" -> 返回拼音码包含 DG 的药材
  - [ ] pageSize=101 -> 返回 400 (ERR-50106)

### FR-HERB-003: 查看药材详情

- **描述**: 获取单个药材的完整信息
- **业务规则**:
  1. 返回 HerbDetailDto (含成本价、功效、用法等完整字段)
- **远程模式**: GET `/api/v1/herbs/{id}`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 有效ID -> 返回 200 + HerbDetailDto (含成本价/功效/用法)

### FR-HERB-004: 更新药材信息

- **描述**: 修改药材的基本信息和价格
- **业务规则**:
  1. 统一所有权检查
  2. 名称变更时自动重新生成拼音码
  3. InputDto 不含 Status 字段，状态变更通过专用 API
- **远程模式**: PUT `/api/v1/herbs/{id}`
- **本地模式**: 本地更新
- **验收标准**:
  - [ ] Name 变更 -> PinYinCode 自动重新生成
  - [ ] 请求体含 Status 字段 -> 忽略，状态不变

### FR-HERB-005: 删除药材

- **描述**: 软删除药材 (IsDeleted=true)
- **业务规则**:
  1. **引用检查: 有处方引用 (PrescriptionItem) 的药材禁止删除，返回 422，建议使用禁用功能** (BR-DEL-001)
  2. 无引用关系时执行软删除，数据保留
  3. 统一所有权检查
- **远程模式**: DELETE `/api/v1/herbs/{id}`
- **本地模式**: 本地软删除
- **验收标准**:
  - [ ] 药材有处方引用 -> 返回 422 "该药材被处方引用，无法删除，请使用禁用功能"
  - [ ] 药材无处方引用 -> 软删除成功，默认列表查询不返回该药材

### FR-HERB-006: 启用/禁用药材

- **描述**: 切换药材的启用/禁用状态
- **业务规则**:
  1. 统一所有权检查
  2. 禁用后在开方时不可选择 (处方模块过滤)
  3. 支持批量启用/禁用
- **远程模式**: POST `/api/v1/herbs/{id}/toggle-status`，批量: POST `/api/v1/herbs/batch-enable` 或 `/batch-disable`
- **本地模式**: 本地状态切换
- **验收标准**:
  - [ ] 药材 Status=Disabled -> 处方药材选择列表不显示
  - [ ] 批量启用/禁用 -> 返回 BatchOperationResultDto

### FR-HERB-007: 恢复已删除药材

- **描述**: 恢复软删除的药材
- **业务规则**:
  1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
- **远程模式**: POST `/api/v1/herbs/{id}/restore`
- **本地模式**: 本地恢复
- **验收标准**:
  - [ ] 恢复成功 -> 药材重新出现在默认列表查询中

### FR-HERB-008: 批量删除

- **描述**: 批量软删除多个药材
- **业务规则**:
  1. 项级错误隔离
  2. 返回 BatchOperationResultDto
- **远程模式**: POST `/api/v1/herbs/batch-delete`
- **本地模式**: 本地批量操作
- **验收标准**:
  - [ ] 批量删除 -> 返回 BatchOperationResultDto (successCount/failureCount)

### FR-HERB-009: Excel 导入

- **描述**: 从 Excel 文件导入药材数据
- **业务规则**:
  1. 支持 .xlsx 格式
  2. 行级错误隔离
  3. 自动生成拼音码
  4. 导入列: 药材名称\*、单位\*、单价\*、产地、规格、功效、用法用量、备注
- **远程模式**: POST `/api/v1/herbs/import` (multipart/form-data)
- **本地模式**: 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext
- **验收标准**:
  - [ ] .xlsx 文件 -> 正确解析名称/单位/单价等列
  - [ ] 行级验证失败 -> 返回行号+失败原因

### FR-HERB-010: JSON 批量导入

- **描述**: 通过 JSON 格式批量导入药材
- **业务规则**:
  1. 最多 10000 条
  2. 支持重复处理策略: Skip (跳过) / Update (覆盖) / Error (报错)
  3. 返回详细导入结果
- **远程模式**: POST `/api/v1/herbs/batch-import`
- **本地模式**: 支持。客户端本地解析 JSON 文件，直接写入 LocalDbContext
- **验收标准**:
  - [ ] 10001条 -> 返回 400 "批量导入最多支持10000条记录"
  - [ ] Skip 跳过/Update 覆盖/Error 报错 各策略正确执行

### FR-HERB-011: 导出药材数据

- **描述**: 将药材数据导出为 Excel 文件
- **业务规则**:
  1. 支持按分类筛选导出
  2. 两种导出方式: Excel (服务端生成) 和 JSON (全量导出，Desktop 负责 Excel 生成)
- **远程模式**: GET `/api/v1/herbs/export?category=` (Excel), GET `/api/v1/herbs/export-all` (JSON)
- **本地模式**: 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件
- **验收标准**:
  - [ ] 导出 -> Excel 包含所有药材字段

### FR-HERB-012: 下载导入模板

- **描述**: 下载药材导入 Excel 模板
- **业务规则**:
  1. 允许匿名访问
  2. 包含表头和示例数据
- **远程模式**: GET `/api/v1/herbs/import-template` (AllowAnonymous)
- **本地模式**: 内置模板
- **验收标准**:
  - [ ] GET 请求 -> 返回 .xlsx 文件，含正确表头和示例数据

### FR-HERB-013: 检查药材引用

- **描述**: 检查药材是否被处方引用
- **业务规则**:
  1. 返回引用次数和最近 5 条处方引用
  2. **有处方引用时 CanDelete=false** (BR-DEL-001)，提示使用禁用功能替代
  3. 支持单个和批量检查 (批量最多 100 条)
- **远程模式**: GET `/api/v1/herbs/{id}/check-reference`，批量: POST `/api/v1/herbs/batch-check-reference`
- **本地模式**: 本地检查
- **验收标准**:
  - [ ] 药材被5个处方引用 -> 返回 referenceCount=5, canDelete=false

---

## 数据模型

### Herb (药材实体)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 药材ID |
| Name | string(100) | Required | 药材名称 |
| PinYinCode | string(50)? | - | 拼音码 (系统生成) |
| Category | string(50)? | - | 分类 (补血药、补气药等) |
| Origin | string(100)? | - | 产地 |
| Spec | string(100)? | - | 规格 |
| Unit | string(10) | Required, Default: "克" | 单位 |
| Price | decimal(18,2) | Required, >0 | 单价 (元/单位) |
| CostPrice | decimal(18,2)? | - | 成本价 |
| Effect | string(500)? | - | 功效说明 |
| Usage | string(500)? | - | 用法用量 |
| Remark | string(500)? | - | 备注 |
| Status | CommonStatus | Default: Enabled | 药材状态 |

> 继承 BaseEntity

---

## 错误码

> Service 层采用 Result 模式统一返回。错误码分区: 5xxxx，编号体系: MCCEE (M=模块5, CC=子类别, EE=序号)。所有权检查: Admin/SuperAdmin 可操作全部，Doctor 仅可操作自己创建的数据。

### 核心错误 (501xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-50101 | HerbNotFound | 404 | 药材不存在 | GetById/Update/Delete/Restore 时 ID 无效或已被软删除 |
| ERR-50102 | HerbValidationFailed | 400 | (FluentValidation 错误列表) | 名称/单位/价格等字段验证不通过 |
| ERR-50103 | HerbNoPermission | 403 | 您没有权限操作此药材，只能操作自己创建的数据 | Doctor 操作他人创建的药材 (Update/Delete/ToggleStatus/Restore) |
| ERR-50104 | HerbNotDeleted | 200 | 该药材未被删除，无需恢复 | 恢复未软删除的药材 |
| ERR-50106 | HerbInvalidPagination | 400 | 页码和页大小参数无效（页码>0，页大小1-100） | 分页参数校验失败 ([nfr.md](nfr.md) NFR-API-001) |

### 批量操作错误 (502xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-50201 | HerbBatchEmpty | 400 | 请至少选择一个药材 | 批量删除/启用/禁用时 ID 列表为空 |
| ERR-50202 | HerbBatchImportExceeded | 400 | 批量导入最多支持10000条记录 | BatchImport herbs.Count > 10000 |
| ERR-50203 | HerbBatchCheckExceeded | 400 | 批量检查最多支持100条记录 | BatchCheckReference herbIds.Count > 100 |
| ERR-50204 | HerbBatchItemNotFound | 200 | 药材不存在 | 批量删除/状态切换时单项不存在 |
| ERR-50205 | HerbBatchItemDeletedOrMissing | 200 | 药材不存在或已删除 | 批量状态更新时实体不存在或已软删除 |
| ERR-50206 | HerbBatchItemError | 200 | 删除操作失败 / 状态更新失败 | 数据库异常，使用安全消息 |

### Excel 导入错误 (FR-HERB-009, 503xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-50301 | HerbImportFileEmpty | 400 | 文件不能为空 | file==null 或 file.Length==0 |
| ERR-50302 | HerbImportFileFormat | 400 | 仅支持.xlsx格式的Excel文件 | 扩展名不是 .xlsx |
| ERR-50303 | HerbImportFileSize | 400 | 文件大小不能超过10MB | file.Length > 10MB |
| ERR-50304 | HerbImportExcelError | 200 | Excel文件格式错误 | 无工作表 |
| ERR-50305 | HerbImportNoData | 200 | Excel文件中没有数据行 | 行数<=1 |

### 导入行级错误 (部分成功模式)

| 失败原因 | 类型 | 触发条件 |
|----------|------|----------|
| 药材名称不能为空 | 验证失败 | 第1列为空 |
| 单位不能为空 | 验证失败 | 第2列为空 |
| 单价格式错误或必须大于0 | 验证失败 | 价格非数字或 <=0 |
| 药材名称重复 (Error策略) | 业务约束 | DuplicateStrategy=Error 且名称已存在 |
| 导入失败：数据处理异常 | 技术异常 | 行级 try-catch 捕获，使用安全消息 |

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下导入导出的支持方式 | FR-HERB-009 ~ 012 | 已确定: 支持。客户端 NPOI/本地 JSON 解析，不依赖 API |
| 2 | 药材价格变更对已有处方的影响策略 | FR-HERB-004 | 已确定: 不影响。PrescriptionItem.UnitPrice 为开方时快照值，新处方使用当前价格 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 HerbsController + HerbModel + herb-card-control spec 提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含核心错误 4 个 + 批量操作 6 个 + 导入错误 10 个场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果]，13 个 FR 共 19 条验收标准 |
| 2026-02-17 | v1.3 | PRD审查修复: A7-FR-HERB-005/013 对齐统一删除策略(BR-DEL-001)，有处方引用时禁止删除 |
| 2026-02-18 | v1.4 | 错误码全量分配: 3 个子类别 (501xx~503xx) 共 15 个错误码，统一 ERR-MCCEE 格式 + 枚举名 |
| 2026-02-18 | v1.5 | FR-HERB-002 补充分页验证规则 (NFR-API-001); 新增 ERR-50106 分页错误码 |
| 2026-02-21 | v1.6 | PRD vs Code 偏差分析修订: 2 项修订 (HERB-05 Price最小值, HERB-06 Price最大值) |
