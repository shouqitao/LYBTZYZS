using LYBT.Desktop.Admin.Sysadmin.ViewModels;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Services.FeatureToggle;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LYBT.Tests.Desktop.Unit.Sysadmin;

/// <summary>
/// 配置中心面板 VM 单测（SHELL-018 Phase 2: 加载 / 校验 / 保存路由）
/// </summary>
public class ConfigurationCenterViewModelTests
{
    private readonly IClientConfigurationStore _store;
    private readonly IFeatureToggleService _featureToggles;
    private readonly IConnectionModeService _connectionMode;
    private readonly IOptions<ClinicSettingsOptions> _clinicOptions;
    private readonly IOptions<ClientSessionOptions> _sessionOptions;
    private readonly IOptions<ApiClientOptions> _apiOptions;
    private readonly IOptions<CardReaderOptions> _cardReaderOptions;

    public ConfigurationCenterViewModelTests()
    {
        _store = Substitute.For<IClientConfigurationStore>();
        _featureToggles = Substitute.For<IFeatureToggleService>();
        _connectionMode = Substitute.For<IConnectionModeService>();
        _clinicOptions = Options.Create(new ClinicSettingsOptions
        {
            Name = "凌隐宝堂", Address = "测试路 1 号", Phone = "010-1234", Department = "中医科"
        });
        _sessionOptions = Options.Create(new ClientSessionOptions
        {
            InactivityTimeoutMinutes = 30, WarningBeforeTimeoutMinutes = 2, ActivityCheckIntervalSeconds = 30
        });
        _apiOptions = Options.Create(new ApiClientOptions
        {
            BaseUrl = "http://localhost:5000/", RemoteUrl = "http://192.168.1.10:5000", TimeoutSeconds = 60
        });
        _cardReaderOptions = Options.Create(new CardReaderOptions());
    }

    private ConfigurationCenterViewModel CreateVm()
    {
        _featureToggles.IsEnabled("OverwriteConflicts").Returns(true);
        _featureToggles.GetValue("DuplicateHerbMergeStrategy").Returns("Max");
        _store.SaveSectionAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, object>>())
            .Returns(true);

        return new ConfigurationCenterViewModel(
            Substitute.For<IViewModelServices>(),
            _store, _featureToggles, _connectionMode,
            _clinicOptions, _sessionOptions, _apiOptions, _cardReaderOptions);
    }

    [Fact]
    public void Constructor_LoadsOptions_IntoProperties()
    {
        var vm = CreateVm();

        vm.ClinicName.Should().Be("凌隐宝堂");
        vm.ClinicAddress.Should().Be("测试路 1 号");
        vm.InactivityTimeoutMinutes.Should().Be(30);
        vm.WarningBeforeTimeoutMinutes.Should().Be(2);
        vm.ApiBaseUrl.Should().Be("http://192.168.1.10:5000"); // RemoteUrl 优先（远程模式地址）
        vm.ApiTimeoutSeconds.Should().Be(60);
        vm.OverwriteConflicts.Should().BeTrue();
        vm.DuplicateHerbMergeStrategy.Should().Be("Max");
        vm.CardReaderStatus.Should().Contain("UsbPort");
    }

    [Fact]
    public async Task SaveSessionAsync_InvalidRange_RejectsWithoutStoreCall()
    {
        var vm = CreateVm();
        vm.InactivityTimeoutMinutes = 999;

        await vm.SaveSessionCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("1-120");
        await _store.DidNotReceiveWithAnyArgs().SaveSectionAsync(default!, default!);
    }

    [Fact]
    public async Task SaveConnectionAsync_InvalidUrl_RejectsWithoutStoreCall()
    {
        var vm = CreateVm();
        vm.ApiBaseUrl = "not-a-url";

        await vm.SaveConnectionCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("URL");
        await _store.DidNotReceiveWithAnyArgs().SaveSectionAsync(default!, default!);
    }

    [Fact]
    public async Task SaveFeatureTogglesAsync_InvalidStrategy_Rejects()
    {
        var vm = CreateVm();
        vm.DuplicateHerbMergeStrategy = "Bogus";

        await vm.SaveFeatureTogglesCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("Skip");
        await _store.DidNotReceiveWithAnyArgs().SaveSectionAsync(default!, default!);
    }

    [Fact]
    public async Task SaveClinicAsync_SavesClinicSection_ToStore()
    {
        var vm = CreateVm();
        vm.ClinicName = "凌隐宝堂二店";

        await vm.SaveClinicCommand.ExecuteAsync(null);

        await _store.Received(1).SaveSectionAsync(
            "ClinicSettings",
            Arg.Is<IReadOnlyDictionary<string, object>>(d => (string)d["Name"] == "凌隐宝堂二店"));
        vm.StatusMessage.Should().Contain("即时生效");
    }

    [Fact]
    public async Task SaveFeatureTogglesAsync_SavesToStore_WithHotReloadMessage()
    {
        var vm = CreateVm();
        vm.OverwriteConflicts = false;

        await vm.SaveFeatureTogglesCommand.ExecuteAsync(null);

        await _store.Received(1).SaveSectionAsync(
            "FeatureToggles",
            Arg.Is<IReadOnlyDictionary<string, object>>(d => (bool)d["OverwriteConflicts"] == false));
        vm.StatusMessage.Should().Contain("即时生效");
    }
}
