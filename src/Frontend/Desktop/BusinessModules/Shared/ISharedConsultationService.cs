using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.WPF.Client.BusinessModules.Shared
{
    /// <summary>
    /// 共享看诊服务接口
    /// 提供跨工作台的看诊管理功能，支持中医四诊
    /// </summary>
    public interface ISharedConsultationService
    {
        /// <summary>
        /// 获取看诊列表
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="searchKeyword">搜索关键词</param>
        /// <returns>分页看诊列表</returns>
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetConsultationsAsync(int page = 1, int pageSize = 20, string searchKeyword = null);

        /// <summary>
        /// 根据ID获取看诊详情
        /// </summary>
        /// <param name="consultationId">看诊ID</param>
        /// <returns>看诊详细信息</returns>
        Task<ServiceResult<ConsultationDetailDto>> GetConsultationByIdAsync(Guid consultationId);

        /// <summary>
        /// 创建新看诊记录
        /// </summary>
        /// <param name="dto">看诊信息</param>
        /// <returns>创建的看诊信息</returns>
        Task<ServiceResult<ConsultationDetailDto>> CreateConsultationAsync(ConsultationDetailDto dto);

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        /// <param name="dto">更新的看诊信息</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult<ConsultationDetailDto>> UpdateConsultationAsync(ConsultationDetailDto dto);

        /// <summary>
        /// 获取患者的看诊历史
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>看诊历史列表</returns>
        Task<ServiceResult<List<ConsultationDto>>> GetPatientConsultationHistoryAsync(Guid patientId, int limit = 10);

        /// <summary>
        /// 开始看诊
        /// 创建看诊会话并初始化基本信息
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID</param>
        /// <param name="medicalCaseId">医疗案例ID</param>
        /// <returns>看诊会话信息</returns>
        Task<ServiceResult<ConsultationDetailDto>> StartConsultationAsync(Guid patientId, Guid doctorId, Guid? medicalCaseId = null);

        /// <summary>
        /// 完成看诊
        /// 保存最终诊断和治疗方案
        /// </summary>
        /// <param name="consultationId">看诊ID</param>
        /// <param name="finalDiagnosis">最终诊断</param>
        /// <param name="treatmentPlan">治疗方案</param>
        /// <returns>完成结果</returns>
        Task<ServiceResult> CompleteConsultationAsync(Guid consultationId, string finalDiagnosis, string treatmentPlan);

        /// <summary>
        /// 保存四诊信息
        /// 保存望闻问切的诊断结果
        /// </summary>
        /// <param name="consultationId">看诊ID</param>
        /// <param name="inspection">望诊结果</param>
        /// <param name="auscultationOlfaction">闻诊结果</param>
        /// <param name="inquiry">问诊结果</param>
        /// <param name="palpation">切诊结果</param>
        /// <returns>保存结果</returns>
        Task<ServiceResult> SaveFourExaminationsAsync(Guid consultationId, string inspection, string auscultationOlfaction, string inquiry, string palpation);

        /// <summary>
        /// 保存舌脉诊
        /// </summary>
        /// <param name="consultationId">看诊ID</param>
        /// <param name="tongueInspection">舌诊结果</param>
        /// <param name="pulseCondition">脉诊结果</param>
        /// <returns>保存结果</returns>
        Task<ServiceResult> SaveTonguePulseDiagnosisAsync(Guid consultationId, string tongueInspection, string pulseCondition);

        /// <summary>
        /// 保存辨证论治
        /// </summary>
        /// <param name="consultationId">看诊ID</param>
        /// <param name="patternDifferentiation">辨证分析</param>
        /// <param name="tcmDiagnosis">中医辨证</param>
        /// <param name="treatmentPrinciple">治疗原则</param>
        /// <returns>保存结果</returns>
        Task<ServiceResult> SavePatternDifferentiationAsync(Guid consultationId, string patternDifferentiation, string tcmDiagnosis, string treatmentPrinciple);

        /// <summary>
        /// 获取看诊模板
        /// 常用的看诊记录模板
        /// </summary>
        /// <param name="category">模板分类</param>
        /// <returns>模板列表</returns>
        Task<ServiceResult<List<ConsultationDetailDto>>> GetConsultationTemplatesAsync(string category = null);

        /// <summary>
        /// 搜索历史相似病例
        /// 基于症状和诊断搜索相似的历史病例
        /// </summary>
        /// <param name="symptoms">症状描述</param>
        /// <param name="diagnosis">初步诊断</param>
        /// <returns>相似病例列表</returns>
        Task<ServiceResult<List<ConsultationDto>>> SearchSimilarCasesAsync(string symptoms, string diagnosis);

        /// <summary>
        /// 获取医生的看诊统计
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>统计信息</returns>
        Task<ServiceResult<object>> GetConsultationStatisticsAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 导出看诊记录
        /// </summary>
        /// <param name="consultationId">看诊ID</param>
        /// <param name="format">导出格式</param>
        /// <returns>导出结果</returns>
        Task<ServiceResult<byte[]>> ExportConsultationAsync(Guid consultationId, string format = "pdf");
    }
}