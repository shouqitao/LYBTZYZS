using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Registration.Controllers;
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
}
