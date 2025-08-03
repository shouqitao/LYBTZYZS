using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Models.Consultation;
using LYBT.WPF.Client.Core.Models.Patients;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 看诊服务接口
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 创建新的诊疗记录
        /// </summary>
        Task<ApiResponse<ConsultationRecord>> CreateConsultationAsync(PatientInfo patient);

        /// <summary>
        /// 保存诊疗记录
        /// </summary>
        Task<ApiResponse<object>> SaveConsultationAsync(ConsultationRecord record);

        /// <summary>
        /// 完成诊疗
        /// </summary>
        Task<ApiResponse<object>> CompleteConsultationAsync(Guid consultationId);

        /// <summary>
        /// 获取诊疗记录列表
        /// </summary>
        Task<ApiResponse<List<ConsultationRecord>>> GetConsultationRecordsAsync(ConsultationQueryRequest request);

        /// <summary>
        /// 获取诊疗记录详情
        /// </summary>
        Task<ApiResponse<ConsultationRecord>> GetConsultationByIdAsync(Guid id);

        /// <summary>
        /// 生成处方打印数据
        /// </summary>
        Task<ApiResponse<PrescriptionPrintData>> GeneratePrescriptionPrintAsync(Guid consultationId);
    }

    /// <summary>
    /// 诊疗查询请求
    /// </summary>
    public class ConsultationQueryRequest
    {
        public string? PatientName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ConsultationStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 处方打印数据
    /// </summary>
    public class PrescriptionPrintData
    {
        public string HospitalName { get; set; } = "凌隐宝堂中医诊所";
        public string PatientName { get; set; } = string.Empty;
        public string PatientAge { get; set; } = string.Empty;
        public string PatientGender { get; set; } = string.Empty;
        public DateTime ConsultationDate { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public List<PrescriptionItem> Prescription { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public string Usage { get; set; } = "水煎服，日一剂，早晚温服";
        public string? DoctorAdvice { get; set; }
    }
}