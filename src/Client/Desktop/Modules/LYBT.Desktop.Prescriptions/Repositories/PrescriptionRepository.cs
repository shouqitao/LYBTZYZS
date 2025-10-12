using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Repositories
{
    /// <summary>
    /// 处方数据仓储实现 - ADR-002合规版本
    /// 直接调用IPrescriptionApi（Refit HTTP客户端），符合架构决策
    /// </summary>
    public class PrescriptionRepository : IPrescriptionRepository
    {
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly ILogger<PrescriptionRepository> _logger;

        public PrescriptionRepository(
            IPrescriptionApi prescriptionApi,
            ILogger<PrescriptionRepository> logger)
        {
            _prescriptionApi = prescriptionApi;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        public async Task<PrescriptionDto> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _prescriptionApi.GetPrescriptionByIdAsync(id);
                return response.Content ?? throw new InvalidOperationException($"处方 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新处方（使用CreateDto）
        /// </summary>
        public async Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _prescriptionApi.CreatePrescriptionAsync(dto);
                return response.Content ?? throw new InvalidOperationException("创建处方失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                throw;
            }
        }

        /// <summary>
        /// 更新处方信息（使用UpdateDto）
        /// </summary>
        public async Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            if (id == Guid.Empty)
            {
                _logger.LogError("Cannot update prescription with invalid id");
                throw new ArgumentException("Prescription ID is required", nameof(id));
            }
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _prescriptionApi.UpdatePrescriptionAsync(id, dto);
                return response.Content ?? throw new InvalidOperationException($"更新处方失败，ID: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除处方（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _prescriptionApi.DeletePrescriptionAsync(id);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败，ID: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 分页查询处方列表（服务端分页）
        /// </summary>
        public async Task<PagedResult<PrescriptionDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await _prescriptionApi.GetPrescriptionsAsync(page, pageSize, keyword);
                return response.Content ?? new PagedResult<PrescriptionDto>
                {
                    Items = new List<PrescriptionDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询处方失败，Page: {Page}, PageSize: {PageSize}, Keyword: {Keyword}",
                    page, pageSize, keyword);
                throw;
            }
        }

        /// <summary>
        /// 根据医案ID获取处方列表
        /// </summary>
        public async Task<List<PrescriptionDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var response = await _prescriptionApi.GetPrescriptionsByMedicalCaseIdAsync(medicalCaseId);
                return response.Content ?? new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案处方列表失败: {MedicalCaseId}", medicalCaseId);
                return new List<PrescriptionDto>();
            }
        }
    }
}
