using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories
{
    /// <summary>
    /// 医疗案例数据仓储实现 - ADR-002合规版本
    /// 直接调用IMedicalCaseApi（Refit HTTP客户端），符合架构决策
    /// </summary>
    public class MedicalCaseRepository : IMedicalCaseRepository
    {
        private readonly IMedicalCaseApi _medicalCaseApi;
        private readonly ILogger<MedicalCaseRepository> _logger;

        public MedicalCaseRepository(
            IMedicalCaseApi medicalCaseApi,
            ILogger<MedicalCaseRepository> logger)
        {
            _medicalCaseApi = medicalCaseApi;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        public async Task<MedicalCaseDto> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _medicalCaseApi.GetMedicalCaseByIdAsync(id);
                return response.Content ?? throw new InvalidOperationException($"医疗案例 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取医疗案例详情（含关联数据）
        /// </summary>
        public async Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id)
        {
            try
            {
                var response = await _medicalCaseApi.GetMedicalCaseByIdWithDetailsAsync(id);
                return response.Content ?? throw new InvalidOperationException($"医疗案例 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情（含关联数据）失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        public async Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var response = await _medicalCaseApi.GetMedicalCasesByPatientIdAsync(patientId);
                return response.Content ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例列表失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 创建新医疗案例（使用CreateDto）
        /// </summary>
        public async Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto medicalCase)
        {
            if (medicalCase == null)
                throw new ArgumentNullException(nameof(medicalCase));

            try
            {
                var response = await _medicalCaseApi.CreateMedicalCaseAsync(medicalCase);
                return response.Content ?? throw new InvalidOperationException("创建医疗案例失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                throw;
            }
        }

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗和可选处方）
        /// </summary>
        public async Task<MedicalCaseDto> CreateWithDetailsAsync(
            MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null)
        {
            if (caseDto == null)
                throw new ArgumentNullException(nameof(caseDto));
            if (consultationDto == null)
                throw new ArgumentNullException(nameof(consultationDto));

            try
            {
                // 构造完整请求DTO
                var request = new MedicalCaseWithDetailsCreateDto
                {
                    MedicalCase = caseDto,
                    Consultation = consultationDto,
                    Prescription = prescriptionDto
                };

                var response = await _medicalCaseApi.CreateMedicalCaseWithDetailsAsync(request);
                return response.Content ?? throw new InvalidOperationException("创建完整医疗案例失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建完整医疗案例失败");
                throw;
            }
        }

        /// <summary>
        /// 更新医疗案例信息（使用UpdateDto）
        /// </summary>
        public async Task<MedicalCaseDto> UpdateAsync(MedicalCaseUpdateDto medicalCase)
        {
            if (medicalCase?.Id == null || medicalCase.Id == Guid.Empty)
            {
                _logger.LogError("Cannot update medical case with null or invalid id");
                throw new ArgumentException("MedicalCase ID is required", nameof(medicalCase));
            }

            try
            {
                var response = await _medicalCaseApi.UpdateMedicalCaseAsync(medicalCase.Id, medicalCase);
                return response.Content ?? throw new InvalidOperationException($"更新医疗案例失败，ID: {medicalCase.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败，ID: {Id}", medicalCase.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _medicalCaseApi.DeleteMedicalCaseAsync(id);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败，ID: {Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 分页查询医疗案例列表（服务端分页）
        /// </summary>
        public async Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await _medicalCaseApi.GetMedicalCasesAsync(page, pageSize, keyword);
                return response.Content ?? new PagedResult<MedicalCaseDto>
                {
                    Items = new List<MedicalCaseDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败，Page: {Page}, PageSize: {PageSize}, Keyword: {Keyword}",
                    page, pageSize, keyword);
                throw;
            }
        }
    }
}
