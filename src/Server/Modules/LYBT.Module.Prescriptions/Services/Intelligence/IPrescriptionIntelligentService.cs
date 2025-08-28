using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Services.Intelligence
{
    /// <summary>
    /// 处方智能服务接口
    /// UltraThink重构：专注于处方的智能检查、验证和辅助功能
    /// </summary>
    public interface IPrescriptionIntelligentService
    {
        /// <summary>
        /// 执行智能检查（药材重复和可用性检查）
        /// </summary>
        /// <param name="items">处方药材项目</param>        /// <param name="prescriptionId">处方ID</param>        /// <param name="operatorName">操作者姓名</param>        /// <returns>检查任务</returns>
        Task PerformIntelligentChecksAsync(List<PrescriptionItemCreateDto> items, Guid prescriptionId, string operatorName);

        /// <summary>
        /// 更新关联医疗案例状态
        /// </summary>        /// <param name="medicalCaseId">医疗案例ID</param>        /// <param name="statusRemark">状态备注</param>        /// <returns>更新任务</returns>
        Task UpdateMedicalCaseStatusAsync(Guid medicalCaseId, string statusRemark);

        /// <summary>
        /// 检测药材重复
        /// </summary>        /// <param name="items">处方药材项目</param>        /// <returns>重复检测结果</returns>
        Task<DuplicateHerbsResult> DetectDuplicateHerbsAsync(List<PrescriptionItemCreateDto> items);

        /// <summary>
        /// 检查药材可用性
        /// </summary>        /// <param name="items">处方药材项目</param>        /// <returns>可用性检查结果</returns>
        Task<HerbAvailabilityResult> CheckHerbAvailabilityAsync(List<PrescriptionItemCreateDto> items);

        /// <summary>
        /// 检查配伍禁忌
        /// </summary>        /// <param name="items">处方药材项目</param>
        /// <returns>配伍禁忌检查结果</returns>
        Task<HerbCompatibilityResult> CheckHerbCompatibilityAsync(List<PrescriptionItemCreateDto> items);
    }

    /// <summary>
    /// 药材重复检测结果
    /// </summary>
    public class DuplicateHerbsResult
    {
        public bool HasDuplicates { get; set; }
        public List<string> DuplicateHerbs { get; set; } = new();
    }

    /// <summary>
    /// 药材可用性检查结果
    /// </summary>
    public class HerbAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public List<string> UnavailableHerbs { get; set; } = new();
    }

    /// <summary>
    /// 药材配伍禁忌检查结果
    /// </summary>
    public class HerbCompatibilityResult
    {
        public bool HasContraindications { get; set; }
        public List<string> ContraindicationPairs { get; set; } = new();
        public string Warning { get; set; } = string.Empty;
    }
}
