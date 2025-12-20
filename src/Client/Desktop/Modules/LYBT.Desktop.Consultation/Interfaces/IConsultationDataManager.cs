using LYBT.Desktop.Contracts.Components;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// 诊断数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IConsultationDataManager : IDataManager<ConsultationDetailDto>
    {
        /// <summary>
        /// 医案ID（聚合根ID）
        /// </summary>
        Guid MedicalCaseId { get; set; }

        // CompleteStep1Async已移除 - 简化业务流程，移除Step概念

        /// <summary>
        /// 更新诊断信息
        /// </summary>
        void UpdateConsultation(ConsultationDetailDto consultation);

        /// <summary>
        /// 更新单个字段
        /// </summary>
        void UpdateField(string fieldName, string? value);
    }
}
