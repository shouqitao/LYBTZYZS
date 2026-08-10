using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Utilities.Text;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Patients.Mappers;

/// <summary>
/// 患者详情模型映射器 - 编译时生成。
/// D2: 启用 Mapperly——统一 PatientEditorViewModel.InitializeFromDto 手写 DTO→EditContext
/// 与 PatientMasterDetailViewModel.SaveDetailAsync 手写回填，消除重复字段映射。
/// </summary>
/// <remarks>
/// 映射关系：
/// - PatientDetailDto → PatientEditContext (编辑真源初始化)
/// - PatientEditContext → PatientInputDto (保存到API)
/// - PatientDetailDto → PatientDetailModel (保存后回填)
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PatientMapper
{
    /// <summary>
    /// 将PatientDetailDto转换为PatientEditContext（编辑真源）。
    /// D2: 替代 PatientEditorViewModel.InitializeFromDto 手写字段映射（保留 PinYinCode 回退行为）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>编辑上下文。</returns>
    public PatientEditContext ToEditContext(PatientDetailDto dto)
    {
        return new PatientEditContext
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
    }

    /// <summary>
    /// 将PatientEditContext转换为PatientInputDto（保存到API）。
    /// D2: 替代 PatientEditorViewModel.GetPatientData 手写映射（保留 Trim 行为）。
    /// </summary>
    /// <param name="context">编辑上下文。</param>
    /// <returns>InputDTO对象。</returns>
    public PatientInputDto ToInputDto(PatientEditContext context)
    {
        return new PatientInputDto
        {
            Id = context.Id,
            Name = context.Name.Trim(),
            PinYinCode = context.PinYinCode?.Trim(),
            Gender = context.Gender,
            BirthDate = context.BirthDate,
            IdNumber = context.IdNumber?.Trim(),
            PhoneNumber = context.PhoneNumber?.Trim()
        };
    }

    /// <summary>
    /// 将保存后返回的PatientDetailDto应用到已有PatientDetailModel（回填）。
    /// D2: 替代 PatientMasterDetailViewModel.SaveDetailAsync 手写回填。
    /// </summary>
    /// <param name="target">目标 Model（回填目标）。</param>
    /// <param name="source">保存后返回的 DTO。</param>
    public void ApplyToDetailModel(PatientDetailModel target, PatientDetailDto source)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.PinYinCode = source.PinYinCode ?? string.Empty;
        target.Gender = source.Gender;
        target.BirthDate = source.BirthDate;
        target.IdNumber = source.IdNumber;
        target.PhoneNumber = source.PhoneNumber;
        target.Status = source.Status;
    }
}
