# 文档 SSOT 架构方案（Single Source of Truth）

> 版本: v1.0 | 日期: 2026-08-04 | 状态: 待产品负责人确认
> 问题背景: 审计发现同一信息点被多处定义——权限 12 处、状态机 10 处、Code Style 10 处、技术栈 9 处、已知问题 6 处、术语 4 处。

---

## 一、核心原则

> **每个信息点只有一个权威定义位置（SSOT）。其他位置只能「引用」，不得「复制定义」。**

判断标准（一句话）：
- 改一个信息点时，**只改一个文件** → 设计正确
- 改一个信息点要同步多个文件 → 设计错误

---

## 二、分层模型

```
┌───────────────────────────────────────────────┐
│ L0 导航层: README.md（每目录一个）              │
│   只列链接和一句话说明，不定义任何信息          │
├───────────────────────────────────────────────┤
│ L1 权威层: 每个信息点的唯一定义文件             │
│   业务规则→需求文档｜数据→数据模型｜API→参考    │
├───────────────────────────────────────────────┤
│ L2 视图层: 速查表/矩阵（13a/13b/权限矩阵）      │
│   权威的压缩视图，头部注明「权威见 XX」         │
│   不得出现权威中没有的新信息                    │
├───────────────────────────────────────────────┤
│ L3 记忆层: Skill（agent 操作规则）              │
│   只放「如何开发」的规则（流程/约束/Pitfalls）  │
│   事实类信息（架构/数据）以 docs 为权威          │
└───────────────────────────────────────────────┘
```

---

## 三、信息点 → 唯一权威映射

| # | 信息点 | 唯一权威（L1） | 现状重复位置（调整动作） |
|---|--------|--------------|------------------------|
| 1 | **术语定义** | `01-product/03-glossary.md` | 02-personas / 07-medical-cases / 13a（→改引用或删） |
| 2 | **权限矩阵（产品规则）** | `01-product/04-permissions.md` | 12 处（模块需求文档删权限表→引用；API 参考权限列标注以 04 为准） |
| 3 | **数据模型（实体/字段/状态枚举）** | `03-architecture/04-data-model.md` | 13a 速查（已加指引 ✅） |
| 4 | **API 端点契约** | `04-api-reference/` | 13b 速查（已加指引 ✅） |
| 5 | **任务清单/状态** | `13-project-master-plan.md` | 14-implementation-tasks（已归档 ✅） |
| 6 | **架构决策** | `03-architecture/decisions/` | — |
| 7 | **状态机（数据转换）** | `03-architecture/04-data-model.md` | 需求文档重复画状态机→只写业务触发规则，转换图引用 04 |
| 8 | **技术栈/架构总览** | `03-architecture/00-architecture-summary.md` | 9 处（AGENTS.md/master plan 保留一行速览→引用；prd/localwebapi/06 删描述） |
| 9 | **Code Style** | `05-development/02-code-standards.md` | localwebapi/03-server 等（→删或引用） |
| 10 | **已知问题** | `03-architecture/13c-current-status.md` | master plan/code-gap-fix-list（→改引用） |
| 11 | **开发规则/流程（agent 层）** | Skill `lybtzys-coder-rules` | AGENTS.md 引用 ✅ 已确立 |
| 12 | **部署/配置** | `06-operations/`（收敛中） | 6 份部署文档（单独批次收敛） |

---

## 四、调整清单（分批执行）

### 批次 A：权限收敛（最大重复源，12 处 → 1 权威）
- [ ] `02-requirements/` 各模块权限表 → 保留「本模块权限需求摘要」并标注「权威见 04-permissions.md」，删除与 04 重复的矩阵
- [ ] `03-architecture/04-data-model.md`、`09-security-architecture.md`、`11-business-flows.md` 中的权限规则 → 改引用
- [ ] `04-api-reference/` 权限列 → 标注「权限以 04-permissions.md 为准」
- [ ] `01-product/02-personas.md`、`03-glossary.md`、`05-role-interactions.md` 权限描述 → 改引用

### 批次 B：状态机收敛（10 处 → 04 数据权威 + 需求业务规则）
- [ ] `07-medical-cases.md` 保留业务触发规则，删除完整状态转换图（引用 04）
- [ ] `08-registration.md`、`13-traceability-matrix.md` 同步
- [ ] `modules/medical-case.md`、`modules/registration.md` 状态机 → 引用 04

### 批次 C：技术栈/Code Style/术语/已知问题收敛
- [ ] 技术栈：prd/localwebapi/06-operations 描述 → 引用 00-summary；AGENTS.md/master plan 保留速览
- [ ] Code Style：localwebapi/03-server 等 → 引用 02-code-standards
- [ ] 术语：02-personas/07-medical-cases/13a 术语行 → 引用 glossary
- [ ] 已知问题：master plan/code-gap-fix-list → 引用 13c

### 批次 D：视图层规范
- [ ] 13a/13b/12-permissions-matrix 头部统一「权威见 XX」格式（13a/13b 已完成 ✅，补 12）

### 批次 E（后续，另行确认）：部署/测试文档收敛
- [ ] 06-operations 6 份部署 → 合并/归档
- [ ] 05-development 6 份测试 → 合并/归档

---

## 五、验证标准

每批次完成后：
1. `grep` 确认该信息点在非权威文件中不再有「定义性内容」（只剩引用）
2. 断链检查：归档/删除前 grep 引用面
3. `git status` 干净 + 提交

---

## 六、风险与边界

- **速查表是合法视图**：13a/13b/权限矩阵保留（高可读性），但必须标注权威源，且不得含权威没有的新信息
- **需求文档的「模块权限摘要」保留**：读模块需求时需要上下文，但必须标注权威（不复制全矩阵）
- **API 参考权限列保留**：API 契约需要标注权限，但以 04 为准（产品规则变更时 API 文档同步标注）
- 不追求 100% 零重复（过度设计），目标是「同一信息点只有一个可编辑的真相」
