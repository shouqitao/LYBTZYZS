# How-to Guides（操作指南）总览

> **文档类型**：How-to Guides（任务导向 + 实践导向）
> **适用场景**：解决特定开发问题、完成具体任务
> **目标读者**：有一定基础知识的实践者

**版本**：v6.0 Diátaxis框架版
**更新时间**：2025-10-29
**维护团队**：开发组

---

## 🎯 什么是 How-to Guides？

How-to Guides 是**任务导向的操作指南**，提供完成特定任务的步骤说明。与Tutorial（学习导向）不同，How-to Guides假设你已有基础知识，专注于解决具体问题。

### 📚 与其他文档类型的区别

| 对比项 | How-to Guides | Tutorial | Reference | Explanation |
|-------|--------------|----------|-----------|-------------|
| **目标** | 解决问题 | 学习 | 查阅信息 | 理解概念 |
| **受众** | 实践者 | 新手 | 所有人 | 架构师 |
| **场景** | 完成特定任务 | 第一次接触 | 查找API/配置 | 深入理解设计 |
| **特点** | 步骤清晰 | 手把手引导 | 精确简洁 | 深入解释 |

**何时使用 How-to Guides？**
- ✅ 你已经了解系统基本概念（如果不了解，先看[Tutorial](../tutorials/README.md)）
- ✅ 你需要完成具体开发任务（如"添加新模块"、"配置依赖注入"）
- ✅ 你需要快速查找解决方案（不需要完整的学习过程）

---

## 🏗️ 开发架构对齐

凌隐宝堂中医诊所管理系统采用**三层对齐架构**，开发指南严格按照Server/Client/Shared三层结构组织，确保开发规范与代码架构完全一致。

### 📋 开发指南结构

| 层级 | 开发指南 | 主要内容 | 目标用户 |
|------|----------|----------|----------|
| **Level 1** | **[How-to总览](README.md)** | 开发规范、流程、标准指引 | 全体开发者 |
| **Level 2** | **[Server端操作](server/README.md)** | 后端开发、API开发、数据库操作 | 后端开发者 |
| **Level 3** | **[Client端操作](client/README.md)** | WPF开发、UI设计、客户端逻辑 | 前端开发者 |
| **Level 4** | **[共享操作](shared/README.md)** | 跨层开发、通用组件、接口定义 | 架构师、全栈开发者 |

---

## 💻 开发栈速查

### Server端开发栈
```
Server端开发 (LYBT.Server)
├── 开发语言：C# (.NET 8)
├── 架构模式：三层架构 (Controller + Service + Repository)
├── 数据库：SQL Server 2019+
├── ORM：Entity Framework Core
├── API文档：Swagger/OpenAPI
└── 测试框架：NUnit + Moq
```

### Client端开发栈
```
Client端开发 (LYBT.Desktop)
├── 开发语言：C# (.NET 8)
├── UI框架：WPF
├── 架构模式：MVVM (五层架构)
├── 依赖注入：Microsoft.Extensions.DependencyInjection
├── 组件库：Material Design
└── 测试框架：NUnit + Moq
```

### 共享开发栈
```
共享开发 (LYBT.Shared)
├── 开发语言：C# (.NET 8)
├── 核心组件：Models + Interfaces + Infrastructure
├── 验证框架：FluentValidation
├── 对象映射：AutoMapper
└── 单元测试：NUnit + NSubstitute
```

---

## 📂 分层导航

### 🖥️ Server端操作指南

**入口**：[Server端开发指南](server/README.md)

**常用指南**：
- [模块开发指南](server/module-development.md) - 如何开发新模块
- [依赖注入配置](server/dependency-injection.md) - 如何配置DI容器
- [Repository模式实践](server/repository-pattern.md) - 如何实现数据访问
- [Service层开发](server/service-layer.md) - 如何编写业务逻辑
- [API端点设计](server/api-design.md) - 如何设计RESTful API
- [数据库迁移](server/database-migration.md) - 如何管理EF Core迁移

### 🖥️ Client端操作指南

**入口**：[Client端开发指南](client/README.md)

**常用指南**：
- [MVVM开发指南](client/mvvm-guide.md) - 如何实现MVVM模式
- [Prism导航配置](client/prism-navigation.md) - 如何配置区域导航
- [数据绑定实践](client/data-binding.md) - 如何绑定ViewModel和View
- [命令绑定](client/command-binding.md) - 如何实现命令模式
- [依赖注入](client/dependency-injection.md) - 如何配置Prism DI
- [UI组件库](client/ui-components.md) - 如何使用Material Design组件

### 🔗 共享操作指南

**入口**：[共享开发指南](shared/README.md)

**常用指南**：
- [Git工作流](shared/git-workflow.md) - 如何使用Git进行版本控制
- [代码审查](shared/code-review-guide.md) - 如何进行代码审查
- [测试编写指南](shared/testing-guide.md) - 如何编写单元测试
- [CI/CD配置](shared/ci-cd-setup.md) - 如何配置持续集成
- [文档编写](shared/documentation-guide.md) - 如何编写技术文档
- [问题排查](shared/troubleshooting-guide.md) - 如何排查常见问题

---

## 🚀 快速开始任务

### 我想...（常见任务直达）

#### 后端开发任务
- **添加新模块** → [模块开发指南](server/module-development.md)
- **创建新API** → [API端点设计](server/api-design.md)
- **修改数据库** → [数据库迁移](server/database-migration.md)
- **实现业务逻辑** → [Service层开发](server/service-layer.md)

#### 前端开发任务
- **创建新页面** → [MVVM开发指南](client/mvvm-guide.md)
- **添加导航** → [Prism导航配置](client/prism-navigation.md)
- **绑定数据** → [数据绑定实践](client/data-binding.md)
- **实现命令** → [命令绑定](client/command-binding.md)

#### 通用开发任务
- **提交代码** → [Git工作流](shared/git-workflow.md)
- **编写测试** → [测试编写指南](shared/testing-guide.md)
- **遇到问题** → [问题排查](shared/troubleshooting-guide.md)
- **代码审查** → [代码审查指南](shared/code-review-guide.md)

---

## 📚 相关文档

### 学习系统（新手）
如果你是第一次接触系统，推荐先学习：
- [Tutorial总览](../tutorials/README.md) - 学习导向的引导式教程
- [5分钟快速开始](../tutorials/quick-start.md) - 快速启动系统
- [开发第一个功能](../tutorials/first-feature.md) - 完整开发流程演示

### 查阅技术细节
需要查找API、配置、命令等技术细节，请查阅：
- [Reference总览](../reference/README.md) - 信息导向的参考手册
- [API参考](../reference/quick-reference/api-reference.md) - 所有API端点
- [代码模式](../reference/quick-reference/code-patterns.md) - 常用代码模式
- [快速参考](../reference/quick-reference/) - 常用命令、配置速查

### 理解架构设计
需要深入理解系统架构和设计决策，请阅读：
- [Explanation总览](../explanation/README.md) - 理解导向的概念解释
- [Server端架构](../explanation/architecture/server/README.md) - 三层架构设计
- [Client端架构](../explanation/architecture/client/README.md) - MVVM架构设计
- [架构决策记录](../explanation/architecture/decisions/) - ADR记录
- [业务规则](../explanation/business-rules.md) - 14条核心业务规则

---

## 🎯 使用建议

### 如何使用 How-to Guides

1. **明确任务目标**
   确定你要解决的具体问题（例如："如何添加新的API端点？"）

2. **查找对应指南**
   使用上方的"我想..."快速导航，或浏览分层导航

3. **按步骤操作**
   跟随指南中的步骤完成任务，注意前置条件和验证方法

4. **遇到问题**
   查阅[问题排查指南](shared/troubleshooting-guide.md)或[Reference](../reference/quick-reference/troubleshooting.md)

5. **深入理解**
   如需理解背后的设计原理，查阅[Explanation](../explanation/README.md)

### How-to Guides 的局限性

How-to Guides **不适合**以下场景：
- ❌ 完全新手学习系统 → 请查阅[Tutorial](../tutorials/README.md)
- ❌ 查找API接口定义 → 请查阅[Reference](../reference/README.md)
- ❌ 理解架构设计原理 → 请查阅[Explanation](../explanation/README.md)

---

## 🔄 文档维护

### 贡献指南

欢迎贡献新的How-to Guide！优秀的操作指南应该：
- ✅ **任务明确**：标题明确指出要完成什么任务
- ✅ **步骤清晰**：每步都有明确的操作指令
- ✅ **可验证**：提供验证方法确认任务完成
- ✅ **前置条件**：说明需要的基础知识和准备工作
- ✅ **故障排查**：列出常见问题和解决方法

### 文档更新记录

- **v6.0 (2025-10-29)**: Diátaxis框架重构，改为How-to Guides定位
- **v5.0 (2025-10-15)**: 三层对齐架构重组
- **v4.0 (2025-09-20)**: 新增跨端开发指南
- **v3.0 (2025-08-10)**: 完善Client端MVVM指南

---

**最后更新**：2025-10-29
**文档版本**：v6.0（Diátaxis框架重构版）
