# 文档首页（凌隐宝堂中医诊所）

本页作为项目文档的导航与导读。请首先阅读“指导思想”，其余分册按主题查阅。根目录 `README.md` 为对仓库的补充说明，docs/ 为知识体系与操作指南。

## 工程要点
- 目标平台：.NET 8；SDK 由 `global.json` 固定（当前 9.0.305）。
- 产物输出：统一 `BIN/`（见 `Directory.Build.props`）。
- 包版本：集中在 `Directory.Packages.props`。
- API 路由：`/api/v1/*`（见 API Versioning 标注）。
- JSON 序列化：System.Text.Json（Refit 使用 SystemTextJsonContentSerializer）。

## 文档目录
- 指导思想：overview/guiding-philosophy.md
- 架构总览：architecture/overview.md
- 配置与环境：configuration.md
- API 文档：api/README.md（或 openapi.v1.json）
- 测试与覆盖：testing/README.md
- 代码风格规范：styleguide.md
- 运行手册：runbook.md
- 模块说明：modules/index.md
- 变更日志：../CHANGELOG.md

## docs/ 目录结构（摘要）
```
docs/
  ├── index.md                       # 文档首页（本页）
  ├── configuration.md               # 配置与环境
  ├── styleguide.md                  # 代码风格
  ├── runbook.md                     # 运行手册
  ├── overview/
  │   └── guiding-philosophy.md      # 项目指导思想（新增）
  ├── architecture/
  │   └── overview.md                # 架构总览
  ├── api/
  │   └── README.md                  # API 说明 / OpenAPI 入口
  ├── testing/
  │   └── README.md                  # 测试与覆盖率
  ├── modules/
  │   └── index.md                   # 模块说明
  ├── ccpm/                          # 需求/交付文档
  ├── development/                   # 开发规范/最佳实践/专题
  ├── prds-summary/                  # PRD 汇总与落地记录
  └── reports/                       # 分析与阶段报告
```

## PRD 与 CCPM
- PRD 文档位于 `docs/ccpm/`（正文）与 `.claude/prds/`（草稿/素材汇聚）。
- 模板：`docs/ccpm/PRD-TEMPLATE.md` 与 `.claude/prds/_TEMPLATE.md`（英文）。
- 严格按 PRD 的范围/验收标准实施；任何偏离需先补充分发版 PRD 与评审记录。
- 每个 PRD 完成后按模板沉淀到 `docs/prds-summary/`，保持可回放。

如需补充或新增文档，请先阅读“指导思想”，并按本页目录结构提交到 docs/ 对应分册。
