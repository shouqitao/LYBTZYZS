# 版本号标准（Versioning Standard）

> 版本: v1.0 ｜ 生效: 2026-09-24 ｜ 关联: [12-desktop-release.md](../06-operations/12-desktop-release.md) §0、ADR-0021
> 背景: 2026-09-24 曾按 1.0.4 错误发布（未经完整测试与真实验收），撤回后重发 v0.0.1，并立本标准防止版本号被随意定义和修改。

---

## 1. 单一版本源（Single Source of Truth）

- 产品版本的**唯一定义处** = `Directory.Build.props` 的 `<VersionPrefix>`（当前 `0.0.1`）。
- 以下全部**派生**自该值，禁止二次书写：
  - `AssemblyVersion` / `FileVersion` / `InformationalVersion`（构建自动盖章）
  - Velopack 包元数据与 `releases.<channel>.json` 清单
  - 安装器、关于页、日志、诊断页的版本显示
  - 文档与脚本中的版本示例
- 运行时展示一律读程序集元数据（`InformationalVersion`）；**代码、配置、测试中禁止出现产品版本字符串字面量**（历史陈述与实测记录除外）。

## 2. 版本格式与升档门槛（SemVer 2.0，硬门）

- 格式：`MAJOR.MINOR.PATCH[-预发布][+构建元数据]`。
- **当前阶段强制 `0.0.x`**（initial development）。升档门槛缺一不可：

| 目标 | 门槛 | 判定依据 |
|------|------|---------|
| `0.0.x` 内递增 | L0 + L1 全绿 | tests/AGENTS.md 分层实测 |
| `0.1.x` | 24 个 RemoteApi 用例解锁后 **L2 全绿** + **诊所真实环境验收通过** | 测试记录 + 验收记录 |
| `1.0.0` | **155 个 US 全量验收通过、无 Skip 缺口** | 13-traceability-matrix + 验收记录 |

## 3. 递增规则

- `0.0.x` 阶段：每次正式发布 patch +1（0.0.1 → 0.0.2），**禁止跳号**。
- 预发布标记（`0.1.x` 起可用）：`-alpha.n < -beta.n < -rc.n`，仅内部分发渠道。
- **已发布版本号永不复用**：已打 tag / 已建 Release 的号一经发布即冻结；撤回的号作废后也不得再用（v1.0.4 为作废示例）。
- **版本号只在发布时刻修改**：先改 `VersionPrefix`（提交）→ L 层全绿 → 打包 → 打 tag → 发布；开发期间 `VersionPrefix` 保持为最近一次已发布值。

## 4. 发布一致性与机器门禁

- **tag == VersionPrefix**：git tag 名为 `v{VersionPrefix}`，逐字符一致。
- 发布前校验，任一失败即中止发布：
  1. **守卫测试**（Architecture）：`InformationalVersion` 前缀 == `VersionPrefix`；
  2. **pack 脚本校验**：显式 `-Version` 不得低于 `VersionPrefix`；
  3. **发布 checklist**：tag 与 VersionPrefix 一致；`releases.<channel>.json` 内版本 == tag；GitHub Release 资产与本地 `SHA256SUMS.txt` 一致。
- **禁止行为**（任何角色，含 agent）：
  - 手工创建、移动、覆盖已发布 tag 或 Release 资产；
  - 绕过门槛修改 `VersionPrefix`；
  - 在代码/配置/测试/文档硬编码产品版本号；
  - 修改本标准的门槛与规则（见 §6）。

## 5. 版本语义分离（三产品轴 + 文档两轴）

| 版本轴 | 载体 | 规则 |
|--------|------|------|
| 产品版本 | `VersionPrefix` + git tag | 本标准 |
| 数据库 schema | EF 迁移编号 | 只增不改，与产品版本解耦 |
| API 契约 | 路由 `/v1`、`/v2`（ADR-0015 已预留 V2） | 与产品版本解耦，仅破坏性契约变更才升轴 |

文档中另有两条**独立语义轴**（规则细则见 [04-project-doc-standard](04-project-doc-standard.md) §六）：

- **文档修订号**：各文档头行 `版本: vN.M`，只表示该文档自身的修订历史，与产品版本完全无关，互不可推导。
- **路线图标签**：正文中的 `v1.0` / `v2.0` 指**未来目标产品版本**，语义对齐 §2 门槛表。约束：
  - 已交付功能**不得**再挂「vX.Y 待实现 / 规划」，状态必须校正为实际（✅/⚠️），并注明校准依据；
  - 带日期的日志行、总账决策行、审计记录属**史实，一律不改**；
  - 状态标记 🧲 必须携带目标版本档（如 `🧲 v1.0 待实现`），档位必须存在于 §2 门槛表。

## 6. 职责与修订

- **执行**：发布人（或发布代理）按 §3–§4 操作；守卫测试与 pack 校验是机器门禁，任何角色不得绕过、不得放宽。
- **修订**：本标准的任何修订必须经用户（牧川先生）明示确认，agent 不得自行变更门槛、格式或禁止项。
- **关联**：[12-desktop-release.md §0](../06-operations/12-desktop-release.md)（发布流程视角，指向本标准）；Hindsight 版本纠正记录（2026-09-24）。
