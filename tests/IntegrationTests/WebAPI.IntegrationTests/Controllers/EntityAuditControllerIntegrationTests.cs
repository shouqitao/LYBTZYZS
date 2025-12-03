using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Entities.Common;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers;

/// <summary>
/// EntityAuditController 集成测试
/// Issue #2249: 添加审计系统单元测试
/// OpenSpec: add-global-audit-system
/// </summary>
public class EntityAuditControllerIntegrationTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;
    private Guid _testPatientId;
    private Guid _testOperatorId;

    public EntityAuditControllerIntegrationTests(ITestOutputHelper output) : base()
    {
        _output = output;
    }

    protected override void SeedBasicTestData(AppDbContext context)
    {
        base.SeedBasicTestData(context);

        // 创建测试患者
        _testPatientId = Guid.NewGuid();
        _testOperatorId = Guid.NewGuid();

        var patient = new Patient
        {
            Id = _testPatientId,
            Name = "审计测试患者",
            Gender = Gender.Male,
            PhoneNumber = "13800138000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
        context.Set<Patient>().Add(patient);

        // 创建测试审计日志
        for (int i = 0; i < 5; i++)
        {
            var auditLog = new EntityAuditLog
            {
                Id = Guid.NewGuid(),
                EntityType = "Patient",
                EntityId = _testPatientId,
                OperatorId = _testOperatorId,
                OperatorName = $"测试操作者{i}",
                OperatorRole = UserRole.Doctor,
                OperationType = AuditOperationType.Update,
                ChangedFields = "[\"Name\"]",
                OldValues = "{\"Name\":\"旧名称\"}",
                NewValues = "{\"Name\":\"新名称\"}",
                Reason = $"测试原因{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            };
            context.Set<EntityAuditLog>().Add(auditLog);
        }

        context.SaveChanges();
        _output?.WriteLine($"Created test patient: {_testPatientId} with 5 audit logs");
    }

    #region GetLogs 测试

    [Fact]
    public async Task GetLogs_WithValidEntityType_ShouldReturnAuditLogs()
    {
        // Arrange
        _output.WriteLine($"Testing GetLogs for Patient: {_testPatientId}");

        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/Patient/{_testPatientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(5);

        _output.WriteLine($"Retrieved {result.Data.Items.Count} audit logs");
    }

    [Fact]
    public async Task GetLogs_WithInvalidEntityType_ShouldReturnValidationError()
    {
        // Arrange
        var invalidEntityType = "InvalidType";

        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/{invalidEntityType}/{Guid.NewGuid()}");

        // Assert - API返回400 BadRequest对于无效实体类型更符合RESTful规范
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("不支持的实体类型");

        _output.WriteLine($"Validation error: {result.Message}");
    }

    [Fact]
    public async Task GetLogs_WithInvalidPagination_ShouldReturnValidationError()
    {
        // Arrange - page=0 is invalid

        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/Patient/{_testPatientId}?page=0&pageSize=10");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("页码和页大小参数无效");

        _output.WriteLine($"Validation error: {result.Message}");
    }

    [Fact]
    public async Task GetLogs_WithPageSizeExceedingMax_ShouldReturnValidationError()
    {
        // Arrange - pageSize=200 exceeds max of 100

        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/Patient/{_testPatientId}?page=1&pageSize=200");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("页码和页大小参数无效");
    }

    [Fact]
    public async Task GetLogs_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var page = 2;
        var pageSize = 2;

        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/Patient/{_testPatientId}?page={page}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(5);
        result.Data.CurrentPage.Should().Be(2);
        result.Data.PageSize.Should().Be(2);

        _output.WriteLine($"Page {page}: {result.Data.Items.Count} items, Total: {result.Data.TotalCount}");
    }

    [Fact]
    public async Task GetLogs_WithNonExistentEntity_ShouldReturnEmptyResult()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/Patient/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    #endregion

    #region 快捷方法测试

    [Fact]
    public async Task GetPatientLogs_ShouldReturnAuditLogs()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/patients/{_testPatientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPrescriptionLogs_WithNonExistent_ShouldReturnEmpty()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/prescriptions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHerbLogs_WithNonExistent_ShouldReturnEmpty()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/herbs/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFormulaLogs_WithNonExistent_ShouldReturnEmpty()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/formulas/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserLogs_WithNonExistent_ShouldReturnEmpty()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConsultationLogs_WithNonExistent_ShouldReturnEmpty()
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/consultations/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    #endregion

    #region 大小写不敏感测试

    [Theory]
    [InlineData("patient")]
    [InlineData("Patient")]
    [InlineData("PATIENT")]
    public async Task GetLogs_WithDifferentCasing_ShouldWork(string entityType)
    {
        // Act
        var response = await Client.GetAsync($"/api/v1/EntityAudit/{entityType}/{_testPatientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<EntityAuditLogDto>>>();
        result!.Success.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(5);

        _output.WriteLine($"EntityType '{entityType}' returned {result.Data.Items.Count} logs");
    }

    #endregion
}
