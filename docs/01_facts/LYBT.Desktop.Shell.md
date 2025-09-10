# LYBT.Desktop.Shell - 项目事实表

## 1) 基本信息

- **项目名称**: LYBT.Desktop.Shell
- **相对路径**: src/Client/Desktop/Shell
- **项目类型**: WPF  
- **目标框架**: net8.0-windows
- **输出类型**: Exe
- **可空引用**: enable
- **语言版本**: 12.0

## 2) 依赖与引用

### 项目引用 (13个)

- ../Core/LYBT.Desktop.Core.csproj
- ../Infrastructure/LYBT.Desktop.Infrastructure.csproj
- ../Services/LYBT.Desktop.Services.csproj
- ../Modules/Auth/LYBT.Desktop.Auth.csproj
- ../Modules/Users/LYBT.Desktop.Users.csproj  
- ../Modules/Patients/LYBT.Desktop.Patients.csproj
- ../Modules/MedicalCase/LYBT.Desktop.MedicalCase.csproj
- ../Modules/Consultation/LYBT.Desktop.Consultation.csproj
- ../Modules/Prescriptions/LYBT.Desktop.Prescriptions.csproj
- ../Modules/Herbs/LYBT.Desktop.Herbs.csproj
- ../Modules/Formula/LYBT.Desktop.Formula.csproj
- ../Workbenches/ConsultationWorkbench/LYBT.Desktop.Workbench.Consultation.csproj
- ../Workbenches/SystemWorkbench/LYBT.Desktop.Workbench.Admin.csproj

### NuGet包引用 (3个)

- Prism.Core
- Prism.DryIoc
- Prism.Wpf

## 3) 公共暴露面

### WPF界面 (6个Views)

#### 主界面

- **Views/MainWindow.xaml** ↔ **ViewModels/MainWindowViewModel.cs** ✅ 匹配
- **Views/HomeView.xaml** ↔ **ViewModels/HomeViewModel.cs** ✅ 匹配

#### 开发工具

- **Views/UIShowcaseWindow.xaml** ↔ **(unknown)** ❌ 未匹配

#### 系统对话框

- **Dialogs/Views/ConfirmationDialog.xaml** ↔ **Dialogs/ViewModels/ConfirmationDialogViewModel.cs** ✅ 匹配
- **Dialogs/Views/ErrorDetailsDialog.xaml** ↔ **Dialogs/ViewModels/ErrorDetailsDialogViewModel.cs** ✅ 匹配
- **Dialogs/Views/InformationDialog.xaml** ↔ **Dialogs/ViewModels/InformationDialogViewModel.cs** ✅ 匹配

## 4) 数据模型

- **DbContext**: 无
- **DbSet列表**: 无
- **主要实体**: 无
- **DTO类型**: 无
- **实体↔DTO匹配**: 无

## 5) 测试特征

- **测试框架**: 不适用 (非测试项目)
- **测试夹具**: 不适用
- **启动方式**: 不适用
- **集成测试**: 不适用

## 6) 特殊标识

- **IsIntegrationTest**: false
- **IsCore**: false  
- **备注**: WPF应用程序主Shell，使用Prism框架，包含主窗口和应用启动逻辑，集成所有8个业务模块和2个工作台