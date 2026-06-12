# 文档系统重构计划

## 现状分析

### 文档分布

| 区域 | 文件数 | 状态 | 问题 |
|------|--------|------|------|
| docs/01-product/ | 6 | 良好 | - |
| docs/02-requirements/ | 17 | 良好 | - |
| docs/03-architecture/ | 14 | 部分过时 | dual-mode.md 已更新为三模式，但 ADR-0002 仍描述双模式 |
| docs/04-api-reference/ | 10 | 部分过时 | 缺少 LocalWebAPI 端点文档 |
| docs/05-development/ | 5 | 部分过时 | 缺少 LocalWebAPI 开发指南 |
| docs/06-operations/ | ~35 | 混乱 | 15+ Newman JSON artifacts (~30MB), 部署文档混杂 |
| docs/plans/ | 1+ | 空壳 | 仅 README |
| docs/code-review/ | ? | 待查 | - |
| docs/reports/ | ? | 待查 | - |
| docs/testing/ | ? | 待查 | - |
| docs/training/ | 1 | 良好 | - |
| docs/superpowers/ | ? | 待查 | - |
| docs/ 根目录 | ~5 | 混乱 | 散落文件 (LYBTZYZS_Environment.json, test checklist 等) |

### AI 文档分布

| 类型 | 数量 | 位置 | 问题 |
|------|------|------|------|
| AGENTS.md | 15 | 散布在 src/, tests/, 根目录 | 与 docs/ 内容有重叠 |
| CLAUDE.md | 1 | 根目录 | 与 AGENTS.md 有重叠 |
| 子目录 AGENTS.md | 14 | 各模块目录 | 由 /init-deep 生成，质量较好 |

### 已知问题

1. **Newman artifacts**: 15+ JSON 文件，每个 ~2MB，共 ~30MB，应移至 artifacts/ 或删除
2. **三模式架构**: dual-mode.md 已更新，但 ADR-0002 仍描述双模式
3. **API 文档缺失**: 缺少 LocalWebAPI 的 8 个控制器端点文档
4. **开发指南缺失**: 缺少 LocalWebAPI 开发指南
5. **散落文件**: docs/ 根目录有不应在此的文件
6. **空目录**: plans/, code-review/, reports/, testing/, superpowers/ 可能为空或仅有 README

## 重构方案

### 原则

1. **docs/ 只保留人类可读文档** - 移除所有 JSON artifacts、测试报告、环境配置
2. **AGENTS.md 保留为 AI 可读导航** - 不删除，但确保与 docs/ 不重复
3. **单一信息源** - 每个主题只在一个地方描述
4. **按读者分类** - 产品/需求/架构/开发/运维 五个维度清晰分离

### 目录结构调整

```
docs/
├── README.md                          # 文档中心索引 (更新)
├── 01-product/                        # 产品文档 (不变)
│   ├── README.md
│   ├── vision.md
│   ├── personas.md
│   ├── user-roles.md
│   ├── jtbd.md
│   ├── glossary.md
│   └── clinical-workflow.md
├── 02-requirements/                   # 需求文档 (不变)
│   ├── README.md
│   ├── prd.md
│   ├── nfr.md
│   ├── user-story-map.md
│   ├── roadmap.md
│   ├── role-permission-matrix.md
│   ├── ui-patterns.md
│   └── [各模块需求文档...]
├── 03-architecture/                   # 架构文档 (更新)
│   ├── README.md
│   ├── system-overview.md             # 更新为三模式
│   ├── server.md
│   ├── desktop.md
│   ├── shared.md
│   ├── data-model.md
│   ├── configuration.md
│   ├── three-mode.md                  # 新: 三模式架构 (重命名 dual-mode.md)
│   ├── error-handling-architecture.md
│   ├── decisions/                     # ADR (更新)
│   │   ├── README.md
│   │   ├── 0001-medicalcase-aggregate-root.md
│   │   ├── 0002-dual-mode-architecture.md  # 更新为三模式
│   │   ├── 0009-localwebapi-embedded.md    # 新: LocalWebAPI 架构决策
│   │   └── [...]
│   └── localwebapi/                   # 新: LocalWebAPI 专项文档
│       ├── overview.md                # 架构概览
│       ├── api-endpoints.md           # 端点文档
│       ├── authentication.md          # 认证机制
│       └── deployment.md              # 部署说明
├── 04-api-reference/                  # API 参考 (更新)
│   ├── README.md
│   ├── auth.md
│   ├── users.md
│   ├── patients.md
│   ├── herbs.md
│   ├── formulas.md
│   ├── medical-cases.md
│   ├── registrations.md
│   ├── sync.md
│   ├── health.md
│   ├── diagnostics.md
│   └── localwebapi.md                 # 新: LocalWebAPI 端点
├── 05-development/                    # 开发指南 (更新)
│   ├── README.md
│   ├── setup.md
│   ├── code-standards.md
│   ├── testing.md
│   └── localwebapi-guide.md           # 新: LocalWebAPI 开发指南
├── 06-operations/                     # 运维文档 (清理)
│   ├── README.md
│   ├── WINDOWS-DEPLOYMENT.md
│   ├── webapi-deployment-summary.md
│   ├── development-environment-spec.md
│   ├── dead-code-analysis-frontend.md
│   ├── postman-guide.md
│   └── postman-collection-changelog.md
├── artifacts/                         # 新: 非文档资产 (从 06-operations 移入)
│   ├── LYBTZYZS_API_Collection.json
│   ├── newman-environment.json
│   ├── newman-environment-updated.json
│   └── cleanup-active-cases.json
└── archives/                          # 新: 归档 (从 06-operations 移入)
    └── newman-test-results/           # 15+ JSON 测试报告
```

### 需要删除/归档的内容

| 文件/目录 | 操作 | 原因 |
|-----------|------|------|
| docs/06-operations/newman-test-results*.json (15 files) | 移至 archives/ | 测试报告 artifact，不是文档 |
| docs/06-operations/newman-run-current.json | 移至 archives/ | 同上 |
| docs/06-operations/newman-report.json | 移至 archives/ | 同上 |
| docs/06-operations/LYBTZYZS_API_Collection.json | 移至 artifacts/ | Postman 集合，不是文档 |
| docs/06-operations/newman-environment*.json | 移至 artifacts/ | 环境配置，不是文档 |
| docs/06-operations/cleanup-active-cases.json | 移至 artifacts/ | API 请求示例，不是文档 |
| docs/ LYBTZYZS_Environment.json | 移至 artifacts/ | 散落文件 |
| docs/ LYBTZYZS_API_Tests.md | 合并到 06-operations/ | 散落文件 |
| docs/test-scenarios-checklist.md | 移至 05-development/ 或删除 | 散落文件 |
| docs/userjourneys-test-checklist*.md | 移至 05-development/ 或删除 | 散落文件 |
| docs/code-review/ | 检查后决定 | 可能为空 |
| docs/reports/ | 检查后决定 | 可能为空 |
| docs/testing/ | 检查后决定 | 可能为空 |
| docs/superpowers/ | 检查后决定 | 可能为空 |

### 需要新增的内容

| 文件 | 内容 | 优先级 |
|------|------|--------|
| 03-architecture/three-mode.md | 三模式架构完整文档 (基于 dual-mode.md 扩展) | 高 |
| 03-architecture/localwebapi/overview.md | LocalWebAPI 架构概览 | 高 |
| 03-architecture/localwebapi/api-endpoints.md | LocalWebAPI 8 个控制器端点文档 | 高 |
| 03-architecture/localwebapi/authentication.md | JWT 认证机制 (简化 HMAC-SHA256) | 中 |
| 03-architecture/localwebapi/deployment.md | SQLite 数据库部署说明 | 中 |
| 03-architecture/decisions/0009-localwebapi-embedded.md | ADR: 嵌入式 LocalWebAPI 决策 | 高 |
| 04-api-reference/localwebapi.md | LocalWebAPI API 端点参考 | 高 |
| 05-development/localwebapi-guide.md | LocalWebAPI 开发指南 | 高 |
| 03-architecture/01-system-overview.md | 更新架构图为三模式 | 高 |
| 03-architecture/decisions/0002-dual-mode-architecture.md | 更新为三模式引用 | 中 |

### AGENTS.md 处理策略

| 策略 | 说明 |
|------|------|
| **保留** | 所有 15 个 AGENTS.md 文件保留不变 |
| **理由** | AGENTS.md 是 AI 可读的模块导航，docs/ 是人类可读的文档，两者职责不同 |
| **去重** | 确保 AGENTS.md 中的 "WHERE TO LOOK" 指向 docs/ 中的对应文档 |
| **更新** | 为 LocalWebAPI 目录新增 AGENTS.md (已由 /init-deep 生成) |

## 执行步骤

### Wave 1: 清理 artifacts (低风险)
1. 创建 docs/artifacts/ 目录
2. 创建 docs/archives/newman-test-results/ 目录
3. 移动所有 Newman JSON 文件到 archives/
4. 移动 Postman/环境 JSON 到 artifacts/
5. 移动 docs/ 根目录散落文件到正确位置

### Wave 2: 更新架构文档 (中风险)
1. 重命名 dual-mode.md → three-mode.md
2. 更新 system-overview.md 为三模式
3. 创建 localwebapi/ 子目录和 4 个文档
4. 创建 ADR-0009
5. 更新 ADR-0002 引用

### Wave 3: 更新 API 和开发文档 (低风险)
1. 创建 04-api-reference/localwebapi.md
2. 创建 05-development/localwebapi-guide.md
3. 更新 docs/README.md 索引

### Wave 4: 验证
1. 检查所有内部链接是否有效
2. 检查 AGENTS.md 的 "WHERE TO LOOK" 是否指向正确位置
3. 确认无悬挂引用
