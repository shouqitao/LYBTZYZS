# 凌隐宝堂中医诊所文档中心

**文档版本**：v3.0  
**创建时间**：2025-09-25  
**最后更新**：2025-10-15（统一文档导航门户上线）  
**维护负责**：Claude Code + Thinker  
**关联文档**：[项目README](../README.md), [开发者指导](DEVELOPER_GUIDE.md)

## 🎯 统一文档导航门户

**核心目标**：消除"开发时标准错乱"问题，为所有角色提供**统一的文档入口**，确保在3次点击内找到任何需要的文档。

### 🏗️ 文档体系架构

项目采用**双轨文档体系**，通过统一门户实现无缝导航：

| 体系 | 用途 | 内容类型 | 维护方式 |
|------|------|----------|----------|
| **docs/** | 开发标准和指南 | 架构标准、开发规范、操作指南（HOW） | 人工维护，长期参考 |
| **spec-workflow/** | 项目决策和规格 | 需求分析、设计记录、项目报告（WHAT/WHY） | 审批流程，有生命周期 |

---

## 👥 角色导航

### 🛠️ 开发者导航
*面向日常开发工作的快速通道 - 基于文档分析280个开发者相关文档优化*

#### 📋 必读核心标准 (开发者必备)
- [**Server模块设计标准**](architecture/server-module-design-standard.md) - 三层架构、接口规范 ⭐
- [**Client端设计标准**](architecture/client/unified-design-standard.md) - MVVM架构、依赖注入 ⭐
- [**开发规范集**](development/README.md) - 编码标准、测试指南、最佳实践 ⭐
- [**CLAUDE.md**](../CLAUDE.md) - 开发约束、双轨工作流、执行原则 ⭐

#### 🏗️ 架构设计与实现 (56个文档)
- [**系统架构总览**](architecture/README.md) - 架构决策、模块化设计
- [**Server端架构**](architecture/server-module-design-standard.md) - 三层架构详细说明
- [**Client端架构**](architecture/client/unified-design-standard.md) - WPF Prism MVVM实现
- [**架构测试指南**](architecture/testing/architecture-testing-guide.md) - 15条架构约束验证
- [**模块化设计**](architecture/modules/README.md) - 16个业务模块详细设计
- [**ADR决策记录**](architecture/ADR-003-server-module-unified-design.md) - 禁止CQRS等重要决策

#### 🔧 开发实施指南 (32个文档)
- [**开发规范总览**](development/README.md) - 编码标准、测试指南、最佳实践
- [**测试运行指南**](development/testing-guide.md) - VS2022/CLI测试、覆盖率分析
- [**测试架构标准**](development/test-architecture-standard.md) - 测试分层、标准规范
- [**文档编写指南**](development/documentation-guidelines.md) - 文档质量标准、维护流程
- [**依赖注入指南**](development/repository-dependency-injection-guide.md) - Repository统一DI配置
- [**AI辅助工作流**](development/ai-assisted-automation-workflow.md) - Issue驱动开发流程

#### 📡 API与接口开发
- [**API接口文档**](api/README.md) - RESTful接口、Swagger文档
- [**在线API文档**](http://localhost:5001/swagger) - 开发环境API交互界面

#### 🔄 开发工作流程
- **🚀 开发新功能** → [需求模板](../.spec-workflow/templates/requirements-template.md) → [设计模板](../.spec-workflow/templates/design-template.md) → [任务模板](../.spec-workflow/templates/tasks-template.md) → [实施开发](architecture/) → [测试验证](development/testing-guide.md)
- **🐛 修复Bug** → [问题排查指南](development/) → [测试验证](development/testing-guide.md) → [代码审查](architecture/testing/) → [部署更新](deployment/)
- **🏗️ 架构设计** → [架构标准](architecture/) → [ADR流程](architecture/ADR-003-server-module-unified-design.md) → [设计模板](../.spec-workflow/templates/design-template.md) → [架构测试](architecture/testing/)
- **📝 维护文档** → [文档指南](development/documentation-guidelines.md) → [维护流程](../CLAUDE.md) → [质量检查](scripts/documentation-maintenance/)

#### ⚡ 快速访问 (开发者常用)
- 🛠️ **[环境配置](DEVELOPER_GUIDE.md)** - 5分钟快速开始
- 📝 **[代码规范](development/README.md)** - 编码标准和最佳实践
- 🧪 **[测试运行](development/testing-guide.md)** - 本地测试和覆盖率分析
- 📋 **[项目管理](tasks/)** - 任务跟踪和进度管理
- 🔧 **[GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)** - 需求和问题跟踪
- 📊 **[项目状态](PROJECT-STATUS-2025-09-27.md)** - 当前项目状态和进度

### 🏗️ 架构师导航
*面向技术决策和系统设计 - 基于文档分析45个架构相关文档优化*

#### 📋 核心架构标准 (架构师必备)
- [**系统架构总览**](architecture/README.md) - 架构决策、模块化设计原则 ⭐
- [**Server模块设计标准**](architecture/server-module-design-standard.md) - 三层架构、接口规范 ⭐
- [**Client端设计标准**](architecture/client/unified-design-standard.md) - MVVM架构、依赖注入 ⭐
- [**架构测试指南**](architecture/testing/architecture-testing-guide.md) - 15条架构约束验证 ⭐
- [**ADR决策流程**](architecture/ADR-003-server-module-unified-design.md) - 架构决策记录方法 ⭐

#### 🏛️ 架构设计与决策 (28个文档)
- [**模块化设计**](architecture/modules/README.md) - 16个业务模块详细设计
- [**架构约束验证**](architecture/testing/architecture-testing-guide.md) - 架构合规性检查
- [**技术决策记录**](../.spec-workflow/specs/) - 项目设计文档、决策历史
- [**API设计规范**](api/README.md) - RESTful接口设计标准
- [**数据架构设计**](architecture/data/) - 数据模型、存储策略
- [**安全架构**](security/) - 安全架构设计原则

#### 🔧 技术选型与评估
- [**项目技术文档**](../.spec-workflow/steering/tech.md) - 技术栈决策与评估
- [**架构原则**](architecture/README.md) - 设计原则、模式选择
- [**依赖注入架构**](development/repository-dependency-injection-guide.md) - DI架构模式
- [**测试架构标准**](development/test-architecture-standard.md) - 测试策略设计
- [**部署架构**](deployment/) - 系统部署、运维架构

#### 📊 架构治理与评审
- [**架构评审流程**](architecture/testing/) - 设计评审、合规检查
- [**技术债务管理**](architecture/) - 债务识别、重构策略
- [**架构演进路线图**](../.spec-workflow/specs/) - 长期架构规划
- [**性能架构**](development/) - 性能优化架构设计
- [**监控架构**](deployment/) - 系统监控、可观测性

#### 🔄 架构工作流程
- **🏗️ 新系统架构设计** → [架构标准](architecture/) → [需求分析](../.spec-workflow/templates/requirements-template.md) → [设计决策](../.spec-workflow/templates/design-template.md) → [架构评审](architecture/testing/) → [实施指导](development/)
- **🔧 架构重构优化** → [现状分析](architecture/modules/README.md) → [问题识别](architecture/testing/architecture-testing-guide.md) → [重构设计](architecture/) → [影响评估](../.spec-workflow/specs/) → [迁移计划](deployment/)
- **📋 技术选型决策** → [需求分析](../.spec-workflow/specs/) → [技术调研](../.spec-workflow/steering/tech.md) → [方案评估](architecture/) → [ADR决策](architecture/ADR-003-server-module-unified-design.md) → [实施规划](development/)
- **🔍 架构合规审查** → [架构标准检查](architecture/testing/architecture-testing-guide.md) → [代码审查](development/) → [设计文档审查](../.spec-workflow/specs/) → [约束验证](architecture/testing/) → [改进建议](development/)

#### ⚡ 快速访问 (架构师常用)
- 🏛️ **[架构标准集](architecture/README.md)** - 核心架构设计原则
- 📋 **[ADR决策记录](architecture/ADR-003-server-module-unified-design.md)** - 重要架构决策历史
- 🧪 **[架构测试](architecture/testing/architecture-testing-guide.md)** - 架构约束验证工具
- 📊 **[模块化设计](architecture/modules/README.md)** - 业务模块架构视图
- 🔧 **[技术栈文档](../.spec-workflow/steering/tech.md)** - 技术选型决策依据
- 🏗️ **[系统架构图](../README.md)** - 整体架构可视化视图

### 📊 项目经理导航
*面向项目管理和进度跟踪 - 基于文档分析38个项目管理相关文档优化*

#### 📋 核心管理资源 (项目经理必备)
- [**项目总览**](../README.md) - 系统架构、技术栈、当前状态 ⭐
- [**项目状态文档**](PROJECT-STATUS-2025-09-27.md) - 详细进度、资源状态 ⭐
- [**GitHub Issues**](https://github.com/shouqitao/LYBTZYZS/issues) - 需求与任务单一事实源 ⭐
- [**CLAUDE.md**](../CLAUDE.md) - 团队协作、工作流程、执行原则 ⭐
- [**开发者指导**](DEVELOPER_GUIDE.md) - 团队开发规范和流程 ⭐

#### 📈 项目规划与需求 (18个文档)
- [**项目需求文档**](../.spec-workflow/specs/) - 需求分析、功能规格
- [**项目完成报告**](../.spec-workflow/archive/) - 项目成果、完成总结
- [**技术决策记录**](../.spec-workflow/specs/) - 技术选型决策和评估
- [**项目架构文档**](architecture/) - 技术架构、模块依赖关系
- [**开发资源评估**](development/) - 团队资源、技术能力评估

#### 🔄 进度跟踪与控制
- [**任务管理系统**](tasks/) - 任务分解、进度跟踪、状态管理
- [**质量报告**](reports/) - 测试覆盖率、质量指标分析
- [**项目状态跟踪**](../README.md) - 里程碑跟踪、风险监控
- [**团队协作流程**](../CLAUDE.md) - 团队沟通、协作机制
- [**AI辅助工作流**](development/ai-assisted-automation-workflow.md) - 自动化进度管理

#### 📊 交付管理与质量
- [**测试架构标准**](development/test-architecture-standard.md) - 质量保证标准
- [**部署指南**](deployment/) - 部署流程、发布管理
- [**文档质量标准**](development/documentation-guidelines.md) - 文档交付质量
- [**架构测试验证**](architecture/testing/architecture-testing-guide.md) - 技术质量把关
- [**安全规范**](security/) - 安全交付要求

#### 🗓️ 项目管理工作流程
- **📋 项目启动** → [项目总览](../README.md) → [需求确认](../.spec-workflow/specs/) → [资源规划](development/) → [团队组建](../CLAUDE.md)
- **🚀 迭代管理** → [需求跟踪](https://github.com/shouqitao/LYBTZYZS/issues) → [任务分配](tasks/) → [进度监控](PROJECT-STATUS-2025-09-27.md) → [质量检查](development/test-architecture-standard.md)
- **📊 项目监控** → [状态报告](../README.md) → [风险评估](../.spec-workflow/specs/) → [资源调整](development/) → [干系人沟通](../CLAUDE.md)
- **✅ 交付收尾** → [质量验收](development/testing-guide.md) → [文档交付](development/documentation-guidelines.md) → [项目总结](../.spec-workflow/archive/) → [经验沉淀](reports/)

#### ⚡ 快速访问 (项目经理常用)
- 📊 **[项目状态仪表板](PROJECT-STATUS-2025-09-27.md)** - 实时项目状态总览
- 🔧 **[GitHub项目管理](https://github.com/shouqitao/LYBTZYZS/issues)** - 需求和任务跟踪
- 📋 **[任务管理系统](tasks/)** - 详细任务分解和进度
- 📈 **[质量报告中心](reports/)** - 测试覆盖率和质量指标
- 👥 **[团队协作指南](../CLAUDE.md)** - 团队沟通和协作规范
- 🎯 **[项目路线图](../.spec-workflow/specs/)** - 长期规划和里程碑

### 🔧 测试工程师导航
*面向质量保证和测试工作 - 基于文档分析25个测试相关文档优化*

#### 📋 核心测试标准 (测试工程师必备)
- [**测试架构标准**](development/test-architecture-standard.md) - 测试分层、标准规范 ⭐
- [**测试运行指南**](development/testing-guide.md) - VS2022/CLI测试、覆盖率分析 ⭐
- [**架构测试指南**](architecture/testing/architecture-testing-guide.md) - 架构约束验证 ⭐
- [**质量报告中心**](reports/) - 测试覆盖率、质量指标分析 ⭐
- [**测试环境配置**](development/testing-guide.md) - 测试环境搭建和管理 ⭐

#### 🧪 测试设计与策略 (15个文档)
- [**测试用例设计**](development/test-architecture-standard.md) - 测试用例编写规范
- [**自动化测试**](development/testing-guide.md) - 自动化测试框架和工具
- [**性能测试**](development/) - 性能测试策略和工具
- [**安全测试**](security/) - 安全漏洞测试和验证
- [**集成测试**](architecture/testing/) - 系统集成测试策略

#### 🔍 测试执行与监控
- [**覆盖率分析**](reports/test-coverage-improvement-report.md) - 代码覆盖率详细分析
- [**测试报告生成**](reports/) - 测试结果报告模板
- [**持续集成测试**](development/testing-guide.md) - CI/CD集成测试流程
- [**缺陷管理**](development/) - Bug跟踪和管理流程
- [**测试数据管理**](development/) - 测试数据生成和管理

#### 📊 质量保证与改进
- [**代码质量标准**](development/README.md) - 代码质量和审查标准
- [**架构合规检查**](architecture/testing/architecture-testing-guide.md) - 架构约束验证
- [**质量度量指标**](reports/) - 质量指标定义和跟踪
- [**测试流程优化**](development/testing-guide.md) - 测试流程持续改进
- [**测试工具链**](development/) - 测试工具选型和配置

#### 🔄 测试工作流程
- **🧪 测试规划** → [需求分析](../.spec-workflow/specs/) → [测试策略](development/test-architecture-standard.md) → [资源计划](development/testing-guide.md) → [环境准备](deployment/)
- **🔍 测试设计** → [用例设计](development/test-architecture-standard.md) → [测试数据](development/) → [自动化脚本](development/testing-guide.md) → [评审确认](development/)
- **⚡ 测试执行** → [单元测试](development/testing-guide.md) → [集成测试](architecture/testing/) → [系统测试](development/) → [验收测试](reports/)
- **📊 质量评估** → [覆盖率分析](reports/test-coverage-improvement-report.md) → [缺陷分析](reports/) → [质量报告](reports/) → [改进建议](development/)
- **🔄 持续改进** → [流程优化](development/testing-guide.md) → [工具升级](development/) → [技能提升](development/) → [经验分享](../CLAUDE.md)

#### ⚡ 快速访问 (测试工程师常用)
- 🧪 **[测试运行指南](development/testing-guide.md)** - 测试执行和工具使用
- 📊 **[覆盖率分析报告](reports/test-coverage-improvement-report.md)** - 代码覆盖率详情
- 🏗️ **[架构测试验证](architecture/testing/architecture-testing-guide.md)** - 架构合规性检查
- 📋 **[测试架构标准](development/test-architecture-standard.md)** - 测试设计规范
- 📈 **[质量报告中心](reports/)** - 测试结果和质量指标
- 🛠️ **[测试工具配置](development/testing-guide.md)** - 测试环境搭建

---

## 🎯 任务类型导航

*基于实际工作流程优化，为每种任务类型提供详细的执行指导和资源链接*

### 🚀 开发新功能
**完整流程**：Spec-Workflow + 技术标准 + 开发实施 - *基于文档分析优化*

#### 📋 Phase 1: 需求分析 (1-2天)
- **🎯 创建需求文档** → [需求文档模板](../.spec-workflow/templates/requirements-template.md)
- **📊 功能分析** → [项目状态文档](PROJECT-STATUS-2025-09-27.md) + [现有模块清单](architecture/modules/README.md)
- **👥 角色协作** → 开发者主导，架构师技术评审，项目经理需求确认

#### 🏗️ Phase 2: 设计规划 (2-3天)
- **📐 技术设计** → [设计文档模板](../.spec-workflow/templates/design-template.md)
- **🔧 架构评审** → [Server设计标准](architecture/server-module-design-standard.md) + [Client设计标准](architecture/client/unified-design-standard.md)
- **🧪 测试规划** → [测试架构标准](development/test-architecture-standard.md)

#### 📝 Phase 3: 任务分解 (0.5天)
- **✨ 任务清单** → [任务模板](../.spec-workflow/templates/tasks-template.md)
- **🔗 GitHub创建** → 基于任务清单创建GitHub Issues，关联标签和里程碑
- **📊 工作量评估** → 结合[项目状态](PROJECT-STATUS-2025-09-27.md)评估开发资源

#### 🛠️ Phase 4: 开发实施 (按任务复杂度)
- **🏗️ Server端开发** → [Server模块设计标准](architecture/server-module-design-standard.md) + [三层架构指南](architecture/)
- **💻 Client端开发** → [Client端MVVM标准](architecture/client/unified-design-standard.md) + [依赖注入指南](development/repository-dependency-injection-guide.md)
- **🧪 测试开发** → [测试运行指南](development/testing-guide.md) + [覆盖率分析](reports/test-coverage-improvement-report.md)

#### ✅ Phase 5: 验证与交付 (1-2天)
- **🔍 代码审查** → [架构测试指南](architecture/testing/architecture-testing-guide.md)
- **🚀 部署验证** → [部署指南](deployment/) + [API文档更新](api/README.md)
- **📝 文档同步** → 更新[模块文档](architecture/modules/)和[导航索引](#-完整文档索引)

---

### 🐛 修复Bug
**快速响应**：问题定位 → 根因分析 → 解决实施 → 预防措施 - *基于文档分析优化*

#### 🔍 Phase 1: 问题定位 (0.5-1天)
- **📊 问题复现** → [测试运行指南](development/testing-guide.md) + [日志分析](deployment/)
- **🎯 影响评估** → 检查[架构约束](architecture/testing/architecture-testing-guide.md)和[业务影响]
- **🔍 根因分析** → 结合[开发规范](development/README.md)分析代码问题

#### 🛠️ Phase 2: 解决方案设计 (0.5天)
- **💡 解决方案** → 基于现有[架构标准](architecture/)设计修复方案
- **⚡ 快速修复** → 简单Bug直接修复，复杂Bug需设计方案
- **🧪 测试策略** → [测试架构标准](development/test-architecture-standard.md)制定回归测试

#### 🔧 Phase 3: 实施与验证 (1-2天)
- **🏗️ 代码修复** → 遵循[编码规范](development/README.md)实施修复
- **🧪 测试验证** → [测试运行指南](development/testing-guide.md) + [覆盖率分析](reports/)
- **📋 代码审查** → [架构测试](architecture/testing/)确保修复不影响系统架构

#### 📊 Phase 4: 预防与总结 (0.5天)
- **🛡️ 预防措施** → 更新[最佳实践](development/)和[检查清单]
- **📝 知识沉淀** → 更新相关[模块文档](architecture/modules/)和[故障排查指南]
- **🔄 流程改进** → 优化[开发工作流](development/ai-assisted-automation-workflow.md)

---

### 🏗️ 架构设计
**系统思维**：架构决策 → 技术选型 → 设计实施 → 架构验证 - *基于文档分析优化*

#### 📐 Phase 1: 架构分析 (1-2天)
- **🎯 需求理解** → [架构设计标准](architecture/README.md) + [系统架构总览](architecture/README.md)
- **🔍 现状分析** → [现有架构决策](../.spec-workflow/specs/) + [模块化现状](architecture/modules/README.md)
- **📊 技术债务** → [架构测试](architecture/testing/)评估现有架构约束

#### 🏛️ Phase 2: 架构决策 (1-2天)
- **📋 ADR流程** → [ADR决策记录](architecture/ADR-003-server-module-unified-design.md)
- **🔧 技术选型** → [项目技术文档](../.spec-workflow/steering/tech.md) + [最佳实践](development/)
- **🎯 架构原则** → 三层架构、模块化设计、依赖方向控制

#### 📝 Phase 3: 设计实施 (2-3天)
- **🏗️ 架构设计** → [设计文档模板](../.spec-workflow/templates/design-template.md)
- **📋 接口设计** → [API设计规范](api/README.md) + [模块接口标准](architecture/)
- **🧪 架构验证** → [架构测试指南](architecture/testing/architecture-testing-guide.md)

#### ✅ Phase 4: 评审与确认 (1天)
- **👥 技术评审** → 架构师主导，开发者参与，项目经理确认
- **📊 影响评估** → 评估对现有系统和开发流程的影响
- **🔄 持续优化** → 建立[架构演进路线图](../.spec-workflow/specs/)

---

### 📝 维护文档
**持续改进**：文档更新 → 质量检查 → 知识管理 → 流程优化 - *基于文档分析优化*

#### 📝 Phase 1: 文档更新 (按需)
- **🔄 变更检测** → [文档指南](development/documentation-guidelines.md)识别需要更新的文档
- **📋 内容更新** → 基于[代码变更](../CLAUDE.md)同步更新相关文档
- **🔗 链接维护** → 检查和修复文档间的引用链接

#### 🔍 Phase 2: 质量检查 (定期)
- **🛠️ 健康检查** → [健康检查脚本](scripts/documentation-maintenance/) + [链接验证](development/)
- **📊 质量评估** → [文档质量标准](development/documentation-guidelines.md)评估文档完整性
- **🎯 用户体验** → 收集反馈，优化[导航结构](#-角色导航)

#### 📚 Phase 3: 知识管理 (持续)
- **🗂️ 归档管理** → [项目档案](../.spec-workflow/archive/) + [报告归档](reports/)
- **🔍 知识检索** → 优化[搜索体验](#-搜索和帮助)和[分类索引](#-完整文档索引)
- **📖 培训支持** → 基于[新用户指南](#-新用户快速开始)制作培训材料

#### 🔄 Phase 4: 流程优化 (持续)
- **⚡ 效率提升** → 基于[AI辅助工作流](development/ai-assisted-automation-workflow.md)优化维护流程
- **📊 指标监控** → 跟踪[成功指标](#-设计原则)和用户满意度
- **🎯 持续改进** → 基于[GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)反馈持续优化

---

## 📚 完整文档索引

### 🏗️ 架构与设计 (docs/)
- [architecture/](architecture/README.md) - 系统架构设计文档集合
- [api/](api/README.md) - API接口规范与文档
- [架构决策记录](../.spec-workflow/specs/) - 项目设计文档和决策历史

### 🛠️ 开发与质量 (docs/)
- [development/](development/README.md) - 开发规范指导集合
- [testing/](development/testing-guide.md) - 测试运行指南和最佳实践
- [security/](security/) - 安全指导文档
- [deployment/](deployment/) - 部署与配置指南

### 📊 项目管理 (跨体系)
- [GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues) - 需求与任务单一事实源
- [tasks/](tasks/) - 任务管理系统
- [reports/](reports/) - 分析报告文档
- [项目规格](../.spec-workflow/specs/) - 需求分析和设计文档
- [项目档案](../.spec-workflow/archive/) - 已完成项目文档

### 🔧 开发资源 (源码文档)
- [src/](../src/) - 源码目录结构
  - [Server/](../src/Server/) - 后端项目文档
  - [Client/Desktop/](../src/Client/Desktop/) - 前端项目文档
  - [Shared/](../src/Shared/) - 共享层文档

---

## 🆕 新用户快速开始

### 🎯 5分钟快速上手
1. **了解项目** → [项目README](../README.md) (1分钟)
2. **掌握规范** → [CLAUDE.md](../CLAUDE.md) (2分钟)
3. **环境配置** → [开发者指南](DEVELOPER_GUIDE.md) (1分钟)
4. **选择导航** → 根据你的角色选择上方角色导航 (1分钟)

### 🎮 根据你的角色开始
- **我是开发者** → 🛠️ [开发者导航](#-️-开发者导航)
- **我是架构师** → 🏗️ [架构师导航](#️-架构师导航)
- **我是项目经理** → 📊 [项目经理导航](#-项目-经理导航)
- **我是测试工程师** → 🔧 [测试工程师导航](#-测试工程师导航)

---

## ⚡ 搜索和帮助

### 🔍 快速查找
- **按文档类型** → 查看[完整文档索引](#-完整文档索引)
- **按任务类型** → 查看[任务类型导航](#-任务类型导航)
- **按角色查找** → 查看[角色导航](#-角色导航)
- **关键词搜索** → 使用IDE/Git的全文搜索功能

### 🆘 获取帮助
- **文档问题** → 在[GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)提交文档改进建议
- **开发问题** → 查看相关开发规范或联系技术负责人
- **流程问题** → 查看[CLAUDE.md](../CLAUDE.md)中的工作流程定义

---

## 📈 文档体系说明

### 🎯 设计原则
- **统一入口**：docs/index.md作为唯一文档入口
- **角色导向**：按用户角色组织导航，提高查找效率
- **任务驱动**：根据具体任务提供相关文档和工作流
- **双轨整合**：无缝连接docs/和spec-workflow/两个体系

### 📊 成功指标
- ✅ **3次点击内**找到任何需要的文档
- ✅ **5分钟内**理解导航结构
- ✅ **消除标准错乱**问题
- ✅ **统一文档门户**成为团队主要入口

### 🔄 维护机制
- **实时更新**：文档变更时同步更新导航
- **定期审查**：每月检查链接有效性和内容准确性
- **持续优化**：根据用户反馈不断改进导航体验

---

## 🔗 相关资源

- [项目Git仓库](https://github.com/shouqitao/LYBTZYZS) - 代码和文档版本管理
- [Spec-Workflow Dashboard](http://localhost:3000) - 规格文档审批和管理
- [API在线文档](http://localhost:5001/swagger) - 开发环境API交互界面
- [文档系统架构](DOCUMENTATION_SYSTEM.md) - 本导航系统的设计理念

---

*本统一文档导航门户旨在解决"开发时标准错乱"问题。如果在使用过程中遇到任何问题或有改进建议，请通过GitHub Issues反馈。*

**最后更新：2025-10-15 - 统一文档导航门户v3.0正式上线** 🎉