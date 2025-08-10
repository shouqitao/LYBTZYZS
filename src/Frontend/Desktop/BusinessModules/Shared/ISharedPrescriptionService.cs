using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.WPF.Client.BusinessModules.Shared
{
    /// <summary>
    /// 共享处方服务接口
    /// 提供跨工作台的处方管理功能
    /// </summary>
    public interface ISharedPrescriptionService
    {
        /// <summary>
        /// 创建新处方
        /// </summary>
        /// <param name="dto">处方信息</param>
        /// <returns>创建的处方信息</returns>
        Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionDto dto);

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>处方详细信息</returns>
        Task<ServiceResult<PrescriptionDto>> GetPrescriptionByIdAsync(Guid prescriptionId);

        /// <summary>
        /// 获取患者的处方历史
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>处方历史列表</returns>
        Task<ServiceResult<List<PrescriptionDto>>> GetPatientPrescriptionHistoryAsync(Guid patientId, int limit = 10);

        /// <summary>
        /// 基于验方创建处方
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <param name="patientId">患者ID</param>
        /// <param name="adjustments">药材调整信息</param>
        /// <returns>创建的处方信息</returns>
        Task<ServiceResult<PrescriptionDto>> CreatePrescriptionFromFormulaAsync(
            Guid formulaId, 
            Guid patientId, 
            Dictionary<Guid, decimal> adjustments = null);

        /// <summary>
        /// 验证处方合理性
        /// 包括剂量、配伍禁忌等
        /// </summary>
        /// <param name="dto">处方信息</param>
        /// <returns>验证结果</returns>
        Task<ServiceResult<List<string>>> ValidatePrescriptionAsync(PrescriptionDto dto);

        /// <summary>
        /// 计算处方价格
        /// </summary>
        /// <param name="dto">处方信息</param>
        /// <returns>价格信息</returns>
        Task<ServiceResult<decimal>> CalculatePrescriptionPriceAsync(PrescriptionDto dto);

        /// <summary>
        /// 保存处方草稿
        /// </summary>
        /// <param name="dto">处方信息</param>
        /// <returns>保存结果</returns>
        Task<ServiceResult<Guid>> SavePrescriptionDraftAsync(PrescriptionDto dto);

        /// <summary>
        /// 获取处方草稿
        /// </summary>
        /// <param name="draftId">草稿ID</param>
        /// <returns>草稿处方信息</returns>
        Task<ServiceResult<PrescriptionDto>> GetPrescriptionDraftAsync(Guid draftId);

        /// <summary>
        /// 提交处方
        /// 将草稿转为正式处方
        /// </summary>
        /// <param name="draftId">草稿ID</param>
        /// <returns>正式处方信息</returns>
        Task<ServiceResult<PrescriptionDto>> SubmitPrescriptionAsync(Guid draftId);

        /// <summary>
        /// 复制历史处方
        /// </summary>
        /// <param name="prescriptionId">原处方ID</param>
        /// <param name="patientId">新患者ID</param>
        /// <returns>复制的处方信息</returns>
        Task<ServiceResult<PrescriptionDto>> CopyPrescriptionAsync(Guid prescriptionId, Guid patientId);

        /// <summary>
        /// 获取处方模板
        /// 常用处方组合
        /// </summary>
        /// <param name="category">分类</param>
        /// <returns>处方模板列表</returns>
        Task<ServiceResult<List<PrescriptionDto>>> GetPrescriptionTemplatesAsync(string category);

        /// <summary>
        /// 打印处方
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>打印结果</returns>
        Task<ServiceResult> PrintPrescriptionAsync(Guid prescriptionId);

        /// <summary>
        /// 作废处方
        /// </summary>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="reason">作废原因</param>
        /// <returns>作废结果</returns>
        Task<ServiceResult> VoidPrescriptionAsync(Guid prescriptionId, string reason);
    }
}