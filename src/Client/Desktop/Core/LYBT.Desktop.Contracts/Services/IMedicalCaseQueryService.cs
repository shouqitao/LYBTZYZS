using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 医案查询服务接口 - 跨模块共享
/// OpenSpec: rationalize-module-architecture - 遵循依赖倒置原则
/// OpenSpec: refactor-frontend-srp-patterns (ADR-1) - SRP职责分离，查询职责
/// Patients模块通过此接口查询MedicalCase数据，消除对具体实现的依赖
/// </summary>
public interface IMedicalCaseQueryService
{
    /// <summary>
    /// 分页查询病案列表（返回轻量级ListDto）
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="searchText">搜索关键字</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null);

    /// <summary>
    /// 统一查询医案
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query);

    /// <summary>
    /// 获取患者未完成的医案
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="doctorId">医生ID</param>
    /// <param name="checkAllDoctors">是否检查所有医生的未完成医案</param>
    /// <returns>未完成的医案详情，如果没有返回null</returns>
    Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(
        Guid patientId,
        Guid doctorId,
        bool checkAllDoctors = false);

    /// <summary>
    /// 关闭医案
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>API响应，包含关闭后的医案详情</returns>
    Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid medicalCaseId);
}
