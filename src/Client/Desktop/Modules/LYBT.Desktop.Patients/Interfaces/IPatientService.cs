using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces
{
    /// <summary>
    /// 患者Service接口
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: cleanup-patient-dead-code - 清理未使用的事件和Command
    /// </summary>
    public interface IPatientService
    {
        #region 患者CRUD操作

        /// <summary>
        /// 创建患者
        /// </summary>
        Task<CommandResult<PatientDetailDto>> CreatePatientAsync(PatientInputDto inputDto);

        /// <summary>
        /// 更新患者
        /// </summary>
        Task<CommandResult<PatientDetailDto>> UpdatePatientAsync(PatientInputDto inputDto);

        /// <summary>
        /// 删除患者
        /// </summary>
        Task<CommandResult<bool>> DeletePatientAsync(Guid patientId);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除患者
        /// OpenSpec: optimize-batch-operations Phase 2 - 返回BatchOperationResultDto
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeletePatientsAsync(IEnumerable<Guid> patientIds);

        #endregion

        #region 查询操作

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<CommandResult<IEnumerable<PatientListDto>>> SearchPatientsAsync(string keyword);

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<CommandResult<PagedResult<PatientListDto>>> GetPatientsPagedAsync(int page, int pageSize, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者（Issue #1788: 支持单个患者查询）
        /// </summary>
        Task<CommandResult<PatientDetailDto>> GetByIdAsync(Guid patientId);

        #endregion
    }
}
