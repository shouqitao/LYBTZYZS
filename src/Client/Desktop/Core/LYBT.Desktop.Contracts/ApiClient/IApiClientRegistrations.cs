// ---------------------------------------------------------------------------
// IApiClientRegistrations — Registration API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IRegistrationApi (remote) and ILocalRegistrationApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 挂号 API 子接口——CRUD、队列管理、就诊操作。
/// </summary>
/// <remarks>
/// <para>Combines methods from IRegistrationApi (remote) and ILocalRegistrationApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientRegistrations
{
    /// <summary>
    /// 创建挂号（分诊台模式）。
    /// US-REG-001：Source=Receptionist，Status=Waiting
    /// </summary>
    /// <param name="request">Registration input data.</param>
    Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request);

    /// <summary>
    /// 按 ID 获取挂号详情。
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页获取挂号列表并支持筛选。
    /// US-REG-007：支持按日期范围、患者、医生筛选。
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="patientId">Patient ID filter (optional).</param>
    /// <param name="doctorId">Doctor ID filter (optional).</param>
    Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? patientId = null,
        Guid? doctorId = null);

    /// <summary>
    /// 获取候诊队列。
    /// US-REG-003：Waiting 状态，按挂号时间升序排列。
    /// </summary>
    /// <param name="doctorId">Doctor ID filter (optional).</param>
    Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId = null);

    /// <summary>
    /// 开始就诊——将挂号流转为 InProgress。
    /// US-REG-003 验收标准 #4。
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task<ApiResponse<Guid>> StartVisitAsync(Guid id);

    /// <summary>
    /// 快速就诊（B2 US-REG-002: 医生直接开始就诊——急诊通道/本地无前台场景）。
    /// 服务端原子创建挂号并启动医案。

    /// <summary>
    /// 取消挂号。
    /// US-REG-004：仅 Waiting 状态可取消。
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task<ApiResponse> CancelAsync(Guid id);
}
