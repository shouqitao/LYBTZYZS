# 文档质量一致性审查报告（R26-R30）

**审查日期**: 2026-08-20
**审查范围**: `docs/` 目录下全部 151 个 `.md` 文件
**审查维度**: 日期一致性、术语使用、图表/Mermaid、frontmatter 格式、标题层级

---

## R26: 日期一致性

### 发现

**1. 文档头日期 vs Git 提交日期不一致（17/19 个有头文件不一致）**

| 文件 | 头日期 | Git 最后提交 | 差异天数 |
|------|--------|-------------|---------|
| 01-product/01-vision.md | 2026-06-15 | 2026-08-20 | 66天 |
| 01-product/02-personas.md | 2026-08-02 | 2026-08-20 | 18天 |
| 01-product/03-glossary.md | 2026-06-15 | 2026-08-20 | 66天 |
| 01-product/04-permissions.md | 2026-08-03 | 2026-08-20 | 17天 |
| 01-product/05-role-interactions.md | 2026-08-02 | 2026-08-20 | 18天 |
| 02-requirements/02-auth.md | 2026-06-15 | 2026-08-20 | 66天 |
| 02-requirements/09-printing.md | 2026-08-02 | 2026-08-20 | 18天 |
| 02-requirements/11c-error-handling.md | 2026-06-28 | 2026-08-20 | 53天 |
| 02-requirements/11d-observability.md | 2026-06-28 | 2026-08-20 | 53天 |
| 02-requirements/11e-cardreader.md | 2026-08-08 | 2026-08-20 | 12天 |
| 02-requirements/12-nfr.md | 2026-06-15 | 2026-08-20 | 66天 |
| 02-requirements/archive/14-feature-inventory.md | 2026-08-02 | 2026-08-06 | 4天 |
| 02-requirements/README.md | 2026-08-02 | 2026-08-06 | 4天 |
| 03-architecture/16-desktop-architecture-spec.md | 2026-08-09 | 2026-08-20 | 11天 |
| 00-governance/01-naming-convention.md | 2026-08-02 | 2026-08-11 | 9天 |
| 00-governance/03-technical-adoption-governance.md | 2026-08-08 | 2026-08-20 | 12天 |
| compose/specs/desktop-view-inventory.md | 2026-08-16 | 2026-08-19 | 3天 |

**仅 2 个文件头日期与 Git 提交一致**：
- `02-requirements/01-prd.md` (2026-08-20)
- `02-requirements/13-traceability-matrix.md` (2026-08-20)

**2. 无未来日期** — 所有文档日期均 ≤ 2026-08-20（当天），✅

**3. 日期分布** — 文档中引用 52 个不同日期，集中在 2026-06-25/28/08-03/13（高频决策日）

### 问题清单

| 级别 | 问题 | 影响 |
|------|------|------|
| 🟠 | 17 个文档头日期未随内容更新而刷新 | 读者无法判断文档最新修改时间 |
| 🟡 | 部分文档变更日志中有新日期但 header 未同步 | header 与变更日志脱节 |

---

## R27: 术语使用一致性

### 发现

**1. 核心术语中英文混用比例（全库统计）**

| 术语组 | 中文次数 | 英文次数 | 中文占比 | 评估 |
|--------|---------|---------|---------|------|
| 医案/MedicalCase | 628 | 657 | 49% | ⚠️ 混用 |
| 药材/Herb | 560 | 383+162 | 51% | ⚠️ 混用 |
| 验方/Formula | 365 | 296+96 | 48% | ⚠️ 混用 |
| 处方/Prescription | 403 | 269+19 | 58% | ⚠️ 混用 |
| 挂号/Registration | 324 | 324+96 | 44% | ⚠️ 混用 |
| 患者/Patient | 574 | 445+173 | 48% | ⚠️ 混用 |
| 用户/User | 629 | 385+216 | 51% | ⚠️ 混用 |
| 权限/Permission | 381 | 15+74 | 81% | ✅ 中文为主 |

> **评估**：这是项目双语特性的正常现象（中文文档 + 英文代码术语），中英文各占约 50%。但需要确认同一文档内是否混用一致。

**2. 非标准术语残留**

| 非标准用法 | 出现次数 | 文件 | 应改为 |
|-----------|---------|------|--------|
| 病案 | 1 | 04-api-reference/06-medical-cases.md | 医案/MedicalCase |
| 中药 | 10 | 8 个文件 | 药材/Herb（上下文确认） |
| 草药 | 2 | compose/archive/09-postman-vs-dotnet-testing.md | 药材/Herb |
| 药方 | 1 | 02-requirements/06-formulas.md | 验方/Formula |
| 方剂 | 7 | 6 个文件 | 验方/Formula（上下文确认） |
| Medical Case（空格） | 3 | 2 个文件 | MedicalCase（无空格） |

**3. 评估**

| 级别 | 问题 | 说明 |
|------|------|------|
| 🟠 | 病案/药方/草药 非标准术语残留 | 虽然少量，但与术语表不一致 |
| 🟡 | 中药/方剂 在不同上下文中可能合理 | 需逐个确认语境（中药=中药材总称 vs 药材=系统管理对象） |
| 🟡 | Medical Case 空格变体 | 与代码实体名 MedicalCase 不一致 |

---

## R28: 图表/Mermaid 一致性

### 发现

**1. Mermaid 图表使用统计**

- 包含 Mermaid 的文件：**16 个**
- Mermaid 代码块总数：**43 个**
- 语法验证：**0 个语法错误** ✅

**Mermaid 分布**：

| 文件 | Mermaid 块数 | 图表类型 |
|------|-------------|---------|
| 01-product/02-personas.md | 10 | flowchart TD |
| 03-architecture/01-system-overview.md | 6 | flowchart TD |
| 03-architecture/10-printing-architecture.md | 6 | flowchart TD |
| 01-product/05-role-interactions.md | 4 | flowchart TD |
| 03-architecture/09-security-architecture.md | 3 | flowchart TD |
| 03-architecture/04-data-model.md | 2 | flowchart |
| 03-architecture/05-dual-mode.md | 2 | flowchart |
| 05-development/05-security-password-management.md | 2 | flowchart |
| 其他 8 个文件 | 各 1 | flowchart |

**2. 图表类型一致性**

- 全部使用 `flowchart TD` 或 `flowchart LR` — **一致** ✅
- 无 `graph TD` 旧语法（已统一为 flowchart）— **一致** ✅
- 使用 subgraph 分组 — **一致** ✅

**3. 问题**

| 级别 | 问题 | 说明 |
|------|------|------|
| ✅ | Mermaid 语法全部正确 | 0 个语法错误 |
| ✅ | 图表类型统一 | 全部使用 flowchart |
| 🟡 | 03-architecture/03-server.md 仅 1 个 Mermaid | 服务器架构图可能不完整 |

---

## R29: frontmatter 格式一致性

### 发现

**1. 元数据格式现状**

项目**不使用 YAML frontmatter**（`---` 包裹），而是使用 **blockquote 元数据头**（`> 版本: ... | 日期: ...`）。

| 格式类型 | 文件数 | 说明 |
|---------|-------|------|
| 有 blockquote 头（V+D+S） | 15 | 版本+日期+状态（主流格式） |
| 有 blockquote 头（V+D+M） | 4 | 版本+日期+维护者 |
| 有 blockquote 头（V+D） | 2 | 版本+日期 |
| 仅有状态字段 | 10 | 03-architecture/modules/* |
| 无任何元数据 | 67 | 大量文档无版本/日期追踪 |

**2. 格式一致性**

- 分隔符：全部使用 `|`（半角管道符）✅
- 字段名：全部使用 `版本:` / `日期:` / `状态:` ✅
- 但存在多种组合模式（V+D+S / V+D+M / V+D / 仅 S）

**3. 问题**

| 级别 | 问题 | 文件数 |
|------|------|-------|
| 🟠 | 67/151 文件无任何版本/日期元数据 | 67 |
| 🟠 | 10 个模块文档仅有状态字段，无版本/日期 | 10 |
| 🟡 | 4 个文件用「维护者」替代「状态」字段 | 4 |
| 🟡 | `docs/03-architecture/13-project-master-plan.md` 用「更新:」替代「日期:」 | 1 |

**缺失元数据的关键文档**（部分）：
- `02-requirements/04-patients.md`
- `02-requirements/05-herbs.md`
- `02-requirements/07-medical-cases.md`
- `02-requirements/08-registration.md`
- `02-requirements/10-reports.md`
- `03-architecture/00-architecture-summary.md` ~ `13c-current-status.md`（除 07/13/16 外全部缺失）
- 全部 `03-architecture/decisions/*.md`（21 个 ADR）
- 全部 `04-api-reference/*.md`（14 个）
- 全部 `05-development/*.md`（除 08 外）
- 全部 `06-operations/*.md`（11 个）

---

## R30: 文档标题格式一致性

### 发现

**1. H1 标题格式**

所有需求/产品文档的 H1 标题采用统一格式：

```
# 中文名 (English Name)
```

示例：
- `# 药材管理 (Herb Management)`
- `# 验方管理 (Formula Management)`
- `# 医案管理 (Medical Case Management)`

**例外**：
- `# 顶层 PRD (Product Requirements Document)` — 格式一致但「顶层」非标准前缀
- `# Shell (平台壳程序)` — 中文在括号内
- `# Configuration (配置管理)` — 英文在前
- `# Error Handling (异常处理)` — 英文在前
- `# Observability (可观测性: 日志与健康诊断)` — 冒号用法不一致
- `# Card Reader (身份证读卡器)` — 英文在前
- `# LYBTZYZS 产品功能清单` — 无英文名

**2. 标题层级一致性**

- **0 个跳级问题** ✅（如 H1→H3）
- **0 个 H1 重复问题** ✅（每文件仅 1 个 H1）

**3. 分隔线使用**

多处使用 `---` 作为视觉分隔（非 frontmatter），这在 Markdown 中会渲染为水平线。出现在：
- `00-governance/02-ssot-architecture.md`（4 处）
- `00-governance/03-technical-adoption-governance.md`（6 处）
- `01-product/01-vision.md`（6 处）
- `01-product/02-personas.md`（4 处）
- 等

**4. 问题**

| 级别 | 问题 | 文件数 |
|------|------|-------|
| 🟡 | H1 标题中英文顺序不一致 | 6 个文件 |
| 🟡 | 部分 H1 无英文名 | 2 个文件 |
| 🟡 | `---` 分隔线在多个文件中混用 | ~10 个文件 |

---

## 综合汇总

### 问题统计

| 维度 | 🔴 严重 | 🟠 中等 | 🟡 轻微 | ✅ 合格 |
|------|--------|--------|--------|--------|
| R26 日期一致性 | 0 | 1 | 1 | 1 |
| R27 术语使用 | 0 | 1 | 2 | 1 |
| R28 图表/Mermaid | 0 | 0 | 1 | 3 |
| R29 frontmatter | 0 | 2 | 2 | 1 |
| R30 标题层级 | 0 | 0 | 2 | 2 |
| **合计** | **0** | **4** | **8** | **8** |

### 优先修复建议

**P1（中等问题 — 影响可维护性）**：
1. **R29**：67 个文档无版本/日期元数据 → 建议统一添加 blockquote 头
2. **R26**：17 个文档头日期未同步 → 更新 header 日期
3. **R27**：病案/药方/草药 非标准术语残留 → 统一为术语表定义
4. **R29**：10 个模块文档仅状态字段 → 补充版本/日期

**P2（轻微问题 — 影响一致性）**：
5. **R27**：中药/方剂 在不同上下文中的使用 → 确认是否需要统一
6. **R30**：H1 标题中英文顺序 → 统一为「中文 (English)」格式
7. **R29**：4 个文件用「维护者」替代「状态」字段 → 统一字段集
8. **R28**：03-server.md 仅 1 个 Mermaid → 评估是否需要补充
9. **R26**：部分文档变更日志中有新日期但 header 未同步
10. **R30**：`---` 分隔线使用需统一规范
11. **R27**：Medical Case 空格变体（2 个文件）
12. **R29**：`13-project-master-plan.md` 用「更新:」替代「日期:」

---

*报告生成：Hermes Agent R26-R30 审查 | 2026-08-20*
