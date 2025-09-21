# PRD 命名标准（更新）

为避免 PRD 文档名过长，统一采用“端名称优先”的简短命名：

- 允许端名称：`server`（后端 Web API）、`desktop`（WPF 客户端）、`shared`（共享模型/工具）。
- 文件名模式：`PRD-<端>[-<主题>]-<YYYYMMDD>.md`（`<主题>`可选，建议 1 个短词）。
- 总结文件：`PRD-<端>[-<主题>]-<YYYYMMDD>-SUMMARY.md`。
- 文件名长度建议：不超过 32 个字符。

示例
- `PRD-server-20250921.md`
- `PRD-desktop-quickfix-20250921.md`
- `PRD-shared-enums-20250921.md`

Notes (EN)
- Prefer endpoint-only names; add a short topic only when necessary.
- Keep names short and scannable for Git diffs and directory listings.
