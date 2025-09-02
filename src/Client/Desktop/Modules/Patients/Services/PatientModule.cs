using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 简化版本：仅支持基础操作
/// </summary>
public class PatientModule(
    IPatientQueryService queryService,
    IPatientBusinessService businessService) : IPatientModule, IDisposable
{
    private readonly IPatientQueryService _queryService = queryService;
    private readonly IPatientBusinessService _businessService = businessService;

    #region 基础查询操作 - 对应简化接口

    /// <summary>
    /// 分页查询患者
    /// </summary>
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
        => await _queryService.GetPagedAsync(query);

    /// <summary>
    /// 根据ID获取患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <summary>
    /// 搜索患者
    /// </summary>
    public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    /// <summary>
    /// 获取患者统计
    /// </summary>
    public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
        => await _queryService.GetStatisticsAsync();

    #endregion

    #region 基础业务操作 - 对应简化接口

    /// <summary>
    /// 创建患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
        => await _businessService.CreateAsync(createDto);

    /// <summary>
    /// 更新患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    /// <summary>
    /// 启用患者
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid patientId)
        => await _businessService.EnableAsync(patientId);

    /// <summary>
    /// 禁用患者
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid patientId)
        => await _businessService.DisableAsync(patientId);

    /// <summary>
    /// 删除患者
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid patientId)
        => await _businessService.DeleteAsync(patientId);

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 简化版本无需特殊清理
        GC.SuppressFinalize(this);
    }

    #endregion
}