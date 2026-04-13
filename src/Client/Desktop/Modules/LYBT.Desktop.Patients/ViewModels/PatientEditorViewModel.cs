using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 子 VM - 患者编辑 (编辑真源)
/// OpenSpec: frontend-architecture-unification
///
/// 封装 PatientEditContext，提供 DTO 初始化和数据提取
/// 替代手动字段映射和 CopyToXxx 模式
/// </summary>
public partial class PatientEditorViewModel : ObservableObject
{
    private PatientEditContext _patient = PatientEditContext.CreateNew();

    /// <summary>患者编辑上下文 (XAML 绑定目标)</summary>
    public PatientEditContext Patient
    {
        get => _patient;
        set => SetProperty(ref _patient, value);
    }

    /// <summary>是否已修改 (脏数据标记)</summary>
    public bool IsDirty { get; private set; }

    /// <summary>性别选项 (静态)</summary>
    public static IEnumerable<Gender> GenderOptions => Enum.GetValues<Gender>();

    /// <summary>
    /// 从 DTO 初始化 (查看/编辑已有患者)
    /// </summary>
    public void InitializeFromDto(PatientDetailDto dto)
    {
        var context = new PatientEditContext
        {
            Id = dto.Id,
            Name = dto.Name,
            PinYinCode = dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.Name),
            Gender = dto.Gender,
            BirthDate = dto.BirthDate,
            IdNumber = dto.IdNumber,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            Status = dto.Status
        };

        Patient = context;
        IsDirty = false;
        Patient.PropertyChanged += OnPatientPropertyChanged;
    }

    /// <summary>
    /// 初始化为新患者 (新建场景)
    /// </summary>
    public void InitializeForNewCase()
    {
        Patient = PatientEditContext.CreateNew();
        IsDirty = false;
        Patient.PropertyChanged += OnPatientPropertyChanged;
    }

    /// <summary>
    /// 提取编辑数据为 PatientInputDto (用于保存)
    /// </summary>
    public PatientInputDto GetPatientData()
    {
        return new PatientInputDto
        {
            Id = Patient.Id,
            Name = Patient.Name.Trim(),
            PinYinCode = Patient.PinYinCode?.Trim(),
            Gender = Patient.Gender,
            BirthDate = Patient.BirthDate,
            IdNumber = Patient.IdNumber?.Trim(),
            PhoneNumber = Patient.PhoneNumber?.Trim(),
            Address = Patient.Address?.Trim()
        };
    }

    /// <summary>验证编辑内容</summary>
    public bool Validate()
    {
        return Patient.ValidateAll();
    }

    /// <summary>重置编辑状态</summary>
    public void Reset()
    {
        Patient.PropertyChanged -= OnPatientPropertyChanged;
        Patient = PatientEditContext.CreateNew();
        IsDirty = false;
    }

    private void OnPatientPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
    }
}
