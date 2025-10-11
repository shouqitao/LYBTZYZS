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
- **LYBT.Desktop.Foundation**: 提供Repository基类和ApiClient。
- **模块内 Repositories/**: 提供与后端交互的数据访问层实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Formula\LYBT.Desktop.Formula.csproj
```

## 🔌 数据访问层架构

此项目为UI模块，采用 **ViewModel → Repository → ApiClient** 三层架构：

- **ViewModel 层**：UI业务逻辑，通过依赖注入获取 Repository
- **Repository 层**（模块内 `Repositories/`）：数据访问与转换，调用 Foundation 层的 ApiClient
- **ApiClient 层**（Foundation）：统一的HTTP通信封装

### 代码示例

```csharp
// FormulaManagementViewModel.cs
using LYBT.Desktop.Formula.Interfaces;

public class FormulaManagementViewModel : UnifiedViewModelBase
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaManagementViewModel(IFormulaRepository formulaRepository, ...)
    {
        _formulaRepository = formulaRepository;
    }

    private async Task LoadFormulasAsync()
    {
        var result = await _formulaRepository.GetPagedAsync(1, 100);
        if (result != null && result.Items != null)
        {
            foreach (var formula in result.Items)
            {
                Formulas.Add(formula);
            }
        }
    }
}
```

**关键差异**：
- ❌ 禁止直接依赖 `LYBT.Desktop.Services` 的 Server Service（会导致运行时崩溃）
- ✅ 使用模块内 Repository，返回裸类型而非 `Result<T>`

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*