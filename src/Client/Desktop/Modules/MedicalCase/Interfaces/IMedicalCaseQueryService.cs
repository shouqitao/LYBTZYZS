using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案查询服务接口 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public interface IMedicalCaseQueryService
{
    #region 基础查询操作 - 简化实现

    /// <summary>
    /// 根据ID获取医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页查询医案
    /// </summary>
    Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query);

    /// <summary>
    /// 根据患者ID获取医案列表
    /// </summary>
    Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

    /// <summary>
    /// 获取患者活跃医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto?>> GetActiveByPatientIdAsync(Guid patientId);

    #endregion
}