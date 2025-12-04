# Spec: client-layer-architecture

## Purpose

定义Client层(16个项目)的详细架构，包括Core层、Modules层、Roles层和Shell层的职责边界、MVVM模式规范和模块注册机制。

## Requirements

### Requirement: CLI-001 Core层职责

Core层(5个项目) SHALL 提供客户端基础设施支持。

**项目职责**:

| 项目 | 职责 | 主要内容 |
|------|------|----------|
| LYBT.Desktop.Contracts | 接口定义 | IApi接口(Refit)、IService接口、IRepository接口 |
| LYBT.Desktop.Foundation | 基础设施 | HTTP客户端、缓存、安全、配置、日志 |
| LYBT.Desktop.Infrastructure | WPF服务 | DialogService、NavigationService、控件、转换器、主题 |
| LYBT.Desktop.Models | 客户端模型 | ViewState、Item模型、事件模型 |
| LYBT.Desktop.Presentation | UI基类 | UnifiedViewModelBase、DialogViewModelBase、BaseApiRepository |

#### Scenario: 定义API接口
- **WHEN** 需要调用后端API
- **THEN** SHALL 在Contracts/Apis/目录创建I{Entity}Api.cs
- **AND** SHALL 使用Refit特性标注
- **AND** SHALL 返回Task<ApiResponse<T>>

#### Scenario: 创建ViewModel基类
- **WHEN** ViewModel有通用功能(加载状态、错误消息等)
- **THEN** SHALL 继承UnifiedViewModelBase
- **AND** SHALL 使用IsLoading/ErrorMessage等通用属性

#### Scenario: 创建客户端Repository
- **WHEN** 需要封装API调用
- **THEN** SHALL 继承BaseApiRepository<T>
- **AND** SHALL 在Foundation中注册为Singleton

---

### Requirement: CLI-002 Modules层职责

Modules层(8个项目) SHALL 实现业务UI功能。

**标准目录结构**:
```
LYBT.Desktop.{Domain}/
├── {Domain}Module.cs              # Prism模块注册
├── Views/                         # XAML视图
│   ├── {Feature}View.xaml
│   └── Dialogs/                   # 弹窗视图
├── ViewModels/                    # ViewModel
│   ├── {Feature}ViewModel.cs
│   └── Dialogs/                   # 弹窗ViewModel
└── Services/                      # 客户端服务(可选)
```

**模块清单**:

| 模块 | 主要视图 | ViewModel数 | 说明 |
|------|----------|-------------|------|
| Auth | LoginView | 1 | 登录、令牌管理 |
| Users | UserManagement | 2 | 用户CRUD |
| Patients | PatientSelection | 5 | 患者选择、CRUD |
| MedicalCase | MedicalCaseWorkspace | 17 | 医案核心工作区 |
| Consultation | ConsultationForm | 1 | 诊断录入 |
| Prescriptions | PrescriptionPanel | 2 | 处方编辑 |
| Herbs | HerbManagement | 2 | 药材CRUD |
| Formula | FormulaManagement | 2 | 经验方CRUD |

#### Scenario: 创建业务视图
- **WHEN** 需要新增功能界面
- **THEN** SHALL 创建{Feature}View.xaml和{Feature}ViewModel.cs
- **AND** View SHALL 只包含XAML声明
- **AND** ViewModel SHALL 继承UnifiedViewModelBase

#### Scenario: 创建弹窗
- **WHEN** 需要模态对话框
- **THEN** SHALL 创建{Dialog}Dialog.xaml和{Dialog}DialogViewModel.cs
- **AND** ViewModel SHALL 实现IDialogAware
- **AND** SHALL 使用Prism DialogService

---

### Requirement: CLI-003 Roles层职责

Roles层(2个项目) SHALL 组装角色工作站。

**LYBT.Desktop.Clinical - 临床端**:
- 包含模块: Auth, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula
- 导航配置: 主工作区导航
- 权限配置: 医生角色

**LYBT.Desktop.Admin - 管理端**:
- 包含模块: Auth, Users, Patients, Herbs, Formula
- 导航配置: 管理功能导航
- 权限配置: 管理员角色

#### Scenario: 配置角色工作站
- **WHEN** 添加新角色工作站
- **THEN** SHALL 创建LYBT.Desktop.{Role}项目
- **AND** SHALL 引用需要的业务模块
- **AND** SHALL 配置模块加载顺序

---

### Requirement: CLI-004 Shell层职责

Shell层(LYBT.Desktop.Shell) SHALL 作为应用入口。

**职责**:
- 应用启动和初始化
- 主窗口和Region定义
- 模块加载编排
- 全局异常处理

**目录结构**:
```
LYBT.Desktop.Shell/
├── App.xaml                       # PrismApplication
├── App.xaml.cs                    # 模块配置
├── Views/
│   └── MainWindow.xaml            # 主窗口
└── ViewModels/
    └── MainWindowViewModel.cs
```

#### Scenario: 配置模块加载
- **WHEN** 应用启动
- **THEN** App.xaml.cs SHALL 在ConfigureModuleCatalog中配置模块
- **AND** SHALL 指定InitializationMode

#### Scenario: 定义Region
- **WHEN** 需要动态内容区域
- **THEN** SHALL 在MainWindow定义RegionName
- **AND** 模块 SHALL 通过RegionManager导航

---

### Requirement: CLI-005 ViewModel基类规范

ViewModel SHALL 遵循标准基类层次。

**基类层次**:
```csharp
BindableBase (Prism)
    └── UnifiedViewModelBase
        ├── IsLoading, IsRefreshing
        ├── ErrorMessage, SuccessMessage
        ├── INavigationAware实现
        └── DialogViewModelBase
            └── IDialogAware实现
```

**UnifiedViewModelBase核心功能**:
- 加载状态管理(IsLoading, IsRefreshing)
- 消息状态管理(ErrorMessage, SuccessMessage)
- 导航生命周期(OnNavigatedTo, OnNavigatedFrom)
- 通用服务注入(IRegionManager, IEventAggregator, IDialogService)

#### Scenario: 创建列表ViewModel
- **WHEN** 实现列表功能
- **THEN** SHALL 继承UnifiedViewModelBase
- **AND** SHALL 使用ObservableCollection<T>
- **AND** SHALL 实现分页属性(CurrentPage, TotalPages)

#### Scenario: 创建表单ViewModel
- **WHEN** 实现表单功能
- **THEN** SHALL 继承UnifiedViewModelBase
- **AND** SHALL 使用DTO属性绑定
- **AND** SHALL 实现Save/Cancel命令

#### Scenario: 使用异步命令
- **WHEN** 命令执行异步操作
- **THEN** SHALL 使用DelegateCommand
- **AND** SHALL 在执行前设置IsLoading=true
- **AND** SHALL 在finally中设置IsLoading=false

---

### Requirement: CLI-006 模块注册规范

模块 SHALL 通过标准方式注册。

**注册内容**:
- Repository (Singleton)
- ViewModel (Transient)
- View导航 (RegisterForNavigation)
- Dialog (RegisterDialog)

**示例**:
```csharp
public class {Domain}Module : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Repository
        containerRegistry.RegisterSingleton<I{Entity}Repository, {Entity}Repository>();

        // ViewModel
        containerRegistry.Register<{Feature}ViewModel>();

        // View导航
        containerRegistry.RegisterForNavigation<{Feature}View>();

        // Dialog
        containerRegistry.RegisterDialog<{Dialog}Dialog, {Dialog}DialogViewModel>();
    }
}
```

#### Scenario: 注册Repository
- **WHEN** 模块有数据访问需求
- **THEN** SHALL 使用RegisterSingleton注册Repository
- **AND** SHALL 注册接口到实现

#### Scenario: 注册导航视图
- **WHEN** 视图需要参与Region导航
- **THEN** SHALL 使用RegisterForNavigation注册
- **AND** View SHALL 自动关联同名ViewModel

#### Scenario: 注册对话框
- **WHEN** 需要模态对话框
- **THEN** SHALL 使用RegisterDialog注册
- **AND** SHALL 指定View和ViewModel类型

---

## Cross-Reference

| 相关规范 | 关联说明 |
|----------|----------|
| project-architecture | 项目架构总览 |
| viewmodel-conventions | ViewModel命名和模式 |
| shared-layer-architecture | DTO定义来自Shared层 |
| server-layer-architecture | API契约与Server层对应 |

---

## Changelog

| 日期 | 版本 | 变更 |
|------|------|------|
| 2025-12-04 | 1.0 | 初始版本，定义Client层架构规范 |
