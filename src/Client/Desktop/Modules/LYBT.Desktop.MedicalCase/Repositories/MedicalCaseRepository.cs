using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories
{
    /// <summary>
    /// 医疗案例数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class MedicalCaseRepository : RepositoryBase<MedicalCaseDto, MedicalCaseCreateDto, MedicalCaseUpdateDto, IMedicalCaseApi>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(
            IMedicalCaseApi medicalCaseApi,
            ILogger<MedicalCaseRepository> logger)
            : base(medicalCaseApi, logger)
        {
        }

        /// <summary>
        /// 根据ID获取医疗案例详情（含关联数据）
        /// </summary>
        public async Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id)
        {
            try
            {
                var response = await _api.GetMedicalCaseByIdWithDetailsAsync(id);
                return response.Data ?? throw new InvalidOperationException($"医疗案例 {id} 不存在");
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
                var response = await _api.GetMedicalCasesByPatientIdAsync(patientId);
                return response.Data ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例列表失败，PatientId: {PatientId}", patientId);
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

                var response = await _api.CreateMedicalCaseWithDetailsAsync(request);
                return response.Data ?? throw new InvalidOperationException("创建完整医疗案例失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建完整医疗案例失败");
                throw;
            }
        }

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        public async Task<ConsultationDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _api.UpdateConsultationAsync(medicalCaseId, dto);
                return response.Data ?? throw new InvalidOperationException("更新诊断信息失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医案诊断信息失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        public async Task<List<MedicalCaseDto>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null)
        {
            try
            {
                _logger.LogInformation("查询病案，条件：患者={PatientName}, 日期={StartDate}~{EndDate}, 诊断={DiagnosisKeyword}",
                    patientName ?? "无", startDate, endDate, diagnosisKeyword ?? "无");

                var response = await _api.QueryMedicalCasesAsync(patientName, startDate, endDate, diagnosisKeyword);
                return response.Data ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询病案列表失败");
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<MedicalCaseDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetMedicalCaseByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<MedicalCaseDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetMedicalCasesAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<MedicalCaseDto>> CallApiCreateAsync(MedicalCaseCreateDto dto)
        {
            return _api.CreateMedicalCaseAsync(dto);
        }

        protected override Task<ApiResponse<MedicalCaseDto>> CallApiUpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            return _api.UpdateMedicalCaseAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteMedicalCaseAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(MedicalCaseUpdateDto dto)
        {
            return dto?.Id;
        }

        #endregion
    }
}