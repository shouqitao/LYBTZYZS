using LYBT.Tests.Desktop.Infrastructure;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.LocalData.Context;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.EndToEnd.Formula;

/// <summary>
/// Formula 模块 E2E 集成测试
/// ViewModel -> Repository -> DataSource -> LocalDbContext(SQLite InMemory)
/// </summary>
public class FormulaE2ETests : IDisposable
{
    private readonly DesktopFixture _fixture;

    public FormulaE2ETests()
    {
        _fixture = new DesktopFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public async Task Formula_Create_EndToEnd()
    {
        var vm = _fixture.ServiceProvider.GetRequiredService<FormulaMasterDetailViewModel>();

        // Act - 新建验方
        await vm.CreateNewCommand.ExecuteAsync(null);
        await Task.Delay(200);

        vm.IsEditMode.Should().BeTrue();
        vm.CurrentDetail.Should().NotBeNull();

        vm.CurrentDetail!.Name = "四君子汤";
        vm.CurrentDetail.Effect = "益气健脾";
        vm.CurrentDetail.Usage = "水煎服";
        vm.CurrentDetail.Category = "补益方";

        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 验证 DB
        var db = _fixture.GetDbContext();
        var formula = await db.Formulas.FirstOrDefaultAsync(f => f.Name == "四君子汤");
        formula.Should().NotBeNull();
        formula!.Effect.Should().Be("益气健脾");
        formula.Usage.Should().Be("水煎服");
    }

    [Fact]
    public async Task Formula_LoadWithHerbs_EndToEnd()
    {
        // Arrange - 预置含药材的验方
        var formulaId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Formulas.Add(new LYBT.Entities.Formulas.Formula
            {
                Id = formulaId,
                Name = "六味地黄丸",
                Effect = "滋阴补肾",
                Indication = "肾阴虚证",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.FormulaHerbItems.AddRange(
                new FormulaHerbItem { Id = Guid.NewGuid(), FormulaId = formulaId, HerbName = "熟地黄", Dosage = 24, Unit = "g" },
                new FormulaHerbItem { Id = Guid.NewGuid(), FormulaId = formulaId, HerbName = "山茱萸", Dosage = 12, Unit = "g" },
                new FormulaHerbItem { Id = Guid.NewGuid(), FormulaId = formulaId, HerbName = "山药", Dosage = 12, Unit = "g" }
            );
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<FormulaMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        vm.Items.Should().HaveCount(1);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(300);

        // Assert - 验证详情加载
        vm.CurrentDetail.Should().NotBeNull();
        vm.CurrentDetail!.Name.Should().Be("六味地黄丸");
    }

    [Fact]
    public async Task Formula_Update_EndToEnd()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Formulas.Add(new LYBT.Entities.Formulas.Formula
            {
                Id = formulaId,
                Name = "逍遥散",
                Effect = "疏肝解郁",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<FormulaMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(300);

        vm.EditCommand.Execute(null);
        await Task.Delay(200);
        vm.CurrentDetail!.Effect = "疏肝解郁，养血健脾";
        vm.CurrentDetail.Usage = "水煎温服";

        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        var db = _fixture.GetDbContext();
        var updated = await db.Formulas.FindAsync(formulaId);
        updated.Should().NotBeNull();
        updated!.Usage.Should().Be("水煎温服");
    }

    [Fact]
    public async Task Formula_Delete_EndToEnd()
    {
        // Arrange
        var formulaId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Formulas.Add(new LYBT.Entities.Formulas.Formula
            {
                Id = formulaId,
                Name = "待删除方",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.FormulaHerbItems.Add(new FormulaHerbItem
            {
                Id = Guid.NewGuid(),
                FormulaId = formulaId,
                HerbName = "附子",
                Dosage = 10,
                Unit = "g",
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<FormulaMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(200);

        await vm.DeleteCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().NotContain(f => f.Name == "待删除方");
    }

    [Fact]
    public async Task Formula_Search_ByName()
    {
        // Arrange
        await _fixture.SeedDataAsync(async db =>
        {
            db.Formulas.AddRange(
                new LYBT.Entities.Formulas.Formula { Id = Guid.NewGuid(), Name = "四君子汤", Effect = "益气健脾", Status = CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new LYBT.Entities.Formulas.Formula { Id = Guid.NewGuid(), Name = "四物汤", Effect = "补血活血", Status = CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new LYBT.Entities.Formulas.Formula { Id = Guid.NewGuid(), Name = "逍遥散", Effect = "疏肝解郁", Status = CommonStatus.Enabled, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<FormulaMasterDetailViewModel>();
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().HaveCount(3);

        // Act
        vm.SearchText = "四";
        await vm.SearchCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.Items.Should().OnlyContain(f => f.Name.Contains("四"));
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
