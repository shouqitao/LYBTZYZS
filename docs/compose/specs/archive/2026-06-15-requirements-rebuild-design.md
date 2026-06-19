# 需求文档体系重建设计

> 日期: 2026-06-15
> 状态: 已批准

## [S1] 目标

删除现有 `docs/01-product/`（10 文件）和 `docs/02-requirements/`（22 文件），基于代码现状（138 US 已实现）从零重建。

## [S2] 文档体系

### 01-product/（WHO + WHY）

| 文件 | 内容 |
|------|------|
| README.md | 索引 |
| 01-vision.md | 产品愿景 + 问题定义 + 核心价值 |
| 02-personas.md | 4 角色画像（Receptionist/Doctor/Admin/SuperAdmin） |
| 03-glossary.md | 业务术语表（中医 + 技术术语） |

### 02-requirements/（WHAT）

| 文件 | 模块 | US 数 |
|------|------|-------|
| README.md | 索引 + US 总览表 | — |
| 01-prd.md | 顶层 PRD | — |
| 02-auth.md | 认证与会话 | 13 |
| 03-users.md | 用户管理 | 12 |
| 04-patients.md | 患者管理 | 13 |
| 05-herbs.md | 药材管理 | 13 |
| 06-formulas.md | 验方管理 | 13 |
| 07-medical-cases.md | 医案管理 | 18 |
| 08-registration.md | 挂号管理 | 7 |
| 09-printing.md | 处方打印 | 4 |
| 10-sync.md | 数据同步 | 8 |
| 11-platform.md | 平台基础设施（Shell+Config+Error+Logging+Health+CardReader） | 35 |
| 12-nfr.md | 非功能需求 | — |

### 保留不变

- `docs/03-architecture/` — 架构设计
- `docs/04-api-reference/` — API 参考
- `docs/05-development/` — 开发指南
- `docs/06-operations/` — 运维文档

## [S3] US 格式规范

```markdown
### US-XXX-NNN: 标题

**角色**: 角色
**优先级**: Must / Should / Could
**状态**: ✅ 已实现

**作为** [角色]，**我想要** [功能]，**以便** [价值]

**验收标准**:
- [ ] 条件 1
- [ ] 条件 2

**业务规则**:
1. 规则描述

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | ... |
| 本地 | ... |
```

## [S4] 关键改进

1. 模块从 15 个精简到 10 个（Shell/Config/Error/Logging/Health/CardReader → platform.md）
2. 删除冗余：user-story-map.md、roadmap.md、role-permission-matrix.md（内容合入对应模块）
3. 删除 jtbd.md、value-proposition.md、customer-journey.md（合入 vision.md）
4. 统一 US 格式（角色/优先级/状态/验收标准/业务规则/双模式）

## [S5] 执行计划

| 阶段 | 内容 |
|------|------|
| 1 | 删除旧文档 + 创建新目录结构 |
| 2 | 重写 01-product/（vision + personas + glossary） |
| 3 | 重写 02-requirements/ 顶层 PRD + README |
| 4 | 逐模块重写 US（按优先级：MedicalCase → Patients → Herbs → ...） |
| 5 | 重写 NFR |
| 6 | 验证交叉引用 + 提交 |
