# 凌隐宝堂中医诊所 - 文档导航中心

**文档版本**：v6.1 Diátaxis框架重构版
**创建时间**：2025-10-15
**最后更新**：2025-11-10（Phase 1基础数据模块重构完成）
**维护负责**：项目团队

---

## 🎯 文档框架说明

本项目文档采用 **[Diátaxis](https://diataxis.fr/) 框架**组织，将文档分为4个明确的类别，根据不同的学习和使用目的提供精准的文档导航。

### 📚 四种文档类型

| 类型 | 用途 | 受众 | 何时使用 | 特点 |
|-----|------|------|---------|------|
| **[Tutorial](#-tutorial教程)** | 学习 | 新手 | 第一次接触系统 | 手把手引导 |
| **[How-to Guides](#%EF%B8%8F-how-to-guides操作指南)** | 解决问题 | 实践者 | 完成特定任务 | 步骤清晰 |
| **[Reference](#-reference参考手册)** | 查阅信息 | 所有人 | 查找API/配置 | 精确简洁 |
| **[Explanation](#-explanation概念解释)** | 理解概念 | 架构师 | 深入理解设计 | 深入解释 |

**Diátaxis核心理念**：
- 📖 **Learning vs Using**：Tutorial/Explanation侧重学习，How-to/Reference侧重使用
- 🎯 **Practical vs Theoretical**：Tutorial/How-to侧重实践，Reference/Explanation侧重理论
- 🔄 **清晰边界**：每种文档类型有明确的职责，避免混淆

---

## 📖 Tutorial（教程）

> **定位**：学习导向 + 实践导向
> **目标**：引导式学习，边学边做，快速上手
> **适合人群**：完全新手、需要系统学习的开发者

### 核心教程

- **[Tutorial总览](tutorials/README.md)** ⭐ **新手必读**
  教程体系说明、学习路径、使用指南

- **[5分钟快速开始](tutorials/quick-start.md)** ⚠️ 占位文档
  环境搭建、启动系统、首次操作

- **[开发第一个功能](tutorials/first-feature.md)** ⚠️ 占位文档
  完整端到端开发流程（Server + Client + 测试 + 提交）

### 学习路径

```
新手入门 → 快速开始(5分钟) → 开发第一个功能(1小时) → 深入架构
```

**下一步**：完成Tutorial后，查阅[How-to Guides](#%EF%B8%8F-how-to-guides操作指南)解决具体问题

---

## 🛠️ How-to Guides（操作指南）

> **定位**：任务导向 + 实践导向
> **目标**：解决特定问题，假设有基础知识
> **适合人群**：有基础的开发者，需要解决具体问题

### 核心指南

- **[How-to Guides总览](how-to/README.md)** ⭐ **操作指南入口**
  开发规范、流程指导、任务导航

### 分层操作指南（三层对齐架构）

#### Server端开发指南

- **[Server端操作总览](how-to/server/README.md)** ⭐
  后端开发、API开发、数据库操作
- **[认证集成指南](how-to/server/auth-integration.md)**
  JWT认证、双轨认证实现、权限验证
- **[诊断模块开发](how-to/server/consultation-development.md)**
  Consultation Service/Controller/Repository开发
- **[事件总线集成](how-to/server/eventbus-integration.md)**
  事件发布订阅、跨模块通信
- **[方剂模块开发](how-to/server/formula-development.md)**
  Formula Service/Controller/Repository开发
- **[接口层使用指南](how-to/server/interfaces-usage.md)**
  IRepository/IService接口规范
- **[医案模块开发](how-to/server/medical-case-development.md)**
  MedicalCase Service/Controller/Repository开发
- **[处方模块开发](how-to/server/prescriptions-development.md)**
  Prescription Service/Controller/Repository开发
- **[WebAPI开发指南](how-to/server/webapi-development.md)**
  API端点设计、Swagger配置、错误处理
- **[WebAPI部署指南](how-to/server/webapi-deployment.md)**
  生产环境部署、Windows Service配置、自动化脚本

#### Client端开发指南

- **[Client端操作总览](how-to/client/README.md)** ⭐
  WPF开发、UI设计、客户端逻辑
- **[用户管理操作指南](how-to/client/user-management.md)** ⭐ **Phase 1基础模块**
  用户新建/编辑/查看/删除、状态管理、密码管理、批量删除
- **[患者管理操作指南](how-to/client/patient-management.md)** ⭐ **Phase 1基础模块**
  患者新建/编辑/查看/删除、批量导入/导出、拼音码生成、年龄计算
- **[药材管理操作指南](how-to/client/herbs-management.md)** ⭐ **Phase 1基础模块**
  批量导入/导出（Desktop主导）、引用检查、重复策略、失败恢复流程
- **[诊断模块开发](how-to/client/consultation-development.md)**
  ConsultationViewModel/View开发、四诊合参UI
- **[方剂模块开发](how-to/client/formula-development.md)**
  FormulaViewModel/View开发、验方管理
- **[Foundation层开发](how-to/client/foundation-development.md)**
  ViewModelBase、命令封装、依赖注入
- **[Infrastructure层使用](how-to/client/infrastructure-usage.md)**
  ApiClient、事件聚合器、本地存储
- **[医案模块开发](how-to/client/medical-case-development.md)**
  MedicalCaseViewModel/View开发、病案管理UI
- **[Models层使用指南](how-to/client/models-usage.md)**
  ClientDTO设计、数据绑定、验证规则
- **[处方模块开发](how-to/client/prescriptions-development.md)**
  PrescriptionViewModel/View开发、药材选择器
- **[Presentation层开发](how-to/client/presentation-development.md)**
  样式主题、控件模板、资源字典
- **[打印功能开发](how-to/client/print-functionality.md)**
  打印模板设计、FlowDocument生成、打印预览

#### 共享开发指南

- **[共享操作总览](how-to/shared/README.md)** ⭐
  跨端开发、通用组件、接口定义
- **[DTO开发指南](how-to/shared/dto-development.md)**
  DTO创建五步法、验证规则、AutoMapper配置
- **[共享组件使用](how-to/shared/components-usage.md)**
  Result/PagedResult/ApiResponse、通用工具类

#### 架构模式指南（⭐ Epic #1962新增）

- **[批量操作模式](how-to/patterns/batch-operations.md)** ⭐⭐⭐
  Desktop主导模式、批量导入/导出实现、EPPlus集成、性能优化

### Phase 3 角色模块（⭐ 新增）

> **Phase 3文档增强**：2025-10-30完成，涵盖Admin和Clinical角色模块的完整架构设计和开发指南

#### Client端角色模块开发

- **[Admin模块开发](how-to/client/admin-development.md)**
  管理员控制台、用户管理、系统配置、数据库维护
- **[Clinical模块开发](how-to/client/clinical-development.md)**
  诊疗工作台、待诊列表、快速开单、今日工作总结
- **[Herbs模块集成](how-to/client/herbs-integration.md)**
  药材选择器集成、处方生成、药材搜索
- **[Formula模块集成](how-to/client/formula-integration.md)**
  验方选择器集成、一键套用、验方保存

### Claude Code Skills工具指南（⭐ Phase 2新增）

> **Skills工具体系**：2025-11-09整合，提供自动化开发、质量检查、文档生成等21个专业工具

#### 质量检查工具

- **[架构合规检查](how-to/quality/arch-compliance.md)**
  三层对齐架构验证、依赖方向检查、DDD边界检测
- **[代码审查](how-to/quality/code-review.md)**
  编码规范检查、MVVM模式验证、命名规范审查
- **[设计架构验证](how-to/quality/design-arch-validator.md)**
  设计文档架构合规性检查、技术选型验证
- **[MVP合规检查](how-to/quality/mvp-compliance.md)**
  技术黑名单检测、过度设计识别、MVP原则验证
- **[需求架构守护](how-to/quality/requirements-arch-guard.md)**
  需求文档架构约束检查、技术债务预警
- **[质量报告生成](how-to/quality/quality-reporter.md)**
  质量评分、自动合并决策、测试覆盖率分析

#### 开发辅助工具

- **[上下文构建器](how-to/development/context-builder.md)**
  任务上下文聚合、文档检索、代码依赖分析
- **[依赖分析器](how-to/development/dependency-analyzer.md)**
  依赖图生成、循环依赖检测、影响范围分析
- **[设计文档生成](how-to/development/design-generator.md)**
  需求→设计文档转换、API设计、Phase拆分
- **[Issue模板生成](how-to/development/issue-template.md)**
  GitHub Issue批量生成、标准化模板、Epic关联
- **[PR生成器](how-to/development/pr-generator.md)**
  Pull Request描述生成、commit分析、影响评估
- **[需求文档生成](how-to/development/requirements-generator.md)**
  用户需求→结构化需求文档、业务规则分析
- **[研究助手](how-to/development/research-assistant.md)**
  技术调研、代码示例搜索、方案对比分析
- **[任务分解器](how-to/development/task-breakdown.md)**
  设计→任务清单、Phase划分、工作量估算
- **[任务执行器](how-to/development/task-executor.md)** ⭐ 核心工具
  Issue→代码→验证→提交全流程自动化
- **[任务反思器](how-to/development/task-reflector.md)**
  任务完成后反思、技术债务识别、知识归档
- **[任务追踪器](how-to/development/task-tracker.md)**
  GitHub双向同步、Epic进度聚合、依赖可视化
- **[工作流编排器](how-to/development/workflow-orchestrator.md)** 🔴 自动化引擎
  14状态自动化流程、5个确认点、85%自动化率
- **[工作量估算器](how-to/development/workload-estimator.md)**
  智能工作量估算、历史数据分析、团队速率计算

#### 测试与文档工具

- **[测试生成器](how-to/testing/test-generator.md)**
  xUnit测试用例自动生成、AAA模式、Mock配置
- **[文档同步检查](how-to/documentation/doc-sync.md)**
  代码变更检测、文档更新清单、100%同步保证

#### Skills总览

- **[Skills工具概览](explanation/skills-overview.md)** ⭐ 完整索引
  21个Skills完整列表、分类说明、使用场景
- **[Skills协作指南](explanation/skills-collaboration.md)**
  多Skills联动、工作流编排、最佳实践
- **[自动化系统总结](explanation/automation-system.md)**
  workflow-orchestrator详解、自动化率提升路径

### 常用操作快速入口

| 我想... | 查阅文档 |
|--------|----------|
| 添加新模块 | [Server端操作总览](how-to/server/README.md) |
| 创建新API | [WebAPI开发指南](how-to/server/webapi-development.md) |
| 开发新页面 | [Foundation层开发](how-to/client/foundation-development.md) / [Presentation层开发](how-to/client/presentation-development.md) |
| 编写测试 | [任务工作流清单](how-to/shared/task-workflow-checklist.md) |
| 提交代码 | [任务工作流清单](how-to/shared/task-workflow-checklist.md) |

**下一步**：查阅[Reference](#-reference参考手册)获取具体API和配置信息

---

## 📖 Reference（参考手册）

> **定位**：信息导向 + 查阅导向
> **目标**：快速查找API、配置、命令等技术细节
> **适合人群**：所有开发者

### 核心参考

- **[Reference总览](reference/README.md)** ⭐ **参考手册入口**
  快速参考、API文档、模块文档导航

### 快速参考（80%日常需求）

- **[API快速参考](reference/quick-reference/api-reference.md)**
  最常用API和调用示例

- **[代码模式](reference/code-patterns-enhancement-summary.md)**
  常用代码模式和模板

- **[UI设计规范](reference/ui-design-guidelines.md)** ⭐ **WPF UI标准**
  颜色规范、间距规范、完整视图模板、ModernTextBoxStyle

- **[WPF常见问题](reference/wpf-common-issues.md)** ⭐ **问题解决**
  视觉树循环依赖、命令无限循环、数据绑定、性能优化

- **[Prism框架指南](reference/prism-framework-guide.md)** ⭐ **框架使用**
  Region导航、命令系统、依赖注入、事件聚合器

- **[配置参数指南](reference/configuration-parameters-guide.md)**
  配置文件参数说明和最佳实践

- **[技术栈总览](reference/technology-stack.md)**
  项目使用的技术栈和版本信息

- **[问题排查](reference/quick-reference/troubleshooting.md)**
  常见问题和解决方案

- **[开发清单](reference/quick-reference/development-checklist.md)**
  开发流程和质量检查

- **[配置模板](reference/quick-reference/config-templates.md)**
  常用配置文件模板

- **[模板文档](reference/templates/)**
  Issue模板、PR模板、文档模板等

### API文档

- **[API总览](reference/api/README.md)**
  12个控制器完整API文档

**Phase 1基础数据模块API**（⭐ Users/Patients/Herbs）：
- **[Users API参考](reference/api/users-api.md)** - 用户管理API完整参考（认证、状态管理、批量操作）
- **[Patients API参考](reference/api/patients-api.md)** - 患者管理API完整参考（批量导入Server主导模式，Epic #1934）
- **[Herbs API参考](reference/api/herbs-api.md)** - 药材管理API完整参考（批量操作Desktop主导模式，Epic #1962）

**其他模块API**：
- **[Prescriptions API参考](reference/api/prescriptions-api.md)** - 处方管理API完整参考

- **模块API**：[认证](reference/api/auth/) | [患者](reference/api/patients/) | [医案](reference/api/medicalcase/) | [诊断](reference/api/consultation/) | [处方](reference/api/prescription/) | [药品](reference/api/herbs/) | [方剂](reference/api/formula/) | [用户](reference/api/users/)

### 模块文档

- **[模块总览](reference/modules/README.md)** ✅
  8个业务模块完整说明

- **[医案模块](reference/modules/medical-case/)** ✅
  医案状态管理、业务流程

> **⚠️ 文档补充中**：9个模块中仅2个有完整文档（22%），其余7个模块文档正在按优先级补齐（详见 [文档缺失分析报告](reports/documentation-gap-analysis-2025-10-29.md) 和 [重构计划](reports/refactoring-plan-2025-10-29.md)）

**下一步**：查阅[Explanation](#-explanation概念解释)深入理解设计原理

---

## 💡 Explanation（概念解释）

> **定位**：理解导向 + 理论导向
> **目标**：深入理解架构设计、设计决策、业务规则
> **适合人群**：架构师、需要深入理解系统的开发者

### 核心说明

- **[Explanation总览](explanation/README.md)** ⭐ **概念解释入口**
  架构说明、业务规则、设计文档导航

### 架构说明（三层对齐架构）

> **✨ 文档质量保证**：Server端和Shared端架构文档已于2025-10-28完成全面修复，所有代码示例均来自实际项目，架构描述与实现100%对齐。

- **[架构总览](explanation/architecture/README.md)** ⭐⭐⭐ **核心入口**
  对齐架构设计原理与导航

#### Server端架构设计

- **[Server端架构总览](explanation/architecture/server/README.md)** ⭐⭐⭐ ✅ 已验证
  三层架构、8个模块、服务标准（含PatientService/Controller/Repository实际代码示例）
- **[认证模块架构](explanation/architecture/server/auth-design.md)**
  双轨认证设计、JWT令牌生成、权限验证机制
- **[诊断模块架构](explanation/architecture/server/consultation-design.md)**
  Consultation聚合根设计、四诊合参数据结构、辨证论治流程
- **[事件总线架构](explanation/architecture/server/eventbus-design.md)**
  事件发布订阅模式、跨模块通信机制、事件存储设计
- **[方剂模块架构](explanation/architecture/server/formula-design.md)**
  Formula聚合根设计、验方管理、配伍规则引擎
- **[医案模块架构](explanation/architecture/server/medical-case-design.md)**
  MedicalCase聚合根设计、状态机管理、生命周期控制
- **[处方模块架构](explanation/architecture/server/prescriptions-design.md)**
  Prescription聚合根设计、四种录入方式、药材配伍检查
- **[WebAPI设计](explanation/architecture/server/webapi-design.md)**
  API版本管理、错误处理、Swagger配置、跨域策略

**模块架构文档**（⭐ Phase 1基础数据模块）：
- **[Users模块架构](explanation/architecture/server/modules/users.md)** ⭐⭐⭐
  用户模块三层架构、认证与安全、批量操作、状态管理、性能基准
- **[Patients模块架构](explanation/architecture/server/modules/patients.md)** ⭐⭐⭐
  患者模块三层架构、批量导入（Server主导）、多条件搜索、性能基准
- **[Herbs模块架构](explanation/architecture/server/modules/herbs.md)** ⭐⭐⭐
  药材模块三层架构、批量操作（Desktop主导）、引用检查、性能基准

#### Client端架构设计

- **[Client端架构总览](explanation/architecture/client/README.md)** ⭐⭐⭐
  MVVM架构、5层设计、UI标准
- **[Shell层架构设计](explanation/architecture/client/shell-layer-design.md)**
  Shell层职责边界、组件结构、交互模式
- **[认证模块架构](explanation/architecture/client/auth-design.md)**
  登录流程设计、Token管理、会话持久化
- **[诊断模块架构](explanation/architecture/client/consultation-design.md)**
  ConsultationViewModel设计、四诊合参UI、辨证论治交互
- **[Contracts层设计](explanation/architecture/client/contracts-design.md)**
  接口定义、Service契约、事件契约
- **[方剂模块架构](explanation/architecture/client/formula-design.md)**
  FormulaViewModel设计、验方选择器、一键套用功能
- **[Foundation层设计](explanation/architecture/client/foundation-design.md)**
  ViewModelBase、命令封装、依赖注入容器配置
- **[Infrastructure层设计](explanation/architecture/client/infrastructure-layer-design.md)**
  ApiClient、事件聚合器、本地存储、日志框架
- **[医案模块架构](explanation/architecture/client/medical-case-design.md)**
  MedicalCaseViewModel设计、病案状态管理、UI交互流程
- **[Models层设计](explanation/architecture/client/models-layer-design.md)**
  ClientDTO设计、数据绑定模型、验证规则
- **[处方模块架构](explanation/architecture/client/prescriptions-design.md)**
  PrescriptionViewModel设计、药材选择器、处方生成逻辑
- **[Presentation层设计](explanation/architecture/client/presentation-design.md)**
  样式主题系统、控件模板、资源字典组织

#### Phase 3 角色模块架构（⭐ 新增）

- **[Admin模块架构设计](explanation/architecture/client/admin-module-design.md)**
  管理员控制台架构、AdminHomeViewModel设计、权限控制
- **[Clinical模块架构设计](explanation/architecture/client/clinical-module-design.md)**
  诊疗工作台架构、ClinicalHomeViewModel设计、AC-001实现

#### 共享架构设计

- **[共享架构总览](explanation/architecture/shared/README.md)** ⭐⭐⭐ ✅ 已验证
  跨端组件、按模块组织的DTO、去中心化接口定义（含实际Models/Components/Utilities结构说明）
- **[DTO设计标准](explanation/architecture/shared/dto-design-standard.md)**
  DTO基类体系、命名规范、验证规则、UltraThink v2.0简化原则
- **[共享组件设计](explanation/architecture/shared/components-design.md)**
  Result/PagedResult/ApiResponse设计、通用工具类、扩展方法
- **[跨模块依赖关系](explanation/architecture/shared/cross-module-dependencies.md)** ⭐⭐⭐ ⭐ Epic #1962新增
  Herbs→Prescriptions依赖、引用检查实现、DI配置、聚合根边界保护

### 架构决策记录（ADR）

- **[ADR总览](explanation/architecture/decisions/README.md)**
  所有架构决策记录

- **核心ADR**：
  - [ADR-001: FluentValidation验证框架](explanation/architecture/decisions/ADR-001-fluentvalidation-as-validation-framework.md)
  - [ADR-002: AutoMapper映射框架](explanation/architecture/decisions/ADR-002-automapper-as-mapping-framework.md)
  - [ADR-003: Repository简化设计](explanation/architecture/decisions/ADR-003-repository-simplification.md)
  - [ADR-004: 组件设计指南](explanation/architecture/decisions/ADR-004-component-design-guidelines.md)
  - [ADR-005: 聚合根长期架构](explanation/architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md) ⭐⭐⭐ **重要**
  - [ADR-006: 病案/诊断/处方重构](explanation/architecture/decisions/ADR-006-medicalcase-consultation-prescription-refactoring.md)
  - [ADR-007: Repository和Service层简化重构](explanation/architecture/decisions/ADR-007-repository-service-simplification.md) ⭐ Epic #1725

### 项目愿景与结构（⭐ Phase 3新增）

> **Phase 3文档整合**：2025-11-09从.spec-workflow/steering/迁移核心文档到docs/explanation/，统一文档体系

- **[产品愿景与战略目标](explanation/product-vision.md)** ⭐⭐⭐
  产品愿景、业务目标、用户角色、核心功能、成功指标

- **[项目结构与组织指南](explanation/project-structure.md)** ⭐⭐⭐
  根目录结构、Server/Client/Shared三层代码组织、命名规范、依赖关系

> **📌 归档说明**：原.spec-workflow/已归档至[docs/archive/spec-workflow-legacy-2025-11-09/](archive/spec-workflow-legacy-2025-11-09/)，查看[迁移说明](archive/spec-workflow-legacy-2025-11-09/MIGRATION.md)了解详细映射关系

### 业务规则与设计

- **[业务规则文档](explanation/business-rules.md)** ⭐⭐⭐
  14条核心业务规则（数据约束/业务流程/聚合根/计算规则/访问控制）

- **[看诊流程实体关系](explanation/architecture/shared/clinical-workflow-entity-relationships.md)** ⭐⭐⭐ **权威文档**
  挂号/医案/诊断/处方实体关系与状态机设计

### UI工作流文档（⭐ 新增）

- **[完整启动到工作台流程](explanation/00-complete-startup-to-workstation-flow.md)**
  应用启动、登录、工作台选择完整流程

- **[完整UI交互流程](explanation/01-complete-ui-flow.md)**
  主要功能模块UI交互流程

- **[启动登录优化流程](explanation/02-startup-login-optimized.md)**
  优化后的启动和登录流程

- **[Clinical诊疗工作台](explanation/03-clinical-home-dashboard.md)**
  诊疗工作台设计与交互流程

### 深度技术分析（⭐ 新增）

- **[高级模式](explanation/advanced-patterns.md)**
  高级设计模式和架构模式

- **[API设计最佳实践](explanation/api-design-best-practices.md)**
  API设计原则、RESTful实践、错误处理

- **[FluentValidation验证模式](explanation/validation-patterns.md)** ⭐ **新增**
  条件验证、InputDto统一模式、创建/更新场景区分（Epic #1934）

- **[性能优化](explanation/performance-optimization.md)**
  性能分析、优化策略、最佳实践

- **[测试策略](explanation/testing-strategies.md)**
  测试金字塔、单元测试、集成测试策略

- **[Token认证安全重构](explanation/token-authentication-security-refactor.md)**
  JWT认证安全改进、双轨认证设计

---

## 🚀 快速入口（按角色）

### 🛠️ 开发者快速入口

**新手开发者**：
1. [Tutorial总览](tutorials/README.md) - 学习路径
2. [5分钟快速开始](tutorials/quick-start.md) - 环境搭建
3. [开发第一个功能](tutorials/first-feature.md) - 完整流程

**有经验开发者**：
1. [API快速参考](reference/quick-reference/api-reference.md) - 常用API
2. [代码模式](reference/quick-reference/code-patterns.md) - 代码模板
3. [How-to Guides](how-to-guides/README.md) - 解决具体问题

### 🏗️ 架构师快速入口

**架构理解**：
1. [架构总览](explanation/architecture/README.md) - 三层对齐架构
2. [ADR总览](explanation/architecture/decisions/README.md) - 技术决策
3. [业务规则](explanation/business-rules.md) - 核心约束

**架构决策**：
1. [ADR-005: 聚合根长期架构](explanation/architecture/decisions/ADR-005-aggregate-root-long-term-architecture.md) ⭐⭐⭐
2. [看诊流程实体关系](explanation/architecture/shared/clinical-workflow-entity-relationships.md) ⭐⭐⭐
3. [Server端架构](explanation/architecture/server/README.md) - 设计标准

### 📊 项目经理快速入口

**项目概览**：
1. [项目README](../README.md) - 项目介绍
2. [模块总览](reference/modules/README.md) - 模块状态
3. [GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues) - 进度跟踪

**质量保证**：
1. [任务工作流清单](how-to-guides/shared/task-workflow-checklist.md) - 测试标准与开发流程
2. [开发清单](reference/quick-reference/development-checklist.md) - 质量检查
3. [问题排查](reference/quick-reference/troubleshooting.md) - 常见问题

---

## 📊 项目分析报告

**代码质量与架构演进** - 核心质量报告（18个）

### Phase系列质量分析

- **[Phase 2.1: 三层架构依赖方向验证](reports/phase2.1-analysis-report.md)** ⭐⭐⭐
  架构违规34个（ViewModel→Repository）、违规率50.75%、修复建议

- **[Phase 2.2: DI模式和技术黑名单检查](reports/phase2.2-analysis-report.md)**
  DI合规性100%、技术栈100%符合MVP约束

- **[Phase 3: 代码质量度量分析](reports/phase3-analysis-report.md)** ⭐⭐⭐
  文件大小违规6个、方法复杂度违规41个、修复计划

- **[Phase 4: 组件化质量分析](reports/phase4-analysis-report.md)** ⭐⭐⭐
  Component使用率19.76%、ROI -76.5%、重构建议

- **[Desktop代码质量综合报告](reports/desktop-code-quality-comprehensive-report.md)**
  综合Phase 1-4所有发现、整体质量评估

### Epic与Issue验证报告

- **[Doc Sync Report - Epic #1773](reports/doc-sync-report-epic-1773.md)**
  组件化文档更新清单、架构文档同步验证

- **[Issues完成验证报告](reports/issues-completion-verification-report.md)**
  Issue完成情况汇总验证

- **[Sysadmin数据污染分析报告](reports/sysadmin-数据污染-分析报告.md)**
  超级管理员数据污染问题分析与修复

- **[User模块代码审查报告 (2025-11-08)](reports/User模块代码审查报告_2025-11-08.md)**
  User模块代码规范审查

### 最近分析报告（2025-11）

- **[文档整合最终报告 (2025-11-09)](reports/documentation-consolidation-final-report-2025-11-09.md)** ⭐
  Issue #1933完整执行记录、Phases 1-5成果

- **[文档整合Phase 1分析 (2025-11-09)](reports/documentation-consolidation-phase1-analysis-2025-11-09.md)**
  Phase 1执行分析与后续计划

- **[自动化工作流设计 (2025-11-07)](reports/automation-workflow-design-2025-11-07.md)**
  lybtzyzs-workflow-orchestrator设计方案

- **[Doc Sync Check (2025-11-09)](reports/doc-sync-check-2025-11-09.md)**
  文档同步检查结果

- **[Root Files Audit (2025-11-09)](reports/root-files-audit-2025-11-09.md)**
  根目录文件审计报告

- **[WebAPI配置整合分析 (2025-11-09)](reports/webapi-configuration-consolidation-analysis-2025-11-09.md)**
  WebAPI配置优化分析

- **[WebAPI配置整合验证 (2025-11-09)](reports/webapi-configuration-consolidation-verification-2025-11-09.md)**
  WebAPI配置整合实施验证

- **[用户管理交互统一可行性 (2025-11-08)](reports/user-management-interaction-unification-feasibility-2025-11-08.md)**
  Dialog vs Navigation模式对比、UI统一方案

- **[用户管理交互统一深度分析 (2025-11-08)](reports/user-management-interaction-unification-deep-analysis-2025-11-08.md)** ⭐⭐⭐
  架构模式差异详细分析、代码级别迁移指南

> **📌 历史报告归档**：2025-10月及之前的历史报告（29个）已归档至 [archive/reports-2025-11/](archive/reports-2025-11/)，包括Epic #1886、Issue #1906/1907/1908/1887-1892系列验证报告及历史分析报告

---

## 🔧 支撑文档体系

**质量保证和持续改进** - 文档维护和优化

### 📊 质量监控

- **[文档使用指标](support/documentation-metrics.md)**
  使用数据收集、反馈机制、质量评估

- **[文档维护指南](support/documentation-maintenance.md)**
  维护流程、质量检查、持续改进

### 📋 运营管理

- **自动化工具链** - 文档生成、质量检查、发布流程
- **团队协作机制** - 责任分工、审核流程、应急响应
- **持续改进计划** - 季度规划、质量目标、资源分配

---

## 🎯 核心特性

### 基于实际代码
- ✅ **完全同步**：所有文档基于实际代码分析创建
- ✅ **准确无误**：API接口、实体关系、架构设计完全准确
- ✅ **实时更新**：代码变更后立即同步更新文档

### Diátaxis框架
- ✅ **清晰分类**：4种文档类型，职责明确
- ✅ **精准导航**：根据需求快速定位
- ✅ **用户友好**：3次点击找到任何文档

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

---

## 📈 项目成果

### 🎯 完成度统计
- ✅ **Tutorial**: 3/3文档 - 100%完成（含2个占位文档）
- ✅ **How-to Guides**: 完整开发指南体系 - 100%完成
- ✅ **Reference**: 快速参考5/5 + API文档1/1 + 模块文档2/9 = 85%完成
- ✅ **Explanation**: 架构文档5/5 + ADR 6/6 + 业务规则1/1 = 100%完成
- 📊 **总体完成度**: **91%**（模块文档待补充）

### 🏗️ 架构特色
- ✅ **Diátaxis框架**: 4种文档类型清晰分类
- ✅ **三层对齐**: Server/Client/Shared架构完全对应
- ✅ **三角色体系**: SuperAdmin/Admin/Doctor统一认证架构（Issue #1909）
- ✅ **中医特色**: 四诊合参、辨证论治、处方管理完整覆盖

### 📚 质量保证
- ✅ **代码同步**: 所有文档基于实际代码分析创建
- ✅ **标准一致**: 统一的写作标准和格式规范
- ✅ **用户友好**: 完善的导航、搜索、反馈机制
- ✅ **持续维护**: 自动化监控和质量改进流程

---

## 🔗 相关资源

- [项目Git仓库](https://github.com/shouqitao/LYBTZYZS) - 代码和文档版本管理
- [Steering Documents](../.spec-workflow/steering/) - 产品愿景、技术决策、项目结构
- [在线API文档](http://localhost:5001/swagger) - 开发环境API交互界面
- **[归档文档目录](archive/README.md)** - 已完成实施的需求文档、已废弃的讨论文档归档策略与历史记录

---

## 🔄 版本更新说明

### Issue #1933 文档系统整合 (2025-11-09)

**整合目标**：消除多套文档体系，深度Diátaxis框架重构，清理历史文档

**成果**：
- ✅ **阶段1**：迁移20个开发文档（从.claude/skills/, .design/, .verification/等）到docs/
- ✅ **阶段2**：Diátaxis框架深度重构（6-phase计划）
  - Phase 1: 合并how-to/和how-to-guides/（41个文档统一到how-to/）
  - Phase 2: 清理explanation/子目录（扁平化workflows/，归档spec-workflow/）
  - Phase 3: 重新分配30个非Diátaxis文档（按框架分类）
  - Phase 4: 归档reports/历史报告（保留18个核心，归档29个历史）
  - Phase 5: 更新docs/index.md导航（反映新结构）
- ✅ **阶段3**：清理隐藏文件夹（删除.verification/, .ai/, .temp/等6个目录）
- ✅ **GitHub同步**：同步Skills到GitHub，删除.claude/skills/本地副本

**统计**：
- 文档迁移：26个（20个开发文档 + 6个MD报告）
- 文档重构：71个（合并、重分配、归档）
- 历史归档：60个（报告29个 + spec-workflow 21个 + 其他10个）
- 目录清理：16个（非Diátaxis目录10个 + 隐藏文件夹6个）
- docs/结构：8个一级目录（tutorials/ how-to/ reference/ explanation/ reports/ archive/ support/ index.md）

**详细变更记录**：[文档整合最终报告](reports/documentation-consolidation-final-report-2025-11-09.md)

**相关Issue**：
- [Issue #1933](https://github.com/shouqitao/LYBTZYZS/issues/1933) - 文档系统整合（当前）

---

### Issue #1733 WebAPI MVP合规优化 (2025-10-31)

**优化目标**：移除过度设计的监控系统，统一缓存策略，简化控制器逻辑

**成果**：
- 删除 PerformanceController（6个端点）- 性能监控委托给 Application Insights
- 简化 HealthController（移除环境分支和复杂诊断）
- 简化 CacheHealthController（保留核心管理功能）
- 合并 AuthController 重复端点（2个）
- 移除验方克隆端点（1个）
- 统一缓存策略（移除7处 ResponseCache，使用 OutputCache 基础设施）

**统计**：
- 代码减少：804行
- 端点减少：12个
- 编译状态：✅ 0 errors, 0 warnings

**详细变更记录**：[API文档 - 版本变更记录](reference/api/README.md#-版本变更记录)

**相关Issue**：
- [Issue #1733](https://github.com/shouqitao/LYBTZYZS/issues/1733) - WebAPI MVP合规优化
- [Issue #1732](https://github.com/shouqitao/LYBTZYZS/issues/1732) - OutputCache基础设施配置（前置依赖）

### v6.0 Diátaxis框架重构 (2025-10-29)

**重大变更**：完全采用Diátaxis框架重组文档体系

**目录调整**：
- `docs/development/` → `docs/how-to-guides/`（操作指南）
- `docs/quick-reference/` → `docs/reference/quick-reference/`（快速参考）
- `docs/api/` → `docs/reference/api/`（API文档）
- `docs/modules/` → `docs/reference/modules/`（模块文档）
- `docs/architecture/` → `docs/explanation/architecture/`（架构说明）
- `docs/design/` → `docs/explanation/design/`（设计文档）
- `docs/requirements/` → `docs/explanation/requirements/`（需求文档）
- `docs/business-rules.md` → `docs/explanation/business-rules.md`（业务规则）

**保留内容**：
- `docs/tutorials/`（Tutorial层，Issue #1715已创建）
- 三层对齐架构（Server/Client/Shared）作为子级组织

**新增文档**：
- [Tutorial总览](tutorials/README.md)
- [How-to Guides总览](how-to-guides/README.md)
- [Reference总览](reference/README.md)
- [Explanation总览](explanation/README.md)

**改进点**：
- ✅ 文档分类清晰，职责明确
- ✅ 导航路径优化，3次点击找到目标
- ✅ 保留三层对齐架构的项目特色
- ✅ 符合业界标准（Diátaxis框架）

**相关Issue**：
- [Issue #1715](https://github.com/shouqitao/LYBTZYZS/issues/1715) - Phase 1 Tutorial层创建（已完成）
- [Issue #1716](https://github.com/shouqitao/LYBTZYZS/issues/1716) - Diátaxis完整重构（当前）

### v5.1 Phase 4同步版 (2025-10-28)

**文档修复**：Server端和Shared端架构文档已与实际代码100%对齐
- 删除~930行假内容（泛型基类、不存在的工具类）
- 补充~580行真实代码示例（PatientService/Controller/Repository）
- 修正Shared层目录结构说明（Models/Interfaces/Components/Utilities）

---

*本文档中心基于Diátaxis框架深度重构，提供清晰、准确、易用的技术文档。如有问题或建议，请通过GitHub Issues反馈。*

**最后更新：2025-11-10 - Phase 1基础数据模块重构完成（Tasks 1.1-1.12）** 🎉
**文档框架：Diátaxis 4种类型（Tutorial/How-to/Reference/Explanation）** 📚
**架构特色：三层对齐架构（Server/Client/Shared）** 🏗️
**文档体系：统一docs/体系，清理多套文档（Skills/Spec-workflow整合）** 🧹
**总体完成度：93%（模块文档待补充）** ✅
