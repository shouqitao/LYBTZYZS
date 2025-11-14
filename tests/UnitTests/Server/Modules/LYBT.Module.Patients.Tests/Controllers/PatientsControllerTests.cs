using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using LYBT.WebAPI.Controllers;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Tests.Common;
using FluentAssertions;
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

            _controller = new PatientsController(_mockService.Object, mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidServices_ShouldCreateInstance()
        {
            // Act
            var mockService = CreateMock<IPatientService>();
            var mockLogger = CreateLoggerMock<PatientsController>();
            var controller = new PatientsController(mockService.Object, mockLogger.Object);

            // Assert
            controller.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new PatientsController(null!, CreateLoggerMock<PatientsController>().Object));
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
            var result = await _controller.GetList(pageNumber: invalidPageNumber);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            _mockService.Verify(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GetList_WithInvalidPageSize_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidPageSize = 101;

            // Act
            var result = await _controller.GetList(pageSize: invalidPageSize);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            _mockService.Verify(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GetList_WithNegativePageSize_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidPageSize = -1;

            // Act
            var result = await _controller.GetList(pageSize: invalidPageSize);

            // Assert
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);

            _mockService.Verify(s => s.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
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

            var mockResult = new Mock<ServiceResult<PagedResult<PatientDto>>>();
            mockResult.Setup(r => r.IsSuccess).Returns(true);
            mockResult.Setup(r => r.Data).Returns(new PagedResult<PatientDto>());

            _mockService.Setup(s => s.GetPagedAsync(pageNumber, pageSize, keyword))
                       .ReturnsAsync(mockResult.Object);

            // Act
            await _controller.GetList(pageNumber, pageSize, keyword);

            // Assert
            _mockService.Verify(s => s.GetPagedAsync(pageNumber, pageSize, keyword), Times.Once);
        }

        [Fact]
        public async Task GetList_WithDefaultParameters_ShouldCallServiceWithDefaults()
        {
            // Arrange
            var mockResult = new Mock<ServiceResult<PagedResult<PatientDto>>>();
            mockResult.Setup(r => r.IsSuccess).Returns(true);
            mockResult.Setup(r => r.Data).Returns(new PagedResult<PatientDto>());

            _mockService.Setup(s => s.GetPagedAsync(1, 20, null))
                       .ReturnsAsync(mockResult.Object);

            // Act
            await _controller.GetList();

            // Assert
            _mockService.Verify(s => s.GetPagedAsync(1, 20, null), Times.Once);
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
            apiVersionAttribute!.Versions.Should().Contain(1.0);
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