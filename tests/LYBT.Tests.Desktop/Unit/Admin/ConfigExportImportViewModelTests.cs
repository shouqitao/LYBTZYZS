using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Admin.Services;
using LYBT.Desktop.Admin.Sysadmin.ViewModels;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Tests.Desktop.Infrastructure;

namespace LYBT.Tests.Desktop.Unit.Admin;

/// <summary>
/// US-SHELL-016 配置导入导出 VM 单测：路径选择、导出/导入命令、二次确认、
/// 报告铺开与重启提示（服务层已由 <see cref="ConfigurationPackageServiceTests"/> 覆盖）。
/// </summary>
public class ConfigExportImportViewModelTests : DesktopTestBase
{
    private const string PackagePath = @"C:\temp\lybt-config-20260923100000.json";

    private readonly IConfigurationPackageService _packageService = Substitute.For<IConfigurationPackageService>();
    private readonly IFileDialogService _fileDialogService = Substitute.For<IFileDialogService>();
    private readonly ICommonDialogService _commonDialogService = Substitute.For<ICommonDialogService>();
    private readonly IToastService _toastService = Substitute.For<IToastService>();

    public ConfigExportImportViewModelTests()
    {
        Services.CommonDialogService.Returns(_commonDialogService);
        Services.ToastService.Returns(_toastService);
    }

    private ConfigExportImportViewModel CreateVm() => new(Services, _packageService, _fileDialogService);

    [Fact]
    public void SelectExportPath_UsesJsonFilterAndTimestampedDefaultName()
    {
        _fileDialogService
            .ShowSaveFileDialog(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(PackagePath);
        var vm = CreateVm();

        vm.SelectExportPathCommand.Execute(null);

        vm.ExportPath.Should().Be(PackagePath);
        _fileDialogService.Received(1).ShowSaveFileDialog(
            "JSON 文件 (*.json)|*.json",
            ".json",
            Arg.Is<string>(name => name.StartsWith("lybt-config-", StringComparison.Ordinal)
                                   && name.EndsWith(".json", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ExportAsync_WithoutPath_ReportsErrorWithoutCallingService()
    {
        var vm = CreateVm();

        await vm.ExportCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("保存位置");
        await _packageService.DidNotReceiveWithAnyArgs().ExportAsync(default!, default);
    }

    [Fact]
    public async Task ExportAsync_Success_PublishesSummaryAndToast()
    {
        _packageService.ExportAsync(PackagePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<ConfigurationPackageResult>.Succeeded(
                new ConfigurationPackageResult
                {
                    FilePath = PackagePath,
                    FileSizeBytes = 2048,
                    Sections = new[] { "clinicSettings", "connection", "featureToggles", "rolePermissions" }
                })));
        var vm = CreateVm();
        vm.ExportPath = PackagePath;

        await vm.ExportCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().BeEmpty();
        vm.StatusMessage.Should().Contain("已导出配置包").And.Contain("4 个节");
        _toastService.Received(1).ShowSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ImportAsync_DeclinedConfirmation_SkipsServiceCall()
    {
        _commonDialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(false));
        var vm = CreateVm();
        vm.ImportPath = PackagePath;

        await vm.ImportCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Be("已取消导入");
        await _packageService.DidNotReceiveWithAnyArgs().ImportAsync(default!, default);
    }

    [Fact]
    public async Task ImportAsync_Confirmed_PublishesReportAndRestartHint()
    {
        _commonDialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        _packageService.ImportAsync(PackagePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<ConfigurationPackageResult>.Succeeded(
                new ConfigurationPackageResult
                {
                    FilePath = PackagePath,
                    AppliedSections = new[] { "clinicSettings", "connection.mode" },
                    SkippedItems = new[]
                    {
                        new ConfigurationPackageSkip("rolePermissions", "角色权限由代码定义，导入不应用")
                    },
                    Notes = new[] { "角色权限快照与当前程序一致" },
                    RequiresRestart = true
                })));
        var vm = CreateVm();
        vm.ImportPath = PackagePath;

        await vm.ImportCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().BeEmpty();
        vm.HasReport.Should().BeTrue();
        vm.RequiresRestart.Should().BeTrue();
        vm.ReportLines.Should().Contain("已应用：clinicSettings");
        vm.ReportLines.Should().Contain(line => line.StartsWith("已跳过：rolePermissions", StringComparison.Ordinal));
        vm.ReportLines.Should().Contain("说明：角色权限快照与当前程序一致");
        vm.StatusMessage.Should().Contain("导入完成").And.Contain("需重启生效");
        _toastService.Received(1).ShowSuccess("配置包导入完成");
    }

    [Fact]
    public async Task ImportAsync_ServiceFailure_SurfacesErrorAndToast()
    {
        _commonDialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.FromResult(true));
        _packageService.ImportAsync(PackagePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CommandResult<ConfigurationPackageResult>.Failed(
                "配置文件格式错误（JSON 解析失败），已拒绝导入")));
        var vm = CreateVm();
        vm.ImportPath = PackagePath;

        await vm.ImportCommand.ExecuteAsync(null);

        vm.ErrorMessage.Should().Contain("格式错误");
        vm.HasReport.Should().BeFalse();
        _toastService.Received(1).ShowError(Arg.Any<string>());
    }
}
