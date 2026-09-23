using LYBT.Tests.Desktop.Infrastructure;
using System.Collections.ObjectModel;
using FluentAssertions;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.MedicalCase.Dialogs;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

[Trait("US", "US-FORM-006")]
public class FormulaImportDialogViewModelTests : DesktopTestBase
{
    private readonly IViewModelServices _services;
    private readonly IFormulaSearchProvider _provider;

    public FormulaImportDialogViewModelTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var eventAggregator = Substitute.For<IEventAggregator>();
        var regionManager = Substitute.For<IRegionManager>();
        var sessionManager = Substitute.For<ISessionManager>();
        var userNotification = Substitute.For<IUserNotificationService>();
        var commonDialog = Substitute.For<ICommonDialogService>();
        var toast = Substitute.For<IToastService>();
        var roleRegistry = Substitute.For<IRoleRegistry>();
        var uiDispatcher = Substitute.For<IUiThreadDispatcher>();

        _services = Substitute.For<IViewModelServices>();
        _services.LoggerFactory.Returns(loggerFactory);
        _services.EventAggregator.Returns(eventAggregator);
        _services.RegionManager.Returns(regionManager);
        _services.SessionManager.Returns(sessionManager);
        _services.UserNotificationService.Returns(userNotification);
        _services.CommonDialogService.Returns(commonDialog);
        _services.ToastService.Returns(toast);
        _services.RoleRegistry.Returns(roleRegistry);
        _services.UiThreadDispatcher.Returns(uiDispatcher);

        _provider = Substitute.For<IFormulaSearchProvider>();
        // 默认返回空，避免 LoadFormulasAsync 在构造后空跑
        _provider.GetFormulasPagedAsync(Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromResult(new PagedResult<FormulaListDto> { Items = new List<FormulaListDto>(), TotalCount = 0 }));
    }

    private FormulaImportDialogViewModel CreateSut() => new(_services, _provider);

    private static FormulaListDto CreateFormula(string name, string category, FormulaValidationStatus status = FormulaValidationStatus.Validated, CommonStatus common = CommonStatus.Enabled)
        => new() { Id = Guid.NewGuid(), Name = name, Category = category, ValidationStatus = status, Status = common, Effect = "test", Indication = "test" };

    private void SetAllFormulas(FormulaImportDialogViewModel sut, List<FormulaListDto> list)
    {
        var field = typeof(FormulaImportDialogViewModel).GetField("_allFormulas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(sut, list);
    }

    [Fact]
    public async Task SearchAsync_WithKeyword_ReturnsFilteredList_Should_When_KeywordMatches()
    {
        // Arrange
        var sut = CreateSut();
        var f1 = CreateFormula("四君子汤", "内科方");
        var f2 = CreateFormula("小柴胡汤", "内科方", FormulaValidationStatus.Draft);
        var f3 = CreateFormula("桂枝汤", "外科方");
        SetAllFormulas(sut, new List<FormulaListDto> { f1, f2, f3 });
        // 初始筛选（Draft 被滤）
        sut.SearchText = string.Empty;
        await Task.Delay(50);
        // Act
        sut.SearchText = "四君子";
        await Task.Delay(50);
        // Assert
        sut.FilteredFormulas.Should().Contain(f1);
        sut.FilteredFormulas.Should().NotContain(f2); // Draft 被滤
        sut.FilteredFormulas.Count.Should().Be(1);
    }

    [Fact]
    public void SelectFormula_SetsSelectedFormula_Should_When_ValidFormulaSelected()
    {
        // Arrange
        var sut = CreateSut();
        var f1 = CreateFormula("四君子汤", "内科方");
        _provider.GetFormulaByIdAsync(Arg.Any<Guid>()).Returns(Task.FromResult<FormulaDetailDto?>(new FormulaDetailDto { Id = f1.Id, Name = f1.Name, Herbs = new List<FormulaHerbItemDto> { new() { HerbName = "人参", Dosage = 10, Unit = "g" } } }));

        // Act
        sut.SelectedFormula = f1;

        // Assert
        sut.SelectedFormula.Should().Be(f1);
    }

    [Fact]
    public async Task PreviewImport_ReturnsPreviewData_Should_When_FormulaSelectedWithHerbs()
    {
        // Arrange
        var sut = CreateSut();
        var f1 = CreateFormula("四君子汤", "内科方");
        var detail = new FormulaDetailDto
        {
            Id = f1.Id,
            Name = f1.Name,
            Herbs = new List<FormulaHerbItemDto> { new() { HerbName = "人参", Dosage = 10, Unit = "g" }, new() { HerbName = "白术", Dosage = 10, Unit = "g" } }
        };
        _provider.GetFormulaByIdAsync(f1.Id).Returns(Task.FromResult<FormulaDetailDto?>(detail));

        // Act
        sut.SelectedFormula = f1;
        await Task.Delay(150); // 等待 LoadFormulaPreviewInternalAsync (SafeFireAndForget)

        // Assert
        sut.SelectedFormulaDetail.Should().NotBeNull();
        sut.SelectedFormulaHerbs.Should().HaveCount(2);
        sut.SelectedFormulaHerbs[0].HerbName.Should().Be("人参");
    }

    [Fact]
    public async Task ImportToPrescription_CallsService_Should_When_ConfirmWithHerbs()
    {
        // Arrange
        var sut = CreateSut();
        var f1 = CreateFormula("四君子汤", "内科方");
        var detail = new FormulaDetailDto
        {
            Id = f1.Id,
            Name = f1.Name,
            Herbs = new List<FormulaHerbItemDto> { new() { HerbName = "人参", Dosage = 10, Unit = "g" } }
        };
        _provider.GetFormulaByIdAsync(f1.Id).Returns(Task.FromResult<FormulaDetailDto?>(detail));
        sut.SelectedFormula = f1;
        await Task.Delay(150);
        // 此时 SelectedFormulaHerbs 已有1条，CanConfirm 应为 true
        // Act
        var canConfirm = sut.ConfirmCommand.CanExecute(null);

        // Assert
        canConfirm.Should().BeTrue("选中验方且有药材时应可确认导入");
        sut.SelectedFormulaHerbs.Should().NotBeEmpty();
    }
}
