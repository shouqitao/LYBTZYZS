// -----------------------------------------------------------------------
// <copyright file="MedicalCaseCloneMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: simplify-desktop-data-layer - Mapperly克隆映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.MedicalCase.Mappers;

/// <summary>
/// 医案DTO深拷贝映射器 - Mapperly源生成。
/// </summary>
/// <remarks>
/// OpenSpec: simplify-desktop-data-layer - 替代手写Clone方法
/// 用于创建DTO的深拷贝副本，支持变更检测和回滚
/// </remarks>
[Mapper(UseDeepCloning = true)]
public partial class MedicalCaseCloneMapper
{
    /// <summary>
    /// 深拷贝MedicalCaseDetailDto。
    /// </summary>
    /// <param name="source">源对象。</param>
    /// <returns>深拷贝副本。</returns>
    public partial MedicalCaseDetailDto Clone(MedicalCaseDetailDto source);

    /// <summary>
    /// 深拷贝ConsultationDetailDto。
    /// </summary>
    /// <param name="source">源对象。</param>
    /// <returns>深拷贝副本。</returns>
    public partial ConsultationDetailDto Clone(ConsultationDetailDto source);

    /// <summary>
    /// 深拷贝PrescriptionDetailDto。
    /// </summary>
    /// <param name="source">源对象。</param>
    /// <returns>深拷贝副本。</returns>
    public partial PrescriptionDetailDto Clone(PrescriptionDetailDto source);
}
