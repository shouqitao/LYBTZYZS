namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds MedicalCase-related payloads (Create + Update with Consultation/Prescription).
/// </summary>
public sealed class MedicalCaseBuilder
{
    private Guid _patientId;
    private Guid _userId;
    private Guid? _registrationId;
    private string? _remark;

    public static MedicalCaseBuilder Default() => new();

    public MedicalCaseBuilder WithRegistration(Guid registrationId)
    {
        _registrationId = registrationId; return this;
    }

    public MedicalCaseBuilder ForPatient(Guid patientId)
    {
        _patientId = patientId; return this;
    }

    public MedicalCaseBuilder WithDoctor(Guid userId)
    {
        _userId = userId; return this;
    }

    public MedicalCaseBuilder WithRemark(string remark)
    {
        _remark = remark; return this;
    }

    public object BuildCreate() => new
    {
        PatientId = _patientId,
        UserId = _userId,
        RegistrationId = _registrationId,
        Remark = _remark
    };

    /// <summary>Build aggregate update payload (Consultation + optional Prescription).</summary>
    public static object BuildUpdate(
        Guid caseId,
        Guid patientId = default,
        Guid userId = default,
        object? consultation = null,
        object? prescription = null,
        bool? needsPrescription = null,
        string? editReason = null) => new
    {
        Id = caseId,
        PatientId = patientId,
        UserId = userId,
        Consultation = consultation,
        Prescription = prescription,
        NeedsPrescription = needsPrescription,
        EditReason = editReason
    };

    public static object BuildConsultation(
        string? tcmDiagnosis = "风寒感冒",
        string? presentIllness = "患者近日受凉",
        string? tongueDiagnosis = "舌淡红苔薄白",
        string? pulseDiagnosis = "脉浮紧") => new
    {
        TcmDiagnosis = tcmDiagnosis,
        PresentIllness = presentIllness,
        TongueDiagnosis = tongueDiagnosis,
        PulseDiagnosis = pulseDiagnosis
    };

    public static object BuildPrescription(
        List<object>? items = null,
        int dosageCount = 7,
        string? usage = "日一剂，水煎服",
        string? advice = null,
        Guid medicalCaseId = default) => new
    {
        MedicalCaseId = medicalCaseId,
        DosageCount = dosageCount,
        Usage = usage,
        Advice = advice,
        NeedsPrescription = true,
        Items = items ?? []
    };

    public static object BuildPrescriptionItem(
        Guid herbId, string herbName,
        int dosage, string unit = "克",
        decimal unitPrice = 10.0m) => new
    {
        HerbId = herbId,
        HerbName = herbName,
        Dosage = dosage,
        Unit = unit,
        UnitPrice = unitPrice,
        Subtotal = unitPrice * dosage
    };
}
