using Asp.Versioning;
using LYBT.Infrastructure.Constants;
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
using System.Threading;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - CRUD操作
    /// 从原MedicalCaseController拆分，专注于基本的增删改查操作
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Tags("MedicalCases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCasesController : BaseApiController
    {
        private readonly IMedicalCaseFacade _facade;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCasesController(
            IMedicalCaseFacade facade,
            MedicalCaseMapper mapper,
            ILogger<MedicalCasesController> logger)
            : base(logger)
        {
            _facade = facade;
            _mapper = mapper;
        }

        /// <summary>
        /// 创建新医案
        /// OpenSpec: simplify-medicalcase-dataflow Phase 2 - 统一使用SaveAsync
        /// - 支持创建时同时包含Consultation和Prescription数据
        /// - Id=null时创建新医案
        /// optimize-api-permissions: Doctor或Admin可以创建新医案
        /// </summary>
        /// <param name="dto">创建请求（Id应为null）</param>
        [HttpPost]
        [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
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
                return NotFound("患者不存在");

            _logger.LogInformation("医案创建成功，ID: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                entity.Id, entity.DoctorName, entity.PatientName);

            // Entity → MedicalCaseDetailDto 映射
            var responseDto = _mapper.MapToMedicalCaseDetailDto(entity);

            return Success(responseDto, "医案创建成功");
        }

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
                return NotFound("医案不存在");
            }

            // Entity → DTO映射
            var dto = _mapper.MapToMedicalCaseDetailDto(result);

            _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                id, request.NeedsPrescription);
            return Success(dto, "处方标记更新成功");
        }

        /// <summary>
        /// 保存医案聚合根（统一保存Consultation和Prescription）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
        /// 在单个事务中同时保存诊断和处方数据
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">聚合保存请求</param>
        /// <returns>更新后的医案详情</returns>
        [HttpPut("{id:guid}")]
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
                return Error("请求ID与路由ID不一致");
            }

            // 获取当前用户信息
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            // 调用门面聚合保存服务
            var result = await _facade.SaveAsync(request, operatorId, isAdmin);

            if (result == null)
            {
                return NotFound("医案不存在");
            }

            // Entity → MedicalCaseDetailDto 映射
            var detailDto = _mapper.MapToMedicalCaseDetailDto(result);

            _logger.LogInformation("医案聚合保存成功，MedicalCaseId: {MedicalCaseId}", id);
            return Success(detailDto, "保存成功");
        }

        /// <summary>
        /// 删除医案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// 资源级权限由 Service 层 EnsureCanEdit/EnsureCanDelete 统一检查
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> DeleteMedicalCase(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.DeleteAsync(id, operatorId, isAdmin);
            if (!result)
                return NotFound("医案不存在");

            _logger.LogInformation("医案已软删除，MedicalCaseId: {Id}, OperatorId: {OperatorId}", id, operatorId);
            return Success(true, "医案已删除");
        }

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
                return HandleResult(result);
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
                return NotFound("医案不存在");

            // Entity → DTO映射 - 使用MapToMedicalCaseDetailDto返回完整详情
            var dto = _mapper.MapToMedicalCaseDetailDto(result);

            return Success(dto, "查询成功");
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
            if (ValidatePagination(page, pageSize) is { } error) return error;

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

            return Success(result, "查询成功");
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
            if (ValidatePagination(query.PageIndex, query.PageSize) is { } error) return error;

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

            return Success(result, "查询成功");
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
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _facade.SearchMedicalCasesAsync(
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize);

            return Success(result, "搜索成功");
        }

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

            return Success(result, "查询成功");
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

            return Success(result, "查询成功");
        }
    }
}
