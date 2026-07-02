using LYBT.Entities.Patients;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using System.Threading;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者服务接口 - 统一接口，包含DTO和Entity两种返回模式
    /// 合并自IPatientServiceOptimized
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 分页查询患者
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="filterDisabled">T5-P2-27: 是否过滤禁用患者 (非Admin角色传true)</param>
        Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<Result<PatientDetailDto>> CreateAsync(PatientInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<Result<PatientDetailDto>> UpdateAsync(Guid id, PatientInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<Result<List<PatientDetailDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default);

        #region Entity直接返回方法 (合并自IPatientServiceOptimized)

        /// <summary>
        /// 获取分页患者数据（直接返回Patient Entity）
        /// </summary>
        Task<Result<PagedResult<Patient>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据ID获取患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> GetByIdEntityAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> CreateEntityAsync(PatientInputDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> UpdateEntityAsync(Guid id, PatientInputDto dto, CancellationToken cancellationToken = default);

        #endregion
        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// </summary>
        Task<Result<PatientDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
        /// <summary>
        /// 批量删除患者
        /// </summary>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default);

    }
}


