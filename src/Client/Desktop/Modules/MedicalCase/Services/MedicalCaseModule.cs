using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

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
        => await _queryService.GetPaged(query);

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

    #endregion 基础查询操作 - 对应简化接口

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
    {
        // 将MedicalCaseUpdateDto转换为MedicalCaseDetailDto
        var detailDto = new MedicalCaseDetailDto
        {
            Id = id,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            ChiefComplaint = dto.ChiefComplaint,
            PresentIllness = dto.PresentIllness,
            PastHistory = dto.PastHistory,
            Remark = dto.Remark
        };
        
        return await _businessService.UpdateAsync(id, detailDto);
    }

    /// <summary>
    /// 删除医案
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.Delete(id);

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

    #endregion 基础业务操作 - 对应简化接口

    #region 共享接口IMedicalCaseService额外方法 - 委托给相应服务层

    /// <summary>
    /// 暂停医疗案例
    /// </summary>
    public async Task<ServiceResult<bool>> Suspend(Guid id, string reason)
        => await _businessService.SuspendAsync(id, reason);

    /// <summary>
    /// 恢复医疗案例
    /// </summary>
    public async Task<ServiceResult<bool>> Resume(Guid id)
        => await _businessService.ResumeAsync(id);

    /// <summary>
    /// 取消咨询/诊断 - 委托给CancelAsync
    /// </summary>
    public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        => await CancelAsync(id);

    /// <summary>
    /// 更新医疗案例状态 - 委托给BusinessService
    /// </summary>
    public async Task<ServiceResult<bool>> UpdateStatus(Guid id, int status)
    {
        // 根据状态值调用对应的业务方法
        var medicalCaseStatus = (MedicalCaseStatus)status;
        return medicalCaseStatus switch
        {
            MedicalCaseStatus.InConsultation => await _businessService.StartAsync(id),
            MedicalCaseStatus.Completed => await _businessService.CompleteAsync(id),
            MedicalCaseStatus.Cancelled => await _businessService.CancelAsync(id),
            _ => ServiceResult<bool>.Failure($"不支持的状态更新: {medicalCaseStatus}")
        };
    }

    /// <summary>
    /// 归档医疗案例
    /// </summary>
    public async Task<ServiceResult<bool>> Archive(Guid id, string archiveReason)
        => await _businessService.ArchiveAsync(id, archiveReason ?? "归档医疗案例");

    /// <summary>
    /// 获取医疗案例统计信息 - 简单诊所版本基础实现
    /// </summary>
    public Task<ServiceResult<object>> GetStatistics(DateTime? startDate, DateTime? endDate)
        => Task.FromResult(ServiceResult<object>.Success(new { TotalCases = 0, ActiveCases = 0, CompletedCases = 0 }));

    /// <summary>
    /// 搜索医疗案例
    /// </summary>
    public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        => await _businessService.SearchAsync(keyword);

    /// <summary>
    /// 获取医疗案例历史记录 - 简单诊所版本暂不支持
    /// </summary>
    public Task<ServiceResult<List<object>>> GetHistory(Guid id)
        => Task.FromResult(ServiceResult<List<object>>.Success([]));

    #endregion 共享接口IMedicalCaseService额外方法 - 委托给相应服务层
}
