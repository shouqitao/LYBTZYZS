using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.WPF.Client.BusinessModules.Shared
{
    /// <summary>
    /// 共享患者服务接口
    /// 提供跨工作台的患者管理功能
    /// </summary>
    public interface ISharedPatientService
    {
        /// <summary>
        /// 创建新患者档案
        /// 可被SystemWorkbench、ConsultationWorkbench、ReceptionWorkbench调用
        /// </summary>
        /// <param name="dto">患者详细信息</param>
        /// <returns>创建的患者信息</returns>
        Task<ServiceResult<PatientDetailDto>> CreatePatientAsync(PatientDetailDto dto);

        /// <summary>
        /// 根据ID获取患者信息
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>患者详细信息</returns>
        Task<ServiceResult<PatientDetailDto>> GetPatientAsync(Guid patientId);

        /// <summary>
        /// 快速搜索患者
        /// 支持姓名、拼音、电话号码后四位、身份证后四位
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的患者列表</returns>
        Task<ServiceResult<List<PatientDetailDto>>> QuickSearchAsync(string keyword);

        /// <summary>
        /// 获取活跃患者列表
        /// 用于快速选择最近就诊的患者
        /// </summary>
        /// <param name="limit">返回数量限制，默认20</param>
        /// <returns>活跃患者列表</returns>
        Task<ServiceResult<List<PatientDetailDto>>> GetActivePatientsAsync(int limit = 20);

        /// <summary>
        /// 更新患者基本信息
        /// </summary>
        /// <param name="dto">更新的患者信息</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult> UpdatePatientBasicInfoAsync(PatientDetailDto dto);

        /// <summary>
        /// 查找或创建患者
        /// 根据身份证号查找，如果不存在则创建
        /// </summary>
        /// <param name="dto">患者信息</param>
        /// <returns>找到或创建的患者信息</returns>
        Task<ServiceResult<PatientDetailDto>> FindOrCreatePatientAsync(PatientDetailDto dto);

        /// <summary>
        /// 验证患者档案是否完整
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>验证结果</returns>
        Task<ServiceResult<bool>> ValidatePatientProfileAsync(Guid patientId);

        /// <summary>
        /// 获取患者最后就诊信息
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>最后就诊记录</returns>
        Task<ServiceResult<object>> GetLastVisitInfoAsync(Guid patientId);
    }
}