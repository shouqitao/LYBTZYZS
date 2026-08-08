# 结构审计交叉验证报告（技术总监 + Mimo Code）

> 日期：2026-08-08
> 独立报告 #1：`structure-audit-2026-08-08.md`（技术总监）
> 独立报告 #2：`structure-audit-mimo-2026-08-08.md`（Mimo Code）
> 本报告为两份独立分析的交叉验证结论，供产品负责人拍板。

---

## 一、交叉验证结论

### ✅ 双方一致确认（高置信）

| # | 结论 | 双方证据 |
|---|------|---------|
| 1 | 三层边界健康：无越层/无环/Server 模块 0 直接引用 | 依赖图双份一致 |
| 2 | 双轨业务逻辑真共享（同一 Service/Handler 双宿主，ADR-0010） | Controller 对比一致 |
| 3 | 契约双套双活：`Api/*`(Refit) + `ApiClient/*`(统一面)，合并方向=以 ApiClient 为主 | 引用计数一致（55 vs 26） |
| 4 | Shared 文档严重滞后（声称 8 项目实际 5，Utilities 清单全错） | 08-shared 对照一致 |
| 5 | P07 只守卫 Server，Desktop 模块间引用 0 守卫，且有真违规（Registration→MedicalCase） | csproj + 架构测试一致 |
| 6 | Server 映射双轨：7 模块引 Mapperly 包但 4 模块手写静态类 | 包引用 + [Mapper] grep 一致 |
| 7 | 死代码新发现：AsyncLocalCorrelationIdProvider(未注册)、LocalDbContext(休眠)、DtoConversionExtensions、CacheExtensions | 引用计数一致 |

### ⚠️ 交叉纠错（Mimo 抓到技术总监漏掉的）

| # | 纠错 | 详情 | 影响 |
|---|------|------|------|
| **C1（最重要）** | **P0：本地模式患者/药材/验方 CRUD 主干断裂** | 技术总监只验证了「Controller 文件数 1:1 对称」；Mimo 深入端点级发现：本地 Patients/Herbs/Formulas 控制器未 override `GetList/Create/Update`，基类抛 `NotSupportedException` → 本地列表/创建/更新 **500/405**；MedicalCases 缺 GetList/search/print-completed | **本地模式核心功能不可用**，需产品决策修复优先级 |
| C2 | AsyncLocalCorrelationIdProvider 判定 | 技术总监误把 Enricher 注释当引用；Mimo 确认 `AddAsyncLocalCorrelationIdProvider` 全仓 0 调用点 → 已死 | 可删（P1） |
| C3 | LocalWebAPI 引用模块数 | 任务书称 7，实际 8（含 Reports） | 任务书勘误 |
| C4 | 本地种子数据 | 技术总监标注「待验证」；Mimo 确认双端共享 IdentitySeedData + 本地额外 3 条英文样例 | 样例数据归属问题（P2） |

### 📌 单方独有（不冲突，互补）

| 来源 | 独有发现 |
|------|---------|
| 技术总监 | 双轨检查点 5（种子数据 ✅ 设计正确，EnsureCreated vs Migrate 漂移风险 P2） |
| Mimo | 本地配置链 `ConcurrentDictionary` 内存存储重启即失（链 D，P1 候选）；本地客户端 `POST /patients/import` vs 远程 `batch-import` URL 不对齐（A11）；报表 5 端点无桌面消费（A4） |

---

## 二、合并定级（终版）

### P0（必须处理，本地模式核心功能）

**C1：本地模式 CRUD 主干断裂**
- 患者：GetList/Create/Update 缺失 → 本地无法列表/新增/编辑患者
- 药材：GetList/Create/Update/Delete/ToggleStatus 缺失 → 本地无法管理药材
- 验方：GetList/Create/Update/Delete/ToggleStatus 缺失 → 本地无法管理验方
- 医案：GetList/search/print-completed/prescription-flag 缺失 → 本地无法列表医案、打印审计静默失败
- 建议方向：**三选一**（见 §三）——补 override / 补本地实现 / 评估本地模式真实使用场景

### P1（应修，收敛批次）

| # | 项 | 建议 |
|---|-----|------|
| P1-1 | Desktop 模块间引用无守卫 + Registration→MedicalCase 违规 | 契约下沉 Contracts + 补架构测试（DP07） |
| P1-2 | 契约双套（M1）：Api/* + ApiClient/* | 以 ApiClient 为统一面，Api 内化为远程实现 |
| P1-3 | 领域客户端双实现（M2）：12 对 adapter | 评估收敛为单一实现层 |
| P1-4 | CorrelationId 三轨（M4） | 删 AsyncLocal 侧，统一 Activity + 中间件 |
| P1-5 | Server 映射双轨（M6）：4 模块手写但引 Mapperly 包 | 手写类改 partial + [Mapper] |
| P1-6 | 本地配置内存存储重启即失（Mimo 链 D） | 若本地配置是需求，落库/落文件 |
| P1-7 | 08-shared.md 结构性过时 | 按实际 5 项目重写 |

### P2（可后议）

Desktop.Infrastructure 职责过载 / Shared.Models 一项目八职责 / Roles 引用文档化 / 本地英文样例数据 / EnsureCreated vs Migrate / 报表 5 端点无消费 / 模块 DbContext 连接串双路径（A8，需运行验证）/ 文档偏差 13 项修正

---

## 三、给产品负责人的决策点

### 决策 1：P0 本地 CRUD 断裂怎么处理？（最重要）

| 选项 | 内容 | 代价 |
|------|------|------|
| A | **补全本地 override**：让本地患者/药材/验方 CRUD 走通（复用 Server Service，只需在本地 Controller 补 ~15 个 override） | 0.5-1d，双轨真正对称 |
| B | **评估本地模式真实使用场景**：如果本地模式主要用来离线演示/单机小诊所，确认哪些端点必需，只补必需的 | 需业务确认，可能只需补列表+创建 |
| C | 先确认本地模式是否 v1.0 必须 | 若本地非刚需，可降级 P1 |

我推荐 **A**（补全 override）——复用 Server Service 成本很低（对照远程 Controller 抄 override 即可），且与「双轨真共享」设计一致，不做兼容层、一次到位。

### 决策 2：P1 收敛批次是否全做？

契约双套 + 双实现 + 映射 + CorrelationId 四个收敛（P1-2~P1-5）是「架构完全可控」的核心，建议一个批次做完（预计 2-3d），做完后新增端点只需写一遍。

---

## 四、待运行验证项（静态证据推断，运行确认后定级）

| # | 项 | 风险 |
|---|-----|------|
| V1 | 模块 DbContext（Users/Herbs/Formula/Auth）本地连接串经 Resolver 解析 vs AppDbContext 显式 LocalDB 串——是否同库 | 若不同库：本地用户/药材/配方数据落「另一个库」，用户感知数据丢失 |
| V2 | P0 的 500/405 实际行为（静态证据已充分，运行仅确认） | — |

建议修复批次开工前先做一次本地模式冒烟测试（启动 Desktop 本地模式 → 患者列表/新建 → 药材列表/新建）验证 V1/V2。
