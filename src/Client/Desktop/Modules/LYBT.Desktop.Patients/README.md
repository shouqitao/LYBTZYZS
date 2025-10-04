# LYBT.Desktop.Patients - 患者管理模块

## 🎯 项目概述

**患者管理模块 (Patients Module)** 是WPF桌面客户端的核心业务模块之一，采用MVVM架构。它为用户提供管理患者基本信息、就诊记录和健康档案的用户界面，支持快速搜索和信息维护，是整个诊疗流程的入口。

## 📦 项目结构

```
LYBT.Desktop.Patients/
├── ViewModels/              # MVVM视图模型
│   ├── PatientListViewModel.cs  # 列表视图模型
│   └── PatientEditViewModel.cs  # 编辑视图模型
├── Views/                   # WPF视图
│   ├── PatientListView.xaml       # 患者列表界面
│   └── PatientEditView.xaml       # 患者编辑界面
└── PatientsModule.cs          # Prism模块定义与注册
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
dotnet build src\Client\Desktop\Modules\Patients\LYBT.Desktop.Patients.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IPatientService` 接口，并调用该服务来完成所有患者相关的业务操作。

```csharp
// PatientListViewModel.cs
public class PatientListViewModel : CoreViewModel
{
    private readonly IPatientService _patientService;

    public PatientListViewModel(IPatientService patientService)
    {
        _patientService = patientService;
    }

    private async Task LoadPatients()
    {
        var result = await _patientService.GetPagedAsync(new PagedQueryBaseDto());
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*