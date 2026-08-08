using System.Reflection;
using FluentAssertions;
using LYBT.Infrastructure.ExceptionHandling;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// SystemExceptionHandler 异常→HTTP状态码映射单元测试
/// 防回归: 越权抛 UnauthorizedAccessException 必须映射为 403（当前 tests 零覆盖）
/// </summary>
public class SystemExceptionHandlerTests
{
    [Fact]
    public void GetExceptionInfo_UnauthorizedAccessException_ShouldMapTo403()
    {
        // Arrange
        var handler = new SystemExceptionHandler(
            NullLogger<SystemExceptionHandler>.Instance,
            new TestHostEnvironment());
        var method = typeof(SystemExceptionHandler).GetMethod(
            "GetExceptionInfo",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("GetExceptionInfo 方法未找到");

        // Act
        var (statusCode, _, _) = ((int StatusCode, string Title, string Detail))method.Invoke(
            handler,
            new object[] { new UnauthorizedAccessException("无权限编辑此医案") })!;

        // Assert
        statusCode.Should().Be(403);
    }

    /// <summary>
    /// IHostEnvironment 最小测试替身（测试项目禁用 Mock，手写实现）
    /// </summary>
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "LYBT.Tests.Server";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new EmptyFileProvider();
    }

    /// <summary>
    /// 空 IFileProvider 替身（避免引入可释放资源）
    /// </summary>
    private sealed class EmptyFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath) => new NotFoundFileInfo(subpath);

        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
