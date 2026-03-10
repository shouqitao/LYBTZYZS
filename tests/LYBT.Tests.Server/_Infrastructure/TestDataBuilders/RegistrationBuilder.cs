namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds RegistrationInputDto payloads for API calls.
/// </summary>
public sealed class RegistrationBuilder
{
    private Guid _patientId;
    private string _patientName = "测试患者";
    private Guid _doctorId;
    private string _doctorName = "测试医生";
    private string? _remark;

    public static RegistrationBuilder Default() => new();

    public RegistrationBuilder ForPatient(Guid patientId, string patientName)
    {
        _patientId = patientId;
        _patientName = patientName;
        return this;
    }

    public RegistrationBuilder WithDoctor(Guid doctorId, string doctorName)
    {
        _doctorId = doctorId;
        _doctorName = doctorName;
        return this;
    }

    public RegistrationBuilder WithRemark(string remark)
    {
        _remark = remark; return this;
    }

    public object Build() => new
    {
        PatientId = _patientId,
        PatientName = _patientName,
        DoctorId = _doctorId,
        DoctorName = _doctorName,
        Remark = _remark
    };
}
