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
| A-02 | 死代码删除 | ~737 行零引用代码 (Server 10 + Desktop 5 + Shared 2) | 无 | ⬜ | 0.5d |
| A-03 | MediatR 简化 | Herbs/Formula/Patients/Users 4 模块已完成(24 Handler)；MedicalCase 待做 | 无 | 🟡 | 0.5d |
| A-04 | 超大类型拆分 | 5 个 >600 行文件 | 无 | ⬜ | 2d |
| A-05 | 实体源统一 | 消除 Domain/Shared 双模型 | A-02 | ⬜ | 3d |
| A-06 | Repository 泛型化 | Desktop 15+ 对复制粘贴 | 无 | ⬜ | 1d |
| A-07 | CrossModule 死方法 | 6 个零调用方法 | 无 | ⬜ | 0.25d |
| A-08 | 命名规范统一 | 后缀/目录/注释语言 | 无 | ⬜ | 1d |
| A-09 | 架构测试补全 | 修复 1 skip + 新增 13 个缺口测试 | A-03/A-04 | ⬜ | 1d |
| A-10 | 接口下沉 Contracts | IPatientService/IUserService 移到 Contracts | 无 | ⬜ | 0.5d |
| A-11 | Registration 依赖清理 | 移除对 Patients/Users 直接引用 | A-10 | ⬜ | 0.25d |
| A-12 | AuthService 收敛 | RefreshToken 操作收敛到 Repository | 无 | ✅ 已完成（代码已通过 IAuthSessionRepository） | 0 |

### B 类 — 产品功能

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| B-01 | P0 安全修复 | 明文密码/Shell Bug/死锁 (7 项) | 无 | ✅ | 待定 |
| B-02 | 配置修改 API | ConfigurationController 添加 PUT | 无 | ⬜ | 1d |
| B-03 | Excel 导出/导入 | Herbs/Formula/Patients (NPOI) | 无 | ⬜ | 2-3d |
| B-04 | 报表增强 | 图表/多维度/时间范围 | 无 | ⬜ | 2d |
| B-05 | 配置中心 UI | SystemSettingsView 增强 | B-02 | ⬜ | 1d |
| B-06 | 数据备份/恢复 | SQL Server 备份+恢复 | 无 | ⬜ | 1.5d |
| B-07 | 初始化向导完善 | FirstRunSetupView 增强 | 无 | ⬜ | 1d |
| B-08 | Desktop 发布包 | 打包+依赖裁剪+安装器 | 无 | ⬜ | 2d |
| B-09 | 自动更新 | Velopack 集成 | B-08 | ⬜ | 2d |
| B-10 | SignalR 实时通知 | Hub+客户端+协议设计 | A-03 | ⬜ | 3d |
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
| 2 | B-02 配置修改 API | 1d | ⬜ |
| 3 | A-12 AuthService 收敛 | 0.5d | ✅ 已完成 |
| 4 | C-04 NuGet 包清理 | 0.25d | ✅ 已完成 |
| 5 | C-03 部署脚本清理 | 0.25d | ✅ 已完成 |
| **小计** | | **~2d + 安全修复** | **4/5 完成** |

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
| **小计** | | **~8d** |

### Phase 3: 核心功能 (业务价值最高)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-03 Excel 导出/导入 | 2-3d |
| 2 | B-04 报表增强 | 2d |
| 3 | B-05 配置中心 UI | 1d |
| 4 | B-06 数据备份/恢复 | 1.5d |
| 5 | B-07 初始化向导 | 1d |
| **小计** | | **~8d** |

### Phase 4: 高级功能 (依赖 Phase 2/3)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-08 Desktop 发布包 | 2d |
| 2 | B-09 自动更新 | 2d |
| 3 | B-10 SignalR | 3d |
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
| A-02 死代码删除 | ⬜ | — | — |
| A-03 MediatR 简化 | 🟡 | 2026-08-02 | `29a4675af` `c5aca4e04` `741ca8735` `4b97bcfde` |
| A-04 超大类型拆分 | ⬜ | — | — |
| A-05 实体源统一 | ⬜ | — | — |
| A-06 Repository 泛型化 | ⬜ | — | — |
| A-07 CrossModule 死方法 | ⬜ | — | — |
| A-08 命名规范统一 | ⬜ | — | — |
| A-09 架构测试补全 | ⬜ | — | — |
| A-10 接口下沉 | ⬜ | — | — |
| A-11 Registration 依赖清理 | ⬜ | — | — |
| A-12 AuthService 收敛 | ✅ | 2026-08-04 | 代码已通过 IAuthSessionRepository（RefreshTokenCommandHandler 无直接 DbContext） |
| B-01 P0 安全修复 | ✅ | 2026-08-04 | `831702b51` `cb4d3e6b9` |
| B-02 配置修改 API | ⬜ | — | — |
| B-03 Excel 导出/导入 | ⬜ | — | — |
| B-04 报表增强 | ⬜ | — | — |
| B-05 配置中心 UI | ⬜ | — | — |
| B-06 数据备份/恢复 | ⬜ | — | — |
| B-07 初始化向导 | ⬜ | — | — |
| B-08 Desktop 发布包 | ⬜ | — | — |
| B-09 自动更新 | ⬜ | — | — |
| B-10 SignalR | ⬜ | — | — |
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
| 2026-08-04 | **Phase 0 收尾（3 项小任务）**：A-12 AuthService 收敛——代码已通过 IAuthSessionRepository（RefreshTokenCommandHandler 无直接 DbContext），无需改动；C-03 部署脚本清理——旧 worktree 目录已不存在（git worktree list 确认仅剩主仓库）；C-04 NuGet 废弃包检查——扫描 75 个包，无废弃包（Bogus 未在 csproj 中；SixLabors.Fonts/ImageSharp 为 QuestPDF 安全固定依赖）。Phase 0 仅剩 B-02（配置修改 API，1d） | Phase 0 4/5 完成 | 技术总监 |
| 2026-08-04 | **Phase 0 收尾（3 项小任务）**：A-12 AuthService 收敛——代码已通过 IAuthSessionRepository（RefreshTokenCommandHandler 无直接 DbContext），无需改动；C-03 部署脚本清理——旧 worktree 目录已不存在（git worktree list 确认仅剩主仓库）；C-04 NuGet 废弃包检查——扫描 75 个包，无废弃包（Bogus 未在 csproj 中；SixLabors.Fonts/ImageSharp 为 QuestPDF 安全固定依赖）。Phase 0 仅剩 B-02（配置修改 API，1d） | Phase 0 4/5 完成 | 技术总监 |

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
