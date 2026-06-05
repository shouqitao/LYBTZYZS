using LYBT.Desktop.Patients.Repositories;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Integration._Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace LYBT.Tests.Integration.Flows;

/// <summary>
/// Patient CRUD flow integration tests.
/// Tests the full chain: PatientRepository -> IApiClient -> Server PatientController -> SQL Server.
/// </summary>
[Collection("Integration")]
public class PatientFlowTests : IntegrationTestBase
{
    private static int _counter;

    public PatientFlowTests(IntegrationFixture fixture) : base(fixture) { }

    private async Task<PatientRepository> CreateRepositoryAsync()
    {
        var client = await LoginAsDoctorAsync();
        var apiClient = TestApiClient.Create(client);
        return new PatientRepository(apiClient, NullLogger<PatientRepository>.Instance);
    }

    /// <summary>
    /// Creates a valid PatientInputDto with all required fields populated.
    /// Required by FluentValidation: Name, IdNumber, PhoneNumber, Address.
    /// </summary>
    private static PatientInputDto MakePatient(string name, Gender gender = Gender.Male)
    {
        var seq = Interlocked.Increment(ref _counter);
        return new PatientInputDto
        {
            Name = name,
            Gender = gender,
            IdNumber = $"11010119800101{seq:D4}",
            PhoneNumber = $"138{seq:D8}",
            Address = "北京市朝阳区测试路1号"
        };
    }

    [Fact]
    public async Task CreateAndRetrieve_Patient_Succeeds()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var input = MakePatient("张三");

        // Act
        var created = await ds.CreateAsync(input);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be("张三");

        // Retrieve
        var retrieved = await ds.GetByIdAsync(created.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("张三");
        retrieved.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public async Task UpdatePatient_ChangesFields()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var created = await ds.CreateAsync(MakePatient("李四", Gender.Female));

        // Act
        var updateInput = MakePatient("李四改名", Gender.Female);
        updateInput.Id = created.Id;
        updateInput.Address = "北京市西城区新地址";
        var updated = await ds.UpdateAsync(updateInput);

        // Assert
        updated.Name.Should().Be("李四改名");
    }

    [Fact]
    public async Task DeletePatient_SoftDeletes()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var created = await ds.CreateAsync(MakePatient("王五删除"));

        // Act
        var deleted = await ds.DeleteAsync(created.Id);

        // Assert
        deleted.Should().BeTrue();

        // Verify soft-deleted: server returns 404 for deleted records,
        // which Refit throws as ApiException. The DataSource re-throws.
        var act = () => ds.GetByIdAsync(created.Id);
        await act.Should().ThrowAsync<Refit.ApiException>();
    }

    [Fact]
    public async Task GetPaged_ReturnsPatientList()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        await ds.CreateAsync(MakePatient("分页患者A"));
        await ds.CreateAsync(MakePatient("分页患者B", Gender.Female));

        // Act
        var result = await ds.GetPagedAsync(1, 20);

        // Assert
        result.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Search_ByKeyword_FindsPatient()
    {
        // Arrange
        var ds = await CreateRepositoryAsync();
        var uniqueName = $"搜索测试_{Guid.NewGuid().ToString("N")[..6]}";
        await ds.CreateAsync(MakePatient(uniqueName));

        // Act
        var results = await ds.SearchAsync(uniqueName);

        // Assert
        results.Should().ContainSingle(p => p.Name == uniqueName);
    }
}
