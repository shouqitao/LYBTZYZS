# LYBT.Desktop.Herbs - 中药材管理模块

## 🎯 项目概述

**中药材管理模块 (Herbs Module)** 是WPF桌面客户端的业务模块之一，采用MVVM架构。它为用户提供管理中药材信息（如名称、价格、功效）的用户界面，支持药材的检索、编辑和批量管理。

## 📦 项目结构

```
LYBT.Desktop.Herbs/
├── ViewModels/              # MVVM视图模型
│   ├── HerbListViewModel.cs   # 列表视图模型
│   └── HerbEditViewModel.cs   # 编辑视图模型
├── Views/                   # WPF视图
│   ├── HerbListView.xaml        # 药材列表界面
│   └── HerbEditView.xaml        # 药材编辑界面
└── HerbsModule.cs             # Prism模块定义与注册
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc**: 用于模块化、依赖注入和区域导航。
- **LYBT.Desktop.Core**: 提供ViewModel基类和通用服务。
- **LYBT.Desktop.Services**: 提供与后端交互的业务服务实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Herbs\LYBT.Desktop.Herbs.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IHerbService` 接口，并调用该服务来完成所有药材相关的业务操作。

```csharp
// HerbListViewModel.cs
public class HerbListViewModel : CoreViewModel
{
    private readonly IHerbService _herbService;

    public HerbListViewModel(IHerbService herbService)
    {
        _herbService = herbService;
    }

    private async Task LoadHerbs()
    {
        var result = await _herbService.GetPagedAsync(new PagedQueryBaseDto());
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*