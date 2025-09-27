# LYBTZYZS 文档系统架构

**文档版本**：v1.0  
**创建时间**：2025-09-25  
**维护负责**：Claude Code

## 🎯 系统目标

目标：建立一致、可查、及时的文档体系，作为权威标准。
- 统一性：格式与维护标准一致
- 可发现：清晰导航可快速定位
- 权威性：README 等文档具备强约束力
- 实时性：与代码同步更新，避免过期

## 📁 文档架构设计

### 分层架构
```
文档系统 (3层架构)
├── L1: 项目总览层
│   ├── README.md (根目录) - 系统概览与快速入门
│   ├── CLAUDE.md - Claude Code开发约束与指导
│   └── docs/index.md - 文档系统总导航
├── L2: 领域专题层  
│   ├── docs/api/ - API接口文档
│   ├── docs/architecture/ - 架构设计文档
│   ├── docs/development/ - 开发规范指导
│   ├── docs/prd/ - （废弃）产品需求文档，现使用 GitHub Issues
│   ├── docs/tasks/ - 任务管理文档
│   └── docs/reports/ - 分析报告文档
└── L3: 实施细节层
    ├── src/*/README.md - 各项目具体说明
    └── 专题细分文档
```

### 导航关系
- 垂直：L1 → L2 → L3 逐层深入
- 水平：同层交叉引用
- 反向：各层提供返回上级链接

## 📋 文档标准规范

### README文件标准

**L1 README 要素**：
1. 系统概览（架构、模块）
2. 快速开始（构建/运行/测试）
3. 当前状态（编译、已知问题）
4. 开发约束（优先级、限制）
5. 文档索引（指向 L2）

**L2 README 要素**：
1. 领域说明（范围/目标/负责人）
2. 文档索引（本领域清单）
3. 维护规则（创建/更新/归档）
4. 关联文档（交叉引用）

**L3 README 要素**：
1. 职责（边界/依赖）
2. 技术（架构/API/配置）
3. 指南（构建/测试/部署）

### 文档元信息标准
```markdown
# 文档标题

**文档版本**：vX.Y  
**创建时间**：YYYY-MM-DD  
**最后更新**：YYYY-MM-DD  
**维护负责**：角色名称  
**关联文档**：[文档1](path1), [文档2](path2)
```

## 🔄 维护流程规范

### 角色职责分工

| 角色 | 职责范围 | 主要文档 |
|------|----------|----------|
| **Thinker** | 架构决策、需求管理、任务规划 | README.md, docs/architecture/, docs/prd/, docs/tasks/ |
| **Claude Code** | 开发实现、技术文档、完成报告 | CLAUDE.md, docs/development/, docs/api/, src/*/README.md |
| **系统自动** | 代码生成文档、接口文档 | OpenAPI规范、代码注释文档 |

### 更新触发机制

强制更新：
1. 架构变更 → 更新 architecture/README.md 与根 README.md
2. API 变更 → 更新 api/README.md 与 Swagger
3. 模块重构 → 更新相关 src/*/README.md
4. 任务完成 → 在 tasks/completed/ 创建总结

**定期检查机制**：
- 每周检查文档时效性（通过Git时间戳）
- 每月检查交叉引用有效性
- 每季度检查文档架构完整性

## 🛠 工具和自动化

### 文档白名单（其余统一在 Issues 管理）
- 架构与决策：architecture/README.md、overview.md、ADR-*.md
- 规范与指南：development/README.md、documentation-guidelines.md、styleguide.md、testing/README.md
- 安全与技术：security/*、technical/*
- 任务与交付：tasks/README.md、tasks/INDEX.md、tasks/plan/*（活跃任务）、tasks/completed/*（完成总结）

说明：PRD、阶段性总结/计划、临时分析等不再保存在 docs 中，统一由 GitHub Issues 跟踪与沉淀。

### 文档验证脚本（规划中）
```powershell
# 文档系统健康检查
.\scripts\DocumentationHealthCheck.ps1
- 检查README文件完整性
- 验证交叉引用链接有效性  
- 识别过期文档（>30天未更新）
- 生成文档覆盖率报告
```

### 导航生成规则
- 自动生成 docs/index.md 目录结构
- 自动更新各级 README 的关联链接
- 自动检测断链与循环引用

## ⚡ 实施计划

### Phase 1：架构建立（1-2天）
1. ✅ 创建本文档系统规范
2. 🔄 创建统一的开发者指导文档
3. 🔄 更新docs/index.md为完整导航
4. 🔄 标准化现有L2级README

### Phase 2：内容完善（2-3天）  
1. 更新所有L3级README符合标准
2. 建立文档间交叉引用
3. 创建自动化验证脚本

### Phase 3：流程优化（1-2天）
1. 建立文档更新检查机制
2. 集成到开发工作流程
3. 培训和推广使用

## 📊 成功指标

- **完整性**：所有必需README文件100%符合标准
- **时效性**：文档平均更新滞后<7天
- **可用性**：开发者能在<2分钟内找到所需信息
- **一致性**：文档格式和内容标准执行率>95%

## 🔗 快速导航

- [返回文档首页](index.md)
- [开发者指导](DEVELOPER_GUIDE.md)  
- [文档模板](templates/)
- [维护脚本](../scripts/docs/)

---
*本文档将随着系统演进持续更新，确保文档架构与项目发展保持同步。*
