using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// 职责：医疗案例业务逻辑、状态管理、生命周期管理
    /// </summary>
    public interface IMedicalCaseBusinessService
    {
        /// <summary>
        /// 创建医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto createDto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid caseId, MedicalCaseUpdateDto updateDto);

        /// <summary>
        /// 删除医疗案例
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid caseId);

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        Task<ServiceResult<bool>> CompleteAsync(Guid caseId);

        /// <summary>
        /// 暂停医疗案例
        /// </summary>
        Task<ServiceResult<bool>> SuspendAsync(Guid caseId);

        /// <summary>
        /// 恢复医疗案例
        /// </summary>
        Task<ServiceResult<bool>> ResumeAsync(Guid caseId);

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        Task<ServiceResult<bool>> ArchiveAsync(Guid caseId);

        /// <summary>
        /// 更新案例状态
        /// </summary>
        Task<ServiceResult<bool>> UpdateStatusAsync(Guid caseId, string status);

        /// <summary>
        /// 取消看诊
        /// </summary>
        Task<ServiceResult<bool>> CancelConsultationAsync(Guid caseId);

        /// <summary>
        /// 批量更新状态
        /// </summary>
        Task<ServiceResult<bool>> BatchUpdateStatusAsync(List<Guid> caseIds, string status);

        /// <summary>
        /// 打印病历记录
        /// </summary>
        Task<ServiceResult<object>> PrintMedicalRecordAsync(Guid caseId, object printOptions);
    }
}
