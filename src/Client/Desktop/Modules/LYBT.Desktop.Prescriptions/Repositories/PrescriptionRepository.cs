using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Repositories
{
    /// <summary>
    /// 处方数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class PrescriptionRepository : BaseApiRepository<PrescriptionDto>, IPrescriptionRepository
    {
        public PrescriptionRepository(
            IApiService apiService,
            ILogger<PrescriptionRepository> logger)
            : base(apiService, logger, "api/v1/prescriptions")
        {
        }

        public override Task<PagedResult<PrescriptionDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return base.GetPagedAsync(page, pageSize, keyword);
        }

        public override Task<PrescriptionDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public async Task<PrescriptionDto> CreateAsync(PrescriptionCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return (await _apiService.PostAsync<PrescriptionCreateDto, PrescriptionDto>(_endpoint, dto))!;
        }

        public async Task<PrescriptionDto> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            if (id == Guid.Empty)
            {
                _logger.LogError("Cannot update prescription with invalid id");
                throw new ArgumentException("Prescription ID is required", nameof(id));
            }
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return (await _apiService.PutAsync<PrescriptionUpdateDto, PrescriptionDto>($"{_endpoint}/{id}", dto))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public async Task<List<PrescriptionDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var result = await _apiService.GetAsync<List<PrescriptionDto>>($"{_endpoint}/medicalcase/{medicalCaseId}");
                return result ?? new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案处方列表失败: {MedicalCaseId}", medicalCaseId);
                return new List<PrescriptionDto>();
            }
        }
    }
}
