using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// 挂号管理 Refit API 客户端接口
/// PRD: registration.md US-REG-001~006
/// </summary>
public interface IRegistrationApi
{
    /// <summary>
    /// 创建挂号 (前台模式)
    /// US-REG-001: Source=Receptionist, Status=Waiting
    /// </summary>
    [Refit.Post("/api/v1/registrations")]
    Task<ApiResponse<RegistrationDetailDto>> CreateAsync([Refit.Body] RegistrationInputDto request);

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    [Refit.Get("/api/v1/registrations/{id}")]
    Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页查询挂号记录
    /// </summary>
    [Refit.Get("/api/v1/registrations")]
    Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync(
        [Refit.Query] int page = 1,
        [Refit.Query] int pageSize = 20,
        [Refit.Query] string? keyword = null);

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    [Refit.Get("/api/v1/registrations/queue")]
    Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(
        [Refit.Query] Guid? doctorId = null);

    /// <summary>
    /// 接诊: Registration -> InProgress
    /// US-REG-003 验收标准第4条
    /// </summary>
    [Refit.Put("/api/v1/registrations/{id}/start-visit")]
    Task<ApiResponse<Guid>> StartVisitAsync(Guid id);

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Waiting 状态可取消
    /// </summary>
    [Refit.Put("/api/v1/registrations/{id}/cancel")]
    Task<ApiResponse> CancelAsync(Guid id);
}
