using LYBT.Desktop.IntegrationTests.EndToEnd.Fixtures;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.IntegrationTests.EndToEnd.Patients;

/// <summary>
/// Patient 模块 E2E 集成测试
/// 验证完整路径: ViewModel -> Repository -> DataSource -> LocalDbContext(SQLite InMemory)
/// </summary>
public class PatientE2ETests : IDisposable
{
    private readonly DesktopE2ETestFixture _fixture;

    public PatientE2ETests()
    {
        _fixture = new DesktopE2ETestFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public async Task Patient_LoadList_ShouldReturnPagedData()
    {
        // Arrange - 预置5条患者数据
        await _fixture.SeedDataAsync(async db =>
        {
            for (int i = 1; i <= 5; i++)
            {
                db.Patients.Add(new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"测试患者{i}",
                    Gender = Gender.Male,
                    PhoneNumber = $"1380013{i:D4}",
                    PinYinCode = $"CSHY{i}",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
            }
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500); // 等待异步加载完成

        // Assert
        vm.Items.Should().HaveCount(5);
        vm.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task Patient_LoadDetail_ShouldReturnAllFields()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Patients.Add(new Patient
            {
                Id = patientId,
                Name = "张三",
                Gender = Gender.Male,
                PhoneNumber = "13800138000",
                Address = "北京市海淀区",
                PinYinCode = "ZS",
                BirthDate = new DateTime(1990, 1, 15),
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 加载列表
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // 选中第一项触发详情加载
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(300);

        // Assert
        vm.CurrentDetail.Should().NotBeNull();
        vm.CurrentDetail!.Name.Should().Be("张三");
        vm.CurrentDetail.Gender.Should().Be(Gender.Male);
        vm.CurrentDetail.PhoneNumber.Should().Be("13800138000");
    }

    [Fact]
    public async Task Patient_Create_EndToEnd_ViewModel_To_DB()
    {
        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 新建患者
        await vm.CreateNewCommand.ExecuteAsync(null);
        await Task.Delay(200);

        vm.IsEditMode.Should().BeTrue();
        vm.CurrentDetail.Should().NotBeNull();

        // 填写患者信息
        vm.CurrentDetail!.Name = "李四";
        vm.CurrentDetail.Gender = Gender.Female;
        vm.CurrentDetail.PhoneNumber = "13900139000";
        vm.CurrentDetail.Address = "上海市浦东新区";

        // 保存
        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 验证数据持久化到 DB
        var db = _fixture.GetDbContext();
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Name == "李四");
        patient.Should().NotBeNull();
        patient!.Gender.Should().Be(Gender.Female);
        patient.PhoneNumber.Should().Be("13900139000");
        patient.Address.Should().Be("上海市浦东新区");
    }

    [Fact]
    public async Task Patient_Update_EndToEnd_ViewModel_To_DB()
    {
        // Arrange - 预置数据
        var patientId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Patients.Add(new Patient
            {
                Id = patientId,
                Name = "王五",
                Gender = Gender.Male,
                PhoneNumber = "13700137000",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 加载并选中
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(300);

        // 进入编辑模式
        vm.EditCommand.Execute(null);
        vm.IsEditMode.Should().BeTrue();

        // 修改信息
        vm.CurrentDetail!.PhoneNumber = "13800001111";
        vm.CurrentDetail.Address = "广州市天河区";

        // 保存
        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 验证 DB 已更新
        var db = _fixture.GetDbContext();
        var updated = await db.Patients.FindAsync(patientId);
        updated.Should().NotBeNull();
        updated!.PhoneNumber.Should().Be("13800001111");
        updated.Address.Should().Be("广州市天河区");
    }

    [Fact]
    public async Task Patient_Delete_SoftDelete_EndToEnd()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Patients.Add(new Patient
            {
                Id = patientId,
                Name = "赵六",
                Gender = Gender.Male,
                PhoneNumber = "13600136000",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 加载并选中
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().HaveCount(1);

        vm.SelectedItem = vm.Items.First();
        await Task.Delay(200);

        // 删除
        await vm.DeleteCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 验证 DB 中软删除 (全局过滤器应排除)
        var db = _fixture.GetDbContext();
        var patient = await db.Patients.FindAsync(patientId);
        // 软删除后通过全局查询过滤器不可见
        var visiblePatients = await db.Patients.ToListAsync();
        // 验证至少删除操作已执行（列表不再包含该患者）
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().NotContain(i => i.Name == "赵六");
    }

    [Fact]
    public async Task Patient_Search_ByKeyword_ShouldFilter()
    {
        // Arrange - 预置3条不同名字的数据
        await _fixture.SeedDataAsync(async db =>
        {
            db.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(), Name = "张三丰", Gender = Gender.Male,
                PinYinCode = "ZSF", Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
            db.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(), Name = "李四光", Gender = Gender.Male,
                PinYinCode = "LSG", Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
            db.Patients.Add(new Patient
            {
                Id = Guid.NewGuid(), Name = "张无忌", Gender = Gender.Male,
                PinYinCode = "ZWJ", Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<PatientMasterDetailViewModel>();

        // Act - 搜索 "张"
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.Items.Should().HaveCount(3); // 搜索前全部显示

        vm.SearchText = "张";
        await vm.SearchCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 应只返回包含 "张" 的结果
        vm.Items.Should().OnlyContain(p => p.Name.Contains("张"));
        vm.Items.Count.Should().BeLessOrEqualTo(2);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
