using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Controllers;
using LYBT.Module.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : BaseUsersController
    {
        public UsersController(
            IUserManagerService userManagerService,
            IConfiguration configuration,
            ILogger<UsersController> logger)
            : base(userManagerService, configuration, logger)
        {
        }
    }
}
