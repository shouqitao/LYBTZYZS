using Asp.Versioning;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API V1 - Epic #1612重构版
    /// 遵循CQRS原则：Command/Query/State服务分离
    /// 所有写操作通过MedicalCase聚合根
    /// Phase 3: 拆分为三个职责单一的Service
    /// </summary>
    /// optimize-api-permissions: 医案管理需Doctor或Admin角色
    /// 资源级授权通过MedicalCaseAuthorizationHandler实现
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize(Policy = "DoctorOrAdmin")]
    public class MedicalCaseController : BaseApiController
    {
        private readonly IMedicalCaseFacade _facade;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCaseController(
            IMedicalCaseFacade facade,
            MedicalCaseMapper mapper,
            ILogger<MedicalCaseController> logger)
            : base(logger)
        {
            _facade = facade;
            _mapper = mapper;
        }

        // ========== Write Layer（写操作，通过聚合根）==========

        /// <summary>
        /// 创建新医案
        /// OpenSpec: simplify-medicalcase-dataflow Phase 2 - 统一使用SaveAsync
        /// - 支持创建时同时包含Consultation和Prescription数据
        /// - Id=null时创建新医案
        /// optimize-api-permissions: 只有Doctor可以创建新医案，Admin不能创建
        /// </summary>
        /// <param name="dto">创建请求（Id应为null）</param>
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> CreateMedicalCase(
            [FromBody] MedicalCaseInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 获取当前医生ID
            var (doctorId, _, _) = GetOperator();

            // 确保Id为null以触发创建逻辑
            dto.Id = null;

            // OpenSpec: simplify-medicalcase-dataflow - 统一使用SaveAsync
            var entity = await _facade.SaveAsync(dto, doctorId, isAdmin: false);

            if (entity == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("患者不存在"));

            _logger.LogInformation("医案创建成功，ID: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                entity.Id, entity.DoctorName, entity.PatientName);

            // Entity → MedicalCaseDetailDto 映射
            var responseDto = _mapper.MapToMedicalCaseDetailDto(entity);

            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(responseDto, "医案创建成功"));
        }

        // OpenSpec: simplify-medicalcase-api - UpdateConsultation已删除
        // 诊断更新通过聚合保存 PUT /{id} 处理

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// Epic #1612 - BF-002: 动态流程控制
        /// 资源级权限由 Service 层 EnsureCanEdit/EnsureCanDelete 统一检查
        /// </summary>
        [HttpPut("{id}/prescription-flag")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        public async Task<IActionResult> SetPrescriptionFlag(
            Guid id,
            [FromBody] SetPrescriptionFlagRequest request)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.SetPrescriptionFlagAsync(id, request.NeedsPrescription, operatorId, isAdmin);
            if (result == null)
            {
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));
            }

            // Entity → DTO映射
            var dto = _mapper.MapToMedicalCaseDetailDto(result);

            _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                id, request.NeedsPrescription);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "处方标记更新成功"));
        }

        // OpenSpec: simplify-medicalcase-api - 独立Prescription CRUD端点已删除
        // 处方通过聚合保存 PUT /{id} 处理:
        // - CreatePrescription, CreatePrescriptionSimple: 通过SaveAsync创建
        // - UpdatePrescription, UpdatePrescriptionSimple: 通过SaveAsync更新
        // - DeletePrescription: 通过SaveAsync设置NeedsPrescription=false触发软删除

        #region 聚合保存端点

        /// <summary>
        /// 保存医案聚合根（统一保存Consultation和Prescription）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
        /// 在单个事务中同时保存诊断和处方数据
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">聚合保存请求</param>
        /// <returns>更新后的医案详情</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> Save(
            Guid id,
            [FromBody] MedicalCaseInputDto request)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 验证请求ID与路由ID一致
            if (request.Id != id)
            {
                return BadRequest(ApiResponse<MedicalCaseDetailDto>.CreateFail("请求ID与路由ID不一致"));
            }

            // 获取当前用户信息
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            // 调用门面聚合保存服务
            var result = await _facade.SaveAsync(request, operatorId, isAdmin);

            if (result == null)
            {
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));
            }

            // Entity → MedicalCaseDetailDto 映射
            var detailDto = _mapper.MapToMedicalCaseDetailDto(result);

            _logger.LogInformation("医案聚合保存成功，MedicalCaseId: {MedicalCaseId}", id);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(detailDto, "保存成功"));
        }

        #endregion

        /// <summary>
        /// 更新医案状态
        /// 支持 Draft/Active/Completed 状态流转（Cancelled 已移除，使用 IsDeleted 替代）
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateStatusRequest request)
        {
            // Completed 状态通过 CompleteAsync 统一入口处理
            if (request.Status == MedicalCaseStatus.Completed)
            {
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
                var completeResult = await _facade.CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation: false);
                if (completeResult == null)
                    return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

                var completeDto = _mapper.MapToMedicalCaseDto(completeResult);
                return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(completeDto, "医案已完成"));
            }

            var result = await _facade.UpdateStatusAsync(id, request.Status);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var dto = _mapper.MapToMedicalCaseDto(result);
            _logger.LogInformation("医案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}",
                id, request.Status);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "状态更新成功"));
        }

        /// <summary>
        /// 完成医案（三步流程最后一步）
        /// Epic #1612 - BF-002: 三步流程验证
        /// </summary>
        /// <summary>
        /// 完成医案 - 已废弃
        /// </summary>
        /// <remarks>
        /// OpenSpec refactor-webapi-layer: 此端点从未被Client调用，
        /// Client使用 PUT /{id}/status 并指定 Completed 状态。
        /// </remarks>


        /// <summary>
        /// 删除医案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// 资源级权限由 Service 层 EnsureCanEdit/EnsureCanDelete 统一检查
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult> DeleteMedicalCase(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.DeleteAsync(id, operatorId, isAdmin);
            if (!result)
                return NotFound(ApiResponse.CreateFail("医案不存在"));

            _logger.LogInformation("医案已软删除，MedicalCaseId: {Id}, OperatorId: {OperatorId}", id, operatorId);
            return NoContent();
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除医案
        /// </summary>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] LYBT.Shared.Models.Contracts.Common.BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个医案");
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.BatchDeleteAsync(dto.Ids, operatorId, isAdmin);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量删除失败");
            }

            LogOperation("批量删除医案", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 批量获取医案详情（含处方）
        /// OpenSpec: consolidate-medicalcase-detail-queries
        /// 解决N+1查询问题，一次请求获取多个医案详情
        /// </summary>
        [HttpPost("batch-details")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseDetailDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetBatchDetails([FromBody] BatchDetailQueryDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个医案");
            }

            if (dto.Ids.Count > 50)
            {
                return ValidationFail("单次最多查询50个医案");
            }

            var entities = await _facade.GetBatchAsync(dto.Ids);
            var dtos = entities.Select(e => _mapper.MapToMedicalCaseDetailDto(e)).ToList();

            return Success(dtos, $"查询成功，共{dtos.Count}条记录");
        }

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [HttpPut("{id}/close")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> CloseMedicalCase(Guid id)
        {
            // 委托给统一完成入口（skipWorkflowValidation=true）
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _facade.CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation: true);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var dto = _mapper.MapToMedicalCaseDetailDto(result);
            _logger.LogInformation("医案关闭，MedicalCaseId: {Id}", id);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "医案已关闭"));
        }

        /// <summary>
        /// 暂存医案（保存草稿）
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
        /// 保存当前数据，设置状态为Draft，不触发完成验证
        /// 资源级权限由 Service 层 EnsureCanEdit/EnsureCanDelete 统一检查
        /// </summary>
        [HttpPut("{id}/draft")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        public async Task<IActionResult> SaveDraft(
            Guid id,
            [FromBody] ConsultationInputDto? request = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.SaveDraftAsync(id, request, operatorId, isAdmin);
            if (result == null)
            {
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));
            }

            // Entity → DTO映射
            var dto = _mapper.MapToMedicalCaseDto(result);

            _logger.LogInformation("医案暂存成功，MedicalCaseId: {Id}", id);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "医案已暂存"));
        }

        /// <summary>
        /// 取消医案（统一为软删除 + 审计日志）
        /// 端点保留供客户端调用，内部行为从 CaseStatus=Cancelled 改为 IsDeleted=true
        /// </summary>
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> CancelMedicalCase(
            Guid id,
            [FromBody] CancelMedicalCaseRequest? request = null)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.CancelAsync(id, operatorId, isAdmin, request?.Reason);
            if (result == null)
            {
                return NotFound(ApiResponse.CreateFail("医案不存在"));
            }

            _logger.LogInformation("医案取消成功(软删除)，MedicalCaseId: {Id}", id);
            return NoContent();
        }

        // ========== Read Layer（读操作，独立查询）==========

        /// <summary>
        /// 获取医案详情
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // OpenSpec: optimize-medicalcase-api - GetById统一返回完整DetailDto（含Consultation+Prescription）
            var result = await _facade.GetByIdAsync(id);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            // Entity → DTO映射 - 使用MapToMedicalCaseDetailDto返回完整详情
            var dto = _mapper.MapToMedicalCaseDetailDto(result);

            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "查询成功"));
        }

        /// <summary>
        /// 获取完整的医疗案例（包含所有关联数据）
        /// Epic #2210 Phase 3 P0 Bug修复: 补充缺失的API端点
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <returns>完整的医案详情（包含Consultation和Prescription）</returns>
        /// <summary>
        /// 获取完整的医疗案例（包含所有关联数据）
        /// Epic #2210 Phase 3 P0 Bug修复: 补充缺失的API端点
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <returns>完整的医案详情（包含Consultation和Prescription）</returns>
        // GET /{id}/with-details -- 已移除，请使用 GET /{id}

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        /// 返回用户是否可编辑、可删除、是否需要提供修改原因等权限信息
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <returns>权限详情</returns>
        [HttpGet("{id}/permissions")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 404)]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var entity = await _facade.GetByIdAsync(id);

            if (entity == null)
                return NotFound(ApiResponse<MedicalCasePermissionDto>.CreateFail("医案不存在"));

            // 获取当前用户信息
            var (userId, _, role) = GetOperator();

            // 获取权限详情
            var permissions = _facade.GetPermissions(userId, role, entity);

            _logger.LogDebug("权限查询: 用户 {UserId}({Role}) 对医案 {MedicalCaseId} 的权限: CanEdit={CanEdit}, CanDelete={CanDelete}",
                userId, role, id, permissions.CanEdit, permissions.CanDelete);

            return Ok(ApiResponse<MedicalCasePermissionDto>.CreateSuccess(permissions, "查询成功"));
        }

        /// <summary>
        /// 获取医案的审计日志列表（分页）
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// 返回医案的所有修改历史记录
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="page">页码（默认1）</param>
        /// <param name="pageSize">每页大小（默认20）</param>
        /// <returns>审计日志分页结果</returns>
        [HttpGet("{id}/audit-logs")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 404)]
        public async Task<IActionResult> GetAuditLogs(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 验证医案是否存在
            var entity = await _facade.GetByIdAsync(id);
            if (entity == null)
                return NotFound(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateFail("医案不存在"));

            // 参数验证
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            // 获取审计日志
            var (logs, totalCount) = await _facade.GetAuditLogsPagedAsync(id, page, pageSize);

            // Entity → DTO 映射
            var logDtos = logs.Select(log => new MedicalCaseAuditLogDto
            {
                Id = log.Id,
                MedicalCaseId = log.MedicalCaseId,
                OperatorId = log.OperatorId,
                OperatorName = log.OperatorName,
                OperatorRole = log.OperatorRole,
                OperationType = log.OperationType,
                ChangedFields = log.ChangedFields,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                Reason = log.Reason,
                CreatedAt = log.CreatedAt
            }).ToList();

            var result = new MedicalCaseAuditLogPagedResultDto
            {
                Logs = logDtos,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };

            _logger.LogDebug("审计日志查询: 医案 {MedicalCaseId}, 第 {Page} 页, 共 {TotalCount} 条",
                id, page, totalCount);

            return Ok(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 查询医案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// OpenSpec: optimize-module-list-ui - 添加角色过滤，Doctor只能看到自己的医案
        /// OpenSpec: fix-history-copy-all-patients - 添加includeAllDoctors参数支持历史医案复制
        /// OpenSpec: refactor-medicalcase-management - 添加keyword搜索参数
        /// OpenSpec: post-release-cleanup - 统一返回MedicalCaseListDto
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] MedicalCaseStatus? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeAllDoctors = false,
            [FromQuery] string? keyword = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            // OpenSpec: optimize-module-list-ui - 获取当前用户信息用于角色过滤
            // OpenSpec: fix-history-copy-all-patients - includeAllDoctors=true时跳过医生过滤
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;

            // OpenSpec: post-release-cleanup - 直接使用GetListDtoAsync返回MedicalCaseListDto
            var result = await _facade.GetListDtoAsync(
                status, patientId, page, pageSize,
                currentDoctorId: operatorId,
                isAdmin: isAdmin,
                keyword: keyword);

            return Ok(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 统一医案查询端点
        /// OpenSpec: optimize-medicalcase-api - 整合多个查询端点为统一接口
        /// 支持多种查询类型：All(默认分页)、ByPatient(按患者)、Pending(待看诊)、Unfinished(未完成)、Recent(最近)
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>分页查询结果</returns>
        [HttpGet("query")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 400)]
        public async Task<IActionResult> GetMedicalCases([FromQuery] MedicalCaseQueryDto query)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (query.PageIndex <= 0 || query.PageSize <= 0 || query.PageSize > 100)
            {
                return BadRequest(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            // 获取当前用户信息
            var (operatorId, _, operatorRole) = GetOperator();
            
            // 设置DoctorId和权限
            if (!query.DoctorId.HasValue)
            {
                query.DoctorId = operatorId;
            }
            
            // Admin角色可以查看所有数据
            if (operatorRole is UserRole.SuperAdmin or UserRole.Admin)
            {
                query.IncludeAllDoctors = true;
            }

            var result = await _facade.QueryAsync(query);

            _logger.LogInformation("统一查询完成，QueryType: {QueryType}, 返回{Count}条记录", 
                query.QueryType, result.Items.Count);

            return Ok(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateSuccess(result, "查询成功"));
        }


        /// <summary>
        /// 跨医案搜索
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-015)
        /// 支持按患者名称、诊断关键词等条件查询
        /// </summary>
        /// <param name="patientName">患者名称（模糊匹配）</param>
        /// <param name="diagnosisKeyword">诊断关键词</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="page">页码（从1开始，默认1）</param>
        /// <param name="pageSize">每页大小（默认20，最大100）</param>
        /// <returns>分页结果（含嵌套Consultation/Prescription）</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseDetailDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseDetailDto>>), 400)]
        public async Task<IActionResult> SearchMedicalCases(
            [FromQuery] string? patientName = null,
            [FromQuery] string? diagnosisKeyword = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(ApiResponse<PagedResult<MedicalCaseDetailDto>>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            var result = await _facade.SearchMedicalCasesAsync(
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

            return Ok(ApiResponse<PagedResult<MedicalCaseDetailDto>>.CreateSuccess(result, "搜索成功"));
        }

        /// <summary>
        /// 获取患者最近医案
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-016)
        /// 用于处方编辑器历史处方参考
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="count">返回数量（默认5）</param>
        /// <returns>最近医案列表（按创建时间倒序，含完整Prescription数据）</returns>
        // GET /patient/{patientId}/recent -- 已移除，请使用 GET /query with QueryType=Recent

        // OpenSpec: post-release-cleanup - GetMedicalCasesList已合并到GetList
        // 原GET /list端点已删除，统一使用GET /返回MedicalCaseListDto

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回医案的所有历史辨证记录
        /// </summary>
        [HttpGet("{medicalCaseId}/consultations")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDetailDto>>), 200)]
        public async Task<IActionResult> GetConsultationList(
            Guid medicalCaseId)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var result = await _facade.GetConsultationListAsync(medicalCaseId);

            return Ok(ApiResponse<List<ConsultationDetailDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回医案的所有历史处方记录
        /// </summary>
        [HttpGet("{medicalCaseId}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDetailDto>>), 200)]
        public async Task<IActionResult> GetPrescriptionList(
            Guid medicalCaseId)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var result = await _facade.GetPrescriptionListAsync(medicalCaseId);

            return Ok(ApiResponse<List<PrescriptionDetailDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.3: 添加doctorId筛选，支持可选参数
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">可选医生ID（未传递时使用当前登录医生ID）</param>

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 添加缺失的API端点
        /// 业务规则：返回当前医生的所有Active状态医案的患者信息
        /// OpenSpec: unify-pending-query-api - 添加patientId参数支持按患者筛选
        /// </summary>
        /// <param name="patientId">患者ID（可选）- 传入时仅返回该患者的待看诊医案</param>
        [Obsolete("Use GET /api/v1/medicalcases/query with QueryType=Pending instead. Will be removed in v2.0")]
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 403)]
        public async Task<IActionResult> GetPendingCases([FromQuery] Guid? patientId = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // UnauthorizedAccessException由SystemExceptionHandler转换为401响应
            var (operatorId, operatorName, operatorRole) = GetOperator();

            List<PendingMedicalCaseDto> result;
            // Issue #2241: 根据角色判断查询范围，使用UserRole枚举比较
            if (operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin)
            {
                // 管理员查询所有待诊医案
                _logger.LogInformation("管理员查询全部待诊队列，OperatorId: {OperatorId}, Role: {Role}, PatientId: {PatientId}",
                    operatorId, operatorRole, patientId);
                // OpenSpec: unify-pending-query-api - 管理员目前不支持按患者筛选（返回全部）
                result = await _facade.GetAllPendingCasesAsync();
                // 如果有patientId参数，在内存中过滤
                if (patientId.HasValue)
                {
                    result = result.Where(r => r.PatientId == patientId.Value).ToList();
                }
            }
            else if (operatorRole == UserRole.Doctor)
            {
                // 医生只查询自己的待诊医案
                _logger.LogInformation("医生查询自己的待诊队列，DoctorId: {DoctorId}, PatientId: {PatientId}",
                    operatorId, patientId);
                // OpenSpec: unify-pending-query-api - 传递patientId参数
                result = await _facade.GetPendingCasesAsync(operatorId, patientId);
            }
            else
            {
                _logger.LogWarning("无权限的用户尝试查询待诊队列，OperatorId: {OperatorId}, Role: {Role}",
                    operatorId, operatorRole);
                return Forbid();
            }

            _logger.LogInformation("待诊队列查询成功，Count: {Count}", result.Count);

            return Ok(ApiResponse<List<PendingMedicalCaseDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 根据患者ID获取医案列表
        /// OpenSpec: redesign-history-copy-ui - 补充缺失的API端点
        /// 复用GetListAsync方法，仅包装为专用路由
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>患者的所有医案列表</returns>
        // GET /by-patient/{patientId} -- 已移除，请使用 GET /query with QueryType=ByPatient

        // GET /patient/{patientId}/unfinished -- 已移除，请使用 GET /query with QueryType=Unfinished

    }

    // ========== Request DTOs ==========
    // CreateMedicalCaseRequest 已移至 LYBT.Shared.Models.Contracts.MedicalCase.MedicalCaseInputDto
    // SetPrescriptionFlagRequest 已移至 LYBT.Shared.Models.Contracts.MedicalCase

    /// <summary>
    /// 更新医案状态请求
    /// Epic #1612修正版
    /// </summary>
    public class UpdateStatusRequest
    {
        /// <summary>目标状态：Draft/Active/Completed</summary>
        public MedicalCaseStatus Status { get; set; }
    }

    /// <summary>
    /// 取消医案请求
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
    /// </summary>
    public class CancelMedicalCaseRequest
    {
        /// <summary>取消原因（非当天本人操作时必填）</summary>
        public string? Reason { get; set; }
    }
}
