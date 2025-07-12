using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users;
using LYBT.Module.Logs.Interfaces;

namespace LYBT.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task AddAsync_AddsUserAndLogs()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.ExistsByUsernameAsync(It.IsAny<string>())).ReturnsAsync(false);
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Module.Users.Models.UserModel>())).ReturnsAsync(true);

        var logSvc = new Mock<ILogService>();
        var options = Options.Create(new UserOptions { DefaultUserPassword = "pwd" });
        var service = new UserService(repo.Object, logSvc.Object, options);

        var dto = new UserCreateDto { UserName = "test", RealName = "Test" };
        var result = await service.AddAsync(dto, Guid.Empty, "sys");

        Assert.True(result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Module.Users.Models.UserModel>()), Times.Once);
        logSvc.Verify(l => l.AddLogAsync(It.IsAny<LYBT.Module.Logs.Dtos.LogDto>()), Times.Once);
    }
}
