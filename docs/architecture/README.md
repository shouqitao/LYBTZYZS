# 架构总览

**🏗️ 对齐架构设计原理与导航** - Server/Client/Shared三层架构完全对应代码结构

## 🎯 架构设计理念

凌隐宝堂中医诊所管理系统采用**三层对齐架构**，严格对应Server/Client/Shared三层代码架构，确保架构设计与实现完全一致。本文档提供系统架构的整体概览和导航指南。

## 🏗️ 三层对齐架构

### 架构层次对应关系

| 架构层 | 代码层 | 职责说明 | 主要组件 |
|--------|--------|----------|----------|
| **Server端架构** | Services + Core + Modules + Infrastructure | 业务逻辑处理、数据访问、API服务 | Controllers、Services、Repositories |
| **Client端架构** | Shell + Core + Modules + Workstations | 用户界面、交互逻辑、模块化组件 | Views、ViewModels、Services |
| **共享架构** | Core + Shared + Models + Interfaces | 跨端共享、数据模型、基础设施 | DTOs、Entities、Interfaces |

### 架构设计原则

#### ✅ 核心原则
1. **对齐一致性**：架构设计与代码实现完全对齐
2. **分离关注点**：每层专注自己的职责
3. **依赖倒置**：高层模块不依赖低层模块
4. **接口隔离**：通过接口定义层间契约
5. **单一职责**：每个组件只负责一个功能领域

#### 🔄 数据流向
```
Client UI → API Controller → Application Service → Repository → Database
    ↑            ↑                  ↑              ↑
  WPF Views    REST API         Business Logic    Data Access   SQL Server
```

## 📋 架构文档导航

### 🏛️ 核心架构文档

#### Server端架构
- **[Server端架构指南](server/README.md)** ⭐ ✅ 已验证（2025-10-28更新）
  - 三层架构设计模式（Presentation → Application → Infrastructure）
  - 8个业务模块详细设计
  - MVP阶段服务标准（直接实现接口，无BaseService<T>）
  - 两层Controller设计（BaseControllerCore → BaseApiController）
  - Repository可见性约束（internal实现类，Epic #1600）
  - **包含3个实际代码示例**：PatientService、PatientsController、PatientRepository

- **[Client端架构指南](client/README.md)** ⭐
  - MVVM五层架构设计
  - WPF客户端实现规范
  - UI组件和用户界面标准
  - 客户端模块化设计

- **[共享架构指南](shared/README.md)** ⭐
  - 跨端组件设计原则
  - 认证系统双轨实现
  - 技术决策和架构模式
  - 共享基础设施

#### 模块化设计
- **[模块化设计指南](module-design-guide.md)**
  - 8个业务模块架构设计
  - 模块间通信机制
  - 模块注册和配置
  - 模块测试策略

#### 数据库设计
- **[数据库设计指南](database-design-guide.md)**
  - 11个核心实体设计
  - 关系模型和数据结构
  - 数据迁移和版本控制
  - 数据安全和备份策略

#### 安全架构
- **[安全架构指南](security-architecture-guide.md)**
  - 双轨认证系统设计
  - JWT令牌管理
  - 数据加密和权限控制
  - 安全审计和合规要求

### 🛠️ 开发指南文档

#### 开发总览
- **[开发指南总览](../development/README.md)**
  - 开发规范和流程指导
  - 代码质量标准
  - 团队协作规范
  - 项目结构和配置

#### Server端开发
- **[Server端开发](../development/server/README.md)**
  - Server开发规范和实践
  - API接口开发指南
  - 服务层实现模式
  - 数据访问层设计

#### Client端开发
- **[Client端开发](../development/client/README.md)**
  - WPF客户端开发指南
  - MVVM模式实现
  - 用户界面设计规范
  - 客户端性能优化

#### 共享开发
- **[共享开发](../development/shared/README.md)**
  - 跨端组件开发指南
  - 共享模块设计
  - 测试策略和工具
  - 文档维护规范

## 🎯 架构导航

### 🛠️ 开发者导航
1. **快速开始**：[Server端架构](server/README.md) → [Client端架构](client/README.md)
2. **技术选型**：[共享架构](shared/README.md)
3. **模块开发**：[模块化设计](module-design-guide.md)
4. **数据库设计**：[数据库设计](database-design-guide.md)

### 🏗️ 架构师导航
1. **架构概览**：本页面
2. **技术决策**：[技术决策记录](shared/adr/)
3. **设计标准**：Server + Client + Shared架构标准
4. **架构验证**：[架构测试指南](../development/shared/architecture-testing-guide.md)

### 📊 项目经理导航
1. **架构概览**：本页面
2. **技术规范**：各层级架构标准
3. **开发流程**：[开发指南总览](../development/README.md)
4. **质量保证**：[测试指南](../development/shared/testing-guide.md)

### 🔧 测试工程师导航
1. **架构测试**：[架构测试指南](../development/shared/architecture-testing-guide.md)
2. **API测试**：[API设计最佳实践](../deep/api-design-best-practices.md)
3. **性能测试**：[性能优化指南](../deep/performance-optimization.md)
4. **安全测试**：[安全架构指南](security-architecture-guide.md)

## 🔧 架构工具和流程

### 🛠️ 架构工具链
- **代码分析工具**：SonarQube、Resharper
- **架构图工具**：PlantUML、Draw.io
- **文档生成工具**：Swagger、AutoMapper
- **测试工具**：xUnit、NUnit、Moq

### 📋 架构流程
1. **架构设计**：[技术决策记录](shared/adr/)
2. **代码实现**：各层级开发指南
3. **架构验证**：架构测试和代码审查
4. **文档维护**：架构文档同步更新

## 📊 架构度量指标

### 📈 质量指标
- **架构一致性**：设计与实现100%对齐
- **模块化程度**：8个业务模块独立可测试
- **接口标准化**：统一的API接口规范
- **文档覆盖率**：100%架构文档覆盖

### 🎯 成功指标
- ✅ **零架构偏移**：所有实现严格遵循架构设计
- ✅ **模块独立**：每个模块可独立开发和测试
- ✅ **接口清晰**：层间接口定义明确无歧义
- ✅ **文档同步**：架构文档与代码实时同步

## 🔗 相关资源

### 📚 深度参考
- [深度参考文档](../deep/README.md) - 完整技术细节
- [API设计最佳实践](../deep/api-design-best-practices.md) - API架构规范
- [性能优化指南](../deep/performance-optimization.md) - 性能架构优化
- [测试策略指南](../deep/testing-strategies.md) - 架构测试策略

### 🛠️ 开发资源
- [GitHub仓库](https://github.com/shouqitao/凌隐宝堂中医诊所) - 完整源代码
- [开发工具配置](../development/README.md) - 开发环境设置
- [代码质量检查](../support/documentation-metrics.md) - 质量监控机制

### 📞 支持渠道
- [架构讨论](https://github.com/shouqitao/凌隐宝堂中医诊所/discussions) - 架构讨论
- [问题反馈](https://github.com/shouqitao/凌隐宝堂中医诊所/issues) - 问题和建议
- [文档反馈](../support/documentation-metrics.md) - 文档改进

---

**架构总览** - 为凌隐宝堂中医诊所提供清晰、一致、可维护的架构设计指导 🏗️

*本架构总览基于实际代码架构编写，确保架构设计与实现完全对齐。如有架构问题或建议，请通过相应渠道反馈。*