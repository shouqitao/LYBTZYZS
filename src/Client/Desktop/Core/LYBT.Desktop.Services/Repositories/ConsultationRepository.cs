using LYBT.Desktop.Services.Http;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Repositories
{
    /// <summary>
    /// 问诊数据仓储实现 - API集成 - UltraThink架构
    /// </summary>
    public class ConsultationRepository : BaseApiRepository<ConsultationDto>, IConsultationRepository
    {
        public ConsultationRepository(
            IApiService apiService,
            ILogger<ConsultationRepository> logger)
            : base(apiService, logger, "api/Consultation")
        {
        }

        public override Task<List<ConsultationDto>> GetAllAsync()
        {
            return base.GetAllAsync();
        }

        public override Task<ConsultationDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public override Task<ConsultationDto> CreateAsync(ConsultationDto consultation)
        {
            return base.CreateAsync(consultation);
        }

        public Task<ConsultationDto> UpdateAsync(ConsultationDto consultation)
        {
            if (consultation?.Id == null)
            {
                _logger.LogError("Cannot update consultation with null or invalid id");
                return Task.FromResult<ConsultationDto>(null!);
            }
            return base.UpdateAsync(consultation.Id, consultation);
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<ConsultationDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<ConsultationDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var result = await _apiService.GetAsync<List<ConsultationDto>>($"{_endpoint}/patient/{patientId}");
                return result ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting consultations for patient: {patientId}");
                return new List<ConsultationDto>();
            }
        }

        public async Task<List<ConsultationDto>> GetTodayConsultationsAsync()
        {
            try
            {
                var result = await _apiService.GetAsync<List<ConsultationDto>>($"{_endpoint}/today");
                return result ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's consultations");
                return new List<ConsultationDto>();
            }
        }

        public async Task<ConsultationDto> GetActiveConsultationAsync(Guid patientId)
        {
            try
            {
                return (await _apiService.GetAsync<ConsultationDto>($"{_endpoint}/patient/{patientId}/active"))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取患者活跃诊疗失败: {patientId}");
                throw;
            }
        }

        public async Task<bool> CompleteConsultationAsync(Guid consultationId)
        {
            try
            {
                await _apiService.PostAsync<object, object>($"{_endpoint}/{consultationId}/complete", null!);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing consultation: {consultationId}");
                return false;
            }
        }

        public async Task<List<ConsultationDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = new { startDate, endDate };
                var result = await _apiService.GetAsync<List<ConsultationDto>>($"{_endpoint}/daterange", query);
                return result ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting consultations by date range: {startDate} - {endDate}");
                return new List<ConsultationDto>();
            }
        }

        public async Task<ConsultationDto> GetLatestByPatientIdAsync(Guid patientId)
        {
            try
            {
                return (await _apiService.GetAsync<ConsultationDto>($"{_endpoint}/patient/{patientId}/latest"))!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取患者最新诊疗失败: {patientId}");
                throw;
            }
        }
    }
}
