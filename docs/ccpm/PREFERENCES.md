# CCPM PRD 生成偏好（持久化）

- 默认规范：遵循 https://github.com/automazeio/ccpm 的 PRD 要求与结构。
- 输出位置：
  - 主路径（PRD 文档）：`docs/ccpm/PRD-<slug>-<YYYYMMDD>.md`
  - 总结（完成总结文档）：`docs/prds-summary/PRD-<slug>-<YYYYMMDD>-SUMMARY.md`
  - 镜像：`.claude/prds/<slug>.md`（便于后续与 CCPM 命令集成）
- 模板：`docs/ccpm/PRD-TEMPLATE.md`（中文）与 `.claude/prds/_TEMPLATE.md`（英文）
- 触发约定：
  - 当用户提出“生成 PRD 需求/PRD 文档/PRD”的请求时，按上述模板与路径生成文档。
- 命名约定：
  - `<slug>` 由需求主题的英文短名或拼音短名构成，使用 `-` 分隔。
- 适配说明：
  - PRD 需求文档对外入口：`docs/ccpm`
  - PRD 完成总结与共用产物汇总：`docs/prds-summary`（如 shared-inventory 等）
  - 同时在 `.claude/prds` 下生成镜像，兼容 CCPM 工具链。
