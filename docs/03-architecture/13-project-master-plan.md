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

### B 类 — 产品功能

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| B-01 | P0 安全修复 | 明文密码/Shell Bug/死锁 (7 项) | 无 | ✅ | 待定 |
| B-02 | 配置修改 API | ConfigurationController 添加 PUT | 无 | ✅ | 1d |
|| B-03 | Excel 导出/导入 | Herbs/Formula/Patients (前端 Excel ↔ WebApi JSON) | 无 | ✅ | 2-3d |
| B-04 | 报表增强 | 图表/多维度/时间范围 | 无 | ✅ `cab959dc1` | 2d |
| B-05 | 配置中心 UI | SystemSettingsView 增强 | B-02 | ⬜ | 1d |
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
| B-05 配置中心 UI | ⬜ | — | — |
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
| 代码审计报告 | `docs/reports/2026-08-02-codebase-audit.md` | 项目现状全面审计 |
| 代码审查报告 | `docs/reports/code-review-duplicates.md` | 重复定义与不统一问题 |
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
