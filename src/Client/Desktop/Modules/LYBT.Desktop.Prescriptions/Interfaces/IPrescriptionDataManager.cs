using System.Collections.ObjectModel;
using LYBT.Desktop.Modules.Prescriptions.ViewModels;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Modules.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// ⚠️ Issue #1608: 待重构完成后完善接口定义
    /// </summary>
    public interface IPrescriptionDataManager
    {
        /// <summary>
        /// 医案ID
        /// </summary>
        Guid MedicalCaseId { get; }

        /// <summary>
        /// 处方ID
        /// </summary>
        Guid PrescriptionId { get; }

        /// <summary>
        /// 当前处方数据
        /// </summary>
        PrescriptionDto? CurrentPrescription { get; }

        /// <summary>
        /// 是否为新处方
        /// </summary>
        bool IsNewPrescription { get; }

        /// <summary>
        /// 处方编号
        /// </summary>
        string? PrescriptionNumber { get; }

        /// <summary>
        /// 处方项集合
        /// </summary>
        ObservableCollection<PrescriptionItemViewModel> PrescriptionItems { get; }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// 是否有未保存的变更
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// 初始化处方数据
        /// </summary>
        Task InitializeAsync(Guid medicalCaseId);
    }
}
