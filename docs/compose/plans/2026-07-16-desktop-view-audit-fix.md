# Desktop View Audit Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 2 broken features (batch delete, pending queue), add 2 missing views (deployment, audit log), add batch enable/disable UI, and add reports date picker.

**Architecture:** All changes are in the WPF Desktop client (`src/Client/Desktop/`). Bug fixes modify existing base classes. New views follow the established NavigableViewModelBase pattern with Prism navigation registration.

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, MaterialDesignThemes

## Global Constraints

- Target framework: `net8.0-windows`
- MVVM: CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` for new code, Prism `DelegateCommand` where already used
- Navigation: Prism `RegisterForNavigation` + `INavigationCoordinator.NavigateTo()`
- API calls: Use injected service interfaces (`IRegistrationService`, `IMedicalCaseService`, etc.)
- No cross-module direct references — use shared contracts from `LYBT.Desktop.Contracts`
- ViewNames constants in `LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`
- Build verification: `dotnet build LYBTZYZS.sln` after each task

---

### Task 1: Fix Batch Delete in MasterDetailViewModelBase

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

**Interfaces:**
- Consumes: `SelectedItem`, `SelectedItems` (from DataGrid), `_masterDetailServices.Dialog.ShowConfirmAsync()`
- Produces: Modified `DeleteAsync()` that handles both single and batch deletion

- [ ] **Step 1: Read the current DeleteAsync implementation**

Read `MasterDetailViewModelBase.cs` lines 497-517 to understand the current single-item delete logic.

- [ ] **Step 2: Modify DeleteAsync to support batch deletion**

Replace the `DeleteAsync()` method (lines 500-517) with batch-aware logic:

```csharp
[RelayCommand(CanExecute = nameof(CanDelete))]
protected virtual async Task DeleteAsync()
{
    var itemsToDelete = GetSelectedItemsForDelete();
    if (itemsToDelete.Count == 0) return;

    var message = itemsToDelete.Count == 1
        ? "确定要删除选中的记录吗？"
        : $"确定要删除选中的 {itemsToDelete.Count} 条记录吗？";

    var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync("确认删除", message);
    if (!confirmed) return;

    if (itemsToDelete.Count == 1)
    {
        var success = await DeleteItemAsync(itemsToDelete[0]);
        if (success)
        {
            await RefreshAsync();
            await OnItemDeletedAsync(itemsToDelete[0]);
        }
    }
    else
    {
        await DeleteBatchAsync(itemsToDelete);
        await RefreshAsync();
    }
}
```

- [ ] **Step 3: Add helper methods for batch selection and batch delete**

Add these methods after `DeleteAsync()`:

```csharp
/// <summary>
/// Get items selected via checkbox for batch operations.
/// Falls back to single SelectedItem if no checkboxes are checked.
/// </summary>
protected virtual List<TListDto> GetSelectedItemsForDelete()
{
    // Prefer checkbox selection (batch), fall back to single selection
    if (SelectedItems != null && SelectedItems.Count > 0)
        return new List<TListDto>(SelectedItems);

    if (SelectedItem != null)
        return new List<TListDto> { SelectedItem };

    return new List<TListDto>();
}

/// <summary>
/// Delete multiple items. Override in subclass for batch API call.
/// Default implementation deletes one by one.
/// </summary>
protected virtual async Task DeleteBatchAsync(List<TListDto> items)
{
    foreach (var item in items)
    {
        var success = await DeleteItemAsync(item);
        if (success)
            await OnItemDeletedAsync(item);
    }
}
```

- [ ] **Step 4: Verify SelectedItems property exists**

Check that `MasterDetailViewModelBase` has a `SelectedItems` property of type `ObservableCollection<TListDto>` or `IList<TListDto>`. If not, add one. The DataGrid's `SelectedItems` binding needs to be connected.

- [ ] **Step 5: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs
git commit -m "fix(desktop): batch delete support in MasterDetailViewModelBase"
```

---

### Task 2: Fix Clinical PendingQueue

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`

**Interfaces:**
- Consumes: `IRegistrationService.GetQueueAsync(doctorId)`, `ISessionManager.CurrentUserId`
- Produces: Working `RefreshQueueAsync()` that loads real data

- [ ] **Step 1: Add IRegistrationService and ISessionManager dependencies**

Modify the constructor to accept `IRegistrationService`:

```csharp
public PendingQueueViewModel(
    IMedicalCaseWorkspaceContext context,
    IWorkspaceHost host,
    ILoggerFactory loggerFactory,
    IMedicalCaseService medicalCaseService,
    IRegistrationService registrationService,
    INavigationCoordinator navigationCoordinator)
    : base(host, loggerFactory)
{
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
    _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
    _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));

    RefreshCommand = new DelegateCommand(async () => await RefreshQueueAsync());
    SelectCommand = new DelegateCommand<PendingMedicalCaseDto>(async c => await SelectPendingCaseAsync(c));
}
```

Add field: `private readonly IRegistrationService _registrationService;`

Add using: `using LYBT.Desktop.Contracts.Services;`

- [ ] **Step 2: Implement real RefreshQueueAsync**

Replace the no-op `RefreshQueueAsync()` with:

```csharp
public async Task RefreshQueueAsync()
{
    try
    {
        IsRefreshing = true;
        _queue.Clear();

        var doctorId = _context.CurrentDoctorId;
        var result = await _registrationService.GetQueueAsync(doctorId);

        if (result.Success && result.Data != null)
        {
            // Map RegistrationListDto to PendingMedicalCaseDto
            foreach (var reg in result.Data)
            {
                _queue.Add(new PendingMedicalCaseDto
                {
                    MedicalCaseId = reg.MedicalCaseId,
                    PatientId = reg.PatientId,
                    PatientName = reg.PatientName,
                    CaseStatus = reg.Status, // May need mapping
                    RegistrationTime = reg.CreatedAt
                });
            }
        }

        Logger.LogInformation("待诊队列加载完成，共{Count}条", _queue.Count);
        OnPropertyChanged(nameof(Queue));
        OnPropertyChanged(nameof(HasNoPendingCases));
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "加载待诊队列失败");
    }
    finally
    {
        IsRefreshing = false;
    }
}
```

Note: Check `RegistrationListDto` fields vs `PendingMedicalCaseDto` fields — adjust mapping accordingly. The `MedicalCaseId` field on `RegistrationListDto` may need to be verified.

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs
git commit -m "fix(desktop): wire PendingQueueViewModel to real registration queue API"
```

---

### Task 3: Add Batch Enable/Disable UI

**Covers:** [S6]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

**Interfaces:**
- Consumes: `BatchEnableCommand`, `BatchDisableCommand` from ViewModel
- Produces: Two new buttons in DataGridToolbar, batch enable/disable methods in base ViewModel

- [ ] **Step 1: Add BatchEnableCommand and BatchDisableCommand dependency properties to DataGridToolbar**

In `DataGridToolbar.xaml.cs`, add:

```csharp
public static readonly DependencyProperty BatchEnableCommandProperty =
    DependencyProperty.Register(nameof(BatchEnableCommand), typeof(ICommand), typeof(DataGridToolbar), new PropertyMetadata(null));

public ICommand BatchEnableCommand
{
    get => (ICommand)GetValue(BatchEnableCommandProperty);
    set => SetValue(BatchEnableCommandProperty, value);
}

public static readonly DependencyProperty BatchDisableCommandProperty =
    DependencyProperty.Register(nameof(BatchDisableCommand), typeof(ICommand), typeof(DataGridToolbar), new PropertyMetadata(null));

public ICommand BatchDisableCommand
{
    get => (ICommand)GetValue(BatchDisableCommandProperty);
    set => SetValue(BatchDisableCommandProperty, value);
}
```

- [ ] **Step 2: Add batch enable/disable buttons to DataGridToolbar XAML**

In `DataGridToolbar.xaml`, add icons to the ResourceDictionary:

```xml
<Geometry x:Key="EnableIcon">M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z</Geometry>
<Geometry x:Key="DisableIcon">M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z</Geometry>
```

Add buttons in the right-side StackPanel (after the batch delete button):

```xml
<!-- 批量启用按钮 -->
<Button Command="{Binding BatchEnableCommand, ElementName=Root}"
        Style="{StaticResource MaterialDesignOutlinedButton}"
        Visibility="{Binding BatchEnableCommand, ElementName=Root, Converter={x:Static converters:Cvt.NullToVis}}"
        ToolTip="批量启用"
        Margin="8,0,0,0">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource EnableIcon}" Fill="{DynamicResource MaterialDesign.Brush.Foreground}"
              Width="16" Height="16" Stretch="Uniform" VerticalAlignment="Center"/>
        <TextBlock Text="批量启用" Margin="6,0,0,0" VerticalAlignment="Center"/>
    </StackPanel>
</Button>

<!-- 批量禁用按钮 -->
<Button Command="{Binding BatchDisableCommand, ElementName=Root}"
        Style="{StaticResource MaterialDesignOutlinedButton}"
        Visibility="{Binding BatchDisableCommand, ElementName=Root, Converter={x:Static converters:Cvt.NullToVis}}"
        ToolTip="批量禁用"
        Margin="8,0,0,0">
    <StackPanel Orientation="Horizontal">
        <Path Data="{StaticResource DisableIcon}" Fill="{DynamicResource MaterialDesign.Brush.Foreground}"
              Width="16" Height="16" Stretch="Uniform" VerticalAlignment="Center"/>
        <TextBlock Text="批量禁用" Margin="6,0,0,0" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

- [ ] **Step 3: Add batch enable/disable commands to MasterDetailViewModelBase**

In `MasterDetailViewModelBase.cs`, add after the batch delete methods:

```csharp
[RelayCommand(CanExecute = nameof(HasSelection))]
protected virtual async Task BatchEnableAsync()
{
    var items = GetSelectedItemsForDelete();
    if (items.Count == 0) return;

    var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
        "确认启用", $"确定要启用选中的 {items.Count} 条记录吗？");
    if (!confirmed) return;

    await EnableBatchAsync(items);
    await RefreshAsync();
}

[RelayCommand(CanExecute = nameof(HasSelection))]
protected virtual async Task BatchDisableAsync()
{
    var items = GetSelectedItemsForDelete();
    if (items.Count == 0) return;

    var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
        "确认禁用", $"确定要禁用选中的 {items.Count} 条记录吗？");
    if (!confirmed) return;

    await DisableBatchAsync(items);
    await RefreshAsync();
}

protected virtual async Task EnableBatchAsync(List<TListDto> items)
{
    foreach (var item in items)
        await SetItemEnabledAsync(item, true);
}

protected virtual async Task DisableBatchAsync(List<TListDto> items)
{
    foreach (var item in items)
        await SetItemEnabledAsync(item, false);
}

protected virtual Task SetItemEnabledAsync(TListDto item, bool enabled)
{
    // Override in subclass to call service API
    return Task.CompletedTask;
}
```

- [ ] **Step 4: Wire DataGridToolbar buttons in MasterDetailControl**

In each of the 5 MasterDetail XAML files, bind the new commands on the DataGridToolbar:

```xml
<controls:DataGridToolbar
    ...
    BatchEnableCommand="{Binding BatchEnableCommand}"
    BatchDisableCommand="{Binding BatchDisableCommand}"/>
```

Files to update:
- `LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`
- `LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
- `LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml`
- `LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml`
- `LYBT.Desktop.MedicalCase/Controls/MasterDetailControl.xaml`

- [ ] **Step 5: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml \
        src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml.cs \
        src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs
git commit -m "feat(desktop): add batch enable/disable UI to DataGridToolbar and base ViewModel"
```

---

### Task 4: Create Deployment View

**Covers:** [S3]

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml.cs`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/DeploymentViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/SysadminModule.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`

**Interfaces:**
- Consumes: `IDiagnosticsApi` pattern (reference `LogLevelControlViewModel` for style)
- Produces: `DeploymentView` navigation target, `ViewNames.Deployment` constant

- [ ] **Step 1: Add ViewNames.Deployment constant**

In `ViewNames.cs`, add in the `#region 诊断视图` section:

```csharp
/// <summary>部署管理</summary>
public const string Deployment = "DeploymentView";
```

- [ ] **Step 2: Create DeploymentViewModel**

Create `ViewModels/DeploymentViewModel.cs` following `LogLevelControlViewModel` pattern:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;

namespace LYBT.Desktop.Sysadmin.ViewModels;

public partial class DeploymentViewModel : NavigableViewModelBase
{
    private readonly IDeployApi _deployApi;

    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private bool _isRestarting;
    [ObservableProperty] private double _uploadProgress;
    [ObservableProperty] private string? _selectedFileName;

    public DeploymentViewModel(IViewModelServices services, IDeployApi deployApi)
        : base(services)
    {
        _deployApi = deployApi;
        PageTitle = "部署管理";
    }

    [RelayCommand]
    private void SelectFile()
    {
        var dialog = new OpenFileDialog { Filter = "ZIP 文件|*.zip", Title = "选择更新包" };
        if (dialog.ShowDialog() == true)
        {
            SelectedFileName = dialog.FileName;
            StatusMessage = $"已选择: {Path.GetFileName(dialog.FileName)}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanUpload))]
    private async Task UploadAsync()
    {
        if (string.IsNullOrEmpty(SelectedFileName)) return;

        try
        {
            IsUploading = true;
            StatusMessage = "正在上传...";
            UploadProgress = 0;

            using var stream = File.OpenRead(SelectedFileName);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(stream), "file", Path.GetFileName(SelectedFileName));

            var response = await _deployApi.UploadAsync(content);
            if (response.IsSuccess)
            {
                StatusMessage = "上传成功！";
                UploadProgress = 100;
            }
            else
            {
                StatusMessage = $"上传失败: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "上传更新包失败");
            StatusMessage = $"上传失败: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private bool CanUpload => !IsUploading && !IsRestarting && !string.IsNullOrEmpty(SelectedFileName);

    [RelayCommand(CanExecute = nameof(CanControl))]
    private async Task RestartAsync()
    {
        try
        {
            IsRestarting = true;
            StatusMessage = "正在重启服务...";
            var response = await _deployApi.RestartAsync();
            StatusMessage = response.IsSuccess ? "重启指令已发送" : $"重启失败: {response.ErrorMessage}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "重启服务失败");
            StatusMessage = $"重启失败: {ex.Message}";
        }
        finally
        {
            IsRestarting = false;
        }
    }

    private bool CanControl => !IsUploading && !IsRestarting;
}
```

Note: `IDeployApi` needs to be created in `LYBT.Desktop.Contracts/Api/` if it doesn't exist. Check first — if missing, add a minimal Refit interface:

```csharp
// In LYBT.Desktop.Contracts/Api/IDeployApi.cs
public interface IDeployApi
{
    [Post("/api/v1/deploy/upload")]
    Task<ApiResponse<object>> UploadAsync(MultipartFormDataContent content);

    [Post("/api/v1/deploy/restart")]
    Task<ApiResponse<object>> RestartAsync();
}
```

- [ ] **Step 3: Create DeploymentView XAML**

Create `Views/DeploymentView.xaml` following LogLevelControlView style:

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.DeploymentView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <ScrollViewer VerticalScrollBarVisibility="Auto" Padding="24">
        <StackPanel MaxWidth="600" HorizontalAlignment="Center" Spacing="24">
            <!-- Header -->
            <TextBlock Text="{Binding PageTitle}" Style="{StaticResource MaterialDesignTitleTextBlock}"/>

            <!-- Upload Card -->
            <md:Card Padding="20">
                <StackPanel Spacing="16">
                    <TextBlock Text="上传更新包" FontWeight="SemiBold" FontSize="16"/>
                    <TextBlock Text="选择 .zip 格式的更新包文件" Foreground="{DynamicResource MaterialDesign.Brush.SecondaryForeground}"/>

                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="选择文件..." Command="{Binding SelectFileCommand}"
                                Style="{StaticResource MaterialDesignOutlinedButton}"/>
                        <TextBlock Text="{Binding SelectedFileName}" VerticalAlignment="Center"
                                   TextTrimming="CharacterEllipsis" MaxWidth="300"/>
                    </StackPanel>

                    <Button Content="上传并部署" Command="{Binding UploadCommand}"
                            Style="{StaticResource MaterialDesignRaisedButton}"
                            md:Assist.CornerRadius="4" Padding="16,8"/>

                    <ProgressBar Value="{Binding UploadProgress}" Maximum="100"
                                 Visibility="{Binding IsUploading, Converter={StaticResource BoolToVis}}"/>
                </StackPanel>
            </md:Card>

            <!-- Server Control Card -->
            <md:Card Padding="20">
                <StackPanel Spacing="16">
                    <TextBlock Text="服务控制" FontWeight="SemiBold" FontSize="16"/>
                    <Button Content="重启服务" Command="{Binding RestartCommand}"
                            Style="{StaticResource MaterialDesignRaisedButton}"
                            md:Assist.CornerRadius="4" Padding="16,8"
                            Foreground="White" Background="{DynamicResource MaterialDesignValidationErrorBrush}"/>
                </StackPanel>
            </md:Card>

            <!-- Status -->
            <TextBlock Text="{Binding StatusMessage}" TextWrapping="Wrap"
                       Foreground="{DynamicResource MaterialDesign.Brush.SecondaryForeground}"/>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 4: Create DeploymentView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Sysadmin.Views;

public partial class DeploymentView : UserControl
{
    public DeploymentView() => InitializeComponent();
}
```

- [ ] **Step 5: Register in SysadminModule**

In `SysadminModule.cs`, add:

```csharp
containerRegistry.Register<ViewModels.DeploymentViewModel>();
containerRegistry.RegisterForNavigation<Views.DeploymentView>();
```

- [ ] **Step 6: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 7: Commit**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml \
        src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml.cs \
        src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/DeploymentViewModel.cs \
        src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/SysadminModule.cs \
        src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs
git commit -m "feat(desktop): add deployment management view for sysadmin"
```

---

### Task 5: Create Audit Log View

**Covers:** [S4]

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/AuditLogViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs`

**Interfaces:**
- Consumes: `IMedicalCaseService.GetAuditLogsAsync(medicalCaseId, page, pageSize)`
- Produces: `AuditLogView` navigation target

- [ ] **Step 1: Add ViewNames.AuditLog constant**

In `ViewNames.cs`, add in the `#region MasterDetail视图` section:

```csharp
/// <summary>医案审计日志</summary>
public const string AuditLog = "AuditLogView";
```

- [ ] **Step 2: Create AuditLogViewModel**

Create `ViewModels/AuditLogViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels;

public partial class AuditLogViewModel : NavigableViewModelBase
{
    private readonly IMedicalCaseService _medicalCaseService;

    private Guid _medicalCaseId;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _logs = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private int _totalCount;

    private const int PageSize = 20;

    public AuditLogViewModel(IViewModelServices services, IMedicalCaseService medicalCaseService)
        : base(services)
    {
        _medicalCaseService = medicalCaseService;
        PageTitle = "审计日志";
    }

    public override void OnNavigatedTo(NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        if (navigationContext.Parameters.TryGetValue("MedicalCaseId", out Guid id))
        {
            _medicalCaseId = id;
            _ = LoadLogsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        try
        {
            IsLoading = true;
            Logs.Clear();

            var result = await _medicalCaseService.GetAuditLogsAsync(_medicalCaseId, CurrentPage, PageSize);
            if (result.Success && result.Data != null)
            {
                foreach (var log in result.Data.Items)
                    Logs.Add(log);
                TotalCount = result.Data.TotalCount;
                TotalPages = result.Data.TotalPages;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "加载审计日志失败");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPageAsync()
    {
        CurrentPage--;
        await LoadLogsAsync();
    }

    private bool CanGoPrevious => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync()
    {
        CurrentPage++;
        await LoadLogsAsync();
    }

    private bool CanGoNext => CurrentPage < TotalPages;

    [RelayCommand]
    private void GoBack() => Navigation.GoBack();
}
```

Note: Verify `IMedicalCaseService` has `GetAuditLogsAsync` method. If not, check the API client and add the method.

- [ ] **Step 3: Create AuditLogView XAML**

Create `Views/AuditLogView.xaml`:

```xml
<UserControl x:Class="LYBT.Desktop.MedicalCase.Views.AuditLogView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"
             xmlns:prism="http://prismlibrary.com/"
             xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid Padding="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,12">
            <Button Command="{Binding GoBackCommand}" Style="{StaticResource MaterialDesignIconButton}"
                    ToolTip="返回" Margin="0,0,8,0">
                <md:PackIcon Kind="ArrowLeft"/>
            </Button>
            <TextBlock Text="{Binding PageTitle}" Style="{StaticResource MaterialDesignTitleTextBlock}"
                       VerticalAlignment="Center"/>
        </StackPanel>

        <!-- Log List -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding Logs}" AutoGenerateColumns="False"
                  IsReadOnly="True" GridLinesVisibility="None"
                  HeadersVisibility="Column" CanUserAddRows="False">
            <DataGrid.Columns>
                <DataGridTextColumn Header="时间" Binding="{Binding CreatedAt, StringFormat='{}{0:yyyy-MM-dd HH:mm:ss}'}" Width="180"/>
                <DataGridTextColumn Header="操作人" Binding="{Binding OperatorName}" Width="120"/>
                <DataGridTextColumn Header="操作类型" Binding="{Binding OperationType}" Width="120"/>
                <DataGridTextColumn Header="详情" Binding="{Binding Description}" Width="*"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Loading -->
        <TextBlock Grid.Row="1" Text="加载中..." HorizontalAlignment="Center" VerticalAlignment="Center"
                   Visibility="{Binding IsLoading, Converter={StaticResource BoolToVis}}"/>

        <!-- Pagination -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,12,0,0">
            <Button Content="上一页" Command="{Binding PreviousPageCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}" Margin="0,0,8,0"/>
            <TextBlock VerticalAlignment="Center" Margin="0,0,8,0">
                <Run Text="第"/><Run Text="{Binding CurrentPage, Mode=OneWay}"/><Run Text="页"/>
                <Run Text="/"/><Run Text="{Binding TotalPages, Mode=OneWay}"/><Run Text="页"/>
                <Run Text="(共"/><Run Text="{Binding TotalCount, Mode=OneWay}"/><Run Text="条)"/>
            </TextBlock>
            <Button Content="下一页" Command="{Binding NextPageCommand}"
                    Style="{StaticResource MaterialDesignOutlinedButton}"/>
        </StackPanel>
    </Grid>
</UserControl>
```

- [ ] **Step 4: Create AuditLogView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Views;

public partial class AuditLogView : UserControl
{
    public AuditLogView() => InitializeComponent();
}
```

- [ ] **Step 5: Register in MedicalCaseModule**

In `MedicalCaseModule.cs`, add:

```csharp
containerRegistry.Register<ViewModels.AuditLogViewModel>();
containerRegistry.RegisterForNavigation<Views.AuditLogView>();
```

- [ ] **Step 6: Add "审计日志" button to MedicalCaseCommandsViewModel**

In `ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`, add a command:

```csharp
[RelayCommand]
private void ViewAuditLogs()
{
    var parameters = new Dictionary<string, object> { { "MedicalCaseId", _context.MedicalCaseId } };
    _navigationCoordinator.NavigateTo(ViewNames.AuditLog, parameters);
}
```

And add the button in the corresponding XAML toolbar.

- [ ] **Step 7: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 8: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml \
        src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml.cs \
        src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/AuditLogViewModel.cs \
        src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs \
        src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs
git commit -m "feat(desktop): add medical case audit log view"
```

---

### Task 6: Add Reports Date Picker

**Covers:** [S8]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Reports/Views/ReportsHomeView.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Reports/ViewModels/ReportsHomeViewModel.cs`

**Interfaces:**
- Consumes: Existing `ReportsHomeViewModel` API calls
- Produces: Date-filtered report data

- [ ] **Step 1: Add SelectedDate property to ReportsHomeViewModel**

In `ReportsHomeViewModel.cs`, add:

```csharp
[ObservableProperty] private DateTime _selectedDate = DateTime.Today;

partial void OnSelectedDateChanged(DateTime value)
{
    _ = LoadDataAsync();
}
```

Modify `LoadDataAsync()` to pass the date to API calls (if API supports it). If API doesn't support date parameter, this task is deferred.

- [ ] **Step 2: Add DatePicker to ReportsHomeView XAML**

In `ReportsHomeView.xaml`, add above the 3 data cards:

```xml
<!-- Date Picker -->
<StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,0,0,16">
    <TextBlock Text="日期：" VerticalAlignment="Center" Margin="0,0,8,0"/>
    <DatePicker SelectedDate="{Binding SelectedDate}" Width="200"/>
    <Button Content="今天" Command="{Binding GoToTodayCommand}" Margin="8,0,0,0"
            Style="{StaticResource MaterialDesignOutlinedButton}"/>
</StackPanel>
```

Add `GoToTodayCommand` to ViewModel:

```csharp
[RelayCommand]
private void GoToToday() => SelectedDate = DateTime.Today;
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Reports/Views/ReportsHomeView.xaml \
        src/Client/Desktop/Modules/LYBT.Desktop.Reports/ViewModels/ReportsHomeViewModel.cs
git commit -m "feat(desktop): add date picker to reports view"
```
