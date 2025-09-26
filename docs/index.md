# LYBTZYZS 文档中心

**文档版本**：v1.0  
**创建时间**：2025-09-25  
**维护负责**：Claude Code + Thinker  
**关联文档**：[项目README](../README.md), [开发者指导](DEVELOPER_GUIDE.md)

## 🎯 文档系统概览

本文档中心采用**三层架构**设计，为开发者提供从项目概览到实施细节的完整导航路径：

### 📋 L1: 项目总览层（必读）
| 文档 | 说明 | 维护者 |
|------|------|--------|
| [**README.md**](../README.md) | 📌 **项目权威总览** - 系统架构、技术栈、当前状态 | Thinker |
| [**CLAUDE.md**](../CLAUDE.md) | 📌 **Claude Code开发约束** - 技术限制、开发规范、禁止事项 | Claude Code |
| [**DEVELOPER_GUIDE.md**](DEVELOPER_GUIDE.md) | 📌 **开发者统一入口** - 快速开始、环境配置、常用任务 | Claude Code |

### 🏗 L2: 领域专题层（按需查阅）

#### 架构与设计
| 文档路径 | 说明 | 关键内容 |
|----------|------|----------|
| [architecture/](architecture/README.md) | 系统架构设计文档集合 | ADR决策记录、UltraThink架构、多租户讨论 |
| [api/](api/README.md) | API接口规范与文档 | RESTful接口、Swagger文档、认证授权 |

#### 开发与质量
| 文档路径 | 说明 | 关键内容 |
|----------|------|----------|
| [development/](development/README.md) | 开发规范指导集合 | 编码标准、测试指南、审计字段、枚举规范 |
| [prd/](prd/README.md) | 产品需求文档管理 | PRD模板、需求流程、变更管理 |

#### 任务与交付
| 文档路径 | 说明 | 关键内容 |
|----------|------|----------|
| [tasks/](tasks/) | 任务管理系统 | pending/待办任务、completed/完成总结 |
| [reports/](reports/) | 分析报告文档 | 架构分析、性能报告、完成总结 |

### 🔧 L3: 实施细节层（开发时查阅）

#### 源码文档结构
```
src/
├── Server/                  # 后端项目文档
│   ├── README.md           # Server层总览
│   ├── Core/               # 核心基础设施文档  
│   ├── Modules/            # 各业务模块文档
│   └── Services/           # API服务层文档
├── Client/Desktop/         # 前端项目文档
│   ├── README.md          # Desktop层总览
│   ├── Shell/             # 应用程序壳文档
│   ├── Core/              # 前端基础设施文档
│   └── Modules/           # 各UI模块文档
└── Shared/                 # 共享层文档
    ├── README.md          # 共享层总览
    ├── Models/            # DTO模型文档
    └── Interfaces/        # API接口文档
```

## 🚀 快速导航路径

### 🆕 新开发者入门
1. [项目README](../README.md) → 了解项目全貌
2. [CLAUDE.md](../CLAUDE.md) → 掌握开发约束  
3. [开发者指导](DEVELOPER_GUIDE.md) → 环境配置和开发实践
4. [开发规范](development/README.md) → 编码标准和最佳实践

### 🔧 日常开发任务
1. [API开发](api/README.md) → 接口设计和实现
2. [架构参考](architecture/README.md) → 设计决策和模式
3. [任务管理](tasks/) → 当前任务和优先级
4. [测试指导](development/testing-best-practices.md) → 质量保证

### 📊 项目管理视角
1. [PRD管理](prd/README.md) → 需求规格和变更
2. [任务跟踪](tasks/) → 进度和完成状态
3. [分析报告](reports/) → 项目健康度和改进建议
4. [架构治理](architecture/) → 技术债务和优化方向

## 📋 文档标准说明

### 维护责任分工
| 责任范围 | 负责角色 | 主要文档类型 |
|----------|----------|--------------|
| **架构决策、需求管理** | Thinker | README.md, architecture/, prd/, tasks/pending/ |
| **开发实现、技术文档** | Claude Code | CLAUDE.md, development/, api/, src/*/README.md |
| **完成总结、实施报告** | Claude Code | tasks/completed/, reports/ |

### 文档更新触发规则
- **架构变更** → 必须更新architecture/和根README.md
- **API变更** → 必须更新api/README.md和Swagger文档
- **模块重构** → 必须更新相关src/*/README.md
- **任务完成** → 必须在tasks/completed/创建总结文档

### 文档质量标准
每个README文件必须包含：
- **元信息**：版本、创建时间、维护负责人
- **功能说明**：范围、目标、关键特性
- **导航链接**：上级文档、平级文档、下级文档  
- **维护规则**：更新条件、归档策略

## 🛠 文档工具和自动化

### 文档验证脚本（规划中）
```powershell
# 文档系统健康检查
.\scripts\DocumentationHealthCheck.ps1
- 检查README完整性和标准符合度
- 验证交叉引用链接有效性
- 识别过期文档（超过30天未更新）
- 生成文档覆盖率报告
```

### 自动化生成
- **API文档**：通过Swagger/OpenAPI自动导出
- **代码文档**：通过XML注释和工具生成  
- **依赖图**：通过分析工具生成项目依赖关系

## ⚠️ 重要提醒

### 文档即标准
📌 **本文档系统不仅是说明，更是开发工作的权威标准和限制**：
- README中的技术约束具有**强制执行力**
- 架构决策文档是设计变更的**必要依据**
- 开发规范是代码审查的**硬性标准**

### 实时性要求
📅 **文档必须与代码保持同步**：
- 代码变更时必须同时更新相关文档
- 超过7天未更新的文档将被标记为可能过期
- 断链和错误引用将影响文档系统的可信度

### 贡献要求
🤝 **每个开发者都是文档系统的维护者**：
- 发现文档错误时有责任修正或报告
- 实施新功能时必须更新相关技术文档
- 完成任务后必须提交完成总结和经验沉淀

---

## 🔗 相关资源

- [文档系统架构设计](DOCUMENTATION_SYSTEM.md) - 本系统的设计理念和实施计划
- [项目Git仓库](https://github.com/shouqitao/LYBTZYZS) - 代码和文档的版本管理
- [API在线文档](http://localhost:5001/swagger) - 开发环境API交互界面

---

*本文档索引将随着项目发展持续更新，确保文档导航的完整性和准确性。如需增加新的文档分类或修改导航结构，请遵循文档系统变更流程。*