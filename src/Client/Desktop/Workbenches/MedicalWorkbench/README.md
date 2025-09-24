# LYBT.Desktop.Workbench.Medical - 诊疗工作台

## 🎯 项目概述

**诊疗工作台**是专为医生设计的综合性诊疗环境，它聚合了患者管理、四诊记录、辨证论治、处方开具等多个核心业务模块的视图，提供一站式的完整诊疗流程支持。本项目基于Prism MVVM架构，是医生日常工作的主要交互界面。

## 📦 项目结构

```
MedicalWorkbench/
├── Navigation/                  # 工作台内部导航定义
│   └── IMedicalWorkbenchNavigator.cs
├── Services/                   # 导航服务实现
│   └── MedicalWorkbenchNavigator.cs
├── ViewModels/                 # 主工作台视图模型
│   └── MedicalWorkbenchMainViewModel.cs
├── Views/                      # 主工作台视图
│   └── MedicalWorkbenchMainView.xaml
└── MedicalWorkbenchModule.cs     # Prism模块定义与注册
```

## 🛠 技术栈

- **.NET 8 & WPF**: 基础框架。
- **Prism.DryIoc**: 用于模块化、依赖注入和区域导航（Region Navigation）。
- **LYBT.Desktop.Core**: 提供ViewModel基类和通用服务。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Workbenches\MedicalWorkbench\LYBT.Desktop.Workbench.Medical.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `Services` 层实现的业务服务接口（如 `IPatientService`, `IConsultationService` 等），并调用这些服务来完成业务操作。

```csharp
// MedicalWorkbenchMainViewModel.cs
public class MedicalWorkbenchMainViewModel : CoreViewModel
{
    private readonly IPatientService _patientService;

    public MedicalWorkbenchMainViewModel(IPatientService patientService)
    {
        _patientService = patientService;
    }

    private async Task LoadPatientDetails(Guid patientId)
    {
        // 调用业务服务，间接与后端API交互
        var result = await _patientService.GetByIdAsync(patientId);
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部导航、集成模块等信息请参考本文档后续章节。）*