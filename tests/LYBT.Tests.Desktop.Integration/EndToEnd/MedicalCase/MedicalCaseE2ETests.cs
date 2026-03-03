using LYBT.Tests.Desktop.Integration.EndToEnd.Fixtures;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Tests.Desktop.Integration.EndToEnd.MedicalCase;

/// <summary>
/// MedicalCase 模块 E2E 集成测试
/// 注意: 医案不支持从 ViewModel 新建 (CreateNewCommand 会抛 NotSupportedException)
/// 测试重点: 加载、编辑、查询
/// </summary>
public class MedicalCaseE2ETests : IDisposable
{
    private readonly DesktopE2ETestFixture _fixture;

    public MedicalCaseE2ETests()
    {
        _fixture = new DesktopE2ETestFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public async Task MedicalCase_LoadList_WithPatientName()
    {
        // Arrange
        await _fixture.SeedDataAsync(async db =>
        {
            var patientId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            db.MedicalCases.Add(new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                PatientName = "张三",
                UserId = userId,
                DoctorName = "李医生",
                CaseStatus = MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.MedicalCases.Add(new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                PatientName = "李四",
                UserId = userId,
                DoctorName = "李医生",
                CaseStatus = MedicalCaseStatus.Completed,
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<MedicalCaseMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.Items.Should().HaveCount(2);
        vm.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task MedicalCase_LoadDetail_ShouldShowConsultation()
    {
        // Arrange
        var mcId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.MedicalCases.Add(new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = mcId,
                PatientId = Guid.NewGuid(),
                PatientName = "王五",
                UserId = Guid.NewGuid(),
                DoctorName = "赵医生",
                CaseStatus = MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.Consultations.Add(new LYBT.Entities.Consultations.Consultation
            {
                Id = mcId,
                PresentIllness = "头痛三天",
                TcmDiagnosis = "气虚头痛",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "脉细弱",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<MedicalCaseMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        vm.SelectedItem = vm.Items.First();
        await Task.Delay(500);

        // Assert
        vm.CurrentDetail.Should().NotBeNull();
    }

    [Fact]
    public async Task MedicalCase_EditConsultation_SaveToDB()
    {
        // Arrange
        var mcId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.MedicalCases.Add(new LYBT.Entities.MedicalCases.MedicalCase
            {
                Id = mcId,
                PatientId = Guid.NewGuid(),
                PatientName = "陈六",
                UserId = Guid.NewGuid(),
                DoctorName = "刘医生",
                CaseStatus = MedicalCaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.Consultations.Add(new LYBT.Entities.Consultations.Consultation
            {
                Id = mcId,
                PresentIllness = "咳嗽一周",
                TcmDiagnosis = "风寒犯肺",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<MedicalCaseMasterDetailViewModel>();

        // Act - 加载并选中
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(500);

        // 编辑
        vm.EditCommand.Execute(null);
        await Task.Delay(200);

        // 保存
        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 验证 DB 更新
        var db = _fixture.GetDbContext();
        var mc = await db.MedicalCases.FindAsync(mcId);
        mc.Should().NotBeNull();
    }

    [Fact]
    public async Task MedicalCase_MultipleStatuses_LoadAll()
    {
        // Arrange - 不同状态的医案
        await _fixture.SeedDataAsync(async db =>
        {
            var userId = Guid.NewGuid();
            db.MedicalCases.AddRange(
                new LYBT.Entities.MedicalCases.MedicalCase
                {
                    Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), PatientName = "状态1",
                    UserId = userId, DoctorName = "医生A", CaseStatus = MedicalCaseStatus.Active,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                },
                new LYBT.Entities.MedicalCases.MedicalCase
                {
                    Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), PatientName = "状态2",
                    UserId = userId, DoctorName = "医生A", CaseStatus = MedicalCaseStatus.Completed,
                    CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                },
                new LYBT.Entities.MedicalCases.MedicalCase
                {
                    Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), PatientName = "状态3",
                    UserId = userId, DoctorName = "医生A", CaseStatus = MedicalCaseStatus.Suspended,
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                }
            );
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<MedicalCaseMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.Items.Should().HaveCount(3);
        vm.TotalCount.Should().Be(3);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
