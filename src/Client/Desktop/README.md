# LYBT.Desktop

> **凌隐宝堂中医诊所 - WPF桌面客户端** 
> 基于 .NET 8.0 的现代化中医诊所管理桌面应用

## 🎯 项目概述

- **项目名称**: 凌隐宝堂中医诊所桌面客户端 (LYBT.Desktop)
- **目标框架**: .NET 8.0 Windows 
- **UI框架**: WPF + Prism.DryIoc 8.1.97
- **架构模式**: 三层MVVM + 模块化
- **通信方式**: Refit类型安全REST客户端

**🏆 质量状态**: ✅ **零编译警告** | ✅ **高质量 | ✅ **生产就绪**

## 🏗️ 项目架构

### 核心基础设施

- **Shell**: 应用程序启动壳和模块加载容器
- **Core**: 核心基础设施 (接口、服务、常量)
- **Services**: 统一API服务层和业务逻辑
- **基础设施（基础设施（Infrastructure））**: 通用控件、转换器、样式资源

### 8个业务模块

| 模块 | 功能描述 | 状态 |
| ----------------- | --------- | ---- |
| **Auth** | 身份认证、登录管理 | ✅ 完成 |
| **Users** | 用户管理、角色分配 | ✅ 完成 |
| **Patients** | 患者档案、病历管理 | ✅ 完成 |
| **MedicalCase** | 医疗案例、诊疗流程 | ✅ 完成 |
| **Consultation** | 中医四诊、辨证论治 | ✅ 完成 |
| **Prescriptions** | 处方管理、智能配伍 | ✅ 完成 |
| **Herbs** | 中药材信息管理 | ✅ 完成 |
| **Formula** | 验方模板管理 | ✅ 完成 |

## 🛠️ 技术栈

### 核心技术

- **.NET 8.0**: 统一开发平台
- **WPF**: 原生Windows桌面UI框架
- **Prism.DryIoc 8.1.97**: MVVM框架 + 依赖注入（DI）容器
- **Refit**: 类型安全的HTTP API客户端
- **AutoMapper 15.0.1**: 对象映射 (需要ILoggerFactory参数)

### UI技术

- **Modern UI**: 现代化界面设计
- **Resource Dictionary**: 统一样式和主题管理
- **Pack URI**: 资源文件统一引用机制
- **MVVM DataBinding**: 双向数据绑定模式

## 🚀 快速开始

### 环境要求

- **Visual Studio 2022** 或更高版本
- **.NET 8.0 SDK**
- **Windows 10/11** 操作系统

### 启动步骤

```bash
# 1. 克隆项目
git clone <project-url>

# 2. 还原NuGet包
dotnet restore LYBT.Desktop.sln

# 3. 启动后端API服务 
# (参考后端README启动WebAPI)

# 4. 启动桌面客户端
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```

### 默认登录

- **地址**: sysadmin
- **密码**: Admin@123456

## 🌐 后端集成

### API配置

- **基地址**: https://localhost:7001 (开发环境)
- **认证方式**: JWT Bearer Token 
- **通信协议**: HTTPS + JSON
- **错误处理**: 统一ApiResponse<T>格式

### 连接配置

```csharp
// API客户端配置示例
services.AddRefitClient<IAuthApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:7001"));
```

## 👥 用户角色

### Admin (系统管理员)

- ✅ 完整系统配置和管理权限
- ✅ 用户账户创建和权限分配
- ✅ 数据导入导出和系统维护

### Doctor (医生)

- ✅ 患者档案管理和诊疗记录
- ✅ 中医四诊记录和辨证论治
- ✅ 处方开具和验方管理

## 🏛️ MVVM架构

### 视图模型规范

- **命名约定**: `{Function}ViewModel`
- **依赖注入（DI）**: 构造函数注入模式
- **数据绑定**: 双向绑定和命令模式
- **异步操作**: async/await模式

### 服务层规范

- **接口约定**: I{Module}服务（服务（Service））
- **实现命名**: {Module}ModuleService
- **API封装**: Refit类型安全客户端

## 📊 开发状态

**编译状态**: ✅ 0错误 0警告 
**架构完成度**: ✅ 分层架构完全实施 
**模块完整性**: ✅ 8个核心模块全部完成 
**UI一致性**: ✅ 统一的现代化界面设计 

---

> 📌 **开发提醒**: 遵循 [CLAUDE.md](../../../CLAUDE.md) 中的开发规范和架构约定

## 📦 项目结构

```
src/Client/Desktop/
├── Shell/                         # 应用程序外壳和启动容器
│   ├── LYBT.Desktop.Shell/        # 主启动项目，负责模块加载和导航
│   │   ├── App.xaml               # 应用程序入口
│   │   ├── MainWindow.xaml        # 主窗口
│   │   ├── Views/                 # Shell视图
│   │   └── ViewModels/            # Shell视图模型
├── Core/                          # 核心基础设施层
│   └── LYBT.Desktop.Core/         # 核心服务和接口定义
│       ├── Interfaces/            # 核心服务接口（ISessionManager等）
│       ├── Models/                # 共享视图模型和数据模型
│       ├── Services/              # 核心服务实现（SessionManager等）
│       ├── Constants/             # 常量定义
│       └── Events/                # 统一事件定义
├── Services/                      # 业务服务层
│   └── LYBT.Desktop.Services/     # 业务服务实现和API客户端
│       ├── Clients/               # Refit API客户端实现
│       ├── Services/              # 业务逻辑服务
│       ├── Caching/               # 缓存服务
│       └── Extensions/            # 服务注册扩展
├── Infrastructure/                # 基础设施层
│   └── LYBT.Desktop.Infrastructure/ # UI组件和通用基础设施
│       ├── Controls/              # 自定义WPF控件
│       ├── Converters/            # 数据绑定转换器
│       ├── Behaviors/             # WPF行为扩展
│       └── Resources/             # 统一样式和模板
├── Workstationes/                   # 工作台系统
│   ├── Core/                      # 工作台核心组件
│   ├── Admin/                     # 系统管理工作台
│   └── Medical/                   # 诊疗工作台
├── Modules/                       # 8个业务模块
│   ├── Auth/                      # 身份认证模块（登录界面、权限验证）
│   ├── Users/                     # 用户管理模块（用户CRUD、角色分配）
│   ├── Patients/                  # 患者管理模块（患者档案、基础信息）
│   ├── MedicalCase/               # 医疗案例模块（诊疗流程、状态管理）
│   ├── Consultation/              # 诊疗模块（中医四诊、辨证记录）
│   ├── Prescriptions/             # 处方管理模块（处方开具、剂量计算）
│   ├── Herbs/                     # 药材管理模块（药材信息、价格维护）
│   └── Formula/                   # 验方管理模块（模板管理、方剂库）
├── Resources/                     # 资源文件
│   ├── Images/                    # 图片资源
│   ├── Icons/                     # 图标资源
│   └── Fonts/                     # 字体资源
├── Themes/                        # 主题和样式
│   ├── Generic.xaml               # 通用控件样式
│   ├── Colors.xaml                # 颜色定义
│   └── MaterialDesign.xaml        # Material Design主题
└── Configuration/                 # 配置文件
    ├── appsettings.json           # 应用配置
    └── logging.json               # 日志配置
```

**各层职责说明**：
- **Shell**: 应用启动和模块加载容器，提供主窗口和导航框架
- **Core**: 核心服务接口和会话管理，提供跨模块的基础功能
- **Services**: 统一API访问层和业务逻辑，封装后端服务调用
- **Infrastructure**: UI基础设施，提供通用控件、转换器、样式等
- **Modules**: 业务功能模块，每个模块包含Views、ViewModels、Services等
- **Workstationes**: 工作台系统，提供角色驱动的功能导航界面

## 🔌 API 接口

### Refit API客户端集成

桌面客户端通过Refit类型安全HTTP客户端与后端API进行通信，提供强类型的API调用接口。

**核心API客户端接口**：

| API客户端接口 | 功能描述 | 主要方法 |
|---------------|----------|----------|
| **IAuthApi** | 身份认证服务 | `LoginAsync()`, `LogoutAsync()`, `RefreshTokenAsync()` |
| **IUsersApi** | 用户管理服务 | `GetUsersAsync()`, `CreateUserAsync()`, `UpdateUserAsync()` |
| **IPatientsApi** | 患者管理服务 | `SearchPatientsAsync()`, `CreatePatientAsync()`, `GetPatientHistoryAsync()` |
| **IMedicalCaseApi** | 医疗案例服务 | `StartConsultationAsync()`, `UpdateCaseStatusAsync()`, `GetCaseTimelineAsync()` |
| **IConsultationApi** | 诊疗记录服务 | `SaveConsultationAsync()`, `GetConsultationDetailsAsync()` |
| **IPrescriptionApi** | 处方管理服务 | `CreatePrescriptionAsync()`, `CalculatePrescriptionAsync()`, `CopyPrescriptionAsync()` |
| **IHerbsApi** | 药材管理服务 | `SearchHerbsAsync()`, `GetHerbPricingAsync()`, `BatchUpdateHerbsAsync()` |
| **IFormulasApi** | 验方管理服务 | `GetFormulaTemplatesAsync()`, `CreateFormulaFromPrescriptionAsync()` |

### API客户端配置

```csharp
// 服务注册示例（ServiceCollectionExtensions.cs）
services.AddRefitClient<IAuthApi>()
    .ConfigureHttpClient(c => 
    {
        c.BaseAddress = new Uri("https://localhost:7001");
        c.DefaultRequestHeaders.Add("User-Agent", "LYBT-Desktop/2.1.0");
    })
    .AddPolicyHandler(retryPolicy);

services.AddRefitClient<IUsersApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://localhost:7001"))
    .AddHttpMessageHandler<AuthenticationHandler>(); // 自动添加JWT Token
```

### JWT认证流程

1. **用户登录**: 调用`IAuthApi.LoginAsync()`获取访问令牌
2. **Token存储**: 将JWT Token安全存储到用户配置中
3. **自动认证**: `AuthenticationHandler`拦截器自动在请求头添加`Authorization: Bearer {token}`
4. **Token刷新**: 当Token过期时，自动调用`RefreshTokenAsync()`更新令牌
5. **会话管理**: `SessionManager`统一管理用户登录状态和权限

### API调用示例

```csharp
// 用户服务中的API调用示例
public class UserModuleService : IUserService
{
    private readonly IUsersApi _usersApi;
    
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchUsersAsync(UserSearchDto criteria)
    {
        try
        {
            var apiResponse = await _usersApi.SearchUsersAsync(criteria);
            
            return apiResponse.Success 
                ? ServiceResult<PagedResult<UserDto>>.Success(apiResponse.Data)
                : ServiceResult<PagedResult<UserDto>>.Failure(apiResponse.Message);
        }
        catch (ApiException ex)
        {
            return ServiceResult<PagedResult<UserDto>>.Failure($"API调用失败: {ex.Message}");
        }
    }
}
```

### 错误处理和重试策略

- **网络错误**: 自动重试3次，使用指数退避策略
- **认证失败**: 自动跳转到登录界面，清除本地会话
- **服务器错误**: 显示用户友好的错误消息，记录详细错误日志
- **超时处理**: 30秒请求超时，长时间操作提供取消功能

### API基础配置

- **基础地址**: https://localhost:7001 (开发环境)
- **请求超时**: 30秒
- **内容类型**: application/json
- **字符编码**: UTF-8
- **HTTP版本**: HTTP/2

## 📚 相关文档

### 架构设计
- [系统架构概览](../../../docs/architecture/overview.md) - 整体架构设计和模块关系
- [WPF架构设计](../../../docs/client/wpf-architecture.md) - WPF客户端架构设计文档
- [MVVM模式实践](../../../docs/client/mvvm-patterns.md) - MVVM架构模式和最佳实践

### API集成
- [API接口文档](../../../docs/api/README.md) - 后端API接口规范和调用说明
- [Refit客户端配置](../../../docs/client/refit-configuration.md) - HTTP客户端配置和使用指南
- [认证流程设计](../../../docs/client/authentication-flow.md) - JWT认证和会话管理流程

### 模块文档 
- [模块索引](../../../docs/modules/index.md) - 8个业务模块的功能说明
- [工作台系统设计](../../../docs/client/workbench-system.md) - 角色驱动的工作台架构
- [导航系统文档](../../../docs/client/navigation-system.md) - 模块导航和路由机制

### 基础设施文档
- [Core模块文档](Core/README.md) - 核心基础设施和服务接口
- [Services模块文档](Services/README.md) - 业务服务层和API客户端
- [Infrastructure文档](Infrastructure/README.md) - UI基础设施和通用组件
- [Shell文档](Shell/README.md) - 应用启动壳和模块加载器

### 业务模块
- [身份认证模块](Modules/Auth/README.md) - 登录界面和权限验证
- [用户管理模块](Modules/Users/README.md) - 用户CRUD和角色管理界面
- [患者管理模块](Modules/Patients/README.md) - 患者档案和信息维护界面
- [医疗案例模块](Modules/MedicalCase/README.md) - 诊疗流程管理界面
- [诊疗记录模块](Modules/Consultation/README.md) - 中医四诊录入界面
- [处方管理模块](Modules/Prescriptions/README.md) - 处方开具和管理界面
- [药材管理模块](Modules/Herbs/README.md) - 中药材信息维护界面
- [验方管理模块](Modules/Formula/README.md) - 方剂模板管理界面

### 开发指南
- [WPF开发规范](../../../docs/client/wpf-coding-standards.md) - XAML和C#编码规范
- [UI设计指南](../../../docs/client/ui-design-guide.md) - 界面设计规范和组件库
- [数据绑定最佳实践](../../../docs/client/databinding-practices.md) - WPF数据绑定模式
- [性能优化指南](../../../docs/client/performance-optimization.md) - WPF应用性能调优

### 部署配置
- [客户端部署文档](../../../docs/deployment/desktop-deployment.md) - 桌面应用打包和分发
- [配置管理文档](../../../docs/client/configuration-management.md) - 客户端配置文件管理
- [日志系统配置](../../../docs/client/logging-configuration.md) - 客户端日志记录配置

### 共享组件
- [Shared层文档](../../../src/Shared/README.md) - 前后端共享组件和DTO模型
- [模块化架构标准](TWO_LAYER_ARCHITECTURE_STANDARD.md) - 分层架构设计标准
- [Prism框架集成](../../../docs/client/prism-integration.md) - Prism MVVM框架使用指南

