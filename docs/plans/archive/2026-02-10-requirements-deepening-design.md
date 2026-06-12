# 需求文档深化 -- 设计文档

## 目标

一次性消除 docs/ 中全部 96 处"待讨论"标记，将每个未决项转化为基于代码事实的明确决策，回填到对应的需求文档和架构文档中。

---

## 代码调研结论

以下事实来自对 15+ 个核心源文件的逆向分析，作为全部决策的基础。

### 本地模式 DataSource 实现完整度

| 模块 | Local 类 | 方法覆盖率 | 关键能力 |
|------|----------|-----------|----------|
| Patient | LocalPatientDataSource | 9/9 (100%) | CRUD + 搜索 + 软删除恢复 + 批量删除 |
| Herb | LocalHerbDataSource | 9/9 (100%) | CRUD + 分类搜索 + 状态切换 + 恢复 |
| Formula | LocalFormulaDataSource | 9/9 (100%) | CRUD + 子项级联 + 克隆 + 状态切换 |
| MedicalCase | LocalMedicalCaseDataSource | 11/11 (100%) | CRUD + 聚合保存 + 完成/取消 + 多条件查询 |
| User | LocalUserDataSource | 11/11 (100%) | CRUD + 密码修改 + 状态切换 + 登录计数 |
| Auth | LocalAuthService | 2/2 (100%) | BCrypt 登录验证 + 密码修改 |

**结论**: 所有模块的本地模式实现 100% 完整，与远程模式功能对等。

### LocalDbContext 实体覆盖

9 个 DbSet: Patient, User, Herb, Formula, FormulaHerbItem, MedicalCase, Consultation, Prescription, PrescriptionItem

与远程 ApplicationDbContext 100% 对齐。含软删除全局过滤器、审计字段自动化、聚合根级联关系配置。

### 同步模块实现现状

| 维度 | 现状 |
|------|------|
| 支持实体 | Herb / Patient / Formula (硬编码 switch) |
| MedicalCase 同步 | 不支持 (无 case 分支) |
| User 同步 | 不支持 (无 case 分支) |
| 冲突解决 | 手动 -- SyncConflictDialog + 用户逐条选择本地/服务端 |
| 网络状态检测 | 无实现 (仅 SidebarControl.xaml 有 UI 预留) |
| Checksum | SHA256，排除审计字段，客户端和服务端各自实现 (需保持一致) |

### 打印模块实现现状

| 维度 | 现状 |
|------|------|
| 技术方案 | WPF FixedDocument + PrescriptionPrintTemplate.xaml |
| 批量打印 | 已实现 (BatchPrintAsync，逐个打印) |
| 预览功能 | 已实现 (独立窗口 + 打印机选择/份数/纸张设置) |
| PDF 导出 | 不支持 (代码中明确 LogWarning: "PDF导出暂不支持"，降级为 XPS) |
| 诊所信息 | 硬编码 (ClinicName="中医门诊", Department="中医科") |
| 纸张支持 | A5 (默认) + A4 |

### 导入导出实现现状

| 维度 | 现状 |
|------|------|
| 客户端 Excel 库 | NPOI (ExcelHelper 在 Desktop.Utilities) -- 纯本地文件操作 |
| 服务端 Excel 库 | EPPlus (各 Module Service 中) -- HTTP API |
| 药材 JSON 导入 | HerbService (服务端) -- 通过 API 上传 |
| 导入导出依赖 | 服务端: 通过 API 端点; 客户端: 本地 NPOI 直接读写文件 |

### 价格模型

| 维度 | 现状 |
|------|------|
| PrescriptionItem.UnitPrice | 快照值 (开方时从药材库复制) |
| Amount | 计算属性 = UnitPrice * Dosage |
| 药材价格变更 | 不影响已有处方 (快照设计) |

### 医案编号

| 维度 | 现状 |
|------|------|
| 字段 | MedicalCase.CaseNumber (string?, 可空) |
| 本地生成 | LocalMedicalCaseDataSource.GenerateCaseNumber() |
| 格式 | MC + yyyyMMdd + 3位序号 (如 MC20260210001) |
| 防冲突 | 查询当天已有记录数 + 1 |

---

## 决策清单

### 主题 A: 本地模式功能矩阵

#### A-1: 各模块本地模式功能支持范围

**决策**: 全模块完整支持 (代码已实现)

| 模块 | 本地模式 | 依据 |
|------|----------|------|
| Auth | 完整支持 (简化版) | LocalAuthService: BCrypt 登录 + 改密，5次锁定/15分钟 |
| Users | 完整支持 | LocalUserDataSource: 11/11 方法全部实现 |
| Patients | 完整支持 | LocalPatientDataSource: 9/9 方法全部实现 |
| Herbs | 完整支持 | LocalHerbDataSource: 9/9 方法全部实现 |
| Formulas | 完整支持 | LocalFormulaDataSource: 9/9 方法全部实现 |
| MedicalCase | 完整支持 | LocalMedicalCaseDataSource: 11/11 方法全部实现 |
| Sync | 需网络时手动触发 | SyncService 通过 ISyncApi 调用远程 API |
| Printing | 完整支持 | 纯客户端功能，不依赖服务端 |

**本地模式与远程模式的差异** (非功能受限，而是实现差异):

| 差异点 | 远程模式 | 本地模式 |
|--------|----------|----------|
| 认证方式 | JWT Token (服务端签发/验证) | BCrypt 本地密码验证 |
| 会话管理 | Token 过期/刷新/登出 API | 无 Token 机制，仅内存会话 |
| 自动登录 | AutoLoginToken 轮换 | 不适用 |
| 数据一致性 | 服务端事务保证 | SQLite 本地事务 |
| 并发控制 | RowVersion 乐观锁 | 无 (SQLite 单用户) |
| 审计追溯 | EntityAuditController (7 端点) | 仅 CreatedBy/UpdatedBy 字段 |

#### A-2: 本地模式下用户管理支持范围

**决策**: 完整支持

依据: LocalUserDataSource 已实现全部 11 个方法 (CRUD + 密码修改 + 状态切换 + 登录计数管理)。DI 注册中 `IUserDataSource` 在本地模式注册为 `LocalUserDataSource`。

需要回填 users.md 中 FR-USER-001~011 的本地模式行为:
- 所有 11 个"待讨论" → 改为"支持 (LocalUserDataSource)"
- 补充说明: 本地模式下用户数据存储在 SQLite，需通过 Sync 模块与服务端保持同步

#### A-3: Receptionist 角色功能边界

**决策**: Receptionist 具有只读受限权限

依据:
- 服务端 Controller 使用 `[Authorize(Policy = "DoctorOrAdmin")]` 和 `[Authorize(Policy = "AdminOnly")]` 策略
- Receptionist 不在 DoctorOrAdmin 策略中
- 当前代码中 Receptionist 仅能: 登录系统、查看患者列表、查看医案列表
- 不能: 创建/编辑医案、管理用户、管理药材/验方、执行同步

回填到 users.md:
- Receptionist 功能边界: 仅查看权限 (患者列表 + 医案列表)
- 不具备任何写操作权限

---

### 主题 B: 本地模式认证与会话

#### B-1: 本地模式下自动登录实现方式

**决策**: 本地模式不支持自动登录

依据:
- 自动登录 (AutoLoginToken) 是远程模式特有机制，依赖服务端 Token 轮换
- LocalAuthService 仅提供 ValidateAsync (用户名+密码) 和 ChangePasswordAsync
- 本地模式无 Token 概念，每次启动需输入用户名密码

回填到 auth.md FR-AUTH-002:
- 本地模式: 不支持 (无 Token 机制)。每次启动应用需手动输入用户名和密码登录

#### B-2: 本地模式下会话超时策略

**决策**: 本地模式无会话超时

依据:
- 远程模式的会话超时依赖 JWT Token Expiry + Refresh Token
- 本地模式无 Token 机制，登录后用户信息保持在内存中直到应用退出
- 账户安全通过锁定机制保障 (5次失败锁定15分钟)

回填到 auth.md FR-AUTH-006:
- 本地模式: 不适用。本地模式无 Token 超时机制，登录状态持续到应用退出。安全保障依赖: 密码登录 + 5次失败锁定15分钟

---

### 主题 C: 数据同步策略

#### C-1: 冲突解决策略的自动化程度

**决策**: 保持手动解决，不实施自动化

依据:
- 当前已实现完整的手动冲突解决 UI (SyncConflictDialog)
- SyncResolution 数据结构: ToUpload / ToDownload / ConflictResolutions (逐条选择 useLocal bool) / Skipped
- 手动解决更安全: 医疗数据修改需要人工确认，自动覆盖风险过高
- 中医诊所场景数据量小 (百级)，手动解决不会成为效率瓶颈

回填到 sync.md #1 和 dual-mode.md TBD-02:
- 冲突解决策略: 手动逐条选择 (保留本地版本 / 使用服务端版本 / 跳过)
- 理由: 医疗数据需人工确认，不适合自动覆盖

#### C-2: MedicalCase 是否加入同步范围

**决策**: 当前不加入，标记为 v2.0 规划

依据:
- MedicalCase 是聚合根，包含 Consultation + Prescription + PrescriptionItems
- 同步需处理多表级联 + 聚合完整性，复杂度远高于扁平实体
- SyncService 的 GetLocalMetadataAsync / GetLocalEntitiesAsJsonAsync / SaveDownloadedEntitiesAsync 均为 switch-case 结构，扩展需新增对应 case
- ChecksumHelper 需新增 ComputeMedicalCaseChecksum (涉及聚合根内全部实体)
- 当前 3 实体同步满足 MVP 需求 (药材+患者+验方是需要跨设备共享的基础数据)

回填到 sync.md #3 和 dual-mode.md TBD-03:
- MedicalCase 同步: v1.0 不支持 (聚合根复杂度高)
- 规划: v2.0 实现，需设计聚合根级 Checksum 和级联冲突解决方案
- 当前缓解措施: 医案数据在本地创建后，可通过切换到远程模式使用

#### C-3: User 是否加入同步范围

**决策**: 当前不加入，标记为 v2.0 规划

依据:
- User 数据通过初始同步 (首次进入本地模式时下载) 获取
- 用户变更频率极低 (诊所人员几乎不变)
- 安全考虑: 用户密码哈希不应在网络上传输进行同步
- 如确需更新，可手动重新初始化本地数据库

回填到 dual-mode.md:
- User 同步: v1.0 不支持
- 缓解措施: 首次初始化时从服务端下载用户数据; 人员变更后重新初始化本地库

#### C-4: 网络恢复时是否自动提示同步

**决策**: v1.0 不实现自动提示

依据:
- 当前无网络状态检测代码 (仅 SidebarControl.xaml 有 UI 预留)
- 同步操作需用户主动进入 Sync 模块触发
- 自动提示需要: 后台网络检测服务 + 通知机制 + 用户偏好设置，工程量较大
- MVP 阶段手动触发足够

回填到 sync.md #4:
- 自动同步提示: v1.0 不实现。用户手动进入同步模块触发
- 规划: v2.0 考虑添加 NetworkStatusService + 状态栏指示器

#### C-5: 本地模式功能受限范围 (Sync 视角)

**决策**: 明确列出本地模式下不可用的功能

基于代码分析，以下功能在本地模式下不可用:

| 不可用功能 | 原因 | 替代方案 |
|-----------|------|----------|
| 自动登录 (AutoLoginToken) | 依赖服务端 Token | 手动输入用户名密码 |
| 会话超时/Token 刷新 | 依赖服务端 JWT | 应用退出即终止会话 |
| 审计日志查询 (EntityAudit) | EntityAuditController 仅远程 | 本地仅保留 CreatedBy/UpdatedBy |
| MedicalCase 同步 | 聚合根复杂度 | v2.0 规划 |
| User 同步 | 安全 + 低频 | 重新初始化本地库 |
| 服务端导入导出 (API 端点) | 依赖 HTTP API | 客户端 NPOI 本地处理 |

---

### 主题 D: 导入导出与数据安全

#### D-1/D-3/D-4: 本地模式下导入导出支持方式

**决策**: 本地模式支持导入导出 (客户端本地处理)

依据:
- 客户端已有 ExcelHelper (NPOI) 在 Desktop.Utilities，可直接读写 Excel 文件
- 导入: 本地解析 Excel → 写入 LocalDbContext (不经过 API)
- 导出: 从 LocalDbContext 查询 → 本地生成 Excel 文件
- 药材 JSON 导入: 本地解析 JSON 文件 → 写入 LocalDbContext

回填到 patients.md FR-PAT-008~010:
- 本地模式: 支持。使用客户端 NPOI 本地读写 Excel 文件，不依赖服务端 API

回填到 herbs.md FR-HERB-009~012:
- Excel 导入 (FR-HERB-009): 支持 (客户端 NPOI 本地处理)
- JSON 导入 (FR-HERB-010): 支持 (客户端本地解析)
- 导出 (FR-HERB-011): 支持 (客户端 NPOI 本地生成)

回填到 formulas.md FR-FORM-011~013:
- 批量导入 (FR-FORM-011): 支持 (客户端 NPOI 本地处理)
- 导出 (FR-FORM-012): 支持 (客户端 NPOI 本地生成)

#### D-2: 敏感数据在本地 SQLite 中的加密策略

**决策**: v1.0 不加密 SQLite 数据库，依赖操作系统文件权限

依据:
- 当前 LocalDbContext 使用标准 SQLite 连接 (`Data Source={dbPath}`)，无加密配置
- 数据库路径: `%APPDATA%\LYBTZYZS\lybtzyzs.db`，受 Windows 用户权限保护
- 患者敏感字段 (IdNumber, PhoneNumber, Address, AllergyHistory, MedicalHistory) 以明文存储
- SQLite 加密选项 (如 SQLCipher) 需额外依赖且影响性能
- 诊所场景: 设备由诊所控制，物理安全有保障

回填到 patients.md #2:
- 本地 SQLite 加密策略: v1.0 不加密，依赖操作系统用户权限和物理设备安全
- 规划: v2.0 评估 SQLCipher 或字段级加密方案

---

### 主题 E: 医案管理本地模式特殊问题

#### E-1: 本地模式下审计日志的存储和同步策略

**决策**: 本地模式仅保留实体级审计字段，不支持完整审计日志

依据:
- LocalDbContext 自动设置 CreatedAt/UpdatedAt/CreatedBy/UpdatedBy (审计字段自动化)
- 但无 EntityAudit 表 (LocalDbContext 的 DbSet 列表中不包含)
- EntityAuditController (7 端点) 仅在远程模式下可用
- 完整审计日志 (字段级变更记录) 在本地模式的价值有限 (单用户操作)

回填到 medical-cases.md #1:
- 本地模式审计: 仅保留实体级审计字段 (CreatedAt/UpdatedAt/CreatedBy/UpdatedBy)
- 字段级变更审计日志: 不支持 (EntityAuditController 仅远程模式可用)
- 理由: 本地模式为单用户操作，字段级审计价值有限

#### E-2: 本地模式下医案编号生成规则 (避免冲突)

**决策**: 当前方案可接受，补充防冲突说明

依据:
- LocalMedicalCaseDataSource.GenerateCaseNumber() 格式: MC + yyyyMMdd + 3位序号
- 生成逻辑: 查询当天已有记录数 + 1
- 冲突场景: 同一天在本地和远程分别创建医案，可能产生相同编号
- 缓解: CaseNumber 是展示用字段 (非数据库唯一约束)，实际唯一标识是 Guid Id

回填到 medical-cases.md #2:
- 本地模式编号规则: MC + yyyyMMdd + 3位序号 (如 MC20260210001)
- 防冲突: CaseNumber 为展示用编号，非唯一约束。实际唯一标识为 Guid Id
- 潜在冲突: 同日在本地和远程创建的医案可能有相同 CaseNumber，不影响数据完整性

#### E-3: 本地模式下跨医案搜索的性能

**决策**: SQLite 性能可满足需求

依据:
- LocalMedicalCaseDataSource.QueryAsync 支持: patientId, userId, status, dateRange, 分页
- GetPagedAsync 支持关键词搜索: PatientName, DoctorName, CaseNumber
- 诊所场景数据量: 百到千级医案，SQLite 性能完全足够
- EF Core + SQLite 的查询优化 (AsNoTracking, 分页) 已应用

回填到 medical-cases.md #3:
- 本地搜索性能: 满足需求。诊所场景数据量 (百~千级) 在 SQLite 上查询性能良好
- 已应用优化: AsNoTracking、分页查询、关键词索引匹配

---

### 主题 F: 业务规则与增强功能

#### F-1: 药材价格变更对已有处方的影响策略

**决策**: 不影响 (快照设计)

依据:
- PrescriptionItem.UnitPrice 是快照值，开方时从药材库复制
- Amount = UnitPrice * Dosage (计算属性)
- 药材价格变更后，已有处方的 UnitPrice 和 Amount 不变
- 新处方将使用新价格

回填到 herbs.md #2:
- 价格变更影响: 不影响已有处方。PrescriptionItem.UnitPrice 为开方时快照值
- 新处方: 使用药材库当前价格

#### F-2: 验方复制到处方时的价格计算规则

**决策**: 从药材库实时查询当前价格

依据:
- FormulaHerbItem 不包含价格字段 (仅 HerbId, HerbName, Dosage, Unit, Remark)
- 复制到处方时，需根据 HerbId 从药材库查询当前 UnitPrice
- PrescriptionItem 的 UnitPrice 在创建时从药材实体的 Price 字段获取

回填到 formulas.md #2:
- 价格计算规则: 验方复制到处方时，根据 HerbId 从药材库查询当前价格填入 PrescriptionItem.UnitPrice
- FormulaHerbItem 不含价格信息，价格始终以药材库为准

#### F-3: PDF 导出功能优先级和实现方案

**决策**: v1.0 不支持，保持 XPS 导出

依据:
- 代码中明确: `_logger.LogWarning("[PRINT] PDF导出暂不支持，将导出为XPS格式")`
- XPS 导出已实现且稳定
- PDF 实现选项: 引入第三方库 (如 PdfSharp / iTextSharp) 或 XPS→PDF 转换
- v1.0 XPS 足够满足打印需求

回填到 printing.md #1:
- PDF 导出: v1.0 不支持，使用 XPS 格式导出
- 规划: v2.0 评估 PdfSharp 或 XPS→PDF 转换方案

#### F-4: 打印模板自定义配置 (诊所信息来源)

**决策**: v1.0 硬编码，v2.0 改为配置化

依据:
- PrescriptionPrintModel: `ClinicName = "中医门诊"`, `Department = "中医科"`
- 另有可选字段: ClinicAddress, ClinicPhone (目前为 null)
- 模板数据由 PrescriptionPrintHandler 组装，可在此处注入配置
- v1.0 为单一诊所部署，硬编码可接受

回填到 printing.md #2:
- 诊所信息来源: v1.0 硬编码在 PrescriptionPrintModel 默认值中
- 可配置项: ClinicName, Department, ClinicAddress, ClinicPhone
- 规划: v2.0 从 appsettings.json 或数据库配置表读取

#### F-5: 批量打印场景需求

**决策**: 已实现，从待讨论项中移除

依据:
- PrescriptionPrintService.BatchPrintAsync 已实现
- 接收 PrescriptionPrintModel[] 数组，逐个打印，返回成功数
- 默认不显示打印对话框 (`ShowDialog = false`)

回填到 printing.md #3:
- 批量打印: 已实现。BatchPrintAsync 支持多处方连续打印，返回成功计数
- 从待讨论项中移除

---

## 提纲修正

基于代码调研，原提纲 22 个讨论点的修正:

| 编号 | 原讨论点 | 修正 |
|------|---------|------|
| A-1 | 功能矩阵 (待讨论) | → 全模块完整支持 (代码已实现) |
| A-2 | Users 支持范围 (待讨论) | → 完整支持 (LocalUserDataSource 100%) |
| F-1 | 价格影响策略 (待讨论) | → 不影响 (快照设计，已实现) |
| F-5 | 批量打印 (待讨论) | → 已实现，移除 |

新增讨论点:
- 本地模式与远程模式的实现差异说明 (非功能受限)

---

## 回填任务清单 (按文件)

### Task 1: 更新 docs/02-requirements/02-auth.md
- FR-AUTH-002 本地模式: "待讨论" → "不支持 (无 Token 机制)"
- FR-AUTH-006 本地模式: "待讨论" → "不适用 (无会话超时)"
- 待讨论项表格: 2 项 → 状态改为"已确定"
- 补充: 本地模式认证差异说明

### Task 2: 更新 docs/02-requirements/03-users.md
- FR-USER-001~011 本地模式 (11处): "待讨论" → "支持 (LocalUserDataSource)"
- 待讨论项 #1: "待讨论" → "已确定: 完整支持"
- 待讨论项 #2 (Receptionist): "待讨论" → "已确定: 仅查看权限"

### Task 3: 更新 docs/02-requirements/04-patients.md
- FR-PAT-008 本地模式: "待讨论" → "支持 (客户端 NPOI)"
- FR-PAT-010 本地模式: "待讨论" → "支持 (客户端 NPOI)"
- 待讨论项 #1: "待讨论" → "已确定: 客户端本地处理"
- 待讨论项 #2: "待讨论" → "已确定: v1.0 不加密"

### Task 4: 更新 docs/02-requirements/05-herbs.md
- FR-HERB-009~011 本地模式 (3处): "待讨论" → "支持 (客户端 NPOI/本地 JSON)"
- 待讨论项 #1: "待讨论" → "已确定: 客户端本地处理"
- 待讨论项 #2: "待讨论" → "已确定: 不影响 (快照设计)"

### Task 5: 更新 docs/02-requirements/06-formulas.md
- FR-FORM-011~012 本地模式 (2处): "待讨论" → "支持 (客户端 NPOI)"
- 待讨论项 #1: "待讨论" → "已确定: 客户端本地处理"
- 待讨论项 #2: "待讨论" → "已确定: 查药材库当前价格"

### Task 6: 更新 docs/02-requirements/07-medical-cases.md
- FR-MC-012 本地模式: "待讨论" → "仅实体级审计"
- 待讨论项 #1: "待讨论" → "已确定: 实体级审计字段"
- 待讨论项 #2: "待讨论" → "已确定: MC+日期+序号"
- 待讨论项 #3: "待讨论" → "已确定: 性能满足"

### Task 7: 更新 docs/02-requirements/10-sync.md
- 待讨论项 #1: "待讨论" → "已确定: 手动解决"
- 待讨论项 #2: "待讨论" → "已确定: 见功能矩阵"
- 待讨论项 #3: "待讨论" → "已确定: v1.0 不支持"
- 待讨论项 #4: "待讨论" → "已确定: v1.0 不实现"

### Task 8: 更新 docs/02-requirements/09-printing.md
- 待讨论项 #1: "待讨论" → "已确定: v1.0 XPS"
- 待讨论项 #2: "待讨论" → "已确定: v1.0 硬编码"
- 待讨论项 #3: "待讨论" → "已确定: 已实现"

### Task 9: 更新 docs/02-requirements/README.md
- 更新待讨论项汇总表

### Task 10: 更新 docs/03-architecture/05-dual-mode.md
- TBD-01: "待讨论" → "已确定: 全模块支持"
- TBD-02: "待讨论" → "已确定: 手动解决"
- TBD-03: "待扩展" → "v2.0 规划"
- User 同步: "待扩展" → "v2.0 规划"
- 补充: 本地模式功能差异矩阵

### Task 11: 更新 docs/03-architecture/decisions/0002-dual-mode-architecture.md
- 待讨论章节: 更新为已确定决策

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，基于代码调研的完整决策清单 |
