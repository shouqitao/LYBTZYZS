using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Server.Infrastructure.TestDataBuilders;

/// <summary>
/// Builds PatientInputDto payloads for API calls.
/// Fluent interface: PatientBuilder.Default().WithName("张三").Build()
///
/// Required fields per PatientInputDtoValidator:
/// - Name (non-empty)
/// - IdNumber (non-empty, 18-char format: ^\d{17}[\dXx]$)
/// - Address (non-empty)
/// </summary>
public sealed class PatientBuilder
{
    private static int _seq;

    private string _name = $"测试患者_{Guid.NewGuid():N}"[..12];
    private Gender _gender = Gender.Male;
    private DateTime? _birthDate = new DateTime(1980, 1, 15);
    private string? _phoneNumber = $"138{Random.Shared.Next(10000000, 99999999)}";
    private string _idNumber = GenerateIdNumber();
    private string _address = "北京市测试区测试路1号";
    private string? _medicalHistory;
    private string? _allergyHistory;

    public static PatientBuilder Default() => new();

    public PatientBuilder WithName(string name) { _name = name; return this; }
    public PatientBuilder WithGender(Gender gender) { _gender = gender; return this; }
    public PatientBuilder WithBirthDate(DateTime? date) { _birthDate = date; return this; }
    public PatientBuilder WithPhone(string phone) { _phoneNumber = phone; return this; }
    public PatientBuilder WithIdNumber(string idNumber) { _idNumber = idNumber; return this; }
    public PatientBuilder WithAddress(string address) { _address = address; return this; }
    public PatientBuilder WithMedicalHistory(string history) { _medicalHistory = history; return this; }
    public PatientBuilder WithAllergyHistory(string allergy) { _allergyHistory = allergy; return this; }

    /// <summary>Build a strongly-typed PatientInputDto to ensure correct serialization.</summary>
    public PatientInputDto Build() => new()
    {
        Name = _name,
        Gender = _gender,
        BirthDate = _birthDate,
        PhoneNumber = _phoneNumber,
        IdNumber = _idNumber,
        Address = _address,
        MedicalHistory = _medicalHistory,
        AllergyHistory = _allergyHistory
    };

    /// <summary>
    /// Generate a valid 18-char ID number matching regex ^\d{17}[\dXx]$.
    /// Format: 6(area) + 8(YYYYMMDD) + 3(seq) + 1(check) = 18 chars.
    /// Example: 110101198001150010
    /// </summary>
    private static string GenerateIdNumber()
    {
        var seq = Interlocked.Increment(ref _seq);
        var day = (seq % 28) + 1;
        return $"110101198001{day:D2}{seq % 1000:D3}0";
    }
}
