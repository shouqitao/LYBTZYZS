# 文档体系重构设计

**日期**: 2026-02-10
**状态**: 已确认，待执行

---

## 背景

当前 `docs/` 目录有 608 个文件、444K 行，分布在 17 个顶级目录中。存在以下问题:

1. **18 个空目录** -- 规划了但从未填充
2. **分类重叠** -- `guides/`、`how-to-guides/`、`reference/how-to/`、`tutorials/` 四处放指南类文档
3. **process/ 过于庞大** -- 216 文件/115K 行历史过程日志，有标准后不再需要
4. **state/ 定位模糊** -- 131 文件/130K 行，与其他目录内容重叠
5. **Diataxis 框架执行不一致** -- 文件散落在错误分类中
6. **缺少统一的产品需求文档** -- 业务规则分散在 48 个 OpenSpec 规范中

## 目标

建立以 **产品需求文档 (PRD)** 为核心的文档体系，从 608 个文件精简为约 35 个高质量文档，结构清晰、无冗余、可维护。

---

## 目录结构

```
docs/
├── README.md                    # 文档导航入口
├── assets/                      # 文档引用的图片/图表
├── 01-product/                  # 产品层: 做什么、为谁做
│   ├── README.md               # 产品概述
│   ├── vision.md               # 产品愿景与目标
│   ├── glossary.md             # 术语表 (中英文对照)
│   └── user-roles.md           # 用户角色与权限定义
├── 02-requirements/             # 需求层: 每个模块的完整功能需求
│   ├── README.md               # 需求总览
│   ├── auth.md                 # 认证与会话管理
│   ├── users.md                # 用户管理
│   ├── patients.md             # 患者管理
│   ├── herbs.md                # 药材管理
│   ├── formulas.md             # 验方管理
│   ├── medical-cases.md        # 医案管理 (核心)
│   ├── sync.md                 # 数据同步
│   └── printing.md             # 打印功能
├── 03-architecture/             # 架构层: 怎么做
│   ├── README.md               # 架构总览
│   ├── system-overview.md      # 系统架构图
│   ├── server.md               # 服务端架构
│   ├── desktop.md              # 桌面端架构
│   ├── shared.md               # 共享层架构
│   ├── dual-mode.md            # 双模式架构 (本地+远程)
│   ├── data-model.md           # 数据模型 (实体关系)
│   └── decisions/              # 架构决策记录 (ADR)
│       └── NNNN-title.md
├── 04-api-reference/            # API 参考: 端点详细文档
│   ├── README.md               # API 总览
│   ├── auth.md                 # 认证 API
│   ├── users.md                # 用户 API
│   ├── patients.md             # 患者 API
│   ├── herbs.md                # 药材 API
│   ├── formulas.md             # 验方 API
│   ├── medical-cases.md        # 医案 API
│   └── sync.md                 # 同步 API
├── 05-development/              # 开发指南: 如何参与开发
│   ├── README.md               # 快速开始
│   ├── setup.md                # 环境搭建
│   ├── code-standards.md       # 编码规范
│   ├── patterns.md             # 设计模式速查
│   └── testing.md              # 测试指南
└── 06-operations/               # 运维层: 部署与运行
    ├── README.md               # 运维总览
    ├── deployment.md           # 部署指南
    └── configuration.md        # 配置说明
```

---

## 文档模板标准

### 产品文档 (01-product/)

```markdown
# [文档标题]

## 概述
一段话说明本文档的内容和目的。

## 正文
(按文档类型各异)

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
```

### 需求文档 (02-requirements/[module].md)

```markdown
# [模块名] 需求规格

## 概述
一段话描述该模块的业务目标和价值。

## 用户角色
| 角色 | 在本模块中的操作权限 |
|------|---------------------|

## 功能清单

### FR-[模块缩写]-001: [功能名称]
- **描述**: 一句话描述功能
- **业务规则**:
  1. 规则1
  2. 规则2
- **远程模式**: 行为描述
- **本地模式**: 行为描述 / 不支持 / 待讨论
- **验收标准**:
  - [ ] 标准1
  - [ ] 标准2

## 数据模型
本模块涉及的实体和关键字段。

## 待讨论项
| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
```

### 架构文档 (03-architecture/)

```markdown
# [架构主题]

## 概述
本文档描述 [主题] 的架构设计。

## 架构图
(Mermaid 图表)

## 设计决策
| 决策 | 选项 | 选择 | 理由 |
|------|------|------|------|

## 关键约束

## 变更记录
```

### API 参考文档 (04-api-reference/[module].md)

```markdown
# [模块名] API 参考

## 基本信息
- **Base URL**: `/api/v1/[resource]`
- **认证**: Bearer Token
- **权限**: [角色要求]

## 端点列表

### POST /api/v1/[resource]
- **功能**: 创建[资源]
- **请求体**: (JSON)
- **成功响应** (200): (JSON)
- **错误响应**: (状态码/错误码/说明)

## 变更记录
```

### 开发指南 (05-development/)

```markdown
# [指南标题]

## 目标读者
## 前置条件
## 步骤
## 常见问题
## 变更记录
```

### 运维文档 (06-operations/)

```markdown
# [运维主题]

## 环境要求
## 部署步骤
## 配置项说明
## 监控与故障排查
## 变更记录
```

---

## 核心规则

| 编号 | 规则 | 说明 |
|------|------|------|
| R-01 | 每个文档必须有变更记录 | 追踪文档演进 |
| R-02 | 需求文档必须有双模式对比 | 远程/本地行为差异，未决项标记"待讨论" |
| R-03 | 功能用 FR-XXX-NNN 编号 | 全局唯一可追踪 |
| R-04 | 每个目录有 README.md | 索引导航 |
| R-05 | 无空目录 | 没内容就不建目录 |
| R-06 | 中文正文 + 英文技术标识 | 不翻译代码标识符 |
| R-07 | 运行时资源归 src/ | Excel 模板等随代码部署 |
| R-08 | 文档图片归 docs/assets/ | 统一管理 |

---

## 语言规范

| 内容类型 | 语言 | 示例 |
|----------|------|------|
| 文档正文 | 中文 | "患者管理模块负责..." |
| 技术术语 | 英文原文 | MedicalCase, Repository, DTO |
| API 路径 | 英文原文 | `POST /api/v1/patients` |
| 功能编号 | 英文前缀 | FR-MC-001 |
| 表头/标签 | 中文 | 功能名称、业务规则、验收标准 |

---

## 迁移策略

| 来源 | 处理方式 |
|------|----------|
| `openspec/specs/` (48个规范) | 业务规则 → `02-requirements/`，架构规则 → `03-architecture/`，最终随 openspec 废弃 |
| `openspec/project.md` | 内容合并到 `01-product/` 和 `03-architecture/`，最终废弃 |
| `docs/state/` (131文件) | 提取 ADR 和架构设计到 `03-architecture/`，删除 |
| `docs/process/` (216文件) | 全部删除 |
| `docs/reference/` (142文件) | API 文档 → `04-api-reference/`，操作指南 → `05-development/`，删除 |
| 其他旧目录 | 提取有价值内容后删除 |
| 项目根 `README.md` | 精简为项目简介 + 指向 docs/ 的链接 |
| 项目根 `CHANGELOG.md` | 保留不动 |
| 项目根 `CLAUDE.md` | 保留不动，引用路径后续更新 |

---

## 执行顺序

| Phase | 内容 | 文件数 |
|-------|------|--------|
| Phase 1 | 编写 `01-product/` (产品文档) | 4 |
| Phase 2 | 编写 `02-requirements/` (需求文档) | 9 |
| Phase 3 | 编写 `03-architecture/` (架构文档) | 7+ |
| Phase 4 | 编写 `04-api-reference/` (API 文档) | 8 |
| Phase 5 | 编写 `05-development/` + `06-operations/` | 8 |
| Phase 6 | 清理旧文档 + 更新根目录文件引用 | - |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始设计，brainstorm 完成并确认 |
