using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Repositories
{
    /// <summary>
    /// 诊疗数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class ConsultationRepository : BaseApiRepository<ConsultationDto>, IConsultationRepository
    {
        public ConsultationRepository(
            IApiService apiService,
            ILogger<ConsultationRepository> logger)
            : base(apiService, logger, "api/v1/consultations")
        {
        }

        public override Task<PagedResult<ConsultationDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return base.GetPagedAsync(page, pageSize, keyword);
        }

        public override Task<ConsultationDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public async Task<ConsultationDto> CreateAsync(ConsultationCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return (await _apiService.PostAsync<ConsultationCreateDto, ConsultationDto>(_endpoint, dto))!;
        }

        public async Task<ConsultationDto> UpdateAsync(ConsultationUpdateDto dto)
        {
            if (dto?.Id == null || dto.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update consultation with null or invalid id");
                throw new ArgumentException("Consultation ID is required", nameof(dto));
            }

            return (await _apiService.PutAsync<ConsultationUpdateDto, ConsultationDto>($"{_endpoint}/{dto.Id}", dto))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
        }

        public override Task<List<ConsultationDto>> SearchAsync(string keyword)
        {
            return base.SearchAsync(keyword);
        }

        public async Task<List<ConsultationDto>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var result = await _apiService.GetAsync<List<ConsultationDto>>($"{_endpoint}/medicalcase/{medicalCaseId}");
                return result ?? new List<ConsultationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案诊疗记录失败: {MedicalCaseId}", medicalCaseId);
                return new List<ConsultationDto>();
            }
        }

        public async Task<ConsultationDto> StartAsync(Guid patientId)
        {
            return (await _apiService.PostAsync<object, ConsultationDto>($"{_endpoint}/start", new { PatientId = patientId }))!;
        }
    }
}
