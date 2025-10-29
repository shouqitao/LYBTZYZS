# MVVM模式

**创建日期**: 2025-10-25
**适用范围**: Desktop端（WPF/Avalonia）
**复杂度**: ⭐⭐（中等）

---

## 📋 模式概述

MVVM（Model-View-ViewModel）是Desktop端的标准架构模式，实现UI与业务逻辑的分离。

**核心价值**：
- ✅ **职责分离**：View负责呈现，ViewModel负责逻辑，Model负责数据
- ✅ **可测试性**：ViewModel可独立测试，无需UI
- ✅ **数据绑定**：通过INotifyPropertyChanged实现双向绑定
- ✅ **命令模式**：通过ICommand封装用户操作

---

## 🎯 MVVM三层结构

```
View (XAML + Code-Behind)
  ↓ 数据绑定 + 命令绑定
ViewModel (业务逻辑 + Command)
  ↓ 调用
Model / Repository / API Client
```

---

## 💻 代码示例

### View（XAML）

```xaml
<!-- PrescriptionManagementView.xaml -->
<UserControl x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionManagementView"
             xmlns:prism="http://prismlibrary.com/">

    <!-- 数据绑定到ViewModel -->
    <Grid>
        <!-- 列表 -->
        <DataGrid ItemsSource="{Binding Prescriptions}"
                  SelectedItem="{Binding SelectedPrescription}">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID" Binding="{Binding Id}" />
                <DataGridTextColumn Header="患者姓名" Binding="{Binding PatientName}" />
                <DataGridTextColumn Header="创建时间" Binding="{Binding CreatedAt}" />
            </DataGrid.Columns>
        </DataGrid>

        <!-- 按钮 -->
        <StackPanel Orientation="Horizontal">
            <!-- Command绑定到ViewModel -->
            <Button Content="新增" Command="{Binding CreateCommand}" />
            <Button Content="编辑" Command="{Binding EditCommand}" />
            <Button Content="删除" Command="{Binding DeleteCommand}" />
            <Button Content="刷新" Command="{Binding RefreshCommand}" />
        </StackPanel>
    </Grid>
</UserControl>
```

### ViewModel

```csharp
// PrescriptionManagementViewModel.cs
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;

public class PrescriptionManagementViewModel : BindableBase
{
    // ===== 依赖注入 =====

    private readonly IPrescriptionApi _api;
    private readonly IMedicalCaseRepository _repository;
    private readonly INotificationService _notificationService;

    public PrescriptionManagementViewModel(
        IPrescriptionApi api,
        IMedicalCaseRepository repository,
        INotificationService notificationService)
    {
        _api = api;
        _repository = repository;
        _notificationService = notificationService;

        // 初始化Commands
        CreateCommand = new DelegateCommand(OnCreate);
        EditCommand = new DelegateCommand(OnEdit, CanEdit).ObservesProperty(() => SelectedPrescription);
        DeleteCommand = new DelegateCommand(OnDelete, CanDelete).ObservesProperty(() => SelectedPrescription);
        RefreshCommand = new DelegateCommand(OnRefresh);
    }

    // ===== 属性（数据绑定） =====

    private ObservableCollection<Prescription> _prescriptions;
    public ObservableCollection<Prescription> Prescriptions
    {
        get => _prescriptions;
        set => SetProperty(ref _prescriptions, value);
    }

    private Prescription _selectedPrescription;
    public Prescription SelectedPrescription
    {
        get => _selectedPrescription;
        set => SetProperty(ref _selectedPrescription, value);
    }

    // ===== Commands（命令绑定） =====

    public DelegateCommand CreateCommand { get; }
    public DelegateCommand EditCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    // ===== 业务逻辑 =====

    private void OnCreate()
    {
        // 打开新增对话框
        _dialogService.ShowDialog("PrescriptionEditorDialog");
    }

    private void OnEdit()
    {
        // 打开编辑对话框
        _dialogService.ShowDialog("PrescriptionEditorDialog", new DialogParameters
        {
            { "prescription", SelectedPrescription }
        });
    }

    private bool CanEdit() => SelectedPrescription != null;

    private async void OnDelete()
    {
        try
        {
            // ✅ Write操作：通过聚合根Repository
            await _repository.DeletePrescriptionAsync(SelectedPrescription.Id);

            Prescriptions.Remove(SelectedPrescription);
            _notificationService.ShowSuccess("删除成功");
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"删除失败: {ex.Message}");
        }
    }

    private bool CanDelete() => SelectedPrescription != null;

    private async void OnRefresh()
    {
        try
        {
            // ✅ Read操作：直接使用API
            var response = await _api.GetPrescriptionsAsync(1, 50);
            Prescriptions = new ObservableCollection<Prescription>(response.Data.Items);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"刷新失败: {ex.Message}");
        }
    }
}
```

### Code-Behind（仅UI逻辑）

```csharp
// PrescriptionManagementView.xaml.cs
public partial class PrescriptionManagementView : UserControl
{
    public PrescriptionManagementView()
    {
        InitializeComponent();
    }

    // ✅ 允许：UI交互逻辑（动画、焦点控制）
    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 设置焦点到第一个可编辑单元格
        if (DataGrid.SelectedItem != null)
            DataGrid.Focus();
    }

    // ❌ 禁止：业务逻辑（应该在ViewModel中）
    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        // ❌ 错误：业务逻辑不应该在Code-Behind
        var prescription = DataGrid.SelectedItem as Prescription;
        _repository.DeleteAsync(prescription.Id); // ❌
    }
}
```

---

## ✅ 最佳实践

### 1. ViewModel不依赖View

```csharp
// ✅ 正确：ViewModel独立于View
public class PrescriptionManagementViewModel : BindableBase
{
    // ViewModel不引用View类型
}

// ❌ 错误：ViewModel依赖View
public class PrescriptionManagementViewModel
{
    private PrescriptionManagementView _view; // ❌ 耦合View
}
```

### 2. 使用ObservesProperty自动触发CanExecute

```csharp
// ✅ 正确：ObservesProperty自动刷新
EditCommand = new DelegateCommand(OnEdit, CanEdit)
    .ObservesProperty(() => SelectedPrescription);

private bool CanEdit() => SelectedPrescription != null;

// ❌ 错误：手动RaiseCanExecuteChanged
private Prescription _selectedPrescription;
public Prescription SelectedPrescription
{
    get => _selectedPrescription;
    set
    {
        SetProperty(ref _selectedPrescription, value);
        EditCommand.RaiseCanExecuteChanged(); // ❌ 冗余
    }
}
```

### 3. 异步Command使用AsyncDelegateCommand

```csharp
// ✅ 正确：AsyncDelegateCommand
public AsyncDelegateCommand RefreshCommand { get; }

RefreshCommand = new AsyncDelegateCommand(OnRefreshAsync);

private async Task OnRefreshAsync()
{
    var response = await _api.GetPrescriptionsAsync(1, 50);
    Prescriptions = new ObservableCollection<Prescription>(response.Data.Items);
}

// ❌ 错误：async void
RefreshCommand = new DelegateCommand(OnRefresh);

private async void OnRefresh() // ❌ async void难以测试
{
    var response = await _api.GetPrescriptionsAsync(1, 50);
}
```

---

## 🔗 相关资源

- **架构原则**: [principles.md](../principles.md) - P1-1（MVVM模式严格遵守）
- **Component模式**: [component-pattern.md](./component-pattern.md)
- **Client端架构**: [docs/architecture/client/README.md](../client/README.md)
- **ADR-004**: [Component设计指南](../decisions/ADR-004-component-design-guidelines.md)

---

**最后更新**: 2025-10-25
