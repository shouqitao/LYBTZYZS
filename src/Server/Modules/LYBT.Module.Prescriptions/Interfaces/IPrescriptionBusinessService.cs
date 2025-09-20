using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces
{

    /// <summary>
    /// 处方业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// </summary>
    public interface IPrescriptionBusinessService
    {

        /// <summary>
        /// 复制处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName, Guid operatorId, string operatorName);

        /// <summary>
        /// 复制患者最近处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName);


        /// <summary>
        /// 快速保存处方
        /// </summary>
        Task<ServiceResult<bool>> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 取消处方
        /// </summary>
        Task<ServiceResult<bool>> CancelAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 创建处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto);

        /// <summary>
        /// 删除处方
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
    }
}
