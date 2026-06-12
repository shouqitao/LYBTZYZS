# 验方管理 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所在长期临床实践中积累了大量经验方 (验方)，这些验方目前以纸质笔记、个人文档或口头传承的形式存在。缺乏统一的数字化管理导致验方难以检索、共享和复用。同时，从旧系统迁移的验方数据中药材名称与当前药材库不一致，需要系统化的验证和绑定机制。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 经验方分散在个人笔记中，开方时无法快速调用 | 重复输入药材组成，日均浪费 10-15 分钟 |
| 医生 | 其他医生的优秀验方无法查阅学习 | 临床经验无法在团队内流通 |
| 管理员 | 从旧系统导入的验方药材名称不规范 | 无法准确计算价格，处方数据质量差 |
| 管理员 | 无法控制哪些验方可用于开方 | 未验证的验方可能导致处方错误 |

### 1.3 证据

- 临床工作流观察: 医生开具处方时经常使用固定的经验方组合，手动输入效率低
- 数据迁移需求: 旧系统 (纸质/Excel) 验方导入后药材名称与系统药材库不匹配
- 产品需求分析: 处方模块 (MedicalCase) 需要从验方导入药材组成 (US-MC-016)

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部验方 |
| Admin | CRUD 全部验方 |
| Doctor | CRUD 自己的验方 + 查看共享验方 (只读) |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。资源级权限: Doctor 查看自己创建的 + IsShared=true 的验方。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 验方数字化 | 将纸质/口头经验方统一录入系统，实现结构化存储和快速检索 |
| 处方效率 | 开方时一键导入验方药材组成，减少手动输入，提升诊疗效率 |
| 团队协作 | 验方共享机制让优秀经验方在医生团队内流通 |
| 数据质量 | 延迟绑定 + 验证工作流确保药材数据与系统药材库一致 |
| 安全管控 | 验证状态 + 启用/禁用双重过滤，仅允许经过验证的验方用于开方 |

### 3.2 Why Now

系统已完成药材库 (Herbs) 和医案处方 (MedicalCase) 模块的基础建设。验方作为处方的模板来源，是连接药材库与处方的关键桥梁。同时旧系统数据迁移在即，需要验方导入和延迟绑定机制来承接历史数据。

---

## 4. Solution Overview

验方管理模块负责经验方模板的创建、编辑、药材组成管理和共享。支持延迟绑定 (导入时药材名称未关联系统药材库)、验方验证工作流、批量导入导出。验方是处方的模板来源，可在开具处方时导入复用。

**核心能力:**
- **验方 CRUD**: 创建、查看、编辑、删除经验方模板，含药材组成管理
- **共享机制**: 验方标记为共享后，其他医生可查阅 (只读)
- **延迟绑定**: 导入的药材名称可暂不关联系统药材库，后续手动验证绑定
- **验证工作流**: Draft (未验证) -> Validated (全部药材已绑定)，仅 Validated 验方可用于开方
- **批量操作**: 批量导入 (JSON/Excel)、批量导出 (Excel)、批量删除/启用/禁用
- **双模式支持**: 远程 (HTTP API) + 本地 (LocalWebAPI → LocalDB)

**验方生命周期:**
```
创建/导入 → Draft (药材未验证)
         → 逐个验证药材绑定
         → 全部验证完成 → Validated
         → 启用 (Enabled) + 已验证 (Validated) → 可用于处方导入
         → 禁用 (Disabled) → 处方导入不可见
         → 软删除 → 可恢复
```

---

## 5. Success Metrics

| 指标 | 当前 (纸质流程) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 验方录入率 | 0% (分散在个人笔记) | > 90% 常用验方录入系统 | 系统验方总数统计 |
| 处方导入使用率 | N/A | > 50% 处方通过验方导入 | 处方来源统计 |
| 药材验证完成率 | N/A | > 80% 导入验方完成验证 | ValidationStatus=Validated 占比 |
| 验方共享覆盖率 | 0% (口头传授) | > 30% 验方标记为共享 | IsShared=true 占比 |

---

## 6. Epic Hypothesis

We believe that 实现验方模板的数字化管理 (CRUD + 共享 + 延迟绑定验证 + 批量导入导出) for 诊所医生和管理员 will achieve 处方开具效率显著提升与历史验方数据的平稳迁移。We'll know we're right when 常用验方录入率 > 90%、处方导入使用率 > 50%、且导入验方药材验证完成率 > 80%。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-FORM-001 | 创建验方 | Must |
| US-FORM-002 | 查看验方列表 | Must |
| US-FORM-003 | 查看验方详情 | Must |
| US-FORM-004 | 更新验方 | Must |
| US-FORM-005 | 删除验方 | Must |
| US-FORM-006 | 启用/禁用验方 | Must |
| US-FORM-007 | 恢复已删除验方 | Could |
| US-FORM-008 | 共享验方 | Should |
| US-FORM-009 | 延迟绑定 | Should |
| US-FORM-010 | 获取待验证验方 | Should |
| US-FORM-011 | 批量导入 | Could |
| US-FORM-012 | 导出验方 | Should |
| US-FORM-013 | 下载导入模板 | Could |

---

### US-FORM-001: 创建验方

> As a 医生, I want to 创建新的经验方模板并录入药材组成,
> so that 我的临床经验方可以数字化保存，开方时快速复用。

**Acceptance Criteria:**
- [ ] 药材列表为空 -> 返回 400 验证失败
- [ ] 创建成功 -> ValidationStatus=Draft，记录 UserId 和 CreatedBy

**Business Rules:**
1. 名称必填，1-100 字符
2. 功效选填，最长 500 字符 (以 Server 端字段定义为准)
3. 用法选填，最长 500 字符 (以 Server 端字段定义为准)
4. 药材组成至少 1 味
5. 默认类型为 Experience (经验方)
6. 初始 ValidationStatus=Draft
7. 记录 UserId 和 CreatedBy (用于所有权判断)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/formulas`，返回 FormulaDetailDto (200) |
| 本地 | DataSource 本地存储 |

### US-FORM-002: 查看验方列表

> As a 医生, I want to 分页浏览和搜索验方列表,
> so that 我可以快速找到需要的验方用于开方或参考。

**Acceptance Criteria:**
- [ ] Doctor 查询 -> 仅返回 CreatedBy=自己 或 IsShared=true 的验方
- [ ] Admin 查询 -> 返回全部验方

**Business Rules:**
1. 支持按名称搜索 (keyword)
2. 支持按分类筛选 (category)
3. Admin 返回全部验方
4. Doctor 返回自己创建的 + 共享的验方
5. 列表包含 HerbCount (药材数量)，不显示价格 (经验方不涉及价格)
6. 分页参数验证: page >= 1, pageSize 1-100 (见 [nfr.md](17-nfr.md) NFR-API-001)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/formulas?keyword=&category=&page=&pageSize=` |
| 本地 | 本地查询 |

### US-FORM-003: 查看验方详情

> As a 医生, I want to 查看验方的完整信息和药材组成,
> so that 我可以了解验方的具体内容并决定是否用于开方。

**Acceptance Criteria:**
- [ ] 有效ID -> 返回 FormulaDetailDto + Herbs 列表 (含 IsValidated)

**Business Rules:**
1. 返回 FormulaDetailDto 含完整 Herbs 列表
2. 包含每味药材的验证状态 (IsValidated)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/formulas/{id}` |
| 本地 | 本地查询 |

### US-FORM-004: 更新验方

> As a 医生, I want to 修改自己创建的验方信息和药材组成,
> so that 验方内容可以随临床经验积累持续优化。

**Acceptance Criteria:**
- [ ] Doctor 编辑 CreatedBy!=自己的验方 -> 返回 403
- [ ] 更新 Herbs 列表 -> 原有 Herbs 全部替换为新列表

**Business Rules:**
1. 统一所有权检查 (Doctor 只能编辑自己的)
2. 药材组成采用粗粒度替换策略: 完整替换 Herbs 集合
3. 药材组成至少 1 味

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/formulas/{id}` |
| 本地 | 本地更新 |

### US-FORM-005: 删除验方

> As a 医生, I want to 删除不再使用的验方,
> so that 验方列表保持整洁，只保留有价值的经验方。

**Acceptance Criteria:**
- [ ] Doctor 删除 CreatedBy!=自己的验方 -> 返回 403
- [ ] 删除后验方不出现在默认列表查询中

**Business Rules:**
1. 统一所有权检查
2. 软删除，数据保留
3. 支持批量删除

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | DELETE `/api/v1/formulas/{id}`，批量: POST `/api/v1/formulas/batch-delete` |
| 本地 | 本地软删除 |

### US-FORM-006: 启用/禁用验方

> As a 管理员, I want to 控制验方的启用/禁用状态,
> so that 未经审核或有问题的验方不会出现在医生的处方导入列表中。

**Acceptance Criteria:**
- [ ] 验方 Status=Disabled -> 开方时验方导入列表不显示
- [ ] 处方导入对话框仅展示 ValidationStatus=Validated 且 Status=Enabled 的验方

**Business Rules:**
1. 统一所有权检查
2. 禁用后开方时不可导入 (验方导入对话框过滤 Status=Enabled)
3. 支持批量启用/禁用
4. **处方导入对话框仅展示 ValidationStatus=Validated 且 Status=Enabled 的验方** (MC-D08，见 [medical-cases.md](07-medical-cases.md) US-MC-016)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/formulas/{id}/toggle-status`，批量: POST `/api/v1/formulas/batch-enable` 或 `/batch-disable` |
| 本地 | 本地状态切换 |

### US-FORM-007: 恢复已删除验方

> As a 管理员, I want to 恢复误删除的验方,
> so that 重要的经验方不会因误操作而永久丢失。

**Acceptance Criteria:**
- [ ] 恢复成功 -> 验方重新出现在默认列表查询中

**Business Rules:**
1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/formulas/{id}/restore` |
| 本地 | 本地恢复 |

### US-FORM-008: 共享验方

> As a 医生, I want to 将自己的验方标记为共享,
> so that 其他医生可以查阅我的经验方用于参考和学习。

**Acceptance Criteria:**
- [ ] IsShared=true -> 其他 Doctor 列表查询可见
- [ ] Doctor 编辑他人共享验方 -> 返回 403

**Business Rules:**
1. IsShared=true 的验方对所有 Doctor 可见
2. 共享验方对 Doctor 只读
3. Admin 可编辑任何共享验方

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 通过 Update 修改 IsShared 字段 |
| 本地 | 本地标记 |

### US-FORM-009: 延迟绑定

> As a 管理员, I want to 将导入验方中的药材名称逐个绑定到系统药材库,
> so that 验方药材数据与系统一致，导入处方时可正确关联药材信息。

**Acceptance Criteria:**
- [ ] HerbId=null -> 显示 OriginalHerbName
- [ ] validate 成功 -> IsValidated=true, HerbId 填充
- [ ] 所有 HerbItem.IsValidated=true -> Formula.ValidationStatus=Validated

**Business Rules:**
1. FormulaHerbItem.HerbId 可为空 (未绑定状态)
2. OriginalHerbName 保存原始药材名称 (从旧系统导入)
3. IsValidated=false 表示未验证
4. 手动绑定: 通过 validate 端点将药材关联到系统药材库
5. 绑定后 IsValidated=true, HerbId 填充
6. 当所有药材都已验证时，验方 ValidationStatus 自动变为 Validated

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate`，请求体含 selectedHerbId |
| 本地 | 本地验证 |

### US-FORM-010: 获取待验证验方

> As a 管理员, I want to 查看所有包含未验证药材的验方列表,
> so that 我可以集中处理待验证数据，确保验方质量。

**Acceptance Criteria:**
- [ ] 查询 -> 仅返回 ValidationStatus=Draft 的验方

**Business Rules:**
1. 返回 ValidationStatus=Draft 的验方
2. 用于管理界面批量处理未验证数据

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/formulas/pending-validation` |
| 本地 | 本地查询 |

### US-FORM-011: 批量导入

> As a 管理员, I want to 通过 JSON/Excel 格式批量导入验方,
> so that 旧系统的大量验方数据可以高效迁移到新系统。

**Acceptance Criteria:**
- [ ] 导入完成 -> 返回 successCount/failureCount/matchedCount/unmatchedCount

**Business Rules:**
1. 每个验方包含名称、功效、用法和药材列表
2. 导入的药材默认 IsValidated=false
3. 返回成功列表和失败详情 (含匹配/未匹配药材数)
4. 药材匹配机制: 导入时通过 ICrossModuleService.GetHerbByNameOrPinyinAsync() 匹配系统药材，匹配失败则 HerbId=null、IsValidated=false，保存供后续手动绑定

> **[Sprint 4 已实现]** Formula DataSource 扩展: IFormulaDataSource 新增 BatchImportAsync/GetPendingValidationAsync/GetAllForExportAsync/ValidateHerbBindingsAsync 方法，Local/Remote 双模式实现 (T4-X2-19~22)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/formulas/batch-import` |
| 本地 | 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext |

### US-FORM-012: 导出验方

> As a 管理员, I want to 将验方数据导出为 Excel,
> so that 可以备份数据或与其他系统交换验方信息。

**Acceptance Criteria:**
- [ ] 导出 Excel -> 每行验方包含药材组成详情

**Business Rules:**
1. 支持按分类筛选导出

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/formulas/export?category=` |
| 本地 | 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件 |

### US-FORM-013: 下载导入模板

> As a 管理员, I want to 下载验方导入 Excel 模板,
> so that 我可以按照标准格式准备导入数据。

**Acceptance Criteria:**
- [ ] GET 请求 -> 返回 .xlsx 模板文件

**Business Rules:**
1. 允许匿名访问

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/formulas/import-template` (AllowAnonymous) |
| 本地 | 内置模板 |

---

### UI 规格

#### 验方列表页
| 属性 | 规格 |
|------|------|
| 搜索字段 | 名称、拼音码 |
| 表格列 | 名称、拼音码、类型(个人/共享)、药材数、验证状态、启用状态 |
| 筛选 | 类型切换 (个人/共享/全部)、验证状态筛选、启用状态筛选 |
| 工具栏按钮 | 新增验方、批量导入、批量删除 |

#### 验方新增/编辑表单
| 字段 | 控件 | 必填 | 校验规则 |
|------|------|------|----------|
| 名称 | TextBox | 是 | 2-30 字符 |
| 拼音码 | TextBox (自动生成) | 是 | 自动取首字母 |
| 类型 | ToggleSwitch (个人/共享) | 是 | 默认个人 |
| 备注 | TextBox (多行) | 否 | 最大 500 字符 |

#### 药材项编辑 (表单内嵌子表格)
| 属性 | 规格 |
|------|------|
| 布局 | 表单下方内嵌可编辑 DataGrid |
| 列 | 药材名称 (ComboBox 搜索选择)、剂量 (DecimalUpDown)、单位 (自动填充)、煎法 (ComboBox: 先煎/后下/包煎/另煎/无)、排序号 |
| 添加行 | 表格底部"+ 添加药材"按钮 |
| 删除行 | 行内删除按钮 (需确认) |
| 药材搜索 | ComboBox 支持拼音码即时搜索 (300ms 防抖)，下拉显示药材名称+单价 |
| 延迟绑定 | 药材匹配失败时显示黄色警告图标 + "未绑定"文字，可稍后匹配 |
| 验证状态 | 所有药材绑定完成 → 自动切换为 Validated，显示绿色 ✓ 图标 |

#### 验方导入向导
遵循 18-ui-patterns.md §3.2 导入对话框规范，额外注意:
- 药材名通过拼音码自动匹配，失败项标黄
- 导入完成后显示未匹配药材列表，提示手动绑定

---

### 安全要求

| 要求 | 说明 |
|------|------|
| 个人验方隔离 | 个人验方仅创建者可见，共享验方 Doctor+ 可见 |
| 处方引用门控 | 未验证或已禁用验方不可用于处方 (FORM-D02) |
| 验证操作审计 | 验证状态变更记录到 SecurityAuditLog |


### 性能预期

| 操作 | 目标 | 说明 |
|------|------|------|
| 验方列表 | < 200ms | 含搜索+分页 |
| 创建/编辑 | < 150ms | 含药材项验证 |
| 批量导入 | < 3s | 50 条验方，含拼音匹配 |
| 验证状态检查 | < 100ms | 遍历药材项匹配 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 验方版本管理 (修改历史追踪) | 增加复杂度，v1.0 不需要 |
| 验方评分/评价机制 | 社交功能非当前优先级 |
| 智能推荐验方 (基于症状) | 需 AI/ML 支持，后续版本考虑 |
| 验方审批工作流 (多级审核) | 诊所规模小，所有权检查已足够 |
| 跨诊所验方共享 | 多租户功能，超出 v1.0 范围 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 旧系统药材名称不规范 | 大量验方导入后 ValidationStatus=Draft，药材验证工作量大 | 延迟绑定机制 + 批量导入时自动拼音匹配 (GetHerbByNameOrPinyinAsync) |
| 药材库数据不完整 | 部分药材无法在系统药材库中找到匹配项 | 保留 OriginalHerbName，支持后续补充药材库后再绑定 |
| 验方与处方模块耦合 | 处方导入依赖验方的 ValidationStatus 和 Status 双重过滤 (MC-D08) | 跨模块接口明确定义过滤条件 |
| 批量导入数据质量 | Excel 格式不规范导致导入失败 | 提供标准模板 (US-FORM-013) + 行级错误详情返回 |
| 本地模式导入依赖 NPOI | 客户端体积增加 | NPOI 已作为项目依赖，无额外引入 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-FORM-01 | 验方复制功能是否需要 (基于现有验方创建副本)? | 待确认。当前仅支持处方导入验方，不支持验方间复制 |
| OQ-FORM-02 | 是否需要验方分类管理 (Category 的 CRUD)? | 待确认。当前 Category 为自由文本字段，无独立管理 |
| OQ-FORM-03 | 共享验方是否支持取消共享? | 待确认。当前通过 Update 修改 IsShared 字段实现，技术上已支持 |
| OQ-FORM-04 | Name 字段最大长度 100 vs 200 是否需要统一 DTO 校验? | 延期。实体定义 200，DTO 校验 100，当前以实体为准 (FORM-14) |

---

## Performance (C1)

| 操作 | 目标 |
|------|------|
| 列表 | <200ms |
| 创建 | <150ms |
| 批量导入 50 | <3s |
| 验证 | <100ms |

## Data Volume Estimates

| 维度 | 估算 | 说明 |
|------|------|------|
| 验方数量 | ~50-200 个 | 个人验方 + 共享验方 |
| 每个验方药材数 | 5-20 味 | FormulaHerbItem 记录数 |
| FormulaHerbItem 总量 | ~3000 条 | 200 × 15 (均值) |
| 年增长率 | ~20% | 新增验方 + 从旧系统持续迁移 |

---

## Data Model

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
| DecocteMethod | DecocteMethod | Default: Normal | 煎法 (定义见 [medical-cases.md](07-medical-cases.md) DecocteMethod 枚举) |
| Usage | string(200)? | - | 用法 |
| Remark | string(200)? | - | 备注 |

> 两个实体均继承 BaseEntity

---

## Error Codes

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

### 药材验证错误 (US-FORM-009, 602xx)

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

### 导入行级错误 (US-FORM-011)

| 失败原因 | 类型 | 触发条件 |
|----------|------|----------|
| 验方名称不能为空 | 验证失败 | 名称字段为空 |
| 数据处理异常 | 技术异常 | 行级 try-catch 捕获 |

---

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| FORM-D01 | 本地模式下导入导出的支持方式 | US-FORM-011 ~ 013 | 已确定: 支持。客户端 NPOI 本地处理，不依赖 API |
| FORM-D02 | 验方导入处方时的价格来源 | US-MC-016 | 已确定: 验方不含价格。导入处方时根据 HerbId 从药材库获取当前价格，价格计算在处方层完成 |
| MC-D08 | 处方导入对话框的验方过滤 | US-FORM-006 + US-MC-016 | 已确定: 仅展示 ValidationStatus=Validated 且 Status=Enabled 的验方。Draft 验方需先完成药材绑定验证 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | Name 字段最大长度从 100 修订为 200 | 代码实体 Name=200，PRD/DTO=100 不一致，对齐实体定义 | FORM-14 |

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
| 2026-02-26 | v1.7 | Sprint 4 已实现标记: IFormulaDataSource 扩展 (T4-X2-19~22) |
| 2026-02-28 | v1.8 | PRD 偏差修复: Create 端点返回码明确标注 200 (PRD-11) |
| 2026-03-06 | v2.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节 |
| 2026-03-06 | v2.1 | 修正: 经验方不涉及价格 -- US-FORM-002 移除 TotalPrice 列; US-FORM-009 移除价格计算描述; FORM-D02 明确价格在处方层计算 |
