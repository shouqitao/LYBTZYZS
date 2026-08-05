using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// 实体 API 段泛型接口 — 标准 CRUD 5 方法（GetPaged/GetById/Create/Update/Delete）。
/// 仅适用于标准 CRUD 形状一致的实体；形状特殊的实体（如 MedicalCase/Registration）不实现本接口。
/// 段接口继承本接口时，若现有方法命名不同（如 GetPatientsAsync vs GetPagedAsync），
/// 保留现有命名，并在段接口中用默认实现（DIM）转发到现有方法。
/// </summary>
/// <typeparam name="TListDto">列表 DTO 类型</typeparam>
/// <typeparam name="TDetailDto">详情 DTO 类型</typeparam>
/// <typeparam name="TInputDto">输入 DTO 类型</typeparam>
public interface IEntityApiSegment<TListDto, TDetailDto, TInputDto>
{
    /// <summary>
    /// 分页获取实体列表。
    /// </summary>
    /// <param name="page">页码（默认 1）</param>
    /// <param name="pageSize">每页条数（默认 20）</param>
    /// <param name="keyword">搜索关键词（可选）</param>
    /// <param name="category">分类筛选（可选；无分类的实体忽略）</param>
    Task<ApiResponse<PagedResult<TListDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        string? category = null);

    /// <summary>
    /// 按 ID 获取实体详情。
    /// </summary>
    /// <param name="id">实体 ID</param>
    Task<ApiResponse<TDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 创建新实体。
    /// </summary>
    /// <param name="request">实体输入数据</param>
    Task<ApiResponse<TDetailDto>> CreateAsync(TInputDto request);

    /// <summary>
    /// 更新现有实体。
    /// </summary>
    /// <param name="id">实体 ID</param>
    /// <param name="request">实体输入数据</param>
    Task<ApiResponse<TDetailDto>> UpdateAsync(Guid id, TInputDto request);

    /// <summary>
    /// 删除实体（软删除）。
    /// </summary>
    /// <param name="id">实体 ID</param>
    Task<ApiResponse> DeleteAsync(Guid id);
}
