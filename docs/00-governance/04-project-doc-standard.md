# Project 文档规范（README.md + AGENTS.md 双文档标准）

> **确立**: 2026-08-10 ｜ **原则**: 参考业界 2026 标准（AGENTS.md 60,000+ 仓库采用，Next.js 16.3 first-party），多学习少猜测
> **定位**: 文档管理维护是 coder 主要职责之一（高要求级别），本规范是 project 级文档的唯一权威

## 一、双文档职责分工（业界标准）

| 文件 | 给谁 | 内容 | 语言 |
|------|------|------|------|
| **README.md** | 人类开发者 | 项目定位 + 结构图 + 类设计用途 + 快速上手 | 中文 |
| **AGENTS.md** | AI agent（Mimo/omp/Hermes） | Structure + WHERE TO LOOK + CONVENTIONS + ANTI-PATTERNS | 英文 |

**原则**：两个文件**职责不重叠**——README 讲「这是什么、怎么用」，AGENTS 讲「在哪找、怎么改、别碰什么」。同一 project 必须双文档齐全，禁止只留一个（除非容器目录豁免）。

## 二、README.md 模板（人类版）

```markdown
# {ProjectName}

> {一句话定位} | {职责范围} | {关键能力}

## 项目定位

- **层级**: {src/... 位置} — {层角色}
- **职责**: {本 project 做什么，边界是什么}
- **依赖**: {依赖哪些模块/项目，为什么}

## 目录结构

```
{ProjectName}/
├── {子目录}/          # {职责一句话}
├── {子目录}/          # {职责一句话}
└── {Module}.cs        # {入口文件职责}
```

## 类设计用途

| 类 | 文件 | 用途 |
|----|------|------|
| {ClassName} | {路径} | {用途 + 关键设计点} |
| ... | ... | ... |

## 快速上手

{如何构建/测试/使用本 project 的关键命令}
```

## 三、AGENTS.md 模板（agent 版）

```markdown
# {ProjectName} - {英文一句话 Purpose}

**Purpose**: {英文职责描述}

## Structure

```
{ProjectName}/
├── {Dir}/            # {英文职责}
└── {Module}.cs       # {入口}
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| {任务} | {路径} | {说明} |

## CONVENTIONS

- **{约定名}** — {约定内容}

## ANTI-PATTERNS

- **{禁止模式}** — {为什么 + 替代做法}
```

## 四、容器目录规则

- **容器目录**（src/、src/Client/、src/Server/、Modules/ 等非 project 目录）：只需要 **AGENTS.md**（给 agent 的导航），不需要 README.md（无独立交付物）
- **子内容目录**（如 Shared.Models/Contracts/Auth）：保留 README.md 合理（该目录有独立契约内容），不强制 AGENTS

## 五、维护纪律

1. **新 project 创建时**：必须同时建 README.md + AGENTS.md（按本模板）
2. **重构/合并批次验收时**：横截面同步清单包含受影响 project 的双文档（README 的结构图/类用途、AGENTS 的 Structure/约定）
3. **文档与代码冲突**：先更新文档再改代码（以文档为准规则）
4. **根目录 AGENTS.md**：保持精炼有序（快速入口 + 强制规则 + 命令速查），不放细节
