using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 病历数据仓储实现 - API集成 - UltraThink架构
    /// </summary>
    public class MedicalCaseRepository : BaseApiRepository<MedicalCaseDto>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(
            IApiService apiService,
            ILogger<MedicalCaseRepository> logger)
            : base(apiService, logger, "api/v1/medicalcases")
        {
        }

        public override Task<List<MedicalCaseDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<MedicalCaseDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public override Task<MedicalCaseDto> CreateAsync(MedicalCaseDto medicalCase)
        {
            return base.CreateAsync(medicalCase);
        }

        public Task<MedicalCaseDto> UpdateAsync(MedicalCaseDto medicalCase)
        {
            if (medicalCase?.Id == null)
            {
                _logger.LogError("Cannot update medical case with null or invalid id");
                return Task.FromResult<MedicalCaseDto>(null!);
            }
            return base.UpdateAsync(medicalCase.Id, medicalCase);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<MedicalCaseDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var result = await _apiService.GetAsync<List<MedicalCaseDto>>($"{_endpoint}/patient/{patientId}");
                return result ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting medical cases for patient: {patientId}");
                return new List<MedicalCaseDto>();
            }
        }

        public async Task<List<MedicalCaseDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = new { startDate, endDate };
                var result = await _apiService.GetAsync<List<MedicalCaseDto>>($"{_endpoint}/daterange", query);
                return result ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting medical cases by date range: {startDate} - {endDate}");
                return new List<MedicalCaseDto>();
            }
        }

        public async Task<MedicalCaseDto> GetLatestByPatientIdAsync(Guid patientId)
        {
            try
            {
                return (await _apiService.GetAsync<MedicalCaseDto>($"{_endpoint}/patient/{patientId}/latest"))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取患者最新病历失败: {patientId}");
                throw;
            }
        }
    }
}
