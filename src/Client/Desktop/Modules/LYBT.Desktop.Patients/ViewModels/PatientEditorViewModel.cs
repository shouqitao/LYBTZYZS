using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Utilities.Text;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 子 VM - 患者编辑 (编辑真源)
///
/// 封装 PatientEditContext，提供 DTO 初始化和数据提取
/// 替代手动字段映射和 CopyToXxx 模式
/// </summary>
public partial class PatientEditorViewModel : EditorViewModelBase<PatientEditContext>
{
    private PatientEditContext _patient = PatientEditContext.CreateNew();

    /// <summary>患者编辑上下文 (XAML 绑定目标)</summary>
    public PatientEditContext Patient
    {
        get => _patient;
        set => SetProperty(ref _patient, value);
    }

    /// <summary>性别选项 (静态)</summary>
    public static IEnumerable<Gender> GenderOptions => Enum.GetValues<Gender>();

    protected override PatientEditContext Context
    {
        get => Patient;
        set => Patient = value;
    }

    protected override PatientEditContext CreateNewContext() => PatientEditContext.CreateNew();

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
            Status = dto.Status
        };

        Patient = context;
        IsDirty = false;
        SubscribeContext();
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
            PhoneNumber = Patient.PhoneNumber?.Trim()
        };
    }
}
