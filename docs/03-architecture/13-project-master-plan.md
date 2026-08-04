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
| 架构测试 | 85/86 pass（P07/P08/P10 约束不可违反） |
| Build | 0 错误 **9 个警告** |
| Desktop 测试 | 240 pass **104 fail**（测试主机进程崩溃） |

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
| A-12 | AuthService 收敛 | RefreshToken 操作收敛到 Repository | 无 | ⬜ | 0.5d |

### B 类 — 产品功能

| ID | 任务 | 内容 | 依赖 | 状态 | 预估 |
|----|------|------|------|------|------|
| B-01 | P0 安全修复 | 明文密码/Shell Bug/死锁 (7 项) | 无 | ⬜ | 待定 |
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
| C-03 | 部署脚本清理 | 评估 3 个脚本 | 无 | ⬜ | 0.25d |
| C-04 | NuGet 包清理 | 废弃包检查 | 无 | ⬜ | 0.25d |
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
| E-04 | MCP 配置 | 禁用 tavily/serena + 注释 | 无 | ✅ | — |

---

## 七、执行阶段

### Phase 0: 安全/基础设施 (P0，最高优先级)
| 序号 | 任务 | 预估 |
|------|------|------|
| 1 | B-01 P0 安全修复 (7项) | 待定 |
| 2 | B-02 配置修改 API | 1d |
| 3 | A-12 AuthService 收敛 | 0.5d |
| 4 | C-04 NuGet 包清理 | 0.25d |
| 5 | C-03 部署脚本清理 | 0.25d |
| **小计** | | **~2d + 安全修复** |

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
| A-12 AuthService 收敛 | ⬜ | — | — |
| B-01 P0 安全修复 | ⬜ | — | — |
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
| C-03 部署脚本清理 | ⬜ | — | — |
| C-04 NuGet 包清理 | ⬜ | — | — |
| C-05 文档同步 | ⬜ | — | — |
| D-01 接诊链修复（D8：StartVisit 原子建医案，2026-08-03 决策确认） | ✅ | 2026-08-03 | `7caa1fb4a` |
| D-02 QuickVisit Desktop 接线（US-REG-002 激活） | ✅ | 2026-08-03 | `7caa1fb4a` |
| D-03 医案状态机重构（B6-B9：取消=物理删 / 仅 Completed 打印 / 打印保护简化 / 堵状态机绕过） | ✅ | 2026-08-03 | `64adfebd3` `5bc01de5b` `7fd4a09e5` `d98332394` |
| E-01 规则体系：coder 层（角色 AGENTS.md 精简 + Skill v0.6.0 确立 SSOT） | ✅ | 2026-08-04 | profile 目录（非 repo），备份 `profiles\coder\backups\2026-08-04\` |
| E-02 规则体系：项目层（项目 AGENTS.md 精简为入口+引用，详细规则迁入 Skill） | ✅ | 2026-08-04 | `da2290117` |
| E-03 规则体系：项目总账拆分（13a/13b/13c） | ✅ | 2026-08-04 | `1664f2ffc` |
| E-04 规则体系：MCP 配置清理（移除 tavily 又恢复，serena/tavily 全部保留） | ✅ | 2026-08-04 | 本地修改（`.mimocode` 被 gitignore，无 commit） |

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
| **产品功能清单** | `docs/02-requirements/14-feature-inventory.md` | 128 个用户操作，按模块/角色分类 |
| 代码审计报告 | `docs/reports/2026-08-02-codebase-audit.md` | 项目现状全面审计 |
| 代码审查报告 | `docs/reports/code-review-duplicates.md` | 重复定义与不统一问题 |
| PRD | `docs/02-requirements/01-prd.md` | 产品需求文档 |
| 数据模型 | `docs/03-architecture/13a-data-model.md` | 核心实体 + 状态枚举 |
| API 端点 | `docs/03-architecture/13b-api-endpoints.md` | 全部模块端点 |
| 当前状态 | `docs/03-architecture/13c-current-status.md` | Desktop 视图 + 已知问题 |
| AGENTS.md | `AGENTS.md` | 开发规范与约束 |
