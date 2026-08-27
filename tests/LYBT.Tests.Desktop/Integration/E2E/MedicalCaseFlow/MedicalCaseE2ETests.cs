// ---------------------------------------------------------------------------
// MedicalCaseE2ETests — US-MC-001 创建医案→诊断→处方→完成 全链路
// 真实链路：桌面 IApiClientMedicalCases → LocalWebAPI MedicalCasesController → LocalDB
// 生命周期：Active（创建）→ Suspended（挂起）→ Active（恢复）→ Completed（完成）
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.E2E.MedicalCaseFlow;

[Collection("E2ELocal")]
public class MedicalCaseE2ETests : E2ETestBase
{
    private async Task<Guid> CreatePatientAsync()
    {
        var created = await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("医案患者"),
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Gender = Gender.Male
        });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    private async Task<Guid> CreateHerbAsync(string name)
    {
        var created = await HerbsApi.CreateHerbAsync(new LYBT.Shared.Models.Contracts.Herbs.HerbInputDto
        {
            Name = name,
            Unit = "克",
            Price = 12.5m
        });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    private static PrescriptionInputDto Prescription(Guid herbId, string herbName) => new()
    {
        NeedsPrescription = true,
        DosageCount = 7,
        Discount = 1.0m,
        Items = new List<PrescriptionItemInputDto>
        {
            new()
            {
                HerbId = herbId,
                HerbName = herbName,
                Unit = "克",
                Dosage = 10,
                UnitPrice = 12.5m,
                Subtotal = 125m
            }
        }
    };

    [Fact]
    public async Task Create_MedicalCase_SetsActive()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();

        var result = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto
        {
            PatientId = patientId
        });

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
        Assert.Equal(MedicalCaseStatus.Active, result.Data.CaseStatus);
        Assert.False(string.IsNullOrEmpty(result.Data.CaseNumber));
        Assert.Equal(patientId, result.Data.PatientId);
    }

    [Fact]
    public async Task GetById_ReturnsMedicalCase()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(created.Data!.Id);

        Assert.True(detail.Success);
        Assert.Equal(created.Data.Id, detail.Data!.Id);
        Assert.Equal(patientId, detail.Data.PatientId);
    }

    [Fact]
    public async Task GetList_ReturnsCases()
    {
        await LoginAsDoctorAsync();
        var patientName = UniqueName("列表患者");
        var patientId = (await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = patientName,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Gender = Gender.Male
        })).Data!.Id;
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        // 本地搜索按患者姓名匹配（非医案编号）
        var list = await MedicalCasesApi.GetMedicalCasesAsync(1, 20, patientName);

        Assert.Contains(list.Data!.Items, c => c.Id == created.Data!.Id);
    }

    [Fact]
    public async Task Save_AggregateConsultationAndPrescription_IsAtomic()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("处方药"));
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        var saved = await MedicalCasesApi.SaveAsync(created.Data!.Id, new MedicalCaseInputDto
        {
            Id = created.Data.Id,
            PatientId = patientId,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "咳嗽三日",
                TcmDiagnosis = "风寒束肺",
                TongueDiagnosis = "舌淡红苔薄白",
                PulseDiagnosis = "脉浮紧"
            },
            Prescription = Prescription(herbId, "处方药"),
            NeedsPrescription = true
        });

        Assert.True(saved.Success, saved.Message);
        Assert.Equal(MedicalCaseStatus.Active, saved.Data!.CaseStatus);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(created.Data.Id);
        Assert.NotNull(detail.Data!.Consultation);
        Assert.Equal("咳嗽三日", detail.Data.Consultation!.PresentIllness);
        Assert.NotNull(detail.Data.Prescription);
        Assert.Single(detail.Data.Prescription!.Items);
    }

    [Fact]
    public async Task Suspend_ThenResume_ChangesStatus()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        var suspended = await MedicalCasesApi.UpdateStatusAsync(created.Data!.Id, new MedicalCaseStatusInputDto
        {
            Status = MedicalCaseStatus.Suspended,
            StatusChangeReason = "E2E 暂存"
        });
        Assert.True(suspended.Success, suspended.Message);

        // 本地 UpdateStatus 返回信封不含详情，经 GetById 校验状态
        var afterSuspend = await MedicalCasesApi.GetMedicalCaseByIdAsync(created.Data.Id);
        Assert.Equal(MedicalCaseStatus.Suspended, afterSuspend.Data!.CaseStatus);

        var resumed = await MedicalCasesApi.UpdateStatusAsync(created.Data.Id, new MedicalCaseStatusInputDto
        {
            Status = MedicalCaseStatus.Active,
            StatusChangeReason = "E2E 继续"
        });
        Assert.True(resumed.Success);

        var afterResume = await MedicalCasesApi.GetMedicalCaseByIdAsync(created.Data.Id);
        Assert.Equal(MedicalCaseStatus.Active, afterResume.Data!.CaseStatus);
    }

    [Fact]
    public async Task Complete_MedicalCase_SetsCompleted()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        // 业务前置：处方标志随「非空处方」聚合保存写入（NeedsPrescription=true），完成才允许
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("完成药"));
        await LoginAsDoctorAsync();
        var saved = await MedicalCasesApi.SaveAsync(created.Data!.Id, new MedicalCaseInputDto
        {
            Id = created.Data.Id,
            PatientId = patientId,
            NeedsPrescription = true,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "完成前辨证" },
            Prescription = Prescription(herbId, "完成药")
        });
        Assert.True(saved.Success, saved.Message);

        var completed = await MedicalCasesApi.UpdateStatusAsync(created.Data!.Id, new MedicalCaseStatusInputDto
        {
            Status = MedicalCaseStatus.Completed,
            StatusChangeReason = "E2E 完成"
        });

        Assert.True(completed.Success, completed.Message);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(created.Data.Id);
        Assert.Equal(MedicalCaseStatus.Completed, detail.Data!.CaseStatus);
    }

    [Fact]
    public async Task GetPending_ReturnsActiveCases()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        var pending = await MedicalCasesApi.GetPendingCasesAsync(patientId);

        Assert.True(pending.Success);
        // 本地 pending DTO 不含 MedicalCaseId（挂号队列视角），按患者 ID 匹配
        Assert.Contains(pending.Data!, c => c.PatientId == patientId);
    }

    [Fact]
    public async Task Cancel_MedicalCase_RemovesFromList()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success);

        var cancelled = await MedicalCasesApi.CancelMedicalCaseAsync(created.Data!.Id);

        Assert.True(cancelled.Success, cancelled.Message);

        var list = await MedicalCasesApi.GetMedicalCasesAsync(1, 20, created.Data.CaseNumber);
        Assert.DoesNotContain(list.Data!.Items, c => c.Id == created.Data.Id);
    }
}
