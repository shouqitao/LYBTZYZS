using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Interfaces;

/// <summary>
/// 医案模块接口 - UltraThink双层架构简化版
/// 职责：统一服务入口，纯委托模式
/// </summary>
public interface IMedicalCaseModule : IDisposable
{
    #region 基础查询操作 - 简化版本

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

    #region 基础业务操作 - 简化版本

    /// <summary>
    /// 创建医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto);

    /// <summary>
    /// 更新医案
    /// </summary>
    Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseDetailDto dto);

    /// <summary>
    /// 删除医案
    /// </summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id);

    /// <summary>
    /// 开始医案
    /// </summary>
    Task<ServiceResult<bool>> StartAsync(Guid id);

    /// <summary>
    /// 完成医案
    /// </summary>
    Task<ServiceResult<bool>> CompleteAsync(Guid id);

    /// <summary>
    /// 取消医案
    /// </summary>
    Task<ServiceResult<bool>> CancelAsync(Guid id);

    #endregion
}