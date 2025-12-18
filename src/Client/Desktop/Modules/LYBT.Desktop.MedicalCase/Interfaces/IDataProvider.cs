using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 数据提供者接口 - 替代ISaveable
    /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.1)
    /// </summary>
    /// <remarks>
    /// 设计理念：
    /// 1. Panel仅负责数据收集，不进行独立API调用
    /// 2. 由MedicalCaseWorkspaceCoordinator统一调用聚合保存API
    /// 3. 支持空数据返回（null表示该部分无数据）
    /// </remarks>
    public interface IDataProvider
    {
        /// <summary>
        /// 获取诊断数据（四诊信息）
        /// </summary>
        /// <returns>诊断数据DTO，无数据时返回null</returns>
        ConsultationInputDto? GetConsultationData();

        /// <summary>
        /// 获取处方数据（药材项、剂数、用法等）
        /// </summary>
        /// <returns>处方聚合输入DTO，无数据时返回null</returns>
        PrescriptionAggregateInputDto? GetPrescriptionData();
    }
}
