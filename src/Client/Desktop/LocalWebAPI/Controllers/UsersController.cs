using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Web;
using LYBT.Module.Identity.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseUsersController
{
    public UsersController(
        ISender sender,
        ILogger<UsersController> logger,
        IUserService userService)
        : base(sender, logger, userService)
    {
    }
}
