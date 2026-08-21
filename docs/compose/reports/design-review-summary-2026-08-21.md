# LYBTZYZS 全系统设计审查报告

**日期**: 2026-08-21  
**审查范围**: Server + Desktop + Shared + Documentation 全系统  
**审查规模**: ~1,000 业务 .cs 文件 (~100K 行) + 169 文档文件 (~34K 行)  
**审查轮次**: 53 轮 (R1-R50 设计审查 + R51-R53 项目合并评估)  
**执行方式**: 5 批 × 10 轮 + 1 批 × 3 轮，pi agent 顺序执行  

---

## 一、执行摘要

### 关键数字

| 指标 | 数值 |
|------|------|
| 总发现数 | 209 项 (R1-R50) + 3 项合并评估 (R51-R53) |
| P0 (架构性缺陷) | **0** |
| P1 (设计问题) | **26** |
| P2 (改进建议) | **127** |
| P3 (风格/文档) | **56** |
| 整合优化项 | 9 项 (OP-01 ~ OP-08) |
| 项目合并评估 | 3 候选，全部建议保留 |

### 架构健康度

| 维度 | 评分 | 说明 |
|------|------|------|
| 架构分层与依赖 | **A** | Server 三层单向 + Desktop 四层契约清晰 |
| 领域模型 | **A-** | MedicalCase 充血模型正确，贫血边界清晰 |
| 模块边界 | **B+** | 跨模块接口过度暴露，需收敛 |
| DI 与生命周期 | **B+** | 顺序依赖与生命周期分散，需统一 |
| 状态机 | **B+** | 守卫分散，需统一入口 |
| API 设计 | **B+** | 双轨错误处理待统一 |
| 性能 | **B+** | N+1 与无分批待批量化 |
| 配置与启动 | **B** | KnownKeys 与敏感度待扫描化 |
| DTO 投影 | **B** | Input 只读字段待清理 |
| 文档 SSOT | **B-** | 多源重复，需收敛 |

**综合健康度: B+（良好）**

> 架构骨架扎实，无 P0 阻塞性缺陷。整合优化集中在横切面（状态机/错误处理/授权），而非结构缺陷。

---

## 二、P1 发现汇总（26 项，按主题分组）

### 主题 1：状态机守卫分散（6 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 1 | MedicalCase IsLocked 仅前端置灰 | R15 | API 未强锁，可绕过前端直接 PUT 已锁定医案 |
| 2 | Registration Cancel 未校验医案状态 | R14 | 已完成医案关联的挂号仍可 Cancel |
| 3 | 单患者单 Waiting 约束无 DB 索引 | R14 | 高并发下 AnyAsync 与 Insert 间竞态可致双 Waiting |
| 4 | 审计日志与业务分两次 SaveChanges | R15 | 非原子：审计失败业务已落，业务失败审计已落 |
| 5 | 状态机守卫散落各层 | R20 | MedicalCase + Registration 守卫在 Service/Handler/ViewModel 各处 |
| 6 | 备份恢复未校验完整性 | R35 | 损坏 .bak 直接还原致 DB 损坏 |

### 主题 2：错误处理不统一（4 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 7 | ErrorCode 映射缺失 | R20 | DbUpdateConcurrencyException / ValidationException 未覆盖 |
| 8 | Controller 内 BusinessFail 双轨 | R18 | 同一层两种错误处理模式（抛异常 vs 200+success=false） |
| 9 | AesGcm 解密失败静默回退明文 | R12 | 密钥错误时返回 Base64 原值，安全隐患 |
| 10 | Token 刷新无防重入 | R29 | 并发 401 时多线程同时刷新 Token |

### 主题 3：授权矩阵不一致（4 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 11 | 挂号取消权限三处不一致 | R5 | 产品规则/代码策略/API 文档三处矛盾 |
| 12 | 患者 DELETE 权限目标态 vs 类级 | R6 | 文档目标态 AdminOrSuperAdmin，代码类级仍为 DoctorOrAdminOrReceptionist |
| 13 | Reports Doctor 可看全诊所报表 | R17 | 无行级过滤，与产品规则矛盾 |
| 14 | 类级宽松+方法级严格 | R18 | Swagger 显示可试调，实际被方法级拒绝 |

### 主题 4：DI 与中间件（4 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 15 | AddIdentity 顺序依赖无校验 | R4/R19 | 仅靠注释保证，无启动期断言 |
| 16 | SecurityHeaders 注册位置偏后 | R18/R19 | 401/403 响应缺安全头 |
| 17 | Desktop ModuleCatalog 角色裁剪不彻底 | R4 | 所有角色加载所有 DLL，仅菜单过滤 |
| 18 | MediatR 重复 AddMediatR | R4 | 6 次注册导致 ValidationBehavior 执行 6 次 |

### 主题 5：Repository 与数据访问（3 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 19 | BaseRepository UpdateAsync Detached 全列标记 | R11 | RowVersion WHERE 用错值致 0 rows |
| 20 | BaseEntity vs ApplicationUser 软删除双分支 | R9 | 手抄字段易遗漏新增审计字段 |
| 21 | RefreshToken ER 图与代码不一致 | R9 | ER 图列实体但代码无对应 |

### 主题 6：Desktop 端（3 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 22 | PrescriptionItems VM 间传递未深拷贝 | R38 | 引用泄漏导致编辑互相影响 |
| 23 | Desktop TokenRefreshHandler 无防重入 | R29 | 与 R10 主题 2 第 10 项同因 |
| 24 | 挂号取消按钮仅检查 Source 未校验医生模式 | R30 | 双 Source 模型 UI 按钮使能错位 |

### 主题 7：文档与需求（2 项）

| # | 发现 | 位置 | 问题 |
|---|------|------|------|
| 25 | US-PAT-005 验收与已知问题自相矛盾 | R5 | 同一需求文档内前后矛盾 |
| 26 | 历史报告发现未闭环追踪 | R8 | 15 份报告 145 项发现无修复状态回写 |

---

## 三、整合优化路线图（9 项）

### P1 优化项（优先执行）

| 编号 | 优化项 | 方案 | 工时 | 可组合 |
|------|--------|------|------|--------|
| **OP-01** | 状态机守卫统一 | StateGuard 基类统一校验入口 | 2d | 可与 OP-02 同批 |
| **OP-02** | 错误处理统一 | 单点 ErrorCode 映射 + 删除 Controller 内 BusinessFail | 2d | 可与 OP-01 同批 |
| **OP-03** | 授权矩阵 SSOT | 类级改最严 + Reports 行级过滤 | 1.5d | 独立 |
| **OP-04** | 性能批量+索引 | IN 批量 + 分批 500 + 3 索引 | 2d | 可与 OP-05 同批 |

### P2 优化项（次优执行）

| 编号 | 优化项 | 方案 | 工时 |
|------|--------|------|------|
| **OP-05** | 跨模块接口收敛 | 仅保留被调用方法 | 1d |
| **OP-06** | 配置 KnownKeys 扫描 | 改为扫描 Configuration 提供者 | 0.5d |
| **OP-07** | DTO 投影规则 | Input 仅可写 + 深拷贝 | 1d |

### P3 优化项（择机执行）

| 编号 | 优化项 | 方案 | 工时 |
|------|--------|------|------|
| **OP-08** | 文档 SSOT 收敛 | 唯一 SSOT + ADR 实现状态列 | 1d |

### 执行建议

**快速赢（1 天内）**:
- OP-03 部分：类级权限收紧 + Registration Cancel 增医案状态校验
- OP-06：KnownKeys 改扫描

**中期（1-2 周）**:
- Sprint 1: OP-01 + OP-02（状态机 + 错误处理同属横切面，4d）
- Sprint 2: OP-04 + OP-05（性能 + 接口收敛同属数据流，3d）
- Sprint 3: OP-03 + OP-07（授权 + DTO，2.5d）

**长期（需 ADR）**:
- OP-08 文档 SSOT（需 ADR-0025）
- OP-03 授权 SSOT（需 ADR-0026）

---

## 四、Project 合并决策

### 评估结论：3 候选全部保留

| 候选 | 定位 | 结论 | 理由 |
|------|------|------|------|
| Auth+Users→Identity | 横切认证 vs 业务管理 | **保留** | 两者定位正交，Module 加载语义冲突（WhenAvailable vs OnDemand） |
| Printing→Controls | 打印服务 vs 纯 UI 控件 | **保留** | Controls 将增重 30%，服务与控件边界模糊 |
| Reports→Infrastructure | 只读聚合 vs 数据访问基础设施 | **保留** | Infrastructure 将变"半业务"，未来增长需二次拆分 |

**关键洞察**: 每个 project 的设计目的都是正交的——它们解决的是不同维度的问题。合并后没有一个 project 能覆盖另一个的设计目的，反而会模糊架构边界。当前 13 Desktop + 7 Server 模块结构合理。

---

## 五、审查覆盖范围

### 按轮次

| 批次 | 轮次 | 审查内容 | 文件数 | 发现 |
|------|------|---------|--------|------|
| Batch 1 | R1-R10 | 架构文档+ADR+DI+需求+API文档+Entity+DTO | ~284 | 50 |
| Batch 2 | R11-R20 | Infrastructure+Identity+Patients+Registration+MedicalCases+Catalog+Reports+WebAPI | ~223 | 50 |
| Batch 3 | R21-R30 | Desktop.Contracts+Infrastructure+Foundation+Controls+LocalWebAPI+Shell+Auth+Users | ~380 | 50 |
| Batch 4 | R31-R40 | Desktop.MedicalCase+Patients+Registrations+Catalog+Admin+Clinical+Printing+跨模块 | ~300 | 50 |
| Batch 5 | R41-R50 | 整合优化综合分析（横向聚合，非新发现） | — | 9 |
| 合并评估 | R51-R53 | 3 候选 Project 合并可行性（基于设计意图） | ~80 | 3 |

### 按维度

| 维度 | 轮次 | 发现数 |
|------|------|--------|
| 架构分层与依赖方向 | R1-R4 | 20 |
| 模块边界设计 | R3, R20 | 10 |
| DI 架构与生命周期 | R4, R19, R28 | 15 |
| 配置架构 | R12, R19 | 10 |
| 领域模型设计 | R9, R15 | 15 |
| 状态机完备性 | R14, R15, R20 | 15 |
| API 设计一致性 | R6, R18 | 15 |
| DTO 设计与投影 | R10, R21 | 10 |
| 数据访问模式 | R11, R17 | 10 |
| 安全架构 | R12, R17, R18 | 10 |
| 错误处理契约 | R18, R20 | 10 |
| 文档-代码对账 | R1-R8, R48 | 30 |

---

## 六、文件位置

- **完整详细报告**: `docs/compose/reports/design-review-2026-08-21.md`（1208 行，R1-R53 全部发现）
- **本摘要**: `docs/compose/reports/design-review-summary-2026-08-21.md`

---

*报告由 Hermes Agent 总架构师角色生成，基于 pi agent 53 轮深度审查的完整输出。*
