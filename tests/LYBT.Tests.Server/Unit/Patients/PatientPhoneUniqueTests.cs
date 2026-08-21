using FluentAssertions;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 患者电话唯一约束守卫（PATIENT-PHONE-UNIQUE-FIX: 真机同电话可重复创建——
/// 需求 US-PAT-003/004「PhoneNumber 唯一，重复返回 409」。根因：Handler 查重错误码
/// 用 PatientNotFound（404 语义）且映射 400——需求 409；BatchImport 无电话查重）
/// </summary>
public class PatientPhoneUniqueTests
{
    [Fact]
    public async Task Create_WhenPhoneExists_ReturnsPhoneDuplicate409()
    {
        var repo = new FakePatientRepository { PhoneExists = true };

        var handler = new CreatePatientCommandHandler(repo);
        var result = await handler.Handle(
            new CreatePatientCommand(
                new PatientInputDto
                {
                    Name = "张三",
                    Gender = Gender.Male,
                    PhoneNumber = "13800138000"
                },
                Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.PatientPhoneDuplicate,
            "电话唯一冲突必须用 PatientPhoneDuplicate（映射 409），而非 PatientNotFound（404）");
    }

    [Fact]
    public async Task Update_WhenPhoneExistsOnOtherPatient_ReturnsPhoneDuplicate409()
    {
        var repo = new FakePatientRepository
        {
            ExistingPatient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "李四",
                Gender = Gender.Male,
                PhoneNumber = "13900139000"
            },
            PhoneExists = true // 排除自身外有同电话患者
        };

        var handler = new UpdatePatientCommandHandler(repo);
        var result = await handler.Handle(
            new UpdatePatientCommand(
                repo.ExistingPatient.Id,
                new PatientInputDto
                {
                    Name = "李四",
                    Gender = Gender.Male,
                    PhoneNumber = "13800138000"
                },
                Guid.NewGuid(),
                UserRole.Admin // P1-9: Admin 可通过所有权检查——测试电话唯一分支
            ),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.PatientPhoneDuplicate);
    }

    [Fact]
    public async Task BatchImport_WhenPhoneDuplicatesWithinBatch_MarksRowFailed()
    {
        var repo = new FakePatientRepository();

        var handler = new BatchImportPatientsCommandHandler(repo);
        var result = await handler.Handle(
            new BatchImportPatientsCommand(
                new List<PatientInputDto>
                {
                    new() { Name = "患者甲", Gender = Gender.Male, PhoneNumber = "13800138000" },
                    new() { Name = "患者乙", Gender = Gender.Male, PhoneNumber = "13800138000" }
                },
                DuplicateStrategy.Skip,
                Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FailureCount.Should().Be(1, "同批重复电话行应失败");
        result.Value.SuccessCount.Should().Be(1);
        result.Value.Failures.Should().Contain(f => f.FieldName == "PhoneNumber");
    }

    [Fact]
    public async Task BatchImport_WhenPhoneExistsInSystem_MarksRowFailed()
    {
        var repo = new FakePatientRepository { PhoneExists = true };

        var handler = new BatchImportPatientsCommandHandler(repo);
        var result = await handler.Handle(
            new BatchImportPatientsCommand(
                new List<PatientInputDto>
                {
                    new() { Name = "患者丙", Gender = Gender.Male, PhoneNumber = "13800138000" }
                },
                DuplicateStrategy.Skip,
                Guid.NewGuid()),
            CancellationToken.None);

        result.Value!.FailureCount.Should().Be(1);
        result.Value.Failures.Should().Contain(f => f.FailureReason.Contains("系统已有患者"));
    }

    [Fact]
    public async Task Create_WhenPinYinCodeMissing_AutoGeneratesFromName()
    {
        // 真机同类：API 直调未传 PinYinCode → null → 拼音搜索 0 条（PATIENT-PHONE-UNIQUE-FIX 同类排查）
        var repo = new FakePatientRepository();
        var handler = new CreatePatientCommandHandler(repo);
        var result = await handler.Handle(
            new CreatePatientCommand(
                new PatientInputDto { Name = "张三", Gender = Gender.Male },
                Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PinYinCode.Should().Be("ZS", "服务端应自动生成拼音码（对齐药材 B-03 先例）");
    }

    [Fact]
    public async Task Create_WhenPinYinCodeProvided_KeepsProvidedValue()
    {
        var repo = new FakePatientRepository();
        var handler = new CreatePatientCommandHandler(repo);
        var result = await handler.Handle(
            new CreatePatientCommand(
                new PatientInputDto { Name = "张三", Gender = Gender.Male, PinYinCode = "custom" },
                Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PinYinCode.Should().Be("custom");
    }

    /// <summary>手写 fake（AntiMock 惯例——不引 mock 库）</summary>
    private sealed class FakePatientRepository : IPatientRepository
    {
        public bool PhoneExists { get; set; }
        public Patient? ExistingPatient { get; set; }

        public Task<bool> ExistsByPhoneAsync(string phoneNumber, Guid? excludeId = null, CancellationToken ct = default)
            => Task.FromResult(PhoneExists);

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(ExistingPatient);

        // 其余方法仅满足接口签名（本测试路径不触发）
        public Task<Patient> AddAsync(Patient entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task<Patient> UpdateAsync(Patient entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
#pragma warning disable CS0618
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
#pragma warning restore CS0618
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(id, ct);
        public Task<bool> RestoreAsync(Guid id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> HardDeleteAsync(Patient entity, CancellationToken ct = default) => Task.FromResult(true);
        public Task<PagedResult<Patient>> GetPagedAsync(int page, int pageSize, string? keyword, CommonStatus? status, CancellationToken ct)
            => Task.FromResult(new PagedResult<Patient>());
        public Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<Patient?> GetExactByNameAsync(string name, CancellationToken ct = default) => Task.FromResult<Patient?>(null);
        public Task<Patient?> GetByIdNumberAsync(string idNumber, CancellationToken ct) => Task.FromResult<Patient?>(null);
        public Task<Patient?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken ct) => Task.FromResult<Patient?>(null);
    }
}
