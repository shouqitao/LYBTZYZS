using FluentAssertions;
using LYBT.Tests.Configuration;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;
using Xunit;

namespace LYBT.IntegrationTests.Controllers
{
    /// <summary>
    /// UsersController 集成测试
    /// 测试用户管理API的端到端功能
    /// </summary>
    public class UsersControllerTests : IntegrationTestBase
    {
        public UsersControllerTests()
            : base()
        {
        }

        #region GetUsers Tests

        [Fact]
        public async Task GetUsers_ShouldReturnPagedUsers()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await Client.GetAsync("/api/users?page=1&pageSize=10");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<UserDetailDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().NotBeEmpty();
            apiResponse.Data.CurrentPage.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetUsers_WithSearchKeyword_ShouldReturnFilteredUsers()
        {
            // Arrange
            await SeedTestDataAsync();
            var searchKeyword = "Admin";

            // Act
            var response = await Client.GetAsync($"/api/users?page=1&pageSize=10&searchKeyword={searchKeyword}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<PagedResult<UserDetailDto>>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Items.Should().AllSatisfy(user => 
                user.UserName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                user.RealName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetUsers_WithInvalidPage_ShouldReturnValidationError()
        {
            // Act
            var response = await Client.GetAsync("/api/users?page=0&pageSize=10");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetUserById Tests

        [Fact]
        public async Task GetUserById_WithValidId_ShouldReturnUser()
        {
            // Arrange
            await SeedTestDataAsync();
            var adminUser = await GetAdminUserAsync();

            // Act
            var response = await Client.GetAsync($"/api/users/{adminUser.Id}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<UserDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Id.Should().Be(adminUser.Id);
            apiResponse.Data.UserName.Should().Be(adminUser.UserName);
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"/api/users/{nonExistentId}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUserById_WithEmptyId_ShouldReturnBadRequest()
        {
            // Act
            var response = await Client.GetAsync("/api/users/00000000-0000-0000-0000-000000000000");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUser_WithValidData_ShouldReturnCreatedUser()
        {
            // Arrange
            var createUserDto = new UserInputDto
            {
                UserName = "testuser",
                RealName = "测试用户",
                Email = "test@example.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor,
                Department = "内科"
            };

            var content = CreateJsonContent(createUserDto);

            // Act
            var response = await Client.PostAsync("/api/users", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Created);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<UserDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.UserName.Should().Be(createUserDto.UserName);
            apiResponse.Data.RealName.Should().Be(createUserDto.RealName);
            apiResponse.Data.Role.Should().Be(createUserDto.Role);
            apiResponse.Data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_WithDuplicateUserName_ShouldReturnConflict()
        {
            // Arrange
            await SeedTestDataAsync();
            var adminUser = await GetAdminUserAsync();
            
            var createUserDto = new UserInputDto
            {
                UserName = adminUser.UserName, // 重复的用户名
                RealName = "重复用户",
                Email = "duplicate@example.com",
                Role = UserRole.Doctor
            };

            var content = CreateJsonContent(createUserDto);

            // Act
            var response = await Client.PostAsync("/api/users", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task CreateUser_WithInvalidEmail_ShouldReturnValidationError()
        {
            // Arrange
            var createUserDto = new UserInputDto
            {
                UserName = "invaliduser",
                RealName = "无效用户",
                Email = "invalid-email", // 无效邮箱
                Role = UserRole.Doctor
            };

            var content = CreateJsonContent(createUserDto);

            // Act
            var response = await Client.PostAsync("/api/users", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateUser_WithEmptyData_ShouldReturnValidationError()
        {
            // Arrange
            var createUserDto = new UserInputDto(); // 空数据

            var content = CreateJsonContent(createUserDto);

            // Act
            var response = await Client.PostAsync("/api/users", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_WithValidData_ShouldReturnUpdatedUser()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetAdminUserAsync();
            
            var updateUserDto = new UserInputDto
            {
                Id = user.Id,
                RealName = "更新后的管理员",
                Email = "updated@example.com",
                PhoneNumber = "13900139000",
                Department = "外科"
            };

            var content = CreateJsonContent(updateUserDto);

            // Act
            var response = await Client.PutAsync($"/api/users/{user.Id}", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
            
            var apiResponse = await response.ShouldBeSuccessfulApiResponseAsync<UserDetailDto>();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Id.Should().Be(user.Id);
            apiResponse.Data.RealName.Should().Be(updateUserDto.RealName);
            apiResponse.Data.Email.Should().Be(updateUserDto.Email);
            apiResponse.Data.PhoneNumber.Should().Be(updateUserDto.PhoneNumber);
            apiResponse.Data.Department.Should().Be(updateUserDto.Department);
        }

        [Fact]
        public async Task UpdateUser_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateUserDto = new UserInputDto
            {
                Id = nonExistentId,
                RealName = "不存在的用户"
            };

            var content = CreateJsonContent(updateUserDto);

            // Act
            var response = await Client.PutAsync($"/api/users/{nonExistentId}", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateUser_WithMismatchedId_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateUserDto = new UserInputDto
            {
                Id = Guid.NewGuid(), // 不匹配的ID
                RealName = "ID不匹配的用户"
            };

            var content = CreateJsonContent(updateUserDto);

            // Act
            var response = await Client.PutAsync($"/api/users/{userId}", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetTestUserAsync("testuser");

            // Act
            var response = await Client.DeleteAsync($"/api/users/{user.Id}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            // 验证用户已被删除
            var getResponse = await Client.GetAsync($"/api/users/{user.Id}");
            getResponse.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteUser_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"/api/users/{nonExistentId}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteUser_WithAdminUser_ShouldReturnForbidden()
        {
            // Arrange
            await SeedTestDataAsync();
            var adminUser = await GetAdminUserAsync();

            // Act
            var response = await Client.DeleteAsync($"/api/users/{adminUser.Id}");

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetTestUserAsync("testuser");
            
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "DefaultPassword123!",
                NewPassword = "NewPassword456!",
                ConfirmPassword = "NewPassword456!"
            };

            var content = CreateJsonContent(changePasswordDto);

            // Act
            var response = await Client.PostAsync($"/api/users/{user.Id}/change-password", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ChangePassword_WithWrongCurrentPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetTestUserAsync("testuser");
            
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPassword456!",
                ConfirmPassword = "NewPassword456!"
            };

            var content = CreateJsonContent(changePasswordDto);

            // Act
            var response = await Client.PostAsync($"/api/users/{user.Id}/change-password", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangePassword_WithPasswordMismatch_ShouldReturnValidationError()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetTestUserAsync("testuser");
            
            var changePasswordDto = new ChangePasswordDto
            {
                CurrentPassword = "DefaultPassword123!",
                NewPassword = "NewPassword456!",
                ConfirmPassword = "DifferentPassword" // 不匹配
            };

            var content = CreateJsonContent(changePasswordDto);

            // Act
            var response = await Client.PostAsync($"/api/users/{user.Id}/change-password", content);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Activate/Deactivate User Tests

        [Fact]
        public async Task ActivateUser_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetTestUserAsync("testuser");
            
            // 先停用用户
            await Client.PostAsync($"/api/users/{user.Id}/deactivate", null);

            // Act
            var response = await Client.PostAsync($"/api/users/{user.Id}/activate", null);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            // 验证用户已激活
            var getResponse = await Client.GetAsync($"/api/users/{user.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<UserDetailDto>();
            apiResponse.Data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task DeactivateUser_WithValidId_ShouldReturnSuccess()
        {
            // Arrange
            await SeedTestDataAsync();
            var user = await GetTestUserAsync("testuser");

            // Act
            var response = await Client.PostAsync($"/api/users/{user.Id}/deactivate", null);

            // Assert
            response.ShouldHaveStatusCode(HttpStatusCode.OK);

            // 验证用户已停用
            var getResponse = await Client.GetAsync($"/api/users/{user.Id}");
            var apiResponse = await getResponse.ShouldBeSuccessfulApiResponseAsync<UserDetailDto>();
            apiResponse.Data.IsActive.Should().BeFalse();
        }

        #endregion

        #region Helper Methods

        private async Task<UserDetailDto> GetAdminUserAsync()
        {
            var response = await Client.GetAsync("/api/users?searchKeyword=Admin");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserDetailDto>>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return apiResponse!.Data.Items.First(u => u.UserName == "admin");
        }

        private async Task<UserDetailDto> GetTestUserAsync(string userName)
        {
            var response = await Client.GetAsync($"/api/users?searchKeyword={userName}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserDetailDto>>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            return apiResponse!.Data.Items.First(u => u.UserName == userName);
        }

        private StringContent CreateJsonContent<T>(T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        #endregion
    }
}