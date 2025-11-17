using LYBT.Server.Tests.Common.TestHelpers;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Server.Tests.Common.TestBase;

/// <summary>
/// Controller层测试基类
/// 提供统一的Controller测试基础设施
/// </summary>
public abstract class BaseControllerTest<TController> where TController : class
{
    protected readonly Mock<IOptions<LybtOptions>> _mockOptions;
    protected readonly IConfiguration _configuration;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly TController _sut;
    protected readonly IMapper _mapper;

    protected BaseControllerTest()
    {
        _mockOptions = CreateMockOptions();
        _configuration = CreateInMemoryConfiguration();
        _serviceProvider = BuildServiceProvider();
        _sut = _serviceProvider.GetRequiredService<TController>();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();
    }

    /// <summary>
    /// 创建Mock的LybtOptions
    /// </summary>
    protected virtual Mock<IOptions<LybtOptions>> CreateMockOptions()
    {
        var mockOptions = new Mock<IOptions<LybtOptions>>();
        mockOptions.Setup(o => o.Value).Returns(CreateTestOptions());
        return mockOptions;
    }

    /// <summary>
    /// 创建测试用的LybtOptions
    /// </summary>
    protected virtual LybtOptions CreateTestOptions()
    {
        return new LybtOptions
        {
            Jwt = new JwtConfiguration
            {
                SecretKey = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
                Issuer = "LYBT-Test",
                Audience = "LYBT-TestUsers",
                AccessTokenExpirationMinutes = 30,
                RefreshTokenExpirationDays = 7
            }
        };
    }

    /// <summary>
    /// 创建内存配置
    /// </summary>
    protected virtual IConfiguration CreateInMemoryConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            ["Lybt:Jwt:SecretKey"] = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
            ["Lybt:Jwt:Issuer"] = "LYBT-Test",
            ["Lybt:Jwt:Audience"] = "LYBT-TestUsers",
            ["Lybt:Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Lybt:Jwt:RefreshTokenExpirationDays"] = "7",
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=LYBT_Test;Trusted_Connection=true;",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
        };

        return new TestConfiguration.InMemoryConfiguration(configData);
    }

    /// <summary>
    /// 构建服务提供者
    /// </summary>
    protected virtual IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // 注册基础服务
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(_mockOptions.Object);
        services.AddSingleton(_configuration);
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // 注册特定测试的服务
        RegisterTestServices(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 注册测试特定的服务
    /// 子类必须实现此方法来注册Mock和实际服务
    /// </summary>
    protected abstract void RegisterTestServices(IServiceCollection services);

    /// <summary>
    /// 创建Mock对象
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// 创建带配置的Mock对象
    /// </summary>
    protected Mock<T> CreateMock<T>(Action<Mock<T>> setup) where T : class
    {
        var mock = new Mock<T>();
        setup(mock);
        return mock;
    }

    /// <summary>
    /// 验证OkObjectResult格式的成功响应
    /// </summary>
    protected TData VerifySuccessResult<TData>(IActionResult actionResult, string expectedMessage = null)
    {
        actionResult.Should().NotBeNull();
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<TData>>().Subject;
        TestHelper.AssertApiResponseFormat(apiResponse, true);

        if (expectedMessage != null)
        {
            apiResponse.Message.Should().Be(expectedMessage);
        }

        apiResponse.Data.Should().NotBeNull();
        return apiResponse.Data;
    }

    /// <summary>
    /// 验证OkObjectResult格式的分页成功响应
    /// </summary>
    protected PagedResult<TData> VerifySuccessPagedResult<TData>(IActionResult actionResult, string expectedMessage = null)
    {
        actionResult.Should().NotBeNull();
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<TData>>>().Subject;
        TestHelper.AssertPagedApiResponseFormat(apiResponse, true);

        if (expectedMessage != null)
        {
            apiResponse.Message.Should().Be(expectedMessage);
        }

        apiResponse.Data.Should().NotBeNull();
        return apiResponse.Data;
    }

    /// <summary>
    /// 验证BadRequestObjectResult格式的失败响应
    /// </summary>
    protected void VerifyFailureResult(IActionResult actionResult, int expectedStatusCode = 400, string expectedMessage = null)
    {
        actionResult.Should().NotBeNull();
        var badRequestResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(expectedStatusCode);
        badRequestResult.Value.Should().NotBeNull();

        // 根据实际的错误响应格式进行调整
        if (badRequestResult.Value is ApiResponse<object> apiResponse)
        {
            TestHelper.AssertApiResponseFormat(apiResponse, false);

            if (expectedMessage != null)
            {
                apiResponse.Message.Should().Contain(expectedMessage);
            }
        }
        else
        {
            // 处理其他可能的错误响应格式
            badRequestResult.Value.Should().NotBeNull();
        }
    }

    /// <summary>
    /// 验证NotFoundResult格式的响应
    /// </summary>
    protected void VerifyNotFoundResult(IActionResult actionResult)
    {
        actionResult.Should().NotBeNull();
        actionResult.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// 验证NoContentResult格式的响应
    /// </summary>
    protected void VerifyNoContentResult(IActionResult actionResult)
    {
        actionResult.Should().NotBeNull();
        actionResult.Should().BeOfType<NoContentResult>();
    }

    /// <summary>
    /// 创建测试用的ModelState错误
    /// </summary>
    protected void SetupModelError(ControllerBase controller, string key, string errorMessage)
    {
        if (controller is ControllerBase concreteController)
        {
            concreteController.ModelState.AddModelError(key, errorMessage);
        }
    }

    /// <summary>
    /// 验证ModelState包含特定错误
    /// </summary>
    protected void VerifyModelError(ControllerBase controller, string key, string errorMessage = null)
    {
        if (controller is ControllerBase concreteController)
        {
            concreteController.ModelState.IsValid.Should().BeFalse();
            concreteController.ModelState.Should().ContainKey(key);

            if (errorMessage != null)
            {
                var modelStateEntry = concreteController.ModelState[key];
                modelStateEntry.Should().NotBeNull();
                modelStateEntry.Errors.Should().Contain(e => e.ErrorMessage.Contains(errorMessage));
            }
        }
    }
}