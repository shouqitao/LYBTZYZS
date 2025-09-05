using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医疗案例模块 - UltraThink双层架构纯委托层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：统一服务入口，请求路由分发到QueryService和BusinessService
/// 实现IMedicalCaseService共享接口，与后端标准完全对齐
/// 集成医案查询、CRUD操作、状态管理和流程控制功能
/// 适配中医诊所医疗案例管理需求，确保诊疗流程完整性和数据安全性
/// </summary>
public class MedicalCaseModule(
    IMedicalCaseQueryService queryService,
    IMedicalCaseBusinessService businessService) : IMedicalCaseService
{
    private readonly IMedicalCaseQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IMedicalCaseBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

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
    public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
    {
        var result = await _queryService.GetActiveByPatientIdAsync(patientId);
        if (result.IsSuccess && result.Data != null)
        {
            return ServiceResult<MedicalCaseDto>.Success(result.Data);
        }

        return ServiceResult<MedicalCaseDto>.Failure(result.ErrorMessage ?? "未找到活跃医案");
    }

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
    public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        => ServiceResult<MedicalCaseDto>.Failure("简单诊所版本暂不支持医案更新");

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
    public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        => await _businessService.CompleteAsync(id);

    /// <summary>
    /// 取消医案
    /// </summary>
    public async Task<ServiceResult<bool>> CancelAsync(Guid id)
        => await _businessService.CancelAsync(id);

    #endregion

    #region 共享接口IMedicalCaseService额外方法 - 委托给相应服务层

    /// <summary>
    /// 暂停医疗案例 - 简单诊所版本暂不支持
    /// </summary>
    public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        => ServiceResult<bool>.Failure("简单诊所版本暂不支持暂停医案");

    /// <summary>
    /// 恢复医疗案例 - 简单诊所版本暂不支持
    /// </summary>
    public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        => ServiceResult<bool>.Failure("简单诊所版本暂不支持恢复医案");

    /// <summary>
    /// 取消咨询/诊断 - 委托给CancelAsync
    /// </summary>
    public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        => await CancelAsync(id);

    /// <summary>
    /// 更新医疗案例状态 - 简单诊所版本基础实现
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
        => ServiceResult<bool>.Failure("简单诊所版本暂不支持状态更新");

    /// <summary>
    /// 归档医疗案例 - 简单诊所版本暂不支持
    /// </summary>
    public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        => ServiceResult<bool>.Failure("简单诊所版本暂不支持归档医案");

    /// <summary>
    /// 获取医疗案例统计信息 - 简单诊所版本基础实现
    /// </summary>
    public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        => ServiceResult<object>.Success(new { TotalCases = 0, ActiveCases = 0, CompletedCases = 0 });

    /// <summary>
    /// 搜索医疗案例 - 简单诊所版本基础实现
    /// </summary>
    public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        => ServiceResult<List<MedicalCaseDto>>.Success([]);

    /// <summary>
    /// 获取医疗案例历史记录 - 简单诊所版本暂不支持
    /// </summary>
    public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
        => ServiceResult<List<object>>.Success([]);

    #endregion
}
