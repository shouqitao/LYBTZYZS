# LYBTZYZS 架构与功能设计全面分析报告

**版本**: v1.0 | **日期**: 2026-02-22 | **分析范围**: 全系统 (Server + Desktop + Shared)

---

## 1. 执行摘要

### 项目概况

凌隐宝堂中医诊所管理系统 (LYBTZYZS) 是基于 .NET 8 的全栈中医诊所管理平台，采用 ASP.NET Core (Server) + WPF/Prism (Desktop) 双端架构，支持远程 (SQL Server) 和本地 (SQLite) 双运行模式。目标架构 (SYNC-D02) 已确认: 共享 Service/Repository 层，仅 DbContext Provider 不同。详见 [dual-mode.md](../03-architecture/05-dual-mode.md)。

| 指标 | 数值 |
|------|------|
| 源码项目 | 40+ (Server 12 / Desktop 18 / Shared 8 / 工具 4) |
| 测试项目 | 5 主项目, ~2409 测试用例 |
| 文档体系 | 85 文件, 6 层级, 8 ADR |
| 编译状态 | 0 错误 / 35 警告 |

### 架构健康度总评

**综合评分: 6.83 / 10 (C级 -- 基础扎实，局部薄弱)**

```
优秀区 (>=8.0): D2 设计模式一致性 (8.0) | D5 跨模块依赖 (8.2)
良好区 (7.0-7.9): D6 安全架构 (7.5) | D7 测试架构 (7.0)
待改进 (5.0-6.9): D1 文档合规性 (6.5) | D3 数据模型 (5.5) | D8 代码质量 (6.5)
薄弱区 (<5.0):  D4 错误处理架构 (4.5)
```

### 核心结论

**三个优势**:
1. **分层架构严谨** -- 依赖方向严格单向，无循环引用，ICrossModuleService 解耦优秀
2. **模式统一度高** -- Repository基类、Mapper(Mapperly)、MasterDetailViewModelBase 均100%统一
3. **双模式架构明确** -- 目标架构 (SYNC-D02) 已确认: 共享 Service/Repository 层，仅切换 DbContext Provider。当前过渡态功能完整 (5/5 实体双实现)

**三个风险**:
1. **错误处理体系形同虚设** (D4: 4.5) -- BusinessException/NotFoundException 已定义但 Service 层完全未使用
2. **数据模型偏差严重** (D3: 5.5) -- 打印字段缺失、Discount 精度冲突、索引条件不符
3. **术语违规积累** (D8: 6.5) -- 136处术语铁律违规 + 1299处 OpenSpec 临时标记

### 行动规模

已规划 **305 项任务**，分 5 个 Sprint 执行，覆盖安全加固、功能修复、体系统一、本地补齐、细节完善。已完成 8 个高优先设计问题修复 + 11 项 Code Review 补丁。

---

## 2. 架构健康度诊断

### 2.1 八维度评分总览

| 维度 | 评分 | 等级 | 核心问题 |
|------|------|------|----------|
| D1: 架构文档合规性 | 6.5 | C | Shared层8项目仅文档化3-4个; 空壳模块未标注; 项目总数偏差 |
| D2: 设计模式一致性 | 8.0 | B | FormulaService基类不一致; 其余100%统一 |
| D3: 数据模型对齐 | 5.5 | C | 打印字段缺失; Discount精度冲突; 索引条件不符 |
| D4: 错误处理架构 | 4.5 | D | BusinessException/NotFoundException形同虚设; 术语违规50+ |
| D5: 跨模块依赖 | 8.2 | B+ | 无反向依赖; ICrossModuleService解耦优秀 |
| D6: 安全架构 | 7.5 | B | 100%授权覆盖; Token撤销未完整; AllowAnonymous可疑端点 |
| D7: 测试架构 | 7.0 | B | 架构测试重复; Mock混用; 7模块零覆盖 |
| D8: 代码质量 | 6.5 | C | 术语违规136处; OpenSpec标记1299处 |

**综合: 6.83 / 10 (C级)**

### 2.2 优势区 (>=7.0) 详解

**D5: 跨模块依赖 (8.2/10 -- 最高分)**

系统依赖治理是整个架构最突出的亮点:
- 无反向引用、无循环依赖，分层严格单向
- ICrossModuleService 接口解耦跨模块调用 (Formula -> Herbs 通过 IHerbCrossModuleService)
- 所有跨模块引用仅依赖接口 (DIP)，无具体实现耦合
- Auth -> Users 跨模块 ProjectReference 已修复 (设计问题 #1)

**D2: 设计模式一致性 (8.0/10)**

核心模式统一度极高:
- Repository 基类: 100% 统一 (5/5 模块)
- Mapper (Mapperly): 100% 统一，源生成器零反射
- MasterDetailViewModelBase: 5/5 CRUD 模块 100% 采用
- CQRS 边界清晰: 文档与代码 7/7 服务完全匹配
- 唯一瑕疵: FormulaService 基类继承不一致 (已记录)

**D6: 安全架构 (7.5/10)**

- Controller 100% 类级别 `[Authorize]` 覆盖
- JWT 配置规范，Production 强制配置密钥
- 敏感数据脱敏管线完整 (UserBasicDto 隔离)
- CorrelationId 全链路实现
- 扣分项: Token 撤销 6 项场景未实现 (S1-X3); 3 个 import-template 端点 AllowAnonymous

**D7: 测试架构 (7.0/10)**

- ~2409 测试用例，AAA 模式 6422 处覆盖
- 5 个测试项目分层清晰 (Unit / Desktop.Unit / Architecture / Server.Integration / Desktop.Integration)
- 扣分项: 架构测试有重复规则; Mock 混用 (NSubstitute 为主，少量 Moq 残留); 7 模块零覆盖

### 2.3 待改进区 (5.0-6.9) 详解

**D1: 架构文档合规性 (6.5/10)**

文档体系框架完整 (85 文件, 6 层级, 8 ADR)，但存在覆盖盲区:
- Shared 层 8 个项目仅文档化 3-4 个，Foundation / Models / Utilities 描述缺失
- 空壳模块 (预规划但未激活) 未在文档中标注状态
- 项目总数描述与实际存在偏差

**D3: 数据模型对齐 (5.5/10)**

数据模型是偏差最密集的维度:
- MedicalCase 缺少 IsPrinted / PrintVersion 字段 (S2-X8 待修复)
- Prescription.Discount 精度冲突: 代码 `decimal(5,4)` vs 文档 `decimal(3,2)` (S2-X5)
- MedicalCase 筛选唯一索引条件不符 (S1-A1 已修复，仅 Active 状态)
- PrescriptionPrintLog 未迁移为 MedicalCasePrintLog (S2-X8)

**D8: 代码质量 (6.5/10)**

代码本身可读性和结构良好，但存在大量历史标记:
- 术语铁律违规 136 处 (Consultation 误用为"问诊"等)
- OpenSpec 临时标记 1299 处 (需有计划清理)
- 部分死代码 (已注册但未注入的 Service)

### 2.4 薄弱区 (<5.0) 详解

**D4: 错误处理架构 (4.5/10 -- 最低分)**

这是全系统最需要优先治理的维度:
- `BusinessException` / `NotFoundException` 已定义但 **Service 层完全未使用**
- 错误码体系已设计但未注册 (S3-X1 待修复)
- 术语违规 50+ 处与错误消息混杂
- 双轨响应格式已统一 (ApiResponse + BusinessFail -> 422)，但异常体系尚未切换
- 修复路径: S3-X1 (错误码注册) -> S3-X4 (Service 替换) -> S3-A3 (异常体系切换)

### 2.5 维度间关联

D4 (错误处理) 是全系统架构提升的关键支点:

- D4 修复 -> D8 提升: 错误处理规范化为术语清理提供基础设施
- D4 修复 -> D1 提升: 错误码文档同步完善文档覆盖
- D3 修复 -> D8 提升: 数据模型对齐消除术语不一致

**预估**: D4 + D3 修复后，综合分可从 6.83 提升到 **7.5+ (B级)**。

---

## 3. 偏差与技术债务清单

### 3.1 已修复偏差 (8 + 11 项)

**8 个设计问题** (全部已修复):

| # | 问题 | 严重度 | 修复方案 |
|---|------|--------|----------|
| 1 | Auth -> Users 跨模块 ProjectReference | HIGH | ICrossModuleService 接口解耦 |
| 2+3 | Controller 8 依赖 + 双次 DB 读取 | MEDIUM | IMedicalCaseFacade 门面模式 (8->3 依赖) |
| 4 | Local 模式绕过业务规则 | HIGH | MedicalCaseBusinessRules 提取到 Shared |
| 5 | ViewModel 继承链断裂 | LOW | 组合模式，记录 ADR-0007 |
| 6 | Token 安全可能过度设计 | LOW | 防御性设计保留，记录 ADR-0008 |
| 7 | IHerbItem 接口不一致 | LOW | 删除 Server 端冗余接口 |
| 8 | 双轨响应格式 | MEDIUM | 统一 ApiResponse, BusinessFail -> 422 |

**11 项 Code Review 补丁**: FindAsync 全局过滤器陷阱修复、资源级权限补齐、UserBasicDto 敏感信息隔离等。

### 3.2 待修复偏差 Top 10

| # | 偏差 | 严重度 | Sprint | 影响范围 |
|---|------|--------|--------|----------|
| 1 | Service 层完全未使用统一异常体系 | 严重 | S3-X4 | 全部 7 个 Service |
| 2 | MedicalCase 缺少 IsPrinted/PrintVersion 字段 | 严重 | S2-X8 | 数据模型 + 打印模块 |
| 3 | Token Family 撤销 6 项场景未实现 | 严重 | S1-X3 | 安全架构 |
| 4 | 术语铁律违规 136 处 | 高 | S3-A3 | 全局 |
| 5 | Prescription.Discount 精度冲突 (5,4 vs 3,2) | 严重 | S2-X5 | 数据模型 + 业务计算 |
| 6 | Shared 层 8 项目仅文档化 3-4 个 | 高 | S3-DOC | 文档体系 |
| 7 | Desktop 架构规则仅在旧项目 | 高 | S2-A2 | 架构测试 |
| 8 | 3 个 import-template 端点 AllowAnonymous | 中等 | S2-A2 | 安全 |
| 9 | DataSource 策略模式待迁移 (SYNC-D02) | 高 | S4 | Desktop 双模式架构 |
| 10 | PrescriptionPrintLog 未迁移为 MedicalCasePrintLog | 严重 | S2-X8 | 打印模块 |

### 3.3 技术债务分类

| 类型 | 数量 | 典型项 | 治理 Sprint |
|------|------|--------|------------|
| **架构债务** | ~15 | 错误处理体系、DataSource 迁移 | S3 + S4 |
| **数据模型债务** | ~10 | 字段缺失、精度冲突、索引 | S1 + S2 |
| **安全债务** | ~8 | Token 撤销、AllowAnonymous | S1 |
| **文档债务** | ~20 | Shared 层、术语、PRD 修订 | S3 |
| **代码卫生** | ~50 | 术语违规、OpenSpec 标记、死代码 | S3 + S5 |

### 3.4 债务热力图

按模块聚合债务密度:

```
MedicalCase    ████████████████  最密集: 打印字段+聚合根+状态机
Shared/Infra   █████████████     文档+错误处理+术语
Auth           ██████████        Token撤销+权限矩阵
Sync           █████████         SYNC-D02迁移+冲突策略
Users          ████████
Formula        ███████
Patients       ██████
Herbs          ██████
```

**MedicalCase** 是债务最密集的模块 (打印重构 + 聚合根字段 + 状态机补全)，也是业务核心，修复优先级最高。

---

## 4. 优化方案

### 4.1 优化策略总纲

按"投入产出比"排序，优先修复影响面最广、连锁收益最高的项:

| 象限 | 策略 | 示例 |
|------|------|------|
| 高收益 + 低成本 | 立即执行 | 索引修复、AllowAnonymous 审查 |
| 高收益 + 高成本 | Sprint 规划 | 异常体系激活、SYNC-D02 迁移 |
| 低收益 + 低成本 | 顺手修复 | OpenSpec 标记清理 |
| 低收益 + 高成本 | 推迟或放弃 | -- |

### 4.2 第一优先级: 安全加固 (S1)

**目标**: D6 从 7.5 提升到 8.5+

| 优化项 | 当前状态 | 目标状态 | 关键动作 |
|--------|----------|----------|----------|
| Token Family 撤销 | 6 项场景未实现 | 全覆盖 | S1-X3: Revoke/Rotate/Logout/PasswordChange/RoleChange/Disable |
| AllowAnonymous 端点 | 3 个可疑端点 | 全部加授权或记录 ADR | S2-A2: 审查并补齐 |
| 权限矩阵 | 仅类级别授权 | 方法级别+资源级别 | S1-S2: 完善权限矩阵 |

**连锁收益**: 安全链 S1-X3 -> S1-S2 完成后，为后续所有 Sprint 提供安全基础。

### 4.3 第二优先级: 数据模型修正 (S1-S2)

**目标**: D3 从 5.5 提升到 7.5+

| 优化项 | 当前状态 | 目标状态 | 关键动作 |
|--------|----------|----------|----------|
| 打印字段 | MedicalCase 缺少 4 个字段 | 字段完整 | S2-X8: 打印重构，含 Migration |
| Discount 精度 | 代码 decimal(5,4) vs 文档 decimal(3,2) | 统一 decimal(3,2) | S2-X5: 精度对齐 + Migration |
| PrintLog 迁移 | PrescriptionPrintLog | MedicalCasePrintLog | S2-X8: 重命名 + 数据迁移 |

**连锁收益**: 打印链完成后，打印功能端到端可用。

### 4.4 第三优先级: 错误处理体系激活 (S3)

**目标**: D4 从 4.5 提升到 7.5+ (预期最大提升幅度)

分三步渐进激活，不破坏现有行为:

| 步骤 | 工作包 | 动作 | 风险 |
|------|--------|------|------|
| Step 1 | S3-X1 错误码注册 | ErrorCodeRegistry 模块化注册，纯增量 | 低 |
| Step 2 | S3-X4 Service 替换 | 逐方法: return null -> throw NotFoundException | 中 |
| Step 3 | S3-A3 异常体系切换 | 全局中间件匹配 + 术语修正 | 中 |

**连锁收益**: 错误链完成后，D4 + D8 + D1 三维度联动提升。

### 4.5 第四优先级: 双模式架构迁移 (S4)

**目标**: 实施 SYNC-D02，消除 DataSource 双套维护。详见 [dual-mode.md](../03-architecture/05-dual-mode.md)。

| 阶段 | 动作 | 风险 |
|------|------|------|
| 准备 | LocalDbContext Entity 配置与 Server 端 DbContext 对齐 | 低 |
| 核心 | Repository 注入 DbContext 接口，按 Provider 切换连接 | 中 |
| 清理 | 删除 IDataSource 接口族 + 全部 Remote/Local DataSource | 低 |
| 增强 | 运行时软重启 SYNC-D03 | 中 |

**连锁收益**: 迁移后新增功能只写一套代码。

### 4.6 第五优先级: 文档与代码卫生 (S3+S5)

**目标**: D1 从 6.5 提升到 8.0+, D8 从 6.5 提升到 8.0+

| 优化项 | 数量 | 策略 |
|--------|------|------|
| Shared 层文档补全 | 4-5 个项目 | S3-DOC: 逆向工程生成文档 |
| 术语违规清理 | 136 处 | S3-A3: 批量搜索替换 + 人工审核 |
| OpenSpec 标记清理 | 1299 处 | S5: 按模块分批清理 |
| 死代码清理 | ~20 处 | S5: Grep 无引用 -> 删除 |

### 4.7 优化收益预测

| 阶段 | 完成 Sprint | 预期综合分 | 等级 |
|------|------------|-----------|------|
| 当前 | -- | 6.83 | C |
| S1 完成 | S1 | 7.1 | B |
| S1+S2 完成 | S2 | 7.5 | B |
| S1+S2+S3 完成 | S3 | 8.2 | B+ |
| 全部完成 | S5 | 8.8+ | A- |

---

## 5. Sprint 执行路线图

### 5.1 全局视图

```
S1 安全加固 (33项) ──→ S2 核心修复 (51项) ──→ S3 体系统一 (85项) ──→ S4 本地补齐 (62项) ──→ S5 细节完善 (98项)
       │                      │                      │                      │
       └─ 安全链起点           └─ 打印链核心           └─ 错误链+D5解耦       └─ SYNC-D02落地
```

**总量**: 329 项任务 (原 305 + D2/D5 设计深化新增 24) | **关键链**: 4 条依赖链贯穿全程

### 5.2 Sprint 1: 安全加固与数据完整性 (33 项)

**风险**: 中 | **前置依赖**: 无 | **就绪度**: 可立即启动

| 工作包 | 任务数 | 核心内容 | 交付标准 |
|--------|--------|----------|----------|
| X3: Token 撤销 | 6 | 6 项撤销场景全实现 | 集成测试覆盖 |
| S2: 权限矩阵 | 9 | 方法级+资源级权限补齐 | AllowAnonymous 审查完毕 |
| A1: 索引修复 | 3 | MedicalCase 唯一索引 | Migration + 架构测试 |
| 其他 | 15 | 安全配置加固、审计日志、引用检查 | -- |

**关键里程碑**: S1-X3 完成后解锁 S1-S2

**完成标准**: D6 安全架构评分达到 8.0+

### 5.3 Sprint 2: 核心功能修复 (51 项)

**风险**: 高 | **前置依赖**: S1-A1 | **就绪度**: S1 完成后可启动

| 工作包 | 任务数 | 核心内容 | 交付标准 |
|--------|--------|----------|----------|
| X8: 打印重构 | 15 | 打印字段 + PrintLog 迁移 + A5 模板 | 端到端打印可用 |
| X5: 数据模型修正 | 8 | Discount 精度 + 聚合根字段 | Migration 通过 |
| A2: 架构测试迁移 | 6 | Desktop 规则迁移 + 审查 | NetArchTest 通过 |
| 其他 | 22 | 状态机完善、字段对齐 | -- |

**风险点**: 打印重构涉及数据迁移，需备份策略

**完成标准**: D3 数据模型评分达到 7.5+

### 5.4 Sprint 3: 体系统一与文档同步 (85 项)

**风险**: 高 (任务量最大) | **前置依赖**: S2

| 工作包 | 任务数 | 核心内容 | 交付标准 |
|--------|--------|----------|----------|
| X1: 错误码统一 | 15 | 错误码 MCCEE 5位编码统一 | 全模块错误码可查 |
| X4: Service 层替换 | 5 | Service 层 ErrorCode 替代 | 单元测试全通过 |
| X6: 分页筛选迁移 | 6 | 内存过滤迁移到 Repository | 分页 TotalCount 正确 |
| D5: 跨模块解耦 | 12 | ICrossModuleService ISP 拆分 + Sync 解耦 | 详见 [d2-d5-design](2026-02-22-d2-d5-design-patterns-dependencies.md) |
| 架构+文档+PRD+标准 | 35 | 架构 9 + 文档 16 + PRD 16 + 标准 6 - 重叠 12 | D1 达到 8.0+ |

**完成标准**: D4 达到 7.5+，综合分达到 8.0+

### 5.5 Sprint 4: 本地模式补齐 (62 项)

**风险**: 中 | **前置依赖**: S2-X8

| 工作包 | 任务数 | 核心内容 | 交付标准 |
|--------|--------|----------|----------|
| SYNC-D02: 架构迁移 | ~15 | 废除 DataSource，统一 DbContext Provider | 本地/远程共享代码 |
| SYNC-D03: 运行时切换 | ~5 | DI 热替换 + 导航回首页 | 无需重启切换模式 |
| X2: 接口扩展迁移 | 22 | DataSource 方法迁移到 Repository | 功能零丢失 |
| S5: 打印模板 | 11 | A5 动态分页 + 纸张匹配 | 模板完善 |
| S6: Shell 功能 | 4 | 菜单可见性 / 本地模式分支 | UI 适配完成 |

**完成标准**: 本地模式 UI 解禁，端到端可用

### 5.6 Sprint 5+: 细节完善 (98 项)

**风险**: 低 | **前置依赖**: 部分依赖 S4

| 工作包 | 任务数 | 核心内容 |
|--------|--------|----------|
| OpenSpec 标记清理 | ~30 | 已完成提案的标记批量删除 |
| 死代码清理 | ~15 | 无引用 Service / 空壳模块删除 |
| 测试覆盖补齐 | ~20 | 7 个零覆盖模块补充测试 |
| P2: 唯一性校验 | ~10 | 本地模式唯一性校验 |
| 其他 | ~11 | Mock 统一、架构测试去重 |

### 5.7 依赖链全图

```
安全链:  S1-X3 (Token撤销) ──→ S1-S2 (权限矩阵) ──→ 完整验证
打印链:  S1-A1 (索引修复) ──→ S2-X8 (打印重构) ──→ S4-S5 (打印模板) ──→ 端到端打印
错误链:  S3-X1 (错误码注册) ──→ S3-X4 (Service替换) ──→ S3-A3 (异常体系)
本地链:  S4-SYNC-D02 (架构迁移) ──→ S5-P2 (唯一性校验)
```

### 5.8 风险登记

| 风险 | 概率 | 影响 | 缓解 |
|------|------|------|------|
| S2-X8 打印重构数据迁移失败 | 中 | 高 | 迁移前备份 + 回滚脚本 |
| S3 任务量大 (73项) 延期 | 高 | 中 | 错误链优先，文档可延后 |
| S4 SQLite 兼容性问题 (SYNC-D02) | 中 | 中 | 提前编写兼容性测试矩阵 |
| S3-X4 异常替换引入回归 | 中 | 高 | 逐 Service 替换，每次跑全量测试 |

---

## 附录

### A. 分析数据源

| 文档 | 路径 | 用途 |
|------|------|------|
| system-overview.md | docs/03-architecture/ | 整体架构图 |
| server.md | docs/03-architecture/ | Server 三层 + CQRS |
| desktop.md | docs/03-architecture/ | MVVM + Components |
| dual-mode.md | docs/03-architecture/ | 双模式架构 (权威源) |
| data-model.md | docs/03-architecture/ | ER 图 + 实体定义 |
| architecture-deep-comparison.md | docs/plans/ | 8 维度评分基准 |
| design-issues-solutions.md | docs/plans/ | 8 个设计问题解决方案 |
| full-sprint-design.md v2 | docs/plans/ | 305 项任务 Sprint 计划 |

### B. 术语表

| 术语 | 定义 |
|------|------|
| Consultation | 仅指中医诊断部分 |
| Prescription | 处方部分 |
| MedicalCase | 医案整体 (Consultation + Prescription) |
| Formula | 验方/经验方 |
| SYNC-D02 | 统一本地/远程数据路径决策 |

### C. 变更记录

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-02-22 | v1.0 | 初始版本，包含全部 5 段分析 |
