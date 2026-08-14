using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 处方药材项共享映射器（DTO ↔ Model 双向，纯属性复制）。
/// 抽取自 MedicalCaseDetailModelMapper / PrescriptionMapper 的完全重复实现
/// （mapper-chain-audit H1——两处 14 字段逐字段映射逐行一致）。
/// </summary>
public static class PrescriptionItemMapper
{
    /// <summary>
    /// 将 PrescriptionItemDto 映射为 PrescriptionItemModel（处方药材项）。
    /// </summary>
    public static PrescriptionItemModel ToModel(PrescriptionItemDto dto)
    {
        return new PrescriptionItemModel
        {
            Id = dto.Id,
            PrescriptionId = dto.PrescriptionId,
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Unit = dto.Unit,
            UnitPrice = dto.UnitPrice,
            Dosage = dto.Dosage,
            TotalPrice = dto.TotalPrice,
            TotalWeight = dto.TotalWeight,
            Subtotal = dto.Subtotal,
            Usage = dto.Usage,
            DecocteMethod = dto.DecocteMethod,
            Role = dto.Role,
            Remark = dto.Remark,
        };
    }

    /// <summary>
    /// 将 PrescriptionItemModel 映射为 PrescriptionItemDto（供打印/展示等只读输出）。
    /// </summary>
    public static PrescriptionItemDto ToDto(PrescriptionItemModel model)
    {
        return new PrescriptionItemDto
        {
            Id = model.Id,
            PrescriptionId = model.PrescriptionId,
            HerbId = model.HerbId,
            HerbName = model.HerbName,
            Unit = model.Unit,
            UnitPrice = model.UnitPrice,
            Dosage = model.Dosage,
            TotalPrice = model.TotalPrice,
            TotalWeight = model.TotalWeight,
            Subtotal = model.Subtotal,
            Usage = model.Usage,
            DecocteMethod = model.DecocteMethod,
            Role = model.Role,
            Remark = model.Remark,
        };
    }
}
