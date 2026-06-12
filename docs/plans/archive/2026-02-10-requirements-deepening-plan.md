# 需求文档深化 -- 回填实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 消除 docs/ 中全部 57 处"待讨论/TBD/待扩展"标记，基于代码事实回填明确决策，使需求文档从"草稿"状态升级为"已确认"状态。

**Architecture:** 纯文档编辑任务，无代码变更。每个 Task 对应一个文件，按设计文档 `docs/plans/2026-02-10-requirements-deepening-design.md` 中的决策清单逐项回填。回填内容来自对 15+ 个核心源文件的逆向分析结论。

**Tech Stack:** Markdown 文档编辑

**Design Reference:** `docs/plans/2026-02-10-requirements-deepening-design.md` (22 个决策点，11 个回填任务)

---

## Task 概览

| Task | 文件 | 标记数 | 决策来源 |
|------|------|--------|----------|
| 1 | auth.md | 4 | B-1, B-2 |
| 2 | users.md | 14 | A-2, A-3 |
| 3 | patients.md | 5 | D-1, D-2 |
| 4 | herbs.md | 6 | D-1, F-1 |
| 5 | formulas.md | 5 | D-1, F-2 |
| 6 | medical-cases.md | 5 | E-1, E-2, E-3 |
| 7 | sync.md | 5 | C-1, C-2, C-4, C-5 |
| 8 | printing.md | 4 | F-3, F-4, F-5 |
| 9 | README.md (requirements) | 2 | 汇总更新 |
| 10 | dual-mode.md | 6 | A-1, C-1, C-2, C-3, C-5 |
| 11 | 0002-dual-mode-architecture.md | 1 | 综合 |
| **合计** | **11 文件** | **57** | |

---

## Task 1: 更新 docs/02-requirements/02-auth.md

**Files:**
- Modify: `docs/02-requirements/02-auth.md` (4 处标记)

**Design Reference:** 设计文档主题 B-1 (自动登录), B-2 (会话超时)

**Step 1: 修改 FR-AUTH-002 本地模式行为**

位置: 第 51 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 不支持。本地模式无 Token 机制，不提供自动登录功能。每次启动应用需手动输入用户名和密码登录
```

**Step 2: 修改待讨论项表格 #1**

位置: 第 297 行
```
原文: | 1 | 本地模式下自动登录的实现方式 | FR-AUTH-002 | 待讨论 |
替换: | 1 | 本地模式下自动登录的实现方式 | FR-AUTH-002 | 已确定: 不支持 (无 Token 机制，每次手动登录) |
```

**Step 3: 修改待讨论项表格 #2**

位置: 第 298 行
```
原文: | 2 | 本地模式下的会话超时是否需要与远程一致 | FR-AUTH-006 | 待讨论 |
替换: | 2 | 本地模式下的会话超时策略 | FR-AUTH-006 | 已确定: 不适用。本地模式无 Token 超时，登录状态持续到应用退出。安全保障: 5次失败锁定15分钟 |
```

**Step 4: 在 FR-AUTH-006 附近补充本地模式行为说明**

在 auth.md 中找到 FR-AUTH-006 (会话管理/Token刷新) 的本地模式字段，如存在"待讨论"则替换为:
```
- **本地模式**: 不适用。本地模式无 Token 超时机制，登录状态持续到应用退出
```

**Step 5: 验证**

运行: `grep -c "待讨论" docs/02-requirements/02-auth.md`
Expected: 0

**Step 6: Commit**

```bash
git add docs/02-requirements/02-auth.md
git commit -m "docs(requirements): auth.md 回填本地模式决策 (B-1, B-2)"
```

---

## Task 2: 更新 docs/02-requirements/03-users.md

**Files:**
- Modify: `docs/02-requirements/03-users.md` (14 处标记)

**Design Reference:** 设计文档主题 A-2 (用户管理支持范围), A-3 (Receptionist边界)

**Step 1: 批量替换 FR-USER-001~011 的本地模式 (11 处)**

位置: 第 35, 51, 63, 77, 94, 108, 122, 137, 152, 165, 179 行
每处格式相同:
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持 (LocalUserDataSource)。本地 SQLite 存储，功能与远程模式对等
```

> **注意**: 11 处替换内容相同，可使用全局替换。但需确认每处上下文都是 FR-USER 的本地模式行，而非其他内容。

**Step 2: 修改待讨论项表格 #1**

位置: 第 227 行
```
原文: | 1 | 本地模式下用户管理的支持范围 | 所有 FR-USER | 待讨论 |
替换: | 1 | 本地模式下用户管理的支持范围 | 所有 FR-USER | 已确定: 完整支持。LocalUserDataSource 11/11 方法全覆盖，DI 注册为 IUserDataSource 本地实现 |
```

**Step 3: 修改待讨论项表格 #2**

位置: 第 228 行
```
原文: | 2 | Receptionist 角色的具体功能边界 | FR-USER-001 | 待讨论 |
替换: | 2 | Receptionist 角色的具体功能边界 | FR-USER-001 | 已确定: 仅查看权限 (患者列表 + 医案列表)。不在 DoctorOrAdmin / AdminOnly 策略中，无任何写操作权限 |
```

**Step 4: 验证**

运行: `grep -c "待讨论" docs/02-requirements/03-users.md`
Expected: 0

**Step 5: Commit**

```bash
git add docs/02-requirements/03-users.md
git commit -m "docs(requirements): users.md 回填本地模式+Receptionist决策 (A-2, A-3)"
```

---

## Task 3: 更新 docs/02-requirements/04-patients.md

**Files:**
- Modify: `docs/02-requirements/04-patients.md` (5 处标记)

**Design Reference:** 设计文档主题 D-1 (导入导出), D-2 (加密策略)

**Step 1: 修改 FR-PAT-008 本地模式 (导入)**

位置: 第 127 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。使用客户端 NPOI (ExcelHelper) 本地解析 Excel 文件，直接写入 LocalDbContext，不依赖服务端 API
```

**Step 2: 修改 FR-PAT-010 本地模式 (导出)**

位置: 第 151 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。从 LocalDbContext 查询数据，使用客户端 NPOI 本地生成 Excel 文件
```

**Step 3: 修改待讨论项表格 #1**

位置: 第 223 行
```
原文: | 1 | 本地模式下导入导出的支持方式 | FR-PAT-008 ~ 010 | 待讨论 |
替换: | 1 | 本地模式下导入导出的支持方式 | FR-PAT-008 ~ 010 | 已确定: 支持。客户端 NPOI 本地读写 Excel，不经过 API |
```

**Step 4: 修改待讨论项表格 #2**

位置: 第 224 行
```
原文: | 2 | 敏感数据在本地模式下的加密策略 | 所有敏感字段 | 待讨论 |
替换: | 2 | 敏感数据在本地模式下的加密策略 | 所有敏感字段 | 已确定: v1.0 不加密 SQLite，依赖 OS 用户权限 + 物理设备安全。v2.0 评估 SQLCipher |
```

**Step 5: 验证**

运行: `grep -c "待讨论" docs/02-requirements/04-patients.md`
Expected: 0

**Step 6: Commit**

```bash
git add docs/02-requirements/04-patients.md
git commit -m "docs(requirements): patients.md 回填导入导出+加密决策 (D-1, D-2)"
```

---

## Task 4: 更新 docs/02-requirements/05-herbs.md

**Files:**
- Modify: `docs/02-requirements/05-herbs.md` (6 处标记)

**Design Reference:** 设计文档主题 D-1 (导入导出), F-1 (价格影响)

**Step 1: 修改 FR-HERB-009 本地模式 (Excel 导入)**

位置: 第 132 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext
```

**Step 2: 修改 FR-HERB-010 本地模式 (JSON 导入)**

位置: 第 145 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。客户端本地解析 JSON 文件，直接写入 LocalDbContext
```

**Step 3: 修改 FR-HERB-011 本地模式 (导出)**

位置: 第 157 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件
```

**Step 4: 修改待讨论项表格 #1**

位置: 第 214 行
```
原文: | 1 | 本地模式下导入导出的支持方式 | FR-HERB-009 ~ 012 | 待讨论 |
替换: | 1 | 本地模式下导入导出的支持方式 | FR-HERB-009 ~ 012 | 已确定: 支持。客户端 NPOI/本地 JSON 解析，不依赖 API |
```

**Step 5: 修改待讨论项表格 #2**

位置: 第 215 行
```
原文: | 2 | 药材价格变更对已有处方的影响策略 | FR-HERB-004 | 待讨论 |
替换: | 2 | 药材价格变更对已有处方的影响策略 | FR-HERB-004 | 已确定: 不影响。PrescriptionItem.UnitPrice 为开方时快照值，新处方使用当前价格 |
```

**Step 6: 验证**

运行: `grep -c "待讨论" docs/02-requirements/05-herbs.md`
Expected: 0

**Step 7: Commit**

```bash
git add docs/02-requirements/05-herbs.md
git commit -m "docs(requirements): herbs.md 回填导入导出+价格快照决策 (D-1, F-1)"
```

---

## Task 5: 更新 docs/02-requirements/06-formulas.md

**Files:**
- Modify: `docs/02-requirements/06-formulas.md` (5 处标记)

**Design Reference:** 设计文档主题 D-1 (导入导出), F-2 (价格计算)

**Step 1: 修改 FR-FORM-011 本地模式 (批量导入)**

位置: 第 163 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。客户端 NPOI 本地解析 Excel，直接写入 LocalDbContext
```

**Step 2: 修改 FR-FORM-012 本地模式 (导出)**

位置: 第 173 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 支持。从 LocalDbContext 查询，客户端 NPOI 本地生成 Excel 文件
```

**Step 3: 修改待讨论项表格 #1**

位置: 第 235 行
```
原文: | 1 | 本地模式下导入导出的支持方式 | FR-FORM-011 ~ 013 | 待讨论 |
替换: | 1 | 本地模式下导入导出的支持方式 | FR-FORM-011 ~ 013 | 已确定: 支持。客户端 NPOI 本地处理，不依赖 API |
```

**Step 4: 修改待讨论项表格 #2**

位置: 第 236 行
```
原文: | 2 | 验方复制到处方时的价格计算规则 | FR-FORM-008 | 待讨论 |
替换: | 2 | 验方复制到处方时的价格计算规则 | FR-FORM-008 | 已确定: 根据 HerbId 查药材库当前价格。FormulaHerbItem 不含价格字段，价格始终以药材库为准 |
```

**Step 5: 验证**

运行: `grep -c "待讨论" docs/02-requirements/06-formulas.md`
Expected: 0

**Step 6: Commit**

```bash
git add docs/02-requirements/06-formulas.md
git commit -m "docs(requirements): formulas.md 回填导入导出+价格计算决策 (D-1, F-2)"
```

---

## Task 6: 更新 docs/02-requirements/07-medical-cases.md

**Files:**
- Modify: `docs/02-requirements/07-medical-cases.md` (5 处标记)

**Design Reference:** 设计文档主题 E-1 (审计日志), E-2 (编号冲突), E-3 (搜索性能)

**Step 1: 修改 FR-MC-012 本地模式 (审计日志)**

位置: 第 191 行
```
原文: - **本地模式**: 待讨论
替换: - **本地模式**: 不支持完整审计日志。仅保留实体级审计字段 (CreatedAt/UpdatedAt/CreatedBy/UpdatedBy)。EntityAuditController 仅远程模式可用
```

**Step 2: 修改待讨论项表格 #1**

位置: 第 391 行
```
原文: | 1 | 本地模式下审计日志的存储和同步策略 | FR-MC-012 | 待讨论 |
替换: | 1 | 本地模式下审计日志的存储和同步策略 | FR-MC-012 | 已确定: 仅实体级审计字段。本地模式为单用户操作，字段级变更审计价值有限 |
```

**Step 3: 修改待讨论项表格 #2**

位置: 第 392 行
```
原文: | 2 | 本地模式下医案编号的生成规则 (避免冲突) | FR-MC-001 | 待讨论 |
替换: | 2 | 本地模式下医案编号的生成规则 | FR-MC-001 | 已确定: MC+yyyyMMdd+3位序号。CaseNumber 为展示用编号 (非唯一约束)，Guid Id 为实际唯一标识。同日本地/远程可能重号，不影响数据完整性 |
```

**Step 4: 修改待讨论项表格 #3**

位置: 第 393 行
```
原文: | 3 | 本地模式下跨医案搜索的性能 | FR-MC-010 | 待讨论 |
替换: | 3 | 本地模式下跨医案搜索的性能 | FR-MC-010 | 已确定: 满足需求。诊所场景 (百~千级) SQLite 性能良好，已应用 AsNoTracking + 分页优化 |
```

**Step 5: 验证**

运行: `grep -c "待讨论" docs/02-requirements/07-medical-cases.md`
Expected: 0

**Step 6: Commit**

```bash
git add docs/02-requirements/07-medical-cases.md
git commit -m "docs(requirements): medical-cases.md 回填审计+编号+搜索决策 (E-1~E-3)"
```

---

## Task 7: 更新 docs/02-requirements/10-sync.md

**Files:**
- Modify: `docs/02-requirements/10-sync.md` (5 处标记)

**Design Reference:** 设计文档主题 C-1 (冲突解决), C-2 (MedicalCase同步), C-4 (自动提示), C-5 (功能受限)

**Step 1: 修改待讨论项表格 #1**

位置: 第 158 行
```
原文: | 1 | 冲突解决策略的自动化程度 (按时间戳自动选择?) | FR-SYNC-003, 007 | 待讨论 |
替换: | 1 | 冲突解决策略 | FR-SYNC-003, 007 | 已确定: 手动逐条选择 (保留本地 / 使用服务端 / 跳过)。医疗数据需人工确认，不适合自动覆盖 |
```

**Step 2: 修改待讨论项表格 #2**

位置: 第 159 行
```
原文: | 2 | 本地模式功能受限范围 (哪些功能在本地不可用?) | 全部 FR-SYNC | 待讨论 |
替换: | 2 | 本地模式功能受限范围 | 全部 FR-SYNC | 已确定: 同步需网络连接。不可用项: 自动登录 / Token刷新 / 审计日志查询 / MedicalCase同步 / User同步。详见 dual-mode.md |
```

**Step 3: 修改待讨论项表格 #3**

位置: 第 160 行
```
原文: | 3 | MedicalCase 是否需要加入同步范围 | FR-SYNC-001 | 待讨论 |
替换: | 3 | MedicalCase 同步 | FR-SYNC-001 | 已确定: v1.0 不支持 (聚合根复杂度高，需多表级联 + 聚合完整性)。v2.0 规划 |
```

**Step 4: 修改待讨论项表格 #4**

位置: 第 161 行
```
原文: | 4 | 网络恢复时是否自动提示同步 | FR-SYNC-007 | 待讨论 |
替换: | 4 | 自动同步提示 | FR-SYNC-007 | 已确定: v1.0 不实现。用户手动进入同步模块触发。v2.0 考虑 NetworkStatusService + 状态栏指示器 |
```

**Step 5: 验证**

运行: `grep -c "待讨论" docs/02-requirements/10-sync.md`
Expected: 0

**Step 6: Commit**

```bash
git add docs/02-requirements/10-sync.md
git commit -m "docs(requirements): sync.md 回填冲突解决+同步范围决策 (C-1~C-5)"
```

---

## Task 8: 更新 docs/02-requirements/09-printing.md

**Files:**
- Modify: `docs/02-requirements/09-printing.md` (4 处标记)

**Design Reference:** 设计文档主题 F-3 (PDF导出), F-4 (模板配置), F-5 (批量打印)

**Step 1: 修改待讨论项表格 #1**

位置: 第 154 行
```
原文: | 1 | PDF 导出功能的优先级和实现方案 | FR-PRINT-002 | 待讨论 |
替换: | 1 | PDF 导出功能 | FR-PRINT-002 | 已确定: v1.0 不支持，使用 XPS 格式导出。v2.0 评估 PdfSharp 或 XPS->PDF 转换方案 |
```

**Step 2: 修改待讨论项表格 #2**

位置: 第 155 行
```
原文: | 2 | 打印模板的自定义配置 (诊所信息来源) | FR-PRINT-001 | 待讨论 |
替换: | 2 | 打印模板配置 (诊所信息来源) | FR-PRINT-001 | 已确定: v1.0 硬编码 (ClinicName="中医门诊", Department="中医科")。v2.0 改为 appsettings.json 或数据库配置 |
```

**Step 3: 修改待讨论项表格 #3**

位置: 第 156 行
```
原文: | 3 | 批量打印的场景需求 (多个处方连续打印) | FR-PRINT-001 | 待讨论 |
替换: | 3 | 批量打印 | FR-PRINT-001 | 已确定: 已实现。BatchPrintAsync 支持多处方连续打印，默认静默模式 (ShowDialog=false)，返回成功计数 |
```

**Step 4: 验证**

运行: `grep -c "待讨论" docs/02-requirements/09-printing.md`
Expected: 0

**Step 5: Commit**

```bash
git add docs/02-requirements/09-printing.md
git commit -m "docs(requirements): printing.md 回填PDF+模板+批量打印决策 (F-3~F-5)"
```

---

## Task 9: 更新 docs/02-requirements/README.md

**Files:**
- Modify: `docs/02-requirements/README.md` (2 处标记)

**Design Reference:** 汇总更新

**Step 1: 修改模板说明中的"待讨论"标注**

位置: 第 48 行
```
原文: | **待讨论** | 本地模式下的行为尚未确定，需进一步讨论 |
替换: | **已确定** | 本地模式下的行为已基于代码事实确定 (详见各模块文档) |
```

**Step 2: 修改文档模板结构说明**

位置: 第 62 行
```
原文: ## 待讨论项
替换: ## 决策记录
```

**Step 3: 验证**

运行: `grep -c "待讨论" docs/02-requirements/README.md`
Expected: 0

**Step 4: Commit**

```bash
git add docs/02-requirements/README.md
git commit -m "docs(requirements): README.md 更新标注说明 (待讨论→已确定)"
```

---

## Task 10: 更新 docs/03-architecture/05-dual-mode.md

**Files:**
- Modify: `docs/03-architecture/05-dual-mode.md` (6 处标记)

**Design Reference:** 设计文档主题 A-1 (功能矩阵), C-1 (冲突解决), C-2 (MedicalCase同步), C-3 (User同步), C-5 (功能受限)

**Step 1: 修改同步实体表中 MedicalCase 行**

位置: 第 197 行
```
原文: | MedicalCase | 待扩展 |
替换: | MedicalCase | v1.0 不支持 (聚合根复杂度高，需多表级联)。v2.0 规划 |
```

**Step 2: 修改同步实体表中 User 行**

位置: 第 198 行
```
原文: | User | 待扩展 |
替换: | User | v1.0 不支持 (低频变更 + 密码安全)。缓解: 初始化时下载，人员变更后重新初始化 |
```

**Step 3: 修改待讨论项表格 TBD-01**

位置: 第 226 行
```
原文: | TBD-01 | 本地模式功能受限范围 | 哪些功能在本地模式不可用 | 待讨论 |
替换: | TBD-01 | 本地模式功能受限范围 | 已确定 | 已确定: 不可用项: 自动登录 / Token刷新 / 审计日志查询 / MedicalCase同步 / User同步 / 服务端API导入导出 |
```

**Step 4: 修改待讨论项表格 TBD-02**

位置: 第 227 行
```
原文: | TBD-02 | 数据同步冲突解决策略 | 自动 vs 手动，优先级规则 | 待讨论 |
替换: | TBD-02 | 数据同步冲突解决策略 | 已确定 | 已确定: 手动逐条选择 (保留本地 / 使用服务端 / 跳过)。SyncConflictDialog 已实现 |
```

**Step 5: 修改待讨论项表格 TBD-03**

位置: 第 228 行
```
原文: | TBD-03 | MedicalCase 同步支持 | 聚合根同步复杂度高 | 待扩展 |
替换: | TBD-03 | MedicalCase 同步支持 | v2.0 规划 | v2.0 规划: 需设计聚合根级 Checksum + 级联冲突解决方案 |
```

**Step 6: 验证**

运行: `grep -c "待讨论\|TBD\|待扩展" docs/03-architecture/05-dual-mode.md`
Expected: 0

> **注意**: TBD-01/02/03 是表格中的编号标识符，不是"待讨论"标记。验证时需确认没有 status 列中的 "待讨论" 或 "待扩展" 字样。

**Step 7: Commit**

```bash
git add docs/03-architecture/05-dual-mode.md
git commit -m "docs(architecture): dual-mode.md 回填功能矩阵+同步决策 (A-1, C-1~C-5)"
```

---

## Task 11: 更新 docs/03-architecture/decisions/0002-dual-mode-architecture.md

**Files:**
- Modify: `docs/03-architecture/decisions/0002-dual-mode-architecture.md` (1 处标记)

**Design Reference:** 综合决策

**Step 1: 读取文件，找到"待讨论"标记**

位置: 第 31 行附近
```
原文: ## 待讨论
替换: ## 已确定的决策
```

**Step 2: 补充决策内容**

在该章节下补充:
```markdown
以下事项已基于代码逆向分析确定:

1. **本地模式功能矩阵**: 全模块完整支持 (6 个 LocalDataSource 100% 方法覆盖)
2. **同步冲突解决**: 手动逐条选择 (SyncConflictDialog)
3. **MedicalCase 同步**: v1.0 不支持，v2.0 规划
4. **User 同步**: v1.0 不支持，初始化时下载
5. **SQLite 加密**: v1.0 不加密，依赖 OS 权限

详见 `docs/plans/2026-02-10-requirements-deepening-design.md`
```

**Step 3: 验证**

运行: `grep -c "待讨论" docs/03-architecture/decisions/0002-dual-mode-architecture.md`
Expected: 0

**Step 4: Commit**

```bash
git add docs/03-architecture/decisions/0002-dual-mode-architecture.md
git commit -m "docs(architecture): ADR-0002 待讨论→已确定决策"
```

---

## Task 12: 最终全量验证

**Step 1: 全量验证无残留标记**

运行:
```bash
grep -rn "待讨论" docs/01-product/ docs/02-requirements/ docs/03-architecture/ docs/04-api-reference/ docs/05-development/ docs/06-operations/
```
Expected: 0 匹配 (docs/plans/ 中的设计文档除外)

**Step 2: 统计回填结果**

运行:
```bash
grep -rc "已确定" docs/02-requirements/ docs/03-architecture/
```
Expected: 全部"待讨论"已替换为"已确定"

**Step 3: 更新 planning-with-files 三文件**

更新 `task_plan.md`:
- Goal: 需求文档深化
- 标记所有 Phase 为 complete

更新 `findings.md`:
- 添加回填完成的统计

更新 `progress.md`:
- 添加本次会话的执行日志
- Final Summary 表格

**Step 4: 最终 Commit**

```bash
git add task_plan.md findings.md progress.md
git commit -m "docs: 需求文档深化完成 - 57处待讨论标记全部回填"
```

---

## Task 依赖关系

```
Task 1-8 (8个需求/架构文件回填) ─── 全部可并行 ───┐
                                                    ▼
Task 9 (README.md 汇总更新) ─── 依赖 Task 1-8 ────┐
                                                    ▼
Task 10 (dual-mode.md) ─── 可与 Task 1-8 并行 ────┐
                                                    ▼
Task 11 (ADR-0002) ─── 依赖 Task 10 ──────────────┐
                                                    ▼
Task 12 (全量验证) ─── 依赖全部 Task 1-11 ─────────┘
```

**最大并行度**: Task 1-8 + Task 10 可同时执行 (9 个 Task 并行)

---

## 风险与注意事项

| 风险 | 缓解措施 |
|------|----------|
| 行号偏移 (文件被前序编辑修改) | 按文件内容匹配替换，不硬编码行号 |
| 表格格式破坏 | 替换后检查 Markdown 表格对齐 |
| 遗漏标记 | Task 12 全量验证兜底 |

---

**Created**: 2026-02-10 12:40
**Design Reference**: `docs/plans/2026-02-10-requirements-deepening-design.md`
**Total Tasks**: 12 (11 回填 + 1 验证)
**Total Markers**: 57 处
**Estimated Parallel Batches**: 3 (Batch 1: Task 1-8+10 并行 | Batch 2: Task 9+11 | Batch 3: Task 12 验证)
