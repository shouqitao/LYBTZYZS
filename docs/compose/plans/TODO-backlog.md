---
feature: todo-backlog
status: active
updated: 2026-07-30
scope: 全部待做任务清单（按优先级排序）
---

# LYBTZYZS 待做清单

> 生成时间: 2026-07-30 | 当前状态: Build 0 错误, Server 711/712 pass, Architecture 82/83 pass

---

## P0 — 安全 / 数据正确性

| ID | 任务 | 描述 | 预估 |
|----|------|------|------|
| **T1** | P0 安全 Bug 修复（7 个） | 7 个数据正确性/安全问题，需用户提供具体问题列表 | 待定 |
| **T2** | LocalWebAPI DoctorOrAdminOrReceptionist 策略 | Registration/MedicalCase 端点本地模式 403 已修复（`ce3068465`）。**状态: DONE** | 0 |

## P1 — 功能补全

| ID | 任务 | 描述 | 预估 |
|----|------|------|------|
| **T3** | Herbs/Formulas batch-enable/disable | Server Commands+Handlers+Controllers 已实现（`f299989d9`）。**状态: DONE** | 0 |
| **T4** | 6 个导出/导入模板端点 | IHerbApi/IPatientApi/IFormulaApi 定义了 batch-import/import-template/export 端点，Server 无 NPOI 依赖。需添加 NPOI + 新 service 层 + Excel 生成逻辑。Batch-import 部分已有 CommandHandler。 | **大 (2-3d)** |
| **T5** | Desktop 测试修复（~153 fail） | 全部因 `localhost:5000` 连接拒绝失败。需运行中的 WebAPI 才能测试。非代码问题——需要在 CI 或本地启动 WebAPI 后运行。 | **中 (需环境)** |
| **T6** | US-REG-008 SignalR | T47 未完成，实时通知功能 | **大** |
| **T7** | Shell features | US-SHELL-010/011/013/018（Velopack 自动更新 / 初始化向导 / 备份 / 配置中心）| **大 (多 sprint)** |

## P2 — 架构完善

| ID | 任务 | 描述 | 预估 |
|----|------|------|------|
| **T8** | Controller 共享基类下沉（6 对合并） | Phase 2 T17 deferred — Herbs/Formula/Patients/Users/Registrations/MedicalCases Controller 的重复 CRUD 模式合并到基类。复杂度高。 | **大 (1-2d)** |
| **T9** | Offline-sync 功能延后开发 | 分支 `rebase/offline-sync` 已 rebase 到 master，9 个 commit。待重新评估架构后再决定是否采用。 | **待定** |
| **T10** | 架构测试补全 | 当前 82/83 pass。可增加：Herbs/Formulas 新 batch 端点授权策略测试、MedicalCase 复杂度限制测试 | **小 (0.5d)** |
| **T11** | Documentation update | AGENTS.md/README.md 部分内容过时。IPatientService/IFormulaService 引用经审计实际存在（非过时）。需持续维护。 | **小 (0.5d)** |

## P3 — 运维 / 部署

| ID | 任务 | 描述 | 预估 |
|----|------|------|------|
| **T12** | Desktop 发布包 | 用户说"desktop 我还需要完善"，后续再做 | **待定** |
| **T13** | systemd 服务 | 配置开机自启 | **小 (0.5d)** |
| **T14** | 部署脚本清理 | deploy-fixed.ps1 + fix_both_configs.py + fix_all_now.py 需评估是否仍需要 | **小 (0.25d)** |

## P4 — 代码质量

| ID | 任务 | 描述 | 预估 |
|----|------|------|------|
| **T15** | Package 清理（剩余） | FluentValidation.AspNetCore 已迁移。可检查其他废弃包 | **小** |
| **T16** | code-review-graph 使用 | CRG 已安装（.venv-crg），图谱已构建（8348 nodes/49172 edges）。可用于代码审查、架构分析 | **工具就绪** |

---

## 本次 Session 完成清单

| Commit | 任务 | 日期 |
|--------|------|------|
| `ce5538e69` | LoginVM + MedicalCaseWorkspaceVM CommunityToolkit 迁移 | 2026-07-30 |
| `6c4407565` | MVVM 架构审计修正（51 VM 零 DelegateCommand） | 2026-07-30 |
| `e0df3701b` | HerbListControlViewModel 事件订阅泄漏修复 | 2026-07-30 |
| `7ae5a7ff1` | AGENTS.md migration 版本更新 | 2026-07-30 |
| `ce3068465` | LocalWebAPI 授权策略对齐 Server | 2026-07-30 |
| `8f6d511b8` | Shared.Models 瘦身审计 + stale import 清理 | 2026-07-30 |
| `f299989d9` | Herbs/Formulas batch-enable/disable 全栈实现 | 2026-07-30 |

---

## 快速启动指南（新 Session）

1. **读取本 TODO**: `docs/compose/plans/TODO-backlog.md`
2. **读取 MEMORY**: 项目 MEMORY.md 中的待做列表
3. **运行测试确认基线**: `dotnet test`（Server + Architecture）
4. **检查 code-review-graph**: `.venv-crg\Scripts\python.exe -m code_review_graph status`
5. **选择任务**: 从上面的优先级列表中选一个，按 compose:brainstorm → plan → implement 流程执行
