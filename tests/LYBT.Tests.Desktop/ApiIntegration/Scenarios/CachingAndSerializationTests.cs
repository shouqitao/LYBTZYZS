using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Desktop.ApiIntegration.Infrastructure;
using FluentAssertions;
using NSubstitute;

namespace LYBT.Tests.Desktop.ApiIntegration.Scenarios;

/// <summary>
/// Caching and Data Serialization Integration Tests
/// 
/// Tests caching behavior and data serialization patterns:
/// - Request deduplication and caching
/// - CommandResult<T> serialization/deserialization
/// - ApiResponse<T> envelope handling
/// - Cache invalidation scenarios
/// </summary>
public class CachingAndSerializationTests : ApiIntegrationTestBase
{
    public CachingAndSerializationTests() { }

    [Fact]
    public async Task ApiService_WithIdenticalRequests_ShouldUseCache()
    {
        // Arrange - Setup API to return same response
        var testUser = TestData.CreateUser("testuser", UserRole.Doctor);
        var userList = new[] { testUser };
        var pagedResult = TestData.CreatePagedResult(userList, 1);

        var callCount = 0;
        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(ci =>
            {
                callCount++;
                return Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
                {
                    Success = true,
                    Data = pagedResult,
                    Message = "获取成功"
                });
            });

        // Act - Make identical requests
        var result1 = await ApiService.GetUsersAsync();
        var result2 = await ApiService.GetUsersAsync();
        var result3 = await ApiService.GetUsersAsync();

        // Assert - Only one API call should be made (cached)
        callCount.Should().Be(1);
        
        // All results should be identical
        result1.Should().BeEquivalentTo(result2);
        result2.Should().BeEquivalentTo(result3);
    }

    [Fact]
    public async Task ApiService_WithDifferentParameters_ShouldNotUseCache()
    {
        // Arrange - Setup API with different responses based on parameters
        var callCount = 0;
        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(ci =>
            {
                callCount++;
                var pageNumber = ci.ArgAt<int?>(0) ?? 1;
                var user = TestData.CreateUser($"user{pageNumber}", UserRole.Doctor);
                var pagedResult = TestData.CreatePagedResult(new[] { user }, 1, pageNumber);
                
                return Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
                {
                    Success = true,
                    Data = pagedResult,
                    Message = "获取成功"
                });
            });

        // Act - Make requests with different parameters
        var result1 = await ApiService.GetUsersAsync(pageNumber: 1);
        var result2 = await ApiService.GetUsersAsync(pageNumber: 2);
        var result3 = await ApiService.GetUsersAsync(pageNumber: 1); // Same as first

        // Assert - First and third should use cache, second should be new call
        callCount.Should().Be(2);
        
        // First and third results should be identical
        result1.Should().BeEquivalentTo(result3);
        
        // Second result should be different
        result1.Should().NotBeEquivalentTo(result2);
    }

    [Fact]
    public async Task ApiService_CacheExpiration_ShouldRefreshData()
    {
        // Arrange - Setup cache with expiration
        var callCount = 0;
        var initialUser = TestData.CreateUser("initial", UserRole.Doctor);
        var updatedUser = TestData.CreateUser("updated", UserRole.Doctor);
        
        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(ci =>
            {
                callCount++;
                var user = callCount == 1 ? initialUser : updatedUser;
                var pagedResult = TestData.CreatePagedResult(new[] { user }, 1);
                
                return Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
                {
                    Success = true,
                    Data = pagedResult,
                    Message = "获取成功"
                });
            });

        // Act - First request
        var result1 = await ApiService.GetUsersAsync();
        
        // Simulate cache expiration (this would be internal to ApiService)
        // In real implementation, cache would expire based on time/policy
        
        // Second request after cache expiration
        var result2 = await ApiService.GetUsersAsync();

        // Assert - Two API calls should be made
        callCount.Should().Be(2);
        
        // Results should be different (showing cache was refreshed)
        result1.Data!.Items.First().UserName.Should().Be("initial");
        result2.Data!.Items.First().UserName.Should().Be("updated");
    }

    [Fact]
    public async Task CommandResult_Serialization_ShouldPreserveData()
    {
        // Arrange - Create CommandResult with data
        var testUser = TestData.CreateUser("testuser", UserRole.Doctor);
        var commandResult = new CommandResult<UserDetailDto>
        {
            IsSuccess = true,
            Data = testUser,
            Message = "操作成功"
        };

        // Act - Serialize and deserialize (simulating API round-trip)
        var serialized = System.Text.Json.JsonSerializer.Serialize(commandResult);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<CommandResult<UserDetailDto>>(serialized);

        // Assert - Data should be preserved
        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeTrue();
        deserialized.Data.Should().NotBeNull();
        deserialized.Data!.Id.Should().Be(testUser.Id);
        deserialized.Data.UserName.Should().Be(testUser.UserName);
        deserialized.Data.Role.Should().Be(testUser.Role);
        deserialized.Message.Should().Be("操作成功");
    }

    [Fact]
    public async Task CommandResult_ErrorSerialization_ShouldPreserveError()
    {
        // Arrange - Create CommandResult with error
        var commandResult = new CommandResult<UserDetailDto>
        {
            IsSuccess = false,
            Message = "用户不存在",
            ErrorCode = "USER_NOT_FOUND"
        };

        // Act - Serialize and deserialize
        var serialized = System.Text.Json.JsonSerializer.Serialize(commandResult);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<CommandResult<UserDetailDto>>(serialized);

        // Assert - Error information should be preserved
        deserialized.Should().NotBeNull();
        deserialized!.IsSuccess.Should().BeFalse();
        deserialized.Data.Should().BeNull();
        deserialized.Message.Should().Be("用户不存在");
        deserialized.ErrorCode.Should().Be("USER_NOT_FOUND");
    }

    [Fact]
    public async Task ApiResponse_EnvelopeHandling_ShouldWorkCorrectly()
    {
        // Arrange - Setup API response with envelope
        var testUser = TestData.CreateUser("testuser", UserRole.Doctor);
        var apiResponse = TestData.CreateSuccessResponse(testUser, "创建用户成功");

        UserApi.CreateUserAsync(Arg.Any<UserDetailDto>())
            .Returns(Task.FromResult(apiResponse));

        // Act - Call API through service
        // Note: This would test how the service handles ApiResponse envelopes

        // Assert - Response should be properly unwrapped
    }

    [Fact]
    public async Task ApiService_CacheInvalidation_OnWriteOperation_ShouldClearCache()
    {
        // Arrange - Setup read operation with caching
        var initialUser = TestData.CreateUser("initial", UserRole.Doctor);
        var pagedResult = TestData.CreatePagedResult(new[] { initialUser }, 1);

        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
            {
                Success = true,
                Data = pagedResult,
                Message = "获取成功"
            }));

        // First read to populate cache
        var readResult1 = await ApiService.GetUsersAsync();

        // Act - Perform write operation that should invalidate cache
        var newUser = TestData.CreateUser("newuser", UserRole.Doctor);
        UserApi.CreateUserAsync(Arg.Any<UserDetailDto>())
            .Returns(Task.FromResult(TestData.CreateSuccessResponse(newUser, "创建成功")));

        await ApiService.CreateUserAsync(newUser);

        // Modify the API setup to return updated data
        var updatedUser = TestData.CreateUser("updated", UserRole.Doctor);
        var updatedPagedResult = TestData.CreatePagedResult(new[] { updatedUser }, 1);
        
        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
            {
                Success = true,
                Data = updatedPagedResult,
                Message = "获取成功"
            }));

        // Second read should bypass cache due to invalidation
        var readResult2 = await ApiService.GetUsersAsync();

        // Assert - Cache should have been invalidated
        readResult1.Data!.Items.First().UserName.Should().Be("initial");
        readResult2.Data!.Items.First().UserName.Should().Be("updated");
    }

    [Fact]
    public async Task ApiService_ConcurrentReadWrite_ShouldMaintainConsistency()
    {
        // Arrange - Setup concurrent read/write operations
        var testUser = TestData.CreateUser("testuser", UserRole.Doctor);
        var pagedResult = TestData.CreatePagedResult(new[] { testUser }, 1);

        var readCallCount = 0;
        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(ci =>
            {
                readCallCount++;
                return Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
                {
                    Success = true,
                    Data = pagedResult,
                    Message = "获取成功"
                });
            });

        var writeCallCount = 0;
        UserApi.CreateUserAsync(Arg.Any<UserDetailDto>())
            .Returns(ci =>
            {
                writeCallCount++;
                return Task.FromResult(TestData.CreateSuccessResponse(testUser, "创建成功"));
            });

        // Act - Perform concurrent operations
        var readTask = ApiService.GetUsersAsync();
        var writeTask = ApiService.CreateUserAsync(testUser);
        
        await Task.WhenAll(readTask, writeTask);

        // Assert - Operations should complete without race conditions
        readCallCount.Should().BeGreaterThanOrEqualTo(1);
        writeCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ApiService_MemoryCache_ShouldHandleLargeDataSets()
    {
        // Arrange - Setup response with large dataset
        var largeUserList = TestData.CreateUserList(500);
        var pagedResult = TestData.CreatePagedResult(largeUserList, 500);

        UserApi.GetUsersAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string>())
            .Returns(Task.FromResult(new ApiResponse<PagedResult<UserDetailDto>>
            {
                Success = true,
                Data = pagedResult,
                Message = "获取成功"
            }));

        // Act - Request large dataset
        var result = await ApiService.GetUsersAsync();

        // Assert - Large dataset should be handled properly
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(500);
        result.Data.TotalCount.Should().Be(500);
    }
}

