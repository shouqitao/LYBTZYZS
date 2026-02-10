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
  - [ ] 拼音码自动生成
  - [ ] 价格验证 (>0)

### FR-HERB-002: 查看药材列表

- **描述**: 分页查看药材列表，支持关键词和分类筛选
- **业务规则**:
  1. 支持按名称和拼音码搜索 (keyword)
  2. 支持按分类筛选 (category)
  3. 默认分页: page=1, pageSize=20
  4. 列表缓存: OutputCache("HerbsCache")
  5. 默认按名称升序排列
- **远程模式**: GET `/api/v1/herbs?keyword=&category=&page=&pageSize=`
- **本地模式**: 本地 SQLite 查询
- **验收标准**:
  - [ ] 分类筛选正确
  - [ ] 拼音码搜索匹配

### FR-HERB-003: 查看药材详情

- **描述**: 获取单个药材的完整信息
- **业务规则**:
  1. 返回 HerbDetailDto (含成本价、功效、用法等完整字段)
- **远程模式**: GET `/api/v1/herbs/{id}`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 返回完整药材信息

### FR-HERB-004: 更新药材信息

- **描述**: 修改药材的基本信息和价格
- **业务规则**:
  1. 统一所有权检查
  2. 名称变更时自动重新生成拼音码
  3. InputDto 不含 Status 字段，状态变更通过专用 API
- **远程模式**: PUT `/api/v1/herbs/{id}`
- **本地模式**: 本地更新
- **验收标准**:
  - [ ] 拼音码随名称自动更新
  - [ ] 不能通过更新接口修改状态

### FR-HERB-005: 删除药材

- **描述**: 软删除药材 (IsDeleted=true)
- **业务规则**:
  1. 软删除，数据保留
  2. 统一所有权检查
  3. 始终可删除 (软删除模式)
- **远程模式**: DELETE `/api/v1/herbs/{id}`
- **本地模式**: 本地软删除
- **验收标准**:
  - [ ] 删除后列表不显示

### FR-HERB-006: 启用/禁用药材

- **描述**: 切换药材的启用/禁用状态
- **业务规则**:
  1. 统一所有权检查
  2. 禁用后在开方时不可选择 (处方模块过滤)
  3. 支持批量启用/禁用
- **远程模式**: POST `/api/v1/herbs/{id}/toggle-status`，批量: POST `/api/v1/herbs/batch-enable` 或 `/batch-disable`
- **本地模式**: 本地状态切换
- **验收标准**:
  - [ ] 禁用药材在开方时不可选
  - [ ] 批量操作返回详细结果

### FR-HERB-007: 恢复已删除药材

- **描述**: 恢复软删除的药材
- **业务规则**:
  1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
- **远程模式**: POST `/api/v1/herbs/{id}/restore`
- **本地模式**: 本地恢复
- **验收标准**:
  - [ ] 恢复后药材重新出现

### FR-HERB-008: 批量删除

- **描述**: 批量软删除多个药材
- **业务规则**:
  1. 项级错误隔离
  2. 返回 BatchOperationResultDto
- **远程模式**: POST `/api/v1/herbs/batch-delete`
- **本地模式**: 本地批量操作
- **验收标准**:
  - [ ] 返回成功数和失败数

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
  - [ ] 正确解析 Excel 数据
  - [ ] 部分失败时返回详细报告

### FR-HERB-010: JSON 批量导入

- **描述**: 通过 JSON 格式批量导入药材
- **业务规则**:
  1. 最多 10000 条
  2. 支持重复处理策略: Skip (跳过) / Update (覆盖) / Error (报错)
  3. 返回详细导入结果
- **远程模式**: POST `/api/v1/herbs/batch-import`
- **本地模式**: 支持。客户端本地解析 JSON 文件，直接写入 LocalDbContext
- **验收标准**:
  - [ ] 超过 10000 条返回错误
  - [ ] 三种重复策略正确执行

### FR-HERB-011: 导出药材数据

- **描述**: 将药材数据导出为 Excel 文件
- **业务规则**:
  1. 支持按分类筛选导出
  2. 两种导出方式: Excel (服务端生成) 和 JSON (全量导出，Desktop 负责 Excel 生成)
- **远程模式**: GET `/api/v1/herbs/export?category=` (Excel), GET `/api/v1/herbs/export-all` (JSON)
- **本地模式**: 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件
- **验收标准**:
  - [ ] Excel 内容完整

### FR-HERB-012: 下载导入模板

- **描述**: 下载药材导入 Excel 模板
- **业务规则**:
  1. 允许匿名访问
  2. 包含表头和示例数据
- **远程模式**: GET `/api/v1/herbs/import-template` (AllowAnonymous)
- **本地模式**: 内置模板
- **验收标准**:
  - [ ] 模板格式正确

### FR-HERB-013: 检查药材引用

- **描述**: 检查药材是否被处方引用
- **业务规则**:
  1. 返回引用次数和最近 5 条处方引用
  2. 软删除模式下始终可删除
  3. 支持单个和批量检查 (批量最多 100 条)
- **远程模式**: GET `/api/v1/herbs/{id}/check-reference`，批量: POST `/api/v1/herbs/batch-check-reference`
- **本地模式**: 本地检查
- **验收标准**:
  - [ ] 正确返回处方引用数量

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
