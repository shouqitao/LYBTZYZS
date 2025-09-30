using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 问诊服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly ILogger<ConsultationService> _logger;
        private readonly IConsultationRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        public ConsultationService(
            IConsultationRepository repository,
            ILogger<ConsultationService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allConsultations = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    allConsultations = allConsultations.Where(c =>
                        (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (c.PatientName != null && c.PatientName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 分页
                var totalCount = allConsultations.Count;
                var items = allConsultations
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedResult<ConsultationDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<ConsultationDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var consultation = await _repository.GetByIdAsync(id);
                return ServiceResult<ConsultationDto>.Success(consultation);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建诊疗记录: 患者ID={dto.PatientId}");

                // 转换DTO
                var consultation = new ConsultationDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    MedicalCaseId = dto.MedicalCaseId,
                    UserId = dto.UserId,
                    PatientName = dto.PatientName,
                    DoctorName = dto.DoctorName,
                    StartTime = dto.StartTime,
                    ChiefComplaint = dto.ChiefComplaint,
                    PresentIllness = dto.PresentIllness,
                    Inspection = dto.Inspection,
                    AuscultationOlfaction = dto.AuscultationOlfaction,
                    Inquiry = dto.Inquiry,
                    Palpation = dto.Palpation,
                    TCMDiagnosis = dto.TCMDiagnosis,
                    TreatmentPrinciple = dto.TreatmentPrinciple,
                    Remark = dto.Remark,
                    ConsultationStatus = ConsultationStatus.InProgress,
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(consultation);
                return ServiceResult<ConsultationDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 更新字段
                existing.ChiefComplaint = dto.ChiefComplaint ?? existing.ChiefComplaint;
                existing.PresentIllness = dto.PresentIllness ?? existing.PresentIllness;
                existing.Inspection = dto.Inspection ?? existing.Inspection;
                existing.AuscultationOlfaction = dto.AuscultationOlfaction ?? existing.AuscultationOlfaction;
                existing.Inquiry = dto.Inquiry ?? existing.Inquiry;
                existing.Palpation = dto.Palpation ?? existing.Palpation;
                existing.TCMDiagnosis = dto.TCMDiagnosis ?? existing.TCMDiagnosis;
                existing.TreatmentPrinciple = dto.TreatmentPrinciple ?? existing.TreatmentPrinciple;
                existing.Remark = dto.Remark ?? existing.Remark;

                if (dto.ConsultationStatus.HasValue)
                {
                    existing.ConsultationStatus = dto.ConsultationStatus.Value;
                }

                if (dto.EndTime.HasValue)
                {
                    existing.EndTime = dto.EndTime;
                }

                existing.UpdateTime = DateTime.UtcNow;

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<ConsultationDto>.Success(updated);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // Repository需要扩展此方法，当前使用GetAll + 过滤
                var allConsultations = await _repository.GetAllAsync();
                var consultations = allConsultations
                    .Where(c => c.MedicalCaseId == medicalCaseId)
                    .ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }, nameof(GetByMedicalCaseIdAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> StartAsync(Guid patientId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"开始新的诊疗会话: 患者ID={patientId}");

                // 创建新的诊疗记录
                var consultation = new ConsultationDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    MedicalCaseId = Guid.Empty, // 需要关联到具体医案
                    UserId = Guid.Empty, // 需要设置当前医生
                    StartTime = DateTime.UtcNow,
                    ConsultationStatus = ConsultationStatus.InProgress,
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(consultation);
                return ServiceResult<ConsultationDto>.Success(created);
            }, nameof(StartAsync));
        }
    }
}
