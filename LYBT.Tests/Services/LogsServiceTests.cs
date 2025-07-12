using System;
using Moq;
using System.Threading.Tasks;
using Xunit;
using LYBT.Module.Logs.Services;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Logs.Dtos;
using LYBT.Common.Enums.Logs;

namespace LYBT.Tests.Services;

public class LogsServiceTests
{
    [Fact]
    public async Task AddLogAsync_CallsRepository()
    {
        var repo = new Mock<ILogRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<LYBT.Models.Logs.LogModel>())).ReturnsAsync(true);
        var service = new LogService(repo.Object);

        var dto = new LogDto { LogType = LogType.Operation };
        var result = await service.AddLogAsync(dto);

        Assert.NotEqual(Guid.Empty, result);
        repo.Verify(r => r.AddAsync(It.IsAny<LYBT.Models.Logs.LogModel>()), Times.Once);
    }
}
