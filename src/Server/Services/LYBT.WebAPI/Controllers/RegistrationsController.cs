using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
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
[Authorize(Policy = PolicyConstants.PatientAccess)]
public class RegistrationsController : BaseApiController
{
    private readonly IRegistrationService _service;

    public RegistrationsController(
        IRegistrationService service,
        ILogger<RegistrationsController> logger)
        : base(logger)
    {
        _service = service;
    }

    /// <summary>
    /// 创建挂号 (前台模式)
    /// US-REG-001: Source=Receptionist, Status=Waiting
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] RegistrationInputDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result.IsSuccess || result.Data is null)
        {
            return HandleResult(result);
        }

        LogOperation("创建挂号", dto, result.Data.Id);
        return CreatedAtAction(nameof(GetById),
            new { id = result.Data.Id, version = "1" },
            ApiResponse<RegistrationDetailDto>.CreateSuccess(result.Data, "挂号创建成功"));
    }

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RegistrationDetailDto>), 200)]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (ValidateGuid(id, "挂号ID") is { } error) return error;

        var result = await _service.GetByIdAsync(id);
        return HandleResult(result);
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
        if (page <= 0 || pageSize <= 0 || pageSize > 100)
        {
            return ValidationFail("页码和页大小参数无效 (页码>0, 页大小1-100)");
        }

        var result = await _service.GetPagedAsync(page, pageSize, keyword,
            startDate, endDate, patientId, doctorId);
        if (!result.IsSuccess || result.Data is null)
        {
            return HandleResult(result);
        }

        return SuccessPaged(result.Data, "查询成功");
    }

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(ApiResponse<List<RegistrationListDto>>), 200)]
    public async Task<IActionResult> GetQueue([FromQuery] Guid? doctorId = null)
    {
        var result = await _service.GetWaitingQueueAsync(doctorId);
        return HandleResult(result);
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

        var result = await _service.StartVisitAsync(id);
        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        LogOperation("接诊", null, id);
        return Success(result.Data, "接诊成功");
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

        var result = await _service.CancelAsync(id);
        if (!result.IsSuccess)
        {
            return BusinessFail(result.ErrorMessage ?? "取消挂号失败");
        }

        LogOperation("取消挂号", null, id);
        return Success("挂号已取消");
    }
}
