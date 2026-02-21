# 全维度架构深化对比报告

> 创建时间: 2026-02-21
> 分析范围: 8 个架构维度 (D1-D8)
> 与已有分析关系: 互补 prd-code-deep-scan-report (功能级) + deviation-triage-checklist (259项分类) + code-fix-backlog (201项修复)
> 本次聚焦: 架构/设计模式是否正确应用、是否一致、是否与文档定义对齐

---

## 综合评分总览

| 维度 | 权重 | 评分 | 等级 | 加权分 | 核心问题 |
|------|------|------|------|--------|----------|
| D1: 架构文档合规性 | 15% | 6.5 | C | 0.975 | Shared层8项目仅文档化3-4个; 空壳模块未标注; 项目总数偏差大 |
| D2: 设计模式一致性 | 15% | 8.0 | B | 1.200 | FormulaService基类不一致; 其余模式高度统一 |
| D3: 数据模型对齐 | 15% | 5.5 | C | 0.825 | 打印字段缺失; Discount精度冲突; 索引筛选条件不符 |
| D4: 错误处理架构 | 10% | 4.5 | D | 0.450 | 基础设施与实际使用严重脱节; 术语违规50+处 |
| D5: 跨模块依赖 | 15% | 8.2 | B+ | 1.230 | 分层清晰无反向依赖; ICrossModuleQueryService解耦范例 |
| D6: 安全架构 | 15% | 7.5 | B | 1.125 | 100%授权覆盖; Token撤销未实现; AdminOnly过度限制 |
| D7: 测试架构 | 10% | 7.0 | B | 0.700 | 架构测试重复; Mock框架混用; 7模块零覆盖 |
| D8: 代码质量 | 5% | 6.5 | C | 0.325 | 术语违规136处; OpenSpec标记1299处; 硬编码连接字符串 |
| **综合** | **100%** | **6.83** | **C** | **6.830** | **架构骨架优秀, 但文档同步和错误处理体系是短板** |

### 评分标准

| 等级 | 分数 | 含义 |
|------|------|------|
| A | 9-10 | 优秀 |
| B | 7-8.9 | 良好 |
| C | 5-6.9 | 需改进 |
| D | 3-4.9 | 较差 |
| F | <3 | 需重建 |

---

## Top 10 关键发现

| 排名 | 发现 | 维度 | 严重度 | 交叉引用 | 影响 |
|------|------|------|--------|----------|------|
| 1 | Service层完全未使用统一异常体系(BusinessException/NotFoundException形同虚设) | D4 | 严重 | [重合: T3-X4-*] | 所有业务异常返回500而非4xx |
| 2 | MedicalCase缺少IsPrinted/PrintVersion字段(文档v1.3已定义) | D3 | 严重 | [重合: T2-X8-02] | 打印保护逻辑无法实现 |
| 3 | Token Family撤销机制未完整实现(6项Tier 1安全漏洞) | D6 | 严重 | [重合: T1-X3-01~06] | 角色变更/密码修改后旧Token不失效 |
| 4 | 术语铁律违规136处(问诊22+病历44+配方44+草药26) | D8 | 高 | [部分重合: X1] | 代码可维护性和专业性受损 |
| 5 | Prescription.Discount精度冲突(Entity=5,4 vs Config=3,2) | D3 | 严重 | [重合: T2-X5-*] | 运行时数据精度丢失 |
| 6 | Shared层8个项目仅文档化3-4个(Primitives/ExceptionHandling/Validators/Configuration缺失) | D1 | 高 | [新增-架构] | 新开发者对项目结构产生误解 |
| 7 | Desktop架构测试规则仅存在于旧项目,主项目缺失 | D7 | 高 | [新增-架构] | Desktop架构退化无门禁 |
| 8 | 3个import-template端点AllowAnonymous | D6 | 中等 | [新增-架构] | 未认证用户可获取系统数据结构 |
| 9 | MedicalCase筛选唯一索引条件不符(文档Draft+Active,代码仅Active) | D3 | 严重 | [新增-架构] | BR-001业务规则保障不完整 |
| 10 | PrescriptionPrintLog未迁移为MedicalCasePrintLog(打印层级不匹配) | D3 | 严重 | [重合: T2-X8-03/12] | 打印日志归属错误 |

---

## 新增 vs 已有发现统计

| 类别 | 数量 | 说明 |
|------|------|------|
| **[新增-架构]** | 44 | 仅本报告首次发现的架构偏差 |
| **[重合: T*-X*-*]** | 18 | 与code-fix-backlog已有项重合 |
| **[根因]** | 5 | 为backlog某项提供根因解释 |
| **[正面]** | 12 | 架构亮点和优秀实践 |

---

## D1: 架构文档 vs 代码合规性 (6.5/10 - C)

### D1.1 项目结构差异

**Server层 (文档: 12个, 实际: 9个活跃 + 2个空壳)**

| 文档定义 | 实际代码 | 差异说明 |
|----------|----------|----------|
| LYBT.Entities | 存在 | 一致 |
| LYBT.Infrastructure | 存在 | 一致 |
| LYBT.Module.Auth | 存在 | 一致 |
| LYBT.Module.Consultation | 仅obj/目录, 无csproj | **空壳模块** |
| LYBT.Module.Formula | 存在 | 一致 |
| LYBT.Module.Herbs | 存在 | 一致 |
| LYBT.Module.MedicalCase | 存在 | 一致 |
| LYBT.Module.Patients | 存在 | 一致 |
| LYBT.Module.Prescriptions | 仅obj/目录, 无csproj | **空壳模块** |
| LYBT.Module.Sync | 存在 | 一致 |
| LYBT.Module.Users | 存在 | 一致 |
| LYBT.WebAPI | 存在 | 一致 |

**Client层 (文档: 18个, 实际: 18个 + 2个文档外)**

| 差异项 | 说明 |
|--------|------|
| **LYBT.Desktop.LocalData** | **文档未记录**, 实际存在 (SQLite本地数据层) |
| **LYBT.Desktop.CardReader** (Core层csproj) | **文档未记录**, Modules下有空壳 |
| LYBT.Desktop.Consultation | **文档声明存在但实际不存在** (功能已合并到MedicalCase) |

**Shared层 (文档: 3-4个, 实际: 8个)**

| 项目 | system-overview.md | shared.md | 实际 |
|------|:--:|:--:|:--:|
| LYBT.Shared.Components | 有 | 有 | 有 |
| LYBT.Shared.Models | 有 | 有 | 有 |
| LYBT.Shared.Utilities | 有 | 有 | 有 |
| LYBT.Shared.Logging | 无 | 有 | 有 |
| **LYBT.Shared.Primitives** | **无** | **无** | **有** |
| **LYBT.Shared.ExceptionHandling** | **无** | **无** | **有** |
| **LYBT.Shared.Validators** | **无** | **无** | **有** |
| **LYBT.Shared.Configuration** | **无** | **无** | **有** |

**工具层**: 实际存在4个工具项目 (ApiTester, LoginTester, PasswordHashGenerator, UserInfoVerifier), 文档均未记录。

**src项目总计**: 文档约33, 实际40+ (含工具和文档未记录的Shared子项目)。

### D1.2 三层配对完整性

| 模块 | Controller | Service | Repository | BaseService继承 | 完整性 |
|------|:--:|:--:|:--:|:--:|--------|
| Auth | AuthController | AuthService等4个 | (用UserRepository) | 否 | 特殊模式, 合理 |
| Users | UsersController | UserService | UserRepository | 是 | **完整** |
| Patients | PatientsController | PatientService | PatientRepository | 是 | **完整** |
| Herbs | HerbsController | HerbService | HerbRepository | 是 | **完整** |
| Formula | FormulasController | FormulaService | FormulaRepository | **否** | **不完整** |
| MedicalCase | MedicalCaseController | Command/Query/State等7个 | MedicalCaseRepository | 是 | **完整(CQRS)** |
| Consultation | 无 | 无 | 无 | - | **空壳(已废弃)** |
| Prescriptions | 无 | 无 | 无 | - | **空壳(已废弃)** |
| Sync | SyncController | SyncService | (直用DbContext) | 否 | 特殊模式, 合理 |

### D1.3 MVVM合规性

| 模块 | XAML视图 | ViewModel | 配对状态 |
|------|----------|-----------|----------|
| Auth | LoginView/LoginWindow (Views/) | LoginViewModel | **完整** |
| Patients | 4个Controls | PatientMasterDetailViewModel | **完整** |
| Herbs | 5个Controls | 3个ViewModel | **完整** |
| Formula | 3个Controls | 2个ViewModel | **完整** |
| Users | 3个Controls | UserMasterDetailViewModel | **完整** |
| MedicalCase | 3个Controls + 3个Dialogs | 4个ViewModel | **完整** |
| Sync | SyncView + SyncConflictDialog | 2个ViewModel | **完整** |

大多数模块使用Controls/目录(而非文档描述的Views/), 页面级View在Roles层(Admin/Clinical), 与ARCH-010"视图分离原则"一致。

### D1.4 双模式实现

| 实体 | IDataSource接口 | Remote实现 | Local实现 | 完整性 |
|------|:--:|:--:|:--:|--------|
| Patient | IPatientDataSource | RemotePatientDataSource | LocalPatientDataSource | **完整** |
| Herb | IHerbDataSource | RemoteHerbDataSource | LocalHerbDataSource | **完整** |
| Formula | IFormulaDataSource | RemoteFormulaDataSource | LocalFormulaDataSource | **完整** |
| User | IUserDataSource | RemoteUserDataSource | LocalUserDataSource | **完整** |
| MedicalCase | IMedicalCaseDataSource | RemoteMedicalCaseDataSource | LocalMedicalCaseDataSource | **完整** |

5/5实体全部有完整的双模式实现, **100%一致**。

### D1 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D1-01 | Consultation/Prescriptions空壳模块仍列在文档模块清单中 | 中 | [新增-架构] |
| D1-02 | system-overview.md项目总数"约33"与实际40+不符 | 中 | [新增-架构] |
| D1-03 | Shared层文档仅记录3-4个项目, 实际8个; Primitives/ExceptionHandling/Validators/Configuration缺失 | 高 | [新增-架构] |
| D1-04 | Desktop.LocalData和Desktop.CardReader(Core层)未在system-overview.md中列出 | 中 | [新增-架构] |
| D1-05 | Desktop端Consultation模块文档声明存在但实际不存在 | 中 | [新增-架构] |
| D1-06 | Desktop模块使用Controls/而非文档标准目录Views/ | 低 | [新增-架构] |
| D1-07 | FormulaService未继承BaseService, 与标准模块不一致 | 中 | [新增-架构] |
| D1-08 | Desktop.CardReader在Modules/下有空壳, Core/下有csproj, 位置混乱 | 低 | [新增-架构] |

---

## D2: 设计模式一致性 (8.0/10 - B)

### D2.1 Server端模式矩阵

#### Repository基类统一性: 5/5 = 100%

| 模块 | Repository | 基类 | 一致 |
|------|-----------|------|:--:|
| Users | UserRepository | BaseRepository\<User\> | 是 |
| Patients | PatientRepository | BaseRepository\<Patient\> | 是 |
| Herbs | HerbRepository | BaseRepository\<Herb\> | 是 |
| Formula | FormulaRepository | BaseRepository\<Formula\> | 是 |
| MedicalCase | MedicalCaseRepository | BaseRepository\<MedicalCase\> | 是 |

#### Service基类统一性: 4/5 = 80%

| 模块 | Service | 基类 | 一致 |
|------|---------|------|:--:|
| Users | UserService | BaseService\<User\> | 是 |
| Patients | PatientService | BaseService\<Patient\> | 是 |
| Herbs | HerbService | BaseService\<Herb\> | 是 |
| **Formula** | **FormulaService** | **IFormulaService(无基类)** | **否** |
| MedicalCase | Command/Query/StateService | BaseService\<MedicalCase\> | 是 |

#### Mapper技术: 5/5 Mapperly = 100%

#### Module注册: 7/7 活跃模块 = 100%

### D2.2 Desktop端模式矩阵

| 模式 | 采用率 | 说明 |
|------|--------|------|
| MasterDetailViewModelBase | 5/5 CRUD模块 = **100%** | Patients/Herbs/Formula/Users/MedicalCase |
| DataSource双模式 | 5/5 实体 = **100%** | 接口+Remote+Local 三件套 |
| IModule注册 | 7/7 活跃模块 = **100%** | Prism Module标准模式 |
| Repository接口分离 | 5/5 = **100%** | IXxxRepository + XxxRepository |

### D2.3 CQRS边界

| 方面 | 评估 |
|------|------|
| MedicalCase CQRS | 7/7服务完全匹配文档: Command/Query/State/Permission/Audit/Rules/Helper |
| 其他模块 | 全部传统单一Service |
| 边界清晰度 | **清晰**, 无混用 |

### D2 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D2-01 | FormulaService未继承BaseService\<T\> | 中 | [新增-架构] |
| D2-02 | BaseReadRepository文档声明存在但代码中从未使用 | 低 | [新增-架构] |
| D2-03 | Validator位置从Module内迁移到Shared.Validators, 文档描述滞后 | 低 | [新增-架构] |
| D2-04 | Desktop端Repository无统一基类(与Server端不同) | 低 | [新增-架构] |
| D2-05 | CQRS边界清晰, 文档与代码7/7服务完全匹配 | 正面 | - |

---

## D3: 数据模型对齐 (5.5/10 - C)

### D3.1 实体覆盖率

| 实体 | data-model.md | Entity.cs | Configuration.cs | 状态 |
|------|:--:|:--:|:--:|------|
| MedicalCase | 有 | MedicalCaseModel.cs | MedicalCaseConfiguration.cs | **有差异** |
| Consultation | 有 | ConsultationModel.cs | ConsultationConfiguration.cs | 对齐 |
| Prescription | 有 | PrescriptionModel.cs | PrescriptionConfiguration.cs | **有差异** |
| PrescriptionItem | 有 | PrescriptionItem.cs | PrescriptionItemConfiguration.cs | 对齐 |
| Patient | 有 | PatientModel.cs | PatientConfiguration.cs | **有差异** |
| User | 有 | UserModel.cs | UserConfiguration.cs | 对齐 |
| Herb | 有 | HerbModel.cs | HerbConfiguration.cs | 轻微差异 |
| Formula | 有 | FormulaModel.cs | FormulaConfiguration.cs | 轻微差异 |
| FormulaHerbItem | 有 | FormulaHerbItem.cs | FormulaHerbItemConfiguration.cs | 对齐 |
| AuthSession | 有 | AuthSessionModel.cs | AuthSessionConfiguration.cs | **有差异** |
| RefreshToken | 有 | RefreshToken.cs | RefreshTokenConfiguration.cs | **有差异** |
| MedicalCaseAuditLog | 有(ER图) | MedicalCaseAuditLog.cs | MedicalCaseAuditLogConfiguration.cs | 对齐 |
| PrescriptionPrintLog | 有(应为MedicalCasePrintLog) | PrescriptionPrintLog.cs | PrescriptionPrintLogConfiguration.cs | **层级不匹配** |
| BlacklistedToken | **无** | BlacklistedToken.cs | 无独立Configuration | **文档缺失** |
| AutoLoginToken | **无** | AutoLoginToken.cs | 无独立Configuration | **文档缺失** |
| SecurityAuditLog | **无** | SecurityAuditLog.cs | SecurityAuditLogConfiguration.cs | **文档缺失** |
| SystemLog | **无** | SystemLog.cs | SystemLogConfiguration.cs | **文档缺失** |

文档定义13个实体, 代码存在17个实体。核心11个全部有代码实现。文档缺失4个辅助实体。

### D3.2 关键字段差异

#### MedicalCase - 严重差异

| 字段 | data-model.md | Entity.cs | 差异 |
|------|:--:|:--:|------|
| **IsPrinted** | bool, Required, default false | **缺失** | **文档有, 代码无** |
| **PrintVersion** | int, Required, default 1 | **缺失** | **文档有, 代码无** |

#### Prescription - 精度冲突

| 字段 | data-model.md | Entity.cs | Configuration | 差异 |
|------|:--:|:--:|:--:|------|
| Discount | decimal(5,4) | Column("decimal(5,4)") | **HasPrecision(3,2)** | **Entity=5,4 vs Config=3,2** |
| PrintVersion | 文档已移除 | int, default 1 仍存在 | 无配置 | 文档移除, 代码保留 |
| IsPrinted | 文档已移除 | bool, default false 仍存在 | 无配置 | 文档移除, 代码保留 |

#### Patient - 代码多出字段

IdType, EmergencyContactName, EmergencyContactPhone, EmergencyContactRelation, DisableReason -- 5个字段未在文档中列出。

#### RefreshToken - 代码多出字段

RevokedReason, RevokedAt, RevokedBy, ClientIp, UserAgent, DeviceId, DeviceName, LastUsedAt, ReplacedByToken, UsedAt -- 10个字段未在文档中列出。

### D3.3 索引差异

| 索引 | 文档定义 | 代码实现 | 差异 |
|------|----------|----------|------|
| MedicalCase筛选唯一索引 | `CaseStatus IN (0,1) AND IsDeleted=0` (Draft+Active) | `CaseStatus = 1 AND IsDeleted = 0` (仅Active) | **筛选条件不同** |
| IX_MedicalCases_UserId | 普通索引 | 缺失 | **代码缺失** |

### D3 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D3-01 | MedicalCase缺少IsPrinted/PrintVersion字段 | 严重 | [重合: T2-X8-02] |
| D3-02 | Prescription.Discount精度冲突: Entity(5,4) vs Configuration(3,2) | 严重 | [重合: T2-X5-*] |
| D3-03 | Prescription保留已从文档移除的IsPrinted/PrintVersion字段 | 中等 | [重合: T2-X8-03] |
| D3-04 | PrescriptionPrintLog未迁移为MedicalCasePrintLog | 严重 | [重合: T2-X8-03/12] |
| D3-05 | MedicalCase筛选唯一索引条件不符(Draft+Active vs Active only) | 严重 | [新增-架构] |
| D3-06 | MedicalCase缺少IX_MedicalCases_UserId索引 | 轻微 | [新增-架构] |
| D3-07 | Patient有5个代码字段未在文档中列出 | 中等 | [新增-架构] |
| D3-08 | RefreshToken有10个代码字段未在文档中列出 | 中等 | [新增-架构] |
| D3-09 | 4个辅助实体未在data-model.md中定义 | 中等 | [新增-架构] |
| D3-10 | 部分外键关系缺少显式Fluent API配置(依赖约定推断) | 轻微 | [新增-架构] |

---

## D4: 错误处理架构 (4.5/10 - D)

### D4.1 ErrorCode枚举分析

| 区间 | 模块 | 数量 | 格式 |
|------|------|------|------|
| 0-12 | 通用(General) | 13 | **整数0-12**(非MCCEE 5位) |
| 10001-10015 | 用户(Users) | 15 | 5位MCCEE |
| 20001-20006 | 患者(Patients) | 6 | 5位MCCEE |
| 30001-30008 | 病例(MedicalCase) | 8 | 5位MCCEE |
| 40001-40007 | 处方(Prescriptions) | 7 | 5位MCCEE |
| 50001-50006 | 草药(Herbs) | 6 | 5位MCCEE |
| 60001-60006 | 配方(Formula) | 6 | 5位MCCEE |
| 70001-70005 | 问诊(Consultation) | 5 | 5位MCCEE |
| 8xxxx | **Sync** | **0** | **完全缺失** |
| **合计** | | **66** | **混合格式** |

### D4.2 异常使用模式分析 -- **三种模式不统一**

| 模式 | 使用位置 | 数量 | 问题 |
|------|---------|------|------|
| `throw new InvalidOperationException(msg)` | MedicalCase Services | ~20+ | 硬编码字符串, 无ErrorCode |
| `Result.Failure("硬编码字符串")` | User/Patient/Formula Service | ~12 | 无ErrorCode |
| `Result.Failure(ErrorCode, msg)` | BaseService | 3 | **推荐模式但仅3处使用** |
| `throw new BusinessException` | **0处使用** | 0 | **定义了但从未使用** |
| `throw new NotFoundException` | **0处使用** | 0 | **定义了但从未使用** |

**核心问题**: BusinessException和NotFoundException有完整的静态工厂方法(如`NotFoundException.MedicalCase(caseId)`), 但在Service层**完全没有被调用**。MedicalCase模块大量使用`InvalidOperationException`, 全部落入SystemExceptionHandler兜底处理, 返回500而非正确的4xx状态码。

### D4.3 ProblemDetails基础设施 -- 完整但未被利用

| 组件 | 状态 | 说明 |
|------|------|------|
| ProblemDetailsConfiguration | 已实现 | RFC 7807标准 |
| BusinessExceptionHandler | 已实现 | 但因Service层不抛BusinessException而形同虚设 |
| SystemExceptionHandler | 已实现(兜底) | 接收了本应由Business Handler处理的异常 |
| CorrelationId全链路 | 已实现 | 中间件->日志->ProblemDetails->响应头->DB存储 |

### D4.4 术语违规 (ErrorCode.cs + ErrorMessages.cs + NotFoundException.cs)

| 违规术语 | 正确术语 | ErrorCode.cs | ErrorMessages.cs | 其他 | 合计 |
|---------|---------|:--:|:--:|:--:|:--:|
| "草药" | 药材(Herb) | 12处 | 2处 | - | 14 |
| "配方" | 验方(Formula) | 8处 | - | - | 8 |
| "问诊" | 诊断(Consultation) | 5处 | 3处 | - | 8 |
| "病历/病例" | 医案(MedicalCase) | 10处 | 6处 | 1处 | 17 |
| **合计** | | **35处** | **11处** | **1处** | **47处** |

### D4 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D4-01 | 通用错误码(0-12)使用简单整数, 非MCCEE 5位编码 | 中等 | [重合: T3-X1-*] |
| D4-02 | MedicalCase模块全部使用InvalidOperationException绕过统一异常体系 | 严重 | [重合: T3-X4-*] |
| D4-03 | BusinessException/NotFoundException已定义完整但从未在Service层使用 | 严重 | [重合: T3-X4-*] [根因: 为X4提供根因] |
| D4-04 | User/Patient/Formula Service使用Result.Failure("硬编码字符串")无ErrorCode | 中等 | [重合: T3-X4-01~04] |
| D4-05 | ErrorCode.cs中35处术语违规 | 中等 | [新增-架构] |
| D4-06 | ErrorMessages.cs中11处术语违规 + NotFoundException.cs 1处 | 中等 | [新增-架构] |
| D4-07 | Sync模块(8xxxx)错误码完全缺失 | 中等 | [重合: T3-X1-13] |
| D4-08 | ProblemDetails基础设施完整但因D4-02/D4-03大量异常落入SystemExceptionHandler | 中等 | [新增-架构] [根因: T3-X4-*的根因] |
| D4-09 | CorrelationId全链路实现完整 | 正面 | - |

---

## D5: 跨模块依赖分析 (8.2/10 - B+)

### D5.1 项目引用方向验证

```
WebAPI (组合根)
  ├-> Module.Auth/Users/Patients/Herbs/Formula/MedicalCase/Sync
  ├-> Shared.Configuration/Models/Utilities/ExceptionHandling

Infrastructure (核心基础设施)
  ├-> Entities
  ├-> Shared.Configuration/Models/Utilities/ExceptionHandling/Logging

Entities (领域实体)
  └-> Shared.Models
```

**反向/循环引用检查结果: 未发现。** 所有引用方向均符合层次结构。

### D5.2 跨模块引用详情

| 来源模块 | 目标模块 | 引用类型 | 评估 |
|---------|---------|---------|------|
| Auth -> Users | IUserService, UserMapper | 接口+Mapper | 合理 (Auth需验证凭证) |
| MedicalCase -> Patients | IPatientService | 接口 | 合理 (医案关联患者) |
| MedicalCase -> Users | IUserService | 接口 | 合理 (医案关联医生) |
| Sync -> Herbs/Patients/Formula | IHerbService等 | 接口 | 合理但较重 (Sync天然跨模块) |
| ~~Formula -> Herbs~~ | 已移除 | 改用ICrossModuleQueryService | **优秀: 已完成解耦** |

### D5.3 ICrossModuleQueryService

**接口位置**: Infrastructure层 (合规)
**返回类型**: DTO投影 (不泄露Entity)
**查询模式**: AsNoTracking() (性能优化)
**使用方**: Formula模块 (FormulaService, FormulaImportExportService)

Formula模块已通过ICrossModuleQueryService解耦了对Herbs模块的直接ProjectReference依赖, 这是优秀的解耦范例。

### D5.4 namespace引用分析

所有跨模块引用仅引用Interface/Mapping, 未引用实现类, 遵循依赖倒置原则(DIP)。

Auth.AuthService引用Users.Mapping是轻微耦合点 -- 理想情况下应通过Users的Service接口获取映射后的DTO。

### D5 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D5-01 | 无反向引用和循环依赖, 分层清晰 | 正面 | [新增-架构] |
| D5-02 | Formula通过ICrossModuleQueryService解耦Herbs依赖 | 正面 | [新增-架构] |
| D5-03 | MedicalCase直接引用Patients+Users(ProjectReference) | 中等 | [新增-架构] |
| D5-04 | Sync模块引用3个其他Module(引用量最大) | 中等 | [新增-架构] |
| D5-05 | Auth.AuthService直接使用Users.Mapping | 低 | [新增-架构] |
| D5-06 | 所有跨模块引用仅依赖接口, 遵循DIP | 正面 | [新增-架构] |

---

## D6: 安全架构 (7.5/10 - B)

### D6.1 Controller授权覆盖率: 9/9 = 100%

| Controller | 类级别授权 | 策略 |
|-----------|-----------|------|
| AuthController | [Authorize] | 默认认证 |
| UsersController | [Authorize(Policy = "AdminOnly")] | SuperAdmin/Admin |
| PatientsController | [Authorize(Policy = "DoctorOrAdmin")] | SuperAdmin/Admin/Doctor |
| HerbsController | [Authorize(Policy = "DoctorOrAdmin")] | SuperAdmin/Admin/Doctor |
| FormulasController | [Authorize(Policy = "DoctorOrAdmin")] | SuperAdmin/Admin/Doctor |
| MedicalCaseController | [Authorize(Policy = "DoctorOrAdmin")] | SuperAdmin/Admin/Doctor |
| SyncController | [Authorize(Policy = "DoctorOrAdmin")] | SuperAdmin/Admin/Doctor |
| HealthController | [Authorize] | 默认认证 |
| DiagnosticsController | [Authorize(Roles = "SuperAdmin")] | 仅SuperAdmin |

### D6.2 AuthorizationHandler覆盖

| 模块 | Handler | 状态 |
|------|---------|------|
| MedicalCase | MedicalCaseAuthorizationHandler | **已实现** (委托PermissionService) |
| Formula | FormulaAuthorizationHandler | **已实现** |
| Patient | 无 | 依赖Policy级别控制 |
| Herb | 无 | 依赖Policy级别控制 |
| User | 无 | AdminOnly策略足够 |

MedicalCase的授权通过PermissionService抽象, 将授权规则与Handler分离, 是优秀设计。

### D6.3 AllowAnonymous清单

| Controller | 端点 | 合理性 |
|-----------|------|--------|
| Auth | POST /login | **合理** |
| Auth | POST /auto-login | **合理** |
| Auth | POST /logout | **需注意** (允许过期Token登出) |
| Auth | POST /refresh | **合理** |
| Health | GET / | **合理** |
| Health | GET /ping | **合理** |
| **Patients** | **GET /import-template** | **可疑** (暴露数据结构) |
| **Herbs** | **GET /import-template** | **可疑** |
| **Formulas** | **GET /import-template** | **可疑** |

### D6.4 敏感数据保护

**Patient敏感字段标记**:

| 字段 | 数据类型 | 脱敏模式 | 标记状态 |
|------|---------|---------|---------|
| IdNumber | IdentityInfo | Partial | 已标记 |
| PhoneNumber | ContactInfo | Partial | 已标记 |
| Address | PersonalInfo | Default | 已标记 |
| AllergyHistory | MedicalInfo | Hash | 已标记 |
| MedicalHistory | MedicalInfo | Hash | 已标记 |
| **EmergencyContactPhone** | - | - | **未标记** |

**User实体缺失标记**:

| 字段 | 应标记类型 | 风险等级 |
|------|-----------|---------|
| **User.PhoneNumber** | ContactInfo | **高** -- 日志中可能泄露 |
| **User.Email** | ContactInfo | **高** -- 日志中可能泄露 |

**脱敏管线完整性**: Entity标记 -> Serilog脱敏 -> API序列化脱敏 -> HTTP传输脱敏 -> URI脱敏, 形成完整保护链路。注意: 基于字段名的启发式脱敏(SensitiveDataMasker)可部分弥补属性标记缺失, 但不应依赖启发式作为主要防线。

### D6 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D6-01 | 所有Controller 100%类级别授权覆盖 | 正面 | [新增-架构] |
| D6-02 | MedicalCase/Formula资源级授权(2/9模块) | 正面 | [新增-架构] |
| D6-03 | UsersController AdminOnly过度限制自助端点 | 高 | [重合: T1-S2-05/06/09] |
| D6-04 | 3个import-template端点AllowAnonymous | 中等 | [新增-架构] |
| D6-05 | Token Family撤销机制未完整实现 | 高 | [重合: T1-X3-01~06] |
| D6-06 | SensitiveDataAttribute重复定义(Entities+Logging) | 低 | [新增-架构] |
| D6-07 | Patient.EmergencyContactPhone未标记SensitiveData | 低 | [新增-架构] |
| D6-08 | **User.PhoneNumber和User.Email未标记SensitiveData** | **高** | [新增-架构] 日志中可能泄露 |
| D6-09 | ExtractUserInfo方法在两个Handler中重复 | 低 | [新增-架构] |
| D6-10 | Patient/Herb缺少资源级AuthorizationHandler | 中等 | [新增-架构] |
| D6-11 | 敏感数据脱敏管线完整 | 正面 | [新增-架构] |
| D6-12 | JWT配置安全性良好, Production强制配置密钥 | 正面 | [新增-架构] |
| D6-13 | **Rate Limiting中间件已注释掉(Login端点有属性但未生效)** | **中等** | [新增-架构] 登录暴力破解风险 |
| D6-14 | FallbackPolicy未设置(为Swagger让步) | 中等 | [新增-架构] |
| D6-15 | 权限矩阵缺少Receptionist角色支持 | 中等 | [重合: T1-S2-01] |

---

## D7: 测试架构评估 (7.0/10 - B)

### D7.1 架构测试规则

**两套架构测试项目** (存在重复):

| 项目 | 位置 | 规则数 | 覆盖 |
|------|------|--------|------|
| LYBT.Tests.Architecture (主) | tests/LYBT.Tests.Architecture/ | 41 | Server端 |
| LYBT.ArchTests (旧) | tests/Architecture/ | 41 | Server + **Desktop**(独有) |

规则分类:

| 类别 | 规则数 | 说明 |
|------|--------|------|
| Server层依赖方向 | 8 | 完整 |
| Desktop层依赖方向 | 5 | **仅在旧项目中** |
| 命名约定 | 8 | 完整 |
| 禁止框架 | 6 | MediatR/Redis/非EF ORM等 |
| API版本控制 | 3 | v1路由等 |
| Record-Only基线 | 3 | 防过度设计 |
| P2架构门禁 | 3 | 防回潮 |
| 聚合根模式 | 2 | MedicalCase聚合根 |
| Desktop控件 | 5 | **仅在旧项目中** |
| **Shared内部依赖** | **0** | **完全缺失** |

### D7.2 测试项目覆盖矩阵

**合计**: 约2409条 [Fact]+[Theory]

| 测试项目 | 测试数 | 主要覆盖 |
|----------|--------|----------|
| LYBT.Tests.Unit | 423 | Entities(207), Utilities(200), Infrastructure(16) |
| LYBT.Tests.Desktop.Unit | 649 | Shell(116), Infrastructure(131), Foundation(120), MedicalCase(74) |
| LYBT.Tests.Architecture | 41 | Server架构规则 |
| LYBT.Tests.Server.Integration | 141 | 7个模块全覆盖 |
| LYBT.Tests.Desktop.Integration | 24 | LocalMode(12), MedicalCase(9) |
| 额外Unit(模块级) | 750 | Server模块(504) + Shared(246) |
| 额外Integration(WebAPI) | 331 | Controller全覆盖(237) + 模块(10) + Desktop(84) |

### D7.3 零覆盖模块

| 模块 | 类型 | 严重度 |
|------|------|--------|
| LYBT.Shared.Components | Shared | 中 |
| LYBT.Shared.Logging | Shared | 中 |
| LYBT.Desktop.Utilities | Desktop Core | 低 |
| LYBT.Desktop.CardReader | Desktop Module | 中 |
| LYBT.Desktop.Sync | Desktop Module | 中 |
| LYBT.Desktop.Admin | Desktop Role | 中 |
| LYBT.Desktop.Clinical | Desktop Role | 中 |

### D7.4 Mock框架混用

| 框架 | 使用次数 | 文件数 | 主要分布 |
|------|----------|--------|----------|
| **Moq** (`Mock<`) | 217 | 33 | UnitTests/Server, Desktop.Unit部分 |
| **NSubstitute** (`Substitute.For`) | 54 | 21 | Tests.Unit, Foundation, Desktop.Integration |

混用两套Mock框架, 甚至有文件同时使用两者。

### D7.5 测试模式

| 模式 | 使用情况 | 评估 |
|------|----------|------|
| AAA模式标记 | 6422处, 161个文件 | **优秀** |
| Fixture/Collection | 71处, 39个文件 | **良好** |
| WebApplicationFactory | 22处, 5个文件 | **良好** |
| Theory/InlineData | 138条, 43个文件 | **良好** |

### D7 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D7-01 | 两套架构测试项目重复(24条规则完全相同) | 中 | [新增-架构] |
| D7-02 | Desktop架构规则仅存在于旧项目, 主项目缺失 | 高 | [新增-架构] |
| D7-03 | Shared内部依赖规则完全缺失 | 中 | [新增-架构] |
| D7-04 | Mock框架混用(Moq 217次 vs NSubstitute 54次) | 中 | [新增-架构] |
| D7-05 | 7个源码模块零测试覆盖 | 高 | [新增-架构] |
| D7-06 | CLAUDE.md列出5个测试项目, 实际有26个csproj | 中 | [新增-架构] |
| D7-07 | AAA模式6422处几乎100%覆盖 | 正面 | - |

---

## D8: 代码质量横切面 (6.5/10 - C)

### D8.1 术语铁律违规

| 违规术语 | 出现次数 | 文件数 | 重灾区 |
|---------|----------|--------|--------|
| "问诊" (应为Consultation/诊断) | 22 | 8 | ErrorCode.cs, ClientErrorMessageMapper.cs |
| "病历/病例" (应为MedicalCase/医案) | 44 | 17 | ErrorMessages.cs, ClientErrorMessageMapper.cs |
| "配方" (应为Formula/验方) | 44 | 9 | ErrorCode.cs, FormulaService.cs, FormulaValidator.cs |
| "草药" (应为Herb/药材) | 26 | 5 | ErrorCode.cs, PrescriptionItemInputDto.cs |
| **合计** | **136** | **39** | |

### D8.2 废弃代码/空壳项目

| 项目/目录 | 状态 |
|-----------|------|
| Server/Modules/LYBT.Module.Consultation/ | 空壳(仅obj目录) |
| Server/Modules/LYBT.Module.Prescriptions/ | 空壳(仅obj目录) |
| Server/Core/LYBT.Server.Interfaces/ | 空目录(0文件) |
| [Obsolete]标记 | 7处 |
| RFC URI重复定义 | 2处(ProblemDetailsConfiguration + ProblemDetailsFactory完全相同的映射) |

### D8.3 TODO/FIXME/OpenSpec

| 标记 | 数量 | 分布 |
|------|------|------|
| TODO | 9 | MedicalCaseQueryService, PatientMasterDetailViewModel等 |
| FIXME | 0 | - |
| HACK | 0 | - |
| **OpenSpec** | **1299处/452文件** | 遍布全项目 (兼容代码标记系统) |

OpenSpec 1299个标记分布在452个.cs文件中, 代表计划中但尚未完成的重构项。

### D8.4 硬编码问题

| 类别 | 严重度 | 位置 |
|------|--------|------|
| 硬编码SQL Server连接字符串(fallback) | 高 | DatabaseServiceCollectionExtensions.cs:121 |
| 硬编码localhost URL | 低 | Tools/目录下的3个工具 |

### D8 发现汇总

| 编号 | 发现 | 严重度 | 交叉引用 |
|------|------|--------|----------|
| D8-01 | 术语铁律违规136处(4类术语, 39文件) | 高 | [部分重合: X1] |
| D8-02 | ErrorCode.cs和ErrorMessages.cs是术语违规重灾区 | 高 | [部分重合: X1] |
| D8-03 | 3个空壳项目/目录未清理 | 低 | [新增-架构] |
| D8-04 | OpenSpec标记1299处/452文件 | 中 | [新增-架构] |
| D8-05 | 硬编码SQL Server连接字符串 | 高 | [新增-架构] |
| D8-06 | RFC URI映射在两个文件中完全重复 | 中 | [新增-架构] |
| D8-07 | [Obsolete]标记7处需清理计划 | 低 | [新增-架构] |

---

## 架构亮点 (正面发现)

| 编号 | 亮点 | 维度 |
|------|------|------|
| P-01 | 双模式(Remote+Local) 5/5实体100%完整 | D1 |
| P-02 | Repository基类100%统一, Mapper(Mapperly)100%统一 | D2 |
| P-03 | MasterDetailViewModelBase 5/5 CRUD模块100%采用 | D2 |
| P-04 | CQRS边界清晰, 文档与代码7/7服务完全匹配 | D2 |
| P-05 | CorrelationId全链路实现(中间件->日志->ProblemDetails->响应头->DB) | D4 |
| P-06 | 无反向引用和循环依赖, 分层清晰 | D5 |
| P-07 | Formula通过ICrossModuleQueryService解耦Herbs依赖 | D5 |
| P-08 | 所有跨模块引用仅依赖接口(DIP) | D5 |
| P-09 | Controller 100%类级别授权覆盖 | D6 |
| P-10 | 敏感数据脱敏管线完整(Entity->日志->API->HTTP) | D6 |
| P-11 | AAA测试模式6422处几乎100%覆盖 | D7 |
| P-12 | JWT配置规范, Production强制配置密钥, Login有Rate Limiting | D6 |

---

## 全量发现交叉引用索引

### 与code-fix-backlog重合项

| 本报告编号 | Backlog任务 | 横切面 | 说明 |
|-----------|-------------|--------|------|
| D3-01 | T2-X8-02 | X8 | MedicalCase IsPrinted/PrintVersion缺失 |
| D3-02 | T2-X5-* | X5 | Discount精度冲突 |
| D3-03 | T2-X8-03 | X8 | Prescription保留已移除字段 |
| D3-04 | T2-X8-03/12 | X8 | PrintLog层级不匹配 |
| D4-01 | T3-X1-* | X1 | 通用错误码格式不统一 |
| D4-02 | T3-X4-* | X4 | MedicalCase使用InvalidOperationException |
| D4-03 | T3-X4-* | X4 | BusinessException/NotFoundException未使用 |
| D4-04 | T3-X4-01~04 | X4 | Result.Failure无ErrorCode |
| D4-07 | T3-X1-13 | X1 | Sync错误码缺失 |
| D6-03 | T1-S2-05/06/09 | S2 | AdminOnly过度限制 |
| D6-05 | T1-X3-01~06 | X3 | Token Family撤销未实现 |
| D6-14 | T1-S2-01 | S2 | Receptionist角色缺失 |
| D8-01/02 | X1(部分) | X1 | 术语违规(错误码部分) |

### 根因分析

| 本报告编号 | 为Backlog提供根因 | 说明 |
|-----------|------------------|------|
| D4-03 | T3-X4-* | BusinessException/NotFoundException未使用是X4所有任务的根因 |
| D4-08 | T3-X4-* | ProblemDetails基础设施未被利用是错误处理体系失效的根因 |
| D2-01 | D4-04 | FormulaService未继承BaseService导致缺少统一错误处理 |
| D7-02 | D1-06 | Desktop架构规则缺失导致Controls/目录模式无门禁保障 |
| D3-05 | D3-01 | 索引条件不符是IsPrinted字段缺失的下游影响 |

### 新增架构偏差 (非code-fix-backlog已有)

| 编号 | 发现 | 建议优先级 | 建议加入Sprint |
|------|------|-----------|---------------|
| D1-03 | Shared层8个项目仅文档化3-4个 | 中 | Sprint 3 (文档) |
| D3-05 | MedicalCase筛选唯一索引条件不符 | 高 | Sprint 2 (数据) |
| D3-07 | Patient 5个字段未文档化 | 低 | Sprint 5 (文档) |
| D3-08 | RefreshToken 10个字段未文档化 | 低 | Sprint 5 (文档) |
| D3-09 | 4个辅助实体未在data-model.md定义 | 中 | Sprint 3 (文档) |
| D4-05/06 | 术语违规47处(ErrorCode/ErrorMessages/NotFoundException) | 中 | Sprint 3 (X1一并处理) |
| D5-05 | Auth.AuthService直接使用Users.Mapping | 低 | Sprint 5 |
| D6-04 | 3个import-template端点AllowAnonymous | 中 | Sprint 2 (安全) |
| D6-07 | EmergencyContactPhone未标记SensitiveData | 低 | Sprint 4 |
| D6-08 | User.PhoneNumber/Email未标记SensitiveData | 高 | Sprint 2 (安全) |
| D6-10 | Patient/Herb缺少资源级AuthorizationHandler | 中 | Sprint 4 |
| D6-13 | Rate Limiting中间件已注释(Login端点属性未生效) | 中 | Sprint 2 (安全) |
| D6-13 | FallbackPolicy未设置 | 中 | Sprint 3 |
| D7-01 | 两套架构测试项目重复 | 中 | Sprint 3 (合并) |
| D7-02 | Desktop架构规则仅在旧项目 | 高 | Sprint 2 (迁移) |
| D7-03 | Shared内部依赖规则缺失 | 中 | Sprint 3 |
| D7-04 | Mock框架混用(Moq vs NSubstitute) | 中 | Sprint 5 (统一) |
| D7-05 | 7个源码模块零测试覆盖 | 高 | Sprint 3-4 (逐步补齐) |
| D8-01 | 术语违规136处(全范围) | 高 | Sprint 3 (系统清理) |
| D8-04 | OpenSpec标记1299处/452文件 | 中 | 持续跟踪 |
| D8-05 | 硬编码SQL Server连接字符串 | 高 | Sprint 1 (安全) |
| D8-06 | RFC URI映射重复定义 | 中 | Sprint 4 (DRY) |

---

## 演进建议

### 短期 (Sprint 1-2): 安全与数据修复

1. **移除硬编码连接字符串** (D8-05) -- 替换为配置文件/环境变量
2. **修复MedicalCase筛选索引条件** (D3-05) -- 对齐文档Draft+Active
3. **迁移Desktop架构规则到主项目** (D7-02) -- 合并去重后统一到LYBT.Tests.Architecture
4. **修复import-template端点授权** (D6-04) -- 改为[Authorize]
5. **补齐Token Family撤销** (D6-05) -- backlog T1-X3已计划
6. **User实体添加SensitiveData标记** (D6-08) -- PhoneNumber/Email标记为ContactInfo
7. **启用Rate Limiting中间件** (D6-13) -- 取消注释, 至少保护Login端点

### 中期 (Sprint 3-4): 体系统一与覆盖补齐

6. **统一异常体系** -- Service层全面采用BusinessException/NotFoundException替代InvalidOperationException和Result.Failure("字符串") (D4-02/03/04, 是backlog X4的根因修复)
7. **术语系统清理** (D8-01, D4-05/06) -- ErrorCode/ErrorMessages/NotFoundException/FormulaValidator/PrescriptionItemInputDto等39个文件
8. **文档同步** (D1-03, D3-07/08/09) -- Shared层项目列表、Patient/RefreshToken字段、辅助实体
9. **测试覆盖补齐** (D7-05) -- 优先Shared.Logging(脱敏管线)、Desktop.Sync
10. **添加Shared内部依赖规则** (D7-03)
11. **Mock框架统一** (D7-04) -- 统一为NSubstitute或Moq之一

### 长期 (Sprint 5+): 优化与清理

12. **FormulaService补齐BaseService继承** (D2-01) -- 享受统一错误处理
13. **MedicalCase/Sync跨模块引用优化** (D5-03/04) -- 评估ICrossModuleQueryService扩展
14. **OpenSpec标记跟踪清理** (D8-04) -- 建立定期清理机制
15. **空壳模块/目录清理** (D8-03) -- Consultation/Prescriptions/Server.Interfaces
16. **架构测试项目合并** (D7-01) -- 消除26个测试csproj中的冗余

---

## 附录: 关键文件路径索引

| 文件 | 关联维度 |
|------|---------|
| `docs/03-architecture/system-overview.md` | D1 |
| `docs/03-architecture/server.md` | D1, D2 |
| `docs/03-architecture/desktop.md` | D1, D2 |
| `docs/03-architecture/data-model.md` | D3 |
| `docs/03-architecture/shared.md` | D1 |
| `docs/03-architecture/dual-mode.md` | D1 |
| `src/Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorCode.cs` | D4, D8 |
| `src/Shared/LYBT.Shared.Primitives/ErrorCodes/ErrorMessages.cs` | D4, D8 |
| `src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/BusinessException.cs` | D4 |
| `src/Shared/LYBT.Shared.ExceptionHandling/Exceptions/Business/NotFoundException.cs` | D4 |
| `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/BusinessExceptionHandler.cs` | D4 |
| `src/Server/Core/LYBT.Infrastructure/Services/ICrossModuleQueryService.cs` | D5 |
| `src/Server/Services/LYBT.WebAPI/Authorization/MedicalCaseAuthorizationHandler.cs` | D6 |
| `src/Server/Services/LYBT.WebAPI/Authorization/FormulaAuthorizationHandler.cs` | D6 |
| `src/Server/Core/LYBT.Infrastructure/Data/Configurations/MedicalCaseConfiguration.cs` | D3 |
| `src/Server/Core/LYBT.Infrastructure/Data/Configurations/PrescriptionConfiguration.cs` | D3 |
| `src/Server/Core/LYBT.Entities/MedicalCases/MedicalCaseModel.cs` | D3 |
| `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs` | D3 |
| `src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs` | D8 |
| `tests/LYBT.Tests.Architecture/ArchTests.cs` | D7 |
| `tests/Architecture/DesktopLayerArchTests.cs` | D7 |
| `docs/plans/2026-02-21-code-fix-backlog.md` | 交叉引用 |
