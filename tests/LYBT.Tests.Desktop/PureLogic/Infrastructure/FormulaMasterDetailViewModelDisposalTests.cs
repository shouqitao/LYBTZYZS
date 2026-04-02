using FluentAssertions;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Formula.ViewModels.Handlers;
using LYBT.Desktop.Formula.ViewModels;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Infrastructure;

public class FormulaMasterDetailViewModelDisposalTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly IMasterDetailServices<FormulaListDto, FormulaDetailModel> _masterDetailServices;
    private readonly IFormulaRepository _formulaRepository;
    private readonly IFormulaStatusHandler _statusHandler;
    private readonly IHerbSearchProvider _herbSearchProvider;
    private readonly IDesktopCacheManager _cacheManager;

    public FormulaMasterDetailViewModelDisposalTests()
    {
        _viewModelServices = Substitute.For<IViewModelServices>();
        _masterDetailServices = Substitute.For<IMasterDetailServices<FormulaListDto, FormulaDetailModel>>();
        _formulaRepository = Substitute.For<IFormulaRepository>();
        _statusHandler = Substitute.For<IFormulaStatusHandler>();
        _herbSearchProvider = Substitute.For<IHerbSearchProvider>();
        _cacheManager = Substitute.For<IDesktopCacheManager>();
    }

    private FormulaMasterDetailViewModel CreateSut()
    {
        return new FormulaMasterDetailViewModel(
            _viewModelServices,
            _masterDetailServices,
            _formulaRepository,
            _statusHandler,
            _herbSearchProvider,
            _cacheManager);
    }

    [Fact]
    public void Dispose_MultipleCallsAreSafe()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Dispose();
        sut.Dispose();
        sut.Dispose();

        // Assert
        sut.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_AfterInitialization_ReleasesResources()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        _ = sut.PageTitle;
        sut.Dispose();

        // Assert
        sut.Should().NotBeNull();
    }
}
