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
- **LYBT.Desktop.Foundation**: 提供Repository基类和ApiClient。
- **模块内 Repositories/**: 提供与后端交互的数据访问层实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Consultation\LYBT.Desktop.Consultation.csproj
```

## 🔌 数据访问层架构

此项目为UI模块，采用 **ViewModel → Repository → ApiClient** 三层架构：

- **ViewModel 层**：UI业务逻辑，通过依赖注入获取 Repository
- **Repository 层**（模块内 `Repositories/`）：数据访问与转换，调用 Foundation 层的 ApiClient
- **ApiClient 层**（Foundation）：统一的HTTP通信封装

### 代码示例

```csharp
// ConsultationManagementViewModel.cs
using LYBT.Desktop.Consultation.Repositories;

public class ConsultationManagementViewModel : UnifiedViewModelBase
{
    private readonly IConsultationRepository _consultationRepository;

    public ConsultationManagementViewModel(IConsultationRepository consultationRepository, ...)
    {
        _consultationRepository = consultationRepository;
    }

    private async Task LoadConsultationsAsync()
    {
        var result = await _consultationRepository.GetPagedAsync(1, 100);
        if (result != null && result.Items != null)
        {
            foreach (var consultation in result.Items)
            {
                Consultations.Add(consultation);
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