using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案查询服务实现 - 读操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：GetById, GetList, Search等查询操作
    /// </summary>
    public class MedicalCaseQueryService : BaseService<MedicalCase>, IMedicalCaseQueryService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCaseQueryService(
            IMedicalCaseRepository repository,
            MedicalCaseMapper mapper,
            ILogger<MedicalCaseQueryService> logger)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper;
        }

        /// <summary>
        /// 查询医案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// Sprint3-X6: 全部筛选迁移到 Repository DB 层执行
        /// </summary>
        public async Task<PagedResult<MedicalCase>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null,
            CancellationToken cancellationToken = default)
        {
            // Sprint3-X6: 全部筛选在 DB 层执行，TotalCount 自然正确
            return await _repository.GetPagedWithDetailsAsync(
                page, pageSize, status, patientId, currentDoctorId, isAdmin, keyword, cancellationToken);
        }

        /// <summary>
        /// 查询医案列表（分页，返回MedicalCaseListDto，用于列表视图）
        /// Sprint3-X6: 复用 GetListAsync 结果 + 映射，消除重复代码
        /// </summary>
        public async Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null,
            CancellationToken cancellationToken = default)
        {
            // Sprint3-X6: 复用 GetListAsync（已在 DB 层完成全部筛选）
            var result = await GetListAsync(status, patientId, page, pageSize, currentDoctorId, isAdmin, keyword, cancellationToken);
            var dtos = _mapper.ToListDtos(result.Items.ToList());

            // Populate computed properties that Mapperly ignores
            for (int i = 0; i < result.Items.Count && i < dtos.Count; i++)
            {
                var entity = result.Items[i];
                dtos[i].HasConsultation = entity.Consultation != null && !entity.Consultation.IsDeleted;
                dtos[i].HasPrescription = entity.Prescription != null && !entity.Prescription.IsDeleted;
            }

            return new PagedResult<MedicalCaseListDto>
            {
                Items = dtos,
                TotalCount = result.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回医案的所有历史辨证记录
        /// </summary>
        public async Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId, cancellationToken);
            if (medicalCase?.Consultation == null)
            {
                return new List<ConsultationDetailDto>();
            }

            // 当前架构下只有一条Consultation（共享主键），直接映射
            var dto = _mapper.ToConsultationDetailDto(medicalCase.Consultation);
            return new List<ConsultationDetailDto> { dto };
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回医案的所有历史处方记录
        /// </summary>
        public async Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId, cancellationToken);
            if (medicalCase?.Prescription == null)
            {
                return new List<PrescriptionDetailDto>();
            }

            // 当前架构下只有一条Prescription（一诊一方），直接映射
            var dto = _mapper.ToPrescriptionDetailDto(medicalCase.Prescription);
            return new List<PrescriptionDetailDto> { dto };
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.2: 添加doctorId参数
        /// </summary>
        public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetUnfinished started - PatientId={PatientId} DoctorId={DoctorId}",
                patientId, doctorId);

            // Epic #2210 Task 3.1.2: 直接传递doctorId到Repository，无额外业务逻辑
            var result = await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, cancellationToken);

            if (result != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.GetUnfinished → Found - MedicalCaseId={MedicalCaseId} CaseStatus={CaseStatus} UserId={UserId}",
                    result.Id, result.CaseStatus, result.UserId);
            }
            else
            {
                _logger.LogInformation("[SVC] MedicalCase.GetUnfinished → NotFound - PatientId={PatientId} DoctorId={DoctorId}",
                    patientId, doctorId);
            }

            return result;
        }

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 实现缺失的Service方法
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetPendingCases started - DoctorId={DoctorId} PatientId={PatientId}",
                doctorId, patientId);

            // Epic #2210: 直接委托给Repository，传递doctorId进行数据隔离
            var result = await _repository.GetPendingCasesAsync(doctorId, patientId, cancellationToken);

            _logger.LogInformation("[SVC] MedicalCase.GetPendingCases completed - DoctorId={DoctorId} PatientId={PatientId} Count={Count}",
                doctorId, patientId, result.Count);

            return result;
        }

        /// <summary>
        /// 获取所有待看诊队列（管理员专用）
        /// 业务规则：返回所有Active状态医案的患者信息，不限定医生
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetAllPendingCases started - Admin");

            var result = await _repository.GetAllPendingCasesAsync(cancellationToken);

            _logger.LogInformation("[SVC] MedicalCase.GetAllPendingCases completed - Count={Count}", result.Count);

            return result;
        }

        /// <summary>
        /// 跨医案搜索（支持多条件组合查询）
        /// </summary>
        public async Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation(
                "[SVC] MedicalCase.Search started - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword} StartDate={StartDate} EndDate={EndDate} Page={Page} PageSize={PageSize}",
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

            // DB 层分页：QueryPagedAsync 在 DB 完成筛选 + 排序 + 分页（已包含 Include 预加载）
            var paged = await _repository.QueryPagedAsync(
                patientName, startDate, endDate, diagnosisKeyword, page, pageSize, cancellationToken);

            // 映射为DTO（包含嵌套Consultation/Prescription）
            var dtos = _mapper.ToDetailDtos(paged.Items);

            _logger.LogInformation("[SVC] MedicalCase.Search completed - TotalCount={TotalCount} ReturnedCount={ReturnedCount}",
                paged.TotalCount, dtos.Count);

            return new PagedResult<MedicalCaseDetailDto>(dtos, paged.TotalCount, page, pageSize);
        }

        /// <summary>
        /// 获取患者最近医案列表
        /// 用于处方编辑器历史处方参考
        /// </summary>
        public async Task<List<MedicalCaseDetailDto>> GetPatientRecentMedicalCasesAsync(Guid patientId, int count = 5, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetPatientRecent started - PatientId={PatientId} Count={Count}", patientId, count);

            // 获取患者所有医案
            var entities = await _repository.GetByPatientIdAsync(patientId, cancellationToken);

            if (entities == null || !entities.Any())
            {
                _logger.LogInformation("[SVC] MedicalCase.GetPatientRecent → NoHistory - PatientId={PatientId}", patientId);
                return new List<MedicalCaseDetailDto>();
            }

            // 按创建时间倒序，取前count条
            var recentEntities = entities
                .OrderByDescending(e => e.CreatedAt)
                .Take(count)
                .ToList();

            // 映射为DTO（包含嵌套Consultation/Prescription）
            var dtos = _mapper.ToDetailDtos(recentEntities);

            _logger.LogInformation("[SVC] MedicalCase.GetPatientRecent completed - PatientId={PatientId} ReturnedCount={ReturnedCount}",
                patientId, dtos.Count);

            return dtos;
        }

        /// <summary>
        /// 统一查询接口
        /// 根据QueryType分发到不同查询逻辑
        /// </summary>
        public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.Query started - QueryType={QueryType} PatientId={PatientId} DoctorId={DoctorId}",
                query.QueryType, query.PatientId, query.DoctorId);

            return query.QueryType switch
            {
                MedicalCaseQueryType.ByPatient => await QueryByPatientAsync(query, cancellationToken),
                MedicalCaseQueryType.Pending => await QueryPendingAsync(query, cancellationToken),
                MedicalCaseQueryType.Unfinished => await QueryUnfinishedAsync(query, cancellationToken),
                MedicalCaseQueryType.Recent => await QueryRecentAsync(query, cancellationToken),
                _ => await GetListDtoAsync(null, query.PatientId, query.PageIndex, query.PageSize, query.DoctorId, query.IncludeAllDoctors, query.Keyword, cancellationToken)
            };
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryByPatientAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)        {
            if (!query.PatientId.HasValue)
            {
                _logger.LogWarning("[SVC] MedicalCase.Query → ByPatient requires PatientId");
                return new PagedResult<MedicalCaseListDto>();
            }

            // DB 层分页：GetByPatientIdPagedAsync 在 DB 完成排序 + 分页
            var paged = await _repository.GetByPatientIdPagedAsync(
                query.PatientId.Value, query.PageIndex, query.PageSize, cancellationToken);

            var dtos = _mapper.ToListDtos(paged.Items);
            return new PagedResult<MedicalCaseListDto>(dtos, paged.TotalCount, query.PageIndex, query.PageSize);
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryPendingAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)
        {
            List<PendingMedicalCaseDto> pendingCases;
            if (query.IncludeAllDoctors || !query.DoctorId.HasValue)
            {
                pendingCases = await GetAllPendingCasesAsync(cancellationToken);
            }
            else
            {
                pendingCases = await GetPendingCasesAsync(query.DoctorId.Value, null, cancellationToken);
            }

            // 转换为ListDto格式，过滤掉没有MedicalCaseId的挂号记录
            var dtos = pendingCases
                .Where(p => p.MedicalCaseId.HasValue)
                .Select(p => new MedicalCaseListDto
                {
                    Id = p.MedicalCaseId!.Value,
                    PatientId = p.PatientId,
                    PatientName = p.PatientName,
                    CaseStatus = MedicalCaseStatus.Active,
                    CreatedAt = p.CreatedAt
                }).ToList();

            return new PagedResult<MedicalCaseListDto>(dtos, dtos.Count, 1, dtos.Count);
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryUnfinishedAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)
        {
            if (!query.PatientId.HasValue)
            {
                _logger.LogWarning("[SVC] MedicalCase.Query → Unfinished requires PatientId");
                return new PagedResult<MedicalCaseListDto>();
            }

            var doctorId = query.IncludeAllDoctors ? Guid.Empty : (query.DoctorId ?? Guid.Empty);
            var unfinished = await GetUnfinishedCaseByPatientIdAsync(query.PatientId.Value, doctorId, cancellationToken);

            if (unfinished == null)
            {
                return new PagedResult<MedicalCaseListDto>();
            }

            var dto = _mapper.ToListDto(unfinished);
            return new PagedResult<MedicalCaseListDto>(new List<MedicalCaseListDto> { dto }, 1, 1, 1);
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryRecentAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)
        {
            if (!query.PatientId.HasValue)
            {
                _logger.LogWarning("[SVC] MedicalCase.Query → Recent requires PatientId");
                return new PagedResult<MedicalCaseListDto>();
            }

            var count = query.Limit ?? 5;
            var recentCases = await GetPatientRecentMedicalCasesAsync(query.PatientId.Value, count, cancellationToken);

            // 从DetailDto手动映射为ListDto（DetailDto包含ListDto的所有字段）
            var listDtos = recentCases.Select(detail => new MedicalCaseListDto
            {
                Id = detail.Id,
                CaseNumber = detail.CaseNumber,
                PatientId = detail.PatientId,
                PatientName = detail.PatientName,
                PatientGender = detail.PatientGender,
                PatientAge = detail.PatientAge,
                UserId = detail.UserId,
                DoctorName = detail.DoctorName,
                CompletedAt = detail.CompletedAt,
                CaseStatus = detail.CaseStatus,
                Diagnosis = detail.Diagnosis,
                HasConsultation = detail.HasConsultation,
                HasPrescription = detail.HasPrescription,
                CreatedAt = detail.CreatedAt
            }).ToList();

            return new PagedResult<MedicalCaseListDto>(listDtos, listDtos.Count, 1, listDtos.Count);
        }

        /// <summary>
        /// 根据ID获取医案详情DTO（含NotFound语义）
        /// </summary>
        public async Task<Result<MedicalCaseDetailDto>> GetDetailDtoAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id, cancellationToken);
            if (medicalCase == null)
                return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            var dto = _mapper.MapToMedicalCaseDetailDto(medicalCase);
            return Result<MedicalCaseDetailDto>.Success(dto);
        }

        /// <summary>
        /// 批量获取医案详情DTO列表
        /// </summary>
        public async Task<Result<List<MedicalCaseDetailDto>>> GetBatchDetailDtosAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        {
            var medicalCases = await _repository.GetBatchWithDetailsAsync(ids, cancellationToken);
            var dtos = _mapper.ToDetailDtos(medicalCases);
            return Result<List<MedicalCaseDetailDto>>.Success(dtos);
        }

        /// <summary>
        /// 获取患者辨证记录历史（分页）
        /// </summary>
        public async Task<PagedResult<ConsultationDetailDto>> GetPatientConsultationsAsync(
            Guid patientId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            // DB 层分页：仅取含未删除 Consultation 的医案（含预加载），排序/分页/TotalCount 均在 DB 层完成
            var paged = await _repository.GetPatientConsultationsPagedAsync(patientId, page, pageSize, cancellationToken);

            var consultations = paged.Items
                .Select(mc =>
                {
                    var dto = _mapper.ToConsultationDetailDto(mc.Consultation!);
                    dto.MedicalCaseId = mc.Id;
                    dto.PatientId = mc.PatientId;
                    dto.UserId = mc.UserId;
                    dto.PatientName = mc.PatientName;
                    dto.DoctorName = mc.DoctorName;
                    dto.CreatedAt = mc.Consultation!.CreatedAt;
                    dto.UpdatedAt = mc.Consultation.UpdatedAt;
                    dto.CreatedBy = mc.Consultation.CreatedBy;
                    return dto;
                })
                .ToList();

            return new PagedResult<ConsultationDetailDto>(consultations, paged.TotalCount, page, pageSize);
        }

        /// <summary>
        /// 获取患者处方历史（分页）
        /// </summary>
        public async Task<PagedResult<PrescriptionDetailDto>> GetPatientPrescriptionsAsync(
            Guid patientId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            // DB 层分页：仅取含未删除 Prescription 的医案（含预加载），排序/分页/TotalCount 均在 DB 层完成
            var paged = await _repository.GetPatientPrescriptionsPagedAsync(patientId, page, pageSize, cancellationToken);

            var prescriptions = paged.Items
                .Select(mc =>
                {
                    var p = mc.Prescription!;
                    var dto = _mapper.ToPrescriptionDetailDto(p);
                    dto.MedicalCaseId = mc.Id;
                    dto.CreatedAt = p.CreatedAt;
                    dto.UpdatedAt = p.UpdatedAt;
                    dto.Items = p.Items?.Select(_mapper.ToPrescriptionItemDto).ToList()
                        ?? new List<PrescriptionItemDto>();
                    dto.SingleDosePrice = p.Items?.Sum(x => x.Amount) ?? 0;
                    dto.TotalPrice = dto.SingleDosePrice * p.DosageCount * p.Discount;
                    dto.TotalWeight = p.Items?.Sum(x => x.Dosage) ?? 0;
                    return dto;
                })
                .ToList();

            return new PagedResult<PrescriptionDetailDto>(prescriptions, paged.TotalCount, page, pageSize);
        }

        /// <summary>
        /// 获取医案审计日志（分页）
        /// </summary>
        public async Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(
            Guid caseId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId, cancellationToken);
            if (medicalCase == null)
                return Result<PagedResult<AuditLogDto>>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            var totalCount = await _repository.CountAuditLogsAsync(caseId, cancellationToken);
            var logs = await _repository.GetAuditLogsAsync(caseId, page, pageSize, cancellationToken);

            var items = logs.Select(l => new AuditLogDto
            {
                Timestamp = l.CreatedAt,
                Action = l.OperationType switch
                {
                    0 => "医案创建",
                    1 => "医案更新",
                    2 => "状态变更",
                    3 => "医案删除",
                    4 => "医案取消",
                    _ => "未知操作"
                },
                PerformedBy = l.OperatorId.ToString("D"),
                Details = l.Reason ?? $"操作人: {l.OperatorName}"
            }).ToList();

            var paged = new PagedResult<AuditLogDto>(items, totalCount, page, pageSize);
            return Result<PagedResult<AuditLogDto>>.Success(paged);
        }

        /// <summary>
        /// 获取医案操作权限
        /// </summary>
        public async Task<Result<MedicalCasePermissionsDto>> GetPermissionsAsync(
            Guid caseId, Guid userId, int userRole, CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId, cancellationToken);
            if (medicalCase == null)
                return Result<MedicalCasePermissionsDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            var isOwner = medicalCase.UserId == userId;
            var isAdmin = userRole == (int)UserRole.Admin || userRole == (int)UserRole.SuperAdmin;
            var status = medicalCase.CaseStatus;

            var dto = new MedicalCasePermissionsDto
            {
                CanEdit = (status == MedicalCaseStatus.Active || status == MedicalCaseStatus.Suspended) && (isOwner || isAdmin),
                CanComplete = status == MedicalCaseStatus.Active && isOwner,
                CanSuspend = (status == MedicalCaseStatus.Active || status == MedicalCaseStatus.Suspended) && isOwner,
                CanCancel = status == MedicalCaseStatus.Active && isOwner,
                CanDelete = !medicalCase.IsDeleted && (isOwner || isAdmin)
            };

            return Result<MedicalCasePermissionsDto>.Success(dto);
        }

    }
}


