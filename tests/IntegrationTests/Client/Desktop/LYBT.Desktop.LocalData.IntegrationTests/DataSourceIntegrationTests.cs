using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.IntegrationTests.Fixtures;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.LocalData.IntegrationTests;

/// <summary>
/// DataSource 端到端集成测试
/// 验证 DI 容器注册 -> DataSource -> LocalDbContext 完整数据流
/// OpenSpec: implement-local-mode Phase 5.2
/// </summary>
public class DataSourceIntegrationTests : IClassFixture<LocalModeTestFixture>
{
    private readonly LocalModeTestFixture _fixture;

    public DataSourceIntegrationTests(LocalModeTestFixture fixture)
    {
        _fixture = fixture;
    }

    #region DI 容器集成测试

    [Fact]
    public void DI_PatientDataSource_CanBeResolved()
    {
        // Arrange & Act
        var serviceProvider = _fixture.CreateServiceProvider();
        // 确保数据库已创建
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        dbContext.Database.EnsureCreated();

        var dataSource = serviceProvider.GetRequiredService<IPatientDataSource>();

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Should().BeOfType<LYBT.Desktop.LocalData.DataSources.LocalPatientDataSource>();
    }

    [Fact]
    public void DI_HerbDataSource_CanBeResolved()
    {
        // Arrange & Act
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IHerbDataSource>();

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Should().BeOfType<LYBT.Desktop.LocalData.DataSources.LocalHerbDataSource>();
    }

    [Fact]
    public void DI_FormulaDataSource_CanBeResolved()
    {
        // Arrange & Act
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IFormulaDataSource>();

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Should().BeOfType<LYBT.Desktop.LocalData.DataSources.LocalFormulaDataSource>();
    }

    [Fact]
    public void DI_UserDataSource_CanBeResolved()
    {
        // Arrange & Act
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IUserDataSource>();

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Should().BeOfType<LYBT.Desktop.LocalData.DataSources.LocalUserDataSource>();
    }

    [Fact]
    public void DI_MedicalCaseDataSource_CanBeResolved()
    {
        // Arrange & Act
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IMedicalCaseDataSource>();

        // Assert
        dataSource.Should().NotBeNull();
        dataSource.Should().BeOfType<LYBT.Desktop.LocalData.DataSources.LocalMedicalCaseDataSource>();
    }

    #endregion

    #region Patient DataSource CRUD 集成测试

    [Fact]
    public async Task PatientDataSource_CRUD_EndToEnd()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IPatientDataSource>();

        // Create
        var patient = new Patient
        {
            Name = "集成测试患者",
            Gender = Gender.Female,
            PhoneNumber = "13800138999",
            BirthDate = new DateTime(1990, 5, 15),
            Address = "测试地址",
            PinYinCode = "JCCSHY"
        };

        var created = await dataSource.CreateAsync(patient);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be("集成测试患者");

        // Read
        var read = await dataSource.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Name.Should().Be("集成测试患者");
        read.Gender.Should().Be(Gender.Female);

        // Update
        read.Name = "更新后的名称";
        read.PhoneNumber = "13900139888";
        var updated = await dataSource.UpdateAsync(read);
        updated.Should().NotBeNull();
        updated.Name.Should().Be("更新后的名称");
        updated.PhoneNumber.Should().Be("13900139888");

        // Delete (Soft)
        var deleteResult = await dataSource.DeleteAsync(created.Id);
        deleteResult.Should().BeTrue();

        // Verify deleted (should return null due to soft delete filter)
        var afterDelete = await dataSource.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull("软删除后应该查不到");

        // Restore
        var restored = await dataSource.RestoreAsync(created.Id);
        restored.Should().NotBeNull();
        restored!.Name.Should().Be("更新后的名称");
    }

    [Fact]
    public async Task PatientDataSource_Paging_ReturnsCorrectPage()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IPatientDataSource>();

        // 创建 15 条测试数据
        for (int i = 1; i <= 15; i++)
        {
            await dataSource.CreateAsync(new Patient
            {
                Name = $"分页测试患者{i:D2}",
                Gender = Gender.Male,
                PhoneNumber = $"138000{i:D5}",
                PinYinCode = $"FYCS{i:D2}"
            });
        }

        // Act - 获取第一页
        var (items, total) = await dataSource.GetPagedAsync(page: 1, pageSize: 10);

        // Assert
        total.Should().Be(15);
        items.Should().HaveCount(10);

        // Act - 获取第二页
        var (page2Items, _) = await dataSource.GetPagedAsync(page: 2, pageSize: 10);
        page2Items.Should().HaveCount(5);
    }

    #endregion

    #region 跨 DataSource 数据隔离测试

    [Fact]
    public async Task DataSources_UsesSameDbContext_DataIsShared()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        dbContext.Database.EnsureCreated();
        var patientDataSource = serviceProvider.GetRequiredService<IPatientDataSource>();

        // Act - 通过 DataSource 创建患者
        var patient = await patientDataSource.CreateAsync(new Patient
        {
            Name = "共享数据测试",
            Gender = Gender.Male,
            PhoneNumber = "13800000001",
            PinYinCode = "GXSJCS"
        });

        // Assert - 通过 DbContext 直接查询验证数据存在
        var dbPatient = await dbContext.Patients.FindAsync(patient.Id);
        dbPatient.Should().NotBeNull();
        dbPatient!.Name.Should().Be("共享数据测试");
    }

    [Fact]
    public async Task MultipleDataSources_SameServiceProvider_ShareDbContext()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<LocalDbContext>();
        dbContext.Database.EnsureCreated();
        var patientDataSource = serviceProvider.GetRequiredService<IPatientDataSource>();
        var herbDataSource = serviceProvider.GetRequiredService<IHerbDataSource>();

        // Act - 通过不同 DataSource 创建数据
        var patient = await patientDataSource.CreateAsync(new Patient
        {
            Name = "多源测试患者",
            Gender = Gender.Male,
            PhoneNumber = "13800000002",
            PinYinCode = "DYCSHY"
        });

        var herb = await herbDataSource.CreateAsync(new LYBT.Entities.Herbs.Herb
        {
            Name = "多源测试中药",
            PinYinCode = "DYCSZY",
            Category = "测试药",
            Price = 10.5m,
            Unit = "g"
        });

        // Assert - 两个数据源创建的数据都可以通过 DbContext 查询到
        var dbPatient = await dbContext.Patients.FindAsync(patient.Id);
        var dbHerb = await dbContext.Herbs.FindAsync(herb.Id);

        dbPatient.Should().NotBeNull();
        dbHerb.Should().NotBeNull();
    }

    #endregion
}
