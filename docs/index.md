# 凌隐宝堂中医诊所文档中心

**文档版本**：v5.0 彻底重构版
**创建时间**：2025-10-15
**最后更新**：2025-10-15
**维护负责**：项目团队

## 🎯 文档体系架构

项目采用**三层对齐架构**，严格对应Server/Client/Shared三层代码架构：

| 层级 | 用途 | 内容 | 对应代码层 |
|------|------|------|------------|
| **Level 1** | 快速参考 | 常用API、配置模板、代码模式 | - |
| **Level 2** | 架构指南 | Server/Client/Shared架构对齐 | Core/Modules |
| **Level 3** | 深度参考 | 完整API、模块详细文档 | Services/API |

## 🚀 快速参考中心 (Level 1)

**解决80%日常需求** - 精简文档，快速查找

| 文档 | 用途 | 快速访问 |
|------|------|----------|
| **[API快速参考](quick-reference/api-reference.md)** | 最常用API和调用示例 | 查接口 |
| **[配置模板](quick-reference/config-templates.md)** | 常用配置文件模板 | 找配置 |
| **[代码模式](quick-reference/code-patterns.md)** | 常用代码模式和模板 | 学模式 |
| **[问题解决](quick-reference/troubleshooting.md)** | 常见问题和解决方案 | 解问题 |
| **[开发清单](quick-reference/development-checklist.md)** | 开发流程和质量检查 | 做检查 |

## 🏗️ 架构指南 (Level 2)

**Server/Client/Shared三层对齐架构** - 严格对应代码结构

### 核心架构文档
- **[架构总览](architecture/README.md)** - 对齐架构设计原理与导航 ⭐ 核心入口
- **[Server端架构](architecture/server/README.md)** - 三层架构、8个模块、服务标准 ⭐
- **[Client端架构](architecture/client/README.md)** - MVVM架构、5层设计、UI标准 ⭐
  - **[Shell层架构设计](architecture/client/shell-layer-design.md)** - Shell层职责边界、组件结构、交互模式
- **[共享架构](architecture/shared/README.md)** - 跨端组件、认证系统、技术决策 ⭐

### 业务架构文档
- **[看诊流程实体关系](architecture/shared/clinical-workflow-entity-relationships.md)** - 挂号/医案/诊断/处方实体关系与状态机设计 ⭐⭐⭐ **权威文档**
- **[业务规则文档](business-rules.md)** - 14条核心业务规则（数据约束/业务流程/聚合根/计算规则/访问控制）⭐⭐⭐
- **[医案/诊断/处方增强设计](design/medicalcase-consultation-prescription-enhancement-design.md)** - 三步工作流优化、处方管理增强、其他病案查询功能详细设计 ⭐
- **[医案/诊断/处方差距分析](design/medicalcase-consultation-prescription-gap-analysis.md)** - 现有代码与设计的差距、修改计划、工作量估算 ⭐⭐

### 开发指南文档
- **[开发指南总览](development/README.md)** - 开发规范和流程指导
- **[Server端开发](development/server/README.md)** - Server开发规范和实践
- **[Client端开发](development/client/README.md)** - WPF客户端开发指南
- **[共享开发](development/shared/README.md)** - 跨端组件开发指南

## 📚 深度参考 (Level 3)

**完整技术文档** - 详细参考信息（5%深度需求）

### 🎯 核心深度文档
- **[高级设计模式](deep/advanced-patterns.md)** - 7种设计模式在中医诊所系统中的实际应用
- **[性能优化指南](deep/performance-optimization.md)** - 数据库、内存、并发、前端全方位性能优化
- **[测试策略指南](deep/testing-strategies.md)** - 单元测试、集成测试、UI测试、性能测试完整方案
- **[部署指南](deep/deployment-guide.md)** - 从开发到生产的完整部署流程
- **[API设计最佳实践](deep/api-design-best-practices.md)** - RESTful设计、认证授权、版本控制、安全策略

### API文档
- **[API总览](api/README.md)** - 12个控制器完整API文档
- **[认证API](api/auth/)** - 双轨认证、JWT验证、超级管理员隔离
- **[业务模块API](api/modules/)** - 8个业务模块详细API文档

### 模块文档
- **[模块总览](modules/README.md)** - 8个业务模块完整说明
- **[认证模块](modules/auth/)** - 双轨认证系统完整实现
- **[患者模块](modules/patients/)** - 患者管理、Excel导入、查询统计
- **[医案模块](modules/medical-case/)** - 医案状态管理、业务流程
- **[诊疗模块](modules/consultation/)** - 四诊合参、辨证论治、诊断记录
- **[处方模块](modules/prescriptions/)** - 四种录入方式、药材配伍、价格计算
- **[药材模块](modules/herbs/)** - 药材字典、拼音码检索、价格管理
- **[验方模块](modules/formula/)** - 验方模板、智能推荐、统计分析
- **[用户模块](modules/users/)** - 用户管理、角色权限、密码安全

## 📊 项目分析报告

**代码现状与架构演进** - 关键模块深度分析

- **[医案/诊断/处方三模块现状分析 (2025-10-24)](reports/medicalcase-consultation-prescription-current-status-analysis-2025-10-24.md)** - Server端3199行、Desktop端17231行、文档20078行完整统计分析，包含架构演进、代码复杂度、测试覆盖率、优化建议 ⭐⭐⭐

## 👥 角色导航

### 🛠️ 开发者导航
- **快速开始**：[API快速参考](quick-reference/api-reference.md) → [Server开发](development/server/README.md) → [Client开发](development/client/README.md)
- **常用流程**：[开发清单](quick-reference/development-checklist.md) → [问题解决](quick-reference/troubleshooting.md)
- **核心标准**：[Server架构](architecture/server/README.md) + [Client架构](architecture/client/README.md)

### 🏗️ 架构师导航
- **架构总览**：[架构总览](architecture/README.md) → [技术决策](architecture/shared/adr/)
- **设计标准**：[Server设计标准](architecture/server/design-standard.md) + [Client设计标准](architecture/client/design-standard.md)
- **架构验证**：[架构测试](development/shared/architecture-testing-guide.md)

### 📊 项目经理导航
- **项目概览**：[项目README](../README.md) → [开发指南](development/README.md)
- **进度跟踪**：[GitHub Issues](https://github.com/shouqitao/凌隐宝堂中医诊所/issues) → [模块状态](modules/README.md)
- **质量保证**：[测试指南](development/shared/testing-guide.md) → [部署指南](deployment/README.md)

### 🔧 测试工程师导航
- **测试标准**：[测试架构标准](development/shared/test-architecture-standard.md)
- **测试实施**：[测试运行指南](development/shared/testing-guide.md)
- **质量报告**：[测试覆盖率报告](reports/test-coverage.md) → [质量分析报告](reports/quality-analysis.md)

## 🔄 常用工作流程

### 🚀 开发新功能
1. **需求分析** → [模块文档](modules/README.md)
2. **技术设计** → [架构标准](architecture/README.md)
3. **开发实施** → [开发指南](development/README.md)
4. **测试验证** → [测试指南](development/shared/testing-guide.md)

### 🐛 修复Bug
1. **问题定位** → [问题解决](quick-reference/troubleshooting.md)
2. **解决方案** → [代码模式](quick-reference/code-patterns.md)
3. **测试验证** → [测试指南](development/shared/testing-guide.md)

### 📝 更新文档
1. **变更评估** → [文档更新指南](development/shared/documentation-update-guide.md)
2. **同步更新** → [文档维护](development/shared/documentation-maintenance.md)
3. **质量检查** → [文档质量检查](development/shared/documentation-quality-check.md)

## 🎯 核心特性

### 基于实际代码
- ✅ **完全同步**：所有文档基于实际代码分析创建
- ✅ **准确无误**：API接口、实体关系、架构设计完全准确
- ✅ **实时更新**：代码变更后立即同步更新文档

### 三层对齐架构
- ✅ **Server端**：Core + Modules + Services 三层架构
- ✅ **Client端**：Shell + Core + Modules + Workstations 五层设计
- ✅ **Shared层**：Models + Interfaces + Infrastructure + Utilities

### 双轨认证系统
- ✅ **普通用户轨道**：Users表标准认证流程
- ✅ **超级管理员轨道**：AdminSecrets表物理隔离
- ✅ **JWT机制**：AccessToken(2小时) + RefreshToken(7天)

### 中医特色功能
- ✅ **四诊合参**：望闻问切完整记录
- ✅ **辨证论治**：中医诊断和治法方案
- ✅ **处方管理**：四种录入方式、药材配伍检查
- ✅ **药材字典**：2000+药材、拼音码检索

## 📊 成功指标

- ✅ **3次点击内**找到任何需要的文档
- ✅ **5分钟内**理解导航结构
- ✅ **100%准确**的代码同步度
- ✅ **零历史包袱**的现代化文档体系

## 🔧 支撑文档体系

**质量保证和持续改进** - 文档维护和优化

### 📊 质量监控
- **[文档使用指标](support/documentation-metrics.md)** - 使用数据收集、反馈机制、质量评估
- **[文档维护指南](support/documentation-maintenance.md)** - 维护流程、质量检查、持续改进

### 📋 运营管理
- **自动化工具链** - 文档生成、质量检查、发布流程
- **团队协作机制** - 责任分工、审核流程、应急响应
- **持续改进计划** - 季度规划、质量目标、资源分配

## 🔗 相关资源

- [项目Git仓库](https://github.com/shouqitao/凌隐宝堂中医诊所) - 代码和文档版本管理
- [Steering Documents](../.spec-workflow/steering/) - 产品愿景、技术决策、项目结构
- [在线API文档](http://localhost:5001/swagger) - 开发环境API交互界面
- **[归档文档目录](archive/README.md)** - 已完成实施的需求文档、已废弃的讨论文档归档策略与历史记录

## 📈 项目成果

### 🎯 完成度统计
- ✅ **Level 1** (快速参考): 5个文档 - 100%完成
- ✅ **Level 2** (架构指南): 5个文档 - 100%完成
- ✅ **Level 3** (深度参考): 5个文档 - 100%完成
- ✅ **Level 4** (支撑体系): 2个文档 - 100%完成
- 📊 **总文档数量**: 17个核心文档

### 🏗️ 架构特色
- ✅ **三层对齐**: Server/Client/Shared架构完全对应
- ✅ **双轨认证**: Users表 + AdminSecrets表物理隔离
- ✅ **中医特色**: 四诊合参、辨证论治、处方管理完整覆盖
- ✅ **实用导向**: 80/15/5需求分层，3次点击找到目标

### 📚 质量保证
- ✅ **代码同步**: 所有文档基于实际代码分析创建
- ✅ **标准一致**: 统一的写作标准和格式规范
- ✅ **用户友好**: 完善的导航、搜索、反馈机制
- ✅ **持续维护**: 自动化监控和质量改进流程

---

*本文档中心基于实际代码完全重构，提供准确、同步、易用的技术文档。如有问题或建议，请通过GitHub Issues反馈。*

**最后更新：2025-01-24 - v5.1文档规整版** 🎉
**总文档数：19个（新增业务规则文档、三模块现状分析报告），完成度：100%** ✨