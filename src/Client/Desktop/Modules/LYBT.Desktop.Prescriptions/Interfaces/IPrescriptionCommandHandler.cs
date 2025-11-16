using System.Windows.Input;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Modules.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方命令处理器接口
    /// Desktop层架构重构 Phase 1: 接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IPrescriptionCommandHandler
    {
        #region 事件定义

        /// <summary>
        /// 价格重算事件
        /// </summary>
        event Action? OnPriceRecalculated;

        /// <summary>
        /// 处方保存成功事件
        /// </summary>
        event Action? OnPrescriptionSaved;

        /// <summary>
        /// 处方清空事件
        /// </summary>
        event Action? OnPrescriptionCleared;

        /// <summary>
        /// 验方导入成功事件 (Issue #1368 ENTRY-10)
        /// </summary>
        event Action? OnFormulaImported;

        #endregion

        #region 命令定义

        /// <summary>
        /// 重新计算命令
        /// </summary>
        ICommand RecalculateCommand { get; }

        /// <summary>
        /// 打印预览命令
        /// </summary>
        ICommand PrintPreviewCommand { get; }

        /// <summary>
        /// 保存命令
        /// </summary>
        ICommand SaveCommand { get; }

        /// <summary>
        /// 清空命令
        /// </summary>
        ICommand ClearCommand { get; }

        /// <summary>
        /// 添加药材命令
        /// </summary>
        ICommand AddHerbCommand { get; }

        /// <summary>
        /// 移除药材命令
        /// </summary>
        ICommand RemoveHerbCommand { get; }

        /// <summary>
        /// 导入验方命令
        /// </summary>
        ICommand ImportFormulaCommand { get; }

        /// <summary>
        /// 生成处方编号命令
        /// </summary>
        ICommand GeneratePrescriptionNoCommand { get; }

        /// <summary>
        /// 验证命令
        /// </summary>
        ICommand ValidateCommand { get; }

        #endregion

        #region 业务方法

        /// <summary>
        /// 设置依赖（DataManager）
        /// </summary>
        void SetDependencies(ViewModels.Components.PrescriptionDataManager dataManager);

        /// <summary>
        /// 创建处方
        /// </summary>
        Task<ViewModels.Components.CommandResult<PrescriptionDto>> CreatePrescriptionAsync(
            Guid medicalCaseId,
            string prescriptionNumber,
            Guid patientId,
            string patientName,
            string doctorName,
            IEnumerable<ViewModels.PrescriptionItemViewModel> items,
            string? notes = null);

        /// <summary>
        /// 更新处方
        /// </summary>
        Task<ViewModels.Components.CommandResult<PrescriptionDto>> UpdatePrescriptionAsync(
            Guid prescriptionId,
            string prescriptionNumber,
            IEnumerable<ViewModels.PrescriptionItemViewModel> items,
            string? notes = null);

        /// <summary>
        /// 删除处方
        /// </summary>
        Task<ViewModels.Components.CommandResult<bool>> DeletePrescriptionAsync(Guid prescriptionId);

        /// <summary>
        /// 批量删除处方
        /// </summary>
        Task<bool> BatchDeletePrescriptionsAsync(IEnumerable<Guid> prescriptionIds);

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        Task<(bool success, List<PrescriptionDto>? data, string? errorMessage)> GetPrescriptionsByPatientAsync(Guid patientId);

        #endregion
    }
}
