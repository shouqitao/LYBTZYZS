using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Controllers;
using LYBT.Module.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseUsersController
{
    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
        : base(userService, logger)
    {
    }
}
