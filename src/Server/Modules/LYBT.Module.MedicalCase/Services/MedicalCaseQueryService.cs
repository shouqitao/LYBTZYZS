using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案查询服务实现 - 读操�?
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：GetById, GetList, Search等查询操�?
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用MedicalCaseMapper替代AutoMapper
    /// </summary>
    public class MedicalCaseQueryService : BaseService<MedicalCase>, IMedicalCaseQueryService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly MedicalCaseMapper _mapper = new();

        public MedicalCaseQueryService(
            IMedicalCaseRepository repository,
            ILogger<MedicalCaseQueryService> logger)
            : base(logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 根据ID获取医案详情（包含完整关联数据）
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        public async Task\u003cMedicalCase?\u003e GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            var result = await _repository.GetByIdWithDetailsAsync(id);
            return result;
        }

        /// <summary>
        /// 查询医案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// Sprint3-X6: 全部筛选迁移到 Repository DB 层执�?
        /// </summary>
        public async Task\u003cPagedResult\u003cMedicalCase\u003e\u003e GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null, CancellationToken cancellationToken = default)
        {
            // Sprint3-X6: 全部筛选在 DB 层执行，TotalCount 自然正确
            return await _repository.GetPagedWithDetailsAsync(
                page, pageSize, status, patientId, currentDoctorId, isAdmin, keyword);
        }

        /// <summary>
        /// 查询医案列表（分页，返回MedicalCaseListDto，用于列表视图）
        /// Sprint3-X6: 复用 GetListAsync 结果 + 映射，消除重复代�?
        /// </summary>
        public async Task\u003cPagedResult\u003cMedicalCaseListDto\u003e\u003e GetListDtoAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null, CancellationToken cancellationToken = default)
        {
            // Sprint3-X6: 复用 GetListAsync（已�?DB 层完成全部筛选）
            var result = await GetListAsync(status, patientId, page, pageSize, currentDoctorId, isAdmin, keyword);
            var dtos = _mapper.ToListDtos(result.Items.ToList());

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
        /// Epic #1612: 返回医案的所有历史辨证记�?
        /// </summary>
        public async Task\u003cList\u003cConsultationDetailDto\u003e\u003e GetConsultationListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase?.Consultation == null)
            {
                return new List<ConsultationDetailDto>();
            }

            // 当前架构下只有一条Consultation（共享主键），直接映�?
            var dto = _mapper.ToConsultationDetailDto(medicalCase.Consultation);
            return new List<ConsultationDetailDto> { dto };
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回医案的所有历史处方记�?
        /// </summary>
        public async Task\u003cList\u003cPrescriptionDetailDto\u003e\u003e GetPrescriptionListAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
            if (medicalCase?.Prescription == null)
            {
                return new List<PrescriptionDetailDto>();
            }

            // 当前架构下只有一条Prescription（一诊一方），直接映�?
            var dto = _mapper.ToPrescriptionDetailDto(medicalCase.Prescription);
            return new List<PrescriptionDetailDto> { dto };
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed�?
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.2: 添加doctorId参数
        /// </summary>
        public async Task\u003cMedicalCase?\u003e GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetUnfinished started - PatientId={PatientId} DoctorId={DoctorId}",
                patientId, doctorId);

            // Epic #2210 Task 3.1.2: 直接传递doctorId到Repository，无额外业务逻辑
            var result = await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);

            if (result != null)
            {
                _logger.LogInformation("[SVC] MedicalCase.GetUnfinished �?Found - MedicalCaseId={MedicalCaseId} CaseStatus={CaseStatus} UserId={UserId}",
                    result.Id, result.CaseStatus, result.UserId);
            }
            else
            {
                _logger.LogInformation("[SVC] MedicalCase.GetUnfinished �?NotFound - PatientId={PatientId} DoctorId={DoctorId}",
                    patientId, doctorId);
            }

            return result;
        }

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 实现缺失的Service方法
        /// OpenSpec: unify-pending-query-api - 添加patientId参数支持按患者筛�?
        /// </summary>
        public async Task\u003cList\u003cPendingMedicalCaseDto\u003e\u003e GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetPendingCases started - DoctorId={DoctorId} PatientId={PatientId}",
                doctorId, patientId);

            // Epic #2210: 直接委托给Repository，传递doctorId进行数据隔离
            // OpenSpec: unify-pending-query-api: 传递patientId支持按患者筛�?
            var result = await _repository.GetPendingCasesAsync(doctorId, patientId);

            _logger.LogInformation("[SVC] MedicalCase.GetPendingCases completed - DoctorId={DoctorId} PatientId={PatientId} Count={Count}",
                doctorId, patientId, result.Count);

            return result;
        }

        /// <summary>
        /// 获取所有待看诊队列（管理员专用�?
        /// 业务规则：返回所有Active状态医案的患者信息，不限定医�?
        /// </summary>
        public async Task\u003cList\u003cPendingMedicalCaseDto\u003e\u003e GetAllPendingCasesAsync(CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetAllPendingCases started - Admin");

            var result = await _repository.GetAllPendingCasesAsync();

            _logger.LogInformation("[SVC] MedicalCase.GetAllPendingCases completed - Count={Count}", result.Count);

            return result;
        }


        /// <summary>
        /// 跨医案搜索（支持多条件组合查询）
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-015)
        /// </summary>
        public async Task\u003cPagedResult\u003cMedicalCaseDetailDto\u003e\u003e SearchMedicalCasesAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation(
                "[SVC] MedicalCase.Search started - PatientName={PatientName} DiagnosisKeyword={DiagnosisKeyword} StartDate={StartDate} EndDate={EndDate} Page={Page} PageSize={PageSize}",
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

            // 使用Repository的QueryAsync方法获取实体（已包含Include预加载）
            var entities = await _repository.QueryAsync(patientName, startDate, endDate, diagnosisKeyword);

            // 按创建时间倒序排列
            var orderedEntities = entities.OrderByDescending(e => e.CreatedAt).ToList();

            // 分页处理
            var totalCount = orderedEntities.Count;
            var pagedEntities = orderedEntities
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 映射为DTO（包含嵌套Consultation/Prescription�?
            var dtos = _mapper.ToDetailDtos(pagedEntities);

            _logger.LogInformation("[SVC] MedicalCase.Search completed - TotalCount={TotalCount} ReturnedCount={ReturnedCount}",
                totalCount, dtos.Count);

            return new PagedResult<MedicalCaseDetailDto>(dtos, totalCount, page, pageSize);
        }

        /// <summary>
        /// 获取患者最近医案列�?
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-016)
        /// 用于处方编辑器历史处方参�?
        /// </summary>
        public async Task\u003cList\u003cMedicalCaseDetailDto\u003e\u003e GetPatientRecentMedicalCasesAsync(Guid patientId, int count = 5, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            _logger.LogInformation("[SVC] MedicalCase.GetPatientRecent started - PatientId={PatientId} Count={Count}", patientId, count);

            // 获取患者所有医�?
            var entities = await _repository.GetByPatientIdAsync(patientId);

            if (entities == null || !entities.Any())
            {
                _logger.LogInformation("[SVC] MedicalCase.GetPatientRecent �?NoHistory - PatientId={PatientId}", patientId);
                return new List<MedicalCaseDetailDto>();
            }

            // 按创建时间倒序，取前count�?
            var recentEntities = entities
                .OrderByDescending(e => e.CreatedAt)
                .Take(count)
                .ToList();

            // 映射为DTO（包含嵌套Consultation/Prescription�?
            var dtos = _mapper.ToDetailDtos(recentEntities);

            _logger.LogInformation("[SVC] MedicalCase.GetPatientRecent completed - PatientId={PatientId} ReturnedCount={ReturnedCount}",
                patientId, dtos.Count);

            return dtos;
        }

        /// <summary>
        /// 统一查询接口
        /// OpenSpec: optimize-medicalcase-api - 整合多个查询端点为统一接口
        /// 根据QueryType分发到不同查询逻辑
        /// </summary>
        public async Task\u003cPagedResult\u003cMedicalCaseListDto\u003e\u003e QueryAsync(MedicalCaseQueryDto query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.Query started - QueryType={QueryType} PatientId={PatientId} DoctorId={DoctorId}",
                query.QueryType, query.PatientId, query.DoctorId);

            return query.QueryType switch
            {
                MedicalCaseQueryType.ByPatient => await QueryByPatientAsync(query),
                MedicalCaseQueryType.Pending => await QueryPendingAsync(query),
                MedicalCaseQueryType.Unfinished => await QueryUnfinishedAsync(query),
                MedicalCaseQueryType.Recent => await QueryRecentAsync(query),
                _ => await GetListDtoAsync(null, query.PatientId, query.PageIndex, query.PageSize, query.DoctorId, query.IncludeAllDoctors, query.Keyword)
            };
        }

        /// <summary>
        /// 批量获取医案详情
        /// OpenSpec: consolidate-medicalcase-detail-queries
        /// </summary>
        public async Task\u003cList\u003cMedicalCase\u003e\u003e GetBatchAsync(List\u003cGuid\u003e ids, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("[SVC] MedicalCase.GetBatch started - Count={Count}", ids?.Count ?? 0);

            if (ids == null || !ids.Any())
            {
                return new List<MedicalCase>();
            }

            var result = await _repository.GetBatchWithDetailsAsync(ids);
            _logger.LogInformation("[SVC] MedicalCase.GetBatch completed - Found={Found}", result.Count);

            return result;
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryByPatientAsync(MedicalCaseQueryDto query)
        {
            if (!query.PatientId.HasValue)
            {
                _logger.LogWarning("[SVC] MedicalCase.Query �?ByPatient requires PatientId");
                return new PagedResult<MedicalCaseListDto>();
            }

            var entities = await _repository.GetByPatientIdAsync(query.PatientId.Value);
            var pagedEntities = entities
                .OrderByDescending(e => e.CreatedAt)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var dtos = _mapper.ToListDtos(pagedEntities);
            return new PagedResult<MedicalCaseListDto>(dtos, entities.Count, query.PageIndex, query.PageSize);
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryPendingAsync(MedicalCaseQueryDto query)
        {
            List<PendingMedicalCaseDto> pendingCases;
            if (query.IncludeAllDoctors || !query.DoctorId.HasValue)
            {
                pendingCases = await GetAllPendingCasesAsync();
            }
            else
            {
                pendingCases = await GetPendingCasesAsync(query.DoctorId.Value);
            }

            // 转换为ListDto格式，过滤掉没有MedicalCaseId的挂号记�?
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

        private async Task<PagedResult<MedicalCaseListDto>> QueryUnfinishedAsync(MedicalCaseQueryDto query)
        {
            if (!query.PatientId.HasValue)
            {
                _logger.LogWarning("[SVC] MedicalCase.Query �?Unfinished requires PatientId");
                return new PagedResult<MedicalCaseListDto>();
            }

            var doctorId = query.IncludeAllDoctors ? Guid.Empty : (query.DoctorId ?? Guid.Empty);
            var unfinished = await GetUnfinishedCaseByPatientIdAsync(query.PatientId.Value, doctorId);

            if (unfinished == null)
            {
                return new PagedResult<MedicalCaseListDto>();
            }

            var dto = _mapper.ToListDto(unfinished);
            return new PagedResult<MedicalCaseListDto>(new List<MedicalCaseListDto> { dto }, 1, 1, 1);
        }

        private async Task<PagedResult<MedicalCaseListDto>> QueryRecentAsync(MedicalCaseQueryDto query)
        {
            if (!query.PatientId.HasValue)
            {
                _logger.LogWarning("[SVC] MedicalCase.Query �?Recent requires PatientId");
                return new PagedResult<MedicalCaseListDto>();
            }

            var count = query.Limit ?? 5;
            var recentCases = await GetPatientRecentMedicalCasesAsync(query.PatientId.Value, count);

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

    }
}
