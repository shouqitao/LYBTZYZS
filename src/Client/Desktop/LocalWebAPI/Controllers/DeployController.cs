using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
public class DeployController : BaseApiController
{
    public DeployController(ILogger<DeployController> logger) : base(logger) { }

    [HttpPost("upload")]
    public IActionResult Upload(IFormFile file)
    {
        return BusinessFail("本地模式不支持部署更新，请切换到远程服务器");
    }

    [HttpPost("restart")]
    public IActionResult Restart()
    {
        return BusinessFail("本地模式不支持远程重启");
    }
}
