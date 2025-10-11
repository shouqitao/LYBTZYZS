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
- **LYBT.Desktop.Foundation**: 提供Repository基类和ApiClient。
- **模块内 Repositories/**: 提供与后端交互的数据访问层实现。

## 🚀 快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src\Client\Desktop\Modules\Prescriptions\LYBT.Desktop.Prescriptions.csproj
```

## 🔌 数据访问层架构

此项目为UI模块，采用 **ViewModel → Repository → ApiClient** 三层架构：

- **ViewModel 层**：UI业务逻辑，通过依赖注入获取 Repository
- **Repository 层**（模块内 `Repositories/`）：数据访问与转换，调用 Foundation 层的 ApiClient
- **ApiClient 层**（Foundation）：统一的HTTP通信封装

### 代码示例

```csharp
// PrescriptionManagementViewModel.cs
using LYBT.Desktop.Prescriptions.Interfaces;

public class PrescriptionManagementViewModel : UnifiedViewModelBase
{
    private readonly IPrescriptionRepository _prescriptionRepository;

    public PrescriptionManagementViewModel(IPrescriptionRepository prescriptionRepository, ...)
    {
        _prescriptionRepository = prescriptionRepository;
    }

    private async Task SavePrescriptionAsync()
    {
        var prescriptionToSave = new PrescriptionCreateDto { ... };
        var createdPrescription = await _prescriptionRepository.CreateAsync(prescriptionToSave);
        if (createdPrescription != null)
        {
            // 保存成功
        }
    }
}
```

**关键差异**：
- ❌ 禁止直接依赖 `LYBT.Desktop.Services` 的 Server Service（会导致运行时崩溃）
- ✅ 使用模块内 Repository，返回裸类型而非 `Result<T>`

---

*（详细的内部视图、状态管理等信息请参考本文档后续章节。）*