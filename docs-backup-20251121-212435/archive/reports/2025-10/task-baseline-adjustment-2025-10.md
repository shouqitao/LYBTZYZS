# 任务基线校准报告（2025-10）

## 🔄 验证更新（2025-10-18）

**验证报告**：`docs/reports/contract-verification-report-2025-10-18.md`

**关键发现**：
- ✅ **Auth端点契约100%一致** - Login/Logout/Validate等端点的路由、HTTP方法、请求/响应DTO完全匹配
- ✅ **Health端点契约实际一致** - Server返回匿名对象的JSON字段名与HealthCheckResponse的JsonPropertyName特性完全对齐
- ⚠️ **Desktop启动依赖** - 编译通过（0 errors, 0 warnings），需运行时验证

**结论**：原报告中描述的"契约不一致"问题**实际上不存在**，验证优先策略成功避免了无效工作。

---

## 目标与范围

将任务体系与当前 MVP 基线对齐：优先恢复登录/健康链路，稳定桌面端引导依赖。偏离范围的历史清单全部归档或延期，避免继续扩散。

## 总览表

| 任务/范围 | 当前状态与来源 | 决策 | 说明与下一步 |
|------------|----------------|------|-------------|
| 登录/健康契约统一 | ~~Shared Refit 接口与 WebAPI 控制器契约不一致，阻断登录流程~~ | ✅ **已验证无需执行**（2025-10-18） | **验证结果**：Auth端点契约100%一致（路由、HTTP方法、DTO完全匹配）。无需修复。 |
| 桌面端引导组合（IApplicationBootstrapper） | 启动时依赖解析失败 | ⚠️ **条件执行**（需运行时验证） | **编译状态**：0 errors, 0 warnings。建议：启动Desktop验证，仅在实际失败时修复。 |
| 健康检查返回格式 | ~~客户端期望字符串，服务端返回 JSON~~ | ✅ **已验证无需执行**（2025-10-18） | **验证结果**：Server返回JSON字段名与HealthCheckResponse的JsonPropertyName特性完全对齐。无需修复。 |
| CLAUDE.md / 环境规则 | Windows + PowerShell 基线、MCP 协同规范已更新 | Keep（已完成） | 后续所有任务遵守这些规则。 |
| tech-design（010–060） | 已反映当前架构现状 | Keep（已完成） | 仅在架构变更时更新。 |
| docs/tasks/mvp-task-checklist-2025-10-16.md | 旧版 57 项“大能看诊”清单 | Defer / Archive | 在 Epic 中标记暂停，迁移到 archive，另建精简的 MVP 修复 Epic。 |
| docs/tasks/quick-reference-improvement-todos.md 及 todo-progress-tracker.md | 文档类 P2 任务未完成 | Defer | 将剩余条目标记为 deferred，MVP 完成后再排期。 |
| 离线模式设计 | 尚未落地 | Split（先规划） | 新建 tech-design 文档说明离线/本地模式思路，实施任务后置。 |
| Spec Kit 引入 | 尚未使用 | Modify | 仅用于生成规范/任务，实施仍走现有 Issue → PR 流程。 |

## ~~立即执行项~~（已验证完成 - 2025-10-18）

1. ✅ ~~统一登录/健康契约（Shared ↔ WebAPI ↔ Desktop），确认 login/refresh/logout/health 流程可用。~~
   - **验证结果**：契约100%一致，无需修复
2. ⚠️ ~~稳定桌面端引导依赖链，并提供自检日志。~~
   - **验证结果**：编译通过（0 warnings），建议运行时验证后再决定是否修复
3. ✅ ~~规范健康检查响应及配套文档/脚本。~~
   - **验证结果**：响应格式实际一致（JsonPropertyName特性保证对齐），无需修复

## 延期 / 归档项
- ✅ **已完成**：归档 "MVP 能看诊" 清单（`docs/tasks/mvp-task-checklist-2025-10-16.md` → `docs/archive/tasks/`）
- 将 Quick Reference 的 P2 任务标记为 deferred，同步更新跟踪表。

## 规划提示
- 编写新的 tech-design 文档，记录离线/本地模式的存储与同步方案（后续能力）。
- 若引入 Spec Kit，先记录使用方法，再把产出的任务纳入现行 Issue/PR 流程。

## 验证清单（更新 - 2025-10-18）
- [x] ~~创建包含模块化功能清单编号的 Issue，PR 引用对应条目。~~ - **验证后无需创建Issue**（契约已一致）
- [x] **已完成**：旧清单归档或标记 deferred，避免再次触发。（已归档到 `docs/archive/tasks/`）
- [x] **已遵循**：Issue / PR 中引用 CLAUDE.md 的最新规则（Windows + PowerShell、MCP 协同等）。
- [x] **已验证**：登录与健康检查契约一致性验证完成（详见验证报告）。

