using Asp.Versioning;
using MediatR;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理 API
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
    public class PatientsController : BaseCrudController
    {
        public PatientsController(ISender sender, ILogger<PatientsController> logger)
            : base(sender, logger)
        {
        }

        /// <summary>
        /// 获取患者列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "PatientsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;

            var result = await Sender.Send(new GetPatientsQuery(page, pageSize, keyword, FilterDisabled: !isAdmin), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "查询失败");
            }

            return SuccessPaged(result.Value, "查询成功");
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } error) return error;

            var result = await Sender.Send(new GetPatientQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "患者不存在");
            }

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status201Created)]
        public override async Task<IActionResult> Create([FromBody] object dto, CancellationToken ct)
        {
            if (dto is not PatientInputDto inputDto)
                return ValidationFail("无效的请求数据");

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new CreatePatientCommand(inputDto, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "创建失败");
            }

            LogOperation("新增患者成功", result.Value, null);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                ApiResponse<PatientDetailDto>.CreateSuccess(result.Value, "患者创建成功"));
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        [HttpPut("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] object dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;
            if (dto is not PatientInputDto inputDto)
                return ValidationFail("无效的请求数据");

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new UpdatePatientCommand(id, inputDto, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("不存在") == true)
                {
                    return NotFound(result.Error);
                }
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新患者成功", result.Value, id);
            return Success(result.Value, "患者更新成功");
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [HttpDelete("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new DeletePatientCommand(id, operatorId), ct);
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("医案记录") == true)
                    return BusinessFail(result.Error);
                return NotFound("患者不存在");
            }

            LogOperation("删除患者成功", null, id);
            return Success(true, "删除成功");
        }

        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// </summary>
        [HttpPost("{id:guid}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new TogglePatientStatusCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "操作失败");
            }

            LogOperation("切换患者状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"患者已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的患者
        /// </summary>
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new RestorePatientCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("未被删除") == true)
                    return BusinessFail(result.Error);
                return NotFound(result.Error ?? "患者不存在");
            }

            LogOperation("恢复患者成功", result.Value, id);
            return Success(result.Value, "患者恢复成功");
        }

        /// <summary>
        /// 批量删除患者
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个患者");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchDeletePatientsCommand(dto.Ids, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除患者", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 检查患者是否被医案引用
        /// </summary>
        [HttpGet("{id:guid}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<PatientReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var result = await Sender.Send(new CheckPatientReferenceQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "患者不存在");
            }

            return Success(result.Value, "引用检查完成");
        }

        /// <summary>
        /// 根据身份证号查询患者
        /// </summary>
        [HttpGet("by-id-number/{idNumber}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)
        {
            var result = await Sender.Send(new SearchPatientByIdNumberQuery(idNumber), ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "未找到匹配的患者");
            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 批量检查多个患者的引用关系
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientReferenceCheckDto>>), 200)]
        public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
        {
            if (dto.PatientIds == null || dto.PatientIds.Count == 0)
            {
                return ValidationFail("请至少选择一个患者");
            }

            if (dto.PatientIds.Count > 100)
            {
                return ValidationFail("批量检查最多支持100条");
            }

            var result = await Sender.Send(new BatchCheckPatientReferenceQuery(dto.PatientIds), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量检查失败");
            }

            return Success(result.Value, "批量引用检查完成");
        }

        /// <summary>
        /// 通过ISender查询患者并验证所有权
        /// </summary>
        private async Task<(PatientDetailDto? dto, IActionResult? error)> CheckOwnershipAsync(Guid id, CancellationToken ct)
        {
            var result = await Sender.Send(new GetPatientQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return (null, NotFound("患者不存在"));
            }

            if (ValidateOwnership(result.Value.CreatedBy, "患者") is { } ownerError)
            {
                return (null, ownerError);
            }

            return (result.Value, null);
        }
    }
}
