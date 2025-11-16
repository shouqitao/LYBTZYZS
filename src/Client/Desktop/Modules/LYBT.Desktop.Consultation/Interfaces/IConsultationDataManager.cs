using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// 诊断数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IConsultationDataManager : IDataManager<ConsultationDto>
    {
        /// <summary>
        /// 医案ID（聚合根ID）
        /// </summary>
        Guid MedicalCaseId { get; set; }

        /// <summary>
        /// 完成Step1（辨证）
        /// </summary>
        Task<ConsultationStepDto> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request);

        /// <summary>
        /// 更新诊断信息
        /// </summary>
        void UpdateConsultation(ConsultationDto consultation);

        /// <summary>
        /// 更新单个字段
        /// </summary>
        void UpdateField(string fieldName, string? value);
    }
}
