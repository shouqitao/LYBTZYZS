using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 处方服务接口 - UltraThink统一标准
    /// </summary>
    public interface IPrescriptionService
    {
        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 分页查询处方
        /// </summary>
        Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);
        
        /// <summary>
        /// 创建新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);
        
        /// <summary>
        /// 更新处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto);
        
        /// <summary>
        /// 删除处方
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId);
        
        /// <summary>
        /// 根据看诊ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByConsultationIdAsync(Guid consultationId);
        
        /// <summary>
        /// 验证处方数据
        /// </summary>
        Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto);
        
        /// <summary>
        /// 导出处方为PDF
        /// </summary>
        Task<ServiceResult<byte[]>> ExportToPdfAsync(Guid id);
        
        /// <summary>
        /// 获取处方统计信息
        /// </summary>
        Task<ServiceResult<PrescriptionStatisticsDto>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
        
        /// <summary>
        /// 批准处方
        /// </summary>
        Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote);
        
        /// <summary>
        /// 拒绝处方
        /// </summary>
        Task<ServiceResult<bool>> RejectAsync(Guid id, string reason);
        
        /// <summary>
        /// 复制处方
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName);
        
        /// <summary>
        /// 搜索处方
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword);
    }

    /// <summary>
    /// 处方验证结果
    /// </summary>
    public class PrescriptionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}