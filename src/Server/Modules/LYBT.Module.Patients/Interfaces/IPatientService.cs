using LYBT.Entities.Patients;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

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
        Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false);

        /// <summary>
        /// 分页查询患者列表（返回PatientListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键词</param>
        /// <param name="filterDisabled">T5-P2-27: 是否过滤禁用患者</param>
        Task<Result<PagedResult<PatientListDto>>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false);

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<Result<PatientDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<Result<PatientDetailDto>> CreateAsync(PatientInputDto dto);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<Result<PatientDetailDto>> UpdateAsync(Guid id, PatientInputDto dto);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<Result<List<PatientDetailDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// 支持部分成功模式、失败恢复机制（BR-002）
        /// </summary>
        /// <param name="stream">Excel文件流</param>
        /// <param name="fileName">文件名（可选，用于日志记录）</param>
        /// <returns>批量导入结果，包含成功/失败/跳过数量和详细失败信息</returns>
        Task<Result<PatientBatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        /// <param name="config">模板配置（示例数据行数等）</param>
        /// <returns>Excel模板文件流</returns>
        Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config);

        /// <summary>
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        /// <param name="keyword">搜索关键词（可选）</param>
        /// <returns>Excel文件流</returns>
        Task<MemoryStream> ExportPatientsAsync(string? keyword = null);

        #region Entity直接返回方法 (合并自IPatientServiceOptimized)

        /// <summary>
        /// 获取分页患者数据（直接返回Patient Entity）
        /// </summary>
        Task<Result<PagedResult<Patient>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> GetByIdEntityAsync(Guid id);

        /// <summary>
        /// 创建患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> CreateEntityAsync(PatientInputDto dto);

        /// <summary>
        /// 更新患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> UpdateEntityAsync(Guid id, PatientInputDto dto);

        #endregion

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法 ==========

        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// </summary>
        Task<Result<PatientDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复软删除的患者
        /// </summary>
        /// <param name="id">患者ID</param>
        Task<Result<PatientDetailDto>> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除患者
        /// </summary>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

        // ========== OpenSpec: implement-data-sync - 引用检查 ==========

        /// <summary>
        /// 检查患者是否被医案引用
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>引用检查结果</returns>
        Task<Result<PatientReferenceCheckDto>> CheckReferenceAsync(Guid patientId);

        /// <summary>
        /// 批量检查患者引用关系
        /// </summary>
        /// <param name="patientIds">患者ID列表</param>
        /// <returns>引用检查结果列表</returns>
        Task<Result<List<PatientReferenceCheckDto>>> BatchCheckReferenceAsync(List<Guid> patientIds);
    }
}
