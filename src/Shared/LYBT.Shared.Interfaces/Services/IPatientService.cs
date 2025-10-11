using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 从Excel文件导入患者数据 (Issue #1165)
        /// </summary>
        /// <param name="stream">Excel文件流</param>
        /// <param name="fileName">文件名（可选，用于日志记录）</param>
        /// <returns>导入结果，包含成功、失败数量和详细错误信息</returns>
        Task<ServiceResult<ImportResultDto<PatientDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 生成患者导入模板 (Issue #1165)
        /// </summary>
        /// <returns>包含示例数据的Excel模板流</returns>
        MemoryStream GenerateImportTemplate();
    }
}
