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
  5. **身份证号必填 + 格式验证 (18 位) + 唯一性检查** (PAT-D03)
  6. 默认状态为 Enabled
  7. 建议操作流程: 先按手机号/身份证号查询是否已存在，不存在再创建
- **远程模式**: POST `/api/v1/patients`，返回 PatientDetailDto (201)
- **本地模式**: DataSource 本地存储
- **验收标准**:
  - [ ] 手机号已存在 -> 返回 400 "手机号 {PhoneNumber} 已存在"
  - [ ] 创建成功 -> 拼音码自动生成
  - [ ] BirthDate=1990-01-01 -> Age 自动计算为当前年龄

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
  - [ ] keyword="ZS" -> 返回拼音码包含 ZS 的患者
  - [ ] BirthDate 非空时 -> Age 字段正确计算

### FR-PAT-003: 查看患者详情

- **描述**: 获取单个患者的完整信息
- **业务规则**:
  1. 返回 PatientDetailDto (含审计字段)
  2. 包含计算属性 Age
  3. 包含 CreatedBy (用于所有权检查)
- **远程模式**: GET `/api/v1/patients/{id}`
- **本地模式**: 本地查询
- **验收标准**:
  - [ ] 有效ID -> 返回 200 + PatientDetailDto (含 Age 计算属性)

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
  - [ ] Name 变更 -> PinYinCode 自动重新生成
  - [ ] 手机号已被占用 -> 返回 400

### FR-PAT-005: 删除患者

- **描述**: 软删除患者 (IsDeleted=true)
- **业务规则**:
  1. **引用检查: 有关联医案 (任何状态) 的患者禁止删除，返回 422** (MC-D04，见 [medical-cases.md](medical-cases.md))
  2. 无关联医案时执行软删除，数据保留
  3. 统一所有权检查
  4. 自动过滤已删除记录
  5. 有关联医案的患者建议使用禁用功能 (Status=Disabled) 替代删除
- **远程模式**: DELETE `/api/v1/patients/{id}`
- **本地模式**: 本地软删除
- **验收标准**:
  - [ ] 患者有关联医案 -> 返回 422 "该患者有历史医案，无法删除，请使用禁用功能"
  - [ ] 患者无关联医案 -> 软删除成功，默认列表查询不返回该患者

### FR-PAT-006: 恢复已删除患者

- **描述**: 恢复软删除的患者
- **业务规则**:
  1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
  2. 检查患者确实处于已删除状态
- **远程模式**: POST `/api/v1/patients/{id}/restore`
- **本地模式**: 本地恢复
- **验收标准**:
  - [ ] 恢复成功 -> 患者重新出现在默认列表查询中

### FR-PAT-007: 批量删除

- **描述**: 批量软删除多个患者
- **业务规则**:
  1. 项级错误隔离: 单项失败不影响其他项
  2. 返回详细的成功/失败报告 (BatchOperationResultDto)
- **远程模式**: POST `/api/v1/patients/batch-delete`
- **本地模式**: 本地批量操作
- **验收标准**:
  - [ ] 批量删除 -> 返回 BatchOperationResultDto (successCount/failureCount/failedItems)

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
- **本地模式**: 支持。使用客户端 NPOI (ExcelHelper) 本地解析 Excel 文件，直接写入 LocalDbContext，不依赖服务端 API
- **验收标准**:
  - [ ] 999行Excel -> 导入成功，返回 successCount=999
  - [ ] 1001行Excel -> 返回 400 "导入数据超过限制"
  - [ ] 部分行验证失败 -> 返回行号+失败原因+修复建议

### FR-PAT-009: 下载导入模板

- **描述**: 下载患者导入 Excel 模板
- **业务规则**:
  1. 包含表头和 3 行示例数据
  2. 允许匿名访问
- **远程模式**: GET `/api/v1/patients/import-template` (AllowAnonymous)
- **本地模式**: 内置模板
- **验收标准**:
  - [ ] GET 请求 -> 返回 .xlsx 文件，含8列表头

### FR-PAT-010: 导出患者数据

- **描述**: 将患者数据导出为 Excel 文件
- **业务规则**:
  1. 支持按姓名关键词筛选导出
  2. 导出列 (12 列): 姓名、性别、出生日期、年龄、身份证号、手机号码、地址、过敏史、既往病史、最后就诊时间、就诊次数、状态
- **远程模式**: GET `/api/v1/patients/export?keyword=`
- **本地模式**: 支持。从 LocalDbContext 查询数据，使用客户端 NPOI 本地生成 Excel 文件
- **验收标准**:
  - [ ] keyword="张" -> 导出的 Excel 仅包含姓名含"张"的患者

### FR-PAT-011: 检查患者引用

- **描述**: 检查患者是否被医案引用，用于删除前确认
- **业务规则**:
  1. 返回引用次数 (医案总数，含所有状态)
  2. 返回最近 5 条引用的医案记录
  3. **有关联医案时 CanDelete=false** (MC-D04)，提示使用禁用功能替代
- **远程模式**: POST `/api/v1/patients/{id}/check-reference`
- **本地模式**: 本地检查
- **验收标准**:
  - [ ] 患者有3条医案 -> 返回 referenceCount=3, canDelete=true

### FR-PAT-012: 批量检查患者引用

- **描述**: 批量检查多个患者的引用关系
- **业务规则**:
  1. 最多 100 条患者 ID
  2. 返回每个患者的引用检查结果
- **远程模式**: POST `/api/v1/patients/batch-check-reference`
- **本地模式**: 本地批量检查
- **验收标准**:
  - [ ] 101个ID -> 返回 400 "批量检查最多支持100条记录"

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
| IdNumber | string(50) | Required, Unique, 敏感 | 证件号码 (IdentityInfo, 部分掩码) (PAT-D03) |
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

## 错误码

> 错误码分区: 2xxxx (Patients 模块)。Service 层采用 Result 模式统一返回。

### 结构化错误码

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-20001 | PatientNotFound | 404 | 患者不存在 | GetById/Update/Delete 时 ID 无效或已被软删除 |
| ERR-20002 | PatientIdCardExists | 409 | 系统中已存在该身份证 | 创建/更新/导入时身份证号重复 (PAT-D03) |
| ERR-20003 | PatientPhoneExists | 409 | 患者电话已存在 | 创建/更新时手机号重复 |
| ERR-20004 | PatientHasReferencedCases | 422 | 该患者有历史医案，无法删除，请使用禁用功能 | 删除时有关联医案 (任何状态) (MC-D04) |
| ERR-20005 | PatientDisabled | 403 | 患者已被禁用 | 患者状态无效 |
| ERR-20006 | InvalidPatientStatus | 400 | 无效的患者状态 | 状态转换非法 |
| ERR-00003 | ValidationFailed | 400 | 参数验证失败 | FluentValidation 验证不通过 |

### 业务规则错误

| 场景 | HTTP | 用户消息 | 触发条件 |
|------|------|----------|----------|
| 手机号重复 | 400 | 手机号 {PhoneNumber} 已存在 | 创建/更新时同一手机号已被占用 |
| 患者未被删除 | 200 (success=false) | 该患者未被删除，无需恢复 | 恢复未软删除的患者 |
| 批量操作为空 | 400 | 请至少选择一个患者 | 批量删除时 ID 列表为空 |
| 批量检查超限 | 400 | 批量检查最多支持100条记录 | BatchCheckReference 超过 100 条 |

### 导入错误 (FR-PAT-008)

| 场景 | HTTP | 用户消息 | 触发条件 |
|------|------|----------|----------|
| 文件为空 | 400 | 文件不能为空 | file==null 或 file.Length==0 |
| 文件格式错误 | 400 | 仅支持.xlsx格式的Excel文件 | 扩展名不是 .xlsx |
| 文件过大 | 400 | 文件大小不能超过10MB | file.Length > 10MB |
| 无工作表 | 400 | Excel文件中没有工作表 | Workbook 无 Worksheets |
| 数据行超限 | 400 | 导入数据超过限制（最大1000行） | rowCount > 1000 |

### 导入行级错误 (部分成功模式)

| 失败原因 | 类型 | 修复建议 |
|----------|------|----------|
| 姓名无效 | 验证失败 | 请输入有效的患者姓名（1-50个字符） |
| 手机号格式错误 | 验证失败 | 请输入11位手机号码 |
| 身份证号格式错误 | 验证失败 | 请输入18位身份证号 |
| 出生日期无效 | 验证失败 | 请输入有效的出生日期（YYYY-MM-DD） |
| 年龄超出范围 | 验证失败 | 年龄必须在0-150之间 |
| 手机号已存在 | 业务约束 | 跳过记录，继续导入 |
| 数据解析异常 | 技术异常 | 行级隔离，记录安全错误消息 |

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 本地模式下导入导出的支持方式 | FR-PAT-008 ~ 010 | 已确定: 支持。客户端 NPOI 本地读写 Excel，不经过 API |
| 2 | 敏感数据在本地模式下的加密策略 | 所有敏感字段 | 已确定: v1.0 不加密 SQLite，依赖 OS 用户权限 + 物理设备安全。v2.0 评估 SQLCipher |
| PAT-D03 | 身份证号必填 + 唯一性检查 | FR-PAT-001 + 数据模型 | 已确定: IdNumber 改为 Required + Unique，创建/更新时验证重复 |
| PAT-D04 | 患者合并功能 | - | 已确定: v1.0 不包含。操作流程"先查后建"防重复 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 PatientsController + PatientModel 提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含结构化错误码 7 个 + 导入错误 12 个场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果]，12 个 FR 共 18 条验收标准 |
| 2026-02-17 | v1.3 | Round 9: FR-PAT-005 增加引用检查 (有医案禁止删除)，FR-PAT-011 CanDelete 规则变更，ERR-20004 更新 |
| 2026-02-17 | v1.4 | Round 10: FR-PAT-001 身份证号改必填+唯一性检查 (PAT-D03)，数据模型 IdNumber 约束变更，ERR-20002 更新，PAT-D04 确认无患者合并 |
