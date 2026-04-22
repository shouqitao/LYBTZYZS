using FluentAssertions;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.UserJourneys;

/// <summary>
/// User Journey 框架验证测试
/// 验证测试基础设施是否正常工作
/// </summary>
public class FrameworkVerificationTests : UserJourneyTestBase
{
    public FrameworkVerificationTests(UserJourneyFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SQLiteInMemory_DatabaseShouldBeInitialized()
    {
        // Act
        var canConnect = await DbContext.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue("SQLite InMemory 数据库应该可以连接");
    }

    [Fact]
    public async Task TestDataFactory_CreatePatient_ShouldSaveToDatabase()
    {
        // Arrange
        var patient = TestDataFactory.CreatePatient(name: "测试患者张三");

        // Act
        DbContext.Patients.Add(patient);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedPatient = await DbContext.Patients.FindAsync(patient.Id);
        savedPatient.Should().NotBeNull();
        savedPatient!.Name.Should().Be("测试患者张三");
    }

    [Fact]
    public async Task TestDataFactory_SavePatientAsync_ShouldCreatePatient()
    {
        // Act
        var patient = await TestDataFactory.SavePatientAsync(DbContext);

        // Assert
        patient.Id.Should().NotBe(Guid.Empty);
        var exists = await DbContext.Patients.AnyAsync(p => p.Id == patient.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task TestDataFactory_CreateUser_ShouldSaveToDatabase()
    {
        // Arrange
        var user = TestDataFactory.CreateUser(userName: "testdoctor", realName: "测试医生");

        // Act
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var savedUser = await DbContext.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.UserName.Should().Be("testdoctor");
        savedUser.RealName.Should().Be("测试医生");
    }

    [Fact]
    public async Task TestDataFactory_SaveMedicalCaseAsync_ShouldCreateCompleteCase()
    {
        // Act
        var consultation = TestDataFactory.CreateConsultation(
            presentIllness: "头痛发热",
            tcmDiagnosis: "风热感冒");

        var medicalCase = await TestDataFactory.SaveMedicalCaseAsync(
            DbContext,
            consultation: consultation);

        // Assert
        medicalCase.Id.Should().NotBe(Guid.Empty);

        var savedCase = await DbContext.MedicalCases
            .Include(mc => mc.Consultation)
            .FirstOrDefaultAsync(mc => mc.Id == medicalCase.Id);

        savedCase.Should().NotBeNull();
        savedCase!.Consultation.Should().NotBeNull();
        savedCase.Consultation!.TcmDiagnosis.Should().Be("风热感冒");
    }

    [Fact]
    public void CreateViewModelServicesMock_ShouldReturnConfiguredMock()
    {
        // Act
        var services = CreateViewModelServicesMock();

        // Assert
        services.Should().NotBeNull();
        services.LoggerFactory.Should().NotBeNull();
        services.EventAggregator.Should().NotBeNull();
        services.RegionManager.Should().NotBeNull();
        services.SessionManager.Should().NotBeNull();
    }

    [Fact]
    public void CreateMasterDetailServicesMock_ShouldReturnConfiguredMock()
    {
        // Act
        var services = CreateMasterDetailServicesMock<object, object>();

        // Assert
        services.Should().NotBeNull();
        services.List.Should().NotBeNull();
        services.DetailEditor.Should().NotBeNull();
        services.Dialog.Should().NotBeNull();
        services.Navigation.Should().NotBeNull();
        services.Loading.Should().NotBeNull();
        services.Pagination.Should().NotBeNull();
        services.Search.Should().NotBeNull();
        services.Selection.Should().NotBeNull();
        services.ErrorHandler.Should().NotBeNull();
        services.AsyncExecutor.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetDatabase_ShouldClearAllData()
    {
        // Arrange
        await TestDataFactory.SavePatientAsync(DbContext);
        await TestDataFactory.SaveUserAsync(DbContext);

        var patientCountBefore = await DbContext.Patients.CountAsync();
        var userCountBefore = await DbContext.Users.CountAsync();

        patientCountBefore.Should().BeGreaterThan(0);
        userCountBefore.Should().BeGreaterThan(0);

        // Act
        await ResetDatabaseAsync();

        // Assert
        var patientCountAfter = await DbContext.Patients.CountAsync();
        var userCountAfter = await DbContext.Users.CountAsync();

        patientCountAfter.Should().Be(0);
        userCountAfter.Should().Be(0);
    }

    [Fact]
#pragma warning disable CS1998
    public async Task ServiceProvider_ShouldResolveLocalDbContext()
    {
        // Act
        var context = ServiceProvider.GetService<LocalDbContext>();

        // Assert
        context.Should().NotBeNull();
        context!.Database.IsSqlite().Should().BeTrue();
    }
#pragma warning restore CS1998

    [Fact]
    public void WpfTestHelper_ShouldInitializeWithoutError()
    {
        // Act & Assert - 不应抛出异常
        WpfTestHelper.InitializeWpf();

        // 如果执行到这里，说明初始化成功
        true.Should().BeTrue();
    }
}
