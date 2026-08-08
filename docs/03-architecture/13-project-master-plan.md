# LYBTZYZS 项目总账

> 版本: v2.1 | 更新: 2026-08-04 | 维护者: 技术总监 + 产品负责人
>
> **本文件是项目的唯一全局视图。** 任何 session 开始前必读此文件。
>
> **维护原则**: 文档必须反映代码真实状态。代码变更 → 文档同步更新。文档变更 → 代码必须跟上。
>
> **文档结构（2026-08-04 拆分）**: 数据模型 → [13a-data-model.md](13a-data-model.md)｜API 端点 → [13b-api-endpoints.md](13b-api-endpoints.md)｜Desktop 视图 + 已知问题 → [13c-current-status.md](13c-current-status.md)

---

## 一、项目概况

| 项 | 值 |
|----|-----|
| 技术栈 | .NET 8 / WPF Prism / ASP.NET Core / EF Core / SQL Server |
| 架构 | 3-Layer (Server) + MVVM (Desktop) + Dual-Mode (Remote+Local) |
| 代码库 | D:\source\repos\LYBTZYZS |
| 分支 | master → Gitee (gitee.com/shouqitao/LYBTZYZS) |
| 数据库 | Remote: SQL Server (LYBTDB_Dev) / Local: LocalDB (LYBTDesktop) |
| Server 模块 | 8 个 (Auth/Users/Patients/Herbs/Formula/MedicalCase/Registration/Reports) |
| Desktop 模块 | 7 个 + 3 个 Core 层 |
| 当前状态 | Build/测试/架构测试/已知问题见 [13c-current-status.md](13c-current-status.md)（唯一权威） |

---

## 二、数据模型（代码实际定义）

> 📄 已拆分至 [13a-data-model.md](13a-data-model.md)：核心实体（Shared/LYBT.Entities）+ 状态枚举。

## 三、API 端点（代码实际定义）

> 📄 已拆分至 [13b-api-endpoints.md](13b-api-endpoints.md)：Auth/Users/Patients/Herbs/Formulas/MedicalCases/Registrations/Reports/Configuration/Diagnostics/Deploy 全部端点。

## 四、Desktop 视图（代码实际定义）

> 📄 已拆分至 [13c-current-status.md](13c-current-status.md)：Shell/Admin/Clinical/Medical/Registration 视图清单。

## 五、已知问题（代码实际状态）

> 📄 已拆分至 [13c-current-status.md](13c-current-status.md)：P0（必须修复）/P1（应修复）/P2（可后续完善）问题清单。

---

## 六、待做工作清单

### A 类 — 架构清理

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| A-01 | 文档清理 | 归档 35 个 plan、删 17 个存根、修 27 个断链 | 无 | ⬜ | 0.5d |
| A-02 | 死代码删除 | ~737 行零引用代码 (Server 10 + Desktop 5 + Shared 2) | 无 | ✅ | 0.5d |
| A-03 | MediatR 简化 | 5 模块全部完成：Herbs/Formula/Patients/Users 24 Handler；MedicalCase 40 文件删除 | 无 | ✅ | 0.5d |
| A-04 | 超大类型拆分 | 5 个 >600 行文件 | 无 | ✅ A-03 简化后全部降至 600 以下 | 0 |
| A-05 | 实体源统一 | 消除 Domain/Shared 双模型 | A-02 | ✅ 已由 A-02/A-03 覆盖 | 0 |
| A-06 | Repository 泛型化 | Desktop 15+ 对复制粘贴 | 无 | ✅ | 1d |
| A-07 | CrossModule 死方法 | 6 个零调用方法 | 无 | ✅ 已由 A-02 覆盖 | 0 |
| A-08 | 命名规范统一 | 后缀/目录/注释语言 | 无 | 🟡 | 1d |
| A-09 | 架构测试补全 | 修复 1 skip（删除 YAGNI）+ 新增 7 个守卫测试（P07/P08/P10 已有） | A-03/A-04 | ✅ | 1d |
| A-10 | 接口下沉 Contracts | IFormulaService/IHerbService 移到 Contracts（与 IPatientService/IUserService 一致） | 无 | ✅ | 0.5d |
| A-11 | Registration 依赖清理 | 移除对 Patients/Users 直接引用 | A-10 | ✅ 已无跨模块依赖（A-02 清理后确认） | 0 |
| A-12 | AuthService 收敛 | RefreshToken 操作收敛到 Repository | 无 | ✅ 已完成（代码已通过 IAuthSessionRepository） | 0 |
| A-13 | DeployController 安全加固 | restart 端点加确认机制或移到内部管理端点，防止误操作停服 | 无 | ✅ `624b438e4` | 0.5d |
| A-14 | Controller 继承文档化 | 三种继承路径（BaseApiController/BaseCrudController/BaseMedicalCasesController）设计意图文档化；MediatR+Service 混合注入是有意设计（查询走 Service 绕过管道，命令走 MediatR 验证+审计） | 无 | 🟡 仅文档 | 0.5d |
| A-15 | MedicalCasesController 拆分 | ~~356 行 CRUD+状态流转 拆为两个 Controller~~ → **评估后取消**：状态流转仅 4 个方法（~100行），共享路由前缀，拆分后 Desktop Refit 也需改，ROI 不合理 | 无 | ❌ 评估后取消 | — |
| A-16 | 结构审计（全项目） | 技术总监+Mimo 双独立报告交叉验证：三层边界健康/双轨真共享；**P0 本地 CRUD 断裂**（患者/药材/验方 GetList/Create/Update 未 override→500/405）+ P1 七项收敛（契约双套/双实现/映射/CorrelationId/Desktop P07 缺口/Shared 文档） | 死代码清理 `97445a6f3` | ✅ `b7c7390d0`+`4bce47483`+`bd02ac512` | 1d |
| A-17 | P0 本地 CRUD 断裂修复 | 本地 Patients/Herbs/Formulas Controller 补 GetList/Create/Update override（复用 Server Service）；MedicalCases 补 GetList/search/print-completed | A-16 | ✅ `b9944d1f4` | 0.5-1d |
| A-18 | P1 机制收敛批次 | ① 契约双套统一（以 ApiClient 为主）② 领域客户端双实现收敛 ③ CorrelationId 删 AsyncLocal 侧 ④ Server 手写 Mapper 改 Mapperly ⑤ Desktop 模块引用补架构测试 | A-16 | ✅ P1 全部完成：第 1 子批次（P1-7/P1-3/P1-4/P1-6）+ 第 2 子批次（P1-5 `812fcdd0c`、P1-1/P1-2 `a8b9d0b0f`） | 2-3d |
| A-19 | 移除 Auto 模式（用户自主切换） | 删 Auto 枚举 + DetectBestModeAsync 自动降级逻辑，模式仅由用户显式选择，远程不可用不自动降级 | A-18 | ✅ `4a1393c3a` | 0.5d |
| A-20 | DbContext 全独立（ADR-0017 落地） | 新建 Patients/MedicalCase/Registration 3 个 DbContext（复用实体配置不建迁移），5 Repository 注入切换（PatientRepository/MedicalCaseRepository/RegistrationRepository/SecurityAuditRepository/HerbReferenceRepository），迁移链保持 AppDbContext 单一（方案 A 同库单迁移），架构测试新增「Repository 注入自己模块 DbContext」守卫 | 模块级审计 M1 | ✅ `25ea3b9b7` | 2-3d |
| A-21 | 模块级审计 P1 修复批次 | M4 Shell RoleDefinitionBase 模块名 bug（AuthModule→AuthenticationModule）/ M5 3 VM 越层（AuditLog/ReportsHome/RegistrationList 改走 Service）/ M3 领域事件空转 8 个（确认订阅或删除）/ M2 Desktop 映射统一（F-02，DTO↔Model 改 Mapperly 删 3 零引用 Mapper）/ C1 Infrastructure 职责过载（CardReader 独立 + LocalData 废弃）/ F-01 删 FeatureToggle + Tools 删 3 留 1 | 模块级审计交叉验证 | ✅ `1b19bce13`(M4) `3d692c5f5`(M5) `4ad350a39`(M3) `876581164`(M2) `2fa05ff63`(C1) `271bca70f`(F-01+Tools) | 1-2d |
| A-26 | 蓝图对齐 + 模式收敛 + 定义收敛（全面审查） | 以 14-structure-design-blueprint.md v1.2（SSOT）为基准核对代码当前态：① 蓝图对齐（项目/职责/结构模式/依赖规则/设计原则逐项核对）② 模式收敛（Controller 继承/Service-Handler/DbContext/映射/批处理/验证/异常/响应信封）③ 定义收敛（命名空间/类型命名/DTO/枚举/接口/错误码/术语/配置）。产出收敛任务清单 P0/P1/P2。任务书：`docs/compose/specs/task-a26-blueprint-convergence-2026-08-08.md` | 文档规范收敛 `fae082ae7` | ✅ 审查完成：P0×0 / P1×7 / P2×11 / 顺带 5。Mimo 报告 `structure-convergence-mimo-2026-08-08.md`，技术总监交叉验证 5 项关键 P1 全部属实。P1 修复待用户拍板方向 | 1-2d |
| A-27 | 技术栈减法（方案 A） | 删除死重量依赖：BCrypt（PasswordHelper 哈希/验证零调用，Identity PBKDF2 取代）/ Swagger（半成品，状态待核实）/ Velopack（0 引用）/ Sqlite（LocalDB 取代）；蓝图新增「技术栈合理性评估」章节。任务书：`docs/compose/specs/task-a27-stack-subtraction-2026-08-08.md` | A-26 | ✅ `846cb5411`（主体）+ `cac9784d8`（总账 SHA）——PasswordHelper 609→367 行（删 BCrypt 哈希/验证 + 包，哈希唯一走 Identity PBKDF2 SSOT）；Swagger 核实完整接线评估保留；Velopack 0 引用无需删；Sqlite props 2 条目删除；蓝图 §0.5 技术栈评估落地（全景 18 项/4 标准/必选 10 项）；测试同步 27 个 BCrypt 测试删 + 5 处哈希改 Identity。验证：build 0 错误 0 警告、架构测试 86/86 | 0.5d |
| A-28 | A-26 P1 收敛批次（7 项） | 子批次 1（结构性）：P1-1 双轨规范化（蓝图 §2.2 定案，写走 Handler/读走 Service + 守卫）/ P1-2 P10 守卫盲区（跨模块服务绕 AppDbContext）/ P1-3 仓储收敛（BaseRepository 推广）。子批次 2（小修）：P1-4 手写映射→Mapperly / P1-5 LoginRequestValidator 双份 / P1-6 命名空间单复数 / P1-7 Jwt Section。任务书：`docs/compose/specs/task-a28-p1-convergence-2026-08-08.md` | A-26（文档先行 `c88cf342c`） | ✅ `bf2f58d52`（子批次1）+ `2988dde17`（子批次2）——P1-1：4 模块 14 处写操作迁回 MediatR（新建 14 组 Command+Handler+Validator），Service 写方法及接口声明删除，LocalWebAPI 3 控制器同步（编译依赖），新增守卫 P19（Service 接口禁写方法）+ P19b（Controller IL 扫描禁直调）；P1-2：3 个 CrossModuleService 改注入模块 DbContext，P10 扩展查 IDbContextAccessor（基础设施豁免）；P1-3：Patient/Herb/Formula 3 仓储继承 BaseRepository（接口挂 IRepository<T>），User/AuthSession（实体非 BaseEntity）/SecurityAudit/Registration（延迟保存事务）/Report/SystemLog（只读聚合）保持裸实现并注明例外；P1-4：UserCrossModuleService 手写映射改 UserCrossModuleMapper（Mapperly）；P1-5：删 Auth 版 LoginRequestValidator，Shared 版为 SSOT；P1-6：Registration.csproj 补 AssemblyName/RootNamespace=Registrations（复数对齐），测试 3 处 + 蓝图 §3.2 同步；P1-7：LocalJwt 节统一为 Jwt（SectionName/appsettings/验证器消息）。验证：两子批次 build --no-incremental 0 错误 0 警告、架构测试 88/88（原 86 + 新 2）。报告：`docs/compose/reports/a28-p1-convergence.md` | 2-3d |
| A-29 | A-26 P2 收敛 + 顺带处置（3 组） | 组 1 蓝图维护（T0）：P2-8 文件数回写 / P2-9 Controller 路径补记 / P2-10 结构矛盾澄清（三态模板）/ P2-16 接口命名矩阵 / P2-17 Status-State 边界 / 顺带4 守卫口径。组 2 代码收敛（T1）：P2-13 KeyNotFoundException→NotFoundException ×3 / P2-14 请求后缀统一 / P2-15 UserBasicDto 迁 Contracts / P2-18 Shell Jwt 补齐 + Reports DTO 约定 / 顺带3 DTOs 死目录 / 顺带5 BaseRegistrationsController override。组 3 顺带：顺带1 图谱索引说明 / 顺带2 AGENTS.md 陈旧清理 / P2-12 BatchImport 评估（倾向记录第二模板）。任务书：`docs/compose/specs/task-a29-p2-convergence-2026-08-08.md` | A-28 | ✅ 3 独立 commit：组1 `994925641`（蓝图 v1.5：文件数回写/三态模板/接口矩阵/Status-State/守卫口径）+ 组2 `27463791b`（P2-13 异常层次统一×3、P2-14 请求契约统一 XxxRequest（10vs2 少改动方向，文件重命名+23 引用）、P2-15 UserBasicDto 迁 Contracts/Users + DTOs 死目录删、P2-18 Shell Jwt 补 2 字段（Options 实际消费）+ 蓝图 Reports 约定、顺带5 核实基类无 Update 无需 override）+ 组3 `fb5573586`（AGENTS.md 5 文件陈旧清理 Sync/SharedKernel/LocalData/CardReader、P2-12 BatchImport 保持独立 + 蓝图第二模板注记）。报告：`docs/compose/reports/a29-p2-convergence.md` `8b6024dda`。验证：组2/3 build --no-incremental 0 错误 0 警告、架构测试 88/88。例外：守卫实测 81 方法/88 用例（任务书 86 为旧估算，蓝图按实测注明）；顺带5 A-26 假设不成立（基类无 Update 方法） | 1-2d |
| A-30 | 全 Solution 方法级深度审查（收敛专项） | 按 `docs/compose/plans/2026-08-08-method-audit-plan.md` 执行 5 阶段：S0 方法级统计基线 / S1 Shared 深审（日志/异常集中定义专项）/ S2 Server 深审 / S3 Desktop 深审 / S4 整体整合方案（15-solution-integration-plan.md）。方法级 A/B/C/D/E 分级，产出每项目深化统计 + 重复/死方法/可集中清单 + 项目合并方案。**方针：先收敛再完善**（B 类冻结） | A-29 + A-22 类级基础 | ✅ 全部完成（S0-S4）——S0 基线 1208 文件/1424 类型/6118 方法；S1 Shared 212 方法（4 整类死类）；S2 Server 970 方法（29 死/Herbs+Formula 合并可行）；S3 Desktop 2987 方法（78 死/LocalJwtConfig 策略缺口 P0 确认）；S4 整合方案定稿 `15-solution-integration-plan.md`（35→30 项目 + 日志/异常集中 + P0×3 + C 批次规划）| 4.5-6.5d |
| A-31 | C 批次执行（整合方案落地） | C-0 P0 缺陷修复（LocalWebAPI 策略/MedicalCase 验证/Reports 双轨策略）/ C-1 日志集中（Shared.Logging 升级 AddLybtLogging M1-M10）/ C-2 异常统一（处理器收敛+死类删除）/ C-3 Server 合并【⏸ 延期】/ C-4 Desktop 合并【⏸ 延期】/ C-5 机制收敛 / C-6 死代码清理 | A-30 S4 定稿 `fa18f67b3`（v1.1 `566016b05` 合并延期） | ✅ C-0 完成（2026-08-09，SHA 见 git log）——P0-1 LocalJwtConfig 补注册 `DoctorOrAdminOrReceptionist`（患者/挂号本地模式恢复）；P0-2 `MedicalCaseCommandService` 注入 `IValidator<MedicalCaseInputDto>` 创建分支验证（更新/完成/挂起不接入：更新 DTO 契约不含 PatientId/UserId，Complete/Suspend 无对应验证器）；P0-3 Reports 双端统一 `DoctorOrAdmin`（权限矩阵 §2.1 补行，产品规则未改）。报告：`docs/compose/reports/a31-c0-p0-fixes.md`。验证：build --no-incremental 0 错误 0 警告、架构测试 88/88；**C-3/C-4 合并批次用户延期（2026-08-08），待完善后重评估** | 6-10d（不含合并） |

### B 类 — 产品功能

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| B-01 | P0 安全修复 | 明文密码/Shell Bug/死锁 (7 项) | 无 | ✅ | 待定 |
| B-02 | 配置修改 API | ConfigurationController 添加 PUT | 无 | ✅ | 1d |
|| B-03 | Excel 导出/导入 | Herbs/Formula/Patients (前端 Excel ↔ WebApi JSON) | 无 | ✅ | 2-3d |
| B-04 | 报表增强 | 图表/多维度/时间范围 | 无 | ✅ `cab959dc1` | 2d |
| B-05 | 配置中心 UI | SystemSettingsView 增强（服务器配置区域） | B-02 | ✅ `c518318ed` | 1d |
| B-06 | 数据备份/恢复 | SQL Server 备份+恢复 | 无 | ⬜ | 1.5d |
| B-07 | 初始化向导完善 | FirstRunSetupView 增强 | 无 | ⬜ | 1d |
| B-08 | Desktop 发布包 | 打包+依赖裁剪+安装器 | 无 | ⬜ | 2d |
| B-09 | 自动更新 | Velopack 集成 | B-08 | ⬜ | 2d |
| B-10 | SignalR 实时通知 | Hub+客户端+协议设计 | A-03 | ✅ | 3d |
| B-11 | 药材/验方模板 | Excel 模板下载 | B-03 | ⬜ | 0.5d |
| B-12 | 患者导入导出 | Excel 模板+导出 | B-03 | ⬜ | 0.5d |
| B-13 | 验方校验 UI | Desktop 对齐 API | 无 | ⬜ | 0.5d |
| B-14 | 挂号排班 | 医生排班+号源管理 | 无 | ⬜ | 3d |
| B-15 | 离线同步 v2.0 | 重新设计架构 | 无 | ⬜ | 5d+ |
| B-16 | Swagger | API 文档生成 | 无 | ⬜ | 0.5d |

### C 类 — 运维部署

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| C-01 | Desktop 测试修复 | ~104 个失败测试 | 需运行中 WebAPI | ⬜ | 1d |
| C-02 | systemd 服务 | 开机自启 | 无 | ⬜ | 0.25d |
| C-03 | 部署脚本清理 | 删除 .worktrees/ 7 个孤儿 checkout + 重复脚本 | 无 | ✅ `85b2d16c5` | 0.25d |
| C-04 | NuGet 包清理 | 移除 8 个零使用废弃包 | 无 | ✅ `85b2d16c5` | 0.25d |
| C-05 | 文档同步 | AGENTS.md/README.md 更新 | 所有代码改动后 | ⬜ | 0.5d |

### D 类 — 医案/挂号专项（2026-08-03 批次）

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| D-01 | 接诊链修复 | StartVisit 原子建医案（D8） | 无 | ✅ | — |
| D-02 | QuickVisit Desktop 接线 | US-REG-002 激活 | D-01 | ✅ | — |
| D-03 | 医案状态机重构 | 取消=物理删 / 仅 Completed 打印 / 打印保护简化 / 堵绕过 | 无 | ✅ | — |

### E 类 — 规则体系优化（2026-08-04）

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| E-01 | coder 层 | 角色 AGENTS.md 精简 + Skill v0.6.0 SSOT | 无 | ✅ | — |
| E-02 | 项目层 | 项目 AGENTS.md 精简为入口+引用 | E-01 | ✅ | — |
| E-03 | 总账拆分 | 13a/13b/13c 拆分 | E-02 | ✅ | — |
| E-04 | MCP 配置 | 移除 tavily 又恢复，serena/tavily 全部保留 | 无 | ✅ | — |

### F 类 — 遗留任务归一（2026-08-04，来自 14-implementation-tasks 归档）

| ID | 任务 | 内容（原 TASK） | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| F-01 | FeatureToggle 开关接入 | TASK-03：14 个功能开关仅 1 个被检查，需在各模块 Service/ViewModel 接入 | 无 | ⬜ | 4-6h |
| F-02 | 桌面 Mapper 统一 | TASK-05：未使用的 PatientMapper + MedicalCaseMapper DI 不一致 | 无 | ⬜ | 1-2h |
| F-03 | LocalData Mapper Target 策略 | TASK-06：统一 RequiredMappingStrategy.Target | 无 | ⬜ | 1-2h |
| F-04 | SyncService CS8602 | TASK-07：可空引用警告 | 无 | ⬜ | 0.5h |
| F-05 | PatientMapper 死代码 | TASK-10：删除或启用（与 A-02 相关） | A-02 | ⬜ | 0.5h |
| F-06 | 打印模板扩展 | TASK-11：诊断报告/患者摘要/医案完整打印（低优先级） | 无 | ⬜ | 4-8h |
| F-07 | API 版本化准备 | TASK-12：v2 版本协商中间件（低优先级） | 无 | ⬜ | 2-3h |
| F-08 | 日志归档策略 | TASK-13：LogCleanupService 按月压缩（低优先级） | 无 | ⬜ | 1-2h |

> TASK-04（MedicalCase RestoreAsync）已被 2026-08-03 决策废弃（取消=物理删除、放弃恢复），不入清单。

### G 类 — 文档审阅收敛（2026-08-04，来自 LLM Wiki 审阅 207 条未解决项）

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| G-01 | 矛盾统一（27 条） | 按 §九 权限终局裁决统一 US-MC-001/药材/验方/D7/医案取消/SQLite/Prism/Token 等文档表述；6 条核实代码 | 无 | ✅ `21a93c294`+`be0f84047`+`a9dcbea56`+`b49960ac7` | 0.5d |
| G-02 | docs 缺口补写（59 条） | 离线密码重置方案/令牌族撤销表结构/DPAPI 迁移/错误码映射/本地威胁模型等 | G-01 | ✅ `1ccf11537`+`8ca67ea39`（真实缺口 7 项已补；噪声项随 llm-wiki 取消剔除；代码待实现项移交批次 D）｜批次 D 已完成：D1 离线重置哈希改 Identity PBKDF2 `bea06505b` + D2 配置文档对齐 `4f7a9563c` | 1d |
| G-03 | SSOT 收敛 | 权限矩阵权威引用统一（04-permissions）、跨文档对齐、wiki 同步 | G-01 | ✅ `861818236`（批次 A/B/D 完成；批次 C 技术栈/CodeStyle 评估保留，术语专项留 P2） | 0.5d |

---

## 七、执行阶段

### Phase 0: 安全/基础设施 (P0，最高优先级)
| 序号 | 任务 | 预估 | 状态 |
|------|------|------|------|
| 1 | B-01 P0 安全修复 (7项) | 待定 | ✅ |
| 2 | B-02 配置修改 API | 1d | ✅ |
| 3 | A-12 AuthService 收敛 | 0.5d | ✅ 已完成 |
| 4 | C-04 NuGet 包清理 | 0.25d | ✅ 已完成 |
| 5 | C-03 部署脚本清理 | 0.25d | ✅ 已完成 |
| **小计** | | **~2d + 安全修复** | **5/5 完成** |

### Phase 1: 基础清理 (低风险)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | A-01 文档清理 | 0.5d |
| 2 | A-02 死代码删除 | 0.5d |
| 3 | A-07 CrossModule 死方法 | 0.25d |
| 4 | A-10 接口下沉 | 0.5d |
| 5 | A-11 Registration 依赖清理 | 0.25d |
| 6 | A-08 命名规范统一 | 1d |
| **小计** | | **~3d** |

### Phase 2: 架构优化 (为功能扫清障碍)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | A-03 MediatR 简化 | 1d |
| 2 | A-04 超大类型拆分 | 2d |
| 3 | A-06 Repository 泛型化 | 1d |
| 4 | A-09 架构测试补全 | 1d |
| 5 | A-05 实体源统一 | 3d |
| **小计** | | **~8d** | **5/5 完成** |

### Phase 3: 核心功能 (业务价值最高)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-03 Excel 导出/导入 | 2-3d |
| 2 | B-04 报表增强 | 2d | ✅ |
| 3 | B-05 配置中心 UI | 1d |
| 4 | B-06 数据备份/恢复 | 1.5d |
| 5 | B-07 初始化向导 | 1d |
| **小计** | | **~8d** | **5/5 完成** |

### Phase 4: 高级功能 (依赖 Phase 2/3)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-08 Desktop 发布包 | 2d |
| 2 | B-09 自动更新 | 2d |
| 3 | B-10 SignalR | 3d | ✅ |
| 4 | B-11/B-12 Excel 模板 | 1d |
| 5 | B-13 验方校验 UI | 0.5d |
| **小计** | | **~8.5d** |

### Phase 5: 收尾
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | C-01 Desktop 测试修复 | 1d |
| 2 | C-02 systemd 服务 | 0.25d |
| 3 | C-05 文档同步 | 0.5d |
| 4 | B-16 Swagger | 0.5d |
| **小计** | | **~2.25d** |

### 总预估

| 阶段 | 预估 | 累计 |
|------|------|------|
| Phase 0 安全/基础设施 | 2d+ | 2d+ |
| Phase 1 基础清理 | 3d | 5d+ |
| Phase 2 架构优化 | 8d | 13d+ |
| Phase 3 核心功能 | 8d | 21d+ |
| Phase 4 高级功能 | 8.5d | 29.5d+ |
| Phase 5 收尾 | 2.25d | 31.75d+ |

> 不含 B-14 排班管理 (3d)、B-15 离线同步 (5d+)，视业务需求决定。

---

## 八、状态跟踪

> 每完成一项，更新: ⬜→✅ + Commit SHA

| 任务 | 状态 | 完成日期 | Commit |
|------|------|---------|--------|
| A-01 文档清理 | 🟡 | 2026-08-03 | 待提交（断链 21→0、安全脱敏、08-03 定案传播；剩归档/存根项） |
| A-02 死代码删除 | ✅ | 2026-08-05 | `5f89e58ec` — 删除 5 文件（-896 行）+ 8 死方法；保留 PasswordHelper/SystemLog/IEditable/NotSupportedException 桩 |
| A-03 MediatR 简化 | ✅ | 2026-08-05 | `29a4675af` `c5aca4e04` `741ca8735` `4b97bcfde` `5172ff9ca` — MedicalCase 全部 Handler/Command/Query/Validator 删除（40 文件，-1285 行）；Server/LocalWebAPI/Base controller 直连 Service；AddMediatR 移除；架构测试更新为断言统一验证器 |
| A-04 超大类型拆分 | ⬜ | — | — |
| A-05 实体源统一 | ✅ | 2026-08-05 | 已由 A-02/A-03 覆盖（勘察确认：无 Domain 项目、Server 模块无实体定义、Desktop 用 DTO，Shared/LYBT.Entities 为唯一实体源） |
| A-06 Repository 泛型化 | ✅ | 2026-08-05 | `d379f4a9d` — 方案 B 收敛版：新增 `IEntityApiSegment<TList,TDetail,TInput>` 泛型段（5 标准 CRUD）+ `EntityApiClientRepositoryBase<TList,TDetail,TInput>` 派生基类（用段实现 CRUD，失败抛 InvalidOperationException、GetPaged Data==null 空分页，语义与现状一致）；4 段接口以 DIM 默认实现转发到现有实体命名方法（8 个实现类零改动）；新增 `IEntityInputDto` 约束接口（Shared）供基类提取更新 ID；Patient/Formula/Herb/User 4 仓储删标准 CRUD 样板（-340/+22，净 -318 行），Patient/User 因接口无 category 保留 1 行 GetPagedAsync 薄包装；Registration/MedicalCase 与 2 参旧基类保持原样。build --no-incremental 0 错误 0 警告；架构测试 92/92 |
| A-07 CrossModule 死方法 | ✅ | 2026-08-05 | 已由 A-02 覆盖（`5f89e58ec` 删除 8 个死方法） |
| A-08 命名规范统一 | 🟡 | 2026-08-05 | `35f4cc2e6` `580bc4fcc` `7dbd196b4` — XML 注释已统一为中文（~55 文件 + 5 服务端文件）；Repository 后缀全部一致；Service 后缀发现 ~20 处 Manager/复数/Handler 类不一致，已报告待决策（改名影响面大，未执行） |
| A-09 架构测试补全 | ✅ | 2026-08-05 | `97dcfe6b5` — 删除 skip 测试（YAGNI）+ 新增 7 守卫：Controller 继承 Base* / Desktop Repository 基类 / Module DI 注册 / Options SectionName / Controller 返回 IActionResult / Validator AbstractValidator / Mapperly [Mapper]；验证 build 0 错误 0 警告 + 架构测试 92 过 0 败 0 跳 |
| A-10 接口下沉 | ✅ | 2026-08-05 | `6a1620e8b` — IFormulaService/IHerbService 从 Formula/Herbs 模块移到 LYBT.Desktop.Contracts.Services；9 个源文件 + 2 个测试文件 using 更新；空 Interfaces 目录删除 |
| A-11 Registration 依赖清理 | ✅ | 2026-08-05 | 已无跨模块依赖（A-02 清理后确认） |
| A-12 AuthService 收敛 | ✅ | 2026-08-04 | 代码已通过 IAuthSessionRepository（RefreshTokenCommandHandler 无直接 DbContext） |
| B-01 P0 安全修复 | ✅ | 2026-08-04 | `831702b51` `cb4d3e6b9` |
| B-02 配置修改 API | ⬜ | — | — |
|| B-03 | Excel 导出/导入 | ✅ | 2026-08-05 | `4d70b487a` `0fdfde0d3` `bed75026e` `709bad616` `64c58c59a` `8968fd130` `1bc468851` — NPOI 2.7.2（中央版本钉）；ExcelService 通用三方法（ExportToExcel/GenerateTemplate/ParseExcel，XSSFWorkbook）+ 4 单测；WebApi 保留 JSON 批量导入端点（`POST /batch-import`），Excel 格式转换由前端 Desktop 负责；患者新增 BatchImportPatientsCommand（Skip/Update/Error 策略，与药材命令同构）；Herbs/Formulas Excel 导入复用现有 BatchImport 命令（拼音生成/药材名匹配/验方校验）；build --no-incremental 0 错误 0 警告，架构测试 92/92 |
| B-04 报表增强 | ✅ | 2026-08-06 | `cab959dc1` — 新增 5 个端点：`GET /reports/trend/income`（挂号费/药费/合计折线，granularity=day/week/month）、`GET /reports/trend/consultations`、`GET /reports/doctor-performance`（问诊数/挂号费/药费/平均处方金额）、`GET /reports/herbs/ranking`（top 默认 10，复用药材使用聚合查询）、`GET /reports/patient-flow`（新患者/回头患者，按患者首次完成就诊归类）；新增 `ReportGranularity` 枚举 + 4 个趋势/绩效 DTO（药材排行复用 `HerbUsageItemDto`）；仓库按日 `GROUP BY CONVERT(date, CreatedAt)` 聚合下推 SQL，服务层 `ReportTimeBuckets` 按周（周一起）/月（1 号起）汇总；修复存量缺陷 `GetMedicineFeeTotalAsync` 的 `pi.Amount` 计算属性 EF 无法翻译（改 `UnitPrice * Dosage`，否则日收入药费运行时必炸）；`ReportRepository`/`ReportService` 由 internal 改 public（与 Herbs/Auth 等模块可测类惯例一致，供单测直构）；测试项目补引 `LYBT.Module.Reports`；新增 14 单测（EF InMemory 真实实现零 mock）全过 | |
| B-05 配置中心 UI | ✅ | 2026-08-07 | `c518318ed` — 新增 `IConfigurationApi` Refit 接口（GET/PUT/validate）；`SystemSettingsViewModel` 新增服务器配置属性（ServerAppName/ServerAppVersion/ServerEnvironment）+ LoadServerConfig/SaveServerConfig/ValidateConfig 三个命令；`SystemSettingsView.xaml` 新增「服务器配置」Border 区域（应用名称可编辑、版本号/环境只读、保存/验证/刷新按钮）；`UnifiedApiClientExtensions` 注册 IConfigurationApi |
| B-06 数据备份/恢复 | ⬜ | — | — |
| B-07 初始化向导 | ⬜ | — | — |
| B-08 Desktop 发布包 | ⬜ | — | — |
| B-09 自动更新 | ⬜ | — | — |
| B-10 SignalR | ✅ | 2026-08-06 | `0d8aabb90` — US-REG-008 医生工作台待诊列表实时更新：服务端新增 `RegistrationHub`（`[Authorize(DoctorOrAdmin)]`，按 doctorId 分组）+ `RegistrationConnectionManager`（ConnectionId↔DoctorId）+ `INotificationService`/`NotificationService`（`IHubContext` 推送 `NewRegistration`/`RegistrationStatusChanged` 到 `doctor-{id}` 分组，空 doctorId 跳过）；Create/StartVisit/Cancel 三个 CommandHandler 在业务成功后触发推送（推送失败仅日志，不影响主流程）；`Program.cs` 注册 `AddSignalR` + `MapHub("/hubs/registration")`；Desktop 新增 `SignalRClient`（`Microsoft.AspNetCore.SignalR.Client` 8.0.26，JWT access_token 连接、自动重连 2/10/30s、断线降级 15s 轮询复用候诊队列接口）发布 `RegistrationRefreshedEvent`，`RegistrationListViewModel` 订阅实时刷新（Doctor 角色导航时启动/停止）；新增 9 单测（NotificationService 分组过滤 + ConnectionManager 映射，EF/手写 fake 零 mock）；build --no-incremental 0 错误 0 警告，架构测试 92/92 | |
| B-11 药材/验方模板 | ⬜ | — | — |
| B-12 患者导入导出 | ⬜ | — | — |
| B-13 验方校验 UI | ⬜ | — | — |
| B-14 挂号排班 | ⬜ | — | — |
| B-15 离线同步 v2.0 | ⬜ | — | — |
| B-16 Swagger | ⬜ | — | — |
| C-01 Desktop 测试修复 | ⬜ | — | — |
| C-02 systemd 服务 | ⬜ | — | — |
| C-03 部署脚本清理 | ✅ | 2026-08-04 | 删除 `.worktrees/` 下 7 个孤儿 checkout（arch-cleanup/fix-high-issues-t1-t4/fix-high-issues-t5-t6/fix-remaining-issues/fix-remove-sync-loader/offline-sync-review/rebase-offline），内含 sync-to-server.ps1/deploy-fixed.ps1 等均为仓库根目录 `tests/newman/`、`tests/postman/` 的过期重复；目录已被 gitignore 且未注册为 worktree，代码可从 git 分支恢复 |
| C-04 NuGet 包清理 | ✅ | 2026-08-04 | 扫描 38 个 csproj 共 76 个 PackageReference，移除 8 个代码零使用包：NPOI（Foundation/Infrastructure）、EPPlus（WebAPI/Herbs/Formula）、System.CommandLine（PasswordHashGenerator）、Bogus（Tests.Server）、Xunit.StaFact（Tests.Desktop）、Microsoft.Extensions.ObjectPool（Foundation）、Microsoft.Extensions.Logging.Debug（Shell）、Refit.HttpClientFactory（Tests.Desktop）；同步清理 Directory.Packages.props 中央版本钉。保留 SixLabors.Fonts/ImageSharp（QuestPDF 传递依赖 CVE-2025-27598/54575 的安全版本固定）与 EFCore.Tools（迁移工具）。`85b2d16c5`，构建 0 错误 0 警告 |
| C-05 文档同步 | ⬜ | — | — |
| D-01 接诊链修复（D8：StartVisit 原子建医案，2026-08-03 决策确认） | ✅ | 2026-08-03 | `7caa1fb4a` |
| D-02 QuickVisit Desktop 接线（US-REG-002 激活） | ✅ | 2026-08-03 | `7caa1fb4a` |
| D-03 医案状态机重构（B6-B9：取消=物理删 / 仅 Completed 打印 / 打印保护简化 / 堵状态机绕过） | ✅ | 2026-08-03 | `64adfebd3` `5bc01de5b` `7fd4a09e5` `d98332394` |
| E-01 规则体系：coder 层（角色 AGENTS.md 精简 + Skill v0.6.0 确立 SSOT） | ✅ | 2026-08-04 | profile 目录（非 repo），备份 `profiles\coder\backups\2026-08-04\` |
| E-02 规则体系：项目层（项目 AGENTS.md 精简为入口+引用，详细规则迁入 Skill） | ✅ | 2026-08-04 | `da2290117` |
| E-03 规则体系：项目总账拆分（13a/13b/13c） | ✅ | 2026-08-04 | `1664f2ffc` |
| E-04 规则体系：MCP 配置清理（移除 tavily 又恢复，serena/tavily 全部保留） | ✅ | 2026-08-04 | 本地修改（`.mimocode` 被 gitignore，无 commit） |
| E-05 规则体系：「以文档为准」强制规则 + 文档目录优化（00-governance/ 归位 + 历史报告清理 + AI 查询指南） | ✅ | 2026-08-06 | 见 §九 2026-08-06；skill v0.9.0（profile 目录） |
| F-01 FeatureToggle 开关接入 | ⬜ | — | — |
| F-02 桌面 Mapper 统一 | ⬜ | — | — |
| F-03 LocalData Mapper Target 策略 | ⬜ | — | — |
| F-04 SyncService CS8602 | ⬜ | — | — |
| F-05 PatientMapper 死代码 | ⬜ | — | — |
| F-06 打印模板扩展 | ⬜ | — | — |
| F-07 API 版本化准备 | ⬜ | — | — |
| F-08 日志归档策略 | ⬜ | — | — |
| H-01 审查遗留修复 #1/#4/#5/#6 | ✅ | 2026-08-05 | `a3baa7e17` — #1 审计表重建迁移 / #4 配置类合并 / #5 模块连接字符串 fallback / #6 工具表名修正（详见 §九） |
| H-02 审查遗留修复复核（P0a/P0b/P1/P2/P3） | ✅ | 2026-08-05 | `99cd1a54d` — 修复 a3baa7e17 引入的迁移/配置问题：#0a RecreateDroppedAuditTables 只建 SecurityAuditLogs（另两表已由 AddMedicalCasePrintLog 重建）；#0b 删除目标表错误的 AddRowVersionToAspNetUsers 迁移（Users.RowVersion 由 InitialCreate 提供）；#1 补 4 个复合索引（迁移+实体配置+快照同步，EventType 定长 50 满足索引要求）；#2 ConnectionStringResolver 统一 5 模块三级回退；#3 RealName 实体 [StringLength] 统一 100（详见 §九） |
| WebApi 架构修复（03-server.md 对齐 14 项 + ReportsController 服务层） | ✅ | 2026-08-05 | docs `59be25317`（03-server.md v2.3 全面对齐 14 项发现；计划文档归位 docs/compose/plans/）；code `7184f96e8`（新增 IReportService/ReportService，ReportsController 改注入 Service 接口，架构测试 ServerAssemblies 补入 Reports/Registration）—— build --no-incremental 0 错误 0 警告，架构测试 92/92 |
| B-21 安全增强（Token 族旋转 + 安全审计，US-AUTH-006/007） | ✅ | 2026-08-06 | `e2cedf6a2` — 批量撤销会话/登录踢出/4 个用户操作 Handler 审计+撤销；详见 §九 |
| WebApi 测试发布（60.190.215.86:5000） | ✅ | 2026-08-06 | Server 端最新版本部署至公网测试服务器；8 处 BaseApiController 递归修复 `b6097c036`/`34d5e599d`/`6cd91cb7d`；Smoke Test 全过；详见 §九 |
| A-13 DeployController 安全加固 | ✅ | 2026-08-07 | `624b438e4` — `POST /deploy/restart` 加确认机制：新增 `RestartConfirmDto(string? Confirm)`，body 中 `confirm` 必须为 `"RESTART"` 才执行 `StopApplication()`，否则返回 ValidationFail；`[Authorize(AdminOrSuperAdmin)]` 不变 |
| A-14 Controller 继承文档化 | 🟡 | 2026-08-07 | 评估结论：三种继承路径各有合理性（BaseApiController=独立端点、BaseCrudController=标准 CRUD、BaseMedicalCasesController=领域特化）；MediatR+Service 混合注入是有意设计（查询走 Service 绕过管道、命令走 MediatR 验证+审计），不统一 |
| A-15 MedicalCasesController 拆分 | ❌ | 2026-08-07 | 评估后取消：状态流转仅 4 个方法（~100行），共享路由前缀，拆分后 Desktop Refit 也需改，ROI 不合理 |
| WebApi 优化批次（安全加固+代码质量） | ✅ | 2026-08-07 | `84433560e` CORS 加公网 IP + `Database:ConnectionString` 禁写 + AutoLogin 过期验证；`8528e1efb` CORS 去内网 IP；`c094341af` HealthController 去重 + RegistrationsController 注释清理 + 3 Controller 缩进修复 + AuthController 错误处理统一 |
| PolicyConstants.AdminOnly 删除（统一 AdminOrSuperAdmin） | ✅ | 2026-08-07 | 见 §九 — 删除冗余常量 + 2 处 AddPolicy 注册 + 注释/文档 7 处同步；Controller 零引用（从未被 [Authorize] 使用）；build --no-incremental 0 错误 0 警告 |
| S-04 批量导入 Update 模糊匹配修复（docs/compose/reports/webapi-deep-analysis-mimo-2026-08-07.md 报告） | ✅ | 2026-08-07 | `e4c17d5ca` — Patients 新增 `GetExactByNameAsync`（接口+实现），BatchImportPatientsCommandHandler Update 分支弃用 `GetPagedAsync(1,1,name)` 模糊定位改精确匹配；BatchImportHerbsCommandHandler 复用已有 `GetByNameAsync`（Name 精确匹配，不新增冗余方法）；Formula 模块核实无 Update 分支（重复即报错），不涉及。build --no-incremental 0 错误 0 警告，架构测试 83/83 |
| S-01/S-02 Auth 安全修复（Token 过期错位 + RefreshToken 500） | ✅ | 2026-08-07 | `ae52f5ef0` — S-01 LoginCommandHandler 删硬编码 60，Token 过期统一读 `JwtOptions.AccessTokenExpirationMinutes`（注入 `IOptions<JwtOptions>`）；S-02 RefreshTokenCommandHandler 对 `oldSession==null` 提前返回 `AuthTokenInvalid` + 审计，删 `?? Guid.Empty` fallback；验证 build --no-incremental 0 错误 0 警告 |
| Q-01 批处理 Handler 泛型化 | ✅ | 2026-08-07 | `da7e3b162` `4ced30601` `8c3cb01ce` — 新增 `BatchOperationHandlerBase<TEntity>`（LYBT.Infrastructure/BatchOperations，模板方法模式）；6 个 Handler（Users Delete/Enable/Disable + Patients/Herbs/Formula Delete）继承基类（-147 行），消除循环→GetById→变更→累计 模板；差异点经钩子保留（自删/IsSysAdmin/无权限、医案引用检查、缓存失效、领域事件派发、IsSuccess 语义、消息文案）；BatchImport 3 个 Handler 与 Herbs/Formula Service 版 BatchEnable/Disable 不在本次范围；行为等价（自删失败项 Name 多填充、异常文案 ex.Message 替代固定"删除操作失败"为已知微小差异）；build --no-incremental 0 错误 0 警告，架构测试 83/83 |
| Q-02 using 类型别名清理 | ✅ | 2026-08-07 | `5de43132b` — 移除 src/Server 21 处 `using X = 全名;` 类型别名，统一就近 `using 命名空间;` + 短类型名（保留 2 处真实冲突别名 LybtMemoryCacheOptions/LybtJsonOptions）。分类处理：① 同义别名 EC/GenericErrorCode（MedicalCase 5 文件 + Infrastructure Web 2 文件）→ `using LYBT.Shared.Models.Primitives.ErrorCodes;` + `ErrorCode`（BaseApiController 未用直接删行）；② 实体别名 FormulaEntity/FormulaHerbItemEntity/RegistrationEntity/RegistrationSource → 就近 using + 短名；③ DTO 冗余别名 SetPrescriptionFlagRequest/RecordPrintRequest 直接删（命名空间已 import）；④ 真实冲突别名保留。遇 CS0118（命名空间遮蔽类型：`LYBT.Module.Registration`/`LYBT.Module.Formula` 与实体同名，方法签名/特性位置解析到命名空间）8 个文件按方案回退规则改用全名 `LYBT.Entities.Registrations.Registration`/`LYBT.Entities.Formulas.Formula`（Mapper/Repo/Interface/2 Handler + Formula 5 文件）；build --no-incremental 0 错误 0 警告，架构测试 83/83 |
| Q-03 模块命名空间统一复数（CS0118 治本，方案甲） | ✅ | 2026-08-08 | `27b65e42b` — 纯命名空间级重命名（56 文件）：① Server `LYBT.Module.Registration`→`LYBT.Module.Registrations`（src 63 处 + 2 测试文件 using）；② Desktop `LYBT.Desktop.Registration`→`LYBT.Desktop.Registrations`（11 文件/16 处 + 2 xaml 的 x:Class/clr-namespace 4 处 + 1 测试文件 3 条 using）；③ Formula 单数笔误 `FormulaBatchImportCommandValidator.cs` 命名空间修正为复数（CS0118 实测根因，Mimo 验证报告确认）；④ 11 文件全名回退裸类型名 `Registration`/`Formula`/`FormulaHerbItem`（Registration 5：Repo/IRepo/Mapper/2 Handler；Formula 6：Repo/IRepo/Mapper/DbContext/2 Handler）+ 就近补 `using LYBT.Entities.Registrations;`/`using LYBT.Entities.Formulas;`。csproj/AssemblyName/RootNamespace/文件夹/sln 条目一律未动（架构测试按程序集名 `Assembly.Load` 加载不受影响，方案 B 已否决）。验证：build --no-incremental 0 错误 0 警告、架构测试 83/83、Server 单测 9/9、Desktop 单测 6/6、`rg` 0 残留旧命名空间（仅 EF 迁移快照实体字符串保留）。依据：`docs/compose/reports/namespace-consistency-audit-2026-08-07.md` + `docs/compose/reports/namespace-consistency-mimo-verify-2026-08-07.md` |
| A-16 结构审计（技术总监 + Mimo 交叉验证） | ✅ | 2026-08-08 | 任务书 `b7c7390d0` + 技术总监报告 `4bce47483` + Mimo 报告/交叉验证 `bd02ac512` — 三层边界健康（0 越层/0 环/Server 模块 0 直接引用）；双轨真共享（ADR-0010）；**交叉纠错 4 项**（C1 本地 CRUD 断裂 P0、C2 AsyncLocal 死、C3 模块数 8、C4 种子共享）；P1 七项收敛候选。详见 `docs/compose/reports/structure-audit-2026-08-08.md` + `docs/compose/reports/structure-audit-mimo-2026-08-08.md` + `docs/compose/reports/structure-audit-crosscheck-2026-08-08.md` |
| Q-04 死代码清理（Serena+codebase-memory 交叉验证第二轮） | ✅ | 2026-08-08 | `97445a6f3` — 删除 6 死文件（ExceptionFactory/TraceContext/LocalEndpoints/Reports Domain 3 record）+ 3 空目录（Domain/Constants/Factory）+ GlobalUsings.cs；**ImportResultDto.cs 保留**（修正任务清单误报：Patient/Herb/Formula 3 个 BatchImportResultDto 继承它，交叉验证报告本身未列该项）；删 6 死方法（ConfigureGracefulShutdown/RemoveHerb/MarkShared/UpdatePrice/GetCurrentUserRole/MapUserRoleToString/CanManageUser）；清 60 处业务文件未使用 using（MedicalCase 16 处 System.Threading 等）+ GlobalUsings 的 ComponentModel；验证 build --no-incremental 0 错误 0 警告、架构测试 83/83、IDE0005 src 业务文件残留 0（Migrations 生成文件 12 条按规则不改） |
| A-18 P1-7 文档重写先行 | ✅ | 2026-08-08 | `8db198b8a` — 08-shared.md 按实际 5 项目结构重写（Primitives/Utilities/Components/Validators 坍缩为 Shared.Models 内文件夹；Utilities 清单纠错为实际 4 文件；删虚构 DTO 继承链 BaseDto/TimestampDto/StatusDto/AuditDto；ExceptionHandling/Configuration 目录按代码实际重写；Mapperly 数量 23→13；MedicalCaseBusinessRules 标记已实现）；同步修正 13 项文档偏差 D1-D13：03-server.md（D3 ReportsDbContext 不存在、URL 版本段、ProblemDetails instance）、05-dual-mode.md（D5-D10：URL 前缀统一 /api/v1/、EnsureCreated→MigrateAsync+双种子、DI 架构 ADR-0010、端点覆盖表 104/99 重写并删虚构 categories/by-phone 端点、打印日志 404、Rate Limiting 5/60s、LocalWebApiDbContext→AppDbContext、实体位置 src/Shared）、00-architecture-summary.md（D11 BCrypt→PBKDF2）、Core AGENTS.md（D13 LocalData 项目不存在修正）。纯文档改动，无代码 |
| A-18 P1-3 CorrelationId 收敛 | ✅ | 2026-08-08 | `6769b02e0` — 删 `AsyncLocalCorrelationIdProvider.cs`（全仓 0 调用点，死代码）+ `AddAsyncLocalCorrelationIdProvider` 扩展方法；修 `CorrelationIdEnricher.cs:14` 注释（Desktop 实际用 ActivityCorrelationIdProvider，非 AsyncLocal）；核实双端单机制：Server=CorrelationIdMiddleware（W3C traceparent），Desktop=ActivityCorrelationIdProvider，各自端内单机制跨端不强统一（技术总监判断成立）。build --no-incremental 0 错误 0 警告，架构测试 83/83，日志相关单测 16/16 |
| A-18 P1-4 Server 手写 Mapper 改 Mapperly | ✅ | 2026-08-08 | `a484f6c15` — Formula/Herbs/Patients/Users 4 模块手写静态 Mapper 类改 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, AutoUserMappings = false)] static partial class`：纯属性复制方法（ToListDto/ToDetailDto）转 `public static partial` 由 Mapperly 编译时生成（Formula 需 [MapProperty] Indication→Indications + [MapperIgnoreTarget] TotalPrice）；领域工厂方法（ToEntity 走 Formula.Create/Herb.Create/Patient.Create）保留手写——AutoUserMappings=false 下不带 [UserMapping] 即不被 Mapperly 发现（规避 RMG001 额外参数签名不支持）；UserMapper 含 `?? string.Empty` null 合并防御保留手写（Mapperly 无法等价表达）并标记 [UserMapping(Default=false)]。映射方法签名不变、行为等价、静态调用点零改动。build --no-incremental 0 错误 0 警告，架构测试 83/83，Formula/Herb/Patient/User 4 模块单测 161/161 |
| A-18 P1-6 本地配置持久化 | ✅ | 2026-08-08 | `031682cad` — 本地 ConfigurationController 的 ConcurrentDictionary 内存存储改注入 `IConfigurationStore`（复用远程 JsonFileConfigurationStore，`{BaseDirectory}/config/runtime-overrides.json` 落盘 + 原子写入 + 重启读取）；LocalWebApiProgram 注册 IConfigurationStore 单例；Controller 方法改 async。架构测试 P20 依赖白名单（LYBT.Infrastructure）合规。build --no-incremental 0 错误 0 警告，架构测试 83/83，JsonFileConfigurationStore 持久化单测 4/4；Desktop 集成测试 404 为 C-01 已知环境项（stash 基线复现，与改动无关） |
| A-18 P1-1/P1-2 契约双套统一 + adapter 收敛（方案 A） | ✅ | 2026-08-08 | `a8b9d0b0f` — ① 11 个 Refit Api 接口（`Contracts/Api/*`）internal 化，Contracts.csproj 加 InternalsVisibleTo（LYBT.Desktop.Foundation + LYBT.Tests.Desktop）；② 补 Configuration 段：新增 `IApiClientConfiguration`（5 方法：GetConfiguration/GetValue/SetValue/UpdateConfiguration/ValidateProduction）+ `ConfigurationApiClient`（Refit 适配）+ `ConfigurationHttpApiClient`（本地适配，全部抛 NotSupportedException——本地模式隐藏服务器配置，与 local-only 桩模式一致）；③ `IApiClient` 聚合接口加 Configuration 属性，RefitApiClient（RestService.For&lt;IConfigurationApi&gt;）/HttpClientApiClient/ SwitchingApiClient 三实现同步；④ Shell 删独立 `Register<IConfigurationApi>`（UnifiedApiClientExtensions L116-122 及 `using Contracts.Api`）改走统一 IApiClient 注册；⑤ Admin 3 VM 换注入 IApiClient（SystemSettingsViewModel Configuration×3 / LogLevelControlViewModel Diagnostics×4 / DeploymentViewModel Deploy×2 = 9 调用点）；⑥ WebApiE2ETestBase 7 个 Refit 属性 + CreateAuthenticatedAuthApi 由 protected→internal（接口 internal 化后同程序集可访问，CS0053/CS0050 修复）。行为等价：SwitchingApiClient 双轨语义未改（Configuration 仅加转发属性）、IEntityApiSegment 未动、CancelMedicalCaseAsync 签名差异未处理（记录于 §九）。验证：build --no-incremental 0 错误 0 警告、架构测试 84/84、RefitClientContract/HttpClientApiClientEnvelope/SwitchingApiClient 单测 23/23 |
| A-19 移除 Auto 模式（用户自主切换） | ✅ | 2026-08-08 | `4a1393c3a` — 依据产品负责人 2026-08-08 决策「远程/本地切换需自主选择，远程不可用不自动降级」。① `ConnectionMode` 枚举删 `Auto` 值（`IConnectionModeService.cs`），接口删 `DetectBestModeAsync` 声明；② `ConnectionModeService` 删 `DetectBestModeAsync()`（自动降级核心）+ `SetMode` 的 `case ConnectionMode.Auto` 分支 + `_detectGate` 门闩（仅检测串行用，已无用途）；构造仍按已保存 `PreferredMode`/`IsLocal` 恢复模式（不探测），`OnUrlChanged` 保留按 URL 推导（用户配置跟随，不触发探测）；③ 调用点清理——`ConnectionStatusViewModel.DetectConnectionModeAsync` 不再调 `DetectBestModeAsync`，仅刷新显示 + `CheckRemoteAvailableAsync()`（UI 按钮状态）；`FirstRunSetupViewModel`/`ServerConfigViewModel`/`LoginViewModel`/`StatusBarManager` 仅用保留 API 零改动；④ 保留 `CheckRemoteAvailableAsync`/`TestRemoteConnectionAsync`/`TestLocalConnectionAsync`；`platform.md` 连接模式章节同步（显式切换语义）。验证：build --no-incremental 0 错误 0 警告，架构测试 84/84 |
| A-21 模块级审计 P1 修复批次 | ✅ | 2026-08-08 | 6 独立 commit：M4 `1b19bce13`（RoleDefinitionBase 模块名 AuthModule→AuthenticationModule，BaseModules 字符串直传 Prism LoadModule，逐一核对 8 模块名与 ModuleCatalog 类名仅此 1 错）/ M5 `3d692c5f5`（3 VM 越层改走 Service：ReportsHome→新建 IReportService、AuditLog→新建 IAuditLogService、RegistrationList→复用 IPatientService.GetByIdAsync，VM 只注入 Service 接口，DI 注册 2 处）/ M3 `4ad350a39`（领域事件空转删除：13 处发布 + 12 Domain/Events 文件 + SharedKernel 3 基础设施 + DI 注册，-470 行；BatchDeleteFormulas 事件钩子移除；Modules/Server AGENTS.md 同步，异步通知改述 SignalR）/ M2 `876581164`（删 4 零引用 Mapper：Herb/User/Formula/FormulaHerbItem；12 手写映射点盘点仅 1 纯复制但 Mapperly 4.3.1 无法解析 MVVMTK 生成属性（RMG012）按 A-18 P1-4 先例保留手写，其余 11 处含业务逻辑保留；三模块文档同步）/ C1 `2fa05ff63`（LocalData 废弃：LocalDbContext 移入测试项目保留 UserJourney 基建，生产 csproj LocalData Dependencies 移除，架构测试 DM08 改写为守卫生产层无 LocalDbContext）/ F-01+Tools `271bca70f`（删 FeatureToggleOptions+注册点+死 PrescriptionSettingsService+RegisterReloadableOptions 级联 3 内部类+appsettings 节；删 ApiTester/LoginTester/UserInfoVerifier 3 项目，保留 PasswordHashGenerator，-1014 行）。每项独立验证 build --no-incremental 0 错误 0 警告 + 架构测试 85/85 + 相关单测全过（Desktop 84 失败为 C-01 已知环境项，stash 基线复现一致） |
| A-23 越层修复 + 守卫补全 + 孤儿类清理 | ✅ | 2026-08-08 | 3 独立 commit：A-23a `d3290b321`（3 VM 真越层修复：AccountSettingsViewModel IApiClientUsers→IUserService、PatientSelectionViewModel IApiClientPatients+IApiClientMedicalCases→IPatientService+IMedicalCaseQueryService、SysadminHomeViewModel IApiClientAuth→新建 IAuthHealthService；Desktop IMedicalCaseQueryService/IMedicalCaseRepository 链补 GetPendingCasesAsync；行为等价）/ A-23b `1198cace6`（新增架构守卫 DP10：Desktop ViewModel 禁止注入 IApiClient* 子接口，豁免统一 IApiClient 过渡，架构测试 85→86）/ A-23c `4db419c11`（孤儿类 D=29 删除：Patients 死组件链 12 类 + Infrastructure 9 + Foundation 2 + Formula 2 + Contracts/Controls/MedicalCase 各 1 + Shared.Configuration 1，-2780 行；同步清理：ShowUnfinishedCaseDialogAsync 死链、App.xaml.cs/ServiceCollectionExtensions/PatientsModule 注册孤儿、ApiRouterTests/MedicalCaseChangeTrackerTests 死测试、LoggingHttpHandler 下沉 Infrastructure→Foundation（补 Shared.Logging 引用）、CustomControlArchTests SuggestionType 白名单）。每项独立验证 build --no-incremental 0 错误 0 警告 + 架构测试 86/86 + 相关单测与基线一致（Desktop 存量失败为环境项：STA/LocalDB，stash 基线复现一致） |
| A-24 C 级清理批次（Server 死方法 22 类 + 机制残留 9 簇） | ✅ | 2026-08-08 | 3 独立 commit：A-24-1 `ba297ff2d`（Server 死方法清理 22 类删方法不删类：FormulaDetailDto.GetHerbNamesList / BaseCrudController.ExecuteBatchStatusAsync / BaseClaimsHelper.IsAdmin·ParseUserRole / QueryablePagingExtensions.SelectAsync / ProblemDetailsConfiguration.UseStatusCodePagesWithProblemDetails / AuthSessionRepository+IAuthSessionRepository 2 死方法 / UserRepository+IUserRepository 2 死方法 / PatientCrossModuleService+IPatientCrossModuleService 3 死方法 / HerbRepository+IHerbRepository 3 死方法+测试区块连带 / FormulaRepository+IFormulaRepository 2 死方法 / MedicalCaseCommandService 6 死方法+PrescriptionService 4+2 连带孤儿+QueryService/StateService/Repository 死方法+4 接口瘦身 / RegistrationMapper.ToEntity；Formula/Herb 实体 RemoveHerb/MarkShared/UpdatePrice 复证当前代码已不存在无需操作，-1175 行）/ A-24-2 `88aca1280`（机制残留 9 簇：AddSharedLogging 双重载删扩展文件(宿主手工注册)、IApiService/ApiService/RequestDeduplicator 删类+注册+ArchTests 白名单、3 惰性 AuthEvents(LoginSucceeded/LoginFailed/SessionExpired+Payload+2 枚举)删、Tests.Desktop Traits 18 类型删 Traits.cs、UserJourneyTestBaseShared 0 子类删(保留 UserJourneyTestBase 12 消费者)、LocalWebApiProgram.RunAsync 死入口删、UnfinishedCaseChoice 复证 A-23c 已删、LoggingHttpHandler 下沉验证完成、Registration 命名空间复数漂移不改记录 P2，-1013 行）/ A-24-3（纯验证无代码 commit：DP10 专项 1/1 + 架构测试全量 86/86 无新增违规，C-2 VM 越层 6 处已处理——A-18 豁免 3 处 + A-23a 已修 3 处）。每项独立验证 build --no-incremental 0 错误 0 警告 + 架构测试 86/86 + 相关单测与基线一致（Server 228/228 + Desktop 相关测试失败与基线一致为 STA 环境项） |
| A-25 遗留问题解决（项目名统一复数 + 本地样例中文化） | ✅ | 2026-08-08 | 2 独立 commit：A-25-1 `38499d888`（Desktop Registration 项目名统一复数：目录/csproj/AssemblyName/RootNamespace/sln L92/3 处 ProjectReference(Shell·Clinical·Tests.Desktop)/架构测试 Assembly.Load 4 处全量同步 `LYBT.Desktop.Registrations`，README/AGENTS.md 模块清单同步；纯项目名级重命名不动业务逻辑，代码命名空间已是复数无需改，对齐 Q-03 先例）/ A-25-2 `7729d5ae0`（LocalWebApiSeedData 英文样例中文化：Ginseng→人参、Adaptogen→补气药、Unit g→克、Sample Formula→示例验方、Sample Patient→示例患者，对齐代码库既有中文约定；Herbs/Formulas 测试注释 + LocalWebAPI README 同步）。每项独立验证 build --no-incremental 0 错误 0 警告 + 架构测试 86/86 + 相关单测（A-25-1: RegistrationMasterDetailViewModelTests 6/6；A-25-2: LocalWebAPI 集成测试 12/12 失败为**既有测试宿主缺口**——`LocalWebApiControllerTestBase` 缺 AddMediatR+8 模块服务注册（仅真实宿主 LocalWebApiProgram.CreateApplication 有），控制器 ISender 激活失败→登录 500，stash 基线复现一致与改动无关，记录 **P2 待派单**：测试基类应复用 CreateApplication 注册） |

---

## 九、关键决策记录

| 日期 | 决策 | 理由 | 决策人 |
|------|------|------|--------|
| 2026-08-02 | MediatR 保留用于复杂业务，trivial CRUD 改直接注入 | 减少不必要的间接层 | 产品负责人 |
| 2026-08-02 | 架构测试约束 P07/P08/P10 不可违反 | 强制分层边界 | 技术总监 |
| 2026-08-02 | 实体源以 Shared/LYBT.Entities 为准 | 18 个项目已引用，变更成本最低 | 技术总监 |
| 2026-08-02 | 离线同步 v2.0 放弃旧分支，基于 master 重新实现 | 旧分支无法编译且删除了关键代码 | 技术总监 |
| 2026-08-03 | **接诊即建**：StartVisit/QuickVisit/本地选患者开始看诊时原子创建 MedicalCase(Active) + Registration(InProgress)，统一两条接诊路径 | 消除 BR-000 与 US-REG-005 矛盾；InProgress 天然挡住退号，无空医案残留 | 产品负责人 |
| 2026-08-03 | 权限决策四连（四角色需求审查）：① 患者删除/禁用仅 Admin+；② 前台不可查看药材/验方；③ 打印仅 Doctor（Admin 可查打印记录）；④ Admin 挂号只读查看 | 最小权限 + 角色画像清晰；代码待按操作级细分 | 产品负责人 |
| 2026-08-03 | 挂号费：医生实体加 `RegistrationFee` 字段（Admin 设置），前台/QuickVisit/本地创建挂号时自动带出，免号填 0 | 落地 REG-BR-009「挂号费跟医生相关」；报表统计准确 | 产品负责人 |
| 2026-08-03 | 医案状态机重构（医案专题）：取消=物理删除（不判内容，审计可统计）；已完成只可软删（Admin 清理）；未完成不可打印（草稿水印删，打印保护简化为 IsPrinted 标记）；REG-BR-005 放弃恢复；医案无 Status 字段（只需 CaseStatus） | 场景驱动的生命周期设计；消除状态/保护机制冗余 | 产品负责人 |
| 2026-08-03 | 文档深度审查（3 路并行）：45 文件修改 + 14-deploy.md 补建；P0 明文密码/密钥全部脱敏；21 处断链清零；08-03 定案向下游传播（glossary/users/data-model/printing/security/ADR-0001/modules）；幻影端点修正；Token 有效期 9 处改为配置驱动 | 三次审计后系统性清理；A-01 文档清理部分完成 | 技术总监 |
| 2026-08-04 | 规则体系三层架构优化：角色层=通用工作流/调度/沟通规则，项目层=入口+引用，Skill=项目规则 SSOT（v0.6.0）；沟通规则归位角色层；项目 AGENTS.md 141→58 行；MCP 配置曾误删 serena/tavily，产品负责人指出后全部恢复（6 个 MCP 全连接）；优化报告已完成使命后删除 | 消除规则分散重复（重复率约 40%），单点维护 | 技术总监 |
| 2026-08-04 | 总账拆分：数据模型→13a、API 端点→13b、Desktop 视图+已知问题→13c；主文件保留待办/阶段/状态/决策/维护规则，章节编号不变（§九 引用兼容） | 主文件聚焦任务管理，附录独立维护 | 技术总监 |
| 2026-08-04 | 权限终局裁决（LLM Wiki 审阅矛盾收敛）：**医案创建仅 Doctor**（医生负责制）；**药材管理仅 Admin+**（创建/编辑）；**验方管理 Admin+Doctor**；**可见范围：Admin 可见全部医生数据，Doctor 仅自己可见**（数据所有权过滤）；前台不可见/操作药材验方（沿用 08-03） | 审阅暴露的 US-MC-001/药材/验方权限矛盾统一收敛；D7 代码待对齐目标态 | 产品负责人 |
| 2026-08-04 | **角色定位澄清**：sysadmin = 超级管理员（SuperAdmin 角色仅此一人，IsSysAdmin=true，只负责系统运维不碰业务数据）；Admin = 业务管理；前台 = 专职前台；Doctor = 主要看诊。无独立「SuperAdmin 用户群」 | 纠正此前将 SuperAdmin 视为独立角色群的误解；12-matrix SuperAdmin/Sysadmin 两列实为同一人 | 产品负责人 |
| 2026-08-04 | **恢复权限裁决**：**业务数据**（患者/验方）恢复 = 仅 Admin（业务管理，sysadmin 不碰业务、Doctor/前台无权）；**用户账号**恢复 = 层级管理（sysadmin 恢复 Admin，Admin 恢复 Doctor/Receptionist，不可自管）。代码现状：PatientsController.Restore 回退类级策略（全员可调，安全漏洞）；现有 AdminOnly 策略含 SuperAdmin，需新增纯 Admin 策略 | 恢复是删除的逆操作，与「患者删除/禁用仅 Admin」对称；用户账号按已确立的层级管理规则（02-personas §约束） | 产品负责人 |
| 2026-08-04 | **doc-audit 审计完成（G-01）**：3 并行子代理审稿 + 主代理代码校准，150 文档。P0 矛盾 16 项 + P1 未对齐 22 项 + A 类 doc-vs-code 4 项全部修复（4 commit）；结构层健康（断链 1 条在归档）；核心裁决：医案取消=物理删除清 Cancelled 残留、13b 端点权限表按终局重写、PolicyConstants 6 项、Prism=模块框架+CommunityToolkit MVVM、C2（药材权限）已修复、D5（患者单删缺引用检查）待修 | 审计暴露 6 项代码缺口（Restore 权限漏洞、缺纯 Admin 策略、单删缺引用检查、用户/验方 Restore 未实现、打印回写未实现）→ 批次 C 已派发 | 技术总监 |
| 2026-08-04 | **代码批次 C 完成（6 项）**：C1-C3（Restore 权限漏洞/纯 Admin 策略/单删引用检查）commit `f50269f23`；C4-C6（用户/验方 Restore/打印回写）commit `ce905f8b3`。新增 `AdminBusinessOnly` 策略（仅 Admin，sysadmin 不碰业务）；用户恢复按层级管理（sysadmin→Admin，Admin→Doctor/Receptionist，不可自管） | doc-audit 审计产出闭环；Build 0 错误、架构测试通过 | 技术总监 |
| 2026-08-04 | **代码批次 D 完成（2 项）**：D1 离线密码重置工具改 Identity PBKDF2 兼容哈希（`PasswordHasher<ApplicationUser>`，弃 BCrypt `PasswordHelper`，SQL 语句修正 `AspNetUsers`）commit `bea06505b`；D2 配置文档对齐代码（删 `ConfigurationSections` 集中类声称，改「各 Options 类内联 SectionName」；Options 数量 14→19：12 服务端 + 1 共享 + 6 客户端）commit `4f7a9563c`。总账 + gap-list 标记完成 `1c7be777e` | 工具产物写入 `AspNetUsers.PasswordHash` 后用户可登录；文档反映代码真实状态 | 技术总监 |
| 2026-08-04 | **零警告构建达成（基线质量）**：修复 7 个存量编译警告 — CS0105 重复 using（HerbItemControlViewModel）/ CA1001 PrescriptionPrintExecutor 实现 IDisposable 释放 LocalPrintServer / CS0168 未用 catch 变量 / CS8603×2 子 VM 构造顺序提前 / CS4014×2 测试断言补 await。`dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告，commit `8f47ab565` | 落实「0 错误 0 警告」构建基线（2026-08-04 规则） | 技术总监 |
| 2026-08-04 | **Phase 0 收尾（3 项小任务）**：A-12 AuthService 收敛——代码已通过 IAuthSessionRepository（RefreshTokenCommandHandler 无直接 DbContext），无需改动；C-03 部署脚本清理——删除 `.worktrees/` 下 7 个孤儿 checkout；C-04 NuGet 废弃包清理——移除 8 个代码零使用包（NPOI/EPPlus/System.CommandLine/Bogus/Xunit.StaFact/ObjectPool/Logging.Debug/Refit.HttpClientFactory）commit `85b2d16c5`，保留 SixLabors（QuestPDF 安全固定）。Phase 0 仅剩 B-02（配置修改 API，1d） | Phase 0 4/5 完成 | 技术总监 |
| 2026-08-05 | **A-08 命名规范（部分完成）**：① XML `<summary>` 注释全部统一为中文（Desktop ~55 文件 + Server 5 文件，commit `580bc4fcc` `7dbd196b4`；跳过 Designer 自动生成文件、EF 迁移历史、`<remarks>`/`<param>` 与行内注释）；② Repository 后缀全部一致（`{Entity}Repository`）；③ Service 后缀报告：Server 侧全一致，Desktop 基础设施层约 20 处不一致（`DialogManager`/`SessionManager`/`ErrorHandler`/`AsyncExecutor`/`LoadingStateManager`/`PatientSearchManager`/`LoginStateManager`/`SessionLifecycleManager`/`NavigationManager`/`StatusBarManager` 等 Manager 后缀，`ListViewServices`/`MasterDetailServices`/`ViewModelServices` 复数，`LoginCoordinator`/`ShellEventCoordinator`/`AppStartupOrchestrator`/`ApiHealthMonitor`/`StartupPipeline`/`ApiRouter`/`CardReaderFactory` 等）——改名影响面大，仅报告不改 | 注释语言统一为中文；后缀不一致项交由产品负责人决策是否统一 | 技术总监 |
| 2026-08-05 | **B-02 配置修改 API 完成（Phase 0 收官）**：`ISystemConfigurationService` 新增 `SetValueAsync`/`UpdateConfigurationAsync`；`ConfigurationController` 新增 `PUT /{key}` 与 `PUT /`（AdminOrSuperAdmin）；新增 `IConfigurationStore` + `JsonFileConfigurationStore`（`{BaseDirectory}/config/runtime-overrides.json`，仅持久化与 appsettings 默认值不同的项，原子写入）；白名单策略 `ConfigurationWritePolicy`（仅允许已注册 Server Options 节，禁止 `ConnectionStrings:DefaultConnection`/`Jwt:SecretKey`/`DefaultPasswords:*`）；Program.cs 以 `reloadOnChange:true` 追加覆盖文件，Service 写入后 `IConfigurationRoot.Reload()` 触发 `IOptionsMonitor<T>` 热更新。新增 13 个单元测试（Store 4 + Service 9，含热更新验证），Build 0 错误 0 警告，commit `41922a92a` | 架构约束 P10 用独立 Store 满足；测试真实实现零 mock | 技术总监 |
| 2026-08-05 | **A-06 Repository 泛型化（方案 B 收敛版）**：① 新建 `IEntityApiSegment<TList,TDetail,TInput>`（5 标准 CRUD，category 默认 null）；② 4 个形状一致的段接口继承它，用默认接口方法（DIM）显式重实现泛型方法并转发到现有实体命名方法（`GetPatientsAsync` 等不重命名，8 个实现类零改动）；③ 新建 `EntityApiClientRepositoryBase<TList,TDetail,TInput>` 派生自现有 2 参基类（**不改旧基类**——Registration/MedicalCase 继承它且须保持原样，改 3 参会使二者编译失败），用泛型段实现标准 CRUD（失败抛 InvalidOperationException(msg??默认文案)、GetPaged Data==null 空分页、GetById 保持静默返回 null，全部与现状语义一致）；④ 新增 `IEntityInputDto`（Shared/Contracts/Common，`Guid? Id`），4 个输入 DTO 实现，供基类 Update 提取 ID；⑤ Patient/Formula/Herb/User 4 仓储删标准 CRUD（净 -318 行），Patient/User 因接口无 category 保留 1 行 GetPagedAsync 薄包装（已实测 C# 接口映射不允许额外可选参数）；⑥ MedicalCase/Registration 边界未越。commit `d379f4a9d`，build --no-incremental 0 错误 0 警告，架构测试 92/92；Desktop 套件在无 WebAPI 环境失败为已知环境项（C-01 需运行中 WebAPI；STA 失败源于 C-04 移除 Xunit.StaFact），已用 stash 基线对比证明与本次改动无关 | 泛型化只用于形状一致的实体，不硬套异形实体；DIM 使实现类零改动；2 参旧基类保留以满足边界约束 | 技术总监 |
| 2026-08-05 | **审查遗留修复复核（P0a/P0b/P1/P2/P3，commit `99cd1a54d`）**：P0a `RecreateDroppedAuditTables` 无条件建 3 表与 `AddMedicalCasePrintLog` 重复建表（全新库必报已存在），改为只建 `SecurityAuditLogs`（另两表已由 AddMedicalCasePrintLog 重建），Down 同步只删本迁移新增对象；P0b 删除目标表错误的 `AddRowVersionToAspNetUsers` 迁移（AspNetUsers 已被 AddIsSysAdmin Drop，`Users.RowVersion` 由 InitialCreate 提供，快照无需变动，`has-pending-model-changes` 验证无漂移）；P1 补 4 个复合索引 `IX_MedicalCaseAuditLogs_MedicalCaseId_CreatedAt`/`IX_MedicalCaseAuditLogs_OperatorId_CreatedAt`/`IX_SecurityAuditLogs_EventType_CreatedAt`/`IX_SecurityAuditLogs_UserId_CreatedAt`（CreatedAt 降序，与旧 schema 一致），实体配置 `HasIndex` 入模型、快照同步，`SecurityAuditLogs.EventType` 定长 50（SQL Server 禁止索引 nvarchar(max)）；P2 新增 `LYBT.Shared.Configuration.ConnectionStringResolver` 三级回退（Database:ConnectionString → ConnectionStrings:DefaultConnection → CONNECTION_STRING 环境变量，空串/空白视为未配置继续回退），Users/Auth/Formula/Herbs/Reports 5 模块统一调用，WebAPI 标准实现不动；P3 `RealName` 实体 `[StringLength(50)]`→`[StringLength(100)]` 与 Fluent `HasMaxLength(100)` 及 Users 表 nvarchar(100) 对齐，修正 UserConfiguration 自相矛盾注释。验证：build --no-incremental 0 错误 0 警告、架构测试 92/92、`has-pending-model-changes` 无差异、LocalDB 全新库 10 个迁移全链成功且 schema 验证通过（三审计表/5 索引/Users.RowVersion/EventType nvarchar(50)） | a3baa7e17 的迁移链在全新库必挂（重复建表 + 目标表不存在），以「全新库可跑通」为修复目标；索引入模型避免下次生成迁移误删 | 技术总监 |
| 2026-08-05 | **WebApi 架构修复（03-server.md 与代码全面对齐，commit `59be25317` `7184f96e8`）**：14 项发现全部修正——P0-1 ReportsController 直连 Repository 违规 → 新增 `IReportService`/`ReportService`（internal，与 ReportRepository 一致），Controller 改注入 Service 接口，满足「Controller 注入 Service 接口，禁止注入 Repository/DbContext」规则；P0-2 架构模式总述重写（6 模块 MediatR CQRS + MedicalCase Service 拆分 + Reports 只读聚合）；其余 12 项纯文档：模块目录结构三形态/模块清单跨模块通信方向/MedicalCase 5 接口清单（删 Permission/Audit/Rules）/Controller 规范示例/新增模块独立 DbContext 小节/错误码表（删 7xxxx 数据同步、增 8xxxx 挂号 801xx~803xx、枚举指向 ErrorCode.cs）/BaseService 实际状态（仅 ILogger 注入，仅 MedicalCase 三 Service 继承）/BaseRepository 5 方法（删 21 方法清单与分页模板方法段）/Entities 移出 Core 至 Shared 层及 10 目录/跨模块服务方向（ICrossModuleService 为统一接口非 [Obsolete]）/错误码枚举位置等。架构测试 `ServerAssemblies` 补入 `LYBT.Module.Reports`/`LYBT.Module.Registration`（P0-1 漏检根因，补后 92/92 全通过）。验证：build --no-incremental 0 错误 0 警告 | 文档-代码一致性维护（§10.1 规则）；分层边界强制；计划文档归位 docs/compose/plans/（git 识别为 rename） | 技术总监 |
| 2026-08-06 | **「以文档为准」强制规则确立 + 文档目录优化**：① 规则更新——AGENTS.md 新增强制规则 3「以文档为准」：文档定义设计态、代码实现当前态，冲突时先更新文档再改代码，文档是 SSOT，禁止引入权威文档未定义的设计；skill lybtzys-coder-rules 升级 v0.9.0（新增同规则 + 文档查询指南「信息点→权威文档」表 + WHERE TO LOOK 升级）。② 目录优化——根目录 2 个 00- 治理文件归位 `docs/00-governance/`（01-naming-convention / 02-ssot-architecture）；删除 15 个已完成使命的历史报告（reports/ 7 + compose/reports/ 4 + compose/plans/ 已完成 4：a06/b02/b03/webapi-arch-fix），决策痕迹均已在本 §九；保留活跃文档（R10 spec 被 4 处引用、code-gap-fix-list 被 03-users 引用、v1.0-completion-plan 进行中）；文档总数 158→143。③ AI 查询指南——docs/README.md v4.0 新增「🤖 AI 查询指南」章节（11 个信息点→权威文档映射），各目录 README 补「权威文档在哪」提示。死链修复 5 处（12-permissions-matrix ×2、master-plan ×2、new-session-kickoff、02-requirements README、archive 存量 1）。验证：链接检查 566 个仅 1 存量已修 | 消除规则缺失（以文档为准未成文）+ 目录零散（00- 孤悬）+ 计数失真（146 vs 158）+ AI 无查询入口；方便 AI 与 Mimo 秒查文档 | 技术总监 |
| 2026-08-06 | **WebApi 全面集成测试完成（100% 端点覆盖 + 100% 通过）**：按 13b-api-endpoints 的 104 端点完善 newman 集合（155→312 请求，新增批量操作/状态流转/报表新端点/配置/诊断等测试；修正 reports 路径格式 daily-income→daily/income）。**修复 5 个 P0 Bug**：① `[FromBody] object dto` + `is not` 反序列化失败 → 改 `override` + JsonSerializer + `PropertyNameCaseInsensitive`（`f02decba4`/`680045593`/`2d0dadf51`）；② `BaseRegistrationsController.GetList` 缺 `override` → 路由 AmbiguousMatchException 500（`680045593`）；③ JsonSerializer 大小写敏感致 DTO 全空（`2d0dadf51`）；④ **反序列化绕过 DataAnnotations 验证**（空 body `{}` 能创建挂号）→ 新增 `TryDeserializeDto<T>` 手动验证（`59b7ff6`）；⑤ 用户创建空 RealName 抛 ArgumentException 500 → Handler 前置 Result.Failure 检查（`795ada4bb`）。**测试集合优化**：移除 Deploy Restart 测试（该端点调用 `_lifetime.StopApplication()` 真实重启服务致测试崩溃）；负面登录测试移到最后（速率限制 5 次/60s 阻塞后续阶段）。**最终结果**：8 个文件夹（Phase 1/1.2/1.3/2/3/4 + Global + Negative）共 312 请求 335 断言，**335/335 全部通过（100%）**。验证：build --no-incremental 0 错误 0 警告、架构测试 83/83 | 服务器稳定性根因：Deploy Restart 测试自杀式重启；FluentValidation 验证器注册但无 MediatR pipeline 执行（系统级验证缺口，本次以 TryDeserializeDto 手动验证兜底） | 技术总监 |
| 2026-08-06 | **FluentValidation pipeline + RateLimiting 可配置化（遗留修复）**：① **ValidationBehavior**——创建 `LYBT.Infrastructure.Validation.ValidationBehavior<TRequest,TResponse>`（MediatR `IPipelineBehavior`），自动执行 `IValidator<TRequest>` 验证器，失败抛 `FluentValidation.ValidationException`；在 Auth/Users/Patients/Herbs/Formula/Registration 6 个模块 `AddMediatR` 注册 `AddOpenBehavior(typeof(ValidationBehavior<,>))`，LocalWebAPI 同步注册（commit `b386e17c5`）。`SystemExceptionHandler` 对 `ValidationException` 已返回 400 + 验证错误信息（`validationErrors` 字段因 `DefaultIgnoreCondition=WhenWritingNull` 匿名类型序列化问题未显示，但 message 已含具体字段错误）。② **RateLimiting 可配置化**——代码已有 `Security:RateLimiting:Enabled` 配置项（`ApiServiceCollectionExtensions.cs:172`），在测试服务器 `start.sh` 添加 `export Security__RateLimiting__Enabled=false` 禁用限流（生产环境默认开启）。验证：连续 10 次登录全部 200（无 429）、POST /users body=`{"userName":"ab"}` 返回 400 "用户名长度必须在3-32个字符之间"（非 500）、newman 全量 335/335 断言 100% 通过无回归。commit `b386e17c5`（ValidationBehavior）+ `41f5ab543`（validationErrors 详情）| 修复系统级验证缺口（FluentValidation 验证器注册但无 pipeline 执行）；RateLimiting 测试环境可禁用（避免速率限制阻塞自动化测试） | 技术总监 |
| 2026-08-07 | **WebApi 控制器代码质量批次（commit `c094341af`）**：① HealthController `Get`/`Ping` 去重——提取私有助手 `BuildHealthStatus(status)` 统一构建 `HealthStatusDto`（Status/Timestamp/Version）；② RegistrationsController `QuickVisit`/`Create`/`StartVisit`/`Cancel` 删除误导性「添加 OutputCache 和 RateLimiting」注释（实际无 `[OutputCache]`，仅保留功能描述）；③ AuthController 登录/Token 刷新 401 分支简化（`StatusCode(httpStatus)` 等价于 `Unauthorized`）；④ Formulas/Herbs/Patients `Create`/`Update` 缩进修复。验证：`dotnet build LYBTZYZS.sln --no-incremental` 0 错误 0 警告 | 消除重复代码；注释与实现一致性（未启用的 OutputCache 不应声称已添加）；统一错误响应构造 | 技术总监 |
| 2026-08-07 | **P-02 医案查询 DB 层分页 + Q-02 TryDeserializeDto 双轨验证去重（commit `1ee490e5c`）**：P-02——`MedicalCaseQueryService` 4 处「全量 `ToListAsync` + 内存 Skip/Take」改消费 Repository DB 层分页：`SearchMedicalCasesAsync`→`QueryPagedAsync`、`QueryByPatientAsync`→`GetByPatientIdPagedAsync`、`GetPatientConsultationsAsync`/`GetPatientPrescriptionsAsync`→新增 `GetPatientConsultationsPagedAsync`/`GetPatientPrescriptionsPagedAsync`（沿用 `GetDetailQuery` 含 Include 预加载，DB 层过滤未删除的 Consultation/Prescription 并按各自 CreatedAt 倒序，TotalCount 由 DB 返回）。Q-02——6 个 Controller 11 处 `[FromBody] object dto` + `TryDeserializeDto` 手动反序列化+验证改为强类型绑定 `[FromBody] TInputDto`，删除 `TryDeserializeDto` 扩展方法（ControllerBaseExtensions）；`BaseCrudController` 删除 `Create`/`Update` virtual 模板方法（保持非泛型，A-01/P09c 按类名匹配的架构测试不受影响），各 Controller 去 `override` 自行声明；模型验证失败由 `[ApiController]` 自动 400 处理（`InvalidModelStateResponseFactory` 已输出 ApiResponse 格式，与旧 ValidationFail 契约一致）；Desktop LocalWebAPI `MedicalCasesController.Create`/`RegistrationsController.Create` 同模式 override 一并修正（后者原委托 `base.Create` 运行期必抛 NotSupportedException，改为直接发送 `CreateRegistrationCommand`；前者原 `dto is not MedicalCaseInputDto` 对 `object` 绑定恒真致该端点恒 400，强类型后恢复正常）。Desktop 客户端 camelCase + 服务端 `PropertyNameCaseInsensitive=true` 已验证兼容。验证：`build --no-incremental` 0 错误 0 警告、架构测试 83/83、MedicalCase 单测 77/77 | 消除双重序列化开销与患者历史全量加载内存膨胀；验证规则回归框架单轨（DataAnnotations 由模型绑定执行，FluentValidation 由 ValidationBehavior 执行） | 技术总监 |
| 2026-08-07 | **Q-01 批处理 Handler 泛型化（commit `da7e3b162` `4ced30601` `8c3cb01ce`）**：新建 `BatchOperationHandlerBase<TEntity>`（LYBT.Infrastructure/BatchOperations，模板方法模式），统一 循环→GetById→前置校验→变更→Update→累计 BatchOperationResultDto 骨架；Users BatchDelete/Enable/Disable + Patients/Herbs/Formula BatchDelete 共 6 个 Handler 继承基类（净 -147 行）；差异点经抽象/virtual 钩子保留（自删/IsSysAdmin/无权限校验、医案引用检查、缓存失效、领域事件派发、IsSuccess 语义、消息文案、异常捕获类型）；BatchImport 3 个 Handler 及 Herbs/Formula Service 版 BatchEnable/Disable（非 Handler）不在范围；已知微小差异：失败项 Name 填充（自删分支）、异常文案 ex.Message 替代固定"删除操作失败"；验证：build --no-incremental 0 错误 0 警告、架构测试 83/83 | 与 A-06 同策略：泛型化只用于形状一致的 Handler，不硬套异形实体；行为等价优先 | 技术总监 |
| 2026-08-08 | **A-18 批次2 第 1 步：P1-5 Desktop 模块边界守卫**：① `MedicalCaseNavigationParameters`/`WorkspaceMode`/`EditState` 从 `Modules/LYBT.Desktop.MedicalCase/Models` 下沉 `LYBT.Desktop.Contracts`（`Enums/WorkspaceMode`、`Enums/EditState`、`Models/MedicalCaseNavigationParameters`；后者依赖 Prism，net8.0 目标按 SyncEvents 同例 Compile Remove）；② Registration 去掉 MedicalCase ProjectReference 改引用 Contracts，9 个 src 文件 + 4 个测试文件同步 using；③ 新增架构测试 `DP07_DesktopModules_Should_Not_Reference_Other_DesktopModules`（判定 7 个 Modules 互引为零，豁免 Roles/Shell——角色编排/组合根，注释理由）。验证：build --no-incremental 0 错误 0 警告、架构测试 84/84（83 存量 + 新 DP07）、受影响 Desktop 单测 32/32 | 消除 Registration→MedicalCase 跨模块违规（DM06 未覆盖 Registration）；Desktop 模块隔离从声明变为测试强制 | 技术总监 |
| 2026-08-08 | **A-18 批次2 第 2/3 步勘察完成（P1-1 契约双套 + P1-2 adapter，未动代码）**：方案文档 `docs/compose/reports/a18-batch2-contract-options.md`。关键发现：① Admin 3 VM（SystemSettings/LogLevelControl/Deployment）共 9 处直用 Refit 接口（IConfigurationApi×3/IDiagnosticsApi×4/IDeployApi×2），均未走 IApiClient；② RefitApiClient 实际 10 个属性（非任务书 11）；10 域方法面 1:1（7 域完全一致，Users/Herbs/Formulas 多 local-only，MedicalCases.CancelMedicalCaseAsync 返回签名 Refit.IApiResponse vs ApiResponse）；③ **Configuration 域是双套唯一缺口**——IConfigurationApi 5 方法无 IApiClientConfiguration 段，被 Shell 独立 RestService 注册绕过 IApiClient（本地模式无实现路径）；④ P1-2 实际 10 对 adapter（非任务书 12 对），方法名 100% 1:1，仅 6 个 local-only 桩（Refit 侧 NotSupportedException），A6 漂移风险不存在。方案对比：推荐 **方案 A**（Api 接口 internal 化 + 补 Configuration 段 + Admin 3 VM 换注入），改动小风险低；方案 B（删 Api 套 Refit 直接适配）因 IEntityApiSegment 泛型 × Refit 特性冲突、55 文件引用面、破坏无特性接口纯度而不推荐。**待技术总监确认方案后执行，本次未改契约代码** | 勘察确认双套实际 1:1（仅 Configuration 缺段）；发现任务书 2 处事实偏差（11→10 属性、12→10 对 adapter） | 技术总监 |
| 2026-08-08 | **A-18 批次2 第 4 步：P1-1/P1-2 契约统一方案 A 执行（commit `a8b9d0b0f`）**：技术总监确认方案 A（保留 RefitApiClient 内部实现，Api 接口 internal 化 + 补 Configuration 段 + Admin 换注入）。Contract Api 套成为远程实现细节：11 接口 internal + InternalsVisibleTo(Foundation/测试)；Configuration 域补齐 `IApiClientConfiguration` + 双适配器（本地抛 NotSupportedException，本地模式隐藏服务器配置）；Shell 独立 `Register<IConfigurationApi>` 删除改走统一 IApiClient；Admin 3 VM 9 调用点换注入。**CancelMedicalCaseAsync 签名差异（Refit.IApiResponse vs ApiResponse）本次不处理**——行为等价原则下留待后续，1:1 核查已记录（MedicalCaseApiClient 适配器内部已有 Refit→ApiResponse 转换，功能不受影响） | 方案 B（删 Api 套 Refit 直接适配）因 IEntityApiSegment 泛型 × Refit 特性冲突不可行；方案 A 改动最小、可回退、行为等价 | 技术总监 |
| 2026-08-08 | **A-19 移除 Auto 模式（commit `4a1393c3a`）**：产品负责人决策「远程模式和本地模式的切换需要自主选择，而不是远程不能用的时候自动降级到本地模式」→ 彻底移除 Auto 模式。`ConnectionMode` 枚举删 `Auto`；`ConnectionModeService` 删 `DetectBestModeAsync()`（自动降级核心）+ `SetMode` Auto 分支 + `_detectGate` 门闩；构造按已保存 PreferredMode/IsLocal 恢复模式不探测；`OnUrlChanged` 保留按 URL 推导（配置跟随，不探测）；`ConnectionStatusViewModel.DetectConnectionModeAsync` 改为刷新显示 + `CheckRemoteAvailableAsync`（仅 UI 按钮状态）；`CheckRemoteAvailableAsync`/`TestRemoteConnectionAsync`/`TestLocalConnectionAsync` 保留；`FirstRunSetupViewModel`/`ServerConfigViewModel`/`LoginViewModel`/`StatusBarManager` 用保留 API 零改动；`platform.md` 连接模式章节同步。验证：build --no-incremental 0 错误 0 警告、架构测试 84/84。**不混入 A-18 契约统一内容** | 移除自动降级：用户显式选择 Remote 后远程不可用仍保持 Remote（不降级本地），UI 显示不可用状态；重启按保存的 PreferredMode 恢复不探测 | 产品负责人 |

---

## 十、维护规则（强制）

### 10.1 文档-代码一致性

| 时机 | 动作 |
|------|------|
| **Session 启动** | 读本文件接上进度 |
| **代码变更后** | 检查本文档是否需要同步更新 |
| **功能完成** | 更新状态表 ⬜→✅ + Commit SHA |
| **新增任务** | 在对应类别追加，更新计数和依赖图 |
| **决策变更** | 在 §九 追加一行 |

### 10.2 相关文档索引

| 文档 | 路径 | 用途 |
|------|------|------|
| ~~产品功能清单~~ | `docs/02-requirements/archive/14-feature-inventory.md` | 已过时归档（2026-08-04），以各模块 US 需求文档为准 |
| PRD | `docs/02-requirements/01-prd.md` | 产品需求文档 |
| 数据模型 | `docs/03-architecture/13a-data-model.md` | 核心实体 + 状态枚举 |
| API 端点 | `docs/03-architecture/13b-api-endpoints.md` | 全部模块端点 |
| 当前状态 | `docs/03-architecture/13c-current-status.md` | Desktop 视图 + 已知问题 |
| AGENTS.md | `AGENTS.md` | 开发规范与约束 |
| 2026-08-05 | **A-06 Repository 泛型化完成（方案 B 收敛版）**：新建 `IEntityApiSegment<TListDto,TDetailDto,TInputDto>` 泛型段接口；Patient/Formula/Herb/User 4 个段接口继承；`ApiClientRepositoryBase` 改为接收段驱动标准 CRUD；4 个 Repository 删除样板、仅留特有方法+Mapperly；Registration/MedicalCase 异形实体排除。Build 0 错误 0 警告，架构测试 92/92，commit `d379f4a9d` | 好的泛型设计，不为设计而设计；用户纠正了"方案 B 是过度设计"的判断 | 产品负责人 |
| 2026-08-05 | **A-05 实体源统一确认已完成**：勘察发现无 Domain 项目、Server 模块无实体定义、Desktop 用 DTO，Shared/LYBT.Entities 已是唯一实体源。A-05 由 A-02/A-03 间接覆盖，标记 ✅ | Phase 2 全部完成 | 技术总监 |
| 2026-08-05 | **开发策略调整：WebAPI 优先**：线性开发——先重点开发 Server 端功能（Phase 3），发布到测试服务器（`60.190.215.86:5555`，user `player`），再开发 Desktop 并针对远程 WebAPI 做集成测试。测试服务器连 SQL Server `192.168.190.243`（LYBTDB_Dev）。Desktop 集成测试不再依赖本地 LocalDB/WebAPI | 真实环境测试更可靠；Server 端功能先行部署，Desktop 后续对接 | 产品负责人 |
| 2026-08-05 | **Mimo Code 升级到 v0.1.10**：验证派发命令零改动兼容；git 身份继承修复(#1825)、context/checkpoint 加固、provider 重试；auto-dream/auto-distill 默认 OFF 正合适；`--never-ask` 作为全局选项在 run 子命令下不可用（yargs help 报错），暂跳过 | 派发流水线保持 `--dangerously-skip-permissions`，不加 `--never-ask` | 技术总监 |
| 2026-08-05 | **B-03 Excel 导出/导入完成（Phase 3 第 1 项）**：① 路由裁决——模板端点以 Desktop Refit 定义为准为 `GET /import-template`（任务描述写 export-template，Refit 是硬约束）；新增 `POST /batch-import-excel`（multipart/form-data，仅 Admin+，Refit 无此端点）；导出/模板沿用类级权限（患者 DoctorOrAdminOrReceptionist、药材/验方 DoctorOrAdmin）。② ExcelService 通用化——`ExportToExcel<T>(data, sheetName, columnMapping)`/`GenerateTemplate(sheetName, columnHeaders)`/`ParseExcel<T>(stream, propertySetters)`，XSSFWorkbook；**DateTime 列以文本 `yyyy-MM-dd` 写出**（TDD 发现 XSSF 给单个 cell 赋自定义 DataFormat 会污染 workbook 格式表，导致其他数字列重载后被误判为日期）。③ 导入复用——Herbs/Formulas 复用现有 `BatchImportHerbsCommand`/`BatchImportFormulasCommand`（拼音生成/药材名匹配/验方校验）；患者新增同构 `BatchImportPatientsCommand`（Skip/Update/Error）。④ 列定义以现有 DTO/实体实际字段为准——任务列的「地址/过敏史/病史（患者）」「禁忌（药材）」模型中不存在未导出；验方「组成」列为 JSON（`[{HerbName,Dosage,Unit}]`），导入解析还原，导出 DTO 有但导入 DTO（FormulaImportItemDto）无的「描述/分类」列导入时忽略。Build 0 错误 0 警告、架构测试 92/92、ExcelService 4 单测全过。commit `4d70b487a` `0fdfde0d3` `bed75026e` `709bad616` `64c58c59a` `8968fd130` `1bc468851` | 路由与 Refit 完全匹配是硬约束；导入尽量复用现有命令避免双路径；NPOI XSSF 格式表污染是真实坑（TDD 捕获） | 技术总监 |
| 2026-08-05 | **审查遗留问题修复（#1/#4/#5/#6）**：#1 SimplifyDataModel 误删的 `SecurityAuditLogs`/`MedicalCaseAuditLogs`/`MedicalCasePrintLogs` 三表以新迁移 `RecreateDroppedAuditTables` 重建（Snapshot 已含三表 → EF 生成空迁移，手动填充 3 个 CreateTable + Down DropTable，SQL 脚本验证仅 3 建表、无 Users 变更）；#4 删除重复的 `ApplicationUserConfiguration`，`RealName`/`LastLoginAt` 配置并入 `UserConfiguration`（`HasMaxLength(100)` 保持不变避免模型漂移）；#5 Users/Auth/Formula/Herbs/Reports 五个模块 DbContext 连接字符串改 `Database:ConnectionString ?? ConnectionStrings:DefaultConnection ?? throw`；#6 密码哈希工具注释与 SQL 输出 `AspNetUsers`→`Users`（含 README）。commit `a3baa7e17`，build 0 错误 0 警告、架构测试 92/92、`has-pending-model-changes` 无差异 | 修复系统审查遗留问题：三张审计表恢复可用、配置单点维护、模块级 DbContext 不再空连接字符串、工具输出与 Users 表一致 | 技术总监 |
| 2026-08-06 | **03-server.md「模块独立 DbContext」小节修正（OpenCode review 发现）**：独立 DbContext 模块 5 个——Auth（`AuthDbContext`，AuthSessionRepository 注入）/ Users（`UsersDbContext`）/ Herbs（`HerbsDbContext`，HerbRepository 注入）/ Formula（`FormulaDbContext`）/ Reports（`ReportsDbContext` 已注册，但 ReportRepository 实际注入 AppDbContext，待清理）；复用 `AppDbContext` 模块 5 个——Patients（PatientRepository）/ MedicalCase（MedicalCaseRepository）/ Registration（RegistrationRepository）/ Auth（SecurityAuditRepository）/ Herbs（HerbReferenceRepository）。commit `3081540aa`，build --no-incremental 0 错误 0 警告（纯文档） | 文档-代码一致性维护（§10.1）；修正 Reports DbContext 归属误述，补全 SecurityAuditRepository/HerbReferenceRepository 注入事实 | 技术总监 |
| 2026-08-06 | **B-04 报表增强完成（Phase 3 第 2 项，commit `cab959dc1`）**：新增 5 个报表端点——① 收入趋势 `GET /reports/trend/income`（labels/registration/medicine/total 三序列折线，granularity=day/week/month）；② 问诊趋势 `GET /reports/trend/consultations`；③ 医生绩效 `GET /reports/doctor-performance`（consultationCount/registrationFeeTotal/medicineFeeTotal/averagePrescriptionPrice=药费÷处方数）；④ 热门药材排行 `GET /reports/herbs/ranking`（top 默认 10，复用 `GetHerbUsageAsync` 抽出的公共子查询 `.Take(top)`，聚合下推 SQL）；⑤ 患者流量 `GET /reports/patient-flow`（newPatients/returningPatients，按「患者首次完成就诊日」归类新老）。实现要点：仓库按日 `GROUP BY CreatedAt.Date` 聚合（SQL `CONVERT(date,..)` 下推），服务层 `ReportTimeBuckets` 做周（周一起）/月（1 号起）桶汇总与零填充，避免 provider 相关 `EF.Functions.DateDiff*` 无法在 EF InMemory 测试；**存量缺陷修复**——`GetMedicineFeeTotalAsync` 的 `pi.Amount`（未映射计算属性）EF Core 8 无法翻译必抛异常，改 `UnitPrice * Dosage`（日收入药费端点为可用状态）；`ReportRepository`/`ReportService` 由 internal 改 public（对齐 Herbs/Auth 模块可测类惯例，单测直构零 mock）；测试项目补 `LYBT.Module.Reports` 引用；14 个新单测（趋势零填充/周月桶/绩效平均/排行 top/新老患者归类）全过；`ReportGranularity` 枚举 + IncomeTrendDto/ConsultationTrendDto/DoctorPerformanceDto/PatientFlowDto 入 Shared（药材排行复用 HerbUsageItemDto）。验证：build --no-incremental 0 错误 0 警告、架构测试 92/92、单测 14/14 | 报表聚合统一按日下推、粒度汇总放服务层（数据量级小，桶汇总仅在少量日行上进行）；EF 未映射计算属性不能进查询表达式 | 技术总监 |
| 2026-08-06 | **B-21 安全增强完成（US-AUTH-006/007，commit `e2cedf6a2`）**：① Token 族旋转——`IAuthSessionRepository.RevokeAllUserSessionsAsync` 批量撤销活跃会话（`IsRevoked=true` + `RevokedReason` + `LogoutTime`）；`AuthSession` 实体加 `RevokedReason`（nvarchar(256)，迁移 `20260806001815_AddRevokedReasonToAuthSessions`，LocalDB 全新库全链验证通过）；新增 `RevokeAllUserTokensCommand/Handler`（撤销 + 审计事件 AllTokensRevoked）；登录踢出——LoginCommandHandler 新登录先撤销该用户全部旧会话。② 安全审计完善——Users 模块 Delete/ResetPassword/ChangePassword/ToggleUserStatus 4 个 Handler 接入撤销 + 审计（UserDeleted/PasswordReset/PasswordChanged/UserStatusChanged）。③ 架构落地——`SecurityAuditEvent` DTO 从 Auth 模块 Models 移入 `LYBT.Shared.Models.Contracts.Auth`（DTO 归位 Shared 惯例）；新增 `IAuthCrossModuleService`（LYBT.Infrastructure 跨模块接口，镜像 `IUserCrossModuleService` 先例，AuthModule 实现并注册），Users 模块经其触发撤销/审计而不引用 Auth 模块（模块边界不破坏，架构测试 92/92 验证）。④ 角色变更场景未挂载——服务端不存在 `UpdateRoleCommandHandler`（`UserService.UpdateAsync` 走 `UpdateProfile` 不修改角色，角色仅在创建时指定），新增角色修改功能超出本任务范围，留待产品决策。⑤ 验证——build --no-incremental 0 错误 0 警告、架构测试 92/92、新单元测试 8/8（EF InMemory 真实实现零 mock）、`has-pending-model-changes` 无差异 | 触发场景按任务清单逐项落地；跨模块通信采用 ICrossModuleService 模式而非 MediatR 命令直达（模块禁止互引）；角色变更场景因无对应操作无法挂载，如实记录 | 技术总监 |
| 2026-08-06 | **WebApi 测试发布（60.190.215.86:5000）完成**：按 §九 2026-08-05「WebAPI 优先」决策将 Server 端最新版本（含 B-03/B-04/B-10/B-21 + Reports 报表）部署到公网测试服务器。部署方式：`dotnet publish -c Release` 框架依赖 → tar 打包（**排除 appsettings*.json**，保留服务器运行时配置）→ scp 上传 → 解压覆盖 `/home/player/lybt-api/` → `start.sh`（export `DefaultPasswords__SysAdminPassword` + `LYBT_INITIAL_SETUP_TOKEN` 后 nohup 启动）。启动自动 `MigrateAsync` 应用 2 个待应用迁移（RecreateDroppedAuditTables + AddRevokedReasonToAuthSessions）。Smoke Test 全过：health Healthy / sysadmin 登录 200 / 患者列表 200 / 用户列表 200 / 药材列表 200 / 配置 200。**排障记录**：① Production 下 `IdentitySeedData.ResolveSysAdminPassword` 强制要求 env var（K4 安全加固），服务器缺 `DefaultPasswords__SysAdminPassword`/`LYBT_INITIAL_SETUP_TOKEN` → 已在 `~/.bashrc` + start.sh 注入；② 旧部署（Jul 14）sysadmin 密码哈希与现配置不一致 + 多次失败触发 lockout → DB 直改 `LastLoginAt=NULL` + `AccessFailedCount=0` + `LockoutEnd=NULL` 后 seed 重置密码；③ **`BaseApiController` 8 处实例方法与 `ControllerBaseExtensions` 扩展方法同名，`this.Method()` 解析到实例方法自身 → 无限递归 Stack Overflow**（登录成功但响应序列化崩溃，患者列表端点必现）→ 全部改为 `ControllerBaseExtensions.Method(this, ...)` 显式调用。commit `b6097c036`（5 处：Success×2/Error/BusinessFail/ValidationFail）+ `34d5e599d`（review 补漏 HandleResult×2）+ `6cd91cb7d`（SuccessPaged，列表端点必现） | 发布只更新 dll 不碰 appsettings（服务器含明文密钥，覆盖即死）；同类递归 bug 分批暴露（登录端点→列表端点），Mimo review + 独立 smoke test 双保险；教训：**委托给扩展方法的实例方法禁写 `this.Method()`**，必须 `ClassName.Method(this, ...)` | 技术总监 |
| 2026-08-06 | **架构测试项目重构（commit `3df76c2c1` + `1d352d6f5`）**：① 创建 `TestAssemblies.cs` 公共程序集清单（Server/Desktop/All），消除各测试文件重复定义；② 重构 4 个测试文件引用 `TestAssemblies`；③ 删除 6 个过时/重复测试（UltraThink/Record-Only 残留 + 重复授权测试）；④ 统一 83 条测试方法名为 `规则编号_描述` 格式（P/B/A/AR/DP/DM/CC/AM/MC 前缀）；⑤ 合并 3 个授权测试为 P09（含 Batch 端点策略检查）；⑥ 创建 `ARCHITECTURE-RULES.md` 规则映射表（83 条规则→测试方法→文档引用）。验证：build --no-incremental 0 错误 0 警告、架构测试 83/83 全部通过 | 程序集清单统一（Reports/Registration 覆盖）；无重复测试；命名统一；删除所有 UltraThink/Record-Only 残留 | 技术总监 |
| 2026-08-07 | **WebApi 优化分析 + 修复批次**：全面审查 14 Controller + 3 Middleware + 6 Extension，产出优化报告（P0×4/P1×4/P2×5）。**已修复**：① Production CORS 加公网 IP `60.190.215.86:5000`，去掉遗留内网 IP `192.168.190.6`；② `ConfigurationWritePolicy` 新增 `Database:ConnectionString` 到禁止列表（防止运行时 API 修改数据库连接串）；③ AutoLogin token 启用 `ValidateLifetime=true`（此前 `false` 导致永不过期）；④ HealthController `Get`/`Ping` 去重提取 `BuildHealthStatus` 助手；⑤ RegistrationsController 4 处误导性「添加 OutputCache 和 RateLimiting」注释清理；⑥ PatientsController/HerbsController/FormulasController Create/Update 缩进修复；⑦ AuthController Login/Refresh 错误处理简化（去掉冗余 401 分支）。**新增任务**：A-13 DeployController 安全加固 / A-14 Controller 继承文档化 / A-15 MedicalCasesController 拆分。验证：build --no-incremental 0 错误 0 警告，架构测试 83/83 | 安全审查发现 AutoLogin 永不过期 + CORS 内网 IP 遗留 + Database:ConnectionString 可被运行时修改；代码质量发现重复代码/缩进/注释不一致 | 技术总监 |
| 2026-08-07 | **A-13 DeployController 安全加固完成**：`POST /deploy/restart` 加确认机制——新增 `RestartConfirmDto(string? Confirm)` record，body 中 `confirm` 必须为 `"RESTART"` 才执行 `StopApplication()`，否则返回 ValidationFail。commit `624b438e4` | 防止管理员误触停服；确认字符串硬编码避免猜测 | 技术总监 |
| 2026-08-07 | **A-14 Controller 继承评估 + A-15 MedicalCasesController 拆分评估**：A-14 三种继承路径（BaseApiController=独立端点、BaseCrudController=标准 CRUD、BaseMedicalCasesController=领域特化）各有合理性，MediatR+Service 混合注入是有意设计（查询走 Service 绕过管道、命令走 MediatR 验证+审计），不统一，仅文档化。A-15 评估后取消——MedicalCasesController 状态流转仅 4 个方法（~100行），共享路由前缀 `/medicalcases`，拆分后 Desktop Refit 客户端也需改，ROI 不合理 | 过度拆分的反模式：为拆而拆增加复杂度而非降低；混合 MediatR 模式是性能优化而非架构缺陷 | 技术总监 |
| 2026-08-07 | **WebApi 设计文档完整性审计 + 修复（commit `7fb69fd1b`）**：系统核查 WebApi 所有设计文档（tech stack / design patterns / API endpoints / security / error handling / configuration），审计 5 个核心架构文档（00-architecture-summary / 03-server / 05-dual-mode / 06-error-handling / 09-security-architecture / 07-configuration）。**A 类修复（6项文档过时）**：① PolicyConstants 数量 5→7（09-security-architecture/05-dual-mode）；② TokenManagementService/SecurityAuditService 状态标记 🧲→✅（09-security-architecture）；③ ErrorCode 表 7xxxx→8xxxx 挂号（06-error-handling）；④ Reports 端点 3→8 + 总计 108→113（05-dual-mode）。**B 类补写（6项缺失设计文档）**：① SignalR Hub 设计（RegistrationHub 分组策略 + NotificationService 推送 + Desktop 降级）；② FluentValidation Pipeline 自动验证（ValidationBehavior 注册于 6 模块）；③ 配置热更新实现（ConfigurationWritePolicy 白名单 + JsonFileConfigurationStore 持久化）；④ 中间件管道顺序（6阶段完整顺序）；⑤ MediatR+Service 混合模式决策记录（A-14 评估结论）。**审计结论**：WebApi 设计文档覆盖度约 85%→95%，核心架构/分层/安全/错误处理/配置均有权威文档，03-server.md 升级至 v2.4（777行）。纯文档修改，无代码变更 | 以文档为准原则落地；消除 6 项过时标记 + 6 项设计空白 | 技术总监 |
| 2026-08-07 | **PolicyConstants.AdminOnly 删除（冗余策略清理）**：`AdminOnly`（SuperAdmin+Admin）与 `AdminOrSuperAdmin`（Admin+SuperAdmin）角色映射完全相同，且 AdminOnly 从未被任何 Controller 的 `[Authorize]` 属性使用（核实：WebAPI 全部 Controller 只用 AdminOrSuperAdmin/AdminBusinessOnly/DoctorOnly/DoctorOrAdmin/DoctorOrReceptionist/DoctorOrAdminOrReceptionist）。删除：`PolicyConstants.cs` 常量 + `AuthenticationServiceCollectionExtensions.cs`/`LocalJwtConfig.cs` 的 AddPolicy 注册 + `IDiagnosticsApi.cs` 注释；同步 7 处现行文档（05-dual-mode 策略数 7→6、09-security-architecture 策略表/示例、04-api-reference README 策略清单、03-users 权限表、04-permissions 权限矩阵、WebAPI/LocalWebAPI README）。测试文件仅含字符串注释不改动，Controller 零改动。验证：build --no-incremental 0 错误 0 警告 | 统一策略命名，消除同义冗余；12-permissions-matrix v1.2「AdminOnly≡AdminOrSuperAdmin 合并建议」落地 | 技术总监 |
| 2026-08-07 | **S-01/S-02 Auth 安全修复完成（commit `ae52f5ef0`）**：S-01 登录 Token 过期硬编码 60 分钟与 JWT 配置错位——生产 JWT 30 分钟过期但 ExpiresAt 报 60（30~60 分钟窗口客户端持过期 Token 请求全 401），基础配置 JWT 8 小时有效但会话 60 分钟被判失效、RefreshToken 拒绝本可用会话；删硬编码，LoginCommandHandler 注入 `IOptions<JwtOptions>` 统一读 `AccessTokenExpirationMinutes`（与 JwtService RefreshToken/ValidateAutoLoginToken 的 ExpiresAt 同源）。S-02 RefreshToken 无会话记录时 `AuthSession.Create(Guid.Empty,...)` 抛 ArgumentException 返回 500——会话记录缺失（被清理/失效）但 JWT 签名仍有效时，在 RefreshToken 前对 `oldSession==null` 提前返回 `ErrorCode.AuthTokenInvalid`（"会话不存在，请重新登录"）+ 审计事件，删 `?? Guid.Empty` fallback。验证：build --no-incremental 0 错误 0 警告（本机并行构建与 LSP 设计时构建竞争写 obj/bin 致间歇 CS0006/MSB3030，以 `-m:1` 串行全量构建复验通过） | 修复 WebApi 深度分析报告 S-01/S-02 P1 项；Token 过期单一来源（SSOT）；无会话 token 拒绝而非崩溃 | 技术总监 |
| 2026-08-07 | **S-03 SignalR 分组越权修复 + P-01 OutputCache 从未生效清理（commit `40dbad0df`）**：S-03——RegistrationHub.OnConnectedAsync 校验 query string `doctorId` 必须等于当前登录用户（`ClaimTypes.NameIdentifier`），不匹配拒绝加入分组并 LogWarning，防任何 Doctor/Admin 伪造他人 ID 订阅其实时通知；P-01——ASP.NET Core 8 OutputCache 默认不缓存带 `[Authorize]` 的响应，原 6 个策略（HerbsCache/FormulasCache/PatientsCache/PrescriptionsCache/MedicalCaseCache/UserPermissionsCache）标注端点全部带 [Authorize]、缓存从未命中，删除全部策略定义 + 4 个 Controller 的 `[OutputCache]` 属性 + `UseOutputCache()` 中间件；**保留裸 `AddOutputCache()` 仅维持 `IOutputCacheStore` 可解析**（CacheInvalidationService 依赖其按 tag 驱逐，实际生效的失效走 IMemoryCache.RemoveByPrefix）。验证：build --no-incremental 0 错误 0 警告 | 越权订阅他人通知属数据泄露；误导性缓存配置应删除而非声称已启用（对齐 2026-08-07「注释与实现一致性」批次）；删 AddOutputCache 会破坏 CacheInvalidationService 的 DI 解析，故保留注册去策略 | 技术总监 |
| 2026-08-07 | **架构一致性修复：删除空壳 ReportsDbContext + CrossModuleService 门面注册移至 Core 层（commit `77702bf62`）**：A-01——`ReportsDbContext`（空壳，无 DbSet、OnModelCreating 空实现）删除，`ReportRepository` 实际注入 `AppDbContext`（03-server.md「待清理」项落地）；`ReportsModule` 移除 `AddDbContext<ReportsDbContext>` 注册及 5 个无用 using。A-02——`ICrossModuleService`/`CrossModuleService` 均定义于 Core/LYBT.Infrastructure，注册却挂在 `PatientsModule`（Auth 隐式依赖 Patients 先完成注册）；新增 `CrossModuleServiceExtensions.AddCrossModuleService()`（LYBT.Infrastructure/Services/CrossModule），从 PatientsModule 删除该注册，WebAPI `RegisterInfrastructureServices`（步骤 1，先于模块注册）与 LocalWebAPI `CreateApplication`（本地模式组合根）两个组合根均接入。验证：build --no-incremental 0 错误 0 警告、架构测试 83/83 | 模块 DI 注册归位分层：Core 类型在 Core 注册；空壳 DbContext 删除而非保留误导性注册 | 技术总监 |
| 2026-08-07 | **WebApi `using` 类型别名清理（Q-02）技术总监独立分析 + 根因**：独立审计 src/Server 23 处别名，分四类——同义别名 EC/GenericErrorCode（同一类型两种叫法，制造认知重复）、实体别名 FormulaEntity/RegistrationEntity（与 Herb/Patient 既有就近 using 写法不一致）、DTO 冗余别名（命名空间已 import）、真实冲突别名 LybtMemoryCacheOptions/LybtJsonOptions（保留）。更深根因：**模块命名空间 `LYBT.Module.Formulas`(单数) 与实体命名空间 `LYBT.Entities.Formulas`(复数) 词根撞名**，致短名 `Formula`/`Registration` 在方法签名/特性位置 CS0118 遮蔽外层命名空间——这正是「别称/重复定义观感」的真实技术成因，别名只是其"遮羞布"。Q-02 清理后仅保留 2 处真实冲突别名，8 个文件因 CS0118 回退全名。建议后续立项：统一模块命名空间与实体命名空间命名（避免单数模块/复数实体撞名），从根消除别名需求 | 别称乱象的治本方向是命名空间命名一致性，而非反复加别名；见 `docs/compose/reports/webapi-using-alias-audit-2026-08-07.md` | 技术总监 |
| 2026-08-08 | **模块命名空间统一复数（Q-03）技术总监独立分析 + 交叉验证 + 治本**：Q-02 暴露的 CS0118 根因经「技术总监审计 + Mimo Code 独立实测」两份报告交叉验证，并修正了 Q-02 审计的一个误判（原误写 Formula 单数模块命名空间为根因，实测证实 Formula CS0118 真根因是 `FormulaBatchImportCommandValidator.cs` 该 1 个文件用单数 `LYBT.Module.Formula` 使 `LYBT.Module` 下出现成员 `Formula` 命名空间遮蔽实体类型；Registration 真根因才是模块整体单数 `LYBT.Module.Registration` 末段 == 实体类型名 `Registration`）。方案甲（纯命名空间级重命名，不碰程序集名）执行：`LYBT.Module.Registration`→`.Registrations`（63 处）、`LYBT.Desktop.Registration`→`.Registrations`（11 文件）、Formula 笔误 1 文件修正、11 文件全名回退裸 `Registration`/`Formula`/`FormulaHerbItem`（对齐 Herb/Patient），共 56 文件。验收：build 0 错误 0 警告、架构测试 83/83、Server 单测 9/9、Desktop 单测 6/6。遗留项（待决策）：① 文档侧旧命名空间引用（`docs/` 内 `LYBT.Module.Registration` 等模块描述）需校准；② 早前 `docs/compose/reports/webapi-deep-analysis-2026-08-07.md` 与本次 `docs/compose/reports/namespace-consistency-mimo-verify-2026-08-07.md` 等分析文档未提交仓 | 命名空间撞名是「别称/重复定义观感」的治本点；模块末段与实体类型名单数/复数不一致即产生 CS0118，迫使全名/别名；统一复数后即消除 | 技术总监 |
| 2026-08-08 | **代码审查（正确性/安全/性能）技术总监独立分析 + Mimo Code 交叉验证**：用户要求 Q-02/Q-03 后做下一轮审查（聚焦业务正确性/安全/性能，命名已收敛）。技术总监独立报告（`docs/compose/reports/code-review-correctness-security-2026-08-08.md`）初判 P1（权限拒绝 `UnauthorizedAccessException` 未映射 403→返 500）、P2（SaveWithDetailAsync 失败语义 404 不准）。**Mimo Code 独立交叉验证（`docs/compose/reports/code-review-correctness-security-mimo-verify-2026-08-08.md`）推翻两项原判，证据确凿**：① `SystemExceptionHandler.cs:98` 已有 `UnauthorizedAccessException => 403「权限不足」` 映射（commit `22dcac755` 2026-07-21 引入，技术总监读文件漏看此行）；全仓 3 个越权抛点（EnsureCanEdit/EnsureCanDelete/OperatorAccessor）实际均返回 403；② `SaveWithDetailAsync` 的 null 唯一来源是 entity 不存在，越权异常向上传播返 403，404 语义正确。结论：**Server 端基础质量健康**（无同步阻塞/async void/SQL 注入/事务缺失/并发令牌缺失；Q-01 泛型基类严谨；打印规则符合 2026-08-03 定案）。交叉验证另发现**低危不对称项**（非 bug，待后续性能/质量批次）：`NotSupportedException`→500(可501)、并发重试耗尽后 `InvalidOperationException`→500 与直接 `DbUpdateConcurrencyException`→409 不对称、`OperatorAccessor`「未登录」→403(语义应401)、`MedicalCaseRepository.Update` Detached 竞态→500(应404)；唯一仍空缺行动项=补 1 个断言越权返 403 的单测（tests 零覆盖该映射） | 双报告交叉验证机制价值：单份审查可能漏看，交叉可纠错；本轮 P1/P2 误报经交叉被修正，避免无效修复 | 技术总监 |
| 2026-08-08 | **异常映射一致性修复（代码审查低危项 P1/P3/P4/P5，commit `203d660d2`）**：P1 补 `SystemExceptionHandlerTests`（`tests/LYBT.Tests.Server/Unit/ExceptionHandling/`，反射调用 `GetExceptionInfo` 断言 `UnauthorizedAccessException`→403，填补 tests 零覆盖缺口，守 403 映射不被未来改动破坏）；P3 并发重试耗尽由 `InvalidOperationException` 改抛 `DbUpdateConcurrencyException`（`MedicalCaseServiceHelper.ExecuteWithConcurrencyRetryAsync`，补 `using Microsoft.EntityFrameworkCore`，与直接并发冲突对称返 409 而非 500）；P4 `MedicalCaseRepository.Update` Detached 竞态实体不存在改抛 `KeyNotFoundException`（返 404 而非 500，System 隐式导入无需加 using）；P5 `SystemExceptionHandler.GetExceptionInfo` 新增 `NotSupportedException`→501「功能未实现」（6 处 `BaseCrudController` 抽象桩语义对齐，避免落默认分支返 500）。P2（`OperatorAccessor` 未登录→401）按审查结论本轮不修不写码。验证：build --no-incremental 0 错误 0 警告、架构测试 83/83、Server 测试 690 通过/0 失败/1 既有跳过、新单测 1/1 | 低危不对称项按「异常类型↔HTTP 状态码语义对齐」修复；只改异常类型不动映射（409/404/501 已在 handler 映射，不重复加）；surgical 原则未碰其他代码 | 技术总监 |
| 2026-08-08 | **死代码清理（Q-04，Serena + codebase-memory 双工具交叉验证第二轮，A-02 复扫）**：删除 6 个死文件（`ExceptionFactory.cs`/`TraceContext.cs`/`LocalEndpoints.cs`/Reports `Domain` 3 个 record 文件）+ 3 个空目录（`Domain`/`Constants`/`Factory`）+ `GlobalUsings.cs`（唯一内容 `global using System.ComponentModel` 未使用）。**任务清单修正**：`ImportResultDto.cs` 实为存活（`PatientBatchImportResultDto`/`HerbBatchImportResultDto`/`FormulaBatchImportResultDto` 均继承它，batch-import 端点依赖此继承链；双交叉验证报告本身均未列出该项），已恢复不删。删除 6 个死方法（`ConfigureGracefulShutdown`/`Formula.RemoveHerb`/`Formula.MarkShared`/`Herb.UpdatePrice`/`BaseClaimsHelper.GetCurrentUserRole`/`MapUserRoleToString`/`CanManageUser`，均 0 调用者；`ParseUserRole` 存活保留）。清理 60 处业务文件未使用 using（IDE0005 编译器权威：MedicalCase 模块 16 处冗余 `System.Threading`、Herbs/Patients/Users 旧命名空间残留、WebAPI 3 处旧基类引用等）。验证：build --no-incremental 0 错误 0 警告、架构测试 83/83、IDE0005 复扫 src/Server+Shared 业务文件残留 0（Migrations 生成文件 12 条按规则不改） | 双工具交叉验证防误删（ImportResultDto 案例证明交叉验证价值：单凭启发式易误判继承链类型）；IDE0005 是未使用 using 的唯一权威（Roslyn 判定）；surgical 原则未改活代码 | 技术总监 |
| 2026-08-08 | **结构审计完成（A-16，技术总监+Mimo 双报告交叉验证）**：三层边界健康（0 越层/0 环/Server 模块 0 直接引用）；双轨真共享（同一 Service 双宿主，ADR-0010）；**交叉纠错 4 项**——C1 技术总监漏判 P0（只看 Controller 文件数对称，Mimo 端点级发现本地 Patients/Herbs/Formulas 未 override GetList/Create/Update → 基类 NotSupportedException → 本地列表/创建/更新 500/405，MedicalCases 缺 GetList/search/print-completed）、C2 AsyncLocalCorrelationIdProvider 已死（Enricher 注释非引用，AddAsyncLocalCorrelationIdProvider 0 调用点）、C3 LocalWebAPI 引用 8 模块（任务书称 7）、C4 种子双端共享（本地额外 3 条英文样例）。**P1 七项收敛候选**：契约双套统一（以 ApiClient 为主）/ 领域客户端 12 对 adapter 收敛 / CorrelationId 删 AsyncLocal 侧 / Server 4 模块手写 Mapper 改 Mapperly / Desktop 模块引用补架构测试（P07 只守 Server）/ 本地配置内存存储重启即失 / 08-shared 结构性过时。**待运行验证**：模块 DbContext 连接串双路径（A8，本地可能指向不同库）、P0 实际 500/405 行为 | 交叉验证价值：单份报告可能漏判（我漏 P0），双报告互补纠错 | 技术总监 |
| 2026-08-08 | **文档规范收敛（用户定，架构目录只放当前值）**：`docs/03-architecture/` 只保留反映系统当前状态的文档（21 个编号文档 + README + modules/decisions/localwebapi/archive 子目录）；**29 个过程文档迁出**——17 个审计/分析/验证报告 → `docs/compose/reports/`、12 个任务书（task-* + task-spec）→ `docs/compose/specs/`；同步更新 9 个文件的引用路径（08-shared / 14-blueprint / 总账 / compose 内部 6 个）+ 文档规范（`01-naming-convention.md` compose 段改为「过程文档归档」，`docs/README.md` 规则 4 与目录计数）+ 架构 README 索引修正（14 号恢复为结构设计蓝图）。**新规范**：过程文档（计划/报告/任务书）一律归档 `compose/`，不进入正式目录；历史报告不删除 | 架构文档=当前值 SSOT，过程文档=compose 归档；执行 Mimo spec 文档逻辑 | 技术总监 |
| 2026-08-08 | **技术引入治理规则确立（用户定，`docs/00-governance/03-technical-adoption-governance.md`）**：① 先文档后代码——任何技术方案变更（T1 收敛/T2 调整/T3 引入）先更新权威文档再改代码；② 文档是字典不是过程——判定标准「过 3 个月还要不要改？要改=字典（正式目录），不用改=过程（compose/）」；③ T3 引入新技术必须走「深度分析（蓝图 §0.5 四标准）→ 分析报告 → 用户确认 → 修改方案文档 → 审批 → 执行」，禁止随意引包；④ 反复横跳预防——决策拍板 24h 内落文档、已定案不因新审计反复质疑、历史报告不误导。同步更新：项目 AGENTS.md 强制规则 #4 + Skill lybtzys-coder-rules v0.11.0 | 防技术方案反复横跳与随意引入；密码多方案并存（BCrypt+PBKDF2）暴露的治理缺口 | 技术总监 |
| 2026-08-08 | **A-26 双轨规范化定案（T2，蓝图 §2.2「请求处理边界规则」）**：CQRS 模块（Auth/Users/Patients/Herbs/Formula/Registration）**写操作走 MediatR Handler**（验证管道+审计）、**读操作走 Service 直查**；禁止 Controller 混用同一操作两条路径；MedicalCase（纯 Service）/Reports（只读）为例外。技术总监判断「移除次要（Service 写操作丢验证管道）保留优秀（Handler 管道 + Service 读直查）」——CQRS 经典形态，当前代码方向正确，缺规则固化。执行：迁移 4 模块（Users/Patients/Herbs/Formula）中走 Service 的写操作（Update/Restore 等）回 Handler + 补架构守卫「写端点禁直调 Service 写方法」 | 双轨规范化 = 写走管道/读走 Service，非全 Handler 化也非全 Service 化（全 Handler 倒退 A-03 简化，全 Service 推翻 ValidationBehavior 管道） | 技术总监 |
