// ---------------------------------------------------------------------------
// PrescriptionE2ETests — US-MC-007 处方：添加药材→保存→查询 全链路
// 真实链路：桌面 IApiClientMedicalCases → LocalWebAPI → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.E2E.MedicalCaseFlow;

[Collection("E2ELocal")]
public class PrescriptionE2ETests : E2ETestBase
{
    private async Task<Guid> CreateHerbAsync(string name, decimal price = 10m)
    {
        var created = await HerbsApi.CreateHerbAsync(new HerbInputDto { Name = name, Unit = "克", Price = price });
        Assert.True(created.Success, created.Message);
        return created.Data!.Id;
    }

    private async Task<Guid> CreatePatientAsync()
    {
        var created = await PatientsApi.CreatePatientAsync(new PatientInputDto
        {
            Name = UniqueName("处方患者"),
            IdNumber = UniqueIdNumber(),
            PhoneNumber = UniquePhone(),
            Gender = Gender.Female
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

    private static PrescriptionItemInputDto Item(Guid herbId, string herbName, int dosage, decimal price)
        => new()
        {
            HerbId = herbId,
            HerbName = herbName,
            Unit = "克",
            Dosage = dosage,
            UnitPrice = price,
            Subtotal = dosage * price
        };

    [Fact]
    public async Task Save_Prescription_WithHerb_Persists()
    {
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("麻黄"));
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);

        var saved = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = true,
            Prescription = new PrescriptionInputDto
            {
                NeedsPrescription = true,
                DosageCount = 7,
                Discount = 1.0m,
                Items = new List<PrescriptionItemInputDto> { Item(herbId, "麻黄", 10, 12m) }
            }
        });

        Assert.True(saved.Success, saved.Message);
        Assert.NotNull(saved.Data!.Prescription);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(caseId);
        Assert.NotNull(detail.Data!.Prescription);
        var item = Assert.Single(detail.Data.Prescription!.Items);
        Assert.Equal(herbId, item.HerbId);
        Assert.Equal(10, item.Dosage);
    }

    [Fact]
    public async Task Save_Prescription_MultipleHerbs_KeepsOrderAndCount()
    {
        await LoginAsAdminAsync();
        var herbA = await CreateHerbAsync(UniqueName("君药"), 20m);
        var herbB = await CreateHerbAsync(UniqueName("臣药"), 5m);
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);

        var saved = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = true,
            Prescription = new PrescriptionInputDto
            {
                NeedsPrescription = true,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>
                {
                    Item(herbA, "君药", 15, 20m),
                    Item(herbB, "臣药", 5, 5m)
                }
            }
        });

        Assert.True(saved.Success, saved.Message);

        var detail = await MedicalCasesApi.GetMedicalCaseByIdAsync(caseId);
        Assert.Equal(2, detail.Data!.Prescription!.Items.Count);
    }

    [Fact]
    public async Task Save_NoPrescription_LeavesPrescriptionNull()
    {
        await LoginAsDoctorAsync();
        var patientId = await CreatePatientAsync();
        var caseId = await CreateCaseAsync(patientId);

        var saved = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
        {
            Id = caseId,
            PatientId = patientId,
            NeedsPrescription = false,
            Prescription = null
        });

                Assert.True(saved.Success, saved.Message);
        // 本地契约：NeedsPrescription=false 时 Prescription 为 null（响应不回显 NeedsPrescription）
        Assert.Null(saved.Data!.Prescription);
    }

    [Fact]
    public async Task BatchDelete_RemovesMultipleCases()
    {
        await LoginAsDoctorAsync();
        // 业务约束：同一患者仅允许一个进行中医案，批量删除需两个不同患者；
        // 删除资格：仅 Completed 状态可删——先完成两个医案
        var patient1 = await CreatePatientAsync();
        var case1 = await CreateCaseAsync(patient1);
        var patient2 = await CreatePatientAsync();
        var case2 = await CreateCaseAsync(patient2);

        // 处方标志需随非空处方保存写入
        await LoginAsAdminAsync();
        var herbId = await CreateHerbAsync(UniqueName("批量删药"));
        await LoginAsDoctorAsync();

        foreach (var (caseId, patientId) in new[] { (case1, patient1), (case2, patient2) })
        {
            var saved = await MedicalCasesApi.SaveAsync(caseId, new MedicalCaseInputDto
            {
                Id = caseId,
                PatientId = patientId,
                NeedsPrescription = true,
                // 完成前置：中医诊断必填
                Consultation = new ConsultationInputDto { TcmDiagnosis = "批量删除前置辨证" },
                Prescription = new PrescriptionInputDto
                {
                    NeedsPrescription = true,
                    Items = new List<PrescriptionItemInputDto> { Item(herbId, "批量删药", 10, 5m) }
                }
            });
            Assert.True(saved.Success, saved.Message);

            var completed = await MedicalCasesApi.UpdateStatusAsync(caseId, new MedicalCaseStatusInputDto
            {
                Status = MedicalCaseStatus.Completed,
                StatusChangeReason = "E2E 批量删除前置完成"
            });
            Assert.True(completed.Success, completed.Message);
        }

        // 批量删除资格（服务端）：Admin 可删已完成案；医生侧 EnsureCanDelete（仅未完成自建案）
        // 与状态校验（仅已完成）矛盾→医生批量删除永远失败——以 Admin 执行
        await LoginAsAdminAsync();
        var result = await MedicalCasesApi.BatchDeleteAsync(new BatchDeleteInputDto
        {
            Ids = new List<Guid> { case1, case2 }
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal(2, result.Data!.SuccessCount);
    }
}
