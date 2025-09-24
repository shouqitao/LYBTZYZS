# LYBT.Desktop.Consultation - 诊疗管理模块

## 🎯 项目概述

**诊疗管理模块 (Consultation Module)** 是WPF桌面客户端的核心业务模块之一，采用MVVM架构。它为医生提供了记录中医四诊（望、闻、问、切）信息、进行辨证论治和管理诊断记录的用户界面和业务逻辑。

## 📦 项目结构

```
LYBT.Desktop.Consultation/
├── ViewModels/              # MVVM视图模型
│   ├── ConsultationViewModel.cs     # 主视图模型
│   └── ConsultationListViewModel.cs # 列表视图模型
├── Views/                   # WPF视图
│   ├── ConsultationView.xaml        # 诊疗主界面
│   └── FourDiagnosesView.xaml     # 四诊录入界面
├── Services/                # 客户端业务服务 (可选)
└── ConsultationModule.cs      # Prism模块定义与注册
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
dotnet build src\Client\Desktop\Modules\Consultation\LYBT.Desktop.Consultation.csproj
```

## 🔌 API 接口

此项目为UI模块，不直接调用API。它通过依赖注入获取在 `LYBT.Desktop.Services` 层实现的 `IConsultationService` 接口，并调用该服务来完成所有业务操作。

```csharp
// ConsultationViewModel.cs
public class ConsultationViewModel : CoreViewModel
{
    private readonly IConsultationService _consultationService;

    public ConsultationViewModel(IConsultationService consultationService)
    {
        _consultationService = consultationService;
    }

    private async Task LoadConsultations()
    {
        var result = await _consultationService.GetPagedAsync(new PagedQueryBaseDto());
        if(result.IsSuccess)
        {
            // ...
        }
    }
}
```

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*