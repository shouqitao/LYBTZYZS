using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Users.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
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


