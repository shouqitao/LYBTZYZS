using LYBT.Tests.Desktop.Integration.EndToEnd.Fixtures;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Prism.Regions;

namespace LYBT.Tests.Desktop.Integration.EndToEnd.Navigation;

/// <summary>
/// Navigation Flow E2E 集成测试
/// 验证 Prism 导航上下文中 ViewModel 的加载和状态管理
/// </summary>
public class NavigationFlowE2ETests : IDisposable
{
    private readonly DesktopE2ETestFixture _fixture;

    public NavigationFlowE2ETests()
    {
        _fixture = new DesktopE2ETestFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public void Navigation_ResolveViewModel_ShouldSucceed()
    {
        // Act - 验证 DI 容器能成功解析所有 ViewModel
        var patientVm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();
        var herbVm = _fixture.ServiceProvider.GetRequiredService<HerbMasterDetailViewModel>();

        // Assert
        patientVm.Should().NotBeNull();
        herbVm.Should().NotBeNull();
    }

    [Fact]
    public async Task Navigation_OnNavigatedTo_ShouldTriggerLoad()
    {
        // Arrange - 预置数据
        await _fixture.SeedDataAsync(async db =>
        {
            for (int i = 1; i <= 3; i++)
            {
                db.Patients.Add(new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"导航测试患者{i}",
                    Gender = Gender.Male,
                    PinYinCode = $"DHCS{i}",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 模拟 Prism 导航
        var navService = Substitute.For<IRegionNavigationService>();
        var navContext = new NavigationContext(
            navService,
            new Uri("PatientMasterDetailView", UriKind.Relative));

        vm.OnNavigatedTo(navContext);
        await Task.Delay(1000); // 等待异步加载

        // Assert
        vm.Items.Should().HaveCount(3);
        vm.TotalCount.Should().Be(3);
    }

    [Fact]
    public void Navigation_PatientAndHerb_IndependentViewModels()
    {
        // Act - 同时解析两个不同模块的 ViewModel
        var patientVm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();
        var herbVm = _fixture.ServiceProvider.GetRequiredService<HerbMasterDetailViewModel>();

        // Assert - 独立实例，各有各的 Items 集合
        patientVm.Should().NotBeNull();
        herbVm.Should().NotBeNull();
        patientVm.Items.Should().NotBeNull();
        herbVm.Items.Should().NotBeNull();
        patientVm.PageTitle.Should().NotBe(herbVm.PageTitle);
    }

    [Fact]
    public async Task Navigation_Refresh_ShouldReloadData()
    {
        // Arrange
        await _fixture.SeedDataAsync(async db =>
        {
            db.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(), Name = "刷新测试", Gender = Gender.Female,
                PinYinCode = "SXCS", Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 首次加载
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        var firstCount = vm.Items.Count;

        // Act - 再次刷新（应保持一致）
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.Items.Count.Should().Be(firstCount);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
