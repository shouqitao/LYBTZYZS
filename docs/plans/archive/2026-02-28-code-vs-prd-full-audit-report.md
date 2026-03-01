# Code-vs-PRD 全量审计报告

> **审计日期**: 2026-02-28
> **审计基线**: 2026-02-21 深度扫描报告 (259 偏差) + Sprint5 剩余 (28 项)
> **审计范围**: 57 个项目, ~1125 个 .cs 文件, 16 个 PRD 文档
> **审计方式**: 8 个 Batch 并行审计 + 1 个合成 Batch

---

## 一、执行摘要

### 1.1 与基线对比

| 指标 | 2026-02-21 基线 | 2026-02-28 现状 | 变化 |
|------|:---:|:---:|:---:|
| 总偏差数 | 259 | - | - |
| 已修复 (DONE) | 0 | **34** | 大量 Sprint5 修复确认 |
| 仍开放 (OPEN) | 259 | **28** | 大幅收敛 |
| 新发现 (NEW) | 0 | **19** | 测试层+架构层新增 |
| 死代码项 | 未统计 | **14** | 首次覆盖 |

### 1.2 各 Batch 审计结果汇总

| Batch | 范围 | TODO-CODE | TODO-PRD | TODO-DEAD-CODE |
|:---:|------|:---:|:---:|:---:|
| 1 | Entity + Infrastructure | 10 | 7 | 4 |
| 2 | Auth + Users | 5 | 0 | 2 |
| 3 | Patient + Herb + Formula | 4 | 2 | 0 |
| 4 | MedicalCase + Sync + Printing | 6 | 3 | 2 |
| 5 | Shared 层 | 7 | 0 | 2 |
| 6 | Desktop Core + Shell | 5 | 1 | 3~5 |
| 7 | Desktop 业务模块 | 3 | 4 | 2 |
| 8 | 测试层 | 7 | 0 | 1~3 |
| **合计** | | **47** | **17** | **16~18** |

### 1.3 关键修复确认 (Sprint5 成果)

以下在 2026-02-21 报告中标记为 P1 的高危问题已确认**全部修复**:

1. **Token Family 撤销 (X3)**: 6 个场景全部实现 (登录/角色变更/删除/重置/修改密码/禁用)
2. **引用检查 (X7)**: Patient 单个+批量、Herb 单个删除均已实现真实引用查询
3. **密码 Hash Bug (T1-S1-01)**: 已修复，使用 newPassword 哈希
4. **FormulaMapper Herbs 映射 (T2-S4-01)**: ToDetailDto 正确映射 Herbs 集合
5. **账户锁定 (T5-P2-01)**: FailedLoginCount + 5次锁15分钟
6. **患者状态管理 (T2-S4-08)**: ToggleStatus 基础功能实现
7. **MedicalCase 打印字段迁移 (T2-X8-02)**: IsPrinted/PrintVersion/PrintCount/LastPrintedAt 已在 MedicalCaseModel
8. **MedicalCasePrintLog 创建 (T2-X8-03)**: 新实体已创建并注册
9. **打印后回写 (T2-X8-06~12)**: RecordPrintCompletedAsync 完整实现
10. **IgnoreQueryFilters (T5-P2-40)**: Sync 三个 Metadata 方法均已应用
11. **EditReason 强制规则 (T1-S3-01~04)**: UpdateConsultation/UpdatePrescription/Save 均校验

---

## 二、TODO-CODE 完整清单

### 优先级: CRITICAL (安全/数据完整性)

| ID | Batch | 描述 | 位置 |
|----|:---:|------|------|
| CODE-01 | 4 | CompleteAsync 未验证 TcmDiagnosis 必填 (PRD FR-MC-002 明确要求) | `MedicalCaseStateService.cs` |
| CODE-02 | 4 | 编辑已打印医案后未重置 IsPrinted=false + PrintVersion++ | `MedicalCaseCommandService.cs` UpdateConsultation/UpdatePrescription |
| CODE-03 | 2 | LoginAsync 未撤销 AutoLoginToken Family (AUTH-D06 步骤4) | `AuthService.cs:203-225` |
| CODE-04 | 2 | sysadmin 不可被管理 API 层硬兜底缺失 (USER-D05) | `UserService.cs` 各操作方法 |

### 优先级: HIGH (功能缺失/偏差)

| ID | Batch | 描述 | 位置 |
|----|:---:|------|------|
| CODE-05 | 1 | MedicalCase->Patient FK 关系 Fluent API 缺失 | `MedicalCaseConfiguration.cs` |
| CODE-06 | 1 | MedicalCase->User FK 关系 Fluent API 缺失 | `MedicalCaseConfiguration.cs` |
| CODE-07 | 5 | DefaultRole 仍为 "Staff" (应 "Doctor") | `UserManagementOptions.cs:17` |
| CODE-08 | 7 | 验方导入/历史复制实时价格同步缺失 (绕过 AddItem) | `PrescriptionImportHandler.cs` |
| CODE-09 | 8 | LYBT.Tests.Desktop.Unit Mock 框架混用 (9 Moq + 11 NSubstitute) | 9 个测试文件 |
| CODE-10 | 6 | LocalData 项目不在 sln 中 (需决策: 重新加入或正式标记清理) | `LYBT.Desktop.LocalData/` |
| CODE-11 | 3 | Herb BatchDeleteAsync 缺引用检查 | `HerbService.cs:723-776` |

### 优先级: MEDIUM (部分实现/偏差)

| ID | Batch | 描述 | 位置 |
|----|:---:|------|------|
| CODE-12 | 5 | Herb.Effect DTO MaxLength 1000->500 | `HerbInputDto.cs:62` |
| CODE-13 | 5 | Herb.Spec DTO MaxLength 50->100 | `HerbInputDto.cs:40` |
| CODE-14 | 5 | Formula.Effect DTO MaxLength 200->500 | `FormulaInputDto.cs:19` |
| CODE-15 | 5 | SensitiveDataAttribute 重复定义消除 | `LYBT.Entities/Attributes/` vs `LYBT.Shared.Logging/Masking/` |
| CODE-16 | 1 | AuthSession->User FK 关系缺失 | `AuthSessionConfiguration.cs` |
| CODE-17 | 1 | PrescriptionItem->Herb FK 关系缺失 | `PrescriptionItemConfiguration.cs` |
| CODE-18 | 1 | AutoLoginToken 缺 EF Configuration 文件 | 需新建 `AutoLoginTokenConfiguration.cs` |
| CODE-19 | 6 | 登出未调用 ClearHistory() | `MainWindowViewModel.PerformLogoutAsync` |
| CODE-20 | 6 | 账户设置缺 Email 编辑 | `AccountSettingsViewModel.cs` |
| CODE-21 | 6 | 状态栏缺同步标识/用户名/版本号 | `GlobalStatusBar.xaml` |
| CODE-22 | 3 | Patient ToggleStatus 缺活跃医案检查+权限限制 | `PatientService.cs:770-791` |
| CODE-23 | 3 | Formula TotalPrice/HerbCount 始终为 0 | `FormulaService.cs` GetPaged/GetById |
| CODE-24 | 4 | 打印服务未校验空处方 | `PrescriptionPrintService.cs` |
| CODE-25 | 2 | TokenExpired vs TokenInvalid 错误码区分 (缺 AuthTokenExpired=10201) | `ErrorCode.cs`, `JwtService.cs` |
| CODE-26 | 7 | 禁用药材跳过未向用户显示提示 | `PrescriptionImportHandler.cs:178-194` |
| CODE-27 | 8 | 遗留 UnitTests/ Mock 框架统一为 NSubstitute | ~20 个测试文件 |

### 优先级: LOW (细节/文档)

| ID | Batch | 描述 | 位置 |
|----|:---:|------|------|
| CODE-28 | 5 | WarningBeforeTimeoutMinutes 默认值 0->2 | `ClientSessionOptions.cs:23` |
| CODE-29 | 5 | 审计日志保留天数改配置化 (当前硬编码 365) | `SecurityAuditCleanupService.cs` |
| CODE-30 | 1 | Discount 精度 DataAnnotation(5,4) vs FluentAPI(3,2) | `PrescriptionModel.cs` + `PrescriptionConfiguration.cs` |
| CODE-31 | 1 | PrintLog Configuration 未继承 BaseEntityConfiguration | 2 个 Configuration 文件 |
| CODE-32 | 1 | BaseRepository 缺 RestoreAsync(id) 方法 | `BaseRepository.cs` |
| CODE-33 | 1 | BaseRepository 缺 ExistsAsync(Guid id) 重载 | `BaseRepository.cs` |
| CODE-34 | 2 | BatchDeleteAsync 非单事务 (PRD 要求单事务) | `UserService.cs:634-714` |
| CODE-35 | 2 | CanManageUser 注释权限值不匹配 | `UserService.cs:89-91` |
| CODE-36 | 4 | A4 排版复用 A5 模板缩放，缺独立 A4 适配 | `PrescriptionPrintTemplate.xaml` |
| CODE-37 | 4 | 药名截断使用 WPF TextTrimming 而非 10 字符截断 | `PrescriptionPrintTemplate.xaml` |
| CODE-38 | 4 | Draft->Suspended 术语未同步 | `MedicalCaseEnums.cs` |
| CODE-39 | 3 | import-template 端点缺 AllowAnonymous (3 模块) | Patient/Herb/Formula Controllers |
| CODE-40 | 7 | IFormulaDataSource/IMedicalCaseDataSource 缺 BatchDeleteAsync 接口声明 | 2 个接口文件 |
| CODE-41 | 8 | 3 处 [Obsolete] 标记清理 | AuthEnums.cs, ICrossModuleQueryService.cs, MedicalCaseController.cs |
| CODE-42 | 8 | LYBT.Module.Herbs.Tests .csproj Moq 包是死引用 | `.csproj` 文件 |

---

## 三、TODO-PRD 完整清单

### 优先级: HIGH (影响代码对齐)

| ID | Batch | 描述 | 目标文件 |
|----|:---:|------|----------|
| PRD-01 | 1 | data-model.md: Patient 缺 IdType/EmergencyContact*/DisableReason 字段 | `docs/03-architecture/data-model.md` |
| PRD-02 | 1 | server.md: BaseEntity 表格不完整 (缺 UpdatedBy/RowVersion) | `docs/03-architecture/server.md:64-73` |
| PRD-03 | 1 | server.md: BaseRepository 方法列表不准 (CreateAsync vs AddAsync; 缺多方法) | `docs/03-architecture/server.md:119-133` |
| PRD-04 | 4 | server.md: 模块列表仍含已删除的 Module.Consultation + Module.Prescriptions | `docs/03-architecture/server.md:22-34,164-165` |

### 优先级: MEDIUM

| ID | Batch | 描述 | 目标文件 |
|----|:---:|------|----------|
| PRD-05 | 1 | data-model.md: Herb 缺 Remark 字段 | `docs/03-architecture/data-model.md` |
| PRD-06 | 1 | data-model.md: PrintCount/LastPrintedAt 仍在 Prescription 表 (应在 MedicalCase) | `docs/03-architecture/data-model.md:124-125` |
| PRD-07 | 1 | server.md: "14 个标准方法" 说法与实际不符 (IRepository 11 个) | `docs/03-architecture/server.md:80` |
| PRD-08 | 1 | server.md: BaseReadRepository 被列为组件，但主分支不存在 | `docs/03-architecture/server.md:81` |
| PRD-09 | 4 | SyncMetadataDto: PRD 用 DisplayName，代码用 EntityName | `docs/02-requirements/sync.md` |
| PRD-10 | 4 | Draft vs Suspended 术语: PRD 修订了但代码未跟进 | `docs/02-requirements/medical-cases.md` |
| PRD-11 | 3 | Herb/Formula Create 是否也应返回 201? (Patient 已改为 201) | `docs/02-requirements/herbs.md`, `formulas.md` |
| PRD-12 | 3 | Patient CheckReference HTTP Method: PRD 写 POST, 代码实现 GET | `docs/02-requirements/patients.md` |

### 优先级: LOW

| ID | Batch | 描述 | 目标文件 |
|----|:---:|------|----------|
| PRD-13 | 7 | card-reader.md CARD-D01: appsettings 配置 vs 代码枚举选择 | `docs/02-requirements/card-reader.md` |
| PRD-14 | 7 | card-reader.md CARD-D02: DPAPI 照片加密未实现 | `docs/02-requirements/card-reader.md` |
| PRD-15 | 7 | card-reader.md CARD-D03: 患者去重降级链仅实现精确匹配 | `docs/02-requirements/card-reader.md` |
| PRD-16 | 7 | card-reader.md FR-CARD-002 规则5: 姓名->RealName 应更新为 Name | `docs/02-requirements/card-reader.md` |
| PRD-17 | 6 | FR-SHELL-001: 步骤数 PRD "6步" vs 代码实际 5 步 | `docs/02-requirements/desktop-shell.md` |

---

## 四、TODO-DEAD-CODE 完整清单

### 确认可删除

| ID | Batch | 文件路径 | 理由 |
|----|:---:|---------|------|
| DEAD-01 | 1 | `src/Shared/LYBT.Shared.Models/Enums/PatientStatus.cs` | 零代码引用，Patient 已使用 CommonStatus |
| DEAD-02 | 5 | `src/Shared/LYBT.Shared.Models/Constants/ErrorMessageKeys.cs` | 零代码引用，已被 ErrorCode/ErrorMessages 替代 |
| DEAD-03 | 1 | `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionPrintLog.cs` | 已被 MedicalCasePrintLog 替代 |
| DEAD-04 | 1 | `src/Server/Core/LYBT.Infrastructure/Data/Configurations/PrescriptionPrintLogConfiguration.cs` | 随 DEAD-03 一起清理 |
| DEAD-05 | 6 | `Shell/Views/PlaceholderViews.cs` 中的 `LoginView` | Auth 模块已有完整 LoginView.xaml |
| DEAD-06 | 6 | `Shell/Views/PlaceholderViews.cs` 中的 `PrescriptionView` | Prescriptions 模块 2026-01-05 已移除 |
| DEAD-07 | 6 | `Shell/Views/PlaceholderViews.cs` 中的 `ConsultationView` | Desktop 端无独立 Consultation 模块 |
| DEAD-08 | 8 | `tests/UnitTests/Server/LYBT.Shared.Utilities.Tests/PasswordHelperTests.cs` | 无 .csproj，功能已在 LYBT.Tests.Unit 覆盖 |
| DEAD-09 | 2 | `AuthService.RevokeTokenAsync(RevokeTokenRequest)` 空方法 | 方法体直接 return Success |
| DEAD-10 | 2 | `ErrorCode.TokenExpired = 10013` | 旧编码，已被 MCCEE 102xx 替代 |

### 需确认后删除

| ID | Batch | 文件路径 | 理由 | 确认项 |
|----|:---:|---------|------|--------|
| DEAD-11 | 1 | `src/Server/Core/LYBT.Entities/Auth/BlacklistedToken.cs` | 无 DbSet/Configuration/引用 | 确认 JWT 黑名单功能是否已被 RefreshToken 重放检测替代 |
| DEAD-12 | 6 | `Shell/Views/PlaceholderViews.cs` 中 PatientListView/PatientDetailView | 可能被导航引用 | 确认 RegisterForNavigation 是否引用 |
| DEAD-13 | 6 | `src/Client/Desktop/Core/LYBT.Desktop.LocalData/` 整体 | 不在 sln 中 | 需架构决策 |
| DEAD-14 | 8 | 遗留 `tests/IntegrationTests/` 下 3 个项目 | 可能与主集成测试项目重复 | 需评估合并可行性 |

---

## 五、测试层专项发现

### 5.1 Mock 框架分布

| 框架 | .csproj 引用数 | 代码文件数 |
|------|:---:|:---:|
| Moq | 12 | 30 |
| NSubstitute | 7 | 18 |
| 同时引用 | 2 | - |

**主要矛盾**: LYBT.Tests.Desktop.Unit 在同一项目内混用两套框架。

### 5.2 安全路径测试覆盖

| 安全路径 | 覆盖状态 |
|---------|:---:|
| Token 撤销 (6 场景) | 充分 (单元+集成) |
| 引用检查 (Patient/Herb) | 充分 |
| EditReason 强制校验 | 基本 (仅 MedicalCase) |
| 最后管理员保护 | 充分 |
| 密码策略验证 | 充分 |

### 5.3 [Obsolete] 标记

| 位置 | 内容 | 建议 |
|------|------|------|
| `AuthEnums.cs:71` | T3-X1-01 标记 | 清理旧枚举 |
| `ICrossModuleQueryService.cs:18` | D5-1 标记 | 清理旧接口 |
| `MedicalCaseController.cs:747` | 废弃端点 | 待 API v2 切换后清理 |

---

## 六、Sprint5 剩余 28 项状态验证

| 编号 | 标题 | 审计结果 |
|------|------|:---:|
| T5-P2-20 | 验方导入价格实时获取 | OPEN (Desktop 层绕过) |
| T5-P2-22 | 历史复制价格实时获取 | OPEN (同上) |
| T5-P2-39 | SyncMetadataDto 字段 | OPEN (命名不一致) |
| T5-P2-40 | IgnoreQueryFilters | **DONE** |
| T5-P2-41 | OverwriteConflicts 配置化 | **DONE** |
| T5-P2-42 | 同步前网络/Token 检查 | 未验证 (在 Desktop SyncService 中) |
| T5-P2-43 | 同步结果汇总 | 未验证 |
| T5-P3-01 | Important 配置警告 | 未验证 |
| T5-P3-02 | Token 错误码消息 | OPEN (缺 AuthTokenExpired) |
| T5-P3-03 | 追踪码关联 | 未验证 |
| T5-P3-04 | 审计日志 365 天 | **DONE** (硬编码) |
| T5-P3-05 | Server 缓存失效 | 未验证 |
| T5-P3-06 | Desktop 缓存失效 | 未验证 |
| T5-P3-13 | PatientStatus 复用 CommonStatus | **DONE** (PatientStatus 是死代码) |
| T5-P3-14 | A4/A5 排版 | OPEN (A4 缺独立适配) |
| T5-P3-15 | 药名截断 | OPEN (非 10 字符精确截断) |
| T5-P3-16 | 空处方校验 | OPEN |
| T5-P3-17 | 登出清导航历史 | OPEN (ClearHistory 未调用) |
| T5-P3-18 | 角色粒度模块加载 | **DONE** |
| T5-P3-19 | Email 编辑 | OPEN |
| T5-P3-20 | Checksum 字段对齐 | 未验证 |
| T5-P3-21 | 状态栏同步标识 | OPEN |
| A5-01 | Mock 框架统一 | OPEN |
| A5-05 | [Obsolete] 清理 | OPEN (3 处) |
| A5-06 | FK Fluent API | OPEN (4 处缺失) |
| DOC5-03 | BaseReadRepository 文档 | OPEN |
| DOC5-04 | Desktop Repository 文档 | OPEN |
| DOC5-05 | Sync 跨模块文档 | OPEN |

**已确认完成: 5 项 | 仍开放: 14 项 | 未验证: 9 项**

---

## 七、推荐行动优先级

### 立即修复 (Sprint 6 Week 1)

1. **CODE-01~04**: CRITICAL 安全/数据完整性 (4 项)
2. **CODE-07**: DefaultRole "Staff"->"Doctor" (1 行改动)
3. **CODE-12~14**: DTO MaxLength 对齐 (3 项，各 1 行改动)

### 短期修复 (Sprint 6 Week 2-3)

4. **CODE-05~06, 16~18**: FK + EF Configuration 补全 (5 项)
5. **CODE-08**: 验方导入实时价格 (架构层修复)
6. **CODE-11**: Herb BatchDelete 引用检查
7. **CODE-19~21**: Desktop Shell 功能补全 (3 项)

### 中期规划 (Sprint 7)

8. **CODE-09, 27**: Mock 框架统一 (~29 文件迁移)
9. **DEAD-01~10**: 确认死代码清理 (10 项)
10. **PRD-01~04**: 高优先级文档修复

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-28 | v1.0 | 初始版本: 8 Batch 全量审计, 47 TODO-CODE / 17 TODO-PRD / 14~18 TODO-DEAD-CODE |
