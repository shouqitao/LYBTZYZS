using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Registrations.Models.Items;

/// <summary>
/// 挂号编辑上下文 - 创建挂号时使用
/// 支持编辑→取消→恢复
/// D3: 对齐 User/Patient EditContext——基类 ObservableObject → ValidatableModelBase，
/// 获得 INotifyDataErrorInfo + ValidateAll 验证能力；PatientName/DoctorName 加必填验证。
/// </summary>
public class RegistrationEditContext : ValidatableModelBase
{
    private Guid _id;
    private Guid _patientId;
    private string _patientName = string.Empty;
    private Guid _doctorId;
    private string _doctorName = string.Empty;
    private RegistrationSource _source;
    private decimal _registrationFee;
    private string? _remark;

    /// <summary>挂号ID</summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>患者ID</summary>
    public Guid PatientId
    {
        get => _patientId;
        set => SetProperty(ref _patientId, value);
    }

    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    public string PatientName
    {
        get => _patientName;
        set => SetPropertyAndValidate(ref _patientName, value);
    }

    /// <summary>医生ID</summary>
    public Guid DoctorId
    {
        get => _doctorId;
        set => SetProperty(ref _doctorId, value);
    }

    /// <summary>医生姓名</summary>
    [Required(ErrorMessage = "医生姓名不能为空")]
    public string DoctorName
    {
        get => _doctorName;
        set => SetPropertyAndValidate(ref _doctorName, value);
    }

    /// <summary>挂号来源</summary>
    public RegistrationSource Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    /// <summary>挂号费</summary>
    public decimal RegistrationFee
    {
        get => _registrationFee;
        set => SetProperty(ref _registrationFee, value);
    }

    /// <summary>备注</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    /// <summary>创建空上下文</summary>
    public static RegistrationEditContext CreateNew()
    {
        return new RegistrationEditContext
        {
            Source = RegistrationSource.Receptionist
        };
    }

    /// <summary>转换为 InputDto 用于 API 调用</summary>
    public RegistrationInputDto ToInputDto()
    {
        return new RegistrationInputDto
        {
            PatientId = PatientId,
            PatientName = PatientName,
            DoctorId = DoctorId,
            DoctorName = DoctorName,
            Source = Source,
            RegistrationFee = RegistrationFee,
            Remark = Remark
        };
    }
}
