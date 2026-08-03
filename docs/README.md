# 文档中心

> 凌隐宝堂中医诊所管理系统 — 所有文档的入口

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
| [01-product](01-product/) | 产品愿景、用户画像、术语表、权限矩阵、角色交互 | 6 |
| [02-requirements](02-requirements/) | 需求文档（15 模块，142 US） | 19 |
| [03-architecture](03-architecture/) | 架构文档、ADR 决策记录、权限矩阵 | 48 |
| [04-api-reference](04-api-reference/) | API 端点文档 | 15 |
| [05-development](05-development/) | 开发指南、编码规范、测试标准 | 27 |
| [06-operations](06-operations/) | 部署、配置、监控、备份 | 15 |
| [compose](compose/) | 活动 spec/plan/report | 4 |
| [prompts](prompts/) | Prompt 模板 | 2 |
| [reports](reports/) | 校准报告、审计报告 | 6 |
| [training](training/) | 培训材料 | 1 |

**总计：146 个文档**

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
- 命名规范：[00-naming-convention.md](00-naming-convention.md)

---

*文档版本: v3.0 | 最后更新: 2026-08-02*
