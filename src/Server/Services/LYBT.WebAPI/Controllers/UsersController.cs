using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 用户管理 API - 基础CRUD、角色分配
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : BaseUsersController
    {
        public UsersController(
            ISender sender,
            ILogger<UsersController> logger)
            : base(sender, logger)
        {
        }
    }
}
