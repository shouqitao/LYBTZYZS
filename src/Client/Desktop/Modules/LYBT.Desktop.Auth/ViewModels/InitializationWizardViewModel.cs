using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Auth.Models;
using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Utilities.Security;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 初始化向导 ViewModel（B-07 / US-SHELL-011）。
/// </summary>
/// <remarks>
/// <para><b>五步</b>：1 欢迎 + 模式选择 → 2 模式相关配置（本地：数据库连接；远程：服务器地址 + 连接测试）
/// → 3 诊所信息 → 4 初始管理员账号 → 5 校验清单 + 完成。</para>
/// <para><b>双入口同实现</b>：继承 <see cref="ConnectionTestViewModelBase"/>（→ <c>DialogViewModelBase</c>
/// → <c>NavigableViewModelBase</c> + <c>IDialogAware</c>），因此同一 View/VM 既可经
/// <c>RegisterDialog</c> 以模态向导弹出（首次运行 / 系统管理手动重跑），也可经
/// <c>RegisterForNavigation</c> 作为页面导航进入。</para>
/// <para><b>模式切换时机</b>：第 2 步「下一步」时应用（远程须先探测可用），
/// 保证第 4 步的管理员创建作用于用户所选后端。</para>
/// <para><b>完成标记</b>：仅「完成」写入 <c>IFirstRunStateService</c> 标记；未完成关闭不写标记，
/// 下次登录再次提示（不阻塞进入主界面）。</para>
/// </remarks>
public partial class InitializationWizardViewModel : ConnectionTestViewModelBase
{
    /// <summary>总步数</summary>
    public const int TotalSteps = 5;

    /// <summary>对话框参数键：是否为首次运行触发（首次运行不显示「取消」语义）</summary>
    public const string FirstRunParameterKey = "IsFirstRun";

    private readonly IInitialAdminService _adminService;
    private readonly IClinicSettingsService _clinicSettingsService;
    private readonly ILocalDatabaseSettingsService _localDatabaseSettingsService;
    private readonly IFirstRunStateService _firstRunStateService;

    /// <summary>完成页校验清单</summary>
    public ObservableCollection<WizardChecklistItemModel> ValidationItems { get; } = new();

    // ------------------------------------------------------------------
    // 步骤状态
    // ------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(IsStep3))]
    [NotifyPropertyChangedFor(nameof(IsStep4))]
    [NotifyPropertyChangedFor(nameof(IsStep5))]
    [NotifyPropertyChangedFor(nameof(StepIndicator))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private InitializationWizardStep _currentStep = InitializationWizardStep.Welcome;

    /// <summary>是否为首次运行触发（由 Shell 经对话框参数传入）</summary>
    [ObservableProperty]
    private bool _isFirstRunInvocation;

    /// <summary>是否为对话框宿主（false = 经导航进入的页面；决定「完成」后关闭对话框还是返回首页）</summary>
    private bool _isDialogHosted;

    public bool IsStep1 => CurrentStep == InitializationWizardStep.Welcome;
    public bool IsStep2 => CurrentStep == InitializationWizardStep.Connection;
    public bool IsStep3 => CurrentStep == InitializationWizardStep.Clinic;
    public bool IsStep4 => CurrentStep == InitializationWizardStep.Administrator;
    public bool IsStep5 => CurrentStep == InitializationWizardStep.Finish;
    public bool CanGoBack => CurrentStep > InitializationWizardStep.Welcome;

    /// <summary>步骤指示（如「第 2 / 5 步」）</summary>
    public string StepIndicator => $"第 {(int)CurrentStep} / {TotalSteps} 步";

    /// <summary>当前步骤标题</summary>
    public string StepTitle => CurrentStep switch
    {
        InitializationWizardStep.Welcome => "欢迎使用 · 连接模式",
        InitializationWizardStep.Connection => "连接配置",
        InitializationWizardStep.Clinic => "诊所信息",
        InitializationWizardStep.Administrator => "初始管理员账号",
        _ => "配置校验与完成"
    };

    // ------------------------------------------------------------------
    // Step 1：模式选择
    // ------------------------------------------------------------------

    /// <summary>选择本地全栈模式（LocalWebAPI + LocalDB）</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoteModeSelected))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _isLocalModeSelected = true;

    /// <summary>选择远程服务器模式（远程 WebAPI + SQL Server）</summary>
    public bool IsRemoteModeSelected
    {
        get => !IsLocalModeSelected;
        set => IsLocalModeSelected = !value;
    }

    // ------------------------------------------------------------------
    // Step 2（本地分支）：数据库连接配置
    // ------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocalDbProvider))]
    [NotifyPropertyChangedFor(nameof(IsSqlServerProvider))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private LocalDatabaseProvider _localProvider = LocalDatabaseProvider.LocalDb;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _localServer = LocalDatabaseProfile.DefaultLocalDbServer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _localDatabaseName = LocalDatabaseProfile.DefaultDatabase;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSqlServerProvider))]
    private bool _useWindowsAuthentication = true;

    [ObservableProperty]
    private string _localUserId = string.Empty;

    [ObservableProperty]
    private string _localPassword = string.Empty;

    [ObservableProperty]
    private string _localTestMessage = "尚未测试";

    [ObservableProperty]
    private bool _localTestSucceeded;

    /// <summary>提供程序为 LocalDB</summary>
    public bool IsLocalDbProvider => LocalProvider == LocalDatabaseProvider.LocalDb;

    /// <summary>提供程序为 SQL Server 实例（可写——供 RadioButton 双向绑定）</summary>
    public bool IsSqlServerProvider
    {
        get => LocalProvider == LocalDatabaseProvider.SqlServer;
        set => LocalProvider = value ? LocalDatabaseProvider.SqlServer : LocalDatabaseProvider.LocalDb;
    }

    // ------------------------------------------------------------------
    // Step 2（远程分支）：RemoteUrl / TestStatus / TestStatusMessage 由基类提供
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    // Step 3：诊所信息
    // ------------------------------------------------------------------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private string _clinicName = string.Empty;

    [ObservableProperty]
    private string _clinicDepartment = string.Empty;

    [ObservableProperty]
    private string _clinicAddress = string.Empty;

    [ObservableProperty]
    private string _clinicPhone = string.Empty;

    // ------------------------------------------------------------------
    // Step 4：初始管理员账号
    // ------------------------------------------------------------------

    [ObservableProperty]
    private string _adminUserName = "admin";

    [ObservableProperty]
    private string _adminRealName = string.Empty;

    [ObservableProperty]
    private string _adminPassword = string.Empty;

    [ObservableProperty]
    private string _adminConfirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private bool _isAdminStepSatisfied;

    [ObservableProperty]
    private string _adminMessage = string.Empty;

    // ------------------------------------------------------------------
    // Step 5：完成
    // ------------------------------------------------------------------

    [ObservableProperty]
    private bool _isCompleted;

    public InitializationWizardViewModel(
        IViewModelServices services,
        IConnectionModeService connectionModeService,
        IConnectionSettingsService connectionSettingsService,
        IInitialAdminService adminService,
        IClinicSettingsService clinicSettingsService,
        ILocalDatabaseSettingsService localDatabaseSettingsService,
        IFirstRunStateService firstRunStateService)
        : base(services, connectionModeService, connectionSettingsService)
    {
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
        _clinicSettingsService = clinicSettingsService ?? throw new ArgumentNullException(nameof(clinicSettingsService));
        _localDatabaseSettingsService = localDatabaseSettingsService ?? throw new ArgumentNullException(nameof(localDatabaseSettingsService));
        _firstRunStateService = firstRunStateService ?? throw new ArgumentNullException(nameof(firstRunStateService));

        Title = "初始化向导";
    }

    /// <summary>对话框入口（首次运行 / 系统管理手动重跑）</summary>
    protected override void OnDialogOpenedCore(IDialogParameters? parameters)
    {
        _isDialogHosted = true;
        IsFirstRunInvocation = GetDialogParameter(parameters ?? new DialogParameters(), FirstRunParameterKey, false);
        LoadAsync().SafeFireAndForget(ex => Logger.LogError(ex, "[WIZARD] 初始化向导加载失败"));
    }

    /// <summary>导航入口（系统管理「初始化向导」页面）</summary>
    protected override Task InitializeAsync(NavigationContext navigationContext)
    {
        _isDialogHosted = false;
        IsFirstRunInvocation = false;
        return LoadAsync();
    }

    /// <summary>预填当前配置（模式 / 本地库 / 诊所 / 远程地址）</summary>
    private async Task LoadAsync()
    {
        try
        {
            IsLocalModeSelected = _connectionModeService.CurrentMode == ConnectionMode.Local;
            RemoteUrl = _connectionSettingsService.RemoteUrl;
            TestStatus = ConnectionTestStatus.Idle;
            TestStatusMessage = "尚未测试";

            var profile = _localDatabaseSettingsService.Current;
            LocalProvider = profile.Provider;
            LocalServer = profile.Server;
            LocalDatabaseName = profile.Database;
            UseWindowsAuthentication = profile.UseWindowsAuthentication;
            LocalUserId = profile.UserId ?? string.Empty;
            LocalPassword = profile.Password ?? string.Empty;
            LocalTestMessage = "尚未测试";
            LocalTestSucceeded = false;

            ClinicName = _clinicSettingsService.ClinicName;
            ClinicDepartment = _clinicSettingsService.Department;
            ClinicAddress = _clinicSettingsService.ClinicAddress;
            ClinicPhone = _clinicSettingsService.ClinicPhone;

            AdminMessage = string.Empty;
            IsAdminStepSatisfied = false;

            // 本地库连接自检（信息性——不阻断；用户仍可修改后手动测试）
            if (IsLocalModeSelected)
                await TestLocalDatabaseInternalAsync();

            Logger.LogInformation("[WIZARD] 初始化向导已打开（首次运行={FirstRun}，模式={Mode}）",
                IsFirstRunInvocation, _connectionModeService.CurrentMode);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[WIZARD] 加载当前配置失败");
            SetError("加载当前配置失败，可继续手动填写");
        }
    }

    // ------------------------------------------------------------------
    // 步骤导航
    // ------------------------------------------------------------------

    /// <summary>下一步（进入下一步前落盘当前步骤的配置）</summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextAsync()
    {
        ClearError();
        try
        {
            IsBusy = true;

            if (CurrentStep == InitializationWizardStep.Connection && !await ApplyConnectionStepAsync())
                return;

            if (CurrentStep == InitializationWizardStep.Clinic && !await ApplyClinicStepAsync())
                return;

            if (CurrentStep == InitializationWizardStep.Administrator)
                await LoadValidationItemsAsync();

            CurrentStep = (InitializationWizardStep)((int)CurrentStep + 1);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[WIZARD] 步骤推进失败（{Step}）", CurrentStep);
            SetError($"操作失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>上一步</summary>
    [RelayCommand]
    private void Back()
    {
        ClearError();
        if (CanGoBack)
            CurrentStep = (InitializationWizardStep)((int)CurrentStep - 1);
    }

    /// <summary>完成：写入首次运行标记并关闭</summary>
    [RelayCommand]
    private async Task FinishAsync()
    {
        ClearError();
        try
        {
            IsBusy = true;
            await LoadValidationItemsAsync();

            if (ValidationItems.Any(item => !item.IsPassed))
            {
                SetError("仍有未完成的校验项，请返回对应步骤处理");
                return;
            }

            _firstRunStateService.MarkCompleted();
            IsCompleted = true;
            Logger.LogInformation("[WIZARD] 初始化向导已完成（模式={Mode}，诊所={Clinic}）",
                _connectionModeService.CurrentMode, ClinicName);

            if (_isDialogHosted)
                CloseDialog(ButtonResult.OK);
            else
                NavigateToHome();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[WIZARD] 完成向导失败");
            SetError($"完成失败：{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>稍后配置 / 取消：不写标记，下次登录再次提示</summary>
    [RelayCommand]
    private void CancelWizard()
    {
        Logger.LogInformation("[WIZARD] 用户暂缓初始化向导（未写完成标记）");

        if (_isDialogHosted)
            CloseDialog(ButtonResult.Cancel);
        else
            NavigateToHome();
    }

    private bool CanGoNext() => CurrentStep switch
    {
        InitializationWizardStep.Welcome => true,
        InitializationWizardStep.Connection => IsLocalModeSelected
            ? IsLocalConnectionInputValid()
            : TestStatus == ConnectionTestStatus.Success,
        InitializationWizardStep.Clinic => !string.IsNullOrWhiteSpace(ClinicName),
        InitializationWizardStep.Administrator => IsAdminStepSatisfied,
        _ => false
    };

    private bool IsLocalConnectionInputValid() =>
        !string.IsNullOrWhiteSpace(LocalServer) && !string.IsNullOrWhiteSpace(LocalDatabaseName);

    // ------------------------------------------------------------------
    // Step 2：连接配置
    // ------------------------------------------------------------------

    /// <summary>测试本地数据库连接</summary>
    [RelayCommand]
    private Task TestLocalDatabaseAsync() => TestLocalDatabaseInternalAsync();

    private async Task TestLocalDatabaseInternalAsync()
    {
        try
        {
            IsBusy = true;
            LocalTestMessage = "正在测试连接...";
            LocalTestSucceeded = false;

            var result = await _localDatabaseSettingsService.TestAsync(BuildLocalProfile());
            LocalTestSucceeded = result.Success;
            LocalTestMessage = result.Success
                ? $"✓ 可用（{LocalServer} / {LocalDatabaseName}）"
                : $"✗ 不可用：{result.Error}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[WIZARD] 本地数据库连接测试异常");
            LocalTestSucceeded = false;
            LocalTestMessage = $"✗ 不可用：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>落盘第 2 步配置并应用所选模式</summary>
    private async Task<bool> ApplyConnectionStepAsync()
    {
        if (IsLocalModeSelected)
        {
            var save = await _localDatabaseSettingsService.SaveAsync(BuildLocalProfile());
            if (!save.Success)
            {
                SetError(save.Error ?? "保存本地数据库配置失败");
                return false;
            }

            var switchResult = await _connectionModeService.SetModeAsync(ConnectionMode.Local);
            if (!switchResult.Succeeded)
            {
                SetError(switchResult.Message ?? "切换到本地模式失败");
                return false;
            }

            Logger.LogInformation("[WIZARD] 已应用本地模式（{Server}/{Database}）", LocalServer, LocalDatabaseName);
            return true;
        }

        if (!_connectionSettingsService.IsValidUrl(RemoteUrl))
        {
            SetError("服务器地址格式无效（需以 http:// 或 https:// 开头）");
            return false;
        }

        await _connectionSettingsService.SetUrlAsync(RemoteUrl);

        // B2 (US-SHELL-007)：切换远程前必须探测可用性
        var available = await _connectionModeService.CheckRemoteAvailableAsync();
        if (!available)
        {
            SetError("远程服务器不可达，请检查地址或网络后重试");
            return false;
        }

        var result = await _connectionModeService.SetModeAsync(ConnectionMode.Remote);
        if (!result.Succeeded)
        {
            SetError(result.Message ?? "切换到远程模式失败");
            return false;
        }

        Logger.LogInformation("[WIZARD] 已应用远程模式（{Url}）", RemoteUrl);
        return true;
    }

    private LocalDatabaseProfile BuildLocalProfile() => new()
    {
        Provider = LocalProvider,
        Server = LocalServer?.Trim() ?? string.Empty,
        Database = LocalDatabaseName?.Trim() ?? string.Empty,
        UseWindowsAuthentication = UseWindowsAuthentication,
        UserId = string.IsNullOrWhiteSpace(LocalUserId) ? null : LocalUserId.Trim(),
        Password = string.IsNullOrWhiteSpace(LocalPassword) ? null : LocalPassword
    };

    // ------------------------------------------------------------------
    // Step 3：诊所信息
    // ------------------------------------------------------------------

    /// <summary>保存诊所信息（写入 clinic-settings.json，驱动登录页/打印标题）</summary>
    private async Task<bool> ApplyClinicStepAsync()
    {
        if (string.IsNullOrWhiteSpace(ClinicName))
        {
            SetError("诊所名称为必填项");
            return false;
        }

        var options = new ClinicSettingsOptions
        {
            Name = ClinicName.Trim(),
            Department = ClinicDepartment?.Trim() ?? string.Empty,
            Address = ClinicAddress?.Trim() ?? string.Empty,
            Phone = ClinicPhone?.Trim() ?? string.Empty
        };

        var saved = await _clinicSettingsService.SaveSettingsAsync(options);
        if (!saved)
        {
            SetError("保存诊所信息失败，请检查配置目录写入权限");
            return false;
        }

        Logger.LogInformation("[WIZARD] 诊所信息已保存：{Name}", options.Name);
        return true;
    }

    // ------------------------------------------------------------------
    // Step 4：初始管理员账号
    // ------------------------------------------------------------------

    /// <summary>创建初始管理员账号（角色 Admin）</summary>
    [RelayCommand]
    private async Task CreateAdminAsync()
    {
        ClearError();
        AdminMessage = string.Empty;

        var validationError = ValidateAdminInput();
        if (validationError != null)
        {
            AdminMessage = validationError;
            return;
        }

        try
        {
            IsBusy = true;

            var exists = await _adminService.ExistsAsync(AdminUserName.Trim());
            if (exists.Success && exists.Data)
            {
                IsAdminStepSatisfied = true;
                AdminMessage = $"账号 {AdminUserName.Trim()} 已存在，可直接进入下一步";
                return;
            }

            var result = await _adminService.CreateAdminAsync(
                AdminUserName.Trim(),
                AdminRealName.Trim(),
                AdminPassword);

            if (result.Success)
            {
                IsAdminStepSatisfied = true;
                AdminMessage = $"✓ 管理员账号 {AdminUserName.Trim()} 已创建";
                Logger.LogInformation("[WIZARD] 初始管理员账号已创建：{UserName}", AdminUserName.Trim());
            }
            else
            {
                IsAdminStepSatisfied = false;
                AdminMessage = $"✗ 创建失败：{result.Error}";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[WIZARD] 创建管理员账号异常");
            IsAdminStepSatisfied = false;
            AdminMessage = $"✗ 创建失败：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>账号输入校验（用户名/姓名/密码策略——策略 SSOT 为 PasswordPolicyValidator）</summary>
    private string? ValidateAdminInput()
    {
        var userName = AdminUserName?.Trim() ?? string.Empty;
        if (userName.Length is < 3 or > 32)
            return "用户名长度必须在 3-32 个字符之间";

        if (!System.Text.RegularExpressions.Regex.IsMatch(userName, "^[a-zA-Z0-9_]+$"))
            return "用户名只能包含字母、数字和下划线";

        if (string.IsNullOrWhiteSpace(AdminRealName))
            return "请输入管理员姓名";

        if (!string.Equals(AdminPassword, AdminConfirmPassword, StringComparison.Ordinal))
            return "两次输入的密码不一致";

        return PasswordPolicyValidator.Validate(AdminPassword, out var errors)
            ? null
            : string.Join("；", errors);
    }

    // ------------------------------------------------------------------
    // Step 5：校验清单
    // ------------------------------------------------------------------

    /// <summary>生成完成页校验清单</summary>
    private async Task LoadValidationItemsAsync()
    {
        var items = new List<WizardChecklistItemModel>();

        items.Add(new WizardChecklistItemModel
        {
            Title = "连接模式",
            IsPassed = _connectionModeService.CurrentMode == (IsLocalModeSelected ? ConnectionMode.Local : ConnectionMode.Remote),
            Detail = _connectionModeService.CurrentModeDisplay
        });

        if (IsLocalModeSelected)
        {
            items.Add(new WizardChecklistItemModel
            {
                Title = "本地数据库连接",
                IsPassed = LocalTestSucceeded || IsLocalConnectionInputValid(),
                Detail = LocalTestMessage
            });
        }
        else
        {
            items.Add(new WizardChecklistItemModel
            {
                Title = "远程服务器连接",
                IsPassed = TestStatus == ConnectionTestStatus.Success,
                Detail = string.IsNullOrWhiteSpace(RemoteUrl) ? TestStatusMessage : $"{RemoteUrl} · {TestStatusMessage}"
            });
        }

        items.Add(new WizardChecklistItemModel
        {
            Title = "诊所信息",
            IsPassed = !string.IsNullOrWhiteSpace(ClinicName),
            Detail = string.IsNullOrWhiteSpace(ClinicName) ? "未填写诊所名称" : ClinicName
        });

        var adminExists = false;
        try
        {
            var check = await _adminService.ExistsAsync(string.IsNullOrWhiteSpace(AdminUserName) ? "admin" : AdminUserName.Trim());
            adminExists = check.Success && check.Data;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "[WIZARD] 管理员账号存在性检查失败（不阻断完成）");
        }

        items.Add(new WizardChecklistItemModel
        {
            Title = "管理员账号",
            IsPassed = IsAdminStepSatisfied || adminExists,
            Detail = IsAdminStepSatisfied || adminExists
                ? $"{(string.IsNullOrWhiteSpace(AdminUserName) ? "admin" : AdminUserName.Trim())} 可用"
                : "尚未创建管理员账号"
        });

        ValidationItems.Clear();
        foreach (var item in items)
            ValidationItems.Add(item);

        NextCommand.NotifyCanExecuteChanged();
    }
}
