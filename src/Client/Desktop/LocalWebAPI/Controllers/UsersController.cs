using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.LocalWebAPI.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseUsersController
{
    public UsersController(
        ISender sender,
        ILogger<UsersController> logger)
        : base(sender, logger)
    {
    }
}
