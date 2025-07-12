using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using Moq;
using Xunit;
using LYBT.Module.Auth.Services;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Users.Models;
using LYBT.Module.Users.Dtos;
using LYBT.Module.Users.Mapping;
using LYBT.Common.Enums.Users;

namespace LYBT.Tests.Services;

public class AuthServiceTests
{
    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
        });
        return config.CreateMapper();
    }

    [Fact]
    public async Task LoginAsync_ReturnsUserDto_WhenPasswordMatches()
    {
        var user = new UserModel
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            RealName = "Admin",
            Roles = new List<UserRole> { UserRole.Admin },
            IsActive = true,
            PasswordHash = LYBT.Common.Helpers.PasswordHelper.Hash("pwd")
        };

        var repo = new Mock<IAuthRepository>();
        repo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        repo.Setup(r => r.GetAdminPasswordHashAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        repo.Setup(r => r.UpdateLastLoginTimeAsync(user.Id, It.IsAny<DateTime>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.UpdateUserLoginProtectionAsync(It.IsAny<UserModel>())).Returns(Task.CompletedTask);

        var logSvc = new Mock<ILogService>();
        var mapper = CreateMapper();
        var service = new AuthService(repo.Object, mapper, logSvc.Object);

        var dto = new LoginRequestDto { Username = "admin", Password = "pwd" };
        var result = await service.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("admin", result!.UserName);
        logSvc.Verify(l => l.AddLogAsync(It.IsAny<LYBT.Module.Logs.Dtos.LogDto>()), Times.Once);
    }
}
