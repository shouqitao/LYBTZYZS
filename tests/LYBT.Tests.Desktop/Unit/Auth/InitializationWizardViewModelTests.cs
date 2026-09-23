using System.Threading;
using LYBT.Desktop.Auth.Models;
using LYBT.Desktop.Auth.Services;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts.Enums;
using LYBT.Desktop.Contracts.Models;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Tests.Desktop.Infrastructure;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// B-07 初始化向导 ViewModel 单测（US-SHELL-011）。
/// </summary>
/// <remarks>
/// <para><b>如何驱动 <c>LoadAsync</c></b>：<c>LoadAsync</c> 为 private，经公开的 IDialogAware 成员
/// <see cref="InitializationWizardViewModel.OnDialogOpened"/>（对话框入口）触发；本套测试统一用该入口
/// 预置配置，而不是走 <c>OnNavigatedTo</c>（后者把 <c>InitializeAsync</c> 投递到 UI 调度器，多一层间接）。
/// 所有替身的异步成员都以<b>已完成</b>的 <c>Task</c> 返回，因此 <c>LoadAsync</c> 在 <c>OnDialogOpened</c>
/// 返回前同步跑完，断言无需等待、无 fire-and-forget 竞态。</para>
/// <para><b>断言口径</b>：全部为可观察状态（步骤/门禁/清单/错误消息）、命令可执行性与服务调用及入参，
/// 不涉及源码文本或 mock 回显。</para>
/// <para><b>注意</b>：<see cref="InitializationWizardViewModel"/> 冻结，本套测试不修改生产代码。</para>
/// </remarks>
[Trait("US", "US-SHELL-011")]
public class InitializationWizardViewModelTests : DesktopTestBase
{
    /// <summary>满足 PasswordPolicyValidator 策略的可用口令（长度/大小写/数字/特殊字符/无重复无序列）</summary>
    private const string ValidPassword = "Lybt@2024";

    private const string SavedRemoteUrl = "http://10.20.30.40:5000";

    private readonly IConnectionModeService _connectionMode = Substitute.For<IConnectionModeService>();
    private readonly IConnectionSettingsService _connectionSettings = Substitute.For<IConnectionSettingsService>();
    private readonly IInitialAdminService _adminService = Substitute.For<IInitialAdminService>();
    private readonly IClinicSettingsService _clinicSettings = Substitute.For<IClinicSettingsService>();
    private readonly ILocalDatabaseSettingsService _localDatabase = Substitute.For<ILocalDatabaseSettingsService>();
    private readonly IFirstRunStateService _firstRunState = Substitute.For<IFirstRunStateService>();

    public InitializationWizardViewModelTests()
    {
        // IViewModelServices 本身也是替身：基类构造会读取 LoggerFactory / UiThreadDispatcher / EventAggregator 等。
        // LoggerFactory（替身工厂，CreateLogger 返回替身 ILogger）与 UiThreadDispatcher（同步执行版）
        // 已由 DesktopTestBase 装配好，其余成员在此显式给出替身。
        var eventAggregator = Substitute.For<IEventAggregator>();
        var regionManager = Substitute.For<IRegionManager>();
        var sessionManager = Substitute.For<ISessionManager>();
        var userNotificationService = Substitute.For<IUserNotificationService>();
        var commonDialogService = Substitute.For<ICommonDialogService>();
        var roleRegistry = Substitute.For<IRoleRegistry>();

        Services.EventAggregator.Returns(eventAggregator);
        Services.RegionManager.Returns(regionManager);
        Services.SessionManager.Returns(sessionManager);
        Services.UserNotificationService.Returns(userNotificationService);
        Services.CommonDialogService.Returns(commonDialogService);
        Services.RoleRegistry.Returns(roleRegistry);

        // 默认：本地模式 + 默认本地库配置 + 可用的本地库自检 + 合法的远程 URL
        _connectionMode.CurrentMode.Returns(ConnectionMode.Local);
        _connectionMode.CurrentModeDisplay.Returns("本地模式");
        _connectionMode.CheckRemoteAvailableAsync().Returns(Task.FromResult(true));
        _connectionMode.SetModeAsync(Arg.Any<ConnectionMode>()).Returns(Task.FromResult(ModeSwitchResult.Success()));

        _connectionSettings.RemoteUrl.Returns(SavedRemoteUrl);
        _connectionSettings.IsValidUrl(Arg.Any<string>()).Returns(true);

        _localDatabase.Current.Returns(LocalDatabaseProfile.Default);
        _localDatabase.TestAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));
        _localDatabase.SaveAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));

        _clinicSettings.ClinicName.Returns(string.Empty);
        _clinicSettings.Department.Returns(string.Empty);
        _clinicSettings.ClinicAddress.Returns(string.Empty);
        _clinicSettings.ClinicPhone.Returns(string.Empty);
        _clinicSettings.SaveSettingsAsync(Arg.Any<ClinicSettingsOptions>()).Returns(Task.FromResult(true));

        _adminService.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(false)));
        _adminService.CreateAdminAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));
    }

    private InitializationWizardViewModel CreateSut() => new(
        Services,
        _connectionMode,
        _connectionSettings,
        _adminService,
        _clinicSettings,
        _localDatabase,
        _firstRunState);

    /// <summary>构造 SUT 并经对话框入口驱动 LoadAsync（见类型 Remarks）</summary>
    private InitializationWizardViewModel CreateLoadedSut(bool isFirstRun = false)
    {
        var sut = CreateSut();
        var parameters = new DialogParameters();
        parameters.Add(InitializationWizardViewModel.FirstRunParameterKey, isFirstRun);
        sut.OnDialogOpened(parameters);
        return sut;
    }

    private void StubLocalMode() => _connectionMode.CurrentMode.Returns(ConnectionMode.Local);

    private void StubRemoteMode() => _connectionMode.CurrentMode.Returns(ConnectionMode.Remote);

    // ------------------------------------------------------------------
    // 1. 初始状态
    // ------------------------------------------------------------------

    [Fact]
    public void 初始状态_第一步为欢迎且不可返回()
    {
        var sut = CreateSut();

        sut.CurrentStep.Should().Be(InitializationWizardStep.Welcome);
        sut.IsStep1.Should().BeTrue();
        sut.IsStep2.Should().BeFalse();
        sut.CanGoBack.Should().BeFalse();
        sut.StepIndicator.Should().Be("第 1 / 5 步");
        sut.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void 第一步_下一步可执行而上一步无效()
    {
        var sut = CreateSut();

        sut.NextCommand.CanExecute(null).Should().BeTrue();

        sut.BackCommand.Execute(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Welcome);
        sut.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public async Task 第一步_下一步进入连接配置步且此时才可返回()
    {
        var sut = CreateLoadedSut();

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Connection);
        sut.IsStep2.Should().BeTrue();
        sut.CanGoBack.Should().BeTrue();
        sut.StepIndicator.Should().Be("第 2 / 5 步");

        sut.BackCommand.Execute(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Welcome);
        sut.CanGoBack.Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 2. 打开时预填
    // ------------------------------------------------------------------

    [Fact]
    public void 打开向导_远程模式_预填模式地址本地库与诊所信息()
    {
        StubRemoteMode();
        _connectionSettings.RemoteUrl.Returns("http://10.20.30.40:5000");
        _localDatabase.Current.Returns(new LocalDatabaseProfile
        {
            Provider = LocalDatabaseProvider.SqlServer,
            Server = @"srv-01\SQLEXPRESS",
            Database = "ClinicDb",
            UseWindowsAuthentication = false,
            UserId = "sa",
            Password = "DbP@ssw0rd"
        });
        _clinicSettings.ClinicName.Returns("康泰诊所");
        _clinicSettings.Department.Returns("全科");
        _clinicSettings.ClinicAddress.Returns("某市某路 1 号");
        _clinicSettings.ClinicPhone.Returns("010-12345678");

        var sut = CreateLoadedSut();

        sut.IsLocalModeSelected.Should().BeFalse();
        sut.IsRemoteModeSelected.Should().BeTrue();
        sut.RemoteUrl.Should().Be("http://10.20.30.40:5000");

        sut.LocalProvider.Should().Be(LocalDatabaseProvider.SqlServer);
        sut.IsSqlServerProvider.Should().BeTrue();
        sut.LocalServer.Should().Be(@"srv-01\SQLEXPRESS");
        sut.LocalDatabaseName.Should().Be("ClinicDb");
        sut.UseWindowsAuthentication.Should().BeFalse();
        sut.LocalUserId.Should().Be("sa");
        sut.LocalPassword.Should().Be("DbP@ssw0rd");

        sut.ClinicName.Should().Be("康泰诊所");
        sut.ClinicDepartment.Should().Be("全科");
        sut.ClinicAddress.Should().Be("某市某路 1 号");
        sut.ClinicPhone.Should().Be("010-12345678");

        sut.TestStatus.Should().Be(ConnectionTestStatus.Idle);
        sut.IsAdminStepSatisfied.Should().BeFalse();
    }

    [Fact]
    public void 打开向导_本地模式_预填默认本地库并自动自检()
    {
        StubLocalMode();
        _localDatabase.Current.Returns(LocalDatabaseProfile.Default);

        var sut = CreateLoadedSut();

        sut.IsLocalModeSelected.Should().BeTrue();
        sut.LocalProvider.Should().Be(LocalDatabaseProvider.LocalDb);
        sut.LocalServer.Should().Be(LocalDatabaseProfile.DefaultLocalDbServer);
        sut.LocalDatabaseName.Should().Be(LocalDatabaseProfile.DefaultDatabase);

        // 本地模式打开即自检（信息性，不阻断）
        _localDatabase.Received(1).TestAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>());
        sut.LocalTestSucceeded.Should().BeTrue();
        sut.LocalTestMessage.Should().Contain("可用");
    }

    [Fact]
    public async Task 打开向导_远程模式_不执行本地库自检()
    {
        StubRemoteMode();

        _ = CreateLoadedSut();

        await _localDatabase.DidNotReceive().TestAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void 打开向导_首次运行参数_驱动首次运行标记()
    {
        CreateLoadedSut(isFirstRun: true).IsFirstRunInvocation.Should().BeTrue();
        CreateLoadedSut(isFirstRun: false).IsFirstRunInvocation.Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 3. 步骤前进门禁
    // ------------------------------------------------------------------

    [Fact]
    public void 第二步本地分支_服务器或库名为空时不可下一步()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Connection;
        sut.IsLocalModeSelected = true;

        sut.LocalServer = "srv-01";
        sut.LocalDatabaseName = string.Empty;
        sut.NextCommand.CanExecute(null).Should().BeFalse();

        sut.LocalServer = string.Empty;
        sut.LocalDatabaseName = "ClinicDb";
        sut.NextCommand.CanExecute(null).Should().BeFalse();

        sut.LocalServer = "srv-01";
        sut.LocalDatabaseName = "ClinicDb";
        sut.NextCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task 第二步远程分支_测试未成功前不可下一步_成功后放行()
    {
        StubRemoteMode();
        _connectionMode.TestRemoteConnectionAsync(SavedRemoteUrl).Returns(Task.FromResult(false));
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Connection;
        sut.RemoteUrl = SavedRemoteUrl;

        sut.NextCommand.CanExecute(null).Should().BeFalse(); // 尚未测试

        await sut.TestConnectionCommand.ExecuteAsync(null);
        sut.TestStatus.Should().Be(ConnectionTestStatus.Failed);
        sut.NextCommand.CanExecute(null).Should().BeFalse();

        _connectionMode.TestRemoteConnectionAsync(SavedRemoteUrl).Returns(Task.FromResult(true));
        await sut.TestConnectionCommand.ExecuteAsync(null);

        sut.TestStatus.Should().Be(ConnectionTestStatus.Success);
        sut.NextCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void 第三步_诊所名称为空时不可下一步_填写后放行()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Clinic;

        sut.ClinicName = string.Empty;
        sut.NextCommand.CanExecute(null).Should().BeFalse();

        sut.ClinicName = "  ";
        sut.NextCommand.CanExecute(null).Should().BeFalse();

        sut.ClinicName = "康泰诊所";
        sut.NextCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void 第四步_管理员未满足时不可下一步_满足后放行()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Administrator;

        sut.IsAdminStepSatisfied = false;
        sut.NextCommand.CanExecute(null).Should().BeFalse();

        sut.IsAdminStepSatisfied = true;
        sut.NextCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void 第五步_完成页不可继续下一步()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Finish;

        sut.IsStep5.Should().BeTrue();
        sut.StepIndicator.Should().Be("第 5 / 5 步");
        sut.NextCommand.CanExecute(null).Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 4. 第 2 步本地分支：落盘 + 切模式
    // ------------------------------------------------------------------

    [Fact]
    public async Task 第二步本地分支_下一步落盘配置并切换本地模式()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Connection;
        sut.IsLocalModeSelected = true;
        sut.LocalProvider = LocalDatabaseProvider.SqlServer;
        sut.LocalServer = "srv-01";
        sut.LocalDatabaseName = "ClinicDb";
        sut.UseWindowsAuthentication = false;
        sut.LocalUserId = "sa";
        sut.LocalPassword = "DbP@ssw0rd";

        LocalDatabaseProfile? saved = null;
        _localDatabase.SaveAsync(Arg.Do<LocalDatabaseProfile>(p => saved = p), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));
        _connectionMode.SetModeAsync(Arg.Any<ConnectionMode>()).Returns(Task.FromResult(ModeSwitchResult.Success()));
        _connectionMode.ClearReceivedCalls();

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Clinic);
        saved.Should().NotBeNull();
        saved!.Provider.Should().Be(LocalDatabaseProvider.SqlServer);
        saved.Server.Should().Be("srv-01");
        saved.Database.Should().Be("ClinicDb");
        saved.UseWindowsAuthentication.Should().BeFalse();
        saved.UserId.Should().Be("sa");
        saved.Password.Should().Be("DbP@ssw0rd");
        await _connectionMode.Received(1).SetModeAsync(ConnectionMode.Local);
    }

    [Fact]
    public async Task 第二步本地分支_保存失败时停留原步并报错()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Connection;
        sut.IsLocalModeSelected = true;
        sut.LocalServer = "srv-01";
        sut.LocalDatabaseName = "ClinicDb";

        _localDatabase.SaveAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Failed("无法写入配置目录")));
        _connectionMode.ClearReceivedCalls();

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Connection);
        sut.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        await _connectionMode.DidNotReceive().SetModeAsync(Arg.Any<ConnectionMode>());
    }

    // ------------------------------------------------------------------
    // 5. 第 2 步远程分支：探测 + 切模式
    // ------------------------------------------------------------------

    [Fact]
    public async Task 第二步远程分支_服务器不可达时停留原步且不切换模式()
    {
        StubRemoteMode();
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Connection;
        sut.RemoteUrl = SavedRemoteUrl;
        sut.TestStatus = ConnectionTestStatus.Success;

        _connectionMode.CheckRemoteAvailableAsync().Returns(Task.FromResult(false));
        _connectionMode.ClearReceivedCalls();
        _connectionSettings.ClearReceivedCalls();

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Connection);
        sut.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        await _connectionSettings.Received(1).SetUrlAsync(SavedRemoteUrl);
        await _connectionMode.DidNotReceive().SetModeAsync(Arg.Any<ConnectionMode>());
    }

    [Fact]
    public async Task 第二步远程分支_可达时保存地址并切换远程模式()
    {
        StubRemoteMode();
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Connection;
        sut.RemoteUrl = SavedRemoteUrl;
        sut.TestStatus = ConnectionTestStatus.Success;

        _connectionMode.CheckRemoteAvailableAsync().Returns(Task.FromResult(true));
        _connectionMode.ClearReceivedCalls();
        _connectionSettings.ClearReceivedCalls();

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Clinic);
        sut.ErrorMessage.Should().BeEmpty();
        await _connectionSettings.Received(1).SetUrlAsync(SavedRemoteUrl);
        await _connectionMode.Received(1).SetModeAsync(ConnectionMode.Remote);
    }

    // ------------------------------------------------------------------
    // 6. 第 3 步：诊所信息落盘
    // ------------------------------------------------------------------

    [Fact]
    public async Task 第三步_下一步写入诊所信息()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Clinic;
        sut.ClinicName = "康泰诊所";
        sut.ClinicDepartment = "口腔科";
        sut.ClinicAddress = "某市某路 2 号";
        sut.ClinicPhone = "021-88889999";

        ClinicSettingsOptions? saved = null;
        _clinicSettings.SaveSettingsAsync(Arg.Do<ClinicSettingsOptions>(o => saved = o))
            .Returns(Task.FromResult(true));

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Administrator);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("康泰诊所");
        saved.Department.Should().Be("口腔科");
        saved.Address.Should().Be("某市某路 2 号");
        saved.Phone.Should().Be("021-88889999");
    }

    [Fact]
    public async Task 第三步_保存失败时停留原步并报错()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Clinic;
        sut.ClinicName = "康泰诊所";

        _clinicSettings.SaveSettingsAsync(Arg.Any<ClinicSettingsOptions>()).Returns(Task.FromResult(false));

        await sut.NextCommand.ExecuteAsync(null);

        sut.CurrentStep.Should().Be(InitializationWizardStep.Clinic);
        sut.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    // ------------------------------------------------------------------
    // 7. 第 4 步：创建初始管理员
    // ------------------------------------------------------------------

    [Fact]
    public async Task 第四步_密码不满足策略时不调用创建服务并提示()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Administrator;
        sut.AdminUserName = "admin";
        sut.AdminRealName = "张三";
        sut.AdminPassword = "123";
        sut.AdminConfirmPassword = "123";

        await sut.CreateAdminCommand.ExecuteAsync(null);

        await _adminService.DidNotReceive()
            .CreateAdminAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        sut.IsAdminStepSatisfied.Should().BeFalse();
        sut.AdminMessage.Should().NotBeNullOrWhiteSpace();
        sut.NextCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task 第四步_两次密码不一致时不调用创建服务并提示()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Administrator;
        sut.AdminUserName = "admin";
        sut.AdminRealName = "张三";
        sut.AdminPassword = ValidPassword;
        sut.AdminConfirmPassword = "Lybt@2025";

        await sut.CreateAdminCommand.ExecuteAsync(null);

        await _adminService.DidNotReceive()
            .CreateAdminAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        sut.IsAdminStepSatisfied.Should().BeFalse();
        sut.AdminMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task 第四步_账号已存在时视为满足且不重复创建()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Administrator;
        sut.AdminUserName = "admin";
        sut.AdminRealName = "张三";
        sut.AdminPassword = ValidPassword;
        sut.AdminConfirmPassword = ValidPassword;

        _adminService.ExistsAsync("admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));

        await sut.CreateAdminCommand.ExecuteAsync(null);

        sut.IsAdminStepSatisfied.Should().BeTrue();
        sut.AdminMessage.Should().Contain("已存在");
        await _adminService.DidNotReceive()
            .CreateAdminAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        sut.NextCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task 第四步_创建成功时标记满足并提示成功()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Administrator;
        sut.AdminUserName = "admin";
        sut.AdminRealName = "张三";
        sut.AdminPassword = ValidPassword;
        sut.AdminConfirmPassword = ValidPassword;

        string? createdUser = null;
        string? createdRealName = null;
        string? createdPassword = null;
        _adminService.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(false)));
        _adminService.CreateAdminAsync(
                Arg.Do<string>(u => createdUser = u),
                Arg.Do<string>(r => createdRealName = r),
                Arg.Do<string>(p => createdPassword = p),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));

        await sut.CreateAdminCommand.ExecuteAsync(null);

        sut.IsAdminStepSatisfied.Should().BeTrue();
        sut.AdminMessage.Should().Contain("已创建");
        createdUser.Should().Be("admin");
        createdRealName.Should().Be("张三");
        createdPassword.Should().Be(ValidPassword);
        sut.NextCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task 第四步_创建失败时保持未满足并提示错误()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Administrator;
        sut.AdminUserName = "admin";
        sut.AdminRealName = "张三";
        sut.AdminPassword = ValidPassword;
        sut.AdminConfirmPassword = ValidPassword;

        _adminService.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(false)));
        _adminService.CreateAdminAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Failed("用户名已被占用")));

        await sut.CreateAdminCommand.ExecuteAsync(null);

        sut.IsAdminStepSatisfied.Should().BeFalse();
        sut.AdminMessage.Should().Contain("创建失败");
        sut.NextCommand.CanExecute(null).Should().BeFalse();
    }

    // ------------------------------------------------------------------
    // 8. 第 5 步：完成
    // ------------------------------------------------------------------

    [Fact]
    public async Task 向导流程_本地分支走完五步_完成写入标记并关闭对话框()
    {
        IDialogResult? closed = null;
        var sut = CreateLoadedSut();
        sut.RequestClose += r => closed = r;

        // 第 1 步
        sut.NextCommand.CanExecute(null).Should().BeTrue();
        await sut.NextCommand.ExecuteAsync(null);
        sut.CurrentStep.Should().Be(InitializationWizardStep.Connection);

        // 第 2 步（本地分支：默认库配置已合法）
        await sut.NextCommand.ExecuteAsync(null);
        sut.CurrentStep.Should().Be(InitializationWizardStep.Clinic);
        await _localDatabase.Received().SaveAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>());
        await _connectionMode.Received().SetModeAsync(ConnectionMode.Local);

        // 第 3 步
        sut.ClinicName = "康泰诊所";
        await sut.NextCommand.ExecuteAsync(null);
        sut.CurrentStep.Should().Be(InitializationWizardStep.Administrator);

        // 第 4 步
        sut.AdminUserName = "admin";
        sut.AdminRealName = "张三";
        sut.AdminPassword = ValidPassword;
        sut.AdminConfirmPassword = ValidPassword;
        await sut.CreateAdminCommand.ExecuteAsync(null);
        sut.IsAdminStepSatisfied.Should().BeTrue();
        await sut.NextCommand.ExecuteAsync(null);
        sut.CurrentStep.Should().Be(InitializationWizardStep.Finish);
        sut.ValidationItems.Should().NotBeEmpty();
        sut.ValidationItems.Should().OnlyContain(i => i.IsPassed);

        // 第 5 步
        await sut.FinishCommand.ExecuteAsync(null);

        sut.IsCompleted.Should().BeTrue();
        sut.ErrorMessage.Should().BeEmpty();
        _firstRunState.Received(1).MarkCompleted();
        closed.Should().NotBeNull();
        closed!.Result.Should().Be(ButtonResult.OK);
    }

    [Fact]
    public async Task 第五步_校验项未通过时不写标记并报错()
    {
        IDialogResult? closed = null;
        var sut = CreateLoadedSut();
        sut.RequestClose += r => closed = r;
        sut.CurrentStep = InitializationWizardStep.Finish;
        sut.ClinicName = "康泰诊所";
        sut.IsAdminStepSatisfied = false;

        // 管理员既未创建、服务端也不存在 → 校验清单有未通过项
        _adminService.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(false)));

        await sut.FinishCommand.ExecuteAsync(null);

        sut.IsCompleted.Should().BeFalse();
        sut.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        sut.ValidationItems.Should().Contain(i => !i.IsPassed && i.Title == "管理员账号");
        _firstRunState.DidNotReceive().MarkCompleted();
        closed.Should().BeNull();
    }

    [Fact]
    public async Task 第五步_管理员账号已存在时完成通过()
    {
        var sut = CreateLoadedSut();
        sut.CurrentStep = InitializationWizardStep.Finish;
        sut.ClinicName = "康泰诊所";
        sut.IsAdminStepSatisfied = false;

        _adminService.ExistsAsync("admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));

        await sut.FinishCommand.ExecuteAsync(null);

        sut.IsCompleted.Should().BeTrue();
        sut.ValidationItems.Should().OnlyContain(i => i.IsPassed);
        _firstRunState.Received(1).MarkCompleted();
    }

    // ------------------------------------------------------------------
    // 9. 取消
    // ------------------------------------------------------------------

    [Fact]
    public void 取消向导_不写完成标记并请求关闭()
    {
        IDialogResult? closed = null;
        var sut = CreateLoadedSut();
        sut.RequestClose += r => closed = r;

        sut.CancelWizardCommand.Execute(null);

        _firstRunState.DidNotReceive().MarkCompleted();
        sut.IsCompleted.Should().BeFalse();
        closed.Should().NotBeNull();
        closed!.Result.Should().Be(ButtonResult.Cancel);
    }

    // ------------------------------------------------------------------
    // 本地库连接测试命令
    // ------------------------------------------------------------------

    [Fact]
    public async Task 本地库测试命令_成功时更新测试状态()
    {
        var sut = CreateLoadedSut();
        _localDatabase.TestAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Succeeded(true)));

        await sut.TestLocalDatabaseCommand.ExecuteAsync(null);

        sut.LocalTestSucceeded.Should().BeTrue();
        sut.LocalTestMessage.Should().Contain("可用");
    }

    [Fact]
    public async Task 本地库测试命令_失败时给出不可用信息()
    {
        var sut = CreateLoadedSut();
        _localDatabase.TestAsync(Arg.Any<LocalDatabaseProfile>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<bool>.Failed("Login failed for user 'sa'")));

        await sut.TestLocalDatabaseCommand.ExecuteAsync(null);

        sut.LocalTestSucceeded.Should().BeFalse();
        sut.LocalTestMessage.Should().Contain("不可用");
        sut.LocalTestMessage.Should().Contain("Login failed for user 'sa'");
    }
}
