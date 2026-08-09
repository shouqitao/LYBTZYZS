# LYBTZYZS 整体整合方案（SSOT）

> 版本: v1.0 | 日期: 2026-08-08 | 维护者: 技术总监
> 状态: ✅ 定稿（基于 A-30 S0-S3 全部审查报告）
> 依据: `docs/compose/plans/2026-08-08-method-audit-plan.md` + S0-S3 报告
> 用户方针（2026-08-08）：**先收敛再完善**——本方案只做收敛（合并/集中定义），功能完善（B 类）冻结至收敛完成

---

## 一、审查结论摘要（S0-S3 全量）

| 阶段 | 对象 | 结论要点 | 报告 |
|------|------|---------|------|
| S0 | 全仓基线 | **1208 文件 / 1424 类型 / 6118 方法**（公开 3991 / 非公开 2127）；重复聚类 693 组（跨项目 408）；死方法候选 1095（占 17.9%）；Roslyn 校准漏报率 2.9% | `method-audit-baseline-*.md` 等 |
| S1 | Shared 5 项目 | **212 方法分级**（A139/B17/D56，含 4 整类死类 37 方法）；日志/异常集中方案 | `method-audit-shared-*.md` |
| S2 | Server 10 项目 | **330 类型 / 970 方法分级**：29 死方法；跨模块门面双轨；MedicalCase 验证管道丢失；**Herbs+Formula 合并可行（43%）/ Auth+Users 合并可行** | `method-audit-server-*.md` |
| S3 | Desktop 16 项目 | **2987 方法分级**：D 级 78 符号+2 死类；VM 13 组同构命令；**Herbs+Formula 桌面 85% 同构（推荐合并）**；**LocalJwtConfig 策略缺口 P0 确认**；Reports 双轨复刻+策略分歧 | `method-audit-desktop-*.md` |

**全仓方法构成**：6118 = Shared 212 + Server 970 + Desktop 2987 + Tests/Tools ~1949（S0 口径含测试）。

---

## 二、项目合并方案（35 → 30，减 5）【⚠️ 合并决策已延期，见下方注】

> 方法级证据：S2/S3 合并候选分析。**所有合并均为「先文档后代码」（T2），分批次执行，每批独立验收。**
>
> **⚠️ 用户决策（2026-08-08）**：**合并暂缓执行**——待产品功能完善几日后，基于更完整的分析再做决定。当前仅保留合并候选分析（证据在册），不派发合并批次。
>
> **术语澄清（用户纠正，2026-08-08）**：**Formula = 验方（可复用模板）**，**Prescription = 处方（MedicalCase 实例，归 MedicalCase 域）**——两者是不同实体，合并对象为 **Herbs + Formula（主数据/模板域）**，**Prescription 绝不并入**（它是 MedicalCase 域实例数据）。

### 2.1 Server 层（减 2，候选）：Herbs+Formula 合并 + Auth+Users 合并【⏸ 待定】

**合并 1（已完成 ✅，2026-08-09 A-31-C3b）：LYBT.Module.Herbs + LYBT.Module.Formula → LYBT.Module.Catalog**（药材+验方目录域）
- 证据：Server 同构 43%（CRUD/Toggle 5 对逐字相同 + Service 42 行孪生）；Desktop 同构 85%（Editor VM 模板 90%）
- 理由：验证方引用药材链可内部化；消除系统性重复（每次改动两处同步）
- 收益：消除 ~45-50 重复方法；模块数 -1；跨模块引用（Formula→Herbs）内部化；维护成本减半
- 用户决策（2026-08-09）：**从三者关系确认合并合理**——药材是基础主数据，验方是药材的模板组合（同域），处方是实例+收费（MedicalCase 域，不参与合并）；三种处方来源（直接开方/参考验方/复用历史）最终都是 PrescriptionItem 引用药材库，不影响合并
- **边界（用户确认）**：Prescription（处方）归 MedicalCase 域，**不参与**本合并
**合并 2（已完成 ✅，2026-08-09 A-31-C3a）：LYBT.Module.Auth + LYBT.Module.Users → LYBT.Module.Identity**（凭证+用户域）
- 证据：Auth→Users 单向调用（LoginCommandHandler 5 方法）+ Users→Auth 4 个 Handler 撤销回调；编译期与运行时均无环；Identity 已深度集成（ApplicationUser : IdentityUser<Guid>）
- 理由：凭证域高耦合，合并消除两条 CrossModule 通道；以 Identity 为核心 + AuthSession/SecurityAudit 业务增强层
- 收益：跨模块服务对 -2；模块数 -1；双轨漂移消除（Local 登录统一）
- 用户决策（2026-08-09）：**保持 ASP.NET Identity 为核心，不替换框架**；对外接口保留 `IUserService`（替代 `IUserCrossModuleService`）；Desktop 合并 `IApiClientIdentity`【后续批次】；Local 登录统一走共享流程
- **落地**：3 阶段（骨架+实体合并 `d614283bc` / 接口迁移+Command 合并+登录统一 `1164ae0c5` / 外部引用更新+清理 `cd6b80721`），报告 `docs/compose/reports/a31-c3a-auth-users-merge.md`；**Desktop 侧合并（IApiClientAuth+IApiClientUsers→IApiClientIdentity）为后续批次，本批次未动**

### 2.2 Desktop 层（减 3，候选）：Herbs+Formula 合并 + Core 微调【⏸ 待定】

**合并 3（候选，待定）：LYBT.Desktop.Herbs + LYBT.Desktop.Formula → LYBT.Desktop.Catalog**
- 证据：S3 §8 桌面侧 ~85% 同构（Editor VM 模板 90% 4 份拷贝中的 2 份；Repository 同构）
- 与 Server 合并 1 同步执行（均待定）

**合并 4-5（Core 微调，低优先级）**：
- `LYBT.Desktop.Controls` 保持独立（S3 §8：不合并——控件库引用面广）
- `LYBT.Desktop.Printing` 保持独立（QuestPDF 专用职责）
- **结论：Desktop Core 不合并**（方法级证据不支持；Contracts 保持独立）

### 2.3 Shared 层（减 0，但职责集中）

**不合并项目**（S1 §4 结论：Configuration/ExceptionHandling/Logging 职责各异，合并收益低），但**职责集中**（见 §三）。

---

## 三、机制集中定义方案（核心）

### 3.1 日志独立完整项目（专项 A，最高价值）

**目标**：`LYBT.Shared.Logging` 升级为独立完整日志项目，**全程接管 Server+Desktop 日志**，对外只暴露 `AddLybtLogging` 单入口。

**现状证据（S1 §2）**：
- ICorrelationIdProvider **零 DI 注册**——Server 走 `HttpContext.TraceIdentifier`（中间件 + ~9 处直读）、Desktop 走 `Activity` 静态 Lazy → **双机制分叉**
- Serilog 初始化双入口无共用（WebAPI Program.cs 内联 vs Desktop App.xaml.cs）
- 脱敏双实现并存（SensitiveDataMasker vs SensitiveDataJsonConverterFactory）
- MSSQL sink 仅 WebAPI 独有，JSON 与代码配置冲突（autoCreateSqlTable true vs false）

**目标结构**（S1 M1-M10 迁移清单）：
```
LYBT.Shared.Logging/
├── Bootstrap/   # LybtLoggingOptions + LoggingBootstrap + AddLybtLogging 注册
├── Correlation/ # ICorrelationIdProvider（补 DI 注册）+ ActivityCorrelationIdProvider + Enricher
├── Http/        # LoggingHttpHandler + CorrelationIdMiddleware + ApiLoggingFilter（迁移+单点注册）
├── Sinks/       # MssqlSinkConfiguration（Server-only options）
├── Management/  # LoggingLevelManager/DebugModeInfo
├── Masking/     # SensitiveDataMasker/Policy
└── Extensions/  # UseSharedLogging/WithSensitiveDataMasking
```
**迁移**：M1 DesktopSerilogConfiguration / M2 SerilogMSSqlServerExtensions / M3 LoggingRegistrationExtensions / M4 LoggingHttpHandler / M5 CorrelationIdMiddleware / M6 ApiLoggingFilter / M7 Program.cs / M8 App.xaml.cs / M9 SensitiveDataJsonConverterFactory 去重 / M10 Serilog 包 4→1 项目持有

**前置**：Shared.Logging 需补 ASP.NET Core 依赖（`Microsoft.AspNetCore.Http.Abstractions`）——走技术引入治理（更新蓝图 §0.5 + 架构测试 P05b 豁免清单）

### 3.2 异常统一设计（专项 B）

**目标**：`LYBT.Shared.ExceptionHandling` 成为异常**完整职责**项目（层次 + 处理器 + 注册扩展 + 错误码映射）。

**现状证据（S1 §3）**：
- 异常→HTTP 映射**三处独立实现分叉**（子类 GetHttpStatusCode 硬编码常量绕过 ErrorCode 枚举映射，422/429 被吞）
- 处理器分裂：Server System/Business Handler 在 Infrastructure；Desktop DesktopExceptionHandler/ClientErrorMessageMapper 在 Foundation/Infrastructure
- **4 整类死类**：ConflictException/ApiException/UnauthorizedException/ValidationException 生产零构造（37 方法）——删除或恢复（需产品确认是否真不需要）

**统一方案**：
1. `SystemExceptionHandler`/`BusinessExceptionHandler`（192+82 行）→ Shared.ExceptionHandling
2. 注册扩展 `AddLybtExceptionHandling(IServiceCollection)` 由 ExceptionHandling 提供
3. `ErrorCodeExtensions.ToHttpStatusCode` 为唯一映射（合并子类硬编码分支）
4. Desktop `ClientErrorMessageMapper` 复用同一 ErrorCode→消息映射策略（差异化 UI 文案层保留）
5. 4 整类死类：**删除**（生产零构造 + 无产品需求证据）——待用户确认

### 3.3 其他可集中机制

| 机制 | 现状 | 收敛方向 | 证据 |
|------|------|---------|------|
| 配置 Options | WebAPI 自建 JsonOptions 别名（LybtJsonOptions 手动 Bind） | → Shared.Configuration | S2 C1 |
| 错误文案 | 模块 Handler 硬编码（"患者不存在"）vs ErrorMessages.cs 已定义 | 统一 ErrorMessages | S2 C6 |
| 模块 DbContext 引导 | PatientsModule/RegistrationModule 逐行相同（DatabaseOptions+ConnectionStringResolver+UseSqlServer） | 提取 `AddModuleDbContext<TContext>` 扩展 | S2 C7 |
| 仓储镜像方法 | GetByIdIncludingDeletedAsync/GetPagedAsync/ExistsByNameAsync 4 仓储同构 | 模板化收敛（BaseRepository 扩展） | S2 §5 |
| 请求验证管道 | **MedicalCaseInputDtoValidator 注册后 0 注入（验证管道丢失实锤）** | MedicalCase Service 化模块补验证调用 | S2 B7 |
| VM 命令同构 | TestConnectionAsync 95% 同构 / Editor VM 模板 90% 4 份拷贝 | 提取 VM 基类/命令模板 | S3 §2 |
| 映射双机制 | DtoConversionExtensions（手写）vs Mapperly 并存，活链确认 | 按 A-26 P2-11 方向统一（推荐直用 DTO） | S3 §3 |
| 跨模块门面 | ICrossModuleService 统一门面仅覆盖 3/6 域，与 6 域接口并存 | 定方向：删门面 or 扩门面（技术总监决策点） | S2 §3 |

---

## 四、P0 缺陷修复（审查发现，先于收敛执行）

| # | 缺陷 | 证据 | 影响 |
|---|------|------|------|
| P0-1 | **LocalWebAPI 缺 `DoctorOrAdminOrReceptionist` 策略注册** | LocalJwtConfig.cs:70-90 只注册 5 策略；3 控制器引用第 6 个 | 本地模式患者/挂号/报表整体不可用（500） |
| P0-2 | **MedicalCase 验证管道丢失** | MedicalCaseInputDtoValidator 注册后 0 注入点，ValidationBehavior 不生效 | 医案输入无 FluentValidation 校验 |
| P0-3 | **Reports 双轨策略分歧** | Local DoctorOrAdminOrReceptionist vs Remote DoctorOrAdmin | 同端点双端授权面不同 |

---

## 五、执行批次规划（先收敛再完善）

| 批次 | 内容 | 依赖 | 预估 | 类型 |
|------|------|------|------|------|
| **C-0 缺陷修复** | P0-1 策略注册补齐 / P0-2 MedicalCase 验证接入 / P0-3 Reports 策略统一 | 无 | 0.5d | T1 修复 |
| **C-1 日志集中** | 专项 A 全部迁移（M1-M10）| C-0 | 1-2d | T2 收敛 |
| **C-2 异常统一** | 专项 B（处理器收敛 + 死类删除）| C-0 | 1d | T2 收敛 |
| **C-3a Auth+Users→Identity** | Auth+Users Server 端合并（Identity 核心 + 增强层 + Local 登录统一） | ✅ **已完成（2026-08-09 `cd6b80721`）** | 2-3d | T2 收敛 |
| **C-3b Herbs+Formula→Catalog Server** | Herbs+Formula Server 端合并（药材+验方同域） | **✅ 已完成（2026-08-09 5b94893f5，3 阶段独立 commit+push）** | 2-3d | T2 收敛 |
| **C-3c Herbs+Formula→Catalog Desktop** | Herbs+Formula Desktop 同步合并 | **⏸ 随 C-3b** | 1-2d | T2 收敛 |
| **C-4 Desktop 合并** | Desktop Herbs+Formula→Catalog | **⏸ 待定（随 C-3）** | 1-2d | T2 收敛 |
| **C-5 机制收敛** | ErrorMessages / AddModuleDbContext / 仓储镜像模板 / VM 命令模板 / 映射统一 | C-2 | 1-2d | T1 收敛 |
| **C-6 死代码清理** | S1-S3 D 级（14 可安全删 + 37 死类方法 + 29 Server 死方法 + 64 复核项）| 各批后 | 1-2d | T1 清理 |

**执行后**：35 → 30 项目（Server -2 / Desktop -1）【合并批次待定，若暂缓则维持 35】；日志/异常单机制 SSOT，P0 缺陷清零。
**B 类功能**（产品完善）冻结至 C 批次完成（用户方针）；**合并决策**（C-3/C-4）待完善后重新评估。

---

## 六、决策点（待用户拍板）

1. ~~**4 整类死类**（Conflict/Api/Unauthorized/ValidationException）：删除 or 保留？~~ → **C-2 已删**（生产零构造，用户未否决）
2. **跨模块门面**：删 ICrossModuleService 统一门面（只留 6 域接口）or 扩门面覆盖全部 6 域？
3. **~~合并顺序~~**：~~Server 先于 Desktop（推荐）or 同步？~~ → **合并已延期（2026-08-08 用户定），暂不决策**
4. **映射统一方向**：直用 DTO（推荐，A-26 方向）or 补 Model+Mapper？
5. **C 批次全部执行 or 分阶段**？ → **分阶段确认**：C-0 缺陷修复 + C-1/C-2 机制集中先执行；合并批次（C-3/C-4）待定；C-5/C-6/C-7 已完成
6. **~~D72 读卡器 extern 删 or 留？~~** → **保留（2026-08-08 用户确认）**：读卡器是必用硬件功能，HuaDaNativeMethods 13 extern 完整保留

---

## 七、变更记录

| 版本 | 日期 | 变更 |
|------|------|------|
| v0.1 | 2026-08-08 | 草稿框架（待 S0-S3 填充） |
| v1.0 | 2026-08-08 | 定稿：S0-S3 全量结论 + 合并方案（35→30）+ 机制集中方案 + P0 清单 + C 批次规划 |
| v1.1 | 2026-08-08 | **合并决策延期（用户定）**：C-3/C-4 合并批次 ⏸ 待定（完善后重评估）；术语澄清 Formula=验方/Prescription=处方（不同实体，Prescription 归 MedicalCase 域不并入）；决策点更新 |

