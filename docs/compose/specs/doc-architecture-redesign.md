# 文档架构重设计方案

> 原则：**文档 = 状态量字典，不是流水账。** 每个概念只定义一次，其他地方只引用。

## 一、核心概念权威源（SSOT）

每个核心概念指定唯一权威定义位置，其他文档通过 `[[链接]]` 引用。

| 概念 | 权威源 | 引用方式 |
|------|--------|---------|
| 权限矩阵 | `01-product/04-permissions.md` | 所有权限相关文档引用此文件 |
| 角色定义 | `01-product/02-personas.md` | 引用 |
| 术语表 | `01-product/03-glossary.md` | 引用 |
| 认证/Token | `02-requirements/02-auth.md` | Token 过期/旋转只在此定义 |
| 医案生命周期 | `02-requirements/07-medical-cases.md` | 取消策略/状态机只在此定义 |
| 药材管理 | `02-requirements/05-herbs.md` | 引用 |
| 验方管理 | `02-requirements/06-formulas.md` | 引用 |
| 挂号管理 | `02-requirements/08-registration.md` | 引用 |
| 处方打印 | `02-requirements/09-printing.md` | 引用 |
| CQRS 模式 | `03-architecture/03-server.md` | 架构层 CQRS 定义 |
| 模块边界 | `03-architecture/03-server.md` | 模块间通信规则 |
| 配置管理 | `03-architecture/07-configuration.md` | Options 集中规则 |
| 数据模型 | `03-architecture/04-data-model.md` | 实体/字段/枚举 |
| 安全架构 | `03-architecture/09-security-architecture.md` | 安全策略 |
| API 端点 | `04-api-reference/README.md` | 按模块索引 |
| 编码规范 | `05-development/02-code-standards.md` | 命名/模式/规范 |

## 二、目录结构（保持现有，不改目录）

现有目录结构合理，不需要重新组织目录。重点是**内容层面去重**。

```
docs/
├── 00-governance/     (5)  治理规范
├── 01-product/        (6)  产品定义 ← 权限/角色/术语的权威源
├── 02-requirements/   (19) 需求文档 ← 各模块 US 的权威源
├── 03-architecture/   (59) 架构文档 ← 技术设计的权威源
│   ├── decisions/     (22) ADR（历史决策记录）
│   ├── modules/       (10) 各模块架构
│   └── localwebapi/   (1)  本地模式
├── 04-api-reference/  (15) API 文档
├── 05-development/    (19) 开发指南
├── 06-operations/     (12) 运维文档
├── 07-ui-ux/          (2)  UI/UX
├── compose/           (2)  过程文档归档
├── prompts/           (3)  提示词
└── training/          (2)  培训材料
```

## 三、内容去重规则

### 规则 1：权威源只定义一次

每个核心概念**只在权威源中完整定义**，其他文档只引用不复制。

**示例 — 权限矩阵**：
- ✅ `04-permissions.md`：完整定义四角色权限矩阵
- ✅ `02-personas.md`：`> 权限矩阵详见 [[04-permissions]]`
- ❌ `12-permissions-matrix.md`：不应重复定义，应引用 `04-permissions.md`
- ❌ `09-security-architecture.md`：不应嵌入权限表，应引用

### 规则 2：业务规则不重复写

同一业务规则（如"取消=物理删除"）只在需求文档中定义一次。

**示例 — 医案取消策略**：
- ✅ `07-medical-cases.md`：完整定义取消策略（BR-000 + US-MC-014）
- ✅ `03-glossary.md`：`> 取消医案 = 物理删除，详见 [[07-medical-cases#BR-000]]`
- ✅ `04-permissions.md`：`> 医案取消语义详见 [[07-medical-cases#BR-000]]`
- ❌ 同文件内状态机 + 业务规则表格 + 注释块三处重复

### 规则 3：技术模式不重复定义

CQRS/MediatR/模块边界等技术模式只在架构层定义一次。

**示例 — CQRS**：
- ✅ `03-server.md`：定义 CQRS 模式 + 哪些模块使用
- ✅ `07-medical-cases.md`：`> 医案模块采用 CQRS，详见 [[03-server#CQRS]]`
- ❌ 不应在 `07-medical-cases.md` 中重新解释什么是 CQRS

### 规则 4：配置项不重复列出

配置项只在 `07-configuration.md` 中定义一次。

**示例 — Token 过期时间**：
- ✅ `07-configuration.md`：定义 JwtOptions 配置项
- ✅ `02-auth.md`：`> Token 过期时间见 [[07-configuration#JwtOptions]]`
- ❌ `12-nfr.md`：不应重复列出 Token 过期时间
- ❌ `00-architecture-summary.md`：不应嵌入配置值

### 规则 5：历史内容只在历史文档中

变更记录、决策理由、演进过程只在以下文档中：
- **ADR**（decisions/）：决策理由和历史背景
- **变更日志**：版本变更记录
- **总账 §九**：决策时间线

主文档中**不出现**：
- `> **废弃说明**: ...`
- `> **注意**: ...从 XXX 迁移而来`
- `旧 XXX 已废弃，当前为 YYY`

## 四、实施步骤

### Phase 1：识别重复（已完成）

扫描结果：
- 权限矩阵：26 处引用（需去重至 ~6 处引用 + 1 处定义）
- CQRS：16 处引用（需去重至 ~4 处引用 + 1 处定义）
- Token 旋转：11 处引用（需去重至 ~3 处引用 + 1 处定义）
- 医案取消：10 处引用（需去重至 ~3 处引用 + 1 处定义）

### Phase 2：建立引用关系

对每个重复概念：
1. 确认权威源（见第一节）
2. 其他文档中的重复定义替换为 `[[权威文档]]` 引用
3. 保留上下文说明，删除重复的完整定义

### Phase 3：清理历史内容

从主文档中删除：
- 废弃说明
- 迁移历史
- 过时的对比表

保留到：
- ADR（decisions/）
- 变更日志（各文档末尾的版本记录表）
- 总账 §九

### Phase 4：验证

- Obsidian CLI：断链 = 0
- Obsidian CLI：孤儿 ≤ 15（剩余为 ADR/归档/资产）
- 概念重复检测：核心概念引用 ≤ 10 处

## 五、预期效果

| 指标 | 当前 | 目标 |
|------|------|------|
| 权限矩阵定义 | 26 处 | 1 处（04-permissions.md） |
| CQRS 定义 | 16 处 | 1 处（03-server.md） |
| Token 旋转定义 | 11 处 | 1 处（02-auth.md） |
| 医案取消定义 | 10 处 | 1 处（07-medical-cases.md） |
| 主文档总行数 | ~8000 | ~5000（减少 ~37%） |
| 信息丢失 | — | 0（所有信息保留，只是引用化） |

## 六、风险控制

1. **不改目录结构**：只改内容，不改文件位置，避免大规模断链
2. **先备份再改**：每次修改前 git commit 当前状态
3. **逐个概念处理**：一次只处理一个概念的去重，验证后再处理下一个
4. **引用必须有效**：每次修改后运行 `unresolved` 检查断链
