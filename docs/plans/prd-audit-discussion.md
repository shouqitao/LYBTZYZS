# PRD 审计讨论文件（v2.0 近景）

编制目的：结合现有 PRD 文档与实现状态，为产品负责人开展需求对齐讨论，明确已实现范围、存在差距及后续工作优先级。

数据基线与结论：当前项目状态如下所示，来自文档与实现的一致性核对结果。
- US 总数：138（来自 PRD 的 15 个模块总计，US 编号区间均来自文档：US-AUTH-001 ~ US-REG-007 等，见 docs/02-requirements/README.md）
- 模块数量：15（同上文档标注，包含认证、用户、患者、药材、验方、医案、同步、打印、读卡、健康诊断、异常处理、日志、Desktop Shell、配置、挂号等）
- 测试用例总数：约 2021 项（3 套测试项目：Server/Desktop/Architecture；具体分布见 docs/README.md 的测试总览）
- 架构：三层架构（Client/Server/Shared），并支持远程 SQL Server 与本地 SQLite 双模式运行；当前官方架构描述请参阅 docs/03-architecture/system-overview.md。
- 现状概览：本地双模式实现基于 LocalWebAPI（嵌入式 Kestrel + SQLite）替代 LocalDB，详见 docs/03-architecture/dual-mode.md；LocalWebAPI 尚无专门 ADR。

下面进入逐模块的对照矩阵与分析。请以此为讨论起点，现场确认优先级与后续工作。

## 1. PRD 模块对照矩阵

注：PRD 版本以 v2.1 为准（2026-03-06 版本线更新，新增 Registration 模块，总 US 由 131 增至 138，最终在 v2.1 将 15 个模块完整覆盖）。以下表格按文档所列 15 个模块整理，US 总数来自 docs/02-requirements/README.md。

| 模块 | PRD 版本 | US 总数 | 实现状态 | 具体缺口（缺失的 US） | 备注/讨论要点 |
|---|---|---:|---|---|---|
| 认证与会话管理 | v2.1 | 13 | ✅ Fully Implemented | 无显著缺口 | 全部认证相关 US 已落地，JWT/自动登录等均覆盖 |
| 用户管理 | v2.1 | 12 | ✅ Fully Implemented | 无显著缺口 | 用户 CRUD/权限分配等均在实现内完成 |
| 患者管理 | v2.1 | 13 | ✅ Fully Implemented | 无显著缺口 | 拼音码搜索、导入导出等均实现 |
| 药材管理 | v2.1 | 13 | ✅ Fully Implemented | 无显著缺口 | CRUD 与引用检查实现完备 |
| 验方管理 | v2.1 | 13 | ✅ Fully Implemented | 无显著缺口 | 验方 CRUD/引用绑定等完善 |
| 医案管理 | v2.1 | 18 | ✅ Fully Implemented | 无显著缺口 | 完整 CRUD+聚合保存，权限控制完备 |
| 数据同步 | v2.1 | 8 | ⚠️ Partially Implemented | 需要验证 MedicalCase 冲突 UI、字段级对比等 | 已实现双向同步和基础差异检测，但某些冲突处理 UI/字段对比仍需确认 |
| 打印 | v2.1 | 4 | ✅ Fully Implemented | 无显著缺口 | 处方打印、预览、版本追溯、日志等均实现 |
| 身份证读卡器 | v2.1 | 2 | ⚠️ Partially Implemented | 硬件集成状态待确认，南向与局部驱动适配仍需验证 | 读卡器模块存在硬件依赖，代码与流程已实现，但硬件可用性待现场测试 |
| 系统健康与诊断 | v2.1 | 9 | ✅ Fully Implemented | 无显著缺口 | 健康检查与诊断日志等实现完备 |
| 异常处理策略 | v2.1 | 8 | ✅ Fully Implemented | 无显著缺口 | 全局异常、错误码等实现完备 |
| 日志与审计 | v2.1 | 7 | ✅ Fully Implemented | 无显著缺口 | 结构化日志与审计实现完备 |
| Desktop Shell | v2.1 | 7 | ✅ Fully Implemented | 无显著缺口 | 启动/会话/导航等实现完备 |
| 配置参数 | v2.1 | 4 | ✅ Fully Implemented | 无显著缺口 | 环境配置与热加载等实现完备 |
| 挂号管理 | v2.1 | 7 | ✅ Fully Implemented | 无显著缺口 | 挂号历史、队列与过滤等已实现 |

> 总结：除了数据同步与卡读卡器处在讨论中的“待验证/待现场确认”的状态外，其余模块在 PRD 与实现之间的对齐度较高，已覆盖 138 条 US 的全量实现要求。|

## 2. 架构对齐问题（Architecture Alignment）
- 双模架构描述仍沿用 docs/02-requirements/README.md 与 docs/03-architecture/dual-mode.md 的叙述，但实际实现使用 LocalWebAPI（嵌入式 Kestrel + SQLite）替代 LocalDB 做本地访问，导致部分实现细节已变化但 ADR 仍在演进中。
- 具体变化点：当前 Local 模式采用 LocalWebAPI 的 HTTP Proxy Repository，远程为 Refit HTTP，Local 模式仍然通过仓库工厂实现路由。参见 docs/03-architecture/dual-mode.md 的实现细节与 LocalWebAPI 架构章节。
- LocalWebAPI 缺少专门 ADR，当前 ADR 指南中仍以 LocalDB 的路径描述，导致 ADR-0002 的原始定位需要更新以反映当前实现路径。
- PRD 架构章节存在滞后：系统总体架构/双模实现的变更未在 PRD 的相应章节中及时更新，需要补充 ADR 和具体实现变更的记录。

参考资料：
- docs/03-architecture/dual-mode.md（本地模式替换为 LocalWebAPI 的设计说明）
- docs/03-architecture/system-overview.md（三层架构与双模的总览）
- docs/02-requirements/README.md（PRD 版本演进与模块定义）

## 3. 数据准确性问题（Data Accuracy）
- Roadmap 文档中的测试数量口径与实际实现中的测试口径不一致：roadmap.md 声称共有 1654 项测试用例通过；而仓库文档与实际测试项目统计显示约 2021 项测试用例（Server/Desktop/Architecture 三大测试集合）。需要统一口径，避免对外传达错误的测试覆盖度。
- 版本号与分支文档的版本管理也呈现滞后：docs/02-requirements/README.md 对应的版本号自 v2.0 升级至 v2.1，但工程中的其他文档版本时间线并不完全一致，需统一版本标识和时间线。
- 小结：需要建立一个“活文档仪表板”，对关键版本、US 累计数、测试覆盖、以及架构 ADR 的状态进行实时跟踪更新。

参考资料：
- docs/02-requirements/roadmap.md（1654 tests 的口径对照）
- docs/02-requirements/README.md（15 模块/138 US 与 v2.1 的记录）
- docs/README.md（总览中的测试口径与版本历史）

## 4. 具体模块关注点（Module Concerns）
- Sync 模块（docs/02-requirements/sync.md，v4.x 版本）
  - US 数量：8
  - 现状：SHA256 差异检测实现、双向同步实现，但对 MedicalCase 的冲突解析 UI 仍需验证；冲突对话框与冲突明细数据结构（SyncConflictDetailDto）存在延期实现风险。
  - 参考：docs/02-requirements/sync.md 的冲突解决描述与延期项（如 SYNC-02/03 的实现依赖）以及 Sprint 6 的实现记录。
- Card Reader 模块（docs/02-requirements/card-reader.md，v2.0/2.1）
  - US 数量：2
  - 现状：LYBT.Desktop.CardReader 存在代码实现，但对华大 HD100 硬件的实际集成状态与现场可用性尚未完成验证，硬件测试依赖现场环境。
  - 参考：docs/02-requirements/card-reader.md 的“硬件集成状态待现场测试”描述。
- Printing 模块（docs/02-requirements/printing.md，v4.x）
  - US 数量：4
  - 现状：WPF FixedDocument 已实现，PDF 导出已通过 QuestPDF 实现，打包输出稳定，处方打印完整闭环。
  - 参考：docs/02-requirements/printing.md 的详细实现与演示段落（US-PRINT-001~004）及导出格式章节。
- 其他模块：Auth/Users/Patients/Herbs/Formulas/MedicalCase/Health/Error/Logging/Desktop Shell/Config/Registration 均已较为完整实现，具体 US 可以在 docs/02-requirements/README.md 的每个模块条目中对照核验。

## 5. 待讨论的关键问题（Open Questions for Discussion）
- Q1: v2.0 PRD 范围的优先级排序应如何界定？Should/Could 项中哪些是必须优先实现的？
- Q2: LocalWebAPI 的定位应如何定位？是否应替换为 Direct LocalDB 直连，还是继续沿用 LocalWebAPI 代理共存？
- Q3: Sync 模块中的 MedicalCase 冲突解决 UI 是否需要在 v2.0 版本内完成？若延期，是否以简化的冲突处理替代？
- Q4: Card Reader 的华大 HD100 硬件是否可用于测试环境？若不可用，是否应加大 Mock 流程的覆盖？
- Q5: 下一阶段的总发展方向，应优先解决现有差距还是推进新的功能点？是否需要建立“ Living PRD/仪表盘”？

## 6. Proposed v2.0 PRD 结构调整（Proposed v2.0 PRD Structure）
- 将 docs/02-requirements 重新组织为 modules/ 与 cross-cutting/ 子目录，便于跨模块协同与快速定位。
- 在每个模块文档中增加 Implementation Status 字段，清晰标注已实现的 US 与待实现项。
- 新建 living requirements dashboard，定期自动汇总模块实现状态、测试覆盖、风险与待解问题。
- 同步 ADR 记录：在 docs/03-architecture/decisions/ 下增设本地/远程实现的 ADR，更新 LocalWebAPI ADR 的状态。

## 7. Recommended Action Items（优先级建议，简要）
- [高] 确认 Sync 模块与 Card Reader 的现场可用性与测试覆盖范围，尽快把关键缺口转为实现任务。
- [高] 补充 LocalWebAPI 的 ADR 与实现细节，确保 ADR-0002 与实际实现保持一致。
- [高] 统一版本号与时间线：对 roadmaps 与 docs/README 的版本与口径进行对齐，避免外部对外发布的冲突。
- [高] 建立 living PRD 仪表盘，持续跟踪 US 完成率、测试覆盖、风险项与依赖。
- [中] 更新 PRD 的架构章节与 ADR，确保与当前的本地双模实现一致。
- [中] 完成对 Health/Diagnostics/Logging 的要点审阅，确认是否有遗漏的用例或 UI 细节。
- [中] 制定后续迭代计划，优先解决 DPR 中的“冲突 UI/字段级对比 UI”的落地，避免视图阶段性缺口。
- [低] 将以上内容同步至 planning 文档中，形成执行计划与验收准则的闭环。

> 注：本文档用于就绪的 PRD 审计讨论，具体实现变更应在后续协商后以任务形式落地到计划文件中。

---

来源与参考（关键文件）
- docs/02-requirements/README.md（模块索引与 US 数量、PRD 版本信息）
- docs/02-requirements/auth.md、users.md、patients.md、herbs.md、formulas.md、medical-cases.md、sync.md、printing.md、card-reader.md、health-diagnostics.md、error-handling.md、logging.md、desktop-shell.md、configuration.md、registration.md（各模块 PRD 文档）
- docs/03-architecture/system-overview.md、docs/03-architecture/dual-mode.md（架构与双模实现）
- docs/02-requirements/roadmap.md（Roadmap 与测试规模对照）
- docs/README.md（文档总览、测试口径、版本演进）

---

最后更新：请在会后回填具体结论和行动项，确保所有条目都在 planning 文件中更新并形成闭环。
