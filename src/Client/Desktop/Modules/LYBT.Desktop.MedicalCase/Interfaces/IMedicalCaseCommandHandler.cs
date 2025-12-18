using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 病案命令处理器接口
    /// Desktop层架构重构 Phase 1: 接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IMedicalCaseCommandHandler : ICommandHandler
    {
        #region 通用数据操作命令

        /// <summary>
        /// 保存病案聚合根数据（病案+诊疗+处方）
        /// </summary>
        /// <param name="validateBeforeSave">保存前是否验证</param>
        /// <returns>是否保存成功</returns>
        Task<bool> SaveAsync(bool validateBeforeSave = true);

        /// <summary>
        /// 删除病案数据
        /// </summary>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteAsync();

        /// <summary>
        /// 重新加载病案数据
        /// </summary>
        /// <returns>是否重新加载成功</returns>
        Task<bool> ReloadAsync();

        #endregion

        // [已移除] 三步流程步骤验证命令 (CanCompleteStep1, CanMarkForPrescription, CanCreatePrescription, ValidateStepAsync)

        #region 处方管理命令

        /// <summary>
        /// 创建处方
        /// </summary>
        /// <param name="createDto">处方创建DTO</param>
        /// <returns>是否创建成功</returns>
        Task<bool> CreatePrescriptionAsync(PrescriptionInputDto createDto);

        /// <summary>
        /// 更新处方（实际通过Save实现）
        /// </summary>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdatePrescriptionAsync();

        /// <summary>
        /// 删除处方
        /// </summary>
        /// <returns>是否删除成功</returns>
        Task<bool> DeletePrescriptionAsync();

        #endregion

        #region 导航命令

        /// <summary>
        /// 导航到患者病历历史
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>是否成功</returns>
        Task<bool> NavigateToPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 导航到病案列表
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> NavigateToMedicalCaseListAsync();

        #endregion
    }
}
