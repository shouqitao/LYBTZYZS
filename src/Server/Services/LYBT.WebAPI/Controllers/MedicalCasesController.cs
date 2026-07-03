using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

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
        private readonly ISender _sender;

        public MedicalCasesController(
            ISender sender,
            ILogger<MedicalCasesController> logger)
            : base(logger)
        {
            _sender = sender;
        }

        /// <summary>
        /// 创建新医案
        /// - 支持创建时同时包含Consultation和Prescription数据
        /// - Id=null时创建新医案
        /// optimize-api-permissions: Doctor或Admin可以创建新医案
        /// </summary>
        /// <param name="dto">创建请求（Id应为null）</param>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> CreateMedicalCase(
            [FromBody] MedicalCaseInputDto dto)
        {
            var (doctorId, _, _) = GetOperator();

            dto.Id = null;
            var result = await _sender.Send(new CreateMedicalCaseCommand(dto, doctorId));

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "患者不存在");

            var responseDto = result.Value!;

            _logger.LogInformation("医案创建成功，ID: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                responseDto.Id, responseDto.DoctorName, responseDto.PatientName);

            return CreatedAtAction(nameof(GetById),
                new { id = responseDto.Id, version = ApiVersionConstants.V1 },
                ApiResponse<MedicalCaseDetailDto>.CreateSuccess(responseDto, "医案创建成功"));
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
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _sender.Send(new SetPrescriptionFlagCommand(id, request.NeedsPrescription, operatorId, isAdmin));
            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "医案不存在");
            }

            _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                id, request.NeedsPrescription);
            return Success(result.Value!, "处方标记更新成功");
        }

        /// <summary>
        /// 保存医案聚合根（统一保存Consultation和Prescription）
        /// 在单个事务中同时保存诊断和处方数据
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">聚合保存请求</param>
        /// <returns>更新后的医案详情</returns>
        [HttpPut("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> Save(
            Guid id,
            [FromBody] MedicalCaseInputDto request)
        {
            if (request.Id != id)
            {
                return Error("请求ID与路由ID不一致");
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _sender.Send(new SaveMedicalCaseCommand(request, operatorId, isAdmin));

            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "医案不存在");
            }

            _logger.LogInformation("医案聚合保存成功，MedicalCaseId: {MedicalCaseId}", id);
            return Success(result.Value!, "保存成功");
        }

        /// <summary>
        /// 删除医案（软删除）
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// 资源级权限由 Service 层 EnsureCanEdit/EnsureCanDelete 统一检查
        /// </summary>
        [HttpDelete("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> DeleteMedicalCase(Guid id)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _sender.Send(new DeleteMedicalCaseCommand(id, operatorId, isAdmin));
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案已软删除，MedicalCaseId: {Id}, OperatorId: {OperatorId}", id, operatorId);
            return Success(true, "医案已删除");
        }

        /// <summary>
        /// 批量删除医案
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] LYBT.Shared.Models.Contracts.Common.BatchDeleteInputDto dto)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个医案");
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _sender.Send(new BatchDeleteMedicalCasesCommand(dto.Ids, operatorId, isAdmin));
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除医案", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
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
            var result = await _sender.Send(new GetMedicalCaseQuery(id));

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 查询医案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "MedicalCaseCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] MedicalCaseStatus? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeAllDoctors = false,
            [FromQuery] string? keyword = null)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;
            var result = await _sender.Send(new GetMedicalCasesQuery(
                status, patientId, page, pageSize, operatorId, isAdmin, keyword));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 统一医案查询端点
        /// 支持多种查询类型：All(默认分页)、ByPatient(按患者)、Pending(待看诊)、Unfinished(未完成)、Recent(最近)
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>分页查询结果</returns>
        [HttpGet("query")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 400)]
        public async Task<IActionResult> GetMedicalCases([FromQuery] MedicalCaseQueryDto query)
        {
            if (ValidatePagination(query.PageIndex, query.PageSize) is { } error) return error;

            var (operatorId, _, operatorRole) = GetOperator();

            if (!query.DoctorId.HasValue)
            {
                query.DoctorId = operatorId;
            }

            if (operatorRole is UserRole.SuperAdmin or UserRole.Admin)
            {
                query.IncludeAllDoctors = true;
            }

            var result = await _sender.Send(new QueryMedicalCasesCommand(query));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            _logger.LogInformation("统一查询完成，QueryType: {QueryType}, 返回{Count}条记录",
                query.QueryType, result.Value!.Items.Count);

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 跨医案搜索
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
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _sender.Send(new SearchMedicalCasesQuery(
                patientName, diagnosisKeyword, startDate, endDate, page, pageSize));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "搜索失败");

            return Success(result.Value!, "搜索成功");
        }

        /// <summary>
        /// 查询患者辨证记录历史
        /// US-MC-008: 按患者ID分页查询所有医案的辨证记录
        /// </summary>
        [HttpGet("patient/{patientId:guid}/consultations")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ConsultationDetailDto>>), 200)]
        public async Task<IActionResult> GetPatientConsultations(
            Guid patientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _sender.Send(
                new GetPatientConsultationsQuery(patientId, page, pageSize));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 查询患者处方历史
        /// US-MC-009: 按患者ID分页查询所有医案的处方记录
        /// </summary>
        [HttpGet("patient/{patientId:guid}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PrescriptionDetailDto>>), 200)]
        public async Task<IActionResult> GetPatientPrescriptions(
            Guid patientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _sender.Send(
                new GetPatientPrescriptionsQuery(patientId, page, pageSize));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
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
            var result = await _sender.Send(new GetMedicalCaseConsultationsQuery(medicalCaseId));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
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
            var result = await _sender.Send(new GetMedicalCasePrescriptionsQuery(medicalCaseId));

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 批量查询医案详情（≤50条）
        /// 解决列表视图N+1查询问题
        /// </summary>
        [HttpPost("batch-details")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseDetailDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetBatchDetails([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return ValidationFail("IDs不能为空");
            if (ids.Count > 50)
                return ValidationFail("最多查询50条");

            var result = await _sender.Send(new GetMedicalCasesBatchQuery(ids));
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 获取医案操作权限
        /// US-MC-016: 返回当前用户对该医案可执行的操作
        /// </summary>
        [HttpGet("{id}/permissions")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionsDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var roleInt = (int)operatorRole;
            var result = await _sender.Send(new GetMedicalCasePermissionsQuery(id, operatorId, roleInt));
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 获取医案审计日志
        /// US-MC-017: 返回医案的变更历史记录
        /// </summary>
        [HttpGet("{id}/audit-logs")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetAuditLogs(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var result = await _sender.Send(new GetMedicalCaseAuditLogsQuery(id, page, pageSize));
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 记录打印完成
        /// US-PRINT-004: 更新医案打印状态并记录打印日志
        /// </summary>
        [HttpPut("{id:guid}/print-completed")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 422)]
        public async Task<IActionResult> RecordPrint(
            Guid id,
            [FromBody] RecordPrintRequest request)
        {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _sender.Send(new RecordPrintCommand(
                id, request.PrintType, request.PrinterName, operatorId, operatorName));

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("打印记录写入成功，MedicalCaseId: {Id}, PrintType: {PrintType}, Operator: {Operator}",
                id, request.PrintType, operatorName);
            return Success(true, "打印记录已写入");
        }
    }
}


