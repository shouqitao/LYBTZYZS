using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者查询服务实现 - UltraThink双层架构简化版
/// 职责：查询和搜索操作
/// </summary>
public class PatientQueryService(ILogger<PatientQueryService> logger) : IPatientQueryService
{
    private readonly ILogger<PatientQueryService> _logger = logger;

    /// <summary>
    /// 分页查询患者
    /// </summary>
    public Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
    {
        var emptyResult = new PagedResult<PatientDto>
        {
            Items = new List<PatientDto>(),
            TotalCount = 0
        };
        
        return Task.FromResult(ServiceResult<PagedResult<PatientDto>>.Success(emptyResult));
    }

    /// <summary>
    /// 根据ID获取患者
    /// </summary>
    public Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        return Task.FromResult(ServiceResult<PatientDto>.Failure("简单诊所版本暂不支持患者查询"));
    }

    /// <summary>
    /// 搜索患者
    /// </summary>
    public Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
    {
        var emptyList = new List<PatientDto>();
        return Task.FromResult(ServiceResult<List<PatientDto>>.Success(emptyList));
    }

    /// <summary>
    /// 获取患者统计
    /// </summary>
    public Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
    {
        var stats = new PatientStatisticsDto();
        return Task.FromResult(ServiceResult<PatientStatisticsDto>.Success(stats));
    }
}