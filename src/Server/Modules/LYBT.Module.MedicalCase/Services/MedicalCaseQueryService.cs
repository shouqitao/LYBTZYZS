using AutoMapper;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Services;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 病案查询服务实现 - 读操作
    /// Phase 3: 从MedicalCaseService拆分，遵循CQRS原则
    /// 职责：GetById, GetList, Search等查询操作
    /// </summary>
    public class MedicalCaseQueryService : BaseService<MedicalCase>, IMedicalCaseQueryService
    {
        private readonly IMedicalCaseRepository _repository;

        public MedicalCaseQueryService(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseQueryService> logger)
            : base(logger, mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 根据ID获取病案详情（包含完整关联数据）
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        public async Task<MedicalCase?> GetByIdAsync(Guid id)
        {
            try
            {
                var result = await _repository.GetByIdWithDetailsAsync(id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取病案详情失败，MedicalCaseId: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 查询病案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// OpenSpec: optimize-module-list-ui - 添加角色过滤，Doctor只能看到自己的医案
        /// </summary>
        public async Task<PagedResult<MedicalCase>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null)
        {
            try
            {
                // TODO: Repository需要扩展支持status和patientId过滤的分页方法
                // 当前使用GetPagedWithDetailsAsync作为临时实现
                var result = await _repository.GetPagedWithDetailsAsync(page, pageSize);

                // 临时过滤逻辑（后续应在Repository层实现）
                var filteredItems = result.Items.AsQueryable();

                if (status.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.CaseStatus == status.Value);
                }

                if (patientId.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.PatientId == patientId.Value);
                }

                // OpenSpec: refactor-medicalcase-management - 关键字过滤
                // 支持按患者姓名或中医诊断搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    filteredItems = filteredItems.Where(m =>
                        (m.PatientName != null && m.PatientName.Contains(keyword)) ||
                        (m.Consultation != null && m.Consultation.TCMDiagnosis != null && m.Consultation.TCMDiagnosis.Contains(keyword)));
                }

                // OpenSpec: optimize-module-list-ui - 角色过滤
                // 非管理员只能看到自己创建的医案
                if (!isAdmin && currentDoctorId.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.DoctorId == currentDoctorId.Value);
                    _logger.LogDebug("应用角色过滤，DoctorId: {DoctorId}", currentDoctorId.Value);
                }

                return new PagedResult<MedicalCase>
                {
                    Items = filteredItems.ToList(),
                    TotalCount = filteredItems.Count(),
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询病案列表失败");
                throw;
            }
        }

        /// <summary>
        /// 查询病案列表（分页，返回MedicalCaseListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        public async Task<PagedResult<MedicalCaseListDto>> GetListDtoAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize,
            Guid? currentDoctorId = null,
            bool isAdmin = false,
            string? keyword = null)
        {
            try
            {
                var result = await _repository.GetPagedWithDetailsAsync(page, pageSize);
                var filteredItems = result.Items.AsQueryable();

                if (status.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.CaseStatus == status.Value);
                }

                if (patientId.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.PatientId == patientId.Value);
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    filteredItems = filteredItems.Where(m =>
                        (m.PatientName != null && m.PatientName.Contains(keyword)) ||
                        (m.Consultation != null && m.Consultation.TCMDiagnosis != null && m.Consultation.TCMDiagnosis.Contains(keyword)));
                }

                if (!isAdmin && currentDoctorId.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.DoctorId == currentDoctorId.Value);
                }

                var dtos = _mapper.Map<List<MedicalCaseListDto>>(filteredItems.ToList());

                return new PagedResult<MedicalCaseListDto>
                {
                    Items = dtos,
                    TotalCount = dtos.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询病案列表失败");
                throw;
            }
        }

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回病案的所有历史辨证记录
        /// </summary>
        public async Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Consultation == null)
                {
                    return new List<ConsultationDetailDto>();
                }

                // 当前架构下只有一条Consultation（共享主键），直接映射
                var dto = _mapper.Map<ConsultationDetailDto>(medicalCase.Consultation);
                return new List<ConsultationDetailDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询辨证记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回病案的所有历史处方记录
        /// </summary>
        public async Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Prescription == null)
                {
                    return new List<PrescriptionDetailDto>();
                }

                // 当前架构下只有一条Prescription（一诊一方），直接映射
                var dto = _mapper.Map<PrescriptionDetailDto>(medicalCase.Prescription);
                return new List<PrescriptionDetailDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询处方列表失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.2: 添加doctorId参数
        /// </summary>
        public async Task<MedicalCase?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId)
        {
            try
            {
                _logger.LogInformation("查询患者未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);

                // Epic #2210 Task 3.1.2: 直接传递doctorId到Repository，无额外业务逻辑
                var result = await _repository.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId);

                if (result != null)
                {
                    _logger.LogInformation("找到未完成医案，MedicalCaseId: {MedicalCaseId}, CaseStatus: {CaseStatus}, DoctorId: {DoctorId}",
                        result.Id, result.CaseStatus, result.DoctorId);
                }
                else
                {
                    _logger.LogInformation("未找到患者的未完成医案，PatientId: {PatientId}, DoctorId: {DoctorId}",
                        patientId, doctorId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询患者未完成医案失败，PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 实现缺失的Service方法
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("获取待看诊队列，DoctorId: {DoctorId}", doctorId);

                // Epic #2210: 直接委托给Repository，传递doctorId进行数据隔离
                var result = await _repository.GetPendingCasesAsync(doctorId);

                _logger.LogInformation("待看诊队列查询完成，DoctorId: {DoctorId}, Count: {Count}",
                    doctorId, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待看诊队列失败，DoctorId: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取所有待看诊队列（管理员专用）
        /// 业务规则：返回所有Active状态医案的患者信息，不限定医生
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync()
        {
            try
            {
                _logger.LogInformation("获取所有待看诊队列（管理员）");

                var result = await _repository.GetAllPendingCasesAsync();

                _logger.LogInformation("待看诊队列查询完成（管理员），Count: {Count}", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有待看诊队列失败（管理员）");
                throw;
            }
        }


        /// <summary>
        /// 跨医案搜索（支持多条件组合查询）
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-015)
        /// </summary>
        public async Task<PagedResult<MedicalCaseDetailDto>> SearchMedicalCasesAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                _logger.LogInformation(
                    "跨医案搜索: PatientName={PatientName}, DiagnosisKeyword={DiagnosisKeyword}, StartDate={StartDate}, EndDate={EndDate}, Page={Page}, PageSize={PageSize}",
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

                // 映射为DTO（包含嵌套Consultation/Prescription）
                var dtos = _mapper.Map<List<MedicalCaseDetailDto>>(pagedEntities);

                _logger.LogInformation("跨医案搜索完成: TotalCount={TotalCount}, ReturnedCount={ReturnedCount}",
                    totalCount, dtos.Count);

                return new PagedResult<MedicalCaseDetailDto>(dtos, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "跨医案搜索失败");
                throw;
            }
        }

        /// <summary>
        /// 获取患者最近医案列表
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-016)
        /// 用于处方编辑器历史处方参考
        /// </summary>
        public async Task<List<MedicalCaseDetailDto>> GetPatientRecentMedicalCasesAsync(Guid patientId, int count = 5)
        {
            try
            {
                _logger.LogInformation("获取患者最近医案: PatientId={PatientId}, Count={Count}", patientId, count);

                // 获取患者所有医案
                var entities = await _repository.GetByPatientIdAsync(patientId);

                if (entities == null || !entities.Any())
                {
                    _logger.LogInformation("患者无历史医案: PatientId={PatientId}", patientId);
                    return new List<MedicalCaseDetailDto>();
                }

                // 按创建时间倒序，取前count条
                var recentEntities = entities
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(count)
                    .ToList();

                // 映射为DTO（包含嵌套Consultation/Prescription）
                var dtos = _mapper.Map<List<MedicalCaseDetailDto>>(recentEntities);

                _logger.LogInformation("获取患者最近医案完成: PatientId={PatientId}, ReturnedCount={ReturnedCount}",
                    patientId, dtos.Count);

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者最近医案失败: PatientId={PatientId}", patientId);
                throw;
            }
        }

    }
}
