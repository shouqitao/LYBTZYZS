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
- **LYBT.Desktop.Services**: 提供与后端交互的业务服务实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\MedicalCase\LYBT.Desktop.MedicalCase.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IMedicalCaseService` 接口，并调用该服务来完成所有医案相关的业务操作。

```csharp
// MedicalCaseListViewModel.cs
public class MedicalCaseListViewModel : CoreViewModel
{
    private readonly IMedicalCaseService _medicalCaseService;

    public MedicalCaseListViewModel(IMedicalCaseService medicalCaseService)
    {
        _medicalCaseService = medicalCaseService;
    }

    private async Task LoadMedicalCases()
    {
        var result = await _medicalCaseService.GetPagedAsync(new PagedQueryBaseDto());
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*