// ---------------------------------------------------------------------------
// RegistrationE2ETests — US-REG-001/003 创建挂号→查询→状态流转 全链路
// 真实链路：桌面 IApiClientRegistrations → LocalWebAPI RegistrationsController → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.E2E.RegistrationFlow;

[Collection("E2ELocal")]
public class RegistrationE2ETests : E2ETestBase
{
    private async Task<Guid> CreatePatientAsync()
    {
        var created = await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("挂号患者"),
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Gender = Gender.Unknown
        });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    private RegistrationInputDto NewRegistration(Guid patientId, Guid doctorId, string patientName)
        => new()
        {
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = "医生",
            Source = RegistrationSource.Receptionist,
            RegistrationFee = 10m,
            Remark = "E2E 挂号"
        };

    [Fact]
    public async Task Create_Registration_SetsWaiting()
    {
        await LoginAsReceptionistAsync();
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();

        var created = await RegistrationsApi.CreateAsync(NewRegistration(patientId, doctor.User.Id, "挂号患者A"));

        Assert.True(created.Success, created.Message);
        Assert.NotNull(created.Data);
        Assert.Equal(RegistrationStatus.Waiting, created.Data!.Status);
        Assert.Equal(RegistrationSource.Receptionist, created.Data.Source);
        Assert.True(created.Data.QueueNumber > 0);
        Assert.Equal(10m, created.Data.RegistrationFee);
    }

    [Fact]
    public async Task GetById_ReturnsRegistration()
    {
        await LoginAsReceptionistAsync();
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var created = await RegistrationsApi.CreateAsync(NewRegistration(patientId, doctor.User.Id, "挂号患者B"));
        Assert.True(created.Success);

        var detail = await RegistrationsApi.GetByIdAsync(created.Data!.Id);

        Assert.True(detail.Success);
        Assert.Equal(created.Data.Id, detail.Data!.Id);
        Assert.Equal(patientId, detail.Data.PatientId);
        Assert.Equal(RegistrationStatus.Waiting, detail.Data.Status);
    }

    [Fact]
    public async Task GetQueue_ReturnsWaitingRegistrations()
    {
        await LoginAsReceptionistAsync();
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var created = await RegistrationsApi.CreateAsync(NewRegistration(patientId, doctor.User.Id, "挂号患者C"));
        Assert.True(created.Success);

        var queue = await RegistrationsApi.GetQueueAsync(null);

        Assert.True(queue.Success);
        Assert.Contains(queue.Data!, r => r.Id == created.Data!.Id);
    }

    [Fact]
    public async Task StartVisit_CreatesMedicalCase_AndSetsInProgress()
    {
        await LoginAsReceptionistAsync();
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var created = await RegistrationsApi.CreateAsync(NewRegistration(patientId, doctor.User.Id, "挂号患者D"));
        Assert.True(created.Success);

        var started = await RegistrationsApi.StartVisitAsync(created.Data!.Id);

        Assert.True(started.Success, started.Message);
        Assert.NotEqual(Guid.Empty, started.Data);

        // 挂号流转为 InProgress 且关联医案
        var detail = await RegistrationsApi.GetByIdAsync(created.Data.Id);
        Assert.Equal(RegistrationStatus.InProgress, detail.Data!.Status);
        Assert.Equal(started.Data, detail.Data.MedicalCaseId);
    }

    [Fact]
    public async Task Cancel_WaitingRegistration_Succeeds()
    {
        await LoginAsReceptionistAsync();
        var patientId = await CreatePatientAsync();
        var doctor = await LoginAsDoctorAsync();
        var created = await RegistrationsApi.CreateAsync(NewRegistration(patientId, doctor.User.Id, "挂号患者E"));
        Assert.True(created.Success);

        // 取消仅 Receptionist 可执行——重新切回前台身份
        await LoginAsReceptionistAsync();
        var cancelled = await RegistrationsApi.CancelAsync(created.Data!.Id);

        Assert.True(cancelled.Success, cancelled.Message);

        var detail = await RegistrationsApi.GetByIdAsync(created.Data.Id);
        Assert.Equal(RegistrationStatus.Cancelled, detail.Data!.Status);
    }

    [Fact]
    public async Task GetList_Paged_ReturnsRegistrations()
    {
        await LoginAsReceptionistAsync();
        var doctor = await LoginAsDoctorAsync();
        // 业务约束：同一患者当日仅允许一次待诊挂号——每单用独立患者
        for (var i = 0; i < 3; i++)
        {
            var pid = await CreatePatientAsync();
            var created = await RegistrationsApi.CreateAsync(NewRegistration(pid, doctor.User.Id, $"挂号患者{i}"));
            Assert.True(created.Success, created.Message);
        }

        var list = await RegistrationsApi.GetListAsync(1, 10);

        Assert.True(list.Success);
        Assert.True(list.Data!.TotalCount >= 3);
        Assert.True(list.Data.TotalCount > 0);
    }
}
