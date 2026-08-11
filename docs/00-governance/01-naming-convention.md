# 文档命名规范

> 版本: v1.0 | 日期: 2026-08-02

## 命名规则

### 主文档（带编号）

格式：`XX-kebab-case.md`

- `XX` = 两位数字序号（01-99），表示阅读顺序
- `kebab-case` = 小写英文+连字符，简明描述内容
- 编号连续，不留空洞（删除后重编号）

```
01-vision.md
02-personas.md
03-glossary.md
```

### README.md

每个目录必须有 `README.md`，作为该目录的索引和说明。**不带编号**。

### 子目录

子目录按主题分组，不带编号：

```
decisions/       # ADR 架构决策记录
modules/         # 模块架构规格
standards/       # 开发标准
archive/         # 已完成的历史文档（可定期清理）
```

### 子目录内文件

子目录内文件**不带数字编号**，按语义命名：

```
decisions/0001-medicalcase-aggregate-root.md
modules/auth.md
standards/STD-01-CQRS-Boundary.md
```

### compose 目录（过程文档归档）

用于存放全部过程性文档：计划、报告（审计/分析/验证）、任务书（task-spec）。**正式文档（架构/需求/API）只放当前值，过程文档一律归档到 compose，不进入正式目录。**

```
compose/plans/<设计稿>.md              # 方案设计（如 backup-featuretoggle-design）
compose/reports/<审计/分析报告>.md      # 活文档（覆盖矩阵/AC 映射/收官报告等）
compose/specs/<任务书>.md              # 任务书（已收敛——仅保留必要的任务书）
```

## 禁止事项

- ❌ 编号留空洞（如 01, 03, 05 — 缺02、04）
- ❌ 同目录两个相同编号（如两个13-）
- ❌ 中文文件名
- ❌ 空格和特殊字符
- ❌ 无意义的文件名（如 `notes.md`、`temp.md`）

## 重编号规则

当删除导致编号不连续时，重命名后续文件使编号连续：

```
# 删除 03 后：
01-vision.md    → 01-vision.md（不变）
02-personas.md  → 02-personas.md（不变）
04-glossary.md  → 03-glossary.md（重编号）
```

子目录内文件不重编号（ADR 保持原始编号）。

## 示例

```
01-product/
├── README.md
├── 01-vision.md
├── 02-personas.md
└── 03-glossary.md

03-architecture/
├── README.md
├── 00-architecture-summary.md
├── 01-system-overview.md
├── ...
├── 12-permissions-matrix.md
├── 13-project-master-plan.md
├── decisions/
│   ├── 0001-medicalcase-aggregate-root.md
│   └── ...
├── modules/
│   ├── auth.md
│   └── ...
└── localwebapi/
    ├── overview.md
    └── ...
```
