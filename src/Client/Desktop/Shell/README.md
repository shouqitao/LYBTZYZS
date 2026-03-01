# LYBT.Desktop.Shell

> WPF应用程序入口 | Prism.DryIoc模块化容器 | 启动编排与导航中心

## 项目定位

- **层级**: Client端 (Desktop桌面应用)
- **职责**: 整个WPF客户端的统一入口点和容器编排中心。负责应用启动、Prism模块加载、DI容器管理、主界面框架、Region导航和全局异常处理
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Shell/
├── App.xaml / App.xaml.cs         # PrismApplication启动逻辑
├── appsettings.json               # 应用配置 (API地址/UI/功能开关/缓存)
├── GlobalAssemblyInfo.cs          # 程序集版本信息
├── NativeMethods.cs               # Win32互操作
├── Views/                         # 主窗口、闪屏、占位符视图
├── ViewModels/                    # MainWindowViewModel、AccountSettings
├── Controls/                      # AccountSettingsControl
├── Dialogs/                       # Prism对话框 (Views + ViewModels)
├── Extensions/                    # DI注册/错误处理/Prism配置扩展方法
├── Services/                      # 启动管道/Bootstrap/HealthCheck/Session/Login
├── Models/                        # TodayPatientItem等Shell层模型
├── Styles/                        # CommonStyles/Controls/Dialog/Typography
├── Assets/                        # Icons + Images
└── Resources/                     # XAML资源字典
```

## 核心组件

| 名称 | 说明 |
|------|------|
| App.xaml.cs | PrismApplication实现，模块目录配置、DI注册、异常处理 |
| MainWindow | 主窗口容器 (标题栏+菜单栏+ContentRegion+状态栏) |
| MainWindowViewModel | 导航命令、用户会话、状态栏、EventAggregator订阅 |
| StartupPipeline | 启动管道，统一管理启动步骤的唯一入口 |
| ApplicationBootstrapper | 角色驱动的模块加载服务 |
| HealthCheckCoordinator | API健康检查协调器 (定时检查、状态变更事件) |
| NavigationCoordinator | Region导航协调服务 |
| MenuManager | 菜单权限管理 |
| SplashScreenWindow | 启动闪屏窗口 |
| ConfirmationDialog | Prism IDialogAware确认对话框 |

## 设计依据

Shell采用Prism模块化架构，通过ConfigureModuleCatalog集中注册所有业务模块和工作台模块，而非目录自动发现方式，确保加载顺序可控。模块间通过EventAggregator解耦通信，避免直接依赖。启动流程通过StartupPipeline统一编排，替代分散的初始化逻辑。

## 依赖关系

### 依赖

- LYBT.Desktop.Contracts (Refit API接口定义)
- LYBT.Desktop.Presentation (ViewModelBase/DialogService/通用UI组件)
- LYBT.Desktop.Infrastructure (HttpClient配置/Token管理/缓存)
- LYBT.Desktop.Foundation (Result/异常定义/扩展方法)
- LYBT.Shared.Models (跨端DTO)
- 业务模块: Auth, Users, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula
- 工作台: WorkstationCore, Admin, Clinical

### 被依赖

- 无 (顶层Shell，不被其他项目引用)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简README，详细代码知识迁移至CLAUDE.md |
| 2025-12-07 | 初始版本 |
