using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Controllers;
using LYBT.Shared.Models.Contracts.Registration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

/// <summary>
/// 挂号管理 API - 继承 BaseRegistrationsController 提供标准方法（简化版）
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
public class RegistrationsController : BaseRegistrationsController
{
    public RegistrationsController(ISender sender, ILogger<RegistrationsController> logger)
        : base(sender, logger)
    {
    }

    /// <inheritdoc />
    [Authorize(Policy = PolicyConstants.DoctorOnly)]
    public override async Task<IActionResult> QuickVisit([FromBody] QuickVisitInputDto dto, CancellationToken ct)
        => await base.QuickVisit(dto, ct);

    /// <inheritdoc />
    [Authorize(Policy = PolicyConstants.DoctorOnly)]
    public override async Task<IActionResult> StartVisit(Guid id, CancellationToken ct)
        => await base.StartVisit(id, ct);

    /// <inheritdoc />
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public override async Task<IActionResult> Create([FromBody] object dto, CancellationToken ct)
        => await base.Create(dto, ct);

    /// <inheritdoc />
    [Authorize(Policy = PolicyConstants.DoctorOrReceptionist)]
    public override async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => await base.Cancel(id, ct);
}
