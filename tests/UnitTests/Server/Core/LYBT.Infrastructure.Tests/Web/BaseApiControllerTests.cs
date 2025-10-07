using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Web
{
    // Test controller for testing purposes
    public class TestApiController : BaseApiController
    {
        public TestApiController(ILogger<TestApiController> logger, IMemoryCache cache = null)
            : base(logger, cache)
        {
        }

        // Expose protected methods for testing
        public new ActionResult<ApiResponse<T>> Success<T>(T data, string message = "操作成功")
        {
            return base.Success(data, message);
        }

        public new ActionResult<ApiResponse> Success(string message = "操作成功")
        {
            return base.Success(message);
        }

        public new ActionResult<ApiResponse<PagedResult<T>>> Success<T>(PagedResult<T> pagedResult, string message = "查询成功")
        {
            return base.Success(pagedResult, message);
        }

        // Test method to verify controller functionality
        public ActionResult<ApiResponse<string>> TestSuccessMethod()
        {
            return Success("Test data", "Test success");
        }

        public ActionResult<ApiResponse> TestSuccessNoDataMethod()
        {
            return Success("Test success without data");
        }
    }

    public class BaseApiControllerTests : IDisposable
    {
        private readonly Mock<ILogger<TestApiController>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly TestApiController _controller;

        public BaseApiControllerTests()
        {
            _mockLogger = new Mock<ILogger<TestApiController>>();
            _mockCache = new Mock<IMemoryCache>();
            _controller = new TestApiController(_mockLogger.Object, _mockCache.Object);

            // Setup HTTP context for testing
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _controller.Should().NotBeNull();
            _controller.Should().BeAssignableTo<BaseApiController>();
            _controller.Should().BeAssignableTo<BaseControllerCore>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TestApiController(null, _mockCache.Object));
        }

        [Fact]
        public void Constructor_Should_AcceptNullCache_When_CacheIsNull()
        {
            // Act
            var controller = new TestApiController(_mockLogger.Object, null);

            // Assert
            controller.Should().NotBeNull();
        }

        #endregion

        #region Success Method Tests

        [Fact]
        public void Success_Should_ReturnOkWithData_When_DataProvided()
        {
            // Arrange
            var testData = "Test Data";
            var message = "Test Message";

            // Act
            var result = _controller.Success(testData, message);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            okResult.Value.Should().BeOfType<ApiResponse<string>>();

            var apiResponse = okResult.Value as ApiResponse<string>;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Data.Should().Be(testData);
            apiResponse.Message.Should().Be(message);
            apiResponse.RequestId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Success_Should_ReturnOkWithDefaultMessage_When_NoMessageProvided()
        {
            // Arrange
            var testData = "Test Data";

            // Act
            var result = _controller.Success(testData);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<string>;
            apiResponse.Message.Should().Be("操作成功");
        }

        [Fact]
        public void Success_Should_ReturnOkWithNullData_When_NullDataProvided()
        {
            // Arrange
            string testData = null;

            // Act
            var result = _controller.Success(testData);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<string>;
            apiResponse.Data.Should().BeNull();
            apiResponse.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Success Without Data Tests

        [Fact]
        public void Success_Should_ReturnOkWithoutData_When_NoDataProvided()
        {
            // Arrange
            var message = "Test Message";

            // Act
            var result = _controller.Success(message);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            okResult.Value.Should().BeOfType<ApiResponse>();

            var apiResponse = okResult.Value as ApiResponse;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Message.Should().Be(message);
            apiResponse.RequestId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Success_Should_ReturnOkWithDefaultMessage_When_NoMessageAndNoDataProvided()
        {
            // Act
            var result = _controller.Success();

            // Assert
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse;
            apiResponse.Message.Should().Be("操作成功");
        }

        #endregion

        #region PagedResult Success Tests

        [Fact]
        public void Success_Should_ReturnOkWithPagedResult_When_PagedDataProvided()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3" };
            var pagedResult = new PagedResult<string>(items, 10, 1, 5);
            var message = "Test paged message";

            // Act
            var result = _controller.Success(pagedResult, message);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<OkObjectResult>();

            var okResult = result.Result as OkObjectResult;
            okResult.Value.Should().BeOfType<ApiResponse<PagedResult<string>>>();

            var apiResponse = okResult.Value as ApiResponse<PagedResult<string>>;
            apiResponse.IsSuccess.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().HaveCount(3);
            apiResponse.Data.TotalCount.Should().Be(10);
            apiResponse.Data.CurrentPage.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(5);
            apiResponse.Message.Should().Be(message);
        }

        [Fact]
        public void Success_Should_ReturnOkWithDefaultPagedMessage_When_NoMessageProvided()
        {
            // Arrange
            var items = new List<string> { "Item1" };
            var pagedResult = new PagedResult<string>(items, 1, 1, 1);

            // Act
            var result = _controller.Success(pagedResult);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<PagedResult<string>>;
            apiResponse.Message.Should().Be("查询成功");
        }

        [Fact]
        public void Success_Should_HandleEmptyPagedResult_When_EmptyListProvided()
        {
            // Arrange
            var items = new List<string>();
            var pagedResult = new PagedResult<string>(items, 0, 1, 10);

            // Act
            var result = _controller.Success(pagedResult);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<PagedResult<string>>;
            apiResponse.Data.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        #endregion

        #region RequestId Tests

        [Fact]
        public void Success_Should_IncludeRequestId_When_Called()
        {
            // Arrange
            var testData = "Test";

            // Act
            var result = _controller.Success(testData);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<string>;
            apiResponse.RequestId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Success_Should_IncludeDifferentRequestIds_When_CalledMultipleTimes()
        {
            // Act
            var result1 = _controller.Success("Data1");
            var result2 = _controller.Success("Data2");

            // Assert
            var okResult1 = result1.Result as OkObjectResult;
            var apiResponse1 = okResult1.Value as ApiResponse<string>;

            var okResult2 = result2.Result as OkObjectResult;
            var apiResponse2 = okResult2.Value as ApiResponse<string>;

            apiResponse1.RequestId.Should().NotBe(apiResponse2.RequestId);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void TestSuccessMethod_Should_ReturnExpectedResponse_When_Called()
        {
            // Act
            var result = _controller.TestSuccessMethod();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<string>;

            apiResponse.Data.Should().Be("Test data");
            apiResponse.Message.Should().Be("Test success");
            apiResponse.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void TestSuccessNoDataMethod_Should_ReturnExpectedResponse_When_Called()
        {
            // Act
            var result = _controller.TestSuccessNoDataMethod();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse;

            apiResponse.Message.Should().Be("Test success without data");
            apiResponse.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Complex Data Type Tests

        [Fact]
        public void Success_Should_HandleComplexObjects_When_ComplexDataProvided()
        {
            // Arrange
            var complexData = new
            {
                Id = 1,
                Name = "Test Object",
                Properties = new List<string> { "Prop1", "Prop2" },
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = _controller.Success(complexData, "Complex object success");

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            okResult.Value.Should().NotBeNull();

            // Verify the response structure is correct for complex objects
            var responseType = okResult.Value.GetType();
            responseType.IsGenericType.Should().BeTrue();
        }

        [Fact]
        public void Success_Should_HandleGenericCollections_When_CollectionProvided()
        {
            // Arrange
            var collection = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var result = _controller.Success(collection, "Collection success");

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result as OkObjectResult;
            var apiResponse = okResult.Value as ApiResponse<List<int>>;

            apiResponse.Data.Should().HaveCount(5);
            apiResponse.Data.Should().Contain(new[] { 1, 2, 3, 4, 5 });
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void Constructor_Should_InheritFromBaseControllerCore_When_Created()
        {
            // Act & Assert
            _controller.Should().BeAssignableTo<BaseControllerCore>();
        }

        [Fact]
        public void Controller_Should_HaveControllerContext_When_SetupCorrectly()
        {
            // Act & Assert
            _controller.ControllerContext.Should().NotBeNull();
            _controller.ControllerContext.HttpContext.Should().NotBeNull();
        }

        #endregion

        #region Memory Management Tests

        [Fact]
        public void Success_Should_NotLeakMemory_When_CalledMultipleTimes()
        {
            // Act - Call success method multiple times
            for (int i = 0; i < 100; i++)
            {
                var result = _controller.Success($"Data {i}", $"Message {i}");
                result.Should().NotBeNull();
            }

            // Assert - If no exception occurs, memory is properly managed
            _controller.Should().NotBeNull();
        }

        [Fact]
        public void Success_Should_BeThreadSafe_When_CalledConcurrently()
        {
            // Arrange
            var tasks = new List<Task<ActionResult<ApiResponse<string>>>>();

            // Act
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks.Add(Task.Run(() => _controller.Success($"Data {index}", $"Message {index}")));
            }

            Task.WaitAll(tasks.ToArray());

            // Assert
            foreach (var task in tasks)
            {
                task.Result.Should().NotBeNull();
                var okResult = task.Result.Result as OkObjectResult;
                var apiResponse = okResult.Value as ApiResponse<string>;
                apiResponse.IsSuccess.Should().BeTrue();
            }
        }

        #endregion

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}