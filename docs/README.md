# 文档中心

> 凌隐宝堂中医诊所管理系统 — 所有文档的入口

---

## 🤖 AI 查询指南（Agent 必读）

> **给 AI（Mimo Code / 编码代理 / 未来 session）的文档导航。** 任务开始前先查本表定位权威文档，再读代码。完整 SSOT 分层见 [00-governance/02-ssot-architecture.md](00-governance/02-ssot-architecture.md)。

| 信息点 | 权威文档（SSOT） | 速查 |
|--------|-----------------|------|
| 术语定义 | [03-glossary.md](01-product/03-glossary.md) | — |
| 权限矩阵（产品规则） | [04-permissions.md](01-product/04-permissions.md) | [12-permissions-matrix.md](03-architecture/12-permissions-matrix.md)（视图层） |
| 业务规则 / US 需求 | [02-requirements/](02-requirements/README.md)（按模块） | [13-traceability-matrix.md](02-requirements/13-traceability-matrix.md) |
| 数据模型（实体/字段/状态枚举） | [04-data-model.md](03-architecture/04-data-model.md) | [13a-data-model.md](03-architecture/13a-data-model.md)（视图层） |
| API 端点契约 | [04-api-reference/](04-api-reference/README.md)（按模块） | [13b-api-endpoints.md](03-architecture/13b-api-endpoints.md)（视图层） |
| 任务清单 / 进度 / 决策 | [13-project-master-plan.md](03-architecture/13-project-master-plan.md) | — |
| 当前状态 / 已知问题 | [13c-current-status.md](03-architecture/13c-current-status.md) | — |
| 架构决策 (ADR) | [03-architecture/decisions/](03-architecture/decisions/) | — |
| 技术栈 / 架构总览 | [00-architecture-summary.md](03-architecture/00-architecture-summary.md) | — |
| 部署 / 配置 | [06-operations/](06-operations/README.md) | — |
| 命名规范 / 文档规则 | [01-naming-convention.md](00-governance/01-naming-convention.md) | — |

**规则**：
1. 每个信息点只有一个权威定义（SSOT），其他文档只引用不复制
2. 文档与代码冲突时：先更新文档，再按文档改代码（「以文档为准」规则，见 AGENTS.md）
3. 需求模块文档中业务规则以 `BR-*` 编号引用，US 以 `US-{DOMAIN}-{NNN}` 引用
4. 过程文档（计划/报告/任务书）一律归档 `compose/`；正式目录（架构/需求/API）只放当前值，历史报告不删除、不放入正式目录

---

## 按角色找文档

### 🏥 我是前台

| 我要… | 看这里 |
|-------|--------|
| 登记患者 | [患者管理](02-requirements/04-patients.md) |
| 挂号/退号 | [挂号管理](02-requirements/08-registration.md) |
| 读身份证 | [读卡器](02-requirements/11e-cardreader.md) |
| 登录系统 | [认证授权](02-requirements/02-auth.md) |

### 👨‍⚕️ 我是医生

| 我要… | 看这里 |
|-------|--------|
| 看诊全流程 | [医案管理](02-requirements/07-medical-cases.md) + [挂号管理](02-requirements/08-registration.md) |
| 开处方/打印 | [处方打印](02-requirements/09-printing.md) |
| 查药材/验方 | [药材管理](02-requirements/05-herbs.md) + [验方管理](02-requirements/06-formulas.md) |

### 🔧 我是管理员

| 我要… | 看这里 |
|-------|--------|
| 管理用户 | [用户管理](02-requirements/03-users.md) |
| 管理药材/验方 | [药材管理](02-requirements/05-herbs.md) + [验方管理](02-requirements/06-formulas.md) |
| 看报表 | [报表管理](02-requirements/10-reports.md) |
| 系统配置 | [配置管理](02-requirements/11b-configuration.md) |

### 💻 我是开发者

| 我要… | 看这里 |
|-------|--------|
| 快速上手 | [开发指南](05-development/01-setup.md) |
| 了解架构 | [架构总览](03-architecture/00-architecture-summary.md) |
| 查 API | [API 参考](04-api-reference/README.md) |
| 看编码规范 | [编码规范](05-development/02-code-standards.md) |
| 跑测试 | [测试指南](05-development/04-testing.md) |

### 🚀 我是运维

| 我要… | 看这里 |
|-------|--------|
| 部署系统 | [部署指南](06-operations/01-deployment.md) |
| 配置服务器 | [配置管理](06-operations/02-configuration.md) |
| 备份恢复 | [备份恢复](06-operations/07-backup-recovery.md) |
| 监控告警 | [监控告警](06-operations/08-monitoring-alerting.md) |

---

## 文档目录

| 目录 | 内容 | 文件数 |
|------|------|:------:|
| [00-governance](00-governance/) | 文档治理：命名规范、SSOT 架构、技术引入治理 | 3 |
| [01-product](01-product/) | 产品愿景、用户画像、术语表、权限矩阵、角色交互 | 6 |
| [02-requirements](02-requirements/) | 需求文档（15 模块，142 US） | 19 |
| [03-architecture](03-architecture/) | 架构文档（当前值）、ADR 决策记录、权限矩阵、总账 | 52 |
| [04-api-reference](04-api-reference/) | API 端点文档 | 15 |
| [05-development](05-development/) | 开发指南、编码规范、测试标准 | 27 |
| [06-operations](06-operations/) | 部署、配置、监控、备份 | 15 |
| [compose](compose/) | 过程文档归档：plans/reports/specs（5/22/13） | 40 |
| [prompts](prompts/) | Prompt 模板 | 2 |
| [training](training/) | 培训材料 | 1 |

**总计：143 个文档**（历史报告已完成使命删除，2026-08-06）

---

## 快速导航

### 新人上手 3 步走

1. [产品愿景](01-product/01-vision.md) — 系统做什么、为谁做
2. [架构总览](03-architecture/00-architecture-summary.md) — 系统怎么搭的
3. [快速开始](05-development/01-setup.md) — 5 分钟跑起来

### 术语不懂？

看 [术语表](01-product/03-glossary.md)。最常见的：
- **医案** = 一次完整的诊疗记录（⚠️ 不是"病历"）
- **验方** = 可复用的处方模板（⚠️ 不是"公式"）
- **中医诊断** = 望闻问切、辨证论治（⚠️ 不是"问诊"）

---

## 文档约定

- 正文中文，技术标识符英文
- 需求编号：`US-{DOMAIN}-{NNN}`（如 `US-MC-017`）
- 架构决策：`ADR-NNNN`（如 `ADR-0001`）
- 命名规范：[00-governance/01-naming-convention.md](00-governance/01-naming-convention.md)
- SSOT 分层：[00-governance/02-ssot-architecture.md](00-governance/02-ssot-architecture.md)

---

*文档版本: v4.0 | 最后更新: 2026-08-06*
