using Asp.Versioning;
using FluentAssertions;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Tests.Common;
using LYBT.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LYBT.Module.Patients.Tests.Controllers
{
    /// <summary>
    /// PatientsController单元测试
    /// 测试API控制器的HTTP响应和业务逻辑
    /// </summary>
    public class PatientsControllerTests : TestBase
    {
        private readonly PatientsController _controller;
        private readonly Mock<IPatientService> _mockService;

        public PatientsControllerTests()
        {
            _mockService = CreateMock<IPatientService>();
            var mockLogger = CreateLoggerMock<PatientsController>();

            // 注：Mapperly迁移后，Controller内部使用私有Mapper，无需外部注入
            _controller = new PatientsController(_mockService.Object, mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidServices_ShouldCreateInstance()
        {
            // Act
            var mockService = CreateMock<IPatientService>();
            var mockLogger = CreateLoggerMock<PatientsController>();
            // 注：Mapperly迁移后，Controller内部使用私有Mapper
            var controller = new PatientsController(mockService.Object, mockLogger.Object);

            // Assert
            controller.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullService_ShouldCreateInstanceWithNullService()
        {
            // Note: 当前实现不验证null参数，这是一个已知的技术债务
            // Controller依赖.NET的NRT（Nullable Reference Types）在编译时检查
            // 实际运行时不会抛出异常，但会在首次使用null服务时失败
            var mockLogger = CreateLoggerMock<PatientsController>();
            var controller = new PatientsController(null!, mockLogger.Object);

            // 构造函数不会抛出异常，但对象会被创建
            controller.Should().NotBeNull();
        }

        [Fact]
        public void Controller_ShouldInheritFromBaseApiController()
        {
            // Assert
            _controller.Should().BeAssignableTo<BaseApiController>();
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        public async Task GetList_WithInvalidPageNumber_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidPageNumber = 0;

            // Act
            var result = await _controller.GetList(page: invalidPageNumber);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            // OpenSpec: post-release-cleanup - 更新为GetPagedAsync（返回PatientListDto）
            _mockService.Verify(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetList_WithInvalidPageSize_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidPageSize = 101;

            // Act
            var result = await _controller.GetList(pageSize: invalidPageSize);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            // OpenSpec: post-release-cleanup - 更新为GetPagedAsync（返回PatientListDto）
            _mockService.Verify(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task GetList_WithNegativePageSize_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidPageSize = -1;

            // Act
            var result = await _controller.GetList(pageSize: invalidPageSize);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            // OpenSpec: post-release-cleanup - 更新为GetPagedAsync（返回PatientListDto）
            _mockService.Verify(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
        }

        #endregion

        #region Mock Verification Tests

        [Fact]
        public async Task GetList_WithValidParameters_ShouldCallService()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 20;
            var keyword = "测试";

            // OpenSpec: post-release-cleanup - 更新为GetPagedAsync（返回PatientListDto）
            var mockResult = Result<PagedResult<PatientListDto>>.Success(new PagedResult<PatientListDto>
            {
                Items = new List<PatientListDto>(),
                TotalCount = 0,
                CurrentPage = pageNumber,
                PageSize = pageSize
            });

            _mockService.Setup(s => s.GetPagedAsync(pageNumber, pageSize, keyword, It.IsAny<bool>()))
                       .ReturnsAsync(mockResult);

            // Act
            await _controller.GetList(pageNumber, pageSize, keyword);

            // Assert
            _mockService.Verify(s => s.GetPagedAsync(pageNumber, pageSize, keyword, It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task GetList_WithDefaultParameters_ShouldCallServiceWithDefaults()
        {
            // OpenSpec: post-release-cleanup - 更新为GetPagedAsync（返回PatientListDto）
            // Arrange
            var mockResult = Result<PagedResult<PatientListDto>>.Success(new PagedResult<PatientListDto>
            {
                Items = new List<PatientListDto>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            });

            _mockService.Setup(s => s.GetPagedAsync(1, 20, null, It.IsAny<bool>()))
                       .ReturnsAsync(mockResult);

            // Act
            await _controller.GetList();

            // Assert
            _mockService.Verify(s => s.GetPagedAsync(1, 20, null, It.IsAny<bool>()), Times.Once);
        }

        #endregion

        #region Route Configuration Tests

        [Fact]
        public void Controller_ShouldHaveCorrectRouteAttribute()
        {
            // Arrange & Act & Assert
            var controllerType = typeof(PatientsController);
            var routeAttribute = controllerType.GetCustomAttributes(typeof(RouteAttribute), false)
                                             .FirstOrDefault() as RouteAttribute;

            routeAttribute.Should().NotBeNull();
            routeAttribute!.Template.Should().Be("api/v{version:apiVersion}/[controller]");
        }

        [Fact]
        public void Controller_ShouldHaveApiVersionAttribute()
        {
            // Arrange & Act & Assert
            var controllerType = typeof(PatientsController);
            var apiVersionAttribute = controllerType.GetCustomAttributes(typeof(ApiVersionAttribute), false)
                                                   .FirstOrDefault() as ApiVersionAttribute;

            apiVersionAttribute.Should().NotBeNull();
            apiVersionAttribute!.Versions.Should().Contain(new Asp.Versioning.ApiVersion(1, 0));
        }

        [Fact]
        public void Controller_ShouldHaveApiControllerAttribute()
        {
            // Arrange & Act & Assert
            var controllerType = typeof(PatientsController);
            var apiControllerAttribute = controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
                                                         .FirstOrDefault() as ApiControllerAttribute;

            apiControllerAttribute.Should().NotBeNull();
        }

        [Fact]
        public void Controller_ShouldHaveAuthorizeAttribute()
        {
            // Arrange & Act & Assert
            var controllerType = typeof(PatientsController);
            var authorizeAttribute = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                                                   .FirstOrDefault() as AuthorizeAttribute;

            authorizeAttribute.Should().NotBeNull();
        }

        #endregion

        #region Helper Methods

        private void SetupModelStateError(string propertyName, string errorMessage)
        {
            _controller.ModelState.AddModelError(propertyName, errorMessage);
        }

        #endregion
    }
}
