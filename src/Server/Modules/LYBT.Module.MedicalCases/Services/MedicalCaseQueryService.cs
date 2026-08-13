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
            Guid? operatorId = null,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation(
                "[SVC] MedicalCase.Search started - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword} StartDate={StartDate} EndDate={EndDate} Page={Page} PageSize={PageSize}",
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

            // DB 层分页：QueryPagedAsync 在 DB 完成筛选 + 排序 + 分页（已包含 Include 预加载）
            // P1-2（2026-08-14）: Doctor 所有权过滤下推 DB（原内存过滤致 TotalCount 偏大）
            var paged = await _repository.QueryPagedAsync(
                patientName, startDate, endDate, diagnosisKeyword, page, pageSize,
                operatorId, isAdmin, cancellationToken);

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
        /// <summary>批量详情（B1 US-MC-018）</summary>
        public async Task<Result<List<MedicalCaseDetailDto>>> GetDetailDtosBatchAsync(
            IEnumerable<Guid> ids, Guid? operatorId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetByIdsWithDetailsAsync(ids, cancellationToken);
            // B1: Doctor 仅本人医案（Admin 全量）——与 GetDetailDtoAsync 一致
            var filtered = (!isAdmin && operatorId.HasValue)
                ? entities.Where(m => m.CreatedBy == operatorId.Value).ToList()
                : entities;
            var dtos = filtered.Select(_mapper.MapToMedicalCaseDetailDto).ToList();
            return Result<List<MedicalCaseDetailDto>>.Success(dtos);
        }

        public async Task<Result<MedicalCaseDetailDto>> GetDetailDtoAsync(
            Guid id,
            Guid? operatorId = null,
            bool isAdmin = false,
            CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdWithDetailsAsync(id, cancellationToken);
            if (medicalCase == null)
                return Result<MedicalCaseDetailDto>.Failure(ErrorCode.NotFound, ErrorMessages.Get(ErrorCode.McCaseNotFound));

            // T5-1 #8 (US-MC-004): Doctor 仅可查看本人医案（Admin/SuperAdmin 全量）
            if (!isAdmin && operatorId.HasValue && medicalCase.CreatedBy != operatorId.Value)
                return Result<MedicalCaseDetailDto>.Failure(ErrorCode.Forbidden, "无权限查看该医案");

            var dto = _mapper.MapToMedicalCaseDetailDto(medicalCase);
            return Result<MedicalCaseDetailDto>.Success(dto);
        }

        /// <summary>
        /// 获取医案审计日志（分页）
        /// </summary>
        public async Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsAsync(
            Guid caseId, int page, int pageSize, Guid? operatorId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
        {
            var medicalCase = await _repository.GetByIdWithDetailsAsync(caseId, cancellationToken);
            // T5-1 #8 (US-MC-017): Doctor 仅可查看本人医案审计（Admin/SuperAdmin 全量）
            if (!isAdmin && operatorId.HasValue && medicalCase.CreatedBy != operatorId.Value)
                return Result<PagedResult<AuditLogDto>>.Failure(ErrorCode.Forbidden, "无权限查看该医案审计日志");
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
                CanDelete = !medicalCase.IsDeleted && (isOwner || isAdmin),
                // P1 (US-MC-016): 打印后修改 / 非 Admin 编辑已完成医案需 EditReason
                RequiresEditReason = (medicalCase.IsPrinted && medicalCase.PrintVersion > 0)
                    || (status == MedicalCaseStatus.Completed && !isAdmin),
                DenialReason = !(isOwner || isAdmin)
                    ? "仅创建医生或管理员可操作该医案"
                    : status == MedicalCaseStatus.Completed && !isAdmin
                        ? "已完成医案仅管理员可编辑"
                        : null
            };

            return Result<MedicalCasePermissionsDto>.Success(dto);
        }

    }
}


