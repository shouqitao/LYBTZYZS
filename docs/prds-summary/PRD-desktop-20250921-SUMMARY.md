# PRD 完成总结 — desktop-sln-quick-fix — 2025-09-21

- 关联 PRD：.claude/prds/desktop-sln-quick-fix.md

## 实施范围与关键变更（预创建）
- 当前状态：已完成 PRD 严格化与文档对齐；代码修复项待实施
- 计划范围：LYBT.Desktop.sln、src/Client/Desktop/*、src/Shared/*、Directory.Packages.props、Directory.Build.props
- 关键修复（计划）：
  - 构建修复：添加 Microsoft.Extensions.ObjectPool、解决命名冲突、移除 UseWindowsForms、清理 coverlet.collector 重复
  - 一致性：统一 XML 文档输出；移除 Refit.Newtonsoft.Json；路由常量小写；资源包含方式与 WPF 默认一致
  - 清理与文档：移除未用属性；（可选）统一 GUID；更新变更说明

## 验证与测试（完成后补充）
- 还原与构建（Debug/Release）：
- 运行桌面客户端冒烟（首页/资源加载）：
- API 冒烟（/api/v1/health、/api/v1/users）：

## 文档与 README 更新
- 根 README：已收录“PRD 工作流（CCPM）”与统一命令
- docs/index.md：已收录专题入口
- Desktop 门面 README：结构与导航条目已完善
- 本总结将在实施完成后补充更新链路与变更摘要

## 风险与遗留项
- 命名空间更名影响范围较大 → IDE 批量重命名 + 全量编译 + 搜索校验
- JSON 统一兼容差异 → 必要时仅在基础设施层局部保留 Newtonsoft
- 资源包含方式调整 → 运行时验证

## 建议/下一步
- 按里程碑（R1–R3）小步提交并验证；完成后更新本总结
- 在 CI 中加入构建/覆盖率门禁与简单 UI 冒烟验证（可选）

