# LYBT.Desktop

> **凌隐宝堂中医诊所 - WPF桌面客户端**  
> 基于 .NET 8.0 的现代化中医诊所管理桌面应用

## 🎯 项目概述

- **项目名称**: LYBT.Desktop (凌隐宝堂中医诊所桌面客户端)
- **目标框架**: .NET 8.0 Windows  
- **UI框架**: WPF + Prism.DryIoc 9.0.537
- **架构模式**: UltraThink三层MVVM + 模块化
- **通信方式**: Refit类型安全REST客户端

**🏆 质量状态**: ✅ **零编译警告** | ✅ **A+代码质量** | ✅ **生产就绪**

## 🏗️ 项目架构

### 核心基础设施

- **Shell**: 应用程序启动壳和模块加载容器
- **Core**: 核心基础设施 (接口、服务、常量)
- **Services**: 统一API服务层和业务逻辑
- **Infrastructure**: 通用控件、转换器、样式资源

### 8个业务模块

| 模块                | 功能描述      | 状态   |
| ----------------- | --------- | ---- |
| **Auth**          | 身份认证、登录管理 | ✅ 完成 |
| **Users**         | 用户管理、角色分配 | ✅ 完成 |
| **Patients**      | 患者档案、病历管理 | ✅ 完成 |
| **MedicalCase**   | 医疗案例、诊疗流程 | ✅ 完成 |
| **Consultation**  | 中医四诊、辨证论治 | ✅ 完成 |
| **Prescriptions** | 处方管理、智能配伍 | ✅ 完成 |
| **Herbs**         | 中药材信息管理   | ✅ 完成 |
| **Formula**       | 验方模板管理    | ✅ 完成 |

## 🛠️ 技术栈

### 核心技术

- **.NET 8.0**: 统一开发平台
- **WPF**: 原生Windows桌面UI框架
- **Prism.DryIoc 9.0.537**: MVVM框架 + 依赖注入容器
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
dotnet run --project Shell
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
- **依赖注入**: 构造函数注入模式
- **数据绑定**: 双向绑定和命令模式
- **异步操作**: async/await模式

### 服务层规范

- **接口约定**: I{Module}Service
- **实现命名**: {Module}ModuleService
- **API封装**: Refit类型安全客户端

## 📊 开发状态

**编译状态**: ✅ 0错误 0警告  
**架构完成度**: ✅ UltraThink三层架构完全实施  
**模块完整性**: ✅ 8个核心模块全部完成  
**UI一致性**: ✅ 统一的现代化界面设计  

---

> 📌 **开发提醒**: 遵循 [CLAUDE.md](../../../CLAUDE.md) 中的开发规范和架构约定