# 看诊工作流优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将医生看诊入口从 3 步导航（主页→选择→工作区）简化为 1 步（登录即工作台），患者列表常驻左侧。

**Architecture:** 新建 ClinicalWorkspaceView 作为复合视图（左：PatientSelectionControl + 右：MedicalCaseEditControl），通过 ClinicalWorkspaceViewModel 协调两者。医生登录后默认导航到此视图。

**Tech Stack:** WPF / Prism / CommunityToolkit.Mvvm

---

### Task 1: 添加 ViewNames 常量 + 注册导航

**Covers:** [S3], [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs`

- [ ] **Step 1: 添加 ViewNames 常量**

在 `ViewNames.cs` 的 `#region 工作台/选择视图` 中添加：

```csharp
/// <summary>临床工作台（患者列表+看诊工作区一体化）</summary>
public const string ClinicalWorkspace = "ClinicalWorkspaceView";
```

- [ ] **Step 2: 在 ClinicalModule 中注册导航**

在 `ClinicalModule.cs` 的 `RegisterTypes` 方法中添加：

```csharp
containerRegistry.RegisterForNavigation<Views.ClinicalWorkspaceView>();
```

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 编译错误（ClinicalWorkspaceView 尚不存在），这是预期的

- [ ] **Step 4: 暂不提交，等 Task 2 完成后一起提交**

### Task 2: 创建 ClinicalWorkspaceView XAML + ViewModel

**Covers:** [S3], [S4], [S7]

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml.cs`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs`

- [ ] **Step 1: 创建 ClinicalWorkspaceViewModel.cs**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Clinical.ViewModels;

/// <summary>
/// 临床工作台 ViewModel — 协调患者列表和看诊工作区
/// </summary>
public partial class ClinicalWorkspaceViewModel : NavigableViewModelBase
{
    private readonly IPatientService _patientService;
    private readonly INavigationCoordinator _navigationCoordinator;

    [ObservableProperty]
    private PatientListDto? _selectedPatient;

    [ObservableProperty]
    private bool _isLoadingPatient;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ClinicalWorkspaceViewModel(
        IViewModelServices services,
        IPatientService patientService,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _patientService = patientService;
        _navigationCoordinator = navigationCoordinator;
        PageTitle = "看诊工作台";
    }

    /// <summary>选中患者变化时触发 — 加载历史就诊信息</summary>
    partial void OnSelectedPatientChanged(PatientListDto? value)
    {
        if (value != null)
        {
            Logger.LogInformation("选中患者: {PatientName} ({PatientId})", value.Name, value.Id);
        }
    }

    /// <summary>开始看诊 — 导航到医案工作区</summary>
    [RelayCommand]
    private void StartConsultation()
    {
        if (SelectedPatient == null) return;

        var parameters = MedicalCaseNavigationParameters.ForClinical(SelectedPatient.Id);
        _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseWorkspace, parameters);
    }

    /// <summary>新建患者</summary>
    [RelayCommand]
    private void NewPatient()
    {
        _navigationCoordinator.NavigateTo(ViewNames.PatientManagement,
            new Dictionary<string, object> { { "Action", "AddNew" } });
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        Logger.LogInformation("进入临床工作台");
    }
}
```

- [ ] **Step 2: 创建 ClinicalWorkspaceView.xaml**

```xml
<UserControl x:Class="LYBT.Desktop.Clinical.Views.ClinicalWorkspaceView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:patients="clr-namespace:LYBT.Desktop.Patients.Controls;assembly=LYBT.Desktop.Patients"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             d:DesignHeight="800" d:DesignWidth="1200">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="320" MinWidth="260" />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- 左列：患者选择列表 -->
        <Border Grid.Column="0" Background="{DynamicResource SurfaceBrush}" BorderBrush="{DynamicResource BorderBrush}" BorderThickness="0,0,1,0">
            <patients:PatientSelectionControl
                x:Name="PatientListPanel"
                SelectedPatient="{Binding SelectedPatient, Mode=TwoWay}" />
        </Border>

        <GridSplitter Grid.Column="1" Width="4" HorizontalAlignment="Stretch" Background="{DynamicResource BorderBrush}" />

        <!-- 右列：看诊工作区 -->
        <Grid Grid.Column="2" Margin="{StaticResource SpacingLarge}">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- 患者信息栏（选中患者时显示） -->
            <Border Grid.Row="0" Margin="0,0,0,12" Padding="16,12"
                    Background="{DynamicResource LightPrimaryBrush}" CornerRadius="8"
                    Visibility="{Binding SelectedPatient, Converter={x:Static converters:Cvt.NotNullToVis}}">
                <StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{Binding SelectedPatient.Name}" FontSize="18" FontWeight="Bold" />
                        <TextBlock Text="{Binding SelectedPatient.GenderText}" Margin="12,0,0,0" FontSize="14" Foreground="{DynamicResource SecondaryTextBrush}" />
                        <TextBlock Text="{Binding SelectedPatient.AgeText}" Margin="8,0,0,0" FontSize="14" Foreground="{DynamicResource SecondaryTextBrush}" />
                    </StackPanel>
                    <!-- 历史信息在 Task 3 中添加 -->
                </StackPanel>
            </Border>

            <!-- 操作提示（未选患者时显示） -->
            <Border Grid.Row="1" Margin="0,24" Padding="24" HorizontalAlignment="Center"
                    Background="{DynamicResource SurfaceBrush}" CornerRadius="8"
                    Visibility="{Binding SelectedPatient, Converter={x:Static converters:Cvt.NullToVis}}">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="👈 请从左侧选择一位患者" FontSize="16" Foreground="{DynamicResource SecondaryTextBrush}" />
                    <TextBlock Text="或点击下方按钮新建患者" Margin="0,8,0,0" FontSize="13" Foreground="{DynamicResource DisabledTextBrush}" HorizontalAlignment="Center" />
                </StackPanel>
            </Border>

            <!-- 看诊操作区（选中患者时显示） -->
            <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,16"
                        Visibility="{Binding SelectedPatient, Converter={x:Static converters:Cvt.NotNullToVis}}">
                <Button Content="开始看诊" Command="{Binding StartConsultationCommand}"
                        Padding="32,12" FontSize="16" Margin="0,0,12,0" />
                <Button Content="+ 新建患者" Command="{Binding NewPatientCommand}"
                        Padding="20,12" FontSize="14" />
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

注意：需要在 XAML 中添加 `xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure"`。如果 `Cvt.NotNullToVis` / `Cvt.NullToVis` 不存在，需要先创建这两个转换器（在 Task 2 Step 3 中处理）。

- [ ] **Step 3: 创建 NullToVis / NotNullToVis 转换器（如不存在）**

检查 `ConverterInstances.cs` 是否有 `NullToVis` 和 `NotNullToVis`。如果没有，创建：

在 `Converters/` 文件夹中创建 `NullToVisibilityConverter.cs`：

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LYBT.Desktop.Infrastructure.Converters;

/// <summary>null → Collapsed, non-null → Visible</summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>null → Visible, non-null → Collapsed</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value == null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

在 `ConverterInstances.cs` 中添加：

```csharp
public static readonly IValueConverter NotNullToVis = new NotNullToVisibilityConverter();
public static readonly IValueConverter NullToVis = new NullToVisibilityConverter();
```

- [ ] **Step 4: 创建 ClinicalWorkspaceView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Clinical.Views;

public partial class ClinicalWorkspaceView : UserControl
{
    public ClinicalWorkspaceView()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 5: 检查 PatientSelectionControl 的 DependencyProperty**

确认 `PatientSelectionControl` 有 `SelectedPatient` DependencyProperty（如没有需要添加）。检查 `PatientSelectionControl.xaml.cs` 是否暴露 `SelectedPatient` 属性。如果只有事件没有 DependencyProperty，需要添加一个 `SelectedPatient` 依赖属性供双向绑定。

- [ ] **Step 6: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 7: Commit Tasks 1+2**

```bash
git add -A
git commit -m "feat(clinical): create ClinicalWorkspaceView with patient list + consultation area"
```

### Task 3: 患者历史信息面板

**Covers:** [S4], [S5]

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs` — 加载历史
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalWorkspaceView.xaml` — 显示历史

- [ ] **Step 1: 在 ViewModel 中添加历史加载逻辑**

在 `OnSelectedPatientChanged` 方法中，调用 API 加载该患者最近 5 次就诊记录：

```csharp
[ObservableProperty]
private ObservableCollection<MedicalCaseHistoryItem> _patientHistory = new();

partial void OnSelectedPatientChanged(PatientListDto? value)
{
    if (value != null)
    {
        Logger.LogInformation("选中患者: {PatientName}", value.Name);
        _ = LoadPatientHistoryAsync(value.Id);
    }
    else
    {
        PatientHistory.Clear();
    }
}

private async Task LoadPatientHistoryAsync(Guid patientId)
{
    try
    {
        IsLoadingPatient = true;
        // 使用 IMedicalCaseRepository 获取患者历史
        // var history = await _medicalCaseRepository.GetByPatientIdAsync(patientId);
        // PatientHistory.Clear();
        // foreach (var item in history.Data?.Take(5) ?? Enumerable.Empty<...>())
        //     PatientHistory.Add(new MedicalCaseHistoryItem(item));
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载患者历史失败");
    }
    finally
    {
        IsLoadingPatient = false;
    }
}
```

创建简单的历史展示模型：

```csharp
public record MedicalCaseHistoryItem(DateTime Date, string Diagnosis, string Prescription, int Dosage);
```

- [ ] **Step 2: 在 XAML 中添加历史折叠面板**

在患者信息栏下方添加：

```xml
<!-- 历史记录折叠面板 -->
<Expander Grid.Row="2" Header="📋 历史就诊记录" IsExpanded="False" Margin="0,0,0,12"
          Visibility="{Binding SelectedPatient, Converter={x:Static converters:Cvt.NotNullToVis}}">
    <ItemsControl ItemsSource="{Binding PatientHistory}" MaxHeight="200">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border Padding="12,8" Margin="0,0,0,4" Background="{DynamicResource SurfaceBrush}" CornerRadius="4">
                    <StackPanel Orientation="Horizontal">
                        <TextBlock Text="{Binding Date, StringFormat=yyyy-MM-dd}" FontWeight="SemiBold" Width="100" />
                        <TextBlock Text="{Binding Diagnosis}" Width="200" TextTrimming="CharacterEllipsis" />
                        <TextBlock Text="{Binding Prescription}" Foreground="{DynamicResource SecondaryTextBrush}" TextTrimming="CharacterEllipsis" />
                    </StackPanel>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Expander>
```

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(clinical): add patient history panel to ClinicalWorkspaceView"
```

### Task 4: 医生登录后默认导航到 ClinicalWorkspace

**Covers:** [S3], [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/DoctorRoleDefinition.cs` — 改 HomeViewName
- Modify: `src/Client/Desktop/Shell/Services/NavigationCoordinator.cs` — 确认导航逻辑

- [ ] **Step 1: 修改 DoctorRoleDefinition 的 HomeViewName**

将 `HomeViewName` 从 `ViewNames.ClinicalHome` 改为 `ViewNames.ClinicalWorkspace`：

```csharp
public override string HomeViewName => ViewNames.ClinicalWorkspace;
```

- [ ] **Step 2: 确认 NavigationCoordinator.NavigateToHome() 使用 HomeViewName**

检查 `NavigationCoordinator.cs` 的 `NavigateToHome()` 方法是否使用 `RoleRegistry.GetHomeViewName(role)`。如果是，则修改 `DoctorRoleDefinition` 即可自动生效。

- [ ] **Step 3: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(clinical): doctor login now defaults to ClinicalWorkspaceView"
```

### Task 5: Sidebar "看诊"导航目标更新

**Covers:** [S3], [S7]

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs` — "开始看诊"命令改为直接导航到 ClinicalWorkspace

- [ ] **Step 1: 修改 ClinicalHomeViewModel 的 StartMedicalCase 命令**

```csharp
[RelayCommand]
private void StartMedicalCase()
{
    _navigationCoordinator.NavigateTo(ViewNames.ClinicalWorkspace);
}
```

- [ ] **Step 2: 构建验证**

Run: `dotnet build LYBTZYZS.sln --no-restore`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git commit -m "feat(clinical): ClinicalHomeView now navigates to ClinicalWorkspaceView"
```

### Task 6: 最终构建 + 测试

**Covers:** [S2], [S7]

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 2: 运行 Architecture 测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`

- [ ] **Step 3: 提交最终状态**

```bash
git add -A
git commit -m "feat(clinical): workflow optimization complete — 1-step consultation entry"
```
