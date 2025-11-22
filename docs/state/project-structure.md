# 项目结构与组织指南 (Project Structure & Organization Guide)

> **版本**: 1.0  
> **创建日期**: 2025-10-15  
> **迁移日期**: 2025-11-09（从.spec-workflow/steering/迁移）
> **来源**: 基于现有3.0文档整合  
> **维护**: 项目团队  
> **关联**: [Server端架构](architecture/server/README.md), [Client端架构](architecture/client/README.md)

## 📁 项目根目录结构

```
LYBTZYZS/ (凌隐宝堂中医诊所管理系统)
├── src/                          # 源代码目录
│   ├── Server/                   # 服务端项目 (ASP.NET Core)
│   ├── Client/                   # 客户端项目 (WPF)
│   └── Shared/                   # 共享组件
├── tests/                        # 测试项目
│   ├── UnitTests/                # 单元测试
│   ├── IntegrationTests/         # 集成测试
│   └── Architecture/             # 架构测试
├── docs/                         # 项目文档 (Diátaxis框架)
│   ├── index.md                  # 文档中心索引
│   ├── tutorial/                 # Level 1: 新手教程
│   ├── how-to/                   # Level 2: 操作指南
│   ├── reference/                # Level 3: 技术参考
│   └── explanation/              # Level 4: 概念解释
├── scripts/                      # 构建和部署脚本
├── .github/                      # GitHub配置和工作流
├── .claude/                      # Claude开发环境配置
├── CLAUDE.md                     # Claude开发约束和工作流程
├── README.md                     # 项目说明 (系统架构、技术栈、当前状态)
├── LYBT.All.sln                  # 完整解决方案文件
├── LYBT.Server.sln               # 服务端解决方案文件
├── LYBT.Desktop.sln              # 客户端解决方案文件
└── .gitignore                    # Git忽略文件
```

## 🏗 源代码组织

### Server端结构 (三层架构)

```
src/Server/
├── Core/                         # 核心层
│   ├── LYBT.Server.Core/         # 核心基础设施
│   │   ├── Extensions/           # 扩展方法
│   │   ├── Infrastructure/       # 基础设施实现
│   │   │   ├── Cache/           # 缓存实现 (MemoryCache)
│   │   │   ├── DependencyInjection/ # 依赖注入配置
│   │   │   ├── Repositories/     # 仓储基类
│   │   │   └── Specifications/   # 查询规范
│   │   └── Configuration/        # 配置模型
│   └── LYBT.Entities/            # 实体层 (11个核心实体)
│       ├── Models/               # 实体模型
│       │   ├── UserModel.cs      # 用户模型
│       │   ├── PatientModel.cs   # 患者模型
│       │   ├── MedicalCaseModel.cs # 医案模型
│       │   ├── ConsultationModel.cs # 诊疗模型
│       │   ├── PrescriptionModel.cs # 处方模型
│       │   ├── PrescriptionItemModel.cs # 处方项目模型
│       │   ├── HerbModel.cs      # 药材模型
│       │   ├── FormulaModel.cs   # 验方模型
│       │   ├── FormulaHerbItem.cs # 验方药材项目
│       │   ├── AuthSessionModel.cs # 认证会话模型
│       │   └── AdminSecretModel.cs # 超级管理员模型
│       ├── Mappings/             # 实体映射配置
│       └── README.md             # 实体层说明文档
├── Modules/                      # 业务模块层 (8个模块)
│   ├── LYBT.Module.Auth/         # 认证模块
│   │   ├── Controllers/          # AuthController (双轨认证)
│   │   ├── Services/             # 认证服务
│   │   ├── DTO/                  # 认证数据传输对象
│   │   ├── Interfaces/           # 认证接口
│   │   └── Validators/           # 认证验证器
│   ├── LYBT.Module.Users/        # 用户模块
│   │   ├── Controllers/          # UsersController
│   │   ├── Services/             # 用户服务
│   │   ├── DTO/                  # 用户数据传输对象
│   │   └── Validators/           # 用户验证器
│   ├── LYBT.Module.Patients/     # 患者模块
│   │   ├── Controllers/          # PatientsController
│   │   ├── Services/             # 患者服务
│   │   ├── DTO/                  # 患者数据传输对象
│   │   └── Validators/           # 患者验证器
│   ├── LYBT.Module.MedicalCase/  # 医案模块
│   │   ├── Controllers/          # MedicalCaseController
│   │   ├── Services/             # 医案服务
│   │   ├── DTO/                  # 医案数据传输对象
│   │   └── Validators/           # 医案验证器
│   ├── LYBT.Module.Consultation/ # 诊疗模块
│   │   ├── Controllers/          # ConsultationController
│   │   ├── Services/             # 诊疗服务
│   │   ├── DTO/                  # 诊疗数据传输对象
│   │   └── Validators/           # 诊疗验证器
│   ├── LYBT.Module.Prescriptions/ # 处方模块
│   │   ├── Controllers/          # PrescriptionsController
│   │   ├── Services/             # 处方服务
│   │   ├── DTO/                  # 处方数据传输对象
│   │   └── Validators/           # 处方验证器
│   ├── LYBT.Module.Herbs/        # 药材模块
│   │   ├── Controllers/          # HerbsController
│   │   ├── Services/             # 药材服务
│   │   ├── DTO/                  # 药材数据传输对象
│   │   └── Validators/           # 药材验证器
│   └── LYBT.Module.Formula/      # 验方模块
│       ├── Controllers/          # FormulasController
│       ├── Services/             # 验方服务
│       ├── DTO/                  # 验方数据传输对象
│       └── Validators/           # 验方验证器
└── Services/                     # 服务层
    ├── LYBT.Server.Services/      # API服务层项目
    ├── LYBT.Server.API/          # Web API项目
    │   ├── Controllers/          # API控制器
    │   ├── Middleware/           # 中间件
    │   ├── Program.cs             # 应用程序入口
    │   ├── Properties/            # 配置文件
    │   └── appsettings.json       # 应用配置
    └── GlobalUsings.cs            # 全局引用
```

### Client端结构 (WPF五层架构)

```
src/Client/Desktop/
├── Shell/                        # 启动层
│   ├── LYBT.Desktop.Shell/         # 应用程序壳
│   └── Views/                    # 主窗口和启动视图
├── Core/                         # 核心层 (Core_New)
│   ├── LYBT.Desktop.Infrastructure/   # 基础设施层
│   │   ├── Commands/             # 命令系统
│   │   ├── Events/               # 事件系统
│   │   ├── Interfaces/           # 核心接口
│   │   └── Themes/               # 主题和样式
│   ├── LYBT.Desktop.Models/       # 模型层
│   │   ├── ViewModels/           # ViewModel基类
│   │   ├── Models/               # UI模型
│   │   └── Mappings/            # 映射配置
│   ├── LYBT.Desktop.Services/     # 服务层
│   │   ├── Business/            # 业务服务
│   │   ├── Repositories/        # 仓储实现
│   │   ├── Http/               # HTTP客户端
│   │   └── Navigation/          # 导航服务
│   ├── LYBT.Desktop.Presentation/ # 表现层
│   │   ├── Components/           # 通用组件
│   │   ├── Controls/             # 自定义控件
│   │   ├── Converters/           # 转换器
│   │   ├── Templates/            # 数据模板
│   │   └── Themes/               # 主题资源
│   └── LYBT.Desktop.Contracts/   # 契约层
│       ├── Api/                  # API接口定义
│       └── DTO/                  # 数据传输对象
├── Modules/                      # 业务模块层
│   ├── LYBT.Desktop.Auth/          # 认证模块
│   ├── LYBT.Desktop.Users/         # 用户模块
│   ├── LYBT.Desktop.Patients/      # 患者模块
│   ├── LYBT.Desktop.MedicalCase/   # 医案模块
│   ├── LYBT.Desktop.Consultation/  # 诊疗模块
│   ├── LYBT.Desktop.Prescriptions/ # 处方模块
│   ├── LYBT.Desktop.Herbs/         # 药材模块
│   └── LYBT.Desktop.Formula/       # 验方模块
└── Workstations/                # 工作台层
    ├── LYBT.Desktop.ClinicalWorkstation/  # 诊疗工作台
    └── LYBT.Desktop.AdminWorkstation/     # 管理工作台
```

### Shared层结构

```
src/Shared/
├── LYBT.Shared.Models/           # 共享数据模型
│   ├── Entities/                 # 实体模型
│   ├── DTO/                      # 数据传输对象
│   ├── Enums/                    # 枚举定义
│   ├── Constants/                # 常量定义
│   └── Extensions/               # 扩展方法
├── LYBT.Shared.Interfaces/       # 共享接口
│   ├── Services/                 # 服务接口
│   ├── Repositories/             # 仓储接口
│   └── Events/                   # 事件接口
├── LYBT.Shared.Infrastructure/   # 共享基础设施
│   ├── Authentication/           # 认证基础设施
│   ├── Http/                     # HTTP客户端基础设施
│   └── Validation/               # 验证基础设施
└── LYBT.Shared.Utilities/        # 共享工具类
    ├── Helpers/                  # 帮助类
    ├── Validators/               # 验证器
    └── Converters/               # 转换器
```

## 🧪 测试结构

```
tests/
├── UnitTests/                    # 单元测试
│   ├── Server/                   # 服务端单元测试
│   │   ├── LYBT.Module.Auth.Tests/
│   │   ├── LYBT.Module.Users.Tests/
│   │   └── ...
│   ├── Client/                   # 客户端单元测试
│   │   ├── LYBT.Desktop.Auth.Tests/
│   │   ├── LYBT.Desktop.Users.Tests/
│   │   └── ...
│   └── Shared/                   # 共享组件单元测试
├── IntegrationTests/             # 集成测试
│   ├── API/                      # API集成测试
│   ├── Database/                 # 数据库集成测试
│   └── EndToEnd/                 # 端到端测试
├── Architecture/                 # 架构测试
│   ├── Server.Tests/             # 服务端架构测试
│   ├── Client.Tests/             # 客户端架构测试
│   └── Common.Tests/             # 通用架构测试
├── Security/                     # 安全测试
├── Performance/                  # 性能测试
└── TestConfiguration/            # 测试配置
    ├── TestDataBuilders/         # 测试数据构建器
    ├── AssertionHelpers/         # 断言帮助类
    ├── MockHelpers/              # Mock帮助类
    └── TestBase/                 # 测试基类
```

## 📚 文档结构

```
docs/
├── index.md                      # 文档中心索引
├── tutorial/                     # 新手教程（Learning-oriented）
│   └── README.md                 # 教程总览
├── how-to/                       # 操作指南（Problem-oriented）
│   ├── quality/                  # 质量检查工具
│   ├── development/              # 开发辅助工具
│   ├── testing/                  # 测试工具
│   └── documentation/            # 文档工具
├── reference/                    # 技术参考（Information-oriented）
│   ├── api/                      # API文档
│   ├── database/                 # 数据库参考
│   └── configuration/            # 配置参考
├── explanation/                  # 概念解释（Understanding-oriented）
│   ├── architecture/             # 架构文档
│   ├── business-rules.md         # 业务规则
│   ├── product-vision.md         # 产品愿景
│   └── project-structure.md      # 项目结构（本文档）
└── reports/                      # 分析报告
```

## 🔧 脚本与配置

```
scripts/
├── build/                        # 构建脚本
│   ├── build-all.ps1            # 完整构建脚本
│   ├── clean.ps1                # 清理脚本
│   └── test.ps1                 # 测试脚本
├── deployment/                   # 部署脚本
│   ├── deploy-dev.ps1           # 开发环境部署
│   ├── deploy-prod.ps1          # 生产环境部署
│   └── database-migrate.ps1     # 数据库迁移脚本
└── maintenance/                  # 维护脚本
    ├── backup.ps1               # 备份脚本
    ├── health-check.ps1         # 健康检查脚本
    └── performance-test.ps1      # 性能测试脚本
```

## 🎯 架构设计原则

### 三层架构原则 (Server端)

1. **Core层**: 核心基础设施，包含实体模型和通用服务
2. **Modules层**: 业务模块层，8个模块独立实现业务逻辑
3. **Services层**: API服务层，统一对外提供RESTful接口

### 五层架构原则 (Client端)

1. **Shell层**: 应用程序启动和主窗口管理
2. **Core层**: 核心基础设施，包含Models、Services、Infrastructure、Presentation、Contracts
3. **Modules层**: 业务模块层，8个模块对应Server端模块
4. **Workstations层**: 工作台层，提供专业的业务操作界面

### 模块独立性

1. **垂直切片**: 每个模块包含完整的功能实现
2. **依赖方向**: 模块间的依赖关系应该是单向的
3. **接口隔离**: 通过接口定义模块间的契约
4. **配置分离**: 每个模块有独立的配置选项
5. **双轨认证**: 认证模块支持普通用户和超级管理员双轨机制

### 模块标准结构

#### Server端模块结构 (基于实际代码)
```
LYBT.Module.Xxx/
├── Controllers/                  # API控制器
│   └── XxxController.cs          # RESTful API端点
├── Services/                     # 业务服务实现
│   ├── IXxxService.cs            # 服务接口
│   └── XxxService.cs             # 业务逻辑实现
├── DTO/                          # 数据传输对象
│   ├── XxxDto.cs                 # 基础数据传输对象
│   ├── XxxCreateDto.cs           # 创建DTO
│   ├── XxxUpdateDto.cs           # 更新DTO
│   ├── XxxListDto.cs             # 列表DTO
│   └── XxxQueryDto.cs            # 查询DTO
├── Validators/                   # FluentValidation验证器
│   ├── XxxCreateDtoValidator.cs  # 创建验证器
│   ├── XxxUpdateDtoValidator.cs  # 更新验证器
│   └── XxxQueryDtoValidator.cs   # 查询验证器
├── Interfaces/                   # 模块内部接口
│   └── IXxxRepository.cs         # 仓储接口
├── Repositories/                 # 仓储实现
│   └── XxxRepository.cs          # 数据访问实现
├── Enums/                        # 枚举定义
│   └── XxxStatus.cs              # 业务状态枚举
├── Events/                       # 事件定义
│   └── XxxEvents.cs              # 领域事件
├── Mapping/                      # AutoMapper映射配置
│   └── XxxMappingProfile.cs      # 映射配置
├── Options/                      # 模块配置选项
│   └── XxxModuleOptions.cs       # 配置模型
├── Extensions/                   # 扩展方法
│   └── XxxServiceExtensions.cs   # 服务注册扩展
└── XxxModule.cs                  # 模块服务注册类
```

**特殊模块说明**:
- **LYBT.Module.Auth**: 包含双轨认证系统，支持Users表和AdminSecrets表
- **LYBT.Module.Patients**: 支持Excel批量导入功能
- **LYBT.Module.Prescriptions**: 支持四种处方录入方式（表格、快速、方剂导入、历史复制）
- **LYBT.Module.Herbs**: 包含2000+药材字典和拼音码检索

#### Client端模块结构 (基于实际代码)
```
LYBT.Desktop.Xxx/
├── Models/                       # UI专用模型
│   ├── XxxItem.cs               # 列表项模型
│   ├── XxxViewState.cs          # 视图状态
│   ├── XxxStep.cs               # 向导步骤枚举
│   └── XxxFilterModel.cs        # 过滤器模型
├── ViewModels/                   # 视图模型
│   ├── XxxManagementViewModel.cs # 列表管理ViewModel
│   ├── XxxDetailViewModel.cs     # 详情查看ViewModel
│   ├── XxxCreateViewModel.cs     # 创建ViewModel
│   ├── XxxEditViewModel.cs       # 编辑ViewModel
│   ├── XxxDialogViewModel.cs     # 对话框ViewModel
│   └── XxxWizardViewModel.cs     # 向导ViewModel
├── Views/                        # XAML视图
│   ├── XxxManagementView.xaml    # 列表管理视图
│   ├── XxxDetailView.xaml        # 详情视图
│   ├── XxxCreateView.xaml        # 创建视图
│   ├── XxxEditView.xaml          # 编辑视图
│   ├── XxxDialog.xaml            # 对话框视图
│   └── XxxWizardView.xaml        # 向导视图
├── Interfaces/                   # 模块接口目录
│   ├── IXxxRepository.cs        # Repository接口
│   └── IXxxService.cs           # 服务接口
├── Repositories/                 # 数据访问层
│   └── XxxRepository.cs         # Repository实现
├── Services/                     # 业务服务层
│   ├── IXxxDataService.cs       # 数据服务接口
│   └── XxxDataService.cs        # 数据服务实现
├── Commands/                     # 命令类
│   ├── XxxCommands.cs           # 模块命令
│   └── XxxAsyncCommands.cs      # 异步命令
├── Converters/                   # 转换器
│   ├── XxxConverter.cs          # 值转换器
│   └── XxxMultiConverter.cs     # 多值转换器
├── Validators/                   # 验证器
│   └── XxxValidator.cs          # 输入验证器
├── XxxModule.cs                  # Prism模块注册
└── README.md                     # 模块说明文档
```

**特殊模块说明**:
- **LYBT.Desktop.Auth**: 实现双轨登录界面，支持普通用户和超级管理员登录
- **LYBT.Desktop.Patients**: 包含患者导入向导和Excel批量处理功能
- **LYBT.Desktop.MedicalCase**: 实现医案状态管理和工作流控制
- **LYBT.Desktop.Consultation**: 实现四诊合参录入界面（望闻问切）
- **LYBT.Desktop.Prescriptions**: 实现四种处方录入方式和药材配伍检查
- **LYBT.Desktop.Herbs**: 包含药材选择器和拼音码快速检索
- **LYBT.Desktop.Formula**: 实现验方模板管理和智能推荐功能

## 📋 命名规范

### 项目命名

- **命名空间**: `LYBT.Layer.ModuleName`
- **项目名称**: `LYBT.Layer.ModuleName`
- **程序集**: 与项目名称一致

### 文件命名

- **类文件**: `PascalCase.cs` (例: `PatientService.cs`)
- **接口文件**: `I` + `PascalCase.cs` (例: `IPatientService.cs`)
- **枚举文件**: `PascalCase.cs` (例: `PatientStatus.cs`)
- **XAML文件**: `PascalCase.xaml` (例: `PatientManagementView.xaml`)

### 类和成员命名

- **类名**: `PascalCase` (例: `PatientService`)
- **接口名**: `I` + `PascalCase` (例: `IPatientService`)
- **方法名**: `PascalCase` (例: `GetPatientById`)
- **属性名**: `PascalCase` (例: `PatientName`)
- **字段名**: `_camelCase` (例: `_patientRepository`)
- **常量名**: `UPPER_SNAKE_CASE` (例: `MAX_RETRY_COUNT`)

## 🔗 依赖关系

### 模块依赖图 (基于实际架构)

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Desktop (WPF)                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────┐  │
│  │ Workstations│──│   Modules   │──│   Core_New  │──│  Shell  │  │
│  │             │  │ (8个业务模块)│  │(5层核心架构)│  │         │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────┘  │
└─────────────────────────────────────────────────────────────────┘
                             │ HTTP/REST API
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Server (ASP.NET Core)                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────┐  │
│  │  Services   │──│   Modules   │──│    Core     │──│   API   │  │
│  │             │  │ (8个业务模块)│  │(3层架构)   │  │         │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────┘  │
└─────────────────────────────────────────────────────────────────┘
                             │ Database
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Shared Layer                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────┐  │
│  │   Models    │  │ Interfaces  │  │ Infrastructure│  │Utilities│  │
│  │             │  │             │  │             │  │         │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 依赖规则 (基于实际代码)

1. **Client端** 通过 HTTP API 调用 Server端
2. **Client端** 可以依赖 Shared层 (Models, Interfaces, Infrastructure)
3. **Server端** 可以依赖 Shared层 (Models, Interfaces, Infrastructure)
4. **Client端** 不可以直接依赖 Server端代码
5. **Shared层** 不可以依赖 Client端或Server端
6. **模块间** 通过接口进行交互，避免直接依赖
7. **认证模块**: 实现双轨认证，Client和Server都有对应的认证实现

## 🚀 构建和部署

### 解决方案结构

项目使用三个解决方案文件：

1. **LYBT.All.sln**: 完整解决方案，包含所有项目
2. **LYBT.Server.sln**: 服务端解决方案，仅包含服务端项目
3. **LYBT.Desktop.sln**: 客户端解决方案，仅包含客户端项目

### 构建配置

- **Debug**: 开发环境配置，包含调试信息
- **Release**: 生产环境配置，启用代码优化
- **测试配置**: 统一的测试配置和设置

### 部署结构

```
deployment/
├── Development/                 # 开发环境配置
├── Testing/                     # 测试环境配置
├── Staging/                     # 预生产环境配置
└── Production/                  # 生产环境配置
    ├── docker/                  # Docker配置
    ├── iis/                     # IIS配置
    └── scripts/                 # 部署脚本
```

## 📖 相关文档

- **架构设计**: [架构总览](architecture/README.md)
- **产品愿景**: [产品愿景与目标](product-vision.md)
- **架构原则**: [架构原则](architecture/principles.md)
- **业务规则**: [业务规则](business-rules.md)

---

**变更历史**:
- 2025-11-09: 从.spec-workflow/steering/structure.md迁移到docs/explanation/
- 2025-10-15: 创建初始版本（基于3.0文档整合）

*本文档作为项目结构的权威指导，所有新增代码和模块都应遵循这些组织原则和命名规范。*
