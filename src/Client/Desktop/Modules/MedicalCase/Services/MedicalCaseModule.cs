using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Desktop.MedicalCase.Interfaces;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// MedicalCase模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 简化版：仅支持后端实际的基础操作
/// </summary>
public class MedicalCaseModule(
    IMedicalCaseQueryService queryService,
    IMedicalCaseBusinessService businessService) : IMedicalCaseModule, IDisposable
{
    private readonly IMedicalCaseQueryService _queryService = queryService;
    private readonly IMedicalCaseBusinessService _businessService = businessService;

    #region 基础查询操作 - 对应简化接口

    /// <summary>
    /// 根据ID获取医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <summary>
    /// 分页查询医案
    /// </summary>
    public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        => await _queryService.GetPagedAsync(query);

    /// <summary>
    /// 根据患者ID获取医案列表
    /// </summary>
    public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.GetByPatientIdAsync(patientId);

    /// <summary>
    /// 获取患者活跃医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto?>> GetActiveByPatientIdAsync(Guid patientId)
        => await _queryService.GetActiveByPatientIdAsync(patientId);

    #endregion

    #region 基础业务操作 - 对应简化接口

    /// <summary>
    /// 创建医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        => await _businessService.CreateAsync(dto);

    /// <summary>
    /// 更新医案
    /// </summary>
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseDetailDto dto)
        => await _businessService.UpdateAsync(id, dto);

    /// <summary>
    /// 删除医案
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteAsync(id);

    /// <summary>
    /// 开始医案
    /// </summary>
    public async Task<ServiceResult<bool>> StartAsync(Guid id)
        => await _businessService.StartAsync(id);

    /// <summary>
    /// 完成医案
    /// </summary>
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id)
        => await _businessService.CompleteAsync(id);

    /// <summary>
    /// 取消医案
    /// </summary>
    public async Task<ServiceResult<bool>> CancelAsync(Guid id)
        => await _businessService.CancelAsync(id);

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 简化版本无需特殊清理
        GC.SuppressFinalize(this);
    }

    #endregion
}