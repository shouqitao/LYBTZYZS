using AutoMapper;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 病历服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly ILogger<MedicalCaseService> _logger;
        private readonly IMedicalCaseRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        private readonly ILogger<MedicalCaseService> _logger;
        private readonly IMedicalCaseRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly IMapper _mapper;

        public MedicalCaseService(
            IMedicalCaseRepository repository,
            ILogger<MedicalCaseService> logger,
            IExceptionHandler exceptionHandler,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allCases = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    allCases = allCases.Where(c =>
                        (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (c.PatientName != null && c.PatientName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (c.CaseNumber != null && c.CaseNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 分页
                var totalCount = allCases.Count;
                var items = allCases
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedResult<MedicalCaseDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                return ServiceResult<MedicalCaseDto>.Success(medicalCase);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建病历: 患者ID={dto.PatientId}");

                // 使用 AutoMapper 转换 DTO
                var medicalCase = _mapper.Map<MedicalCaseDto>(dto);
                medicalCase.Id = Guid.NewGuid();
                medicalCase.PatientName = string.Empty; // 需要从Patient服务获取
                medicalCase.DoctorName = string.Empty; // 需要从User服务获取

                var created = await _repository.CreateAsync(medicalCase);
                return ServiceResult<MedicalCaseDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 使用 AutoMapper 更新字段
                _mapper.Map(dto, existing);

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<MedicalCaseDto>.Success(updated);
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

        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var cases = await _repository.GetByPatientIdAsync(patientId);
                return ServiceResult<List<MedicalCaseDto>>.Success(cases);
            }, nameof(GetByPatientIdAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
            MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建完整病历: 患者ID={caseDto.PatientId}");

                // TODO: 实现聚合根模式
                // 需要注入IConsultationService和IPrescriptionService
                // 或者使用事件模式/Saga模式协调多个服务

                // 当前简化实现：只创建MedicalCase
                var medicalCase = new MedicalCaseDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = caseDto.PatientId,
                    DoctorId = caseDto.DoctorId,
                    CaseNumber = caseDto.CaseNumber,
                    ChiefComplaint = caseDto.ChiefComplaint,
                    ConsultationDate = DateTime.UtcNow,
                    CaseStatus = caseDto.Status,
                    Remark = caseDto.Remark,
                    PatientName = string.Empty,
                    DoctorName = string.Empty,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(medicalCase);

                _logger.LogWarning("CreateWithDetailsAsync 需要实现Consultation和Prescription的创建逻辑");

                return ServiceResult<MedicalCaseDto>.Success(created);
            }, nameof(CreateWithDetailsAsync));
        }

        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"获取完整病历: ID={id}");

                // TODO: 实现聚合查询
                // 需要注入IConsultationService和IPrescriptionService
                // 或者使用Repository层的联表查询

                // 当前简化实现：只返回MedicalCase基础信息
                var medicalCase = await _repository.GetByIdAsync(id);

                var detailDto = new MedicalCaseDetailDto
                {
                    Id = medicalCase.Id,
                    PatientId = medicalCase.PatientId,
                    DoctorId = medicalCase.DoctorId,
                    PatientName = medicalCase.PatientName,
                    DoctorName = medicalCase.DoctorName,
                    CaseNumber = medicalCase.CaseNumber,
                    ChiefComplaint = medicalCase.ChiefComplaint,
                    ConsultationDate = medicalCase.ConsultationDate,
                    CaseStatus = medicalCase.CaseStatus,
                    Remark = medicalCase.Remark,
                    Status = medicalCase.Status,
                    CreatedAt = medicalCase.CreatedAt,
                    UpdatedAt = medicalCase.UpdatedAt
                };

                _logger.LogWarning("GetByIdWithDetailsAsync 需要实现Consultation和Prescription的加载逻辑");

                return ServiceResult<MedicalCaseDetailDto>.Success(detailDto);
            }, nameof(GetByIdWithDetailsAsync));
        }
    }
}
