using LYBT.Desktop.Infrastructure.Interfaces.Components;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// 诊断命令处理器接口
    /// Desktop层架构重构 Phase 1: 接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IConsultationCommandHandler : ICommandHandler
    {
        /// <summary>
        /// 保存诊断数据（带验证）
        /// </summary>
        Task<bool> SaveAsync(bool validate = true);

        /// <summary>
        /// 重新加载诊断数据
        /// </summary>
        Task<bool> ReloadAsync();

        /// <summary>
        /// 清空表单
        /// </summary>
        void ClearForm();

        /// <summary>
        /// 完成Step1（辨证）
        /// </summary>
        Task<bool> CompleteStep1Async(bool prescriptionEnabled);

        /// <summary>
        /// 保存草稿（不完成Step1）
        /// </summary>
        Task<bool> SaveDraftAsync();

        /// <summary>
        /// 导航到处方录入页（Step2）
        /// </summary>
        Task<bool> NavigateToPrescriptionEditorAsync(Guid medicalCaseId, object? currentPatient = null);
    }
}
