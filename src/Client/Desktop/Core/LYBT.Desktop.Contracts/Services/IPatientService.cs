using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using System.Threading;
namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 患者Service接口
    /// </summary>
    public interface IPatientService : ICrudService<PatientListDto, PatientDetailDto, PatientInputDto>
    {
        #region 批量导入/导出

        /// <summary>
        /// 批量导入患者数据
        /// </summary>
        Task<CommandResult<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default);

        /// <summary>
        /// 下载患者导入模板
        /// </summary>
        Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default);

        /// <summary>
        /// 导出患者数据到Excel
        /// </summary>
        Task<CommandResult<byte[]>> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default);

        /// <summary>
        /// 批量删除患者（软删除）
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);

        #endregion
    }
}
