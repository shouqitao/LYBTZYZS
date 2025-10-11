using LYBT.Desktop.Foundation.Http;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories
{
    /// <summary>
    /// 医疗案例数据仓储实现 - Phase 2模块化架构
    /// Issue #1114 - 支持CreateDto和UpdateDto
    /// </summary>
    public class MedicalCaseRepository : BaseApiRepository<MedicalCaseDto>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(
            IApiService apiService,
            ILogger<MedicalCaseRepository> logger)
            : base(apiService, logger, "api/v1/medicalcases")
        {
        }

        public override Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return base.GetPagedAsync(page, pageSize, keyword);
        }

        public override Task<MedicalCaseDto> GetByIdAsync(Guid id)
        {
            return base.GetByIdAsync(id);
        }

        public async Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return (await _apiService.PostAsync<MedicalCaseCreateDto, MedicalCaseDto>(_endpoint, dto))!;
        }

        public async Task<MedicalCaseDto> UpdateAsync(MedicalCaseUpdateDto dto)
        {
            if (dto?.Id == null || dto.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update medical case with null or invalid id");
                throw new ArgumentException("MedicalCase ID is required", nameof(dto));
            }

            return (await _apiService.PutAsync<MedicalCaseUpdateDto, MedicalCaseDto>($"{_endpoint}/{dto.Id}", dto))!;
        }

        public override Task<bool> DeleteAsync(Guid id)
        {
            return base.DeleteAsync(id);
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
                _logger.LogError(ex, "获取患者医案列表失败: {PatientId}", patientId);
                return new List<MedicalCaseDto>();
            }
        }

        public async Task<MedicalCaseDto> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null)
        {
            var payload = new
            {
                MedicalCase = caseDto,
                Consultation = consultationDto,
                Prescription = prescriptionDto
            };

            return (await _apiService.PostAsync<object, MedicalCaseDto>($"{_endpoint}/with-details", payload))!;
        }

        public async Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id)
        {
            return (await _apiService.GetAsync<MedicalCaseDetailDto>($"{_endpoint}/{id}/details"))!;
        }
    }
}
