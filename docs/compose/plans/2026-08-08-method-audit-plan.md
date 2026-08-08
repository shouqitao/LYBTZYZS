# 全 Solution 方法级深度审查方案（收敛专项）

> 版本: v1.0 | 日期: 2026-08-08 | 维护者: 技术总监
> 用户方针（2026-08-08 确立）：**先收敛再完善**——本次不做功能完善（B 类冻结），只做收敛
> 目标：方法级深度审查 → 整体整合方案（项目合并 + 机制集中定义）→ 分批执行收敛
> 关联: `docs/00-governance/03-technical-adoption-governance.md`（T2/T3 流程）｜A-22 类级审查报告（`structure-audit-perclass-mimo-2026-08-08.md`，已到类级，本次升级到方法级）

---

## 一、背景与现状基线

**已完成的基础**（本次审查的起点）：
- A-22 类级审查：34 项目 / 1422 类型，A/B/C/D 分级（A=1009/B=322/C=62/D=29），93.6% 有设计依据
- A-23 孤儿类处置：D=29 已清理 2780 行
- A-26~A-29 收敛：双轨规范化、仓储收敛、技术栈减法、P1/P2 全闭

**方法级审查的必要性**：类级审查止步于"类是否有依据"，但**类内部的方法**仍有大量收敛空间——
- 同职责方法跨项目重复（如 AddLogging/GetByIdAsync/CorrelationId 实现散落 5+ 处）
- 死方法（类存活但方法 0 调用，A-22 仅在 MedicalCase 发现 6 个，全仓未知）
- 可集中定义的机制（日志/异常/配置/映射/验证）实现位置分散

**当前项目结构**：35 项目（Shared 5 / Server 10 / Desktop 16 / Tests 3 / Tools 1），~1900 顶层类型，1028 业务 cs 文件，预计 6000+ 方法。

---

## 二、审查目标

1. **方法级统计**：每项目每类每方法——依据分级 + 调用链 + 重复聚类
2. **重复识别**：全仓同职责方法聚类（收敛候选）
3. **死方法识别**：0 调用方法全清单
4. **集中定义识别**：日志/异常/配置/映射/验证等机制应集中到哪个项目（含你点名的日志独立完整项目）
5. **整合方案产出**：项目合并方案（35→N）+ 机制集中定义方案 + 执行批次规划

**明确不做**：❌ 不完善功能（B 类冻结）❌ 不修 bug（顺带发现只记录）❌ 不执行合并（只出方案）

---

## 三、审查方法论

### 3.1 方法级判定标准（A-22 的 A/B/C/D 扩展为 A/B/C/D/E）

| 级 | 定义 | 处置 |
|----|------|------|
| **A 依据充分** | 方法有设计依据 + 真实调用链 | 保留 |
| **B 支撑性** | 从属 A 类类的辅助方法（private/内部） | 保留 |
| **C 重复/分散** | 同职责方法在 2+ 处实现（跨项目/跨类） | ⭐ 收敛候选 |
| **D 孤儿/死方法** | 0 调用（类存活但方法死） | 删除候选 |
| **E 可集中** | 机制性方法应集中到特定项目（日志/异常/配置/错误码/映射） | ⭐ 集中定义候选 |

### 3.2 审查工具链

- **自动化基线**：脚本扫全仓 → 方法清单（项目/类/方法/签名/行号）+ 调用关系初筛 + 同名聚类
- **符号级验证**（MCP）：serena（find_symbol/find_referencing_symbols）+ codegraph（调用链）+ codebase-memory（图谱，注意索引过期需重建）
- **人工复核**：每个 C/D/E 级发现读源码确认，grep 只做初筛

### 3.3 分层审查（5 阶段，顺序推进）

| 阶段 | 对象 | 重点 | 产出 |
|------|------|------|------|
| **S0 准备** | 全仓 | 方法级统计基线（自动化脚本） | 方法总账 + 重复聚类初筛 |
| **S1 Shared** | 5 项目（178 类） | 日志/异常/配置/错误码集中定义专项 | Shared 整合方案 |
| **S2 Server** | 10 项目（288 类） | 横切机制、Service/Handler 方法重复、跨模块方法 | Server 合并方案 |
| **S3 Desktop** | 16 项目（703 类） | VM 方法/命令重复、DTO↔Model 映射、双轨差异方法 | Desktop 合并方案 |
| **S4 整合** | 全部产出 | 合并蓝图 + 集中定义方案 + 执行批次 | **15-solution-integration-plan.md** |

每阶段：Mimo 独立审查报告 + 技术总监交叉验证 → 独立验收 → 下一阶段。

---

## 四、专项设计（你点名的两件事）

### 专项 A：日志独立完整项目

**现状证据**（已核实）：
- `LYBT.Shared.Logging` 仅 8 文件（CorrelationId/Enricher/Masking/LevelManager）
- 但配置实现**散落 5 处**：`DesktopSerilogConfiguration.cs`（Desktop.Infrastructure）、`SerilogMSSqlServerExtensions.cs`（WebAPI）、`LoggingRegistrationExtensions.cs`（Shell）、`LoggingHttpHandler`（Foundation）、CorrelationId 机制散落 5+ 文件

**审查问题**：
1. 日志的「配置/初始化/CorrelationId/脱敏/MSSQL sink」是否应收敛回 `LYBT.Shared.Logging`？
2. `LYBT.Shared.Logging` 是否应升级为「独立完整日志项目」（全程接管 Server+Desktop 日志，对外只暴露初始化入口 + ILogger 工厂）？
3. 收敛后 Desktop/Server 各自的 Serilog 扩展应删还是保留薄壳？

### 专项 B：异常统一设计

**现状证据**：
- `LYBT.Shared.ExceptionHandling` 7 文件（AppException 层次）✅ 已集中
- 但 `SystemExceptionHandler`/`BusinessExceptionHandler` 在 **Infrastructure**（不在 ExceptionHandling 项目）
- 错误码 `ErrorCode.cs` 在 `Shared.Models/Primitives/ErrorCodes/`

**审查问题**：
1. 异常处理器是否应收敛到 `LYBT.Shared.ExceptionHandling`（与异常层次同项目）？
2. 错误码枚举是否应收敛到 ExceptionHandling（或与异常层次同目录）？
3. 异常→HTTP 映射是否应成为 ExceptionHandling 的完整职责（含处理器注册扩展方法）？

### 专项 C：其他可集中机制（审查中识别）

配置（Options 验证器）、映射（Mapperly 注册）、验证（FluentValidation 管道）、批处理（BatchOperationHandlerBase vs BatchImport 第二模板）——逐项评估集中位置。

---

## 五、产出物

| 产出 | 位置 | 性质 |
|------|------|------|
| 方法级审查报告（每阶段一份） | `docs/compose/reports/method-audit-{layer}-2026-08-08.md` | 过程文档 |
| 每项目深化统计表（类型→方法→依据→重复聚类） | 同上附章 | 过程文档 |
| **整体整合方案** | `docs/03-architecture/15-solution-integration-plan.md` | **正式文档（SSOT）** |
| 执行批次规划（先收敛后完善） | 总账 A 类新增 | 正式文档 |

---

## 六、规模与时间预估（保守）

| 阶段 | 工作量 | 说明 |
|------|--------|------|
| S0 准备 | 0.5d | 自动化脚本 + 基线 |
| S1 Shared | 1-1.5d | 含日志/异常专项深挖 |
| S2 Server | 1-1.5d | 10 项目方法级 |
| S3 Desktop | 1.5-2d | 16 项目方法级（最重） |
| S4 整合 | 0.5-1d | 方案合并 + 蓝图 |
| **合计** | **4.5-6.5d** | 分阶段派发，每阶段独立验收 |

**说明**：审查是只读的，不阻塞代码开发；但按「先收敛再完善」方针，**收敛执行批次应在审查完成后开始**，B 类功能开发冻结至收敛完成。

---

## 七、执行流程（每阶段）

```
① 阶段任务书（技术总监写，引用本方案 + 蓝图）
② 派发 Mimo 独立审查（只读）
③ Mimo 产出报告（方法级统计 + C/D/E 清单 + 专项分析）
④ 技术总监交叉验证（抽查关键发现）
⑤ 独立验收（build + 架构测试 + 残留 grep——审查本身不改代码，验证的是报告准确性）
⑥ 阶段产出归档 → 下一阶段
```

## 八、风险与边界

- **审查时间长**（4.5-6.5d）：每阶段独立交付，用户可随时看进度；S1 完成后即可看到日志/异常专项结论（最高价值）
- **codebase-memory 索引过期**：S0 需重建索引（运维动作）或审查改用 serena/codegraph + 源码走查
- **不执行合并**：本方案只产出整合方案，执行批次单独立项（用户拍板后）
- **不完善功能**：B 类冻结至收敛完成（用户方针「先收敛再完善」）
