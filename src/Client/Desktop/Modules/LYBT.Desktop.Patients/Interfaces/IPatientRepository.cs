using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using System.IO;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IPatientRepository
    {
        Task<List<PatientDto>> GetAllAsync();
        Task<PatientDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者（使用CreateDto）
        /// </summary>
        Task<PatientDto> CreateAsync(PatientInputDto patient);

        /// <summary>
        /// 更新患者信息（使用UpdateDto）
        /// </summary>
        Task<PatientDto> UpdateAsync(PatientInputDto patient);

        Task<bool> DeleteAsync(Guid id);
        Task<List<PatientDto>> SearchAsync(string keyword);

        /// <summary>
        /// 分页查询患者列表（服务端分页）- P0性能修复
        /// </summary>
        Task<PagedResult<PatientDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 获取患者列表（返回PatientListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        Task<PagedResult<PatientListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null);

        // ========== Epic #1934: 批量导入/导出功能 ==========

        /// <summary>
        /// 批量导入患者数据 (Issue #2004 Task 2.11)
        /// Desktop主导模式：Desktop解析Excel并组装DTO，Repository调用API
        /// </summary>
        /// <param name="request">批量导入请求（包含患者列表和重复处理策略）</param>
        /// <returns>导入结果</returns>
        Task<BatchImportResultDto?> BatchImportAsync(PatientBatchImportRequestDto request);

        /// <summary>
        /// 下载患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        Task<byte[]?> ExportTemplateAsync();

        /// <summary>
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        Task<byte[]?> ExportPatientsAsync(string? keyword = null);

        // ========== OpenSpec: optimize-module-list-ui - 恢复功能 ==========

        /// <summary>
        /// 恢复已删除的患者
        /// 注：患者实体无Status字段，因此无ToggleStatus方法
        /// </summary>
        Task<PatientDto?> RestoreAsync(Guid id);
    }
}
