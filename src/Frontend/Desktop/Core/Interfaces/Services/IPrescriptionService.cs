using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 处方服务接口
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// 分页查询处方
        /// </summary>
        Task<LYBT.WPF.Client.Core.Models.Common.PagedResult<PrescriptionDto>> GetPagedAsync(PagedQueryBaseDto request);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        Task<ServiceResult<PrescriptionDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> UpdateAsync(PrescriptionEditDto dto);

        /// <summary>
        /// 创建或更新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CreateOrUpdateAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 删除处方
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 作废处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CancelAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 获取今日处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetTodayPrescriptionsAsync();

        /// <summary>
        /// 根据医疗案例ID获取处方
        /// </summary>
        Task<ServiceResult<PrescriptionDetailDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
    }
}