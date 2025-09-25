# 文档首页（凌隐宝堂中医诊所）

本页作为项目文档的统一导航。请首先阅读“指导思想”，其余分册按主题查阅。根目录 `README.md` 补充仓库说明，`docs/` 负责知识体系与操作指南。

## 工程要点
- 目标平台：.NET 8；SDK 由 `global.json` 固定（当前 9.0.305）。
- 产物输出：统一 `BIN/`（见 `Directory.Build.props`）。
- 包版本：集中在 `Directory.Packages.props`。
- API 路由：`/api/v1/*`（见 API Versioning 标注）。
- JSON 序列化：System.Text.Json（Refit 使用 `SystemTextJsonContentSerializer`）。

## 导航速查
- 指导思想：`docs/overview/guiding-philosophy.md`
- 架构总览：`docs/architecture/overview.md`
- 架构决策索引：`docs/architecture/README.md`
- 开发规范与专题：`docs/development/`
- 需求/PRD：`docs/prd/`、`docs/prds-summary/`
- API 文档：`docs/api/README.md`
- 测试与覆盖：`docs/testing/README.md`
- 运行手册：`docs/runbook.md`
- 代码风格：`docs/styleguide.md`
- 任务索引：`docs/tasks/README.md`
- 阶段报告索引：`docs/reports/INDEX.md`
- 文档归档策略：`docs/ARCHIVE.md`

## 目录结构摘要
```
docs/
  ├── index.md                     # 文档首页（本页）
  ├── configuration.md             # 配置与环境
  ├── styleguide.md                # 代码风格
  ├── runbook.md                   # 运行手册
  ├── overview/
  │   └── guiding-philosophy.md    # 项目指导思想
  ├── architecture/
  │   ├── overview.md              # 架构总览
  │   └── README.md                # ADR 与架构文档索引
  ├── development/                 # 开发规范、最佳实践
  ├── api/README.md                # API 说明
  ├── testing/README.md            # 测试策略与覆盖
  ├── modules/                     # 模块说明
  ├── prd/、prds-summary/         # PRD 与交付物
  ├── reports/
  │   ├── INDEX.md                 # 阶段报告索引
  │   └── archive/                 # 历史归档资料
  └── tasks/
      ├── README.md                # 任务管理说明
      ├── pending/                 # 待办任务
      └── completed/               # 完成总结
```

## 任务与报告链接
- 最新任务由 Thinker 发布，详见 `docs/tasks/pending/`，完成后由 Coder 在 `completed/` 归档。
- 每份任务应在正文中补充“相关 PRD/报告”字段，保持与 `docs/reports/`、`docs/prd/` 的双向链接。
- 阶段性分析、架构评估请参阅 `docs/reports/INDEX.md`，历史资料见 `docs/ARCHIVE.md`。

## PRD 与 CCPM
- PRD 文档位于 `docs/prd/`（正文）与 `.claude/prds/`（草稿/素材）。
- 模板：`docs/prd/PRD-TEMPLATE.md`、`.claude/prds/_TEMPLATE.md`。
- 实施需严格遵循 PRD 范围与验收标准，任何偏离需更新 PRD 并记录评审。
- 完成后的 PRD 按模板沉淀至 `docs/prds-summary/`，便于回放。

## 更新约定
1. 新增文档前确定放置目录，并在相关索引（`index.md`、`architecture/README.md`、`reports/INDEX.md` 等）补充链接。
2. 大规模重命名或归档需在 `docs/ARCHIVE.md` 记录。
3. 文档更新默认责任人：Thinker（架构/任务/索引）、Coder（实施总结/最佳实践）、QA（测试）、PM/BA（PRD）。

如需补充文档，请遵循本页目录结构及《文档重构建议计划（2025-09-25）》中的统一命名与链接规范。


