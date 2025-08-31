using System;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Services.Core
{
    /// <summary>
    /// 患者基础CRUD操作服务接口
    /// UltraThink重构：单一职责原则，只负责患者的基础增删改查操作
    /// </summary>
    public interface IPatientCrudService
    {
        /// <summary>
        /// 创建新患者档案
        /// </summary>
        /// <param name="dto">患者创建DTO</param>        /// <returns>创建结果</returns>
        Task<ServiceResult<PatientDto>> CreatePatientAsync(PatientCreateDto dto);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        /// <param name="id">患者ID</param>        /// <param name="dto">患者更新DTO</param>        /// <returns>更新结果</returns>
        Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, PatientUpdateDto dto);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        /// <param name="id">患者ID</param>        /// <returns>删除结果</returns>
        Task<ServiceResult<bool>> DeletePatientAsync(Guid id);

        /// <summary>
        /// 删除患者（带操作者信息）
        /// </summary>
        /// <param name="id">患者ID</param>        /// <param name="operatorId">操作者ID</param>        /// <param name="operatorName">操作者姓名</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult<bool>> DeletePatientAsync(Guid id, Guid operatorId, string operatorName);
    }
}
