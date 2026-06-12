# 患者管理 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所患者档案长期依赖纸质病历和手工登记，导致信息分散、检索困难、重复录入频繁。电子化后，患者档案需要集中管理，但面临快速检索 (拼音码)、批量数据迁移 (Excel 导入)、敏感数据保护 (身份证/手机号加密)、以及数据完整性保障 (引用检查) 等挑战。同时，诊所存在离线诊疗场景，患者管理必须在远程和本地两种模式下均可用。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 接诊时需快速查找患者档案，纸质病历翻找耗时 | 每次接诊浪费 1-3 分钟查找，日均浪费 30-60 分钟 |
| 医生 | 外出诊疗 (本地模式) 无法访问患者历史记录 | 无法了解患者既往病史和过敏史，诊疗质量受损 |
| 前台 | 新患者登记需手工填写表格，信息不完整、格式不统一 | 重复录入、信息缺失导致后续诊疗流程延误 |
| 管理员 | 历史患者数据迁移依赖人工逐条录入 | 数百条旧数据迁移耗时数天，且容易出错 |
| 管理员 | 无法有效管理已故/特殊状态患者的档案可见性 | 前台误为已故患者预约，造成尴尬和资源浪费 |

### 1.3 证据

- 临床工作流观察: 医生日均接诊 15-30 人，拼音码检索可将患者查找从分钟级降至秒级
- 诊所运营需求: 历史纸质档案电子化需批量导入能力，单次可达数百条
- 卫生部门信息化要求: 患者身份证号、手机号等敏感信息需加密存储和脱敏展示
- 数据完整性需求: 患者与医案存在关联关系，删除操作需引用检查防止数据孤岛

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | CRUD 全部患者 + 状态管理 (启用/禁用) |
| Admin | CRUD 全部患者 + 状态管理 (启用/禁用) |
| Doctor | 创建/查看/更新/删除全部患者 (Restore 为 Admin-only); 可见禁用患者，列表标注状态 |
| Receptionist | 创建、查看列表/详情、更新患者 (CRU，无删除权限; 自动过滤禁用患者) |

> Doctor/Admin 端点受 `DoctorOrAdmin` 策略保护；Receptionist 端点受 `Authenticated` 策略保护，仅限 CRU 操作。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 患者档案电子化 | 替代纸质病历，实现集中化管理和快速检索 |
| 数据迁移便捷 | Excel 批量导入支持历史数据快速电子化 |
| 敏感数据保护 | 身份证号/手机号加密存储 + 脱敏展示，满足信息安全要求 |
| 数据完整性 | 引用检查 + 软删除确保患者-医案关联关系不被破坏 |
| 离线可用 | 双模式 (远程/本地) 支持外出诊疗场景 |

### 3.2 Why Now

患者管理是诊所管理系统的基础模块，医案管理、处方管理等核心业务模块均依赖患者档案。患者模块是系统可用的前提条件，必须在第一批次交付。

---

## 4. Solution Overview

患者管理模块实现患者档案的全生命周期电子化管理，支持远程和本地双模式运行:

**核心能力:**
- **档案管理**: 患者信息 CRUD，自动生成拼音码 (PinYinCode)，身份证号唯一性校验
- **快速检索**: 按姓名/拼音码关键词搜索，分页浏览，OutputCache 缓存
- **批量操作**: Excel 导入 (最多 1000 行，部分成功模式) / 导出 (按关键词筛选)
- **数据保护**: 敏感字段加密存储 (AES-256) + 脱敏展示 (部分掩码/哈希掩码)
- **引用检查**: 删除前检查医案关联，有关联时禁止删除并建议使用禁用功能
- **状态管理**: 启用/禁用切换，禁用后限制新医案创建和按角色脱敏展示

**核心流程:**
```
新患者登记 → 查重 (手机号/身份证号) → [不存在] 创建档案 (自动生成拼音码) → 就诊
                                    → [已存在] 提示已有档案 → 直接使用

患者删除 → 引用检查 → [有医案] 拒绝删除，建议禁用
                    → [无医案] 软删除

批量导入 → Excel 解析 → 逐行验证 → 部分成功 → 返回详细报告 (成功/失败/修复建议)
```

---

## 5. Success Metrics

| 指标 | 当前 (纸质流程) | v1.0 目标 | 衡量方式 |
|------|----------------|----------|---------|
| 患者查找耗时 | 1-3 分钟 (翻找纸质档案) | < 5 秒 (拼音码搜索) | 操作日志统计 |
| 数据迁移效率 | 逐条手工录入 | 1000 条/次批量导入 | 导入接口统计 |
| 重复患者率 | 无法检测 | 0% (身份证号唯一性检查) | 业务约束保障 |
| 数据完整性 | 无保障 | 100% (引用检查 + 软删除) | 零孤岛医案记录 |
| 离线可用性 | 不可用 | 100% 核心功能本地可用 | 本地模式功能覆盖率 |

---

## 6. Epic Hypothesis

We believe that 实现拼音码快速检索 + Excel 批量导入导出 + 引用检查软删除 + 敏感数据加密保护 + 状态管理的患者档案电子化管理 for 诊所全部角色 (医生/管理员/前台) will achieve 患者档案从纸质到电子化的完整迁移，大幅提升检索效率和数据安全性。We'll know we're right when 患者查找耗时从分钟级降至秒级、重复患者率为零、且离线场景下核心功能 100% 可用。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-PAT-001 | 创建患者 | Must |
| US-PAT-002 | 查看患者列表 | Must |
| US-PAT-003 | 查看患者详情 | Must |
| US-PAT-004 | 更新患者信息 | Must |
| US-PAT-005 | 删除患者 | Should |
| US-PAT-006 | 恢复已删除患者 | Could |
| US-PAT-007 | 批量删除 | Could |
| US-PAT-008 | 批量导入 | Could |
| US-PAT-009 | 下载导入模板 | Could |
| US-PAT-010 | 导出患者数据 | Could |
| US-PAT-011 | 检查患者引用 | Could |
| US-PAT-012 | 批量检查患者引用 | Could |
| US-PAT-013 | 患者状态管理 | Should |

---

### US-PAT-001: 创建患者

> As a 诊所工作人员 (医生/前台), I want to 创建新的患者档案,
> so that 患者信息可以电子化存档并用于后续诊疗。

**Acceptance Criteria:**
- [ ] 填写必填信息 (姓名/身份证号/手机号/地址) 并提交 → 创建成功，拼音码自动生成
- [ ] BirthDate=1990-01-01 → Age 自动计算为当前年龄
- [ ] 手机号已存在 → 返回 400 "手机号 {PhoneNumber} 已存在"
- [ ] 身份证号已存在 → 返回 409 "系统中已存在该身份证"
- [ ] 出生日期晚于当前日期 → 返回 400 验证失败

**Business Rules:**
1. 姓名必填，最长 50 字符
2. 自动生成拼音码 (PinYinCode) 用于快速搜索
3. 手机号唯一性检查 (同一手机号不可重复)
4. 出生日期不能晚于当前日期
5. 身份证号必填 + 格式验证 (18 位) + 唯一性检查 (PAT-D03)
6. 默认状态为 Enabled
7. 建议操作流程: 先按手机号/身份证号查询是否已存在，不存在再创建

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/patients`，返回 PatientDetailDto (201) |
| 本地 | DataSource 本地存储 |

### US-PAT-002: 查看患者列表

> As a 诊所工作人员, I want to 分页浏览患者列表并通过关键词搜索,
> so that 我可以快速找到目标患者进行后续操作。

**Acceptance Criteria:**
- [ ] keyword="ZS" → 返回拼音码包含 ZS 的患者
- [ ] BirthDate 非空时 → Age 字段正确计算
- [ ] Receptionist 查询 → 自动过滤 Status=Disabled 的患者
- [ ] Doctor/Admin 查询 → 可见禁用患者，列表中标注状态

**Business Rules:**
1. 支持按姓名和拼音码搜索 (keyword)
2. 默认分页: page=1, pageSize=20
3. 列表缓存: OutputCache("PatientsCache")
4. 年龄由 Service 层计算 (基于 BirthDate)
5. Receptionist 查询自动过滤 Status=Disabled 的患者; Doctor/Admin 可见全部 (含禁用，列表标注状态)
6. 分页参数验证: page >= 1, pageSize 1-100 (见 [nfr.md](nfr.md) NFR-API-001)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/patients?keyword=&page=&pageSize=` |
| 本地 | 本地 LocalDB 查询 |

### US-PAT-003: 查看患者详情

> As a 诊所工作人员, I want to 查看单个患者的完整信息,
> so that 我可以全面了解患者的基本信息和就诊历史。

**Acceptance Criteria:**
- [ ] 有效 ID → 返回 200 + PatientDetailDto (含 Age 计算属性)
- [ ] 无效 ID → 返回 404

**Business Rules:**
1. 返回 PatientDetailDto (含审计字段)
2. 包含计算属性 Age
3. 包含 CreatedBy (用于所有权检查)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/patients/{id}` |
| 本地 | 本地查询 |

### US-PAT-004: 更新患者信息

> As a 诊所工作人员, I want to 修改患者的基本信息,
> so that 患者档案保持最新和准确。

**Acceptance Criteria:**
- [ ] Name 变更 → PinYinCode 自动重新生成
- [ ] 手机号已被占用 → 返回 400

**Business Rules:**
1. 统一所有权检查
2. 姓名变更时自动重新生成拼音码
3. 手机号唯一性检查
4. FluentValidation 验证

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/patients/{id}` |
| 本地 | 本地更新 |

### US-PAT-005: 删除患者

> As a 医生/管理员, I want to 删除不再需要的患者档案,
> so that 系统数据保持整洁，同时确保有历史医案的患者数据不被误删。

**Acceptance Criteria:**
- [ ] 患者有关联医案 → 返回 422 "该患者有历史医案，无法删除，请使用禁用功能"
- [ ] 患者无关联医案 → 软删除成功，默认列表查询不返回该患者

**Business Rules:**
1. 引用检查: 有关联医案 (任何状态) 的患者禁止删除，返回 422 (MC-D04，见 [medical-cases.md](medical-cases.md))
2. 无关联医案时执行软删除，数据保留
3. 统一所有权检查
4. 自动过滤已删除记录
5. 有关联医案的患者建议使用禁用功能 (Status=Disabled) 替代删除

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | DELETE `/api/v1/patients/{id}` |
| 本地 | 本地软删除 |

### US-PAT-006: 恢复已删除患者

> As a 管理员, I want to 恢复误删的患者档案,
> so that 被误操作删除的患者数据可以找回。

**Acceptance Criteria:**
- [ ] 恢复成功 → 患者重新出现在默认列表查询中
- [ ] 患者未被删除 → 返回 200 "该患者未被删除，无需恢复"

**Business Rules:**
1. 使用 IgnoreQueryFilters() 绕过全局软删除过滤器
2. 检查患者确实处于已删除状态

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/patients/{id}/restore` |
| 本地 | 本地恢复 |

### US-PAT-007: 批量删除

> As a 管理员, I want to 一次性删除多个患者档案,
> so that 批量清理数据时不必逐条操作。

**Acceptance Criteria:**
- [ ] 批量删除 → 返回 BatchOperationResultDto (successCount/failureCount/failedItems)
- [ ] 部分患者有关联医案 → 该项失败，其他项正常执行

**Business Rules:**
1. 项级错误隔离: 单项失败不影响其他项
2. 返回详细的成功/失败报告 (BatchOperationResultDto)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/patients/batch-delete` |
| 本地 | 本地批量操作 |

### US-PAT-008: 批量导入

> As a 管理员, I want to 从 Excel 文件批量导入患者数据,
> so that 历史纸质档案可以快速电子化，无需逐条手工录入。

**Acceptance Criteria:**
- [ ] 999 行 Excel → 导入成功，返回 successCount=999
- [ ] 1001 行 Excel → 返回 400 "导入数据超过限制"
- [ ] 部分行验证失败 → 返回行号 + 失败原因 + 修复建议

**Business Rules:**
1. 支持 .xlsx 格式，最大 10MB
2. 最多导入 1000 行
3. 部分成功模式: 单行失败不影响其他行
4. 失败恢复机制: 返回行号、失败原因、修复建议、数据快照
5. 手机号重复检查
6. 自动生成拼音码
7. 导入列: 姓名\*、性别、出生日期、身份证号\*、手机号码\*、地址\*、过敏史、既往病史

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/patients/import` (multipart/form-data) |
| 本地 | 支持。使用客户端 NPOI (ExcelHelper) 本地解析 Excel 文件，直接写入 LocalDbContext，不依赖服务端 API |

> **[Sprint 4 已实现]** Patient DataSource 扩展: IPatientDataSource 新增 BatchImportAsync/GetAllForExportAsync/HasMedicalCasesAsync/BatchCheckReferencesAsync 方法，Local/Remote 双模式实现 (T4-X2-09~12)

### US-PAT-009: 下载导入模板

> As a 管理员, I want to 下载标准的 Excel 导入模板,
> so that 我可以按正确的格式准备患者数据再批量导入。

**Acceptance Criteria:**
- [ ] GET 请求 → 返回 .xlsx 文件，含 8 列表头和 3 行示例数据

**Business Rules:**
1. 包含表头和 3 行示例数据
2. 允许匿名访问

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/patients/import-template` (AllowAnonymous) |
| 本地 | 内置模板 |

### US-PAT-010: 导出患者数据

> As a 管理员, I want to 将患者数据导出为 Excel 文件,
> so that 我可以用于数据备份、统计分析或上报。

**Acceptance Criteria:**
- [ ] keyword="张" → 导出的 Excel 仅包含姓名含"张"的患者
- [ ] 导出 Excel 包含 12 列完整信息

**Business Rules:**
1. 支持按姓名关键词筛选导出
2. 导出列 (12 列): 姓名、性别、出生日期、年龄、身份证号、手机号码、地址、过敏史、既往病史、最后就诊时间、就诊次数、状态

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/patients/export?keyword=` |
| 本地 | 支持。从 LocalDbContext 查询数据，使用客户端 NPOI 本地生成 Excel 文件 |

### US-PAT-011: 检查患者引用

> As a 医生/管理员, I want to 在删除前查看患者是否被医案引用,
> so that 我可以做出明确的删除/禁用决策，避免数据完整性破坏。

**Acceptance Criteria:**
- [ ] 患者有 3 条医案 → 返回 referenceCount=3, canDelete=false
- [ ] 患者无医案 → 返回 referenceCount=0, canDelete=true

**Business Rules:**
1. 返回引用次数 (医案总数，含所有状态)
2. 返回最近 5 条引用的医案记录
3. 有关联医案时 CanDelete=false (MC-D04)，提示使用禁用功能替代

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/patients/{id}/check-reference` |
| 本地 | 本地检查 |

### US-PAT-012: 批量检查患者引用

> As a 管理员, I want to 批量检查多个患者的引用关系,
> so that 批量删除前可以快速了解哪些患者可删除、哪些需禁用。

**Acceptance Criteria:**
- [ ] 101 个 ID → 返回 400 "批量检查最多支持 100 条记录"
- [ ] 50 个 ID → 返回每个患者的 referenceCount 和 canDelete

**Business Rules:**
1. 最多 100 条患者 ID
2. 返回每个患者的引用检查结果

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/patients/batch-check-reference` |
| 本地 | 本地批量检查 |

### US-PAT-013: 患者状态管理

> As a 管理员, I want to 切换患者状态 (启用/禁用),
> so that 已故或特殊情况的患者档案可以限制访问，同时保留历史数据。

**Acceptance Criteria:**
- [ ] Doctor 调用状态切换 → 返回 403
- [ ] 患者有 Active 医案时禁用 → 返回 422 "该患者有进行中的医案，请先完成或取消"
- [ ] 禁用成功 → Status=Disabled
- [ ] 禁用后为该患者创建医案 → 返回 422 (见 medical-cases.md ERR-30105)
- [ ] 禁用后 Doctor 查看历史医案 → PatientName 掩码显示
- [ ] 禁用后 Admin 查看历史医案 → PatientName 完整显示
- [ ] 禁用后 Receptionist 查询患者列表 → 禁用患者不出现

**Business Rules:**
1. 仅 Admin/SuperAdmin 可执行状态切换
2. 禁用时: 检查患者是否有 Active/Suspended 医案，有则拒绝 (需先完成或取消活跃医案)
3. 禁用后: 禁止为该患者创建新医案 (见 [medical-cases.md](medical-cases.md) US-MC-001)
4. 禁用后: 历史医案可查阅，PatientName 按角色脱敏 -- Admin/SuperAdmin 看完整姓名，Doctor 看掩码 (如 "张*")
5. 启用后: 所有限制解除，脱敏自动取消
6. v1.0 主要禁用场景: 患者已故
7. 查询可见性: Receptionist 查询自动过滤禁用患者 (不可见); Doctor/Admin 可见禁用患者 (列表中标注状态)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/patients/{id}/status`，Body: `{ status: "Enabled"\|"Disabled", reason: "string" }` |
| 本地 | 本地状态切换 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 患者合并功能 | v1.0 通过身份证唯一性 (PAT-D03) + "先查后建"流程防重复，合并功能后续版本考虑 (PAT-D04) |
| 重复患者关系转移 | 后续版本: 将 A2 的医案关系转移到 A1 后禁用 A2 (PAT-D06) |
| 患者自助登记 (小程序/公众号) | v1.0 聚焦桌面端，患者自助渠道后续版本考虑 |
| 患者照片/头像管理 | 非当前优先级，后续版本考虑 |
| 身份证读卡器 OCR 自动填充 | 硬件依赖，后续版本考虑 |

---

## 9. Dependencies & Risks

| 类型 | 项目 | 影响 | 缓解措施 |
|------|------|------|---------|
| 依赖 | 医案模块 (medical-cases.md) | 删除引用检查、禁用后限制新医案创建 | 模块间通过 Service 层接口解耦 |
| 依赖 | 认证模块 (auth.md) | 角色权限控制 (DoctorOrAdmin/Authenticated) | 统一权限策略 |
| 依赖 | NFR 规范 (nfr.md) | 分页参数验证 (NFR-API-001)、敏感数据加密 (NFR-SEC-004) | 遵循 NFR 统一规范 |
| 风险 | 本地 LocalDB 加密字段不支持 LIKE 搜索 | L1 敏感字段 (身份证/手机号) 无法在 LocalDB 中直接搜索 | 搜索在解密后的内存中执行 |
| 风险 | Excel 导入大文件性能 | 1000 行导入可能耗时较长 | 异步处理 + 进度反馈 |
| 风险 | 拼音码生成准确性 | 多音字可能导致拼音码不准确 | 支持手动修正拼音码 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-PAT-01 | 是否支持患者自定义标签/分组? | 延期。v1.0 不包含，后续版本按需评估 |
| OQ-PAT-02 | 导出是否需要敏感字段脱敏选项? | 待定。当前导出完整数据，是否提供脱敏导出选项待业务确认 |
| OQ-PAT-03 | 批量导入是否支持身份证号查重 (更新已有记录)? | 待定。当前仅检查手机号重复，身份证号重复时行级失败 |
| OQ-PAT-04 | 禁用患者的历史医案脱敏规则是否扩展到导出场景? | 待定。当前脱敏仅在 UI 展示层，导出是否脱敏待确认 |

---

## Data Model

### Patient (患者实体)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 患者ID |
| Name | string(100) | Required | 患者姓名 |
| PinYinCode | string(50)? | - | 拼音码 (系统生成) |
| Gender | Gender | Enum | 性别 (Unknown/Male/Female) |
| BirthDate | DateTime? | - | 出生日期 |
| IdType | int | Default: 0 | 证件类型 |
| IdNumber | string(50) | Required, Unique, 敏感 | 证件号码 (IdentityInfo, 部分掩码) (PAT-D03) |
| PhoneNumber | string(20) | Required, 敏感 | 手机号码 (ContactInfo, 部分掩码) |
| Address | string(256) | Required, 敏感 | 地址 (PersonalInfo, 默认掩码) |
| AllergyHistory | string(500)? | 敏感 | 过敏史 (MedicalInfo, 哈希掩码) |
| MedicalHistory | string(1000)? | 敏感 | 既往病史 (MedicalInfo, 哈希掩码) |
| BloodType | int | Default: 0 | 血型 |
| MaritalStatus | int | Default: 0 | 婚姻状态 |
| EmergencyContactName | string? | - | 紧急联系人姓名 |
| EmergencyContactPhone | string? | - | 紧急联系人电话 |
| EmergencyContactRelation | string? | - | 紧急联系人关系 |
| Status | CommonStatus | Default: Enabled | 患者状态 |
| DisableReason | string(128)? | - | 禁用原因 |
| LastVisitTime | DateTime? | - | 最后就诊时间 (自动更新) |
| VisitCount | int | Default: 0 | 就诊次数 |
| Age | int? | 计算属性 | 基于 BirthDate 计算，NotMapped |

> 继承 BaseEntity (Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, RowVersion)

### 敏感数据保护

| 字段 | 数据类型 | 掩码模式 | 敏感级别 | LocalDB 加密 |
|------|----------|----------|---------|------------|
| IdNumber | IdentityInfo | 部分掩码 (前3后4) | L1-高敏感 | AES-256 加密存储 |
| PhoneNumber | ContactInfo | 部分掩码 (前3后4) | L1-高敏感 | AES-256 加密存储 |
| Address | PersonalInfo | 默认掩码 (前6字符) | L2-一般敏感 | 明文 |
| AllergyHistory | MedicalInfo | 哈希掩码 | L2-一般敏感 | 明文 |
| MedicalHistory | MedicalInfo | 哈希掩码 | L2-一般敏感 | 明文 |

> L1 字段在本地 LocalDB 中通过 EF Core Value Converter 透明加密 (AES-256 + DPAPI 密钥保护)。加密字段不支持 LIKE 搜索，搜索在解密后的内存中执行。详见 nfr.md NFR-SEC-004。

---

## Error Codes

> 错误码分区: 2xxxx (Patients 模块)。Service 层采用 Result 模式统一返回。

### 结构化错误码

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-20001 | PatientNotFound | 404 | 患者不存在 | GetById/Update/Delete 时 ID 无效或已被软删除 |
| ERR-20002 | PatientIdCardExists | 409 | 系统中已存在该身份证 | 创建/更新/导入时身份证号重复 (PAT-D03) |
| ERR-20003 | PatientPhoneExists | 409 | 患者电话已存在 | 创建/更新时手机号重复 |
| ERR-20004 | PatientHasReferencedCases | 422 | 该患者有历史医案，无法删除，请使用禁用功能 | 删除时有关联医案 (任何状态) (MC-D04) |
| ERR-20005 | PatientDisabled | 403 | 患者已被禁用 | 对 Status=Disabled 的患者执行需启用状态的操作 |
| ERR-20006 | InvalidPatientStatus | 400 | 无效的患者状态 | 状态转换非法 |
| ERR-00003 | ValidationFailed | 400 | 参数验证失败 | FluentValidation 验证不通过 |

### 业务规则错误 (207xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-20701 | PhoneDuplicate | 400 | 手机号 {PhoneNumber} 已存在 | 创建/更新时同一手机号已被占用 |
| ERR-20702 | PatientNotDeleted | 200 | 该患者未被删除，无需恢复 | 恢复未软删除的患者 |
| ERR-20703 | BatchOperationEmpty | 400 | 请至少选择一个患者 | 批量删除时 ID 列表为空 |
| ERR-20704 | BatchCheckExceeded | 400 | 批量检查最多支持100条记录 | BatchCheckReference 超过 100 条 |
| ERR-20705 | InvalidPagination | 400 | 页码和页大小参数无效（页码>0，页大小1-100） | 分页参数校验失败 ([nfr.md](nfr.md) NFR-API-001) |

### 导入错误 (US-PAT-008, 208xx)

| 错误码 | 枚举名 | HTTP | 用户消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-20801 | ImportFileEmpty | 400 | 文件不能为空 | file==null 或 file.Length==0 |
| ERR-20802 | ImportFileFormat | 400 | 仅支持.xlsx格式的Excel文件 | 扩展名不是 .xlsx |
| ERR-20803 | ImportFileSize | 400 | 文件大小不能超过10MB | file.Length > 10MB |
| ERR-20804 | ImportNoWorksheet | 400 | Excel文件中没有工作表 | Workbook 无 Worksheets |
| ERR-20805 | ImportRowExceeded | 400 | 导入数据超过限制（最大1000行） | rowCount > 1000 |

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

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| PAT-D03 | 身份证号必填 + 唯一性检查 | US-PAT-001 + 数据模型 | 已确定: IdNumber 改为 Required + Unique，创建/更新时验证重复 |
| PAT-D04 | 患者合并功能 | - | 已确定: v1.0 不包含。操作流程"先查后建"防重复 |
| PAT-D05 | 禁用场景与原因 | US-PAT-013 | 已确定: v1.0 主要禁用场景为患者已故。"长期未就诊"不作为禁用条件。重复录入由身份证唯一性 (PAT-D03) 防止 |
| PAT-D06 | 重复患者关系转移 | - | 已确定: 后续版本规划。功能: 将 A2 的医案关系转移到 A1，然后禁用 A2。v1.0 不包含 |
| PAT-D07 | 本地模式下导入导出的支持方式 | US-PAT-008 ~ 010 | 已确定: 支持。客户端 NPOI 本地读写 Excel，不经过 API |
| PAT-D08 | 敏感数据加密策略 | 所有敏感字段 | 已确定: v1.0 采用字段级加密 (以 [nfr.md](nfr.md) 为准)。详细方案见"信息保护深化"独立任务 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-17 | FR-PAT-005 增加引用检查 (有医案禁止删除) | 数据完整性保障 | MC-D04 |
| 2026-02-17 | FR-PAT-001 身份证号改必填 + 唯一性检查 | 防重复患者，替代合并功能 | PAT-D03 |
| 2026-02-17 | Receptionist 改为 CRU 权限 (无删除) | PRD 审查修复 | A2 |
| 2026-02-18 | 敏感数据保护表增加敏感级别 (L1/L2) 和 LocalDB 加密标注 | 信息保护深化 | NFR-SEC-004 |
| 2026-02-18 | 新增 FR-PAT-013 患者状态管理 | 已故患者管理需求 | PAT-D05 |
| 2026-02-18 | FR-PAT-002 补充分页验证规则 | NFR 对齐 | NFR-API-001 |
| 2026-02-26 | IPatientDataSource 扩展双模式实现 | Sprint 4 实现 | T4-X2-09~12 |
| 2026-02-28 | 数据模型补充 IdType/DisableReason/EmergencyContactRelation 字段 | PRD 偏差修复 | PRD-01 |
| 2026-02-28 | CheckReference HTTP 方法从 POST 修正为 GET | PRD 偏差修复 | PRD-12 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 PatientsController + PatientModel 提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含结构化错误码 7 个 + 导入错误 12 个场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果]，12 个 FR 共 18 条验收标准 |
| 2026-02-17 | v1.3 | Round 9: FR-PAT-005 增加引用检查 (有医案禁止删除)，FR-PAT-011 CanDelete 规则变更，ERR-20004 更新 |
| 2026-02-17 | v1.4 | Round 10: FR-PAT-001 身份证号改必填+唯一性检查 (PAT-D03)，数据模型 IdNumber 约束变更，ERR-20002 更新，PAT-D04 确认无患者合并 |
| 2026-02-17 | v1.5 | PRD审查修复: A2-Receptionist改为CRU权限, A6-加密策略对齐nfr.md, B1-canDelete=false修复, C2-PhoneNumber/Address改Required(患者四必填) |
| 2026-02-18 | v1.6 | 信息保护深化: 敏感数据保护表增加敏感级别(L1/L2)和SQLite加密标注，关联nfr.md NFR-SEC-004 |
| 2026-02-18 | v1.7 | 错误码全量分配: 业务规则错误补充207xx编号(4个)，导入错误补充208xx编号(5个) |
| 2026-02-18 | v1.8 | 新增 FR-PAT-013 患者状态管理 (启用/禁用); 明确禁用场景 (PAT-D05: 患者已故); v2.0 规划关系转移 (PAT-D06); ERR-20005 触发条件明确化 |
| 2026-02-18 | v1.9 | FR-PAT-002 补充分页验证规则 (NFR-API-001); 新增 ERR-20705 分页错误码 |
| 2026-02-26 | v2.0 | Sprint 4 已实现标记: IPatientDataSource 扩展 BatchImportAsync/GetAllForExportAsync/HasMedicalCasesAsync/BatchCheckReferencesAsync (T4-X2-09~12) |
| 2026-02-28 | v2.1 | PRD 偏差修复: 数据模型补充 IdType/DisableReason/EmergencyContactRelation 字段 (PRD-01); CheckReference HTTP 方法从 POST 修正为 GET (PRD-12) |
| 2026-03-06 | v3.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节 |
