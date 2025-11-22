# LYBTZYZS 中医诊所管理系统文档

基于 **Diátaxis框架** 的系统化技术文档，为不同用户群体提供精准的文档支持。

## 🎯 Diátaxis框架介绍

Diátaxis框架将文档分为四种类型，每种都回答特定的用户需求：

```
                              认知 (Cognition)
    ┌─────────────────────────────────────────────────────────┐
    │  📚 Explanation (说明)    │  📖 Reference (参考)        │
    │  理解导向                  │  信息导向                    │
    │  深入理解系统原理          │  准确的技术信息              │
    │  背景、设计思路、架构原理  │  API文档、配置说明、技术规范  │
    │  适用：架构师、学习型开发者  │  适用：有经验的开发者         │
    ├─────────────────────────────────────────────────────────┤
    │  🎓 Tutorials (教程)       │  🛠️ How-to Guides (操作指南) │
    │  学习导向                  │  目标导向                    │
    │  手把手入门指导            │  解决具体问题                │
    │  端到端学习体验            │  实用操作步骤                │
    │  适用：新手、业务用户      │  适用：实践型开发者           │
    └─────────────────────────────────────────────────────────┘
                                   行动 (Action)
```

## 📁 文档导航

### 🎓 [Tutorials (教程)](tutorials/) - 新手上路
适合零基础用户，提供手把手的学习指导：

- **[📖 快速开始](tutorials/quick-start/)** - 5分钟运行系统
- **[🚀 入门指南](tutorials/getting-started/)** - 开发环境搭建
- **[📚 模块教程](tutorials/module-specific/)** - 各模块详细学习
- **[🏥 业务领域教程](tutorials/business-domain/)** - 中医诊所业务知识

### 🛠️ [How-to Guides (操作指南)](how-to-guides/) - 解决问题
适合有一定基础的用户，解决具体问题：

- **[💻 开发指南](how-to-guides/development/)** - 功能开发指导
- **[🚀 部署指南](how-to-guides/deployment/)** - 系统部署运维
- **[🏥 业务流程](how-to-guides/business-workflows/)** - 中医诊疗流程
- **[🔧 故障排查](how-to-guides/troubleshooting/)** - 问题诊断解决

### 📖 [Reference (参考)](reference/) - 技术信息
适合有经验的开发者，提供准确的技术参考：

- **[🔌 API文档](reference/api/)** - 完整的接口说明
- **[⚙️ 配置参考](reference/configuration/)** - 系统配置指南
- **[📋 业务规则](reference/business-rules/)** - 业务逻辑规则
- **[📐 技术规范](reference/technical-specs/)** - 技术标准和规范

### 📚 [Explanation (说明)](explanation/) - 深入理解
适合希望深入理解系统的学习者：

- **[🏗️ 系统架构](explanation/architecture/)** - 架构设计和原理
- **[🏥 业务领域](explanation/business-domain/)** - 中医诊所业务知识
- **[💡 设计决策](explanation/design-decisions/)** - 技术选型和决策
- **[📖 背景知识](explanation/background/)** - 相关背景和历史

## 👥 用户群体指南

### 🆕 新手开发者
**推荐路径**：Tutorials → How-to Guides → Reference
1. 先从 [快速开始](tutorials/quick-start/) 体验系统
2. 学习 [开发环境搭建](tutorials/getting-started/)
3. 参考 [API文档](reference/api/) 进行开发

### 💼 有经验开发者
**推荐路径**：Reference → How-to Guides → Explanation
1. 直接查询 [API文档](reference/api/) 和 [配置参考](reference/configuration/)
2. 遇到问题时查看 [开发指南](how-to-guides/development/)
3. 需要深入理解时学习 [系统架构](explanation/architecture/)

### 🏗️ 架构师
**推荐路径**：Explanation → Reference → Design Decisions
1. 深入理解 [系统架构](explanation/architecture/) 和 [设计决策](explanation/design-decisions/)
2. 参考 [技术规范](reference/technical-specs/) 和 [业务规则](reference/business-rules/)
3. 了解 [背景知识](explanation/background/)

### 🔧 运维人员
**推荐路径**：How-to Guides → Reference
1. 学习 [部署指南](how-to-guides/deployment/) 和 [故障排查](how-to-guides/troubleshooting/)
2. 参考 [配置指南](reference/configuration/) 进行系统配置

### 🏥 产品经理/业务用户
**推荐路径**：Tutorials → Explanation → Business Domain
1. 通过 [业务领域教程](tutorials/business-domain/) 了解系统
2. 学习 [业务流程](how-to-guides/business-workflows/)
3. 理解 [业务规则](reference/business-rules/)

### 🧪 测试人员
**推荐路径**：How-to Guides → Reference → Business Rules
1. 参考 [业务流程](how-to-guides/business-workflows/) 和 [业务规则](reference/business-rules/)
2. 使用 [配置参考](reference/configuration/) 搭建测试环境

## 🏥 LYBTZYZS 系统概览

### 技术架构
- **前端**: WPF (.NET 8) + Prism.DryIoc + Refit
- **后端**: ASP.NET Core Web API + Entity Framework Core 8.0
- **数据库**: SQL Server
- **架构模式**: MVVM + Repository + Service

### 业务模块
- **Auth** - 身份认证与授权
- **Users** - 用户管理 (医生/管理员)
- **Patients** - 患者档案管理
- **MedicalCase** - 病历管理
- **Consultation** - 中医诊断 (望闻问切)
- **Prescriptions** - 处方管理
- **Herbs** - 中药管理
- **Formula** - 方剂管理

## 🔍 快速查找

### 按需求查找
- **我想快速体验系统** → [快速开始](tutorials/quick-start/)
- **我需要开发新功能** → [开发指南](how-to-guides/development/)
- **我需要API文档** → [API文档](reference/api/)
- **我需要理解系统架构** → [系统架构](explanation/architecture/)
- **我遇到了问题** → [故障排查](how-to-guides/troubleshooting/)

### 按模块查找
- **Auth模块** → [认证教程](tutorials/module-specific/auth.md) + [API参考](reference/api/auth.md)
- **Users模块** → [用户管理教程](tutorials/module-specific/users.md) + [API参考](reference/api/users.md)
- **Patients模块** → [患者管理教程](tutorials/module-specific/patients.md) + [API参考](reference/api/patients.md)
- **MedicalCase模块** → [病历管理教程](tutorials/module-specific/medicalcase.md) + [API参考](reference/api/medicalcase.md)
- **Consultation模块** → [中医诊断教程](tutorials/module-specific/consultation.md) + [API参考](reference/api/consultation.md)
- **Prescriptions模块** → [处方管理教程](tutorials/module-specific/prescriptions.md) + [API参考](reference/api/prescriptions.md)
- **Herbs模块** → [中药管理教程](tutorials/module-specific/herbs.md) + [API参考](reference/api/herbs.md)
- **Formula模块** → [方剂管理教程](tutorials/module-specific/formula.md) + [API参考](reference/api/formula.md)

## 📞 获取帮助

### 文档问题
- 发现文档错误 → [提交Issue](https://github.com/shouqitao/LYBTZYZS/issues)
- 文档改进建议 → [贡献指南](meta/contributing/)

### 技术支持
- 开发问题 → GitHub Issues
- 业务咨询 → 项目负责人
- 紧急问题 → 技术支持团队

## 📝 文档维护

本文档系统基于Diátaxis框架构建，由架构组负责维护。

- **维护标准**: [文档维护规范](meta/maintenance/)
- **贡献指南**: [贡献者指南](meta/contributing/)
- **更新频率**: 随版本同步更新
- **质量保证**: 定期审查和用户反馈

---

**项目**: 凌隐宝堂中医诊所管理系统 (LYBTZYZS)
**文档框架**: Diátaxis v1.0
**技术栈**: .NET 8, WPF, ASP.NET Core, EF Core, SQL Server
**最后更新**: 2025-11-22