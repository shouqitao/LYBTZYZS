using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Patients.Services.Status
{
    /// <summary>
    /// 患者状态管理服务接口
    /// UltraThink重构：专注于患者状态控制和管理功能
    /// </summary>
    public interface IPatientStatusService
    {
        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        /// <param name="id">患者ID</param>        /// <param name="isActive">是否启用</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> SetPatientStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName);

        /// <summary>
        /// 启用患者
        /// </summary>
        /// <param name="id">患者ID</param>        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> EnablePatientAsync(Guid id);

        /// <summary>
        /// 禁用患者
        /// </summary>
        /// <param name="id">患者ID</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> DisablePatientAsync(Guid id);
    }
}
