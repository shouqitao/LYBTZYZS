using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.WPF.Client.BusinessModules.Shared
{
    /// <summary>
    /// 共享医疗案例服务接口
    /// 提供跨工作台的医疗案例管理功能，作为诊疗流程的聚合根
    /// </summary>
    public interface ISharedMedicalCaseService
    {
        /// <summary>
        /// 获取医疗案例列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="searchKeyword">搜索关键词</param>
        /// <returns>分页医疗案例列表</returns>
        Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetMedicalCasesAsync(int page = 1, int pageSize = 20, string searchKeyword = null);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <returns>医疗案例详细信息</returns>
        Task<ServiceResult<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid caseId);

        /// <summary>
        /// 创建新医疗案例
        /// </summary>
        /// <param name="dto">医疗案例信息</param>
        /// <returns>创建的医疗案例信息</returns>
        Task<ServiceResult<MedicalCaseDetailDto>> CreateMedicalCaseAsync(MedicalCaseDetailDto dto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        /// <param name="dto">更新的医疗案例信息</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult<MedicalCaseDetailDto>> UpdateMedicalCaseAsync(MedicalCaseDetailDto dto);

        /// <summary>
        /// 获取患者的医疗案例历史
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>医疗案例历史列表</returns>
        Task<ServiceResult<List<MedicalCaseDto>>> GetPatientMedicalCaseHistoryAsync(Guid patientId, int limit = 10);

        /// <summary>
        /// 开始新的医疗案例
        /// 创建案例并初始化基本信息
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID</param>
        /// <param name="chiefComplaint">主诉</param>
        /// <returns>医疗案例信息</returns>
        Task<ServiceResult<MedicalCaseDetailDto>> StartMedicalCaseAsync(Guid patientId, Guid doctorId, string chiefComplaint);

        /// <summary>
        /// 完成医疗案例
        /// 保存最终诊断和治疗方案
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <param name="finalDiagnosis">最终诊断</param>
        /// <param name="treatmentPlan">治疗方案</param>
        /// <returns>完成结果</returns>
        Task<ServiceResult> CompleteMedicalCaseAsync(Guid caseId, string finalDiagnosis, string treatmentPlan);

        /// <summary>
        /// 添加病历记录
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <param name="recordType">记录类型</param>
        /// <param name="content">记录内容</param>
        /// <returns>添加结果</returns>
        Task<ServiceResult> AddMedicalRecordAsync(Guid caseId, string recordType, string content);

        /// <summary>
        /// 获取案例的完整病历
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <returns>病历记录列表</returns>
        Task<ServiceResult<List<object>>> GetMedicalRecordsAsync(Guid caseId);

        /// <summary>
        /// 关联看诊记录到案例
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <param name="consultationId">看诊ID</param>
        /// <returns>关联结果</returns>
        Task<ServiceResult> LinkConsultationAsync(Guid caseId, Guid consultationId);

        /// <summary>
        /// 关联处方到案例
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>关联结果</returns>
        Task<ServiceResult> LinkPrescriptionAsync(Guid caseId, Guid prescriptionId);

        /// <summary>
        /// 获取案例统计信息
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>统计信息</returns>
        Task<ServiceResult<object>> GetCaseStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 搜索相似案例
        /// 基于诊断和症状搜索相似的历史案例
        /// </summary>
        /// <param name="symptoms">症状描述</param>
        /// <param name="diagnosis">诊断</param>
        /// <returns>相似案例列表</returns>
        Task<ServiceResult<List<MedicalCaseDto>>> SearchSimilarCasesAsync(string symptoms, string diagnosis);

        /// <summary>
        /// 导出医疗案例
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <param name="format">导出格式</param>
        /// <returns>导出结果</returns>
        Task<ServiceResult<byte[]>> ExportMedicalCaseAsync(Guid caseId, string format = "pdf");

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        /// <param name="caseId">案例ID</param>
        /// <param name="reason">归档原因</param>
        /// <returns>归档结果</returns>
        Task<ServiceResult> ArchiveMedicalCaseAsync(Guid caseId, string reason);
    }
}