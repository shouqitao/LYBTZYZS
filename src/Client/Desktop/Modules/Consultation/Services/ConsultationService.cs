using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationApi _consultationApi;
        private readonly ILogger<ConsultationService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public ConsultationService(
            IConsultationApi consultationApi,
            ILogger<ConsultationService> logger,
            IExceptionHandler exceptionHandler)
        {
            _consultationApi = consultationApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<ConsultationDto>>(async () =>
            {
                var response = await _consultationApi.GetConsultationsAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<ConsultationDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                var response = await _consultationApi.GetConsultationByIdAsync(id);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                var response = await _consultationApi.CreateConsultationAsync(dto);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                var response = await _consultationApi.UpdateConsultationAsync(id, dto);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _consultationApi.DeleteConsultationAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _exceptionHandler.HandleException<List<ConsultationDto>>(async () =>
            {
                // TODO: 当API实现后，调用 _consultationApi.GetConsultationsByMedicalCaseIdAsync(medicalCaseId)
                // 暂时返回空列表以通过编译
                await Task.CompletedTask;
                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }, nameof(GetByMedicalCaseIdAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> StartAsync(Guid patientId)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                // 创建新的诊疗会话
                var createDto = new ConsultationCreateDto
                {
                    PatientId = patientId,
                    MedicalCaseId = Guid.NewGuid(), // 临时生成，实际应该由API处理
                    UserId = Guid.Empty, // TODO: 应该从当前登录用户获取
                    StartTime = DateTime.Now
                };

                var response = await _consultationApi.CreateConsultationAsync(createDto);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(StartAsync));
        }
    }
}