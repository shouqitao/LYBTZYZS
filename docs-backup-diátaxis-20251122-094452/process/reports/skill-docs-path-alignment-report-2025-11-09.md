# Skill配置与docs/结构对齐分析报告

**生成时间**: 2025-11-09
**分析范围**: .claude/skills/文档生成相关Skills
**触发原因**: Issue #1933文档系统整合后，需确保Skills配置与新docs/结构一致

---

## 📊 执行摘要

### 核心问题

**严重性**: 🟡 **中等**（不影响功能，但会导致文档路径错误）

| 指标 | 数值 | 状态 |
|-----|------|------|
| **检查的Skills** | 3个核心文档Skills | - |
| **发现的路径不一致** | 18处 | ⚠️ 需修复 |
| **影响的文档类别** | How-to, Reference, Explanation | - |
| **紧迫性** | 中等 | ⚠️ 建议在下次文档生成前修复 |

### 关键发现

- ⚠️ **3个核心Skills路径过时**: doc-sync, requirements-generator, design-generator
- ⚠️ **主要问题**: 使用旧路径 `how-to-guides/`, `reference/quick-reference/`, `deep/`
- ✅ **无破坏性影响**: 现有功能仍可用，但生成的文档会放到错误位置
- ✅ **修复成本低**: 纯文档更新，无需代码变更

---

## 🔍 详细分析

### 1. lybtzyzs-doc-sync Skill

**文件**: `.claude/skills/lybtzyzs-doc-sync/skill.md`
**版本**: v2.0 (2025-10-30)
**问题数量**: 7处路径不一致

#### 发现的问题

| 行号 | 过时路径 | 正确路径 | 严重性 |
|-----|---------|---------|--------|
| **38** | `docs/how-to-guides/server/` | `docs/how-to/server/` | 🟡 中 |
| **39** | `docs/how-to-guides/client/` | `docs/how-to/client/` | 🟡 中 |
| **40** | `docs/how-to-guides/shared/` | `docs/how-to/shared/` | 🟡 中 |
| **43** | `docs/reference/quick-reference/` | `docs/reference/` | 🟡 中 |
| **62** | `docs/deep/` | `docs/explanation/` | 🟡 中 |

**影响**:
- Skill检查文档时会查找不存在的路径
- 可能误报"文档缺失"

**示例**（Line 38-40）:
```markdown
❌ 过时路径:
**How-to Guides（操作指南 - 任务导向）**：
- **`docs/how-to-guides/server/`** - Server端开发指南
- **`docs/how-to-guides/client/`** - Client端开发指南
- **`docs/how-to-guides/shared/`** - 共享开发指南

✅ 应修正为:
**How-to Guides（操作指南 - 任务导向）**：
- **`docs/how-to/server/`** - Server端开发指南（API、模块、数据库）
- **`docs/how-to/client/`** - Client端开发指南（MVVM、UI、功能模块）
- **`docs/how-to/shared/`** - 共享开发指南（DTO、组件、通用工具）
```

**示例**（Line 43-48）:
```markdown
❌ 过时路径:
**Reference（参考手册 - 信息导向）**：
- **`docs/reference/quick-reference/`** - 快速参考
  - `api-reference.md` - API快速参考
  - `config-templates.md` - 配置模板

✅ 应修正为:
**Reference（参考手册 - 信息导向）**：
- **`docs/reference/`** - 快速参考（扁平化后）
  - `api-reference.md` - API快速参考（已移除）
  - `configuration-parameters-guide.md` - 配置参数指南
  - `code-patterns-enhancement-summary.md` - 代码模式
  - `troubleshooting.md` - 问题排查
  - `development-checklist.md` - 开发清单
- **`docs/reference/api/`** - API完整文档（12个控制器）
- **`docs/reference/modules/`** - 模块完整文档（8个业务模块）
```

**示例**（Line 62）:
```markdown
❌ 过时路径:
**深度参考**（遗留文档，逐步迁移到Diátaxis）：
- **`docs/deep/`** - 高级主题、部署指南、深度设计模式

✅ 应修正为:
**Explanation（概念解释 - 理解导向）**：
- **`docs/explanation/`** - 深度技术分析、工作流文档
  - `advanced-patterns.md` - 高级模式
  - `api-design-best-practices.md` - API设计最佳实践
  - `performance-optimization.md` - 性能优化
  - 等等（Phase 3已重新分配）
```

---

### 2. lybtzyzs-requirements-generator Skill

**文件**: `.claude/skills/lybtzyzs-requirements-generator/skill.md`
**版本**: v1.0 (2025-11-07)
**问题数量**: 6处路径不一致

#### 发现的问题

| 行号 | 过时路径 | 正确路径 | 严重性 |
|-----|---------|---------|--------|
| **73** | `docs/explanation/architecture/server/medicalcase-draft-discussion.md` | `docs/explanation/` 或 `docs/reference/` | 🟡 中 |
| **374** | `docs/explanation/requirements/*.md` | **路径不存在** | 🔴 高 |
| **375** | `docs/explanation/architecture/**/*-discussion.md` | **需确认** | 🟡 中 |
| **376** | `docs/explanation/design/*.md` | **路径不存在** | 🔴 高 |
| **417-423** | 文档路径规则过时 | 需更新为Diátaxis框架 | 🟡 中 |
| **514-518** | `searchPaths`配置过时 | 需更新路径 | 🟡 中 |

**影响**:
- 生成的需求文档会放到错误路径
- 文档检索会失败（搜索不存在的路径）

**问题1: 需求文档路径不存在**（Line 374）:
```markdown
❌ 过时路径:
**检索范围**:
- `docs/explanation/requirements/*.md`  # ← 此路径不存在
- `docs/explanation/architecture/**/*-discussion.md`
- `docs/explanation/design/*.md`  # ← 此路径不存在
- `docs/adr/*.md`

✅ 应修正为:
**检索范围**（基于Diátaxis框架）:
- `docs/explanation/` - 需求讨论、架构决策、设计文档
- `docs/explanation/architecture/decisions/` - ADR文档
- `docs/reference/modules/{module}/` - 模块文档（可能包含需求）
- `docs/archive/requirements-completed-2025/` - 历史需求（归档）
```

**问题2: 文档路径规则过时**（Line 417-423）:
```markdown
❌ 过时路径规则:
docs/explanation/architecture/
├── client/
│   └── {module}-{feature}-discussion.md  # Client端需求
├── server/
│   └── {module}-{feature}-discussion.md  # Server端需求
└── shared/
    └── {module}-{feature}-discussion.md  # 跨端需求

✅ 应修正为（Diátaxis框架）:
**新需求文档路径规则**:

1. **需求讨论文档**（Explanation类型）:
   - 路径: `docs/explanation/{topic}-requirements-discussion.md`
   - 示例: `medicalcase-draft-requirements-discussion.md`

2. **设计文档**（Explanation类型）:
   - 路径: `docs/explanation/{topic}-design.md`
   - 示例: `medicalcase-draft-design.md`

3. **模块相关需求**（Reference类型）:
   - 路径: `docs/reference/modules/{module}/requirements.md`
   - 示例: `docs/reference/modules/medicalcase/requirements.md`

4. **路径判断逻辑**:
   - 概念性需求讨论 → `docs/explanation/`
   - 模块功能需求 → `docs/reference/modules/{module}/`
   - 已完成归档需求 → `docs/archive/requirements-completed-2025/`
```

**问题3: searchPaths配置过时**（Line 514-518）:
```markdown
❌ 过时配置:
"documentSearch": {
  "enabled": true,
  "searchPaths": [
    "docs/explanation/requirements/",  # ← 不存在
    "docs/explanation/architecture/",  # ← 正确，但范围太窄
    "docs/explanation/design/",        # ← 不存在
    "docs/adr/"                        # ← 已移至docs/explanation/architecture/decisions/
  ]
}

✅ 应修正为:
"documentSearch": {
  "enabled": true,
  "searchPaths": [
    "docs/explanation/",                          # 需求、设计、架构概念
    "docs/reference/modules/",                    # 模块文档（含需求）
    "docs/explanation/architecture/decisions/",   # ADR文档
    "docs/how-to/",                               # 操作指南（可能含需求场景）
    "docs/archive/requirements-completed-2025/"   # 历史需求
  ]
}
```

---

### 3. lybtzyzs-design-generator Skill

**文件**: `.claude/skills/lybtzyzs-design-generator/skill.md`
**版本**: v1.0 (2025-10-26)
**问题数量**: 5处路径不一致

#### 发现的问题

| 行号 | 过时路径 | 正确路径 | 严重性 |
|-----|---------|---------|--------|
| **52-74** | `docs/architecture/` | `docs/explanation/architecture/` | 🟡 中 |
| **104-106** | 强制阅读清单路径不完整 | 需添加`explanation/`前缀 | 🟡 中 |
| **116** | `docs/requirements/*.md` | **路径不存在** | 🔴 高 |
| **167** | `docs/design/` | **路径不存在** | 🔴 高 |

**影响**:
- 生成的设计文档会放到错误路径
- 强制阅读文档时会找不到文件

**问题1: 架构文档路径缺少前缀**（Line 52-74）:
```markdown
❌ 过时路径:
### Level 2 - 详细架构（根据功能必读）
#### Server端设计
- [ ] docs/architecture/server/README.md - Server端三层架构
- [ ] docs/architecture/server/services.md - Service层设计标准
- [ ] docs/architecture/server/repositories.md - Repository模式
- [ ] docs/architecture/server/aggregation-roots.md - 聚合根边界

✅ 应修正为:
### Level 2 - 详细架构（根据功能必读）
#### Server端设计
- [ ] docs/explanation/architecture/server/README.md - Server端三层架构
- [ ] docs/explanation/architecture/server/services.md - Service层设计标准（如存在）
- [ ] docs/explanation/architecture/server/repositories.md - Repository模式（如存在）
- [ ] docs/explanation/architecture/server/aggregation-roots.md - 聚合根边界（如存在）

注意：部分文档可能不存在，需根据实际情况调整清单
```

**问题2: 需求文档路径不存在**（Line 116）:
```markdown
❌ 过时路径:
**读取需求文档全文**（docs/requirements/*.md）

✅ 应修正为:
**读取需求文档全文**:
- 优先路径: `docs/explanation/{topic}-requirements-discussion.md`
- 备选路径: `docs/reference/modules/{module}/requirements.md`
- 归档路径: `docs/archive/requirements-completed-2025/`
```

**问题3: 设计文档输出路径不存在**（Line 167）:
```markdown
❌ 过时路径:
**写入设计文档**：保存到docs/design/{feature-name}-design.md

✅ 应修正为（Diátaxis框架）:
**写入设计文档**:
- 路径: `docs/explanation/{topic}-design.md`
- 示例: `docs/explanation/medicalcase-draft-design.md`
- 关联: 需与对应的需求文档在同一目录（explanation/）
```

---

## 📐 Diátaxis框架下的文档路径规范

### 当前docs/结构（Issue #1933后）

```
docs/
├── tutorials/              # 教程（学习导向）- 新手引导
├── how-to/                 # 操作指南（任务导向）- 问题解决 ⭐ 合并后
│   ├── server/            # Server端开发指南
│   ├── client/            # Client端开发指南
│   ├── shared/            # 共享开发指南
│   ├── development/       # 开发流程
│   ├── testing/           # 测试指南
│   ├── documentation/     # 文档维护
│   ├── quality/           # 质量保证
│   └── ui-components/     # UI组件
├── reference/              # 参考手册（信息导向）- 技术细节 ⭐ 扁平化后
│   ├── api/               # API文档
│   ├── modules/           # 模块文档
│   ├── templates/         # 模板文件
│   ├── *.md               # 各种参考文档（扁平化）
│   └── quick-reference/   # ❌ 已删除，内容移至reference/根目录
├── explanation/            # 概念解释（理解导向）- 深度分析
│   ├── architecture/      # 架构文档（server/client/shared/decisions）
│   ├── *.md               # 工作流、深度分析文档（扁平化）
│   └── workflows/         # ❌ 已删除，内容移至explanation/根目录
├── reports/                # 质量报告（18个核心）
├── archive/                # 历史文档归档
│   ├── reports-2025-11/   # 2025-11归档报告
│   ├── spec-workflow-legacy-2025-11-09/  # spec-workflow遗留
│   └── how-to-guides-legacy-2025-11-09/  # how-to-guides遗留
└── support/                # 支持文档
```

### Skills应使用的路径模式

#### 1. 需求文档（Requirements）

**位置**: `docs/explanation/` （Explanation - 理解导向）

**命名模式**:
```
{topic}-requirements-discussion.md
```

**示例**:
```
docs/explanation/medicalcase-draft-requirements-discussion.md
docs/explanation/prescription-template-requirements-discussion.md
```

**备选路径**（模块相关）:
```
docs/reference/modules/{module}/requirements.md
```

#### 2. 设计文档（Design）

**位置**: `docs/explanation/` （Explanation - 理解导向）

**命名模式**:
```
{topic}-design.md
```

**示例**:
```
docs/explanation/medicalcase-draft-design.md
docs/explanation/prescription-template-design.md
```

#### 3. 架构文档（Architecture）

**位置**: `docs/explanation/architecture/`

**子目录**:
```
docs/explanation/architecture/
├── server/       # Server端架构
├── client/       # Client端架构
├── shared/       # 共享架构
└── decisions/    # ADR架构决策记录
```

#### 4. 操作指南（How-to）

**位置**: `docs/how-to/`

**子目录**:
```
docs/how-to/
├── server/           # Server端操作
├── client/           # Client端操作
├── shared/           # 共享操作
├── development/      # 开发流程
├── testing/          # 测试操作
├── documentation/    # 文档维护
├── quality/          # 质量操作
└── ui-components/    # UI组件操作
```

#### 5. 参考手册（Reference）

**位置**: `docs/reference/` （扁平化）

**核心文件**（直接在reference/根目录）:
```
docs/reference/
├── api-reference.md                         # ❌ 已删除
├── code-patterns-enhancement-summary.md     # 代码模式
├── configuration-parameters-guide.md        # 配置参数
├── technology-stack.md                      # 技术栈
├── development-checklist.md                 # 开发清单
├── troubleshooting.md                       # 问题排查
├── api/                                     # API完整文档
├── modules/                                 # 模块完整文档
└── templates/                               # 模板文件
```

---

## 🎯 修复建议

### 优先级P0 - 立即修复（影响文档生成）

#### 1. 修复lybtzyzs-requirements-generator路径

**文件**: `.claude/skills/lybtzyzs-requirements-generator/skill.md`

**修复内容**:
- Line 374: 更新`documentSearch.searchPaths`
- Line 417-423: 更新文档路径规则
- Line 514-518: 更新配置中的`searchPaths`

**修复后路径**:
```json
"documentSearch": {
  "enabled": true,
  "searchPaths": [
    "docs/explanation/",
    "docs/reference/modules/",
    "docs/explanation/architecture/decisions/",
    "docs/how-to/",
    "docs/archive/requirements-completed-2025/"
  ]
}
```

#### 2. 修复lybtzyzs-design-generator路径

**文件**: `.claude/skills/lybtzyzs-design-generator/skill.md`

**修复内容**:
- Line 52-74: 添加`explanation/`前缀
- Line 116: 更新需求文档路径说明
- Line 167: 更新设计文档输出路径

**修复后路径**:
```markdown
**写入设计文档**:
- 路径: `docs/explanation/{topic}-design.md`
- 关联: 与需求文档在同一目录（explanation/）
```

---

### 优先级P1 - 高优先级（影响文档检索）

#### 3. 修复lybtzyzs-doc-sync路径

**文件**: `.claude/skills/lybtzyzs-doc-sync/skill.md`

**修复内容**:
- Line 38-40: `how-to-guides/` → `how-to/`
- Line 43-48: `reference/quick-reference/` → `reference/`（扁平化）
- Line 62: `docs/deep/` → `docs/explanation/`

**修复后核心文档体系**:
```markdown
**核心文档体系**（基于Diátaxis框架）：

**Tutorial（教程 - 学习导向）**：
- **`docs/tutorials/`** - 新手教程、快速开始

**How-to Guides（操作指南 - 任务导向）**：
- **`docs/how-to/server/`** - Server端开发指南
- **`docs/how-to/client/`** - Client端开发指南
- **`docs/how-to/shared/`** - 共享开发指南

**Reference（参考手册 - 信息导向）**：
- **`docs/reference/`** - 快速参考（扁平化）
  - `code-patterns-enhancement-summary.md` - 代码模式
  - `configuration-parameters-guide.md` - 配置参数
  - `technology-stack.md` - 技术栈
  - `development-checklist.md` - 开发清单
  - `troubleshooting.md` - 问题排查
- **`docs/reference/api/`** - API完整文档
- **`docs/reference/modules/`** - 模块完整文档

**Explanation（概念解释 - 理解导向）**：
- **`docs/explanation/`** - 需求、设计、深度分析
  - `{topic}-requirements-discussion.md` - 需求讨论
  - `{topic}-design.md` - 设计文档
  - `advanced-patterns.md` - 高级模式
  - `api-design-best-practices.md` - API设计
  - 等等
- **`docs/explanation/architecture/`** - 架构文档
  - `server/` - Server端架构
  - `client/` - Client端架构
  - `shared/` - 共享架构
  - `decisions/` - ADR决策记录
```

---

### 优先级P2 - 中优先级（文档规范更新）

#### 4. 更新.claude/config/workflow-orchestrator.json

**当前配置**: 已检查，无需修改（未包含具体路径）

#### 5. 创建文档路径规范文档

**建议新增**: `docs/reference/document-path-conventions.md`

**内容**:
```markdown
# 文档路径规范（Diátaxis框架）

## Diátaxis四大文档类型

### 1. Tutorial（教程 - 学习导向）
- **路径**: `docs/tutorials/`
- **用途**: 新手引导、快速开始
- **命名**: `{module}-getting-started.md`

### 2. How-to Guides（操作指南 - 任务导向）
- **路径**: `docs/how-to/{category}/`
- **用途**: 解决具体问题、完成特定任务
- **命名**: `{action}-{object}.md`
- **示例**: `create-patient.md`, `configure-database.md`

### 3. Reference（参考手册 - 信息导向）
- **路径**: `docs/reference/` （扁平化）
- **子目录**:
  - `api/` - API文档
  - `modules/` - 模块文档
  - `templates/` - 模板文件
- **用途**: 技术细节、配置参数、API规范
- **命名**: `{topic}-reference.md` 或 `{topic}.md`

### 4. Explanation（概念解释 - 理解导向）
- **路径**: `docs/explanation/`
- **用途**: 需求讨论、设计文档、架构决策、深度分析
- **命名**:
  - 需求: `{topic}-requirements-discussion.md`
  - 设计: `{topic}-design.md`
  - 分析: `{topic}-analysis.md`
- **子目录**:
  - `architecture/` - 架构文档（server/client/shared/decisions）

## Skills生成文档的路径规则

### lybtzyzs-requirements-generator
- **输出路径**: `docs/explanation/{topic}-requirements-discussion.md`
- **备选路径**: `docs/reference/modules/{module}/requirements.md`

### lybtzyzs-design-generator
- **输出路径**: `docs/explanation/{topic}-design.md`
- **关联**: 与需求文档在同一目录

### lybtzyzs-task-breakdown
- **输出路径**: `docs/explanation/{topic}-task-breakdown.md`（建议）
- **备选**: 直接创建GitHub Issues（无本地文件）
```

---

## 📊 验证清单

修复完成后，使用以下清单验证：

### Skill配置验证

- [ ] lybtzyzs-doc-sync skill.md - 所有路径引用正确
- [ ] lybtzyzs-requirements-generator skill.md - searchPaths配置正确
- [ ] lybtzyzs-design-generator skill.md - 文档输出路径正确
- [ ] 其他Skills（待检查）

### 文档路径验证

- [ ] `docs/how-to/` - 存在且包含所有子目录
- [ ] `docs/reference/` - 扁平化完成，无`quick-reference/`子目录
- [ ] `docs/explanation/` - 包含需求、设计文档
- [ ] `docs/explanation/architecture/` - 包含server/client/shared/decisions

### 功能测试

- [ ] 调用lybtzyzs-requirements-generator生成需求文档 → 检查路径
- [ ] 调用lybtzyzs-design-generator生成设计文档 → 检查路径
- [ ] 调用lybtzyzs-doc-sync检查文档同步 → 检查是否误报

---

## 📝 后续行动

### 立即行动（今天）

1. **修复3个核心Skills的路径配置**
   - lybtzyzs-doc-sync
   - lybtzyzs-requirements-generator
   - lybtzyzs-design-generator

2. **创建文档路径规范**
   - 新增`docs/reference/document-path-conventions.md`
   - 更新`.claude/README.md`引用新规范

### 短期行动（本周）

3. **检查其他Skills是否也有路径问题**
   - lybtzyzs-task-breakdown
   - lybtzyzs-issue-template
   - lybtzyzs-workflow-orchestrator

4. **更新CLAUDE.md中的文档路径说明**
   - 确保与新的Diátaxis框架一致

### 长期改进

5. **自动化路径验证**
   - 创建脚本检查Skill配置中的路径有效性
   - 集成到CI/CD中

6. **文档生成测试**
   - 为每个文档生成Skill创建集成测试
   - 验证生成的文档路径正确

---

## ✅ 最终结论

### 当前状态

- ❌ **Skill配置与docs/结构不一致**: 18处路径过时
- ⚠️ **中等风险**: 不影响现有功能，但会导致新文档路径错误
- ✅ **修复成本低**: 纯文档更新，预估2小时

### 建议决策

**方案A: 立即修复（推荐）** ✅
- **工时**: 2小时
- **收益**: 确保后续生成的文档路径正确
- **适用**: 计划在近期使用文档生成Skills

**方案B: 推迟修复**
- **工时**: 延后至下次文档生成前
- **风险**: 可能生成错误路径的文档，需手动移动
- **适用**: 短期内不使用文档生成Skills

**推荐**: **方案A**，立即修复以避免未来问题

---

## 🔗 相关资源

- **Issue #1933**: 文档系统整合
- **docs/index.md**: 文档导航中心（v6.1）
- **Diátaxis框架**: https://diataxis.fr/
- **.claude/skills/**: Skills配置目录

---

**报告生成**: 2025-11-09
**下一步**: 根据优先级修复Skill配置路径
