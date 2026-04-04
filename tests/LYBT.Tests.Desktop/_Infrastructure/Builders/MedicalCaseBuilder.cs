using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

/// <summary>
/// 医案数据构建器
/// 使用 Fluent API 模式创建测试用的医案数据
/// </summary>
public class MedicalCaseBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _patientId;
    private Guid _userId;
    private Guid? _registrationId;
    private string? _remark;
    private ConsultationInputDto? _consultation;
    private PrescriptionInputDto? _prescription;
    private bool? _needsPrescription = true;

    public static MedicalCaseBuilder Create() => new();

    public MedicalCaseBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public MedicalCaseBuilder WithPatientId(Guid patientId)
    {
        _patientId = patientId;
        return this;
    }

    public MedicalCaseBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public MedicalCaseBuilder WithRegistrationId(Guid? registrationId)
    {
        _registrationId = registrationId;
        return this;
    }

    public MedicalCaseBuilder WithRemark(string? remark)
    {
        _remark = remark;
        return this;
    }

    public MedicalCaseBuilder WithConsultation(ConsultationInputDto? consultation)
    {
        _consultation = consultation;
        return this;
    }

    public MedicalCaseBuilder WithPrescription(PrescriptionInputDto? prescription)
    {
        _prescription = prescription;
        return this;
    }

    public MedicalCaseBuilder WithNeedsPrescription(bool? needsPrescription)
    {
        _needsPrescription = needsPrescription;
        return this;
    }

    /// <summary>
    /// 构建 MedicalCaseInputDto (用于创建/更新)
    /// </summary>
    public MedicalCaseInputDto BuildInputDto() => new()
    {
        Id = _id,
        PatientId = _patientId,
        UserId = _userId,
        RegistrationId = _registrationId,
        Remark = _remark,
        Consultation = _consultation,
        Prescription = _prescription,
        NeedsPrescription = _needsPrescription
    };

    /// <summary>
    /// 预置：简单医案（仅必填字段）
    /// </summary>
    public static MedicalCaseBuilder Simple(Guid patientId, Guid userId) => Create()
        .WithPatientId(patientId)
        .WithUserId(userId);

    /// <summary>
    /// 预置：完整医案（含诊断和处方）
    /// </summary>
    public static MedicalCaseBuilder Complete(Guid patientId, Guid userId) => Create()
        .WithPatientId(patientId)
        .WithUserId(userId)
        .WithConsultation(new ConsultationInputDto
        {
            PresentIllness = "头痛发热3天",
            TcmDiagnosis = "风热感冒"
        })
        .WithPrescription(new PrescriptionInputDto
        {
            NeedsPrescription = true,
            DosageCount = 7,
            Usage = "水煎服，每日一剂",
            Items = new List<PrescriptionItemInputDto>()
        });

    /// <summary>
    /// 预置：无处方医案
    /// </summary>
    public static MedicalCaseBuilder WithoutPrescription(Guid patientId, Guid userId) => Create()
        .WithPatientId(patientId)
        .WithUserId(userId)
        .WithNeedsPrescription(false)
        .WithConsultation(new ConsultationInputDto
        {
            PresentIllness = "体检",
            TcmDiagnosis = "健康"
        });
}
