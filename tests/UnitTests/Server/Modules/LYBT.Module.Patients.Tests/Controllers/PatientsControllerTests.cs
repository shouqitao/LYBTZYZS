using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;
using Asp.Versioning;
using LYBT.WebAPI.Controllers;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Common;
using LYBT.Tests.Common;
using LYBT.Entities.Patients;
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
        private readonly Mock<IMapper> _mockMapper;

        public PatientsControllerTests()
        {
            _mockService = CreateMock<IPatientService>();
            _mockMapper = CreateMock<IMapper>();
            var mockLogger = CreateLoggerMock<PatientsController>();

            _controller = new PatientsController(_mockService.Object, _mockMapper.Object, mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidServices_ShouldCreateInstance()
        {
            // Act
            var mockService = CreateMock<IPatientService>();
            var mockMapper = CreateMock<IMapper>();
            var mockLogger = CreateLoggerMock<PatientsController>();
            var controller = new PatientsController(mockService.Object, mockMapper.Object, mockLogger.Object);

            // Assert
            controller.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullService_ShouldCreateInstanceWithNullService()
        {
            // Note: 当前实现不验证null参数，这是一个已知的技术债务
            // Controller依赖.NET的NRT（Nullable Reference Types）在编译时检查
            // 实际运行时不会抛出异常，但会在首次使用null服务时失败
            var mockMapper = CreateMock<IMapper>();
            var controller = new PatientsController(null!, mockMapper.Object, CreateLoggerMock<PatientsController>().Object);

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

            _mockService.Verify(s => s.GetPagedEntityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
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

            _mockService.Verify(s => s.GetPagedEntityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
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

            _mockService.Verify(s => s.GetPagedEntityAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
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

            var mockEntityResult = Result<PagedResult<Patient>>.Success(new PagedResult<Patient>
            {
                Items = new List<Patient>(),
                TotalCount = 0,
                CurrentPage = pageNumber,
                PageSize = pageSize
            });

            _mockService.Setup(s => s.GetPagedEntityAsync(pageNumber, pageSize, keyword))
                       .ReturnsAsync(mockEntityResult);

            // Act
            await _controller.GetList(pageNumber, pageSize, keyword);

            // Assert
            _mockService.Verify(s => s.GetPagedEntityAsync(pageNumber, pageSize, keyword), Times.Once);
        }

        [Fact]
        public async Task GetList_WithDefaultParameters_ShouldCallServiceWithDefaults()
        {
            // Arrange
            var mockEntityResult = Result<PagedResult<Patient>>.Success(new PagedResult<Patient>
            {
                Items = new List<Patient>(),
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20
            });

            _mockService.Setup(s => s.GetPagedEntityAsync(1, 20, null))
                       .ReturnsAsync(mockEntityResult);

            // Act
            await _controller.GetList();

            // Assert
            _mockService.Verify(s => s.GetPagedEntityAsync(1, 20, null), Times.Once);
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
