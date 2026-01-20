using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者数据仓储接口 - RESTful设计
    /// List返回轻量ListDto，Detail返回完整DetailDto
    /// </summary>
    public interface IPatientRepository
    {
        /// <summary>
        /// 分页查询患者列表（返回轻量级ListDto）
        /// </summary>
        Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者详情（返回完整DetailDto）
        /// </summary>
        Task<PatientDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<PatientDetailDto> CreateAsync(PatientInputDto patient);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<PatientDetailDto> UpdateAsync(PatientInputDto patient);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索患者（基于关键词，返回ListDto）
        /// </summary>
        Task<List<PatientListDto>> SearchAsync(string keyword);

        /// <summary>
        /// 根据身份证号获取患者详情
        /// OpenSpec: integrate-cardreader-module - 支持读卡器查找患者
        /// </summary>
        /// <param name="idNumber">身份证号</param>
        /// <returns>患者详情（如找到），否则返回null</returns>
        Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber);

        #region 批量导入/导出功能

        /// <summary>
        /// 批量导入患者数据
        /// </summary>
        Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request);

        /// <summary>
        /// 下载患者导入模板
        /// </summary>
        Task<byte[]?> ExportTemplateAsync();

        /// <summary>
        /// 导出患者数据到Excel
        /// </summary>
        Task<byte[]?> ExportPatientsAsync(string? keyword = null);

        #endregion

        #region 恢复和批量操作

        /// <summary>
        /// 恢复已删除的患者
        /// </summary>
        Task<PatientDetailDto?> RestoreAsync(Guid id);

        /// <summary>
        /// 批量删除患者
        /// </summary>
        Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids);

        #endregion
    }
}
