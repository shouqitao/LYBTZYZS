# 药材管理 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所日常开方需要从药材库中选取药材，药材库的维护质量直接影响开方效率和准确性。缺乏统一的药材管理系统意味着: 药材信息散落在纸质记录或个人经验中，价格更新不及时导致结算偏差，新药材录入重复劳动，批量药材数据无法高效导入导出。同时，药材与处方存在引用关系，错误删除已引用药材会导致历史处方数据完整性受损。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 开方时查找药材靠记忆，无拼音码快速检索 | 每张处方多花 1-2 分钟查找药材，日均浪费 20-40 分钟 |
| 医生 | 想新增常用药材但权限不明确 | 需要找管理员代为录入，打断诊疗节奏 |
| 管理员 | 新开诊所需录入数百种药材，逐条手工输入 | 初始化药材库耗时数天，易出错 |
| 管理员 | 药材价格调整后不知道是否影响已有处方 | 担心历史处方金额被篡改，不敢轻易改价 |
| 管理员 | 误删被处方引用的药材导致数据不一致 | 历史处方出现 "药材不存在" 错误 |

### 1.3 证据

- 临床工作流观察: 中药处方平均包含 8-15 味药材，快速检索直接影响开方效率
- 药材库规模: 常用中药材 300-500 种，含产地、规格、功效等多维度信息
- 初始化需求: 新诊所需一次性导入完整药材库，手工逐条录入不可行
- 数据完整性: 药材与处方存在外键引用关系，删除操作需引用检查

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部药材 |
| Admin | CRUD 全部药材 |
| Doctor | 创建药材; 编辑/删除/启用/禁用自己创建的药材; 查看全部药材 |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。Update/Delete/ToggleStatus 包含所有权检查: Admin/SuperAdmin 可操作全部，Doctor 仅可操作自己创建的数据。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 开方效率 | 拼音码快速检索 + 分类筛选，药材选取从 "翻找" 变为 "秒达" |
| 数据准确 | 统一药材库确保名称、价格、规格等信息一致，减少人为录入差异 |
| 快速初始化 | Excel/JSON 批量导入支持新诊所数天变数分钟完成药材库建设 |
| 数据安全 | 引用检查 + 软删除 + 恢复机制，防止误操作导致数据丢失 |
| 离线可用 | 本地模式完整支持药材 CRUD 和导入导出，外出诊疗不受网络限制 |

### 3.2 Why Now

药材库是处方模块的基础依赖 -- 没有药材数据，开方功能无法使用。药材管理是系统上线的前置条件，必须在处方模块之前完成。

---

## 4. Solution Overview

药材管理模块负责中药材库的完整生命周期管理，包括基本信息维护、分类、价格管理、启用/禁用状态控制、批量导入导出、以及引用安全检查。

**核心能力:**
- **药材 CRUD**: 创建/查看/更新/软删除/恢复，自动生成拼音码
- **状态管理**: 启用/禁用切换 (单个 + 批量)，禁用药材开方时不可选
- **批量操作**: Excel 导入、JSON 批量导入 (最多 10000 条)、Excel/JSON 导出、批量删除
- **引用安全**: 删除前检查处方/验方引用，有引用则禁止删除并建议使用禁用功能
- **双模式支持**: 远程 (HTTP API + SQL Server) + 本地 (LocalWebAPI → LocalDB)
- **缓存策略**: Desktop 全量预加载到内存 (IHerbCacheService)，开方时 0ms 纯内存过滤

**药材生命周期:**
```
创建 (Enabled) → 正常使用 (开方可选)
              → 禁用 (Disabled，开方不可选，历史处方不受影响)
              → 软删除 (IsDeleted=true，需无引用)
              → 恢复 (IsDeleted=false，回到 Enabled/Disabled 状态)
```

---

## 5. Success Metrics

| 指标 | 当前 (纸质/无系统) | v1.0 目标 | 衡量方式 |
|------|-------------------|----------|---------|
| 药材查找耗时 | 30 秒+ (翻找/回忆) | < 2 秒 (拼音码/分类筛选) | 操作日志 |
| 药材库初始化 | 2-3 天 (手工录入) | < 30 分钟 (Excel 导入) | 导入耗时 |
| 批量导入速度 | N/A | 10000 条 < 5 秒 | 性能测试 |
| 误删导致数据不一致 | 有风险 | 0 次 (引用检查拦截) | 错误日志 |
| 药材数据覆盖率 | 部分 | 100% 常用药材 (300-500 种) | 药材库统计 |

---

## 6. Epic Hypothesis

We believe that 实现拼音码快速检索 + Excel/JSON 批量导入导出 + 引用安全删除 + 全量内存缓存的药材管理系统 for 诊所管理员和医生 will achieve 开方效率大幅提升与药材数据完整性保障。We'll know we're right when 药材查找耗时 < 2 秒、药材库初始化 < 30 分钟、且零次因误删导致的数据不一致事件。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-HERB-001 | 创建药材 | Must |
| US-HERB-002 | 查看药材列表 | Must |
| US-HERB-003 | 查看药材详情 | Must |
| US-HERB-004 | 更新药材信息 | Must |
| US-HERB-005 | 删除药材 | Must |
| US-HERB-006 | 启用/禁用药材 | Should |
| US-HERB-007 | 恢复已删除药材 | Could |
| US-HERB-008 | 批量删除 | Should |
| US-HERB-009 | Excel 导入 | Should |
| US-HERB-010 | JSON 批量导入 | Could |
| US-HERB-011 | 导出药材数据 | Should |
| US-HERB-012 | 下载导入模板 | Could |
| US-HERB-013 | 检查药材引用 | Could |

---

### US-HERB-001: 创建药材

> As a 医生/管理员, I want to 在药材库中创建新的中药材记录,
> so that 新增药材可以在开方时被选用。

**Acceptance Criteria:**
- [ ] 填写必填字段 (名称、单位、单价) 并提交 → 创建成功，拼音码自动生成
- [ ] Price=0 → 返回 400 验证失败
- [ ] 名称为空 → 返回 400 验证失败
- [ ] 创建成功 → 默认状态为 Enabled

**Business Rules:**
1. 名称必填，1-100 字符
2. 单位必填，默认 "克"
3. 单价必须大于 0，最大 100000
4. 自动生成拼音码 (PinYinCode) 用于快速搜索
5. 默认状态为 Enabled

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/herbs`，返回 HerbDetailDto (200) |
| 本地 | DataSource 本地存储 |

### US-HERB-002: 查看药材列表

> As a 医生/管理员, I want to 分页查看药材列表并支持关键词和分类筛选,
> so that 我可以快速找到需要的药材。

**Acceptance Criteria:**
- [ ] category="补血药" → 仅返回补血药分类
- [ ] keyword="DG" → 返回拼音码包含 DG 的药材
- [ ] pageSize=101 → 返回 400 (ERR-50106)
- [ ] 无筛选条件 → 默认按名称升序排列

**Business Rules:**
1. 支持按名称和拼音码搜索 (keyword)
2. 支持按分类筛选 (category)
3. 默认分页: page=1, pageSize=20
4. 列表缓存: IMemoryCache (HERB-D01)
5. 默认按名称升序排列
6. 分页参数验证: page >= 1, pageSize 1-100 (见 [nfr.md](17-nfr.md) NFR-API-001)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/herbs?keyword=&category=&page=&pageSize=` |
| 本地 | 本地 LocalDB 查询 |

### US-HERB-003: 查看药材详情

> As a 医生/管理员, I want to 获取单个药材的完整信息,
> so that 我可以查看药材的成本价、功效、用法等详细数据。

**Acceptance Criteria:**
- [ ] 有效 ID → 返回 200 + HerbDetailDto (含成本价/功效/用法)
- [ ] 无效 ID → 返回 404 (ERR-50101)

**Business Rules:**
1. 返回 HerbDetailDto (含成本价、功效、用法等完整字段)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/herbs/{id}` |
| 本地 | 本地查询 |

### US-HERB-004: 更新药材信息

> As a 管理员, I want to 修改药材的基本信息和价格,
> so that 药材库数据保持准确和最新。

**Acceptance Criteria:**
- [ ] Name 变更 → PinYinCode 自动重新生成
- [ ] 请求体含 Status 字段 → 忽略，状态不变
- [ ] Doctor 操作他人创建的药材 → 返回 403 (ERR-50103)

**Business Rules:**
1. 统一所有权检查
2. 名称变更时自动重新生成拼音码
3. InputDto 不含 Status 字段，状态变更通过专用 API

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/herbs/{id}` |
| 本地 | 本地更新 |

### US-HERB-005: 删除药材

> As a 管理员, I want to 删除不再使用的药材,
> so that 药材库保持整洁，开方时不会选到废弃药材。

**Acceptance Criteria:**
- [ ] 药材有处方引用 → 返回 422 "该药材被处方引用，无法删除，请使用禁用功能"
- [ ] 药材无处方引用 → 软删除成功，默认列表查询不返回该药材
- [ ] Doctor 操作他人创建的药材 → 返回 403 (ERR-50103)

**Business Rules:**
1. **引用检查: 有处方引用 (PrescriptionItem) 或验方引用 (FormulaItem) 的药材禁止删除，返回 422，建议使用禁用功能** (BR-DEL-001, HERB-D03)
2. 无引用关系时执行软删除，数据保留
3. 统一所有权检查

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | DELETE `/api/v1/herbs/{id}` |
| 本地 | 本地软删除 |

### US-HERB-006: 启用/禁用药材

> As a 管理员, I want to 切换药材的启用/禁用状态,
> so that 暂时不用的药材不会出现在开方选择列表中，同时保留历史数据。

**Acceptance Criteria:**
- [ ] 药材 Status=Disabled → 处方药材选择列表不显示
- [ ] 批量启用/禁用 → 返回 BatchOperationResultDto

**Business Rules:**
1. 统一所有权检查
2. 禁用后在开方时不可选择 (处方模块过滤)
3. 支持批量启用/禁用

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/herbs/{id}/toggle-status`，批量: POST `/api/v1/herbs/batch-enable` 或 `/batch-disable` |
| 本地 | 本地状态切换 |

### US-HERB-007: 恢复已删除药材

> As a 管理员, I want to 恢复误删的药材,
> so that 不需要重新手动录入，减少操作失误的影响。

**Acceptance Criteria:**
- [ ] 恢复成功 → 药材重新出现在默认列表查询中
- [ ] 恢复未删除的药材 → 返回 200 + "该药材未被删除，无需恢复" (ERR-50104)

**Business Rules:**
1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/herbs/{id}/restore` |
| 本地 | 本地恢复 |

### US-HERB-008: 批量删除

> As a 管理员, I want to 一次性删除多个不需要的药材,
> so that 清理药材库时不需要逐条操作。

**Acceptance Criteria:**
- [ ] 批量删除 → 返回 BatchOperationResultDto (successCount/failureCount)
- [ ] ID 列表为空 → 返回 400 (ERR-50201)

**Business Rules:**
1. 项级错误隔离
2. 返回 BatchOperationResultDto

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/herbs/batch-delete` |
| 本地 | 本地批量操作 |

### US-HERB-009: Excel 导入

> As a 管理员, I want to 从 Excel 文件批量导入药材数据,
> so that 新开诊所可以快速初始化药材库，无需逐条手工录入。

**Acceptance Criteria:**
- [ ] .xlsx 文件 → 正确解析名称/单位/单价等列
- [ ] 行级验证失败 → 返回行号+失败原因
- [ ] 非 .xlsx 文件 → 返回 400 (ERR-50302)
- [ ] 文件超过 10MB → 返回 400 (ERR-50303)

**Business Rules:**
1. 支持 .xlsx 格式
2. 行级错误隔离
3. 自动生成拼音码
4. 导入列: 药材名称\*、单位\*、单价\*、产地、规格、功效、用法用量、备注

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/herbs/import` (multipart/form-data) |
| 本地 | 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext |

### US-HERB-010: JSON 批量导入

> As a 管理员, I want to 通过 JSON 格式批量导入药材,
> so that 可以从其他系统迁移药材数据，并灵活控制重复处理策略。

**Acceptance Criteria:**
- [ ] 10001 条 → 返回 400 "批量导入最多支持10000条记录" (ERR-50202)
- [ ] Skip 跳过/Update 覆盖/Error 报错各策略正确执行
- [ ] 导入完成 → 触发 IHerbCacheService 全量重加载 (HERB-D01)

**Business Rules:**
1. 最多 10000 条
2. 支持重复处理策略: Skip (跳过) / Update (覆盖) / Error (报错)
3. 返回详细导入结果
4. 内存 HashSet 判重 (避免 10000 次查询) + 分批 100 条/批 SaveChanges (HERB-D02)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/herbs/batch-import` |
| 本地 | 支持。客户端本地解析 JSON 文件，直接写入 LocalDbContext |

### US-HERB-011: 导出药材数据

> As a 管理员, I want to 将药材数据导出为 Excel 或 JSON 文件,
> so that 可以备份数据或在其他系统中使用。

**Acceptance Criteria:**
- [ ] Excel 导出 → 文件包含所有药材字段
- [ ] JSON 导出 → 全量导出，Desktop 负责 Excel 生成

**Business Rules:**
1. 支持按分类筛选导出
2. 两种导出方式: Excel (服务端生成) 和 JSON (全量导出，Desktop 负责 Excel 生成)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/herbs/export?category=` (Excel), GET `/api/v1/herbs/export-all` (JSON) |
| 本地 | 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件 |

### US-HERB-012: 下载导入模板

> As a 管理员, I want to 下载药材导入的 Excel 模板,
> so that 我知道导入文件需要哪些列和格式。

**Acceptance Criteria:**
- [ ] GET 请求 → 返回 .xlsx 文件，含正确表头和示例数据
- [ ] 无需登录即可下载 (AllowAnonymous)

**Business Rules:**
1. 允许匿名访问
2. 包含表头和示例数据

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/herbs/import-template` (AllowAnonymous) |
| 本地 | 内置模板 |

### US-HERB-013: 检查药材引用

> As a 管理员, I want to 在删除药材前检查其是否被处方或验方引用,
> so that 我可以了解删除影响范围并做出正确决策。

**Acceptance Criteria:**
- [ ] 药材被 5 个处方引用 → 返回 referenceCount=5, canDelete=false
- [ ] 药材无引用 → 返回 referenceCount=0, canDelete=true
- [ ] 批量检查超过 100 条 → 返回 400 (ERR-50203)

**Business Rules:**
1. 返回引用次数和最近 5 条处方引用
2. **有处方引用时 CanDelete=false** (BR-DEL-001)，提示使用禁用功能替代
3. 支持单个和批量检查 (批量最多 100 条)
4. 检查 PrescriptionItem + FormulaItem 双重引用 (HERB-D03)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/herbs/{id}/check-reference`，批量: POST `/api/v1/herbs/batch-check-reference` |
| 本地 | 本地检查 |

> **[Sprint 4 已实现]** Herb DataSource 扩展: IHerbDataSource 新增 BatchToggleStatusAsync/BatchImportAsync/GetAllForExportAsync/HasReferencesAsync/GetImportTemplateColumns 方法，Local/Remote 双模式实现 (T4-X2-13~18)

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 药材图片上传 | 增加存储复杂度，诊所开方不依赖药材图片，v2.0+ 考虑 |
| 药材库版本化/审计日志 | 小诊所场景不需要药材变更审计，v2.0+ 考虑 |
| 药材间配伍禁忌检查 | 属于处方校验功能范畴，不在药材管理模块内 |
| 药材库存管理 (进销存) | 超出 v1.0 范围，当前仅管理药材目录信息 |
| 药材价格历史记录 | 价格变更不影响已有处方 (快照机制)，暂不追溯价格变更历史 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 批量导入 10000 条性能 | 导入耗时过长影响用户体验 | HashSet 内存判重 + 分批 100 条/批 SaveChanges，目标 3-5 秒 (HERB-D02) |
| 药材价格变更影响已有处方 | 历史处方金额被意外修改 | PrescriptionItem.UnitPrice 为开方时快照值，新处方使用当前价格 (决策 #2) |
| 并发编辑同一药材 | 数据覆盖 | Last-Write-Wins 策略，小诊所 1-2 个管理员并发极低 (HERB-D04) |
| 本地模式 Excel 解析依赖 NPOI | 本地模式导入导出功能可用性 | 客户端内置 NPOI 库，不依赖网络 |
| Formula 引用检查遗漏 | 删除被验方引用的药材导致数据不一致 | 删除时检查 PrescriptionItem + FormulaItem 双重引用 (HERB-D03) |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-HERB-01 | 药材分类是否需要支持自定义? 当前为固定枚举 (补血药、补气药等) | 待定。v1.0 使用固定分类，v2.0 考虑自定义分类管理 |
| OQ-HERB-02 | 药材名称是否需要唯一性约束? | 待定。当前无唯一约束，批量导入通过重复策略处理 |
| OQ-HERB-03 | 禁用药材是否需要在处方详情页标注 "已禁用" 状态? | 待定。当前仅在开方选择时过滤，历史处方不标注 |

---

## Data Model

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

## Error Codes

> Service 层采用 Result 模式统一返回。错误码分区: 5xxxx，编号体系: MCCEE (M=模块5, CC=子类别, EE=序号)。所有权检查: Admin/SuperAdmin 可操作全部，Doctor 仅可操作自己创建的数据。

### 核心错误 (501xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-50101 | HerbNotFound | 404 | 药材不存在 | GetById/Update/Delete/Restore 时 ID 无效或已被软删除 |
| ERR-50102 | HerbValidationFailed | 400 | (FluentValidation 错误列表) | 名称/单位/价格等字段验证不通过 |
| ERR-50103 | HerbNoPermission | 403 | 您没有权限操作此药材，只能操作自己创建的数据 | Doctor 操作他人创建的药材 (Update/Delete/ToggleStatus/Restore) |
| ERR-50104 | HerbNotDeleted | 200 | 该药材未被删除，无需恢复 | 恢复未软删除的药材 |
| ERR-50106 | HerbInvalidPagination | 400 | 页码和页大小参数无效（页码>0，页大小1-100） | 分页参数校验失败 ([nfr.md](17-nfr.md) NFR-API-001) |

### 批量操作错误 (502xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-50201 | HerbBatchEmpty | 400 | 请至少选择一个药材 | 批量删除/启用/禁用时 ID 列表为空 |
| ERR-50202 | HerbBatchImportExceeded | 400 | 批量导入最多支持10000条记录 | BatchImport herbs.Count > 10000 |
| ERR-50203 | HerbBatchCheckExceeded | 400 | 批量检查最多支持100条记录 | BatchCheckReference herbIds.Count > 100 |
| ERR-50204 | HerbBatchItemNotFound | 200 | 药材不存在 | 批量删除/状态切换时单项不存在 |
| ERR-50205 | HerbBatchItemDeletedOrMissing | 200 | 药材不存在或已删除 | 批量状态更新时实体不存在或已软删除 |
| ERR-50206 | HerbBatchItemError | 200 | 删除操作失败 / 状态更新失败 | 数据库异常，使用安全消息 |

### Excel 导入错误 (US-HERB-009, 503xx)

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

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| HERB-D01 | 药材缓存策略 | US-HERB-002 | 已确定: Desktop 全量预加载 Enabled 药材到 IHerbCacheService (Dict+拼音前缀索引+分类索引)，开处方 0ms 纯内存过滤。Server IMemoryCache + Tag-based Eviction 替代 OutputCache。增量: 单条 CRUD 更新内存; 全量重加载: 批量导入/模式切换/同步完成/闲置30min/登录。参考 POS/EMR/IDE 补全模式 |
| HERB-D02 | 批量导入性能 | US-HERB-010 | 已确定: 内存 HashSet 判重 (避免 10000 次查询) + 分批 100 条/批 SaveChanges，10000 条 3~5 秒完成。进度反馈: 简单进度条 (3~5秒不需轮询)。导入完成后触发 IHerbCacheService 全量重加载 (HERB-D01) |
| HERB-D03 | Formula 引用检查扩展 | US-HERB-005/013 | 已确定: 删除药材时检查 PrescriptionItem + FormulaItem 双重引用，任一有引用则 CanDelete=false。CheckReference 返回值增加 FormulaReferenceCount。禁用不改验方本身，导入处方时禁用药材自动跳过并提示 |
| HERB-D04 | 并发策略 | Herb 实体 | 已确定: Last-Write-Wins，不加 RowVersion。小诊所 1~2 个管理员，并发编辑同一药材概率极低。MedicalCase 有 RowVersion 是因为多医生同时操作概率更高 |
| HERB-D05 | 本地模式下导入导出的支持方式 | US-HERB-009~012 | 已确定: 支持。客户端 NPOI/本地 JSON 解析，不依赖 API |
| HERB-D06 | 药材价格变更对已有处方的影响策略 | US-HERB-004 | 已确定: 不影响。PrescriptionItem.UnitPrice 为开方时快照值，新处方使用当前价格 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | Price 最小值从 >0 修订为 >=0.01 | 代码实现 >0.01 更合理，避免极小金额无实际意义 | HERB-05 |
| 2026-02-21 | Price 最大值从 999999.99 修订为 100000 | 代码实现 100000 上限更符合实际业务场景 | HERB-06 |
| 2026-02-28 | Create 端点返回码明确标注 200 | PRD 偏差修复 | PRD-11 |

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
| 2026-02-22 | v1.7 | Phase 2 模块功能细化: 新增 HERB-D01~D04 决策记录 |
| 2026-02-26 | v1.8 | Sprint 4 已实现标记: IHerbDataSource 扩展 (T4-X2-13~18) |
| 2026-02-28 | v1.9 | PRD 偏差修复: Create 端点返回码明确标注 200 (PRD-11) |
| 2026-03-06 | v2.0 | PRD 全面重写: FR→US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节，修订注释迁移到 Decision Log 修订历史子表，决策记录统一 HERB-Dxx 编号 |
