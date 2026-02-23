using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.LocalData.DataSources;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.Integration.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.Integration.LocalMode;

/// <summary>
/// Desktop 本地模式 DataSource 集成测试。
/// 验证 DI容器 -> DataSource -> LocalDbContext -> SQLite 完整数据流。
/// 每个测试使用独立的 ServiceProvider (独立数据库)。
/// </summary>
public class DataSourceIntegrationTests : IClassFixture<DesktopFixture>
{
    private readonly DesktopFixture _fixture;

    public DataSourceIntegrationTests(DesktopFixture fixture)
    {
        _fixture = fixture;
    }

    #region DI 容器解析测试

    [Fact]
    public async Task DI_AllDataSources_CanBeResolved()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();

        // Act & Assert - 5 个 DataSource 全部可解析
        sp.GetRequiredService<IPatientDataSource>().Should().BeOfType<LocalPatientDataSource>();
        sp.GetRequiredService<IHerbDataSource>().Should().BeOfType<LocalHerbDataSource>();
        sp.GetRequiredService<IFormulaDataSource>().Should().BeOfType<LocalFormulaDataSource>();
        sp.GetRequiredService<IMedicalCaseDataSource>().Should().BeOfType<LocalMedicalCaseDataSource>();
        sp.GetRequiredService<IUserDataSource>().Should().BeOfType<LocalUserDataSource>();
    }

    #endregion

    #region Patient DataSource

    [Fact]
    public async Task Patient_CRUD_EndToEnd()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IPatientDataSource>();

        // Create
        var patientInput = new PatientInputDto
        {
            Name = "集成测试患者",
            Gender = Gender.Female,
            PhoneNumber = "13800138999",
            BirthDate = new DateTime(1990, 5, 15),
            Address = "测试地址",
            PinYinCode = "JCCSHY"
        };
        var created = await ds.CreateAsync(patientInput);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be("集成测试患者");

        // Read
        var read = await ds.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Gender.Should().Be(Gender.Female);
        read.PhoneNumber.Should().Be("13800138999");

        // Update
        var updateInput = new PatientInputDto
        {
            Id = read.Id,
            Name = "更新后的名称",
            Gender = read.Gender,
            PhoneNumber = "13900139888",
            BirthDate = read.BirthDate,
            Address = read.Address,
            PinYinCode = read.PinYinCode
        };
        var updated = await ds.UpdateAsync(updateInput);
        updated.Name.Should().Be("更新后的名称");

        // Delete (Soft)
        var deleted = await ds.DeleteAsync(created.Id);
        deleted.Should().BeTrue();

        // 软删除后查不到
        var afterDelete = await ds.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull();

        // Restore
        var restored = await ds.RestoreAsync(created.Id);
        restored.Should().NotBeNull();
        restored!.Name.Should().Be("更新后的名称");
    }

    [Fact]
    public async Task Patient_Paging_ReturnsCorrectPage()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IPatientDataSource>();

        for (int i = 1; i <= 15; i++)
        {
            await ds.CreateAsync(new PatientInputDto
            {
                Name = $"分页测试患者{i:D2}",
                Gender = Gender.Male,
                PinYinCode = $"FYCS{i:D2}"
            });
        }

        // Act
        var (page1, total) = await ds.GetPagedAsync(page: 1, pageSize: 10);
        var (page2, _) = await ds.GetPagedAsync(page: 2, pageSize: 10);

        // Assert
        total.Should().Be(15);
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(5);
    }

    [Fact]
    public async Task Patient_Search_ByNameAndPinYin()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IPatientDataSource>();

        await ds.CreateAsync(new PatientInputDto { Name = "张三丰", PinYinCode = "ZSF" });
        await ds.CreateAsync(new PatientInputDto { Name = "李四光", PinYinCode = "LSG" });
        await ds.CreateAsync(new PatientInputDto { Name = "张无忌", PinYinCode = "ZWJ" });

        // Act - 按关键字搜索
        var (items, total) = await ds.GetPagedAsync(page: 1, pageSize: 10, keyword: "张");

        // Assert
        total.Should().Be(2);
        items.Should().AllSatisfy(p => p.Name.Should().Contain("张"));
    }

    [Fact]
    public async Task Patient_BatchDelete_ShouldSoftDeleteAll()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IPatientDataSource>();

        var p1 = await ds.CreateAsync(new PatientInputDto { Name = "批删1", PinYinCode = "PS1" });
        var p2 = await ds.CreateAsync(new PatientInputDto { Name = "批删2", PinYinCode = "PS2" });
        var p3 = await ds.CreateAsync(new PatientInputDto { Name = "批删3", PinYinCode = "PS3" });

        // Act
        await ds.BatchDeleteAsync(new List<Guid> { p1.Id, p2.Id, p3.Id });

        // Assert
        var (items, total) = await ds.GetPagedAsync(1, 10);
        total.Should().Be(0);
    }

    #endregion

    #region Herb DataSource

    [Fact]
    public async Task Herb_CRUD_EndToEnd()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IHerbDataSource>();

        // Create
        var herbInput = new HerbInputDto
        {
            Name = "黄芪集成测试",
            PinYinCode = "HQJCCS",
            Category = "补益药",
            Unit = "克",
            Price = 3.5m,
            Effect = "补气固表"
        };
        var created = await ds.CreateAsync(herbInput);
        created.Should().NotBeNull();
        created.Name.Should().Be("黄芪集成测试");

        // Read
        var read = await ds.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Price.Should().Be(3.5m);

        // Update
        var updateInput = new HerbInputDto
        {
            Id = read.Id,
            Name = read.Name,
            PinYinCode = read.PinYinCode,
            Category = read.Category,
            Unit = read.Unit,
            Price = 4.0m,
            Effect = "补气固表，利尿托毒"
        };
        var updated = await ds.UpdateAsync(updateInput);
        updated.Effect.Should().Contain("利尿托毒");
        updated.Price.Should().Be(4.0m);

        // Delete (Soft)
        var deleted = await ds.DeleteAsync(created.Id);
        deleted.Should().BeTrue();
        var afterDelete = await ds.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task Herb_ToggleStatus_ShouldSwitchStatus()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IHerbDataSource>();

        var herb = await ds.CreateAsync(new HerbInputDto
        {
            Name = "状态切换测试",
            PinYinCode = "ZTQH",
            Unit = "克"
        });

        // Act
        await ds.ToggleStatusAsync(herb.Id);

        // Assert
        var toggled = await ds.GetByIdAsync(herb.Id);
        toggled!.Status.Should().Be(CommonStatus.Disabled);
    }

    #endregion

    #region Formula DataSource

    [Fact]
    public async Task Formula_CRUD_EndToEnd()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IFormulaDataSource>();

        // Create
        var formulaInput = new FormulaInputDto
        {
            Name = "四君子汤集成测试",
            Effect = "益气健脾",
            Indications = "脾胃气虚证",
        };
        var created = await ds.CreateAsync(formulaInput);
        created.Should().NotBeNull();
        created.Name.Should().Be("四君子汤集成测试");

        // Read
        var read = await ds.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Effect.Should().Be("益气健脾");

        // Update
        var updateInput = new FormulaInputDto
        {
            Id = read.Id,
            Name = read.Name,
            Effect = "益气健脾和胃",
            Indications = read.Indications
        };
        var updated = await ds.UpdateAsync(updateInput);
        updated.Effect.Should().Be("益气健脾和胃");

        // Delete (Soft)
        var deleted = await ds.DeleteAsync(created.Id);
        deleted.Should().BeTrue();
    }

    #endregion

    #region User DataSource

    [Fact]
    public async Task User_CRUD_EndToEnd()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IUserDataSource>();

        // Create
        var userInput = new UserInputDto
        {
            UserName = "testdoctor_crud",
            RealName = "测试医生",
            Role = UserRole.Doctor,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
        };
        var created = await ds.CreateAsync(userInput);
        created.Should().NotBeNull();
        created.UserName.Should().Be("testdoctor_crud");

        // Read
        var read = await ds.GetByIdAsync(created.Id);
        read.Should().NotBeNull();
        read!.Role.Should().Be(UserRole.Doctor);

        // Update
        var updateInput = new UserInputDto
        {
            Id = read.Id,
            UserName = read.UserName,
            RealName = "更新医生名",
            Role = read.Role,
        };
        var updated = await ds.UpdateAsync(updateInput);
        updated.RealName.Should().Be("更新医生名");

        // Delete (Soft)
        var deleted = await ds.DeleteAsync(created.Id);
        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task User_GetByUsername_ShouldReturnCorrectUser()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IUserDataSource>();

        await ds.CreateAsync(new UserInputDto
        {
            UserName = "findme_user",
            RealName = "可查用户",
            Role = UserRole.Doctor,
            Password = "pass123!",
            ConfirmPassword = "pass123!",
        });

        // Act
        var found = await ds.GetByUsernameAsync("findme_user");

        // Assert
        found.Should().NotBeNull();
        found!.RealName.Should().Be("可查用户");
    }

    #endregion

    #region MedicalCase DataSource

    [Fact]
    public async Task MedicalCase_CRUD_EndToEnd()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var ds = sp.GetRequiredService<IMedicalCaseDataSource>();

        // Create
        var mcInput = new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
        };
        var created = await ds.CreateAsync(mcInput);
        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);

        // Read
        var read = await ds.GetByIdAsync(created.Id);
        read.Should().NotBeNull();

        // Complete (use CompleteAsync instead of direct status update)
        var completed = await ds.CompleteAsync(created.Id);
        completed.Should().BeTrue();
        var finalCase = await ds.GetByIdAsync(created.Id);
        finalCase!.CaseStatus.Should().Be(MedicalCaseStatus.Completed);
    }

    #endregion

    #region 跨 DataSource 测试

    [Fact]
    public async Task MultipleDataSources_SameProvider_ShareDatabase()
    {
        // Arrange
        var sp = await _fixture.CreateServiceProviderAsync();
        var patientDs = sp.GetRequiredService<IPatientDataSource>();
        var herbDs = sp.GetRequiredService<IHerbDataSource>();

        // Act
        var patient = await patientDs.CreateAsync(new PatientInputDto
        {
            Name = "多源测试患者",
            Gender = Gender.Male,
            PinYinCode = "DYCSHY"
        });
        var herb = await herbDs.CreateAsync(new HerbInputDto
        {
            Name = "多源测试中药",
            PinYinCode = "DYCSZY",
            Category = "测试药",
            Price = 10.5m,
            Unit = "g"
        });

        // Assert - 通过 DbContext 直接查询验证数据共享
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        var dbPatient = await db.Patients.FindAsync(patient.Id);
        var dbHerb = await db.Herbs.FindAsync(herb.Id);
        dbPatient.Should().NotBeNull();
        dbHerb.Should().NotBeNull();
    }

    #endregion
}
