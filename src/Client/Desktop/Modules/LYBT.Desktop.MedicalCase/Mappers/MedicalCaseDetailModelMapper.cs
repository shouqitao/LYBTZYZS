// -----------------------------------------------------------------------
// <copyright file="MedicalCaseDetailModelMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 医案详情模型数据映射器 - 编译时生成。
/// </summary>
/// <remarks>
/// 映射关系：
/// - MedicalCaseDetailDto → MedicalCaseDetailModel (从API加载，需要从嵌套DTO提取字段)
/// - MedicalCaseDetailModel → MedicalCaseInputDto (保存到API)
///
/// 注意：
/// - 诊断信息(PresentIllness, TongueDiagnosis等)来自嵌套的Consultation DTO
/// - 处方信息(HerbCount, DoseCount等)来自嵌套的Prescription DTO
/// - 这些字段需要手动映射，无法使用Mapperly自动生成
/// </remarks>
[Mapper]
public partial class MedicalCaseDetailModelMapper
{
    /// <summary>
    /// 将MedicalCaseDetailDto转换为MedicalCaseDetailModel（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    /// <remarks>
    /// 仅映射基础字段，嵌套DTO中的字段由ToItem方法手动处理。
    /// 忽略计算属性（ConsultationDate, DiagnosisSummary, PrescriptionSummary等）。
    /// 忽略验证相关属性（HasErrors, Errors, HasErrorsDictionary）。
    /// </remarks>
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PatientGender))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PatientAge))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.UserId))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CaseNumber))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.ConsultationId))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PrescriptionId))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CompletedAt))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Diagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PresentIllness))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.HasConsultation))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.HasPrescription))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.CreatedBy))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.PresentIllness))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.TongueDiagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.PulseDiagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.TcmDiagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.HerbCount))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.DoseCount))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.ReferencedFormulas))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.PrescriptionItems))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.ConsultationDate))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.DiagnosisSummary))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.FormulaSource))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.PrescriptionSummary))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.HasPrescriptionItems))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.StatusText))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.HasErrors))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.Errors))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailModel.HasErrorsDictionary))]
    [MapProperty(nameof(MedicalCaseDetailDto.CaseStatus), nameof(MedicalCaseDetailModel.Status))]
    public partial MedicalCaseDetailModel ToItemCore(MedicalCaseDetailDto dto);

    /// <summary>
    /// 将MedicalCaseDetailDto转换为MedicalCaseDetailModel（完整映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    public MedicalCaseDetailModel ToItem(MedicalCaseDetailDto dto)
    {
        var model = ToItemCore(dto);

        // 从嵌套Consultation DTO提取诊断信息
        if (dto.Consultation != null)
        {
            model.PresentIllness = dto.Consultation.PresentIllness;
            model.TongueDiagnosis = dto.Consultation.TongueDiagnosis;
            model.PulseDiagnosis = dto.Consultation.PulseDiagnosis;
            model.TcmDiagnosis = dto.Consultation.TcmDiagnosis;
        }

        // 从嵌套Prescription DTO提取处方信息
        if (dto.Prescription != null)
        {
            model.HerbCount = dto.Prescription.Items?.Count ?? 0;
            model.DoseCount = dto.Prescription.DosageCount;
            model.ReferencedFormulas = dto.Prescription.ReferencedFormulas ?? "自拟方";

            // 填充处方药材列表
            if (dto.Prescription.Items != null)
            {
                model.PrescriptionItems = new ObservableCollection<PrescriptionItemDto>(dto.Prescription.Items);
            }
        }

        return model;
    }

    /// <summary>
    /// 将MedicalCaseDetailModel转换为MedicalCaseInputDto（核心映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    /// <remarks>
    /// MedicalCaseInputDto主要包含Id, PatientId, UserId, Remark, EditReason。
    /// Consultation和Prescription嵌套对象需要单独处理。
    /// </remarks>
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.Id))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.PatientName))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.Status))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.PresentIllness))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.TongueDiagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.PulseDiagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.TcmDiagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.HerbCount))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.DoseCount))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.ReferencedFormulas))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.PrescriptionItems))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.CreatedAt))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.UpdatedAt))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.DoctorName))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.ConsultationDate))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.DiagnosisSummary))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.FormulaSource))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.PrescriptionSummary))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.HasPrescriptionItems))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.StatusText))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.HasErrors))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.Errors))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailModel.HasErrorsDictionary))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.Id))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.UserId))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.EditReason))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.NeedsPrescription))]
    public partial MedicalCaseInputDto ToInputDtoCore(MedicalCaseDetailModel model);

    /// <summary>
    /// 将MedicalCaseDetailModel转换为MedicalCaseInputDto（完整映射）。
    /// </summary>
    /// <param name="model">Model对象。</param>
    /// <returns>InputDTO对象。</returns>
    public MedicalCaseInputDto ToInputDto(MedicalCaseDetailModel model)
    {
        var dto = ToInputDtoCore(model);

        // 设置Id（空Guid转为null表示创建）
        dto.Id = model.Id == Guid.Empty ? null : model.Id;

        return dto;
    }
}
