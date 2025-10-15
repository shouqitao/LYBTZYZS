# 项目结构与组织指南 (Project Structure & Organization Guide)

> **版本**: 1.0  
> **创建日期**: 2025-10-15  
> **来源**: 基于现有3.0文档整合  
> **维护**: 项目团队  
> **关联**: [Server模块设计标准](../../docs/architecture/server-module-design-standard.md), [Client端设计标准](../../docs/architecture/client/unified-design-standard.md)

## 📁 项目根目录结构

```
LYBTZYZS/
├── src/                          # 源代码目录
│   ├── Server/                   # 服务端项目
│   ├── Client/                   # 客户端项目
│   └── Shared/                   # 共享组件
├── tests/                        # 测试项目
├── docs/                         # 项目文档
├── scripts/                      # 构建和部署脚本
├── .spec-workflow/               # Spec工作流文档
├── .github/                      # GitHub配置和工作流
├── CLAUDE.md                     # Claude开发约束
├── README.md                     # 项目说明
└── LYBT.All.sln                  # 解决方案文件
```

## 🏗 源代码组织

### Server端结构

```
src/Server/
├── LYBT.Server.API/              # Web API项目
│   ├── Controllers/              # API控制器
│   ├── Middleware/               # 中间件
│   ├── Program.cs                # 应用程序入口
│   └── Properties/               # 配置文件
├── LYBT.Server.Core/             # 核心基础设施
│   ├── Extensions/               # 扩展方法
│   ├── Infrastructure/           # 基础设施实现
│   │   ├── Cache/               # 缓存实现
│   │   ├── DependencyInjection/  # 依赖注入配置
│   │   ├── Repositories/        # 仓储基类
│   │   └── Specifications/      # 查询规范
│   └── Configuration/            # 配置模型
├── Modules/                      # 业务模块
│   ├── LYBT.Module.Auth/         # 认证模块
│   ├── LYBT.Module.Users/        # 用户模块
│   ├── LYBT.Module.Patients/     # 患者模块
│   ├── LYBT.Module.MedicalCase/  # 病案模块
│   ├── LYBT.Module.Consultation/ # 辨证模块
│   ├── LYBT.Module.Prescriptions/ # 处方模块
│   ├── LYBT.Module.Herbs/        # 药材模块
│   └── LYBT.Module.Formula/      # 方剂模块
└── LYBT.Server.Services/         # API服务层项目
```

### Client端结构

```
src/Client/Desktop/
├── LYBT.Desktop.Shell/           # 应用程序壳
│   ├── Views/                    # 主窗口和启动视图
│   ├── ViewModels/               # 主窗口ViewModel
│   ├── Services/                 # 应用程序级服务
│   └── Behaviors/                # 行为类
├── Core/                        # 核心基础设施
│   ├── LYBT.Desktop.Models/     # 共享模型
│   │   ├── ViewModels/           # ViewModel基类
│   │   ├── Models/               # UI模型
│   │   └── Extensions/           # 扩展方法
│   ├── LYBT.Desktop.Infrastructure/ # 基础设施
│   │   ├── DependencyInjection/  # 依赖注入
│   │   ├── Events/               # 事件系统
│   │   ├── Repositories/         # 仓储基类
│   │   └── Validation/           # 验证器
│   ├── LYBT.Desktop.Presentation/ # UI组件
│   │   ├── Components/           # 通用组件
│   │   ├── Controls/             # 自定义控件
│   │   ├── Converters/           # 转换器
│   │   ├── Templates/            # 数据模板
│   │   └── Themes/               # 主题资源
│   └── LYBT.Desktop.Contracts/   # API接口契约
│       ├── Api/                  # API接口定义
│       └── DTO/                  # 数据传输对象
└── Modules/                     # 业务模块
    ├── LYBT.Desktop.Auth/        # 认证模块
    ├── LYBT.Desktop.Users/       # 用户模块
    ├── LYBT.Desktop.Patients/    # 患者模块
    ├── LYBT.Desktop.MedicalCase/ # 病案模块
    ├── LYBT.Desktop.Consultation/ # 辨证模块
    ├── LYBT.Desktop.Prescriptions/ # 处方模块
    ├── LYBT.Desktop.Herbs/       # 药材模块
    └── LYBT.Desktop.Formula/     # 方剂模块
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
├── architecture/                 # 架构文档
│   ├── README.md                 # 架构总览
│   ├── server-module-design-standard.md
│   ├── client-unified-design-standard.md
│   ├── adr/                      # 架构决策记录
│   ├── modules/                  # 模块设计文档
│   └── testing/                  # 架构测试文档
├── development/                  # 开发文档
│   ├── README.md                 # 开发指南总览
│   ├── standards.md              # 编码规范
│   ├── minimal-practice.md       # 最小实践指南
│   ├── documentation-guidelines.md # 文档编写指南
│   ├── testing-guide.md          # 测试指南
│   └── tools-configuration.md    # 工具配置
├── api/                          # API文档
│   ├── README.md                 # API总览
│   ├── authentication.md         # 认证文档
│   ├── patients/                 # 患者API文档
│   └── ...                       # 其他API文档
├── security/                     # 安全文档
├── deployment/                   # 部署文档
├── issues/                       # Issue追踪文档
├── reports/                      # 分析报告
└── assets/                       # 文档资源
    ├── images/                   # 图片资源
    ├── diagrams/                 # 架构图
    └── templates/                # 文档模板
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

## 🎯 模块设计原则

### 模块独立性

1. **垂直切片**: 每个模块包含完整的功能实现
2. **依赖方向**: 模块间的依赖关系应该是单向的
3. **接口隔离**: 通过接口定义模块间的契约
4. **配置分离**: 每个模块有独立的配置选项

### 模块标准结构

#### Server端模块结构
```
LYBT.Module.Xxx/
├── Controllers/                  # API控制器
├── Entities/                     # (已废弃)实体定义
├── Interfaces/                   # 模块内部接口
│   └── IXxxRepository.cs
├── Mapping/                      # AutoMapper映射配置
│   └── XxxMappingProfile.cs
├── Options/                      # 模块配置选项
│   └── XxxModuleOptions.cs
├── Repositories/                 # 仓储实现
│   └── XxxRepository.cs
├── Services/                     # 业务服务实现
│   └── XxxService.cs
├── Validators/                   # DTO验证器
│   ├── XxxCreateDtoValidator.cs
│   └── XxxUpdateDtoValidator.cs
├── Events/                       # 事件定义
│   └── XxxEvents.cs
├── Enums/                        # 枚举定义
│   └── XxxEnums.cs
├── DTO/                          # 数据传输对象
│   ├── XxxDto.cs
│   ├── XxxCreateDto.cs
│   ├── XxxUpdateDto.cs
│   └── XxxListDto.cs
├── Mappings/                     # 手动映射扩展
│   └── XxxMappings.cs
└── XxxModule.cs                  # 模块服务注册
```

#### Client端模块结构
```
LYBT.Desktop.Xxx/
├── Models/                       # UI专用模型
│   ├── XxxItem.cs               # 列表项模型
│   ├── XxxViewState.cs          # 视图状态
│   └── XxxStep.cs               # 向导步骤枚举
├── ViewModels/                   # 视图模型
│   ├── XxxManagementViewModel.cs # 列表管理
│   ├── XxxDetailViewModel.cs     # 详情查看
│   ├── XxxCreateViewModel.cs     # 创建
│   ├── XxxEditViewModel.cs       # 编辑
│   └── XxxDialogViewModel.cs     # 对话框
├── Views/                        # XAML视图
│   ├── XxxManagementView.xaml    # 列表管理视图
│   ├── XxxDetailView.xaml        # 详情视图
│   ├── XxxCreateView.xaml        # 创建视图
│   ├── XxxEditView.xaml          # 编辑视图
│   └── XxxDialog.xaml            # 对话框视图
├── Interfaces/                   # 模块接口目录
│   └── IXxxRepository.cs        # Repository接口
├── Repositories/                 # 数据访问层
│   └── XxxRepository.cs         # Repository实现
├── XxxModule.cs                  # Prism模块注册
├── Commands/                     # 命令类
├── Converters/                   # 转换器
├── Validators/                   # 验证器
└── README.md                     # 模块说明文档
```

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

### 模块依赖图

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Desktop Apps  │───▶│   Shared Layer  │◀───│  Server Modules │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│    Core/Infra   │    │   Models/DTOs   │    │   Core/Infra   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### 依赖规则

1. **Client端** 可以依赖 Shared层
2. **Server端** 可以依赖 Shared层
3. **Client端** 不可以直接依赖 Server端
4. **Shared层** 不可以依赖 Client端或Server端
5. **模块间** 通过接口进行交互，避免直接依赖

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

---

*本文档作为项目结构的权威指导，所有新增代码和模块都应遵循这些组织原则和命名规范。*