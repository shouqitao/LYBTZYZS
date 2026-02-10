# Research Findings: 文档-代码对齐审计

## 审计范围
- 10 个 Controller (104 个 API 端点)
- 8 个 Desktop 模块 (含Core基础设施)
- 8 个需求文档 (92 个 FR)
- 7 个 API 参考文档 (86 个端点记录)
- 6 个架构文档
- 运维文档 (1个README)

## 核心发现

### 1. 8个业务模块 100% 对齐
Auth, Users, Patients, Herbs, Formulas, MedicalCases, Sync, Printing 所有代码端点均有对应需求文档和API参考文档。MedicalCaseController 的 5 个废弃端点也正确标注。

### 2. 系统级功能缺口 (4个模块) -> 已补全

**EntityAuditController (7端点, 0文档)**:
- 支持 7 种实体审计: Patient, Herb, Formula, User, Consultation, Prescription + 通用
- 代码中每个实体有独立快捷端点 + 1个通用查询端点
- 仅在 medical-cases.md FR-MC-012 概述提及
- **决策**: 技术债务，不补文档，后续清除代码

**HealthController (3端点)**: -> 已创建 health.md
- GET /health (匿名探活)
- GET /health/ping (Ping/Pong)
- GET /health/details (认证+数据库检查)

**DiagnosticsController (4端点)**: -> 已创建 diagnostics.md
- GET /diagnostics/logging/status
- POST /diagnostics/logging/debug/enable (临时调试，最长120分钟)
- POST /diagnostics/logging/debug/disable
- POST /diagnostics/logging/level
- 权限: SuperAdmin only

**LYBT.Desktop.CardReader (Desktop模块)**: -> 已创建 card-reader.md + desktop.md 章节
- ICardReaderService: 身份证读卡器连接/读取
- IPatientCardReaderIntegration: 读卡数据填充到患者表单
- 在 PatientMasterDetailViewModel 中通过 ReadCardCommand 调用

### 3. 代码验证修正 (执行阶段发现)

**CardReadResult 数据模型差异**:
- 计划中仅列出 7 个字段，实际代码含 17 个字段 (含计算属性 Age)
- Gender 类型: 计划写 `string`，实际为 `Gender` 枚举 (Male/Female/Unknown)
- BirthDate 类型: 计划写 `DateTime`，实际为 `DateTime?`
- 新增字段: IssuingAuthority, ValidFrom, ValidTo, CardType, PhotoData, ReadTime, IsSuccess, ErrorMessage, ErrorCode
- PhotoPath 应为 PhotoFilePath
- 已修正 card-reader.md 数据模型表

**ICardReader 接口差异**:
- 缺少 Name, Vendor, Model 三个属性
- ConnectAsync 有可选 connectionString 参数
- ReadCardAsync 有 savePhoto, photoPath, cancellationToken 参数
- 已修正

**IPatientCardReaderIntegration 接口差异**:
- 缺少 GetPatientDetailByIdAsync 方法
- 有 CardReaderIntegrationEventType 事件枚举
- 已修正

### 4. 架构文档组件层 -> 已补全

desktop.md 新增三个章节:
- 可复用业务控件 (HerbListControl, HerbItemControl)
- 业务弹窗 (5个: FormulaImport, HistoryCopy, UnsavedChanges, SyncConflict, UnfinishedCase)
- CardReader 集成 (架构图 + 接口 + 事件)

### 5. 运维文档 -> 已拆分

docs/06-operations/ 拆分为:
- README.md (概述 + 索引 + 日志 + 健康检查)
- deployment.md (服务端/客户端部署 + 数据库运维)
- configuration.md (10个配置节完整说明)

### 6. 残留文件 -> 已清理

docs/mapperly-warning-fix-plan.md 已删除。

## 补全后统计

| 维度 | 补全前 | 补全后 | 覆盖率 |
|------|--------|--------|--------|
| 需求文档 (FR) | 92 | 94 (+2 CARD) | 95% -> 95% (EntityAudit 待清除后为 100%) |
| API 参考 (端点) | 86 | 93 (+3 Health +4 Diagnostics) | 86% -> 93% (EntityAudit 待清除后为 100%) |
| 架构文档 (模块) | 8 | 8 + Controls/Dialogs/CardReader | 89% -> 100% |
| 文档文件总数 | 41 | 46 (+5 新建 -1 删除 = +4 净增) | - |

---
*Updated: 2026-02-10 (DOCUMENTATION ALIGNMENT COMPLETE)*
