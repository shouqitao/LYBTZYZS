# UI/UX 设计文档

> 凌隐宝堂中医诊所管理系统 — UI/UX 设计规范与桌面端需求

## 文档清单

| 文档 | 说明 |
|------|------|
| [desktop-design-spec.md](desktop-design-spec.md) | 桌面端 UI 设计规范（布局、组件、交互规则） |
| [desktop-design-tokens.md](desktop-design-tokens.md) | 设计标准 Token（色彩、字体、间距、圆角、按钮/输入框） |
| [desktop-layout-framework.md](desktop-layout-framework.md) | 三栏框架 + 左侧导航 + 中间母版的端规则（SSOT） |
| [desktop-ui-design-guide.md](desktop-ui-design-guide.md) | 设计指南（页面清单与优先级、角色权限矩阵、状态反馈） |
| [desktop-ui-requirements.md](desktop-ui-requirements.md) | 桌面端 UI 需求（角色工作台、表单、导航） |
| [desktop-ui-detailed-design.md](desktop-ui-detailed-design.md) | 详细设计（设计系统、页面规格、数据流、状态机、API 映射） |
| [second-level-design-checklist.md](second-level-design-checklist.md) | 二级界面（对话框/编辑表单）设计清单与执行顺序 |
| [desktop-ux-user-journeys.md](desktop-ux-user-journeys.md) | 四角色用户旅程（登录 → 首页 → 主业务 → 收尾）与核心诊疗链/前台链 |
| [desktop-ux-interaction-spec.md](desktop-ux-interaction-spec.md) | 交互规范（键盘、焦点与 Tab、鼠标、对话框契约、工具栏顺序、文案口径） |
| [desktop-ux-error-handling.md](desktop-ux-error-handling.md) | 错误处理规范（三层兜底、错误分类与恢复、认证失效链路、关联 ID） |
| [desktop-ux-loading-states.md](desktop-ux-loading-states.md) | 加载状态规范（遮罩/进度条矩阵、分页、防重入、超时取消、健康探测、启动管道） |
| [viewmodel-layer-design.md](viewmodel-layer-design.md) | ViewModel 层设计（现状统计口径、CommunityToolkit 最佳实践、渐进式迁移规则与记录） |

> 计数口径（新增 4 份 UX 文档统一声明）：View **30** / Control **33** / Dialog **7** / Root 1（`Shell/App.xaml`）；ViewModel **55**；XAML 合计 **82** = 视图 71 + 资源/模板 11。

## 阅读顺序（职责边界）

| 你要… | 先读 | 再读 |
|-------|------|------|
| 改视觉/主题/布局 | [desktop-design-spec.md](desktop-design-spec.md) + [desktop-design-tokens.md](desktop-design-tokens.md) | [desktop-layout-framework.md](desktop-layout-framework.md) |
| 改页面功能/权限入口 | [desktop-ui-requirements.md](desktop-ui-requirements.md) | [desktop-ui-detailed-design.md](desktop-ui-detailed-design.md)（页面规格/API 映射） |
| 改交互/键盘/对话框 | [desktop-ux-interaction-spec.md](desktop-ux-interaction-spec.md) | design-spec §6/§9 |
| 改错误提示 | [desktop-ux-error-handling.md](desktop-ux-error-handling.md) | — |
| 改加载/防重入 | [desktop-ux-loading-states.md](desktop-ux-loading-states.md) | — |
| 改 ViewModel 分层 | [viewmodel-layer-design.md](viewmodel-layer-design.md) | 架构侧 [03-architecture/02-desktop.md](../03-architecture/02-desktop.md) |
| 理解角色旅程 | [desktop-ux-user-journeys.md](desktop-ux-user-journeys.md) | — |

> **SSOT 规则**：同一视觉/交互定义只在一处维护；spec 与 ux-* 重叠时以更专的文件为准并在 spec 标注链接，不复制正文。

---

*文档版本: v1.3 | 最后更新: 2026-09-17*
