using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 处方服务接口 - 统一定义
    /// </summary>
    public interface IPrescriptionService
    {
        Task<PrescriptionDto> GetByIdAsync(Guid id);
        Task<PagedResult<PrescriptionDto>> GetPagedAsync(PrescriptionQueryDto query);
        Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto);
        Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<PrescriptionDto>> GetByPatientIdAsync(Guid patientId);
        Task<List<PrescriptionDto>> GetByConsultationIdAsync(Guid consultationId);
        Task<PrescriptionValidationResult> ValidateAsync(PrescriptionCreateDto dto);
        Task<byte[]> ExportToPdfAsync(Guid id);
        Task<PrescriptionStatisticsDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
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