# LYBT.Desktop.Prescriptions - 处方管理模块

## 🎯 项目概述

**处方管理模块 (Prescriptions Module)** 是WPF桌面客户端的业务模块之一，采用MVVM架构。它为医生提供开具和管理中医处方的用户界面，支持药材选择、剂量计算、配伍检查和价格预览等核心功能。

## 📦 项目结构

```
LYBT.Desktop.Prescriptions/
├── ViewModels/              # MVVM视图模型
│   ├── PrescriptionListViewModel.cs # 列表视图模型
│   └── PrescriptionEditViewModel.cs # 编辑视图模型
├── Views/                   # WPF视图
│   ├── PrescriptionListView.xaml    # 处方列表界面
│   └── PrescriptionEditView.xaml    # 处方编辑界面
└── PrescriptionsModule.cs     # Prism模块定义与注册
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
dotnet build src\Client\Desktop\Modules\Prescriptions\LYBT.Desktop.Prescriptions.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IPrescriptionService` 接口，并调用该服务来完成所有处方相关的业务操作。

```csharp
// PrescriptionEditViewModel.cs
public class PrescriptionEditViewModel : CoreViewModel
{
    private readonly IPrescriptionService _prescriptionService;

    public PrescriptionEditViewModel(IPrescriptionService prescriptionService)
    {
        _prescriptionService = prescriptionService;
    }

    private async Task SavePrescription()
    {
        var prescriptionToSave = new PrescriptionCreateDto { ... };
        var result = await _prescriptionService.CreateAsync(prescriptionToSave);
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*