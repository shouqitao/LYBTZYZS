using AutoMapper;
using FluentAssertions;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Services;

/// <summary>
/// ConsultationQueryService 单元测试
/// Issue #864 - Phase 2.4: Consultation 模块测试
/// </summary>
public class ConsultationQueryServiceTests
{
    private readonly Mock<IConsultationRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<ConsultationQueryService>> _mockLogger;
    private readonly ConsultationQueryService _sut;

    public ConsultationQueryServiceTests()
    {
        _mockRepository = new Mock<IConsultationRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<ConsultationQueryService>>();
        _sut = new ConsultationQueryService(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPagedConsultationsAsync_WithDefaultParameters_ReturnsEmptyResult()
    {
        // Arrange
        var searchDto = new LYBT.Shared.Models.Contracts.Consultation.ConsultationSearchDto
        {
            PageIndex = 1,
            PageSize = 20
        };

        // Act
        var result = await _sut.GetPagedConsultationsAsync(searchDto);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetConsultationByIdAsync_WithExistingId_ReturnsDto()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var consultation = new LYBT.Entities.Consultation.Consultation
        {
            Id = consultationId,
            ChiefComplaint = "测试主诉"
        };

        var consultationDto = new LYBT.Shared.Models.Contracts.Consultation.ConsultationDto
        {
            Id = consultationId,
            ChiefComplaint = "测试主诉"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(consultationId))
            .ReturnsAsync(consultation);
        _mockMapper.Setup(x => x.Map<LYBT.Shared.Models.Contracts.Consultation.ConsultationDto>(consultation))
            .Returns(consultationDto);

        // Act
        var result = await _sut.GetConsultationByIdAsync(consultationId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(consultationId);
        result.ChiefComplaint.Should().Be("测试主诉");
    }

    [Fact]
    public async Task GetConsultationByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var consultationId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByIdAsync(consultationId))
            .ReturnsAsync((LYBT.Entities.Consultation.Consultation?)null);

        // Act
        var result = await _sut.GetConsultationByIdAsync(consultationId);

        // Assert
        result.Should().BeNull();
    }

}
