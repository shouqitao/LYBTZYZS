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

### FR-FORM-001: 创建验方

- **描述**: 创建新的经验方模板
- **业务规则**:
  1. 名称必填，1-100 字符
  2. 功效必填，1-200 字符
  3. 用法必填，1-200 字符
  4. 药材组成至少 1 味
  5. 默认类型为 Experience (经验方)
  6. 初始 ValidationStatus=Draft
  7. 记录 UserId 和 CreatedBy (用于所有权判断)
- **远程模式**: POST `/api/v1/formulas`，返回 FormulaDetailDto
- **本地模式**: DataSource 本地存储
- **验收标准**:
  - [ ] 无药材时创建失败
  - [ ] 初始状态为 Draft

### FR-FORM-002: 查看验方列表

- **描述**: 分页查看验方列表，支持关键词和分类筛选
- **业务规则**:
  1. 支持按名称搜索 (keyword)
  2. 支持按分类筛选 (category)
  3. Admin 返回全部验方
  4. Doctor 返回自己创建的 + 共享的验方
  5. 列表包含 HerbCount 和 TotalPrice
- **远程模式**: GET `/api/v1/formulas?keyword=&category=&page=&pageSize=`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] Doctor 只能看到自己的和共享的
  - [ ] Admin 能看到全部

### FR-FORM-003: 查看验方详情

- **描述**: 获取验方完整信息，包含药材组成列表
- **业务规则**:
  1. 返回 FormulaDetailDto 含完整 Herbs 列表
  2. 包含每味药材的验证状态 (IsValidated)
- **远程模式**: GET `/api/v1/formulas/{id}`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 返回完整药材组成

### FR-FORM-004: 更新验方

- **描述**: 修改验方信息和药材组成
- **业务规则**:
  1. 统一所有权检查 (Doctor 只能编辑自己的)
  2. 药材组成采用粗粒度替换策略: 完整替换 Herbs 集合
  3. 药材组成至少 1 味
- **远程模式**: PUT `/api/v1/formulas/{id}`
- **本地模式**: 本地更新
- **验收标准**:
  - [ ] Doctor 编辑他人验方返回 403
  - [ ] 药材组成完整替换

### FR-FORM-005: 删除验方

- **描述**: 软删除验方
- **业务规则**:
  1. 统一所有权检查
  2. 软删除，数据保留
  3. 支持批量删除
- **远程模式**: DELETE `/api/v1/formulas/{id}`，批量: POST `/api/v1/formulas/batch-delete`
- **本地模式**: 本地软删除
- **验收标准**:
  - [ ] Doctor 删除他人验方返回 403

### FR-FORM-006: 启用/禁用验方

- **描述**: 切换验方启用/禁用状态
- **业务规则**:
  1. 统一所有权检查
  2. 禁用后开方时不可导入
  3. 支持批量启用/禁用
- **远程模式**: POST `/api/v1/formulas/{id}/toggle-status`，批量: POST `/api/v1/formulas/batch-enable` 或 `/batch-disable`
- **本地模式**: 本地状态切换
- **验收标准**:
  - [ ] 禁用验方不出现在导入列表中

### FR-FORM-007: 恢复已删除验方

- **描述**: 恢复软删除的验方
- **业务规则**:
  1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
- **远程模式**: POST `/api/v1/formulas/{id}/restore`
- **本地模式**: 本地恢复
- **验收标准**:
  - [ ] 恢复后验方重新出现

### FR-FORM-008: 共享验方

- **描述**: 将验方标记为共享，其他 Doctor 可查看
- **业务规则**:
  1. IsShared=true 的验方对所有 Doctor 可见
  2. 共享验方对 Doctor 只读
  3. Admin 可编辑任何共享验方
- **远程模式**: 通过 Update 修改 IsShared 字段
- **本地模式**: 本地标记
- **验收标准**:
  - [ ] 共享后其他 Doctor 可在列表中看到
  - [ ] 其他 Doctor 不可编辑共享验方

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
  - [ ] 未绑定药材显示 OriginalHerbName
  - [ ] 绑定后 IsValidated 更新
  - [ ] 全部绑定后 ValidationStatus 自动更新

### FR-FORM-010: 获取待验证验方

- **描述**: 获取所有包含未验证药材的验方列表
- **业务规则**:
  1. 返回 ValidationStatus=Draft 的验方
  2. 用于管理界面批量处理未验证数据
- **远程模式**: GET `/api/v1/formulas/pending-validation`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 仅返回含未验证药材的验方

### FR-FORM-011: 批量导入

- **描述**: 通过 JSON 格式批量导入验方 (含药材组成)
- **业务规则**:
  1. 每个验方包含名称、功效、用法和药材列表
  2. 导入的药材默认 IsValidated=false
  3. 返回成功列表和失败详情 (含匹配/未匹配药材数)
- **远程模式**: POST `/api/v1/formulas/batch-import`
- **本地模式**: 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext
- **验收标准**:
  - [ ] 返回匹配和未匹配药材统计

### FR-FORM-012: 导出验方

- **描述**: 将验方数据导出为 Excel
- **业务规则**:
  1. 支持按分类筛选导出
- **远程模式**: GET `/api/v1/formulas/export?category=`
- **本地模式**: 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件
- **验收标准**:
  - [ ] 导出包含药材组成

### FR-FORM-013: 下载导入模板

- **描述**: 下载验方导入 Excel 模板
- **业务规则**:
  1. 允许匿名访问
- **远程模式**: GET `/api/v1/formulas/import-template` (AllowAnonymous)
- **本地模式**: 内置模板
- **验收标准**:
  - [ ] 模板格式正确

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
| Dosage | int | Required | 剂量 (整数) |
| Unit | string(16) | Required | 单位 |
| ProcessingMethod | string(100)? | - | 炮制方法 |
| DecocteMethod | DecocteMethod | Default: Default | 煎法 |
| Usage | string(200)? | - | 用法 |
| Remark | string(200)? | - | 备注 |

> 两个实体均继承 BaseEntity

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下导入导出的支持方式 | FR-FORM-011 ~ 013 | 已确定: 支持。客户端 NPOI 本地处理，不依赖 API |
| 2 | 验方复制到处方时的价格计算规则 | FR-FORM-008 | 已确定: 根据 HerbId 查药材库当前价格。FormulaHerbItem 不含价格字段，价格始终以药材库为准 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 FormulasController + Formula 实体 + formula-copy-flow spec 提取 |
