# LYBT.Desktop.MedicalCase - 医案管理模块

## 🎯 项目概述

**医案管理模块 (MedicalCase Module)** 是WPF桌面客户端的业务模块之一，采用MVVM架构。它作为整个诊疗流程的**容器**，负责创建、跟踪和管理一次完整的就诊记录（即“医案”），并将患者信息、四诊记录、处方等关联起来。

## 📦 项目结构

```
LYBT.Desktop.MedicalCase/
├── ViewModels/              # MVVM视图模型
│   ├── MedicalCaseListViewModel.cs # 列表视图模型
│   └── MedicalCaseDetailView.cs   # 详情视图模型
├── Views/                   # WPF视图
│   ├── MedicalCaseListView.xaml     # 医案列表界面
│   └── MedicalCaseDetailView.xaml   # 医案详情界面
└── MedicalCaseModule.cs       # Prism模块定义与注册
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
dotnet build src\Client\Desktop\Modules\MedicalCase\LYBT.Desktop.MedicalCase.csproj
```

## 🔌 数据访问层架构

此项目为UI模块，采用 **ViewModel → Repository → ApiClient** 三层架构：

- **ViewModel 层**：UI业务逻辑，通过依赖注入获取 Repository
- **Repository 层**（模块内 `Repositories/`）：数据访问与转换，调用 Foundation 层的 ApiClient
- **ApiClient 层**（Foundation）：统一的HTTP通信封装

### 代码示例

```csharp
// MedicalCaseManagementViewModel.cs
using LYBT.Desktop.MedicalCase.Repositories;

public class MedicalCaseManagementViewModel : UnifiedViewModelBase
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public MedicalCaseManagementViewModel(IMedicalCaseRepository medicalCaseRepository, ...)
    {
        _medicalCaseRepository = medicalCaseRepository;
    }

    private async Task LoadMedicalCasesAsync()
    {
        var result = await _medicalCaseRepository.GetPagedAsync(1, 100);
        if (result != null && result.Items != null)
        {
            foreach (var medicalCase in result.Items)
            {
                MedicalCases.Add(medicalCase);
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