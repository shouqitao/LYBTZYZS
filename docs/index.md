# 凌隐宝堂文档中心

**文档版本**：v4.0 对齐架构版
**创建时间**：2025-09-25
**最后更新**：2025-10-15（Server/Client对齐架构，代码文档并行）
**维护负责**：Claude Code + Thinker

## 🎯 文档体系架构

项目采用**双轨文档体系**，专注于当下可用的实用信息：

| 体系 | 用途 | 内容类型 | 维护方式 |
|------|------|----------|----------|
| **docs/** | 开发标准和指南 | 架构标准、开发规范、快速参考 | 人工维护，长期参考 |
| **spec-workflow/** | 项目决策和规格 | 需求分析、设计记录、项目规格 | 审批流程，有生命周期 |

## 🚀 快速参考中心 (Level 1)

**解决80%日常需求** - 精简文档，加载飞快

| 文档 | 大小 | 用途 | 快速访问 |
|------|------|------|----------|
| **[API快速参考](quick-reference/api_reference.md)** | 30KB | 最常用API和调用示例 | 查接口 |
| **[配置模板](quick-reference/config_templates.md)** | 17KB | 常用配置文件模板 | 找配置 |
| **[代码模式](quick-reference/code_patterns.md)** | 19KB | 常用代码模式和模板 | 学模式 |
| **[问题解决](quick-reference/troubleshooting.md)** | 46KB | 常见问题和解决方案 | 解问题 |
| **[开发清单](quick-reference/development_checklist.md)** | 7KB | 开发流程和质量检查 | 做检查 |

👉 **[查看完整快速参考文档中心](quick-reference/README.md)**

---

## 👥 核心导航

### 🛠️ 开发者导航

#### 📋 必读核心标准
- **[Server端设计标准](architecture/server/design-standard.md)** - 三层架构、接口规范 ⭐v4.0对齐
- **[Client端设计标准](architecture/client/unified-design-standard.md)** - MVVM架构、依赖注入
- **[测试架构标准](development/shared/test-architecture-standard.md)** - 测试分层、标准规范 ⭐v4.0对齐
- **[CLAUDE.md](../CLAUDE.md)** - 开发约束、执行原则

#### 🔧 开发工具和流程
- **[测试运行指南](development/shared/testing-guide.md)** - VS2022/CLI测试、覆盖率分析 ⭐v4.0对齐
- **[依赖注入指南](development/shared/repository-dependency-injection-guide.md)** - Repository统一DI配置 ⭐v4.0对齐
- **[文档编写指南](development/shared/documentation-guidelines.md)** - 文档质量标准 ⭐v4.0对齐

#### 📡 API与接口
- **[API接口文档](api/README.md)** - RESTful接口、Swagger文档
- **[在线API文档](http://localhost:5001/swagger)** - 开发环境API交互界面

### 🏗️ 架构师导航

#### 📋 对齐架构核心文档 (Server/Client/Shared三层) ⭐v4.0
- **[架构总览](architecture/README.md)** - 对齐架构设计原理与导航 ⭐ 核心入口
- **[Server端架构](architecture/server/README.md)** - 三层架构、模块设计、服务标准 ⭐
- **[Client端架构](architecture/client/README.md)** - MVVM架构、依赖注入、UI标准 ⭐
- **[共享架构](architecture/shared/README.md)** - 跨端ADR、技术决策、测试标准 ⭐

#### 🎯 架构实施指南
- **[Server设计标准](architecture/server/design-standard.md)** - 三层架构详细规范 ⭐ 必读
- **[Client设计标准](architecture/client/unified-design-standard.md)** - MVVM架构详细规范 ⭐ 必读
- **[Server模块模板](architecture/server/module-template/)** - 服务端模块开发脚手架
- **[Client模块模板](architecture/client/module-template/)** - 客户端模块开发脚手架

#### 🧪 架构质量保证
- **[架构测试指南](architecture/shared/testing/architecture-testing-guide.md)** - 15条架构约束验证
- **[ADR决策记录](architecture/shared/adr/)** - 重要架构决策完整记录
- **[技术决策文档](architecture/shared/decisions/)** - 关键技术选型说明

### 📊 项目管理导航

#### 📋 核心管理资源
- **[项目README](../README.md)** - 系统架构、技术栈、当前状态
- **[GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)** - 需求与任务单一事实源
- **[CLAUDE.md](../CLAUDE.md)** - 团队协作、工作流程、执行原则

#### 🔄 项目管理
- **[需求模板](../.spec-workflow/templates/requirements-template.md)** - 需求分析模板
- **[设计模板](../.spec-workflow/templates/design-template.md)** - 设计文档模板
- **[任务模板](../.spec-workflow/templates/tasks-template.md)** - 任务分解模板

### 🔧 测试工程师导航

#### 📋 核心测试标准
- **[测试架构标准](development/shared/test-architecture-standard.md)** - 测试分层、标准规范 ⭐v4.0对齐
- **[测试运行指南](development/shared/testing-guide.md)** - VS2022/CLI测试、覆盖率分析 ⭐v4.0对齐
- **[架构测试指南](architecture/shared/testing/architecture-testing-guide.md)** - 架构约束验证 ⭐v4.0对齐

---

## 🎯 常用工作流程

### 🚀 开发新功能
1. **需求分析** → [需求模板](../.spec-workflow/templates/requirements-template.md)
2. **技术设计** → [设计模板](../.spec-workflow/templates/design-template.md)
3. **任务分解** → [任务模板](../.spec-workflow/templates/tasks-template.md)
4. **开发实施** → [Server设计标准](architecture/server/design-standard.md) + [Client设计标准](architecture/client/unified-design-standard.md) ⭐v4.0对齐
5. **测试验证** → [测试运行指南](development/shared/testing-guide.md) ⭐v4.0对齐

### 🐛 修复Bug
1. **问题定位** → [问题解决](quick-reference/troubleshooting.md)
2. **解决方案** → 遵循现有[架构标准](architecture/)
3. **测试验证** → [测试运行指南](development/shared/testing-guide.md) ⭐v4.0对齐
4. **代码审查** → [架构测试](architecture/shared/testing/) ⭐v4.0对齐

### 🏗️ 架构设计
1. **需求理解** → [架构设计标准](architecture/README.md)
2. **架构决策** → [ADR决策记录](architecture/shared/adr/) ⭐v4.0对齐
3. **设计实施** → [设计模板](../.spec-workflow/templates/design-template.md)
4. **架构验证** → [架构测试指南](architecture/shared/testing/architecture-testing-guide.md) ⭐v4.0对齐

### 🔄 代码与文档并行开发 (v4.0核心要求)

#### ⚡ 文档同步工作流
1. **影响评估** → 实施前分析需要更新的文档清单
2. **同步开发** → 代码变更与文档更新并行进行
3. **即时验证** → 提交前检查文档同步完成度
4. **PR审查** → 代码审查包含文档同步检查

#### 📋 文档同步检查清单 (强制要求)
- [ ] 架构文档是否反映最新代码结构
- [ ] 开发指南是否包含最新流程
- [ ] API文档是否与实际接口一致
- [ ] 快速参考是否包含新增内容
- [ ] 导航链接是否有效正确
- [ ] 所有README是否已更新

#### 🎯 并行开发原则
- **强制同步**：代码变更后必须立即更新文档，不允许滞后
- **路径一致性**：所有文档引用必须使用对齐架构路径
- **完整性保证**：影响范围内的文档必须全部更新

---

## 📚 完整文档索引

### 🚀 三层文档架构

#### Level 1: 快速参考 (80%日常需求)
- **[quick-reference/](quick-reference/README.md)** - 快速参考文档中心
  - [API快速参考](quick-reference/api_reference.md) - 20个常用API和示例
  - [配置模板](quick-reference/config_templates.md) - 15个配置文件模板
  - [代码模式](quick-reference/code_patterns.md) - 18个常用代码模式
  - [问题解决](quick-reference/troubleshooting.md) - 25个常见问题解决方案
  - [开发清单](quick-reference/development_checklist.md) - 12个检查清单

#### Level 2: 实践指南 (15%学习需求)
- **[architecture/](architecture/README.md)** - 系统架构设计文档集合
- **[development/](development/README.md)** - 开发规范指导集合

#### Level 3: 深度参考 (5%深度需求)
- **[api/](api/README.md)** - API接口规范与文档
- **[项目规格](../.spec-workflow/specs/)** - 需求分析和设计文档

### 📊 项目管理 (跨体系)
- **[GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)** - 需求与任务单一事实源
- **[项目档案](../.spec-workflow/archive/)** - 已完成项目文档

### 🔧 开发资源 (源码文档)
- **[src/](../src/)** - 源码目录结构
  - **[Server/](../src/Server/)** - 后端项目文档
  - **[Client/Desktop/](../src/Client/Desktop/)** - 前端项目文档
  - **[Shared/](../src/Shared/)** - 共享层文档

---

## 🆕 新用户快速开始

### 🎯 5分钟快速上手
1. **了解项目** → [项目README](../README.md) (1分钟)
2. **掌握规范** → [CLAUDE.md](../CLAUDE.md) (2分钟)
3. **查看快速参考** → [快速参考中心](quick-reference/README.md) (1分钟)
4. **选择导航** → 根据你的角色选择上方角色导航 (1分钟)

### 🎮 根据你的角色开始
- **我是开发者** → 🛠️ [开发者导航](#️-️-开发者导航)
- **我是架构师** → 🏗️ [架构师导航](#️-架构师导航)
- **我是项目经理** → 📊 [项目管理导航](#-项目-经理导航)
- **我是测试工程师** → 🔧 [测试工程师导航](#-测试工程师导航)

---

## ⚡ 搜索和帮助

### 🔍 快速查找
- **按需求查找** → 查看[快速参考中心](quick-reference/README.md)
- **按任务查找** → 查看[常用工作流程](#-常用工作流程)
- **按角色查找** → 查看[核心导航](#-核心导航)
- **深度查找** → 查看[完整文档索引](#-完整文档索引)

### 🆘 获取帮助
- **文档问题** → 在[GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)提交文档改进建议
- **开发问题** → 查看相关开发规范或联系技术负责人
- **流程问题** → 查看[CLAUDE.md](../CLAUDE.md)中的工作流程定义

---

## 📈 文档体系说明

### 🎯 设计原则
- **统一入口**：docs/index.md作为唯一文档入口
- **精简优先**：删除历史包袱，专注当下可用
- **三层架构**：Level 1快速参考 (80%) + Level 2实践指南 (15%) + Level 3深度参考 (5%)
- **双轨整合**：无缝连接docs/和spec-workflow/两个体系

### 📊 成功指标
- ✅ **3次点击内**找到任何需要的文档
- ✅ **5分钟内**理解导航结构
- ✅ **消除标准错乱**问题
- ✅ **文档大小 < 100KB**，加载速度 < 2秒
- ✅ **历史包袱清零**，专注当下可用

### 🚀 三层架构优势
- **Level 1快速参考**：解决80%日常需求，加载飞快，查找便捷
- **Level 2实践指南**：提供深度学习，结构化指导
- **Level 3深度参考**：完整技术文档，详细参考信息

### 🔄 维护机制
- **实时更新**：文档变更时同步更新导航
- **按需补充**：没有的内容等需要时再补充
- **定期审查**：每月检查链接有效性和内容准确性
- **保持精简**：定期清理历史包袱，确保文档轻量化

---

## 🔗 相关资源

- [项目Git仓库](https://github.com/shouqitao/LYBTZYZS) - 代码和文档版本管理
- [Spec-Workflow Dashboard](http://localhost:3000) - 规格文档审批和管理
- [API在线文档](http://localhost:5001/swagger) - 开发环境API交互界面

---

*本文档中心专注于当下可用的实用信息，删除了所有历史包袱。如果在使用过程中遇到任何问题或有改进建议，请通过GitHub Issues反馈。*

**最后更新：2025-10-15 - 对齐架构v4.0正式上线** 🎉