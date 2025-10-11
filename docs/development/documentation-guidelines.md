# 文档编写与维护指南

- **维护人**：Claude Code
- **最后更新**：2025-10-11
- **版本**：v2.0（Phase 3重构）

本文档定义LYBT项目文档的编写规范、质量标准、维护流程和自动化机制，确保知识体系统一、可维护、高质量。

---

## 📌 核心原则

### 1. SSOT原则（Single Source of Truth）

**定义**：每个知识点只在一个权威位置维护，其他地方通过引用而非复制来使用。

**实践**：
- ✅ **一个主题 = 一个权威文档**
- ✅ **其他文档通过链接引用**，而非复制粘贴
- ✅ **定期审查并合并重复内容**
- ❌ **禁止多处维护相同内容**

**示例**：
- 错误❌：在 `standards.md` 和 `coding-spec.md` 中重复编写命名规范
- 正确✅：在 `standards.md` 中统一编写，`coding-spec.md` 通过链接引用

### 2. 文档分层原则

| 层级 | 说明 | 示例 |
|------|------|------|
| **索引层** | 导航和组织文档 | `docs/index.md`, `docs/architecture/README.md` |
| **规范层** | 约束和标准 | `docs/development/standards.md` |
| **指南层** | 实践和教程 | `docs/development/testing-guide.md` |
| **报告层** | 分析和总结 | `docs/reports/` |

### 3. 最小维护原则

- **能合并就合并**：相似主题文档优先合并（如Phase 2: 4→1, 5→2）
- **能引用就引用**：避免重复编写，使用链接引用
- **能自动化就自动化**：索引、检查、统计尽量自动化

---

## 📝 命名规范

### 文件命名

1. **日期型文档**（报告、分析）：`yyyy-mm-dd-主题.md`
   - 示例：`2025-10-11-architecture-refactoring-summary.md`

2. **永久型文档**（规范、指南）：语义化名称
   - 示例：`standards.md`, `testing-guide.md`, `README.md`

3. **索引型文档**：`README.md` 或 `INDEX.md`
   - 目录索引优先使用 `README.md`
   - 列表索引使用 `INDEX.md`

4. **版本标记**（重大更新）：`-v2`, `-v3` 后缀
   - 示例：`coding-spec-v2.md`
   - 在文首注明变更摘要

### 目录命名

| 目录 | 用途 | 命名规则 |
|------|------|---------|
| `docs/development/` | 开发规范、指南 | 小写+连字符 |
| `docs/architecture/` | 架构设计、ADR | 小写+连字符 |
| `docs/reports/` | 分析报告、总结 | 日期前缀 |
| `docs/tasks/` | 任务计划、总结 | 按状态分类 |
| `docs/api/` | API文档 | 按模块分类 |

---

## 📂 目录与索引管理

### 索引更新规则

**新增文档后必须更新**：
1. **主索引**：`docs/index.md`（所有重要文档）
2. **分类索引**：
   - `docs/architecture/README.md`（架构文档）
   - `docs/development/README.md`（开发文档）
   - `docs/reports/INDEX.md`（报告文档）
   - `docs/tasks/README.md`（任务文档）

### 归档规则

**触发条件**：
- 文档被新版本取代
- 超过3个月未更新的分析报告
- 已完成的一次性任务文档

**归档流程**：
1. 移动到 `docs/reports/archive/` 或 `docs/tasks/archive/`
2. 在 `docs/ARCHIVE.md` 记录（归档日期、原因、新版本链接）
3. 更新所有相关索引
4. 检查并移除失效引用

### 临时文件管理

| 文件类型 | 存放位置 | 说明 |
|---------|---------|------|
| 草稿 | `.claude/drafts/` | 未完成的文档 |
| 临时分析 | `.claude/tmp/` | 临时输出 |
| 个人笔记 | 个人分支 | 不提交到主分支 |

❌ **禁止在根目录或 `docs/` 散落临时文件**

---

## ✍️ 内容编写规范

### 语言规范

- **描述语言**：中文（包括注释、说明）
- **代码示例**：英文变量名、注释可中英混合
- **专业术语**：首次出现时中英对照，如"依赖注入（Dependency Injection, DI）"

### 文档结构模板

#### 规范类文档
```markdown
# 标题

- **维护人**：[姓名/工具名]
- **最后更新**：[日期]
- **版本**：[版本号]

## 目标与范围
## 核心规范
## 示例
## 常见问题
## 参考资料
```

#### 报告类文档
```markdown
# 标题

**版本**：[版本]
**创建时间**：[日期]
**状态**：[进行中/已完成]

## 背景与目标
## 现状分析
## 解决方案
## 实施计划
## 验收标准
## 附件/参考
```

#### 任务类文档
```markdown
# 标题

## 任务描述
## 验收标准
## 实施步骤
## 风险与依赖
## 产出物
```

### 链接规范

1. **使用相对路径**：
   ```markdown
   [架构标准](../architecture/server-module-design-standard.md)
   ```

2. **带行号引用**（精确定位）：
   ```markdown
   [WebAPI入口](../../src/Server/Services/LYBT.WebAPI/Program.cs:39)
   ```

3. **GitHub Issue/PR引用**：
   ```markdown
   参见 #1147, PR #1146
   ```

### 元信息规范

**文首必须包含**：
```markdown
- **维护人**：[负责人]
- **最后更新**：[YYYY-MM-DD]
- **版本**：[v1.0/v2.0等]
```

**可选元信息**：
- 编写人（首次创建者）
- Issue追踪（关联的Issue编号）
- 状态（草稿/审查中/已发布）

---

## 🎯 文档质量标准

### 五维质量模型

| 维度 | 标准 | 检查要点 |
|------|------|---------|
| **准确性** | 内容与代码实现一致 | 代码引用行号正确、API签名匹配、数据准确 |
| **完整性** | 涵盖所有必要信息 | 背景、目标、方案、验收、参考一应俱全 |
| **可读性** | 结构清晰、表达简洁 | 分层合理、段落适中、术语统一、格式规范 |
| **时效性** | 反映最新状态 | 更新日期、版本号、标记过时内容 |
| **可追溯性** | 能找到上下文 | 引用完整、链接有效、Issue/PR关联明确 |

### 质量检查清单

参见：[documentation-quality-checklist.md](documentation-quality-checklist.md)

---

## 🔄 文档合并与重构策略

### 何时合并文档

**合并条件**（满足2项以上）：
- ✅ 内容重复率 >60%
- ✅ 目标受众相同
- ✅ 更新频率一致
- ✅ 逻辑上属于同一主题

**Phase 2经验**：
- 开发规范：4→1（standards.md整合4份规范）
- 测试指南：5→2（testing-guide.md + testing-training-materials.md）
- 架构文档：删除占位+增强索引（保留互补文档）

### 合并流程

1. **创建Issue**：说明合并理由、目标受众、预期结构
2. **内容分析**：
   - 识别共性内容（合并为核心章节）
   - 识别差异内容（作为专题章节）
   - 识别过时内容（删除或归档）
3. **执行合并**：
   - 创建新文档或重写目标文档
   - 删除源文档
   - 更新所有引用
4. **验证**：编译检查、链接检查、索引更新

### 文档拆分策略

**拆分条件**（满足2项以上）：
- ✅ 单文档超过1000行
- ✅ 包含3个以上独立主题
- ✅ 不同受众需要不同深度
- ✅ 更新频率差异大

**拆分原则**：
- 按主题拆分（如架构、安全、性能）
- 按受众拆分（如开发者、运维、测试）
- 按层次拆分（如概览、详细设计、实施指南）

---

## 👥 审阅流程（GitHub集成）

### 文档生命周期

```mermaid
graph LR
    A[创建草稿] --> B[提交PR]
    B --> C[自动检查]
    C --> D{检查通过?}
    D -->|是| E[人工审阅]
    D -->|否| F[修复问题]
    F --> B
    E --> G{审阅通过?}
    G -->|是| H[合并发布]
    G -->|否| F
    H --> I[更新索引]
```

### PR检查清单

**CI自动检查**（必须通过）：
- ✅ Markdown格式检查
- ✅ 链接有效性检查
- ✅ 索引完整性检查
- ✅ 文件编码检查（UTF-8 with BOM）

**人工审阅要点**：
- [ ] 符合SSOT原则（无重复内容）
- [ ] 符合质量五维标准
- [ ] 元信息完整
- [ ] 索引已更新
- [ ] 引用正确

### 角色与职责

| 角色 | 职责 | 关注点 |
|------|------|--------|
| **文档作者** | 编写、提交PR、响应反馈 | 内容准确、结构清晰 |
| **AI审查** | 自动检查格式、链接、索引 | 格式规范、链接有效 |
| **人工审查** | 验证内容质量、逻辑完整性 | 准确性、完整性 |
| **维护者** | 批准合并、管理索引 | 体系一致性 |

---

## 🤖 自动化机制

### CI集成检查

**.github/workflows/doc-check.yml**（建议）：
```yaml
name: 文档质量检查
on: [pull_request]
jobs:
  doc-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Markdown Lint
        uses: DavidAnson/markdownlint-cli2-action@v9
      - name: 链接检查
        uses: gaurav-nelson/github-action-markdown-link-check@v1
      - name: 索引完整性
        run: node scripts/check-index.js
```

### 维护脚本

| 脚本 | 用途 | 位置 |
|------|------|------|
| `scripts/check-dead-links.ps1` | 检测死链 | 定期运行 |
| `scripts/doc-stats.ps1` | 文档统计 | 按需运行 |
| `scripts/sync-index.ps1` | 同步索引 | 新增文档后 |

**使用示例**：
```powershell
# 检测死链
.\scripts\check-dead-links.ps1 -Path docs/

# 文档统计
.\scripts\doc-stats.ps1 -Output docs/reports/doc-stats.csv

# 同步索引
.\scripts\sync-index.ps1 -Scan docs/development/ -Index docs/development/README.md
```

---

## ❓ 常见问题

### Q1: 如何处理命名冲突？
**A**:
1. 检查是否已有同主题文档
2. 若存在，评估是否需要合并
3. 若需独立，在名称中加入子系统或阶段标识
4. 示例：`server-testing-guide.md` vs `desktop-testing-guide.md`

### Q2: 如何避免重复内容？
**A**:
1. 编写前搜索已有文档（`grep -r "关键词" docs/`）
2. 发现重复时，优先引用而非复制
3. 定期运行文档审查（每季度），识别并合并重复内容

### Q3: 老旧文档如何处理？
**A**:
1. 超过3个月未更新的分析报告 → 评估归档
2. 被新版本取代的规范 → 归档并注明新版本链接
3. 一次性任务完成后 → 归档到 `docs/tasks/archive/`

### Q4: 如何保证文档与代码一致？
**A**:
1. 代码变更时同步更新文档（PR检查项）
2. 使用精确的代码引用（文件:行号）
3. 定期审查高频引用的代码位置
4. CI检查代码引用的行号有效性（可选）

### Q5: 新人如何快速了解文档体系？
**A**:
1. 从 `docs/index.md` 开始
2. 阅读本文档（文档规范）
3. 查看 `docs/architecture/README.md`（架构导航）
4. 参考 `docs/development/README.md`（开发指南）

---

## 📚 参考资料

- [文档质量检查清单](documentation-quality-checklist.md)
- [文件组织规范](file-organization-guidelines.md)
- [开发标准](standards.md)
- [架构文档索引](../architecture/README.md)

---

## 📋 变更历史

| 版本 | 日期 | 变更内容 | Issue |
|------|------|---------|-------|
| v1.0 | 2025-09-25 | 初始版本 | - |
| v2.0 | 2025-10-11 | Phase 3重构：增加SSOT原则、质量标准、合并策略、自动化机制 | #1147 |

---

请在新文档中引用本指南，并严格遵守上述规范。如需新增规范，请提交Issue并在审阅通过后更新此文件。

🤖 最后更新：Phase 3 - 文档治理规则建立
