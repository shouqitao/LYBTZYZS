using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Application.Commands;
using LYBT.Module.Registration.Application.Queries;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 挂号管理 API
/// PRD: registration.md US-REG-001~006
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
public class RegistrationsController : BaseApiController
{
    private readonly ISender _sender;

    public RegistrationsController(
        ISender sender,
        ILogger<RegistrationsController> logger)
        : base(logger)
    {
        _sender = sender;
    }

    /// <summary>
    /// 医生快速看诊 (后台静默创建 Registration + MedicalCase)
    /// US-REG-002: Source=Doctor, Status=InProgress, 医生无感知
    /// </summary>
    [HttpPost("quick-visit")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<QuickVisitResultDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto)
    {
        var (doctorId, doctorName, _) = GetOperator();

        var result = await _sender.Send(new QuickVisitCommand(dto, doctorId, doctorName));
        if (!result.IsSuccess || result.Value is null)
        {
            return BusinessFail(result.Error ?? "快速看诊失败");
        }

        LogOperation("医生快速看诊", dto, result.Value.RegistrationId);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Value.RegistrationId, version = ApiVersionConstants.V1 },
            ApiResponse<QuickVisitResultDto>.CreateSuccess(result.Value, "快速看诊创建成功"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] RegistrationInputDto dto)
    {
        var result = await _sender.Send(new CreateRegistrationCommand(dto));

        LogOperation("创建挂号", dto, result.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Id, version = ApiVersionConstants.V1 },
            ApiResponse<RegistrationDetailDto>.CreateSuccess(result, "挂号创建成功"));
    }

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await _sender.Send(new GetRegistrationQuery(id));
        if (result == null)
        {
            return NotFound("挂号不存在");
        }

        return Success(result, "查询成功");
    }

    /// <summary>
    /// 分页查询挂号记录
    /// US-REG-007: 支持按日期范围、患者、医生过滤
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RegistrationListDto>>), 200)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? patientId = null,
        [FromQuery] Guid? doctorId = null)
    {
        if (ValidatePagination(page, pageSize) is { } error) return error;

        var result = await _sender.Send(new GetRegistrationsQuery(page, pageSize, keyword,
            startDate, endDate, patientId, doctorId));

        return SuccessPaged(result, "查询成功");
    }

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(ApiResponse<List<RegistrationListDto>>), 200)]
    public async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null)
    {
        var result = await _sender.Send(new GetWaitingQueueQuery(doctorId));
        return Success(result, "查询成功");
    }

    /// <summary>
    /// 接诊: 从队列选中患者，Registration -> InProgress
    /// US-REG-003 验收标准第4条
    /// </summary>
    [HttpPut("{id}/start-visit")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> StartVisit(Guid id)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await _sender.Send(new StartVisitCommand(id));

        LogOperation("接诊", null, id);
        return Success(result, "接诊成功");
    }

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Receptionist 可操作，仅 Waiting 状态
    /// </summary>
    [HttpPut("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        await _sender.Send(new CancelRegistrationCommand(id));

        LogOperation("取消挂号", null, id);
        return Success("挂号已取消");
    }
}


