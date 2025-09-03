# 项目文档标准规范

**目标**: 为每个project创建对应的文档，确保代码按照文档的稳定要求开发

## 📋 文档驱动开发原则

### 核心原则
1. **文档有要求的代码得有** - 所有文档中描述的功能必须在代码中实现
2. **文档没要求的不增加代码** - 避免过度设计和功能蔓延  
3. **需要增加功能的先有文档再有代码** - 任何新功能必须先完善文档设计

### 文档作为开发契约
- 每个项目文档就是该项目的**开发契约**
- 代码必须严格按照文档要求实现
- 文档变更必须经过评审确认
- 保持文档与代码的同步更新

## 🗂️ 项目文档结构

### 文档目录组织
```
docs/projects/
├── backend/                    # 后端项目文档
│   ├── core/                  # 核心基础设施
│   │   ├── infrastructure.md  # LYBT.Infrastructure
│   │   ├── entities.md        # LYBT.Entities  
│   │   └── webapi.md          # LYBT.WebAPI
│   └── modules/               # 8个业务模块
│       ├── auth.md            # LYBT.Module.Auth
│       ├── users.md           # LYBT.Module.Users
│       ├── patients.md        # LYBT.Module.Patients
│       ├── medicalcase.md     # LYBT.Module.MedicalCase
│       ├── consultation.md    # LYBT.Module.Consultation
│       ├── prescriptions.md   # LYBT.Module.Prescriptions
│       ├── herbs.md           # LYBT.Module.Herbs
│       └── formula.md         # LYBT.Module.Formula
├── frontend/                   # 前端项目文档
│   ├── core/                  # 核心项目
│   │   ├── desktop-core.md    # LYBT.Desktop.Core
│   │   ├── desktop-infrastructure.md # LYBT.Desktop.Infrastructure
│   │   ├── desktop-services.md # LYBT.Desktop.Services
│   │   └── desktop-shell.md   # LYBT.Desktop.Shell
│   ├── modules/               # 前端业务模块
│   │   ├── desktop-auth.md    # LYBT.Desktop.Auth
│   │   ├── desktop-consultation.md # LYBT.Desktop.Consultation
│   │   ├── desktop-formula.md # LYBT.Desktop.Formula
│   │   ├── desktop-herbs.md   # LYBT.Desktop.Herbs
│   │   ├── desktop-medicalcase.md # LYBT.Desktop.MedicalCase
│   │   ├── desktop-patients.md # LYBT.Desktop.Patients
│   │   ├── desktop-prescriptions.md # LYBT.Desktop.Prescriptions
│   │   └── desktop-users.md   # LYBT.Desktop.Users
│   └── workbenches/           # 工作台项目
│       ├── workbench-core.md  # LYBT.Desktop.Workbench.Core
│       ├── consultation-workbench.md # LYBT.Desktop.Workbench.Consultation
│       └── admin-workbench.md # LYBT.Desktop.Workbench.Admin
├── shared/                     # 共享项目文档
│   ├── shared-models.md       # LYBT.Shared.Models
│   ├── shared-interfaces.md   # LYBT.Shared.Interfaces
│   └── shared-utilities.md    # LYBT.Shared.Utilities
└── PROJECT_INDEX.md           # 项目文档索引
```

## 📝 项目文档模板

### 标准文档结构
每个项目文档必须包含以下章节：

```markdown
# [项目名称] 项目文档

## 📋 项目概述
- 项目职责和作用
- 在系统中的位置
- 关键业务价值

## 🏗️ 技术架构
- 项目架构设计
- 核心技术栈
- 依赖项目列表
- 设计模式采用

## 🎯 功能规范
- 必须实现的功能清单
- 接口定义规范
- 数据模型定义
- 业务规则约束

## 📋 开发规范
- 代码结构要求
- 命名规范
- 质量标准
- 测试要求

## 🔌 集成接口
- 对外提供的接口
- 依赖的外部接口
- 数据传输格式
- 错误处理规范

## ⚙️ 配置管理
- 配置项定义
- 环境变量要求
- 部署配置说明

## 🧪 测试规范
- 单元测试要求
- 集成测试要求
- 测试覆盖率目标
- 测试数据准备

## 🚀 部署说明
- 构建要求
- 部署步骤
- 环境依赖
- 运行监控

## 📚 相关文档
- 相关项目文档链接
- API文档链接
- 技术规范引用
```

## 🎯 文档质量要求

### 内容完整性
- ✅ 项目职责清晰定义
- ✅ 技术架构详细说明  
- ✅ 功能需求完整覆盖
- ✅ 接口规范明确定义
- ✅ 开发规范具体可执行

### 文档准确性
- ✅ 与实际代码保持同步
- ✅ 技术细节准确无误
- ✅ 版本信息及时更新
- ✅ 示例代码可执行

### 可操作性
- ✅ 开发人员能够按文档实现
- ✅ 测试人员能够按文档测试
- ✅ 运维人员能够按文档部署
- ✅ 新人能够按文档上手

## 🔄 文档维护流程

### 创建新项目文档
1. 复制标准模板
2. 填写项目特定内容
3. 技术评审确认
4. 更新项目索引

### 修改现有文档
1. 提出修改需求
2. 评估影响范围
3. 更新文档内容
4. 同步代码实现

### 文档质量检查
- **每周检查**: 文档与代码同步性
- **每月评审**: 文档完整性和准确性
- **季度更新**: 技术栈和架构变化

## 🏆 项目分类标准

### 后端项目分类

#### 核心基础设施项目
- **Infrastructure**: 数据访问、缓存、安全、配置管理
- **Entities**: 数据模型和实体定义
- **WebAPI**: Web服务入口和控制器

#### 业务模块项目
- 采用UltraThink双层架构标准
- 包含QueryService + BusinessService
- 统一的模块注册和依赖注入
- 标准的AutoMapper配置

### 前端项目分类

#### 核心基础项目
- **Core**: 基础服务、控件、ViewModels
- **Infrastructure**: HTTP客户端、主题、基础设施
- **Services**: 业务服务层和API适配器
- **Shell**: 主程序和应用程序入口

#### 业务模块项目
- 采用MVVM模式
- Prism模块化架构
- 统一的事件聚合和导航
- 标准的依赖注入配置

### 共享项目分类
- **Models**: 数据传输对象和枚举定义
- **Interfaces**: 服务接口和API契约
- **Utilities**: 通用工具类和帮助方法

## ✅ 实施检查清单

### 项目文档创建完成标准
- [ ] 项目概述清晰准确
- [ ] 技术架构完整说明
- [ ] 功能规范详细定义
- [ ] 开发规范具体可执行
- [ ] 接口定义标准化
- [ ] 测试要求明确
- [ ] 部署说明完整
- [ ] 相关文档关联完善

### 文档质量验证
- [ ] 开发人员能按文档编码
- [ ] 测试人员能按文档测试  
- [ ] 新人能按文档快速上手
- [ ] 文档内容与代码同步
- [ ] 所有链接有效可访问

---

**标准版本**: v1.0  
**生效日期**: 2025-09-01  
**维护者**: UltraThink项目组  
**更新原则**: 文档驱动开发，代码严格按文档实现