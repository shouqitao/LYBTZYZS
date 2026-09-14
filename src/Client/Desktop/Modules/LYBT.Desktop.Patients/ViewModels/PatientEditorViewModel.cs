using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Patients.Mappers;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.ViewModels;

/// <summary>
/// 子 VM - 患者编辑 (编辑真源)
///
/// 封装 PatientEditContext，提供 DTO 初始化和数据提取
/// D2: 改用 Mapperly PatientMapper，消除手写字段映射
/// </summary>
public partial class PatientEditorViewModel : EditorViewModelBase<PatientEditContext>
{
    private readonly PatientMapper _mapper;

    /// <summary>患者编辑上下文 (XAML 绑定目标)——[ObservableProperty] 源生成属性 Patient</summary>
    [ObservableProperty]
    private PatientEditContext _patient = PatientEditContext.CreateNew();

    /// <summary>
    /// 构造函数
    /// </summary>
    public PatientEditorViewModel(PatientMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
    /// D2: 改用 Mapperly ToEditContext（保留 PinYinCode 回退行为）
    /// </summary>
    public void InitializeFromDto(PatientDetailDto dto)
    {
        Patient = _mapper.ToEditContext(dto);
        IsDirty = false;
        SubscribeContext();
    }

    /// <summary>
    /// 提取编辑数据为 PatientInputDto (用于保存)
    /// D2: 改用 Mapperly ToInputDto（保留 Trim 行为）
    /// </summary>
    public PatientInputDto GetPatientData()
    {
        return _mapper.ToInputDto(Patient);
    }
}
