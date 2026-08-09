using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Registrations.Models;

/// <summary>
/// 挂号详情模型 - Master-Detail模式使用
/// 遵循 DTO 不暴露到 UI 层原则
/// </summary>
public class RegistrationDetailModel : ValidatableModelBase
{
    private Guid _id;
    private Guid _patientId;
    private string _patientName = string.Empty;
    private Guid _doctorId;
    private string _doctorName = string.Empty;
    private Guid? _medicalCaseId;
    private RegistrationSource _source;
    private int _queueNumber;
    private decimal _registrationFee;
    private RegistrationStatus _status;
    private string? _remark;
    private DateTime _createdAt;
    private DateTime? _updatedAt;

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
    public string PatientName
    {
        get => _patientName;
        set => SetProperty(ref _patientName, value);
    }

    /// <summary>医生ID</summary>
    public Guid DoctorId
    {
        get => _doctorId;
        set => SetProperty(ref _doctorId, value);
    }

    /// <summary>医生姓名</summary>
    public string DoctorName
    {
        get => _doctorName;
        set => SetProperty(ref _doctorName, value);
    }

    /// <summary>关联医案ID</summary>
    public Guid? MedicalCaseId
    {
        get => _medicalCaseId;
        set => SetProperty(ref _medicalCaseId, value);
    }

    /// <summary>挂号来源</summary>
    public RegistrationSource Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    /// <summary>排队号</summary>
    public int QueueNumber
    {
        get => _queueNumber;
        set => SetProperty(ref _queueNumber, value);
    }

    /// <summary>挂号费</summary>
    public decimal RegistrationFee
    {
        get => _registrationFee;
        set => SetProperty(ref _registrationFee, value);
    }

    /// <summary>挂号状态</summary>
    public RegistrationStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    /// <summary>备注</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    /// <summary>从 DTO 初始化</summary>
    public void InitializeFromDto(RegistrationDetailDto dto)
    {
        Id = dto.Id;
        PatientId = dto.PatientId;
        PatientName = dto.PatientName;
        DoctorId = dto.DoctorId;
        DoctorName = dto.DoctorName;
        MedicalCaseId = dto.MedicalCaseId;
        Source = dto.Source;
        QueueNumber = dto.QueueNumber;
        RegistrationFee = dto.RegistrationFee;
        Status = dto.Status;
        Remark = dto.Remark;
        CreatedAt = dto.CreatedAt;
        UpdatedAt = dto.UpdatedAt;
    }

    /// <summary>转换为 InputDto</summary>
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

    /// <summary>从 DTO 创建模型</summary>
    public static RegistrationDetailModel FromDto(RegistrationDetailDto dto)
    {
        var model = new RegistrationDetailModel();
        model.InitializeFromDto(dto);
        return model;
    }
}
