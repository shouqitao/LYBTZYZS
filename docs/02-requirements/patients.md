# 患者管理 需求规格

## 概述

患者管理模块负责患者档案的电子化管理，包括基本信息维护、快速检索 (拼音码)、批量导入导出 (Excel)、敏感数据保护。支持引用检查以确保数据完整性。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部患者 |
| Admin | CRUD 全部患者 |
| Doctor | CRUD 全部患者 |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。

---

## 功能清单

### FR-PAT-001: 创建患者

- **描述**: 创建新的患者档案
- **业务规则**:
  1. 姓名必填，最长 50 字符
  2. 自动生成拼音码 (PinYinCode) 用于快速搜索
  3. 手机号唯一性检查 (同一手机号不可重复)
  4. 出生日期不能晚于当前日期
  5. 身份证号格式验证 (18 位)
  6. 默认状态为 Enabled
- **远程模式**: POST `/api/v1/patients`，返回 PatientDetailDto (201)
- **本地模式**: DataSource 本地存储
- **验收标准**:
  - [ ] 手机号重复时返回错误
  - [ ] 拼音码自动生成
  - [ ] 年龄从出生日期自动计算

### FR-PAT-002: 查看患者列表

- **描述**: 分页查看患者列表，支持关键词搜索
- **业务规则**:
  1. 支持按姓名和拼音码搜索 (keyword)
  2. 默认分页: page=1, pageSize=20
  3. 列表缓存: OutputCache("PatientsCache")
  4. 年龄由 Service 层计算 (基于 BirthDate)
- **远程模式**: GET `/api/v1/patients?keyword=&page=&pageSize=`
- **本地模式**: 本地 SQLite 查询
- **验收标准**:
  - [ ] 拼音码搜索正确匹配
  - [ ] 年龄正确计算

### FR-PAT-003: 查看患者详情

- **描述**: 获取单个患者的完整信息
- **业务规则**:
  1. 返回 PatientDetailDto (含审计字段)
  2. 包含计算属性 Age
  3. 包含 CreatedBy (用于所有权检查)
- **远程模式**: GET `/api/v1/patients/{id}`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 返回完整患者信息

### FR-PAT-004: 更新患者信息

- **描述**: 修改患者基本信息
- **业务规则**:
  1. 统一所有权检查
  2. 姓名变更时自动重新生成拼音码
  3. 手机号唯一性检查
  4. FluentValidation 验证
- **远程模式**: PUT `/api/v1/patients/{id}`
- **本地模式**: 本地更新
- **验收标准**:
  - [ ] 拼音码随姓名自动更新
  - [ ] 手机号冲突时返回错误

### FR-PAT-005: 删除患者

- **描述**: 软删除患者 (IsDeleted=true)
- **业务规则**:
  1. 软删除，数据保留
  2. 统一所有权检查
  3. 自动过滤已删除记录
- **远程模式**: DELETE `/api/v1/patients/{id}`
- **本地模式**: 本地软删除
- **验收标准**:
  - [ ] 删除后列表不显示

### FR-PAT-006: 恢复已删除患者

- **描述**: 恢复软删除的患者
- **业务规则**:
  1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
  2. 检查患者确实处于已删除状态
- **远程模式**: POST `/api/v1/patients/{id}/restore`
- **本地模式**: 本地恢复
- **验收标准**:
  - [ ] 恢复后患者重新出现在列表中

### FR-PAT-007: 批量删除

- **描述**: 批量软删除多个患者
- **业务规则**:
  1. 项级错误隔离: 单项失败不影响其他项
  2. 返回详细的成功/失败报告 (BatchOperationResultDto)
- **远程模式**: POST `/api/v1/patients/batch-delete`
- **本地模式**: 本地批量操作
- **验收标准**:
  - [ ] 返回成功数、失败数和失败原因

### FR-PAT-008: 批量导入

- **描述**: 从 Excel 文件批量导入患者数据
- **业务规则**:
  1. 支持 .xlsx 格式，最大 10MB
  2. 最多导入 1000 行
  3. 部分成功模式: 单行失败不影响其他行
  4. 失败恢复机制: 返回行号、失败原因、修复建议、数据快照
  5. 手机号重复检查
  6. 自动生成拼音码
  7. 导入列: 姓名\*、性别、出生日期、身份证号、手机号码、地址、过敏史、既往病史
- **远程模式**: POST `/api/v1/patients/import` (multipart/form-data)
- **本地模式**: 待讨论
- **验收标准**:
  - [ ] 1000 行以内正常导入
  - [ ] 超过 1000 行返回错误
  - [ ] 部分失败时返回详细报告

### FR-PAT-009: 下载导入模板

- **描述**: 下载患者导入 Excel 模板
- **业务规则**:
  1. 包含表头和 3 行示例数据
  2. 允许匿名访问
- **远程模式**: GET `/api/v1/patients/import-template` (AllowAnonymous)
- **本地模式**: 内置模板
- **验收标准**:
  - [ ] 下载正确的 Excel 模板

### FR-PAT-010: 导出患者数据

- **描述**: 将患者数据导出为 Excel 文件
- **业务规则**:
  1. 支持按姓名关键词筛选导出
  2. 导出列 (12 列): 姓名、性别、出生日期、年龄、身份证号、手机号码、地址、过敏史、既往病史、最后就诊时间、就诊次数、状态
- **远程模式**: GET `/api/v1/patients/export?keyword=`
- **本地模式**: 待讨论
- **验收标准**:
  - [ ] Excel 内容与筛选条件匹配

### FR-PAT-011: 检查患者引用

- **描述**: 检查患者是否被医案引用，用于删除前确认
- **业务规则**:
  1. 返回引用次数 (医案总数)
  2. 返回最近 5 条引用的医案记录
  3. 软删除模式下始终可删除 (CanDelete=true)
- **远程模式**: POST `/api/v1/patients/{id}/check-reference`
- **本地模式**: 本地检查
- **验收标准**:
  - [ ] 正确返回引用数量

### FR-PAT-012: 批量检查患者引用

- **描述**: 批量检查多个患者的引用关系
- **业务规则**:
  1. 最多 100 条患者 ID
  2. 返回每个患者的引用检查结果
- **远程模式**: POST `/api/v1/patients/batch-check-reference`
- **本地模式**: 本地批量检查
- **验收标准**:
  - [ ] 超过 100 条返回错误

---

## 数据模型

### Patient (患者实体)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 患者ID |
| Name | string(100) | Required | 患者姓名 |
| PinYinCode | string(50)? | - | 拼音码 (系统生成) |
| Gender | Gender | Enum | 性别 (Unknown/Male/Female) |
| BirthDate | DateTime? | - | 出生日期 |
| IdNumber | string(50)? | 敏感 | 证件号码 (IdentityInfo, 部分掩码) |
| PhoneNumber | string(20)? | 敏感 | 手机号码 (ContactInfo, 部分掩码) |
| Address | string(256)? | 敏感 | 地址 (PersonalInfo, 默认掩码) |
| AllergyHistory | string(500)? | 敏感 | 过敏史 (MedicalInfo, 哈希掩码) |
| MedicalHistory | string(1000)? | 敏感 | 既往病史 (MedicalInfo, 哈希掩码) |
| BloodType | int | Default: 0 | 血型 |
| MaritalStatus | int | Default: 0 | 婚姻状态 |
| EmergencyContactName | string? | - | 紧急联系人姓名 |
| EmergencyContactPhone | string? | - | 紧急联系人电话 |
| Status | CommonStatus | Default: Enabled | 患者状态 |
| LastVisitTime | DateTime? | - | 最后就诊时间 (自动更新) |
| VisitCount | int | Default: 0 | 就诊次数 |
| Age | int? | 计算属性 | 基于 BirthDate 计算，NotMapped |

> 继承 BaseEntity (Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, RowVersion)

### 敏感数据保护

| 字段 | 数据类型 | 掩码模式 |
|------|----------|----------|
| IdNumber | IdentityInfo | 部分掩码 |
| PhoneNumber | ContactInfo | 部分掩码 |
| Address | PersonalInfo | 默认掩码 |
| AllergyHistory | MedicalInfo | 哈希掩码 |
| MedicalHistory | MedicalInfo | 哈希掩码 |

---

## 待讨论项

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下导入导出的支持方式 | FR-PAT-008 ~ 010 | 待讨论 |
| 2 | 敏感数据在本地模式下的加密策略 | 所有敏感字段 | 待讨论 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 PatientsController + PatientModel 提取 |
