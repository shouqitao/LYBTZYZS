using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop._Infrastructure.Builders;

/// <summary>
/// 患者数据构建器 - 简化版
/// 使用 Fluent API 模式创建测试用的患者数据
/// </summary>
public class PatientBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "测试患者";
    private string _idNumber = "110101199001011234";
    private string? _phoneNumber;
    private string? _address;
    private DateTime? _birthDate = new(1990, 1, 1);
    private Gender _gender = Gender.Male;
    private string? _allergyHistory;
    private string? _medicalHistory;

    public static PatientBuilder Create() => new();

    public PatientBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PatientBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public PatientBuilder WithIdNumber(string idNumber)
    {
        _idNumber = idNumber;
        return this;
    }

    public PatientBuilder WithPhoneNumber(string? phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public PatientBuilder WithAddress(string? address)
    {
        _address = address;
        return this;
    }

    public PatientBuilder WithBirthDate(DateTime? birthDate)
    {
        _birthDate = birthDate;
        return this;
    }

    public PatientBuilder WithGender(Gender gender)
    {
        _gender = gender;
        return this;
    }

    public PatientBuilder WithAllergyHistory(string? allergyHistory)
    {
        _allergyHistory = allergyHistory;
        return this;
    }

    public PatientBuilder WithMedicalHistory(string? medicalHistory)
    {
        _medicalHistory = medicalHistory;
        return this;
    }

    /// <summary>
    /// 构建 PatientInputDto (用于创建/更新)
    /// </summary>
    public PatientInputDto BuildInputDto() => new()
    {
        Name = _name,
        
        PhoneNumber = _phoneNumber,
        Address = _address,
        BirthDate = _birthDate,
        Gender = _gender,
        AllergyHistory = _allergyHistory,
        MedicalHistory = _medicalHistory
    };

    /// <summary>
    /// 构建 PatientDetailDto
    /// </summary>
    public PatientDetailDto BuildDetailDto() => new()
    {
        Id = _id,
        Name = _name,
        Age = _birthDate.HasValue ? DateTime.Now.Year - _birthDate.Value.Year : null,
        PhoneNumber = _phoneNumber,
        Address = _address,
        BirthDate = _birthDate,
        Gender = _gender,
        AllergyHistory = _allergyHistory,
        MedicalHistory = _medicalHistory,
        CreatedAt = DateTime.UtcNow,
        Status = CommonStatus.Enabled
    };

    /// <summary>
    /// 构建 PatientListDto
    /// </summary>
    public PatientListDto BuildListDto() => new()
    {
        Id = _id,
        Name = _name,
        Age = _birthDate.HasValue ? DateTime.Now.Year - _birthDate.Value.Year : null,
        PhoneNumber = _phoneNumber,
        Gender = _gender,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// 预置：成年男性患者
    /// </summary>
    public static PatientBuilder AdultMale() => Create()
        .WithName("张三")
        .WithIdNumber("110101199001011234")
        .WithGender(Gender.Male)
        .WithBirthDate(new DateTime(1990, 1, 1));

    /// <summary>
    /// 预置：成年女性患者
    /// </summary>
    public static PatientBuilder AdultFemale() => Create()
        .WithName("李四")
        .WithIdNumber("110101199002022345")
        .WithGender(Gender.Female)
        .WithBirthDate(new DateTime(1990, 2, 2));

    /// <summary>
    /// 预置：儿童患者
    /// </summary>
    public static PatientBuilder Child() => Create()
        .WithName("王小明")
        .WithIdNumber("11010120200101001X")
        .WithGender(Gender.Male)
        .WithBirthDate(new DateTime(2020, 1, 1));

    /// <summary>
    /// 预置：有过敏史患者
    /// </summary>
    public static PatientBuilder WithAllergicHistory() => Create()
        .WithName("过敏患者")
        .WithIdNumber("110101199003033456")
        .WithAllergyHistory("青霉素过敏; 花粉过敏")
        .WithMedicalHistory("过敏性鼻炎");
}
