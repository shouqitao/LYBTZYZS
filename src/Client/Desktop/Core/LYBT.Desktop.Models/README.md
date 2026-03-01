# LYBT.Desktop.Models

> ViewModel基类与验证模型 | MVVM核心 | CommunityToolkit.Mvvm + Prism

## 项目定位

- **层级**: Client Core层
- **职责**: 提供MVVM模式的ViewModel基类体系 (CoreViewModelBase / DialogViewModelBase / NavigableViewModelBase)、可验证模型基类 (ValidatableModelBase)、RFC 7807错误响应模型

## 目录结构

```
LYBT.Desktop.Models/
├── Http/
│   └── ProblemDetails.cs                # RFC 7807 标准错误响应模型
└── ViewModels/Base/
    ├── CoreViewModelBase.cs             # 核心ViewModel基类 (ObservableObject)
    ├── DialogViewModelBase.cs           # 对话框ViewModel基类 (IDialogAware)
    ├── NavigableViewModelBase.cs        # 可导航ViewModel基类 (INavigationAware)
    ├── ValidatableModelBase.cs          # 可验证模型基类 (INotifyDataErrorInfo)
    └── ValidationAccessors.cs           # 验证错误索引器 (XAML绑定支持)
```

## 核心组件

| 组件 | 基类 | 说明 |
|------|------|------|
| CoreViewModelBase | ObservableObject (CommunityToolkit.Mvvm) | 状态管理/异步执行/日志/资源释放 |
| DialogViewModelBase | CoreViewModelBase + IDialogAware | 对话框生命周期/参数传递/关闭控制 |
| NavigableViewModelBase | CoreViewModelBase + INavigationAware | 区域导航/会话管理/未保存变更保护 |
| ValidatableModelBase | BindableBase (Prism) + INotifyDataErrorInfo | DataAnnotations验证/XAML错误绑定 |
| ValidationAccessors | -- | 验证错误/状态索引器，支持 `Errors["PropName"]` 绑定 |
| ProblemDetails | -- | RFC 7807 错误响应客户端模型 |

## ViewModel 继承体系

```
ObservableObject (CommunityToolkit.Mvvm)
  └── CoreViewModelBase (状态管理 + 异步执行 + 日志 + IDisposable)
        ├── DialogViewModelBase (IDialogAware + CancelCommand + ConfirmCommand)
        └── NavigableViewModelBase (INavigationAware + IConfirmNavigationRequest)
              └── MasterDetailViewModelBase (在 Infrastructure 层定义)

BindableBase (Prism)
  └── ValidatableModelBase (DataAnnotations + INotifyDataErrorInfo)
        └── 各模块 DetailModel (MedicalCase/Formula/Herb/Patient/User)
```

## 设计依据

- 两套独立继承体系: ViewModel基类基于CommunityToolkit.Mvvm源生成器 (`[ObservableProperty]`)，DetailModel基类基于Prism BindableBase提供验证支持。两者职责不同，不共享继承链
- CoreViewModelBase通过 `IViewModelServices` 聚合服务注入，避免构造函数参数膨胀
- ValidatableModelBase集成INotifyDataErrorInfo，使DataAnnotations验证与WPF绑定引擎无缝协作
- ValidationAccessors提供索引器访问器，支持XAML中 `{Binding Errors[PropertyName]}` 直接绑定验证错误

## 依赖关系

### 依赖
- LYBT.Desktop.Infrastructure (IViewModelServices等服务接口)
- LYBT.Desktop.Contracts (接口定义)
- LYBT.Shared.Models (DTO定义)
- LYBT.Shared.Components (药材业务组件)
- LYBT.Shared.Primitives (基础类型)
- LYBT.Shared.Utilities (工具类)
- CommunityToolkit.Mvvm (源生成器)
- Prism.Core / Prism.Wpf (MVVM框架)
- System.ComponentModel.Annotations (DataAnnotations)
- Microsoft.Extensions.Logging (日志)

### 被依赖
- 所有Desktop业务模块的ViewModel
- 所有Desktop工作站的ViewModel

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 完全重写: 按实际文件结构更新，修正已删除文件 (Exceptions/Mappers/Prescriptions)，更新继承体系为CommunityToolkit.Mvvm |
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |
