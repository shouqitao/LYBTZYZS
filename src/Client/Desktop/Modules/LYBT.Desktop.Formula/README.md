# LYBT.Desktop.Formula - 验方管理模块

## 🎯 项目概述

**验方管理模块 (Formula Module)** 是WPF桌面客户端的业务模块之一，采用MVVM架构。它为医生提供管理经典方剂和个人验方的用户界面，支持方剂的创建、编辑、查询和组方配置，是中医知识管理和经验积累的重要工具。

## 📦 项目结构

```
LYBT.Desktop.Formula/
├── ViewModels/              # MVVM视图模型
│   ├── FormulaListViewModel.cs  # 列表视图模型
│   └── FormulaEditViewModel.cs  # 编辑视图模型
├── Views/                   # WPF视图
│   ├── FormulaListView.xaml       # 验方列表界面
│   └── FormulaEditView.xaml       # 验方编辑界面
└── FormulaModule.cs           # Prism模块定义与注册
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
dotnet build src\Client\Desktop\Modules\Formula\LYBT.Desktop.Formula.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IFormulaService` 接口，并调用该服务来完成所有验方相关的业务操作。

```csharp
// FormulaListViewModel.cs
public class FormulaListViewModel : CoreViewModel
{
    private readonly IFormulaService _formulaService;

    public FormulaListViewModel(IFormulaService formulaService)
    {
        _formulaService = formulaService;
    }

    private async Task LoadFormulas()
    {
        var result = await _formulaService.GetPagedAsync(new PagedQueryBaseDto());
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*