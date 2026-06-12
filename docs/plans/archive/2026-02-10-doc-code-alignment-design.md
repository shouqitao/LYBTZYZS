# 文档-代码对齐补全设计

## 背景

对 10 个 Controller (104 个 API 端点)、8 个 Desktop 模块、8 个需求文档 (92 个 FR)、7 个 API 参考文档 (86 个端点记录) 进行全面交叉审计后，发现 8 个核心业务模块代码-文档 100% 对齐，但存在 4 类系统级缺口。

## 审计结论

### 已对齐 (无需修改)

| 模块 | 代码端点 | API文档 | 需求FR | 状态 |
|------|---------|--------|--------|------|
| Auth | 6 | 6 | 13 | 完全对齐 |
| Users | 14 | 14 | 12 | 完全对齐 |
| Patients | 10 | 10 | 12 | 完全对齐 |
| Herbs | 16 | 17 | 13 | 完全对齐 |
| Formulas | 14 | 15 | 13 | 完全对齐 |
| MedicalCases | 24 | 18 | 17 | 完全对齐 |
| Sync | 6 | 6 | 8 | 完全对齐 |
| Printing | Desktop | - | 4 | 完全对齐 |

### 缺口清单

| # | 缺口 | 影响层 | 优先级 |
|---|------|--------|--------|
| G-1 | EntityAudit: 有7个API端点，无独立需求文档，无独立API参考 | 需求+API | P1 |
| G-2 | Health: 有3个API端点，无独立API参考 (运维README已概述) | API | P2 |
| G-3 | Diagnostics: 有4个API端点，无独立API参考 (运维README已概述) | API | P2 |
| G-4 | CardReader: 有Desktop模块，无任何文档 | 需求+架构 | P2 |
| G-5 | desktop.md缺少Controls/Dialogs组件规范 | 架构 | P2 |
| G-6 | 06-operations/ 缺少独立的deployment.md和configuration.md | 运维 | P3 |
| G-7 | mapperly-warning-fix-plan.md残留文件 | 清理 | P3 |
| G-8 | 02-requirements/README.md和04-api-reference/README.md需更新索引 | 索引 | P3 |

---

## 设计决策

### D-1: EntityAudit 独立为跨模块功能

**决策**: 创建独立需求文档 `docs/02-requirements/entity-audit.md` + 独立API参考 `docs/04-api-reference/entity-audit.md`

**理由**: EntityAuditController 支持 7 种实体类型的审计查询 (Patient/Herb/Formula/User/Consultation/Prescription + 通用)，是跨模块基础设施，不应仅作为 medical-cases.md 的子功能。

**FR 编号**: FR-AUDIT-001 ~ FR-AUDIT-003
- FR-AUDIT-001: 通用实体审计日志查询
- FR-AUDIT-002: 类型化审计日志查询 (预定义快捷端点)
- FR-AUDIT-003: 审计日志展示 (Desktop 端)

**处理 FR-MC-012**: medical-cases.md 中 FR-MC-012 保留，添加交叉引用链接到 entity-audit.md。

### D-2: Health/Diagnostics 轻量级文档

**决策**:
- API参考层: 创建独立文档 (完整端点规格)
- 需求层: **不创建独立需求文档**。Health/Diagnostics 是运维基础设施，非用户功能。相关需求说明合并到 `docs/06-operations/` 对应章节。

### D-3: CardReader 需求 + 架构文档

**决策**:
- 需求层: 创建 `docs/02-requirements/16-card-reader.md` (FR-CARD-001 ~ FR-CARD-002)
- 架构层: 在 desktop.md 中新增 CardReader 集成章节

**FR 编号**:
- FR-CARD-001: 身份证读卡器连接与读取
- FR-CARD-002: 读卡数据填充到患者表单

### D-4: Desktop 架构文档补充组件层

**决策**: 在 desktop.md 中新增两个章节:
1. **可复用业务控件**: HerbListControl, HerbItemControl
2. **业务弹窗**: FormulaImportDialog, HistoryCopyDialog, SyncConflictDialog, UnsavedChangesDialog, UnfinishedCaseDialog

### D-5: 运维文档拆分

**决策**: 将当前 `docs/06-operations/README.md` 的内容拆分为三个文件:
- `README.md`: 概述 + 索引 (精简)
- `deployment.md`: 部署相关 (发布命令、目录结构、环境配置)
- `configuration.md`: 配置参考 (所有 appsettings.json 配置项详解)

README.md 保留概述和健康检查/日志部分，deployment.md 和 configuration.md 提取详细内容。

### D-6: 索引更新

**决策**:
- `docs/02-requirements/README.md`: 新增 EntityAudit 和 CardReader 模块索引行
- `docs/04-api-reference/README.md`: 新增 EntityAudit、Health、Diagnostics 的独立文档链接，更新系统模块索引

---

## 文件变更清单

### 新增文件 (8个)

| 文件 | 内容 | 信息源 |
|------|------|--------|
| `docs/02-requirements/entity-audit.md` | EntityAudit 需求 (FR-AUDIT-001~003) | EntityAuditController.cs |
| `docs/02-requirements/16-card-reader.md` | CardReader 需求 (FR-CARD-001~002) | LYBT.Desktop.CardReader/ |
| `docs/04-api-reference/entity-audit.md` | EntityAudit API 参考 (7端点) | EntityAuditController.cs |
| `docs/04-api-reference/11-health.md` | Health API 参考 (3端点) | HealthController.cs |
| `docs/04-api-reference/12-diagnostics.md` | Diagnostics API 参考 (4端点) | DiagnosticsController.cs |
| `docs/06-operations/01-deployment.md` | 部署指南 (从README.md提取) | 现有 README.md |
| `docs/06-operations/02-configuration.md` | 配置参考 (从README.md提取) | 现有 README.md + appsettings.json |
| `docs/plans/2026-02-10-doc-code-alignment-plan.md` | 本次实施计划 | 本设计文档 |

### 修改文件 (5个)

| 文件 | 变更 |
|------|------|
| `docs/02-requirements/README.md` | 添加 EntityAudit、CardReader 索引行，更新总计 |
| `docs/02-requirements/07-medical-cases.md` | FR-MC-012 添加交叉引用链接 |
| `docs/03-architecture/02-desktop.md` | 新增 Controls、Dialogs、CardReader 三个章节 |
| `docs/04-api-reference/README.md` | 新增3个独立文档链接，更新系统模块索引 |
| `docs/06-operations/README.md` | 精简为概述+索引，详细内容迁移到子文件 |

### 删除文件 (1个)

| 文件 | 原因 |
|------|------|
| `docs/mapperly-warning-fix-plan.md` | 旧文档体系残留 |

**总计: 8 新增 + 5 修改 + 1 删除 = 14 个文件操作**

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始设计 |
