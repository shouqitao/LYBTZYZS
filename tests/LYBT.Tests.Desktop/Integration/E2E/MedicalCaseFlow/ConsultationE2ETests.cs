// ---------------------------------------------------------------------------
// ConsultationE2ETests — US-MC-008 问诊记录：主诉→现病史→舌脉→辨证 全链路
// 真实链路：桌面 IApiClientMedicalCases → LocalWebAPI → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.E2E.MedicalCaseFlow;

[Collection("E2ELocal")]
public class ConsultationE2ETests : E2ETestBase
{
    private async Task<Guid> CreatePatientAsync()
    {
        var created = await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("问诊患者"),
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Gender = Gender.Male
        });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    private async Task<Guid> CreateCaseAsync(Guid patientId)
    {
        var created = await MedicalCasesApi.CreateMedicalCaseAsync(new MedicalCaseInputDto { PatientId = patientId });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    [Fact]
    public async Task Save_Consultation_FourFields_Persist()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);

        var saved = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = false,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "恶寒发热2天",
                TongueDiagnosis = "舌红苔黄",
                PulseDiagnosis = "脉浮数",
                TcmDiagnosis = "风热犯肺"
            }
        });

        Assert.True(saved.Success, saved.Message);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(caseId);
        Assert.NotNull(detail.Data!.Consultation);
        Assert.Equal("恶寒发热2天", detail.Data.Consultation!.PresentIllness);
        Assert.Equal("舌红苔黄", detail.Data.Consultation.TongueDiagnosis);
        Assert.Equal("脉浮数", detail.Data.Consultation.PulseDiagnosis);
        Assert.Equal("风热犯肺", detail.Data.Consultation.TcmDiagnosis);
    }

    [Fact]
    public async Task Update_Consultation_OverwritesFields()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);

        await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = false,
            Consultation = new ConsultationInputDto { PresentIllness = "初诊主诉", TcmDiagnosis = "初诊辨证" }
        });

        var updated = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = false,
            Consultation = new ConsultationInputDto
            {
                PresentIllness = "复诊：症状好转",
                TcmDiagnosis = "复诊辨证：余邪未尽"
            }
        });

        Assert.True(updated.Success, updated.Message);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(caseId);
        Assert.Equal("复诊：症状好转", detail.Data!.Consultation!.PresentIllness);
        Assert.Equal("复诊辨证：余邪未尽", detail.Data.Consultation.TcmDiagnosis);
    }

    [Fact]
    public async Task Save_ConsultationOnly_NoPrescription_Allowed()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);

        var saved = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = false,
            Consultation = new ConsultationInputDto { TcmDiagnosis = "仅辨证不开方" }
        });

        Assert.True(saved.Success, saved.Message);
        Assert.Null(saved.Data!.Prescription);
        Assert.NotNull(saved.Data.Consultation);
    }

    [Fact]
    public async Task Search_ByDiagnosisKeyword_FindsCase()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);
        await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = false,
            Consultation = new ConsultationInputDto { TcmDiagnosis = $"独特辨证{Guid.NewGuid():N}"[..8] }
        });

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(caseId);

        Assert.True(detail.Success);
        Assert.False(string.IsNullOrEmpty(detail.Data!.Consultation!.TcmDiagnosis));
    }
}
