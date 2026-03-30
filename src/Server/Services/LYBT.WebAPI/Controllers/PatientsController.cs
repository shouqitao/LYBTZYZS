using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理 API - 基础CRUD功能
    /// </summary>
    /// optimize-api-permissions: 患者管理需Doctor或Admin角色
    /// T5-P2-30: 扩展为PatientAccess策略，包含Receptionist
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.PatientAccess)]
    public class PatientsController : BaseApiController
    {
        private readonly IPatientService _service;
        private readonly PatientMapper _mapper = new();

        public PatientsController(IPatientService service, ILogger<PatientsController> logger)
            : base(logger)
        {
            _service = service;
        }

        /// <summary>
        /// 获取患者列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "PatientsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), 200)]
        public async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return ValidationFail("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            // T5-P2-27: 非Admin角色只看到启用患者
            var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;
            var filterDisabled = !isAdmin;

            var result = await _service.GetPagedAsync(page, pageSize, keyword, filterDisabled);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "查询失败");
            }

            return SuccessPaged(result.Data, "查询成功");
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public async Task<IActionResult> GetById(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "患者ID") is { } error) return error;

            var entityResult = await _service.GetByIdEntityAsync(id);
            if (!entityResult.IsSuccess || entityResult.Data == null)
            {
                return NotFound(entityResult.ErrorMessage ?? "患者不存在");
            }

            var patientEntity = entityResult.Data;
            var patientDto = _mapper.ToDetailDto(patientEntity);
            patientDto.Age = patientEntity.Age;

            return Success(patientDto, "查询成功");
        }

        /// <summary>
        /// 新增患者
        /// T5-P2-29: 创建成功返回201
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] PatientInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var entityResult = await _service.CreateEntityAsync(dto);
            if (!entityResult.IsSuccess || entityResult.Data == null)
            {
                return ValidationFail(entityResult.ErrorMessage ?? "新增患者失败");
            }

            var patientEntity = entityResult.Data;
            var patientDto = _mapper.ToDetailDto(patientEntity);
            patientDto.Age = patientEntity.Age;

            LogOperation("新增患者成功", patientDto, patientEntity.Id);
            return CreatedAtAction(nameof(GetById),
                new { id = patientEntity.Id, version = "1" },
                ApiResponse<PatientDetailDto>.CreateSuccess(patientDto, "患者创建成功"));
        }

        /// <summary>
        /// 更新患者信息
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public async Task<IActionResult> Update(Guid id, [FromBody] PatientInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法（DTO版本）
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<PatientDetailDto>(
                id, guid => _service.GetByIdAsync(guid), "患者");
            if (ownershipError != null) return ownershipError;

            var entityResult = await _service.UpdateEntityAsync(id, dto);
            if (!entityResult.IsSuccess || entityResult.Data == null)
            {
                if (entityResult.ErrorMessage?.Contains("不存在") == true)
                {
                    return NotFound(entityResult.ErrorMessage);
                }
                return ValidationFail(entityResult.ErrorMessage ?? "更新患者失败");
            }

            var patientEntity = entityResult.Data;
            var patientDto = _mapper.ToDetailDto(patientEntity);
            patientDto.Age = patientEntity.Age;

            LogOperation("更新患者成功", patientDto, id);
            return Success(patientDto, "患者更新成功");
        }

        /// <summary>
        /// 删除患者（软删除）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> Delete(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 使用统一的所有权检查方法（DTO版本）
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<PatientDetailDto>(
                id, guid => _service.GetByIdAsync(guid), "患者");
            if (ownershipError != null) return ownershipError;

            var result = await _service.DeleteAsync(id);
            if (!result.IsSuccess)
            {
                // X7: 区分引用阻塞(422)和不存在(404)
                if (result.ErrorMessage?.Contains("医案记录") == true)
                    return BusinessFail(result.ErrorMessage ?? "无法删除，存在关联医案记录");
                return NotFound("患者不存在");
            }

            LogOperation("删除患者成功", null, id);
            return Success(true, "删除成功");
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复端点 ==========

        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<PatientDetailDto>(id, guid => _service.GetByIdAsync(guid), "患者");
            if (ownershipError != null) return ownershipError;

            var result = await _service.ToggleStatusAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "状态切换失败");
            }

            LogOperation("切换患者状态", new { NewStatus = result.Data.Status }, id);
            return Success(result.Data, $"患者已{(result.Data.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的患者
        /// OpenSpec: optimize-module-list-ui - 使用统一所有权检查模式
        /// </summary>
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> Restore(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (ValidateGuid(id, "患者ID") is { } error) return error;

            // 注: Restore不能使用GetEntityWithOwnershipCheckAsync，因为GetByIdAsync
            // 受全局软删除过滤器影响无法找到已删除记录。
            // RestoreAsync内部使用GetByIdIncludingDeletedAsync绕过过滤器。
            var result = await _service.RestoreAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "恢复失败");
            }

            LogOperation("恢复患者", null, id);
            return Success(result.Data, "患者已恢复");
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除患者
        /// </summary>
        [HttpPost("batch-delete")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个患者");
            }

            var result = await _service.BatchDeleteAsync(dto.Ids);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量删除失败");
            }

            LogOperation("批量删除患者", new { Ids = dto.Ids, Result = result.Data.Message }, null);
            return Success(result.Data, result.Data.Message);
        }

        /// <summary>
        /// 检查患者引用关系
        /// X7: 删除前检查是否有关联医案
        /// </summary>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<PatientReferenceCheckDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> CheckReference(Guid id)
        {
            if (ValidateGuid(id, "患者ID") is { } error) return error;

            var result = await _service.CheckReferenceAsync(id);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "引用检查失败");
            }
            return Success(result.Data, "引用检查完成");
        }

        /// <summary>
        /// 批量检查患者引用关系
        /// X7: 批量删除前预检查
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto request)
        {
            if (request.PatientIds == null || request.PatientIds.Count == 0)
            {
                return ValidationFail("患者ID列表不能为空");
            }

            if (request.PatientIds.Count > 100)
            {
                return ValidationFail("批量检查最多支持100条记录");
            }

            var result = await _service.BatchCheckReferenceAsync(request.PatientIds);
            if (!result.IsSuccess || result.Data == null)
            {
                return BusinessFail(result.ErrorMessage ?? "批量引用检查失败");
            }
            return Success(result.Data, "批量引用检查完成");
        }
    }
}
