using AutoMapper;
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
        private readonly IMapper _mapper;

        public ConsultationService(
            IConsultationRepository repository,
            ILogger<ConsultationService> logger,
            IExceptionHandler exceptionHandler,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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

                // 使用 AutoMapper 转换 DTO
                var consultation = _mapper.Map<ConsultationDto>(dto);
                consultation.Id = Guid.NewGuid();

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

                // 使用 AutoMapper 更新字段
                _mapper.Map(dto, existing);

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

        /// <summary>
        /// 根据医案ID获取诊疗记录
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>诊疗记录列表</returns>
        /// <remarks>
        /// 优化后直接调用Server API，避免GetAll + 内存过滤的低效方式。
        /// 由于共享主键设计（Consultation.Id == MedicalCase.Id），通常返回单个记录。
        /// </remarks>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var consultations = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Success(consultations);
            }, nameof(GetByMedicalCaseIdAsync));
        }

        /// <summary>
        /// 开始新的诊疗会话
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>诊疗记录</returns>
        /// <remarks>
        /// ⚠️ 已废弃：请通过 MedicalCaseService 创建医疗案例，系统会自动创建关联的诊疗记录。
        /// MedicalCase 是聚合根，Consultation 作为其一部分使用共享主键模式（Consultation.Id == MedicalCase.Id）。
        /// 独立创建 Consultation 违反了聚合根架构原则。
        /// </remarks>
        [Obsolete("已废弃。请通过 MedicalCaseService 创建医疗案例，系统会自动创建诊疗记录。此方法保留用于向后兼容。", false)]
        public async Task<ServiceResult<ConsultationDto>> StartAsync(Guid patientId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogWarning("调用了已废弃的 StartAsync 方法。患者ID={PatientId}。建议改用 MedicalCaseService.CreateAsync()", patientId);

                // 创建新的诊疗记录（已废弃的实现，保留向后兼容）
                var consultation = new ConsultationDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    MedicalCaseId = Guid.Empty, // 需要关联到具体医案
                    UserId = Guid.Empty, // 需要设置当前医生
                    StartTime = DateTime.UtcNow,
                    ConsultationStatus = ConsultationStatus.InProgress,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(consultation);
                return ServiceResult<ConsultationDto>.Success(created);
            }, nameof(StartAsync));
        }

        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"搜索诊疗记录: {keyword}");

                var allConsultations = await _repository.GetAllAsync();
                var results = allConsultations.Where(c =>
                    (!string.IsNullOrEmpty(c.PatientName) && c.PatientName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.DoctorName) && c.DoctorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.ChiefComplaint) && c.ChiefComplaint.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(c.TCMDiagnosis) && c.TCMDiagnosis.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return ServiceResult<List<ConsultationDto>>.Success(results);
            }, nameof(SearchAsync));
        }
    }
}
