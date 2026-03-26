using Asp.Versioning;
using LYBT.Entities.MedicalCases;
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

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例基础 CRUD API V1
    /// 职责：医案的创建、读取、更新、删除、批量操作
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
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
        /// 查询医案列表（分页）
        /// 支持按状态、患者ID过滤，角色过滤，关键词搜索
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
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;

            var result = await _facade.GetListDtoAsync(
                status, patientId, page, pageSize,
                currentDoctorId: operatorId,
                isAdmin: isAdmin,
                keyword: keyword);

            return Ok(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 获取医案详情
        /// 返回完整详情（含Consultation和Prescription）
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _facade.GetByIdAsync(id);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var dto = _mapper.MapToMedicalCaseDetailDto(result);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "查询成功"));
        }

        /// <summary>
        /// 创建新医案
        /// 支持创建时同时包含Consultation和Prescription数据
        /// </summary>
        [HttpPost]
        [Authorize(Roles = RoleConstants.Doctor)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> CreateMedicalCase(
            [FromBody] MedicalCaseInputDto dto)
        {
            var (doctorId, _, _) = GetOperator();

            // 确保Id为null以触发创建逻辑
            dto.Id = null;

            var entity = await _facade.SaveAsync(dto, doctorId, isAdmin: false);

            if (entity == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("患者不存在"));

            _logger.LogInformation("医案创建成功，ID: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                entity.Id, entity.DoctorName, entity.PatientName);

            var responseDto = _mapper.MapToMedicalCaseDetailDto(entity);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(responseDto, "医案创建成功"));
        }

        /// <summary>
        /// 保存医案聚合根（统一保存Consultation和Prescription）
        /// 在单个事务中同时保存诊断和处方数据
        /// </summary>
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
            if (request.Id != id)
            {
                return BadRequest(ApiResponse<MedicalCaseDetailDto>.CreateFail("请求ID与路由ID不一致"));
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.SaveAsync(request, operatorId, isAdmin);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var detailDto = _mapper.MapToMedicalCaseDetailDto(result);
            _logger.LogInformation("医案聚合保存成功，MedicalCaseId: {MedicalCaseId}", id);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(detailDto, "保存成功"));
        }

        /// <summary>
        /// 删除医案（软删除）
        /// 使用BaseRepository默认软删除机制（IsDeleted=true）
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<ActionResult> DeleteMedicalCase(Guid id)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.DeleteAsync(id, operatorId, isAdmin);
            if (!result)
                return NotFound(ApiResponse.CreateFail("医案不存在"));

            _logger.LogInformation("医案已软删除，MedicalCaseId: {Id}, OperatorId: {OperatorId}", id, operatorId);
            return NoContent();
        }

        /// <summary>
        /// 批量删除医案
        /// </summary>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
        {
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
        /// 统一医案查询端点
        /// 支持多种查询类型：All(默认分页)、ByPatient(按患者)、Pending(待看诊)、Unfinished(未完成)、Recent(最近)
        /// </summary>
        [HttpGet("query")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<MedicalCaseListDto>>), 400)]
        public async Task<IActionResult> GetMedicalCases([FromQuery] MedicalCaseQueryDto query)
        {
            if (query.PageIndex <= 0 || query.PageSize <= 0 || query.PageSize > 100)
            {
                return BadRequest(ApiResponse<PagedResult<MedicalCaseListDto>>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            var (operatorId, _, operatorRole) = GetOperator();
            
            if (!query.DoctorId.HasValue)
            {
                query.DoctorId = operatorId;
            }
            
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
        /// 支持按患者名称、诊断关键词等条件查询
        /// </summary>
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
        /// 查询辨证记录列表
        /// 返回医案的所有历史辨证记录
        /// </summary>
        [HttpGet("{medicalCaseId}/consultations")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDetailDto>>), 200)]
        public async Task<IActionResult> GetConsultationList(Guid medicalCaseId)
        {
            var result = await _facade.GetConsultationListAsync(medicalCaseId);
            return Ok(ApiResponse<List<ConsultationDetailDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 查询处方列表
        /// 返回医案的所有历史处方记录
        /// </summary>
        [HttpGet("{medicalCaseId}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDetailDto>>), 200)]
        public async Task<IActionResult> GetPrescriptionList(Guid medicalCaseId)
        {
            var result = await _facade.GetPrescriptionListAsync(medicalCaseId);
            return Ok(ApiResponse<List<PrescriptionDetailDto>>.CreateSuccess(result, "查询成功"));
        }
    }
}
