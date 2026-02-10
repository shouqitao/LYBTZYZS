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

    #region Herb DataSource CRUD 集成测试

    [Fact]
    public async Task HerbDataSource_CRUD_EndToEnd()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IHerbDataSource>();

        // Create
        var herb = new LYBT.Entities.Herbs.Herb
        {
            Name = "黄芪集成测试",
            PinYinCode = "HQJCCS",
            Category = "补益药",
            Unit = "克",
            Price = 3.5m,
            Effect = "补气固表"
        };

        var created = await dataSource.CreateAsync(herb);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be("黄芪集成测试");

        // Read
        var read = await dataSource.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.PinYinCode.Should().Be("HQJCCS");

        // Update
        read.Effect = "补气固表，利尿托毒";
        var updated = await dataSource.UpdateAsync(read);
        updated.Should().NotBeNull();
        updated.Effect.Should().Contain("利尿托毒");

        // Delete (Soft)
        var deleteResult = await dataSource.DeleteAsync(created.Id);
        deleteResult.Should().BeTrue();

        var afterDelete = await dataSource.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull("软删除后应该查不到");
    }

    #endregion

    #region Formula DataSource CRUD 集成测试

    [Fact]
    public async Task FormulaDataSource_CRUD_EndToEnd()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IFormulaDataSource>();

        // Create
        var formula = new LYBT.Entities.Formulas.Formula
        {
            Name = "四君子汤集成测试",
            Effect = "益气健脾",
            Indication = "脾胃气虚证",
            Status = CommonStatus.Enabled,
        };

        var created = await dataSource.CreateAsync(formula);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);

        // Read
        var read = await dataSource.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Name.Should().Be("四君子汤集成测试");

        // Update
        read.Effect = "益气健脾和胃";
        var updated = await dataSource.UpdateAsync(read);
        updated.Should().NotBeNull();
        updated.Effect.Should().Be("益气健脾和胃");

        // Delete (Soft)
        var deleteResult = await dataSource.DeleteAsync(created.Id);
        deleteResult.Should().BeTrue();
    }

    #endregion

    #region User DataSource CRUD 集成测试

    [Fact]
    public async Task UserDataSource_CRUD_EndToEnd()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IUserDataSource>();

        // Create
        var user = new LYBT.Entities.Users.User
        {
            UserName = "testdoctor_crud",
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
        };

        var created = await dataSource.CreateAsync(user);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.UserName.Should().Be("testdoctor_crud");

        // Read
        var read = await dataSource.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Role.Should().Be(UserRole.Doctor);

        // Update
        read.RealName = "更新医生名";
        var updated = await dataSource.UpdateAsync(read);
        updated.Should().NotBeNull();
        updated.RealName.Should().Be("更新医生名");

        // Delete (Soft)
        var deleteResult = await dataSource.DeleteAsync(created.Id);
        deleteResult.Should().BeTrue();
    }

    #endregion

    #region MedicalCase DataSource CRUD 集成测试

    [Fact]
    public async Task MedicalCaseDataSource_CRUD_EndToEnd()
    {
        // Arrange
        var serviceProvider = _fixture.CreateServiceProvider();
        serviceProvider.GetRequiredService<LocalDbContext>().Database.EnsureCreated();
        var dataSource = serviceProvider.GetRequiredService<IMedicalCaseDataSource>();

        // Create
        var mc = new LYBT.Entities.MedicalCases.MedicalCase
        {
            PatientId = Guid.NewGuid(),
            PatientName = "医案测试患者",
            UserId = Guid.NewGuid(),
            DoctorName = "医案测试医生",
            CaseStatus = MedicalCaseStatus.Active,
        };

        var created = await dataSource.CreateAsync(mc);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);

        // Read
        var read = await dataSource.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.PatientName.Should().Be("医案测试患者");

        // Update
        read.CaseStatus = MedicalCaseStatus.Completed;
        read.CompletedAt = DateTime.UtcNow;
        var updated = await dataSource.UpdateAsync(read);
        updated.Should().NotBeNull();
        updated.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    #endregion
}
