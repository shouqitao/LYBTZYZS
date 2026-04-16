using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// Patient boundary and validation tests.
/// Tests edge cases, validation rules, and error handling.
/// </summary>
[Collection("Integration")]
public class PatientBoundaryTests : IntegrationTestBase
{
    private static int _counter;

    public PatientBoundaryTests(IntegrationFixture fixture) : base(fixture) { }

    private async Task<PatientRepository> CreateRepositoryAsync()
    {
        var (_, api) = await LoginAsDoctorWithApiAsync<IPatientApi>();
        return new PatientRepository(api, NullLogger<PatientRepository>.Instance);
    }

    private static PatientInputDto MakePatient(string name, string phoneSuffix)
    {
        var seq = Interlocked.Increment(ref _counter);
        return new PatientInputDto
        {
            Name = name,
            Gender = Gender.Male,
            IdNumber = $"11010119800101{seq:D4}",
            PhoneNumber = $"138{phoneSuffix}",
            Address = "北京市朝阳区测试路1号"
        };
    }

    [Fact]
    public async Task CreatePatient_EmptyName_ReturnsValidationError()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = MakePatient("", "00000001");

        // Act
        var act = () => ds.CreateAsync(input);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePatient_InvalidPhoneFormat_ReturnsValidationError()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = MakePatient("张三", "invalid");
        input.PhoneNumber = "123"; // Invalid format

        // Act
        var act = () => ds.CreateAsync(input);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePatient_DuplicatePhone_ReturnsConflict()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var phone = $"138{Guid.NewGuid():N}".Substring(0, 11);
        var input1 = MakePatient("张三", phone.Substring(3));
        var input2 = MakePatient("李四", phone.Substring(3));
        
        // First creation succeeds
        await ds.CreateAsync(input1);

        // Act - Second creation with same phone
        var act = () => ds.CreateAsync(input2);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetPatient_NotFound_Returns404()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => ds.GetByIdAsync(nonExistentId);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePatient_NotFound_Returns404()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = MakePatient("张三", "00000002");
        input.Id = Guid.NewGuid();

        // Act
        var act = () => ds.UpdateAsync(input);

        // Assert
        var ex = await act.Should().ThrowAsync<Refit.ApiException>();
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePatient_NotFound_Returns404()
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
    public async Task CreatePatient_ConcurrentDuplicatePhone_OnlyOneSucceeds()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var phone = $"138{Guid.NewGuid():N}".Substring(0, 11);
        var input = MakePatient("并发测试", phone.Substring(3));
        
        // Act - Multiple concurrent creations
        var tasks = Enumerable.Range(0, 5).Select(_ => 
            ds.CreateAsync(MakePatient($"User{_}", phone.Substring(3)))
                .ContinueWith(t => t.IsCompletedSuccessfully, TaskContinuationOptions.ExecuteSynchronously)
        );
        
        var results = await Task.WhenAll(tasks);

        // Assert - Only one should succeed
        results.Count(r => r).Should().Be(1);
    }
}
