# Reference（参考手册）总览

> **文档类型**：Reference（信息导向 + 查阅导向）
> **适用场景**：快速查找API、配置、命令等技术细节
> **目标读者**：所有开发者

**版本**：v6.0 Diátaxis框架版
**更新时间**：2025-10-29
**维护团队**：开发组

---

## 🎯 什么是 Reference？

Reference 是**信息导向的参考手册**，提供精确、结构化的技术信息。Reference的核心特点是：
- ✅ **精确简洁**：只提供事实信息，不解释原理
- ✅ **结构化**：信息按逻辑分类组织，易于查找
- ✅ **完整性**：覆盖所有API、配置、命令等技术细节
- ✅ **最新性**：与代码库实时同步

### 📚 与其他文档类型的区别

| 对比项 | Reference | Tutorial | How-to Guides | Explanation |
|-------|-----------|----------|---------------|-------------|
| **目标** | 查阅信息 | 学习 | 解决问题 | 理解概念 |
| **受众** | 所有人 | 新手 | 实践者 | 架构师 |
| **场景** | 查找API/配置 | 第一次接触 | 完成特定任务 | 深入理解设计 |
| **特点** | 精确简洁 | 手把手引导 | 步骤清晰 | 深入解释 |

**何时使用 Reference？**
- ✅ 你需要查找特定API端点的参数和返回值
- ✅ 你需要快速查阅配置选项的含义
- ✅ 你需要验证某个命令的语法和选项
- ✅ 你需要查找某个模块的完整接口定义

---

## 📂 Reference 分类

### 📖 快速参考（Quick Reference）

**入口**：[快速参考目录](quick-reference/)

**适用场景**：80%的日常查阅需求

**包含内容**：
- **[API参考](quick-reference/api-reference.md)** - 所有API端点速查表
  - RESTful API端点
  - 请求/响应格式
  - 状态码说明
  - 认证方式

- **[代码模式](quick-reference/code-patterns.md)** - 常用代码模式速查
  - Repository模式示例
  - MVVM模式示例
  - 依赖注入配置示例
  - 数据绑定模式

- **[问题排查](quick-reference/troubleshooting.md)** - 常见问题解决速查
  - 编译错误
  - 运行时错误
  - 配置问题
  - 环境问题

- **[开发清单](quick-reference/development-checklist.md)** - 开发流程检查清单
  - 代码提交前检查
  - 功能完成检查
  - PR创建检查
  - 发布检查

### 🔌 API文档（API Documentation）

**入口**：[API文档目录](api/)

**适用场景**：查找详细的API接口定义

**包含内容**：
- 各模块的API端点完整定义
- 请求/响应DTO结构
- 错误码和异常处理
- API版本和兼容性说明

**常用API文档**：
- [Auth API](api/auth-api.md) - 认证和授权API
- [Patients API](api/patients-api.md) - 患者管理API
- [MedicalCase API](api/medicalcase-api.md) - 病案管理API
- [Consultation API](api/consultation-api.md) - 诊断API
- [Prescription API](api/prescription-api.md) - 处方API
- [Herbs API](api/herbs-api.md) - 药品管理API
- [Formula API](api/formula-api.md) - 方剂管理API

### 📦 模块文档（Module Documentation）

**入口**：[模块文档目录](modules/)

**适用场景**：查找特定模块的详细文档

**包含内容**：
- 模块职责和边界
- 对外接口定义
- 模块依赖关系
- 配置选项说明

**Server端模块**：
- [Auth模块](modules/auth/) - 认证和授权
- [Patients模块](modules/patients/) - 患者管理
- [MedicalCase模块](modules/medicalcase/) - 病案管理
- [Consultation模块](modules/consultation/) - 诊断
- [Prescription模块](modules/prescription/) - 处方
- [Herbs模块](modules/herbs/) - 药品管理
- [Formula模块](modules/formula/) - 方剂管理
- [Users模块](modules/users/) - 用户管理

**Client端模块**：
- [Desktop.Auth](modules/desktop-auth/) - 客户端认证
- [Desktop.Patients](modules/desktop-patients/) - 患者管理UI
- [Desktop.MedicalCase](modules/desktop-medicalcase/) - 病案UI
- [Desktop.Consultation](modules/desktop-consultation/) - 诊断UI
- [Desktop.Prescriptions](modules/desktop-prescriptions/) - 处方UI
- [Desktop.Herbs](modules/desktop-herbs/) - 药品UI
- [Desktop.Formula](modules/desktop-formula/) - 方剂UI

---

## 🚀 快速查找

### 我需要查找...（常见查阅任务）

#### API 相关
- **某个API端点的参数** → [API参考](quick-reference/api-reference.md)
- **API的完整定义** → [API文档](api/)
- **API错误码含义** → [API文档](api/) 对应模块

#### 代码相关
- **Repository模式怎么写** → [代码模式](quick-reference/code-patterns.md)
- **MVVM模式示例** → [代码模式](quick-reference/code-patterns.md)
- **依赖注入配置** → [代码模式](quick-reference/code-patterns.md)

#### 配置相关
- **数据库连接字符串** → [开发清单](quick-reference/development-checklist.md)
- **环境变量配置** → [开发清单](quick-reference/development-checklist.md)
- **模块配置选项** → [模块文档](modules/) 对应模块

#### 问题排查
- **编译错误解决** → [问题排查](quick-reference/troubleshooting.md)
- **运行时错误** → [问题排查](quick-reference/troubleshooting.md)
- **环境配置问题** → [问题排查](quick-reference/troubleshooting.md)

---

## 📚 相关文档

### 学习系统（新手）
如果你是第一次接触系统，推荐先学习：
- [Tutorial总览](../tutorials/README.md) - 学习导向的引导式教程
- [5分钟快速开始](../tutorials/quick-start.md) - 快速启动系统
- [开发第一个功能](../tutorials/first-feature.md) - 完整开发流程演示

### 解决具体问题
需要完成特定开发任务，请查阅：
- [How-to Guides总览](../how-to-guides/README.md) - 任务导向的操作指南
- [Server端操作](../how-to-guides/server/README.md) - 后端开发指南
- [Client端操作](../how-to-guides/client/README.md) - 前端开发指南
- [共享操作](../how-to-guides/shared/README.md) - 通用开发指南

### 理解架构设计
需要深入理解系统架构和设计决策，请阅读：
- [Explanation总览](../explanation/README.md) - 理解导向的概念解释
- [Server端架构](../explanation/architecture/server/README.md) - 三层架构设计
- [Client端架构](../explanation/architecture/client/README.md) - MVVM架构设计
- [架构决策记录](../explanation/architecture/decisions/) - ADR记录
- [业务规则](../explanation/business-rules.md) - 14条核心业务规则

---

## 🎯 使用建议

### 如何使用 Reference

1. **明确查找目标**
   确定你要查找的具体信息（例如："GetPatientByIdAsync API的返回值类型是什么？"）

2. **选择对应分类**
   - API信息 → [API参考](quick-reference/api-reference.md) 或 [API文档](api/)
   - 代码模式 → [代码模式](quick-reference/code-patterns.md)
   - 问题排查 → [问题排查](quick-reference/troubleshooting.md)
   - 模块信息 → [模块文档](modules/)

3. **快速定位**
   使用浏览器的Ctrl+F搜索功能快速定位关键词

4. **需要更多说明**
   如果Reference信息不足，查阅[Explanation](../explanation/README.md)了解背景知识

### Reference 的局限性

Reference **不适合**以下场景：
- ❌ 学习如何使用系统 → 请查阅[Tutorial](../tutorials/README.md)
- ❌ 解决具体开发问题 → 请查阅[How-to Guides](../how-to-guides/README.md)
- ❌ 理解设计原理 → 请查阅[Explanation](../explanation/README.md)

Reference **只提供**技术信息，**不解释**：
- ❌ 为什么这样设计
- ❌ 如何一步步实现
- ❌ 遇到问题如何排查（除troubleshooting外）

---

## 🔄 文档维护

### 贡献指南

欢迎贡献新的Reference内容！优秀的参考手册应该：
- ✅ **精确性**：信息准确无误，与代码库同步
- ✅ **完整性**：覆盖所有公开接口和配置
- ✅ **结构化**：按逻辑分类，易于查找
- ✅ **简洁性**：只提供必要信息，不解释原理
- ✅ **示例**：提供简短的代码示例（如需详细示例，放在How-to Guides）

### 更新频率

- **API文档**：随代码变更同步更新（强制）
- **代码模式**：发现新模式时补充（可选）
- **问题排查**：发现新问题时补充（可选）
- **模块文档**：模块结构变化时更新（强制）

### 文档更新记录

- **v6.0 (2025-10-29)**: Diátaxis框架重构，新建Reference分类
- **v5.0 (2025-10-15)**: 三层对齐架构重组
- **v4.0 (2025-09-20)**: 完善API文档
- **v3.0 (2025-08-10)**: 新增快速参考

---

**最后更新**：2025-10-29
**文档版本**：v6.0（Diátaxis框架重构版）
