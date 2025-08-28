using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services.Archive
{
    /// <summary>
    /// 患者档案管理服务接口
    /// UltraThink重构：包装现有的PatientArchiveService，提供清晰的档案管理接口
    /// </summary>
    public interface IPatientArchiveService
    {
        /// <summary>
        /// 更新患者过敏史
        /// </summary>
        /// <param name="patientId">患者ID</param>        /// <param name="allergyHistory">过敏史</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>更新结果</returns>
        Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取患者标签
        /// </summary>        /// <param name="patientId">患者ID</param>        /// <returns>患者标签列表</returns>
        Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId);

        /// <summary>
        /// 设置患者标签
        /// </summary>        /// <param name="patientId">患者ID</param>        /// <param name="tags">标签列表</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>设置结果</returns>
        Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取患者就诊历史
        /// </summary>        /// <param name="patientId">患者ID</param>        /// <returns>就诊历史</returns>
        Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId);

        /// <summary>
        /// 合并重复患者
        /// </summary>        /// <param name="primaryId">主患者ID</param>        /// <param name="duplicateId">重复患者ID</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>合并结果</returns>
        Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量导入患者
        /// </summary>        /// <param name="patients">患者列表</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>导入结果</returns>
        Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName);

        /// <summary>
        /// 导出患者数据
        /// </summary>        /// <param name="query">导出查询条件</param>
        /// <returns>导出数据</returns>
        Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query);
    }
}
