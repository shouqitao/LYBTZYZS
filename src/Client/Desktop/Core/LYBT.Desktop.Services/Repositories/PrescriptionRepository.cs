using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 处方数据仓储实现 - API集成 - UltraThink架构
    /// </summary>
    public class PrescriptionRepository : BaseApiRepository<PrescriptionDto>, IPrescriptionRepository
    {
        public PrescriptionRepository(
            IApiService apiService,
            ILogger<PrescriptionRepository> logger)
            : base(apiService, logger, "api/Prescriptions")
        {
        }

        public override Task<List<PrescriptionDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<PrescriptionDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public override Task<PrescriptionDto> CreateAsync(PrescriptionDto prescription)
        {
            return base.CreateAsync(prescription);
        }

        public Task<PrescriptionDto> UpdateAsync(PrescriptionDto prescription)
        {
            if (prescription?.Id == null)
            {
                _logger.LogError("Cannot update prescription with null or invalid id");
                return Task.FromResult<PrescriptionDto>(null!);
            }
            return base.UpdateAsync(prescription.Id, prescription);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<PrescriptionDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<PrescriptionDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var result = await _apiService.GetAsync<List<PrescriptionDto>>($"{_endpoint}/patient/{patientId}");
                return result ?? new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting prescriptions for patient: {patientId}");
                return new List<PrescriptionDto>();
            }
        }

        public async Task<List<PrescriptionDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = new { startDate, endDate };
                var result = await _apiService.GetAsync<List<PrescriptionDto>>($"{_endpoint}/daterange", query);
                return result ?? new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting prescriptions by date range: {startDate} - {endDate}");
                return new List<PrescriptionDto>();
            }
        }

        public async Task<PrescriptionDto> DuplicatePrescriptionAsync(Guid prescriptionId)
        {
            try
            {
                return (await _apiService.PostAsync<object, PrescriptionDto>($"{_endpoint}/{prescriptionId}/duplicate", null!))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"复制处方失败: {prescriptionId}");
                throw;
            }
        }
    }
}
