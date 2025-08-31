using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services.ImportExport
{
    /// <summary>
    /// 患者导入导出服务接口
    /// UltraThink重构：专注于患者数据的导入导出功能
    /// </summary>
    public interface IPatientImportExportService
    {
        /// <summary>
        /// 批量导入患者（完整版）
        /// </summary>
        /// <param name="patients">患者导入数据</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>导入结果</returns>
        Task<ServiceResult<PatientImportResultDto>> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量导入患者（简化版）
        /// </summary>
        /// <param name="patients">患者创建数据</param>        /// <returns>导入结果</returns>
        Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients);

        /// <summary>
        /// 导出患者数据（完整版）
        /// </summary>
        /// <param name="query">导出查询条件</param>        /// <returns>导出结果</returns>
        Task<ServiceResult<List<PatientExportDto>>> ExportPatientsAsync(PatientExportQueryDto query);

        /// <summary>
        /// 导出患者数据（简化版）
        /// </summary>
        /// <param name="query">分页查询条件</param>
        /// <returns>导出文件数据</returns>
        Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query);
    }
}
