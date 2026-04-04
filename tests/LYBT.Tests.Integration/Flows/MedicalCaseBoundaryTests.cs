using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// MedicalCase boundary and validation tests.
/// Tests edge cases and error handling for medical cases.
/// </summary>
[Collection("Integration")]
public class MedicalCaseBoundaryTests : IntegrationTestBase
{
    public MedicalCaseBoundaryTests(IntegrationFixture fixture) : base(fixture) { }

    private async Task<MedicalCaseRepository> CreateRepositoryAsync()
    {
        var (_, api) = await LoginAsDoctorWithApiAsync<IMedicalCaseApi>();
        return new MedicalCaseRepository(api, NullLogger<MedicalCaseRepository>.Instance);
    }

    [Fact]
    public async Task CreateMedicalCase_InvalidPatientId_ReturnsError()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(), // Non-existent patient
            UserId = await GetCurrentUserIdAsync()
        };

        // Act
        var act = () => ds.CreateAsync(input);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMedicalCase_NotFound_Returns404()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = new MedicalCaseInputDto
        {
            Id = Guid.NewGuid(), // Non-existent case
            PatientId = await CreateTestPatientAsync(),
            UserId = await GetCurrentUserIdAsync()
        };

        // Act
        var act = () => ds.UpdateAsync(input);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMedicalCase_NotFound_Returns404()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => ds.DeleteAsync(nonExistentId);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMedicalCase_WithConsultation_Succeeds()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var patientId = await CreateTestPatientAsync();
        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = await GetCurrentUserIdAsync(),
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "头痛发热3天",
                TcmDiagnosis = "风热感冒"
            }
        };

        // Act
        var created = await ds.CreateAsync(input);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
    }

    private async Task<Guid> CreateTestPatientAsync()
    {
        // Helper to create a test patient and return its ID
        // Implementation would use PatientRepository
        return Guid.NewGuid(); // Placeholder - would need actual implementation
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        // Helper to get current user ID from login session
        return Guid.NewGuid(); // Placeholder - would need actual implementation
    }
}
