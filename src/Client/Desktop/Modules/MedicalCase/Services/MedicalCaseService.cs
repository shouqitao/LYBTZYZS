using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseApi _medicalCaseApi;
        private readonly ILogger<MedicalCaseService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public MedicalCaseService(
            IMedicalCaseApi medicalCaseApi,
            ILogger<MedicalCaseService> logger,
            IExceptionHandler exceptionHandler)
        {
            _medicalCaseApi = medicalCaseApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<MedicalCaseDto>>(async () =>
            {
                var response = await _medicalCaseApi.GetMedicalCasesAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
                var response = await _medicalCaseApi.GetMedicalCaseByIdAsync(id);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
                var response = await _medicalCaseApi.CreateMedicalCaseAsync(dto);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
                var response = await _medicalCaseApi.UpdateMedicalCaseAsync(id, dto);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _medicalCaseApi.DeleteMedicalCaseAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await _exceptionHandler.HandleException<List<MedicalCaseDto>>(async () =>
            {
                // TODO: 当API实现后，调用 _medicalCaseApi.GetMedicalCasesByPatientIdAsync(patientId)
                // 暂时返回空列表以通过编译
                await Task.CompletedTask;
                return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            }, nameof(GetByPatientIdAsync));
        }

<<<<<<< HEAD
        /// <summary>
        /// 获取包含详情的医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDetailDto>(async () =>
            {
                var response = await _medicalCaseApi.GetMedicalCaseByIdWithDetailsAsync(id);
                return ServiceResult<MedicalCaseDetailDto>.Success(response.Content);
            }, nameof(GetByIdWithDetailsAsync));
        }

        /// <summary>
        /// 创建包含详情的医疗案例（包含诊疗和可选处方）
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto, 
            ConsultationCreateDto consultationDto, 
=======
        public async Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
>>>>>>> feature/medical-case-aggregate-root
            PrescriptionCreateDto prescriptionDto = null)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
<<<<<<< HEAD
                // 组装聚合根DTO
                var dto = new MedicalCaseWithDetailsCreateDto
                {
                    MedicalCase = caseDto,
                    Consultation = consultationDto,
                    Prescription = prescriptionDto
                };
                
                var response = await _medicalCaseApi.CreateMedicalCaseWithDetailsAsync(dto);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(CreateWithDetailsAsync));
        }
=======
                // TODO: 当API实现后，调用API的聚合创建方法
                // 暂时只创建基础的医疗案例
                var response = await _medicalCaseApi.CreateMedicalCaseAsync(caseDto);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(CreateWithDetailsAsync));
        }

        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDetailDto>(async () =>
            {
                // TODO: 当API实现后，调用API的详细查询方法
                // 暂时返回基础医疗案例数据，只映射确实存在的字段
                var basicResponse = await _medicalCaseApi.GetMedicalCaseByIdAsync(id);
                var detailDto = new MedicalCaseDetailDto
                {
                    // 从基础DTO继承的属性 - 只映射确实存在的字段
                    Id = basicResponse.Content.Id,
                    PatientId = basicResponse.Content.PatientId,
                    PatientName = basicResponse.Content.PatientName,
                    DoctorId = basicResponse.Content.DoctorId,
                    DoctorName = basicResponse.Content.DoctorName,
                    ConsultationId = basicResponse.Content.ConsultationId,
                    PrescriptionId = basicResponse.Content.PrescriptionId,
                    ConsultationDate = basicResponse.Content.ConsultationDate,
                    CaseStatus = basicResponse.Content.CaseStatus,
                    Status = basicResponse.Content.Status,
                    Remark = basicResponse.Content.Remark,

                    // MedicalCaseDetailDto特有的属性
                    ChiefComplaint = null, // TODO: 从API获取详细信息
                    PresentIllness = null, // TODO: 从API获取详细信息
                    DiagnosisResult = null, // TODO: 从API获取详细信息
                    TreatmentPlan = null // TODO: 从API获取详细信息
                };
                return ServiceResult<MedicalCaseDetailDto>.Success(detailDto);
            }, nameof(GetByIdWithDetailsAsync));
        }
>>>>>>> feature/medical-case-aggregate-root
    }
}