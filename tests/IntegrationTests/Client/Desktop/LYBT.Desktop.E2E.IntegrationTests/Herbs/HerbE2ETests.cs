using LYBT.Desktop.E2E.IntegrationTests.Fixtures;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.E2E.IntegrationTests.Herbs;

/// <summary>
/// Herb 模块 E2E 集成测试
/// ViewModel -> Repository -> DataSource -> LocalDbContext(SQLite InMemory)
/// </summary>
public class HerbE2ETests : IDisposable
{
    private readonly DesktopE2ETestFixture _fixture;

    public HerbE2ETests()
    {
        _fixture = new DesktopE2ETestFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public async Task Herb_CRUD_EndToEnd()
    {
        var vm = _fixture.ServiceProvider.GetRequiredService<HerbMasterDetailViewModel>();

        // === Create ===
        await vm.CreateNewCommand.ExecuteAsync(null);
        await Task.Delay(200);

        vm.IsEditMode.Should().BeTrue();
        vm.CurrentDetail.Should().NotBeNull();
        vm.CurrentDetail!.Name = "黄芪";
        vm.CurrentDetail.Category = "补益药";
        vm.CurrentDetail.Unit = "克";
        vm.CurrentDetail.Price = 2.5m;

        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // 验证 DB - 核心字段
        var db = _fixture.GetDbContext();
        var created = await db.Herbs.FirstOrDefaultAsync(h => h.Name == "黄芪");
        created.Should().NotBeNull();
        created!.Name.Should().Be("黄芪");
        created.Price.Should().Be(2.5m);

        // === Update ===
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(300);

        vm.EditCommand.Execute(null);
        vm.CurrentDetail!.Price = 3.0m;
        vm.CurrentDetail.Effect = "补气固表";

        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // 验证更新
        var updated = await db.Herbs.FirstOrDefaultAsync(h => h.Name == "黄芪");
        updated.Should().NotBeNull();
        updated!.Price.Should().Be(3.0m);
    }

    [Fact]
    public async Task Herb_Search_ByPinYinCode()
    {
        // Arrange
        await _fixture.SeedDataAsync(async db =>
        {
            db.Herbs.AddRange(
                new Herb { Id = Guid.NewGuid(), Name = "黄芪", PinYinCode = "HQ", Category = "补益药", Unit = "克", Status = CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Herb { Id = Guid.NewGuid(), Name = "当归", PinYinCode = "DG", Category = "补血药", Unit = "克", Status = CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Herb { Id = Guid.NewGuid(), Name = "黄连", PinYinCode = "HL", Category = "清热药", Unit = "克", Status = CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<HerbMasterDetailViewModel>();
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().HaveCount(3);

        // Act - 搜索拼音码
        vm.SearchText = "HQ";
        await vm.SearchCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.Items.Should().Contain(h => h.Name == "黄芪");
    }

    [Fact]
    public async Task Herb_Paging_ShouldReturnCorrectPage()
    {
        // Arrange - 25条药材数据
        await _fixture.SeedDataAsync(async db =>
        {
            for (int i = 1; i <= 25; i++)
            {
                db.Herbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"药材{i:D2}",
                    PinYinCode = $"YC{i:D2}",
                    Unit = "克",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<HerbMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.TotalCount.Should().Be(25);
        vm.Items.Count.Should().BeLessOrEqualTo(vm.PageSize);
        vm.TotalPages.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Herb_SoftDelete_EndToEnd()
    {
        // Arrange
        var herbId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Herbs.Add(new Herb
            {
                Id = herbId,
                Name = "待删除药材",
                PinYinCode = "DCYC",
                Unit = "克",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<HerbMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().HaveCount(1);

        vm.SelectedItem = vm.Items.First();
        await Task.Delay(200);

        await vm.DeleteCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 刷新后不再显示
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().NotContain(h => h.Name == "待删除药材");
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
