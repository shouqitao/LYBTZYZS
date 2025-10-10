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
- **LYBT.Desktop.Foundation**: 提供Repository基类和ApiClient。
- **模块内 Repositories/**: 提供与后端交互的数据访问层实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Herbs\LYBT.Desktop.Herbs.csproj
```

## 🔌 数据访问层架构

此项目为UI模块，采用 **ViewModel → Repository → ApiClient** 三层架构：

- **ViewModel 层**：UI业务逻辑，通过依赖注入获取 Repository
- **Repository 层**（模块内 `Repositories/`）：数据访问与转换，调用 Foundation 层的 ApiClient
- **ApiClient 层**（Foundation）：统一的HTTP通信封装

### 代码示例

```csharp
// HerbManagementViewModel.cs
using LYBT.Desktop.Herbs.Repositories;

public class HerbManagementViewModel : UnifiedViewModelBase
{
    private readonly IHerbRepository _herbRepository;

    public HerbManagementViewModel(IHerbRepository herbRepository, ...)
    {
        _herbRepository = herbRepository;
    }

    private async Task LoadHerbsAsync()
    {
        var result = await _herbRepository.GetPagedAsync(1, 100);
        if (result != null && result.Items != null)
        {
            foreach (var herb in result.Items)
            {
                Herbs.Add(herb);
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