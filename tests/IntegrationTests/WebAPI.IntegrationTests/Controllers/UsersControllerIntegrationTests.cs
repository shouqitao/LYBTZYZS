using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// UsersController集成测试
    /// 测试用户管理API的端到端功能
    /// </summary>
    public class UsersControllerIntegrationTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;
        private Guid _testUserId;
        private Guid _adminUserId;
        private const string BaseUrl = "/api/v1/users";

        public UsersControllerIntegrationTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        protected override void SeedBasicTestData(AppDbContext context)
        {
            base.SeedBasicTestData(context);

            // 创建测试用户数据
            _testUserId = Guid.NewGuid();
            _adminUserId = Guid.NewGuid();

            var testUsers = new List<User>
            {
                new User
                {
                    Id = _adminUserId,
                    UserName = "admin_test",
                    PasswordHash = "hashedpassword",
                    RealName = "测试管理员",
                    PinYinCode = "csgl",
                    Role = UserRole.Admin,
                    Status = CommonStatus.Enabled,
                    PhoneNumber = "13800138000",
                    Email = "admin@test.com",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = _testUserId,
                    UserName = "doctor_test",
                    PasswordHash = "hashedpassword",
                    RealName = "测试医生",
                    PinYinCode = "csys",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled,
                    PhoneNumber = "13800138001",
                    Email = "doctor@test.com",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Set<User>().AddRange(testUsers);
            context.SaveChanges();
        }

        #region GetUsers Tests

        [Fact]
        public async Task GetUsers_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync(BaseUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUsers_ShouldReturnPagedUsers()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDetailDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().HaveCountGreaterThan(0);
            result.Data.CurrentPage.Should().Be(1);
            result.Data.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetUsers_WithSearchKeyword_ShouldReturnFilteredUsers()
        {
            // Arrange
            var searchKeyword = "admin";

            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=1&pageSize=10&keyword={searchKeyword}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDetailDto>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            // 验证搜索结果包含关键字
            if (result.Data!.Items.Any())
            {
                result.Data.Items.Should().Contain(u =>
                    u.UserName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase) ||
                    u.RealName.Contains(searchKeyword, StringComparison.OrdinalIgnoreCase));
            }
        }

        [Fact]
        public async Task GetUsers_WithInvalidPage_ShouldReturnBadRequest()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}?page=0&pageSize=10");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region GetUserById Tests

        [Fact]
        public async Task GetUserById_WithValidId_ShouldReturnUser()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{_testUserId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(_testUserId);
            result.Data.UserName.Should().Be("doctor_test");
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetUserById_WithEmptyId_ShouldReturnBadRequest()
        {
            // Act
            var response = await Client.GetAsync($"{BaseUrl}/{Guid.Empty}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUser_WithValidData_ShouldReturnCreatedUser()
        {
            // Arrange
            var uniqueSuffix = DateTime.Now.Ticks;
            var createUserDto = new UserInputDto
            {
                UserName = $"newuser_{uniqueSuffix}",
                RealName = "新用户",
                Email = $"newuser_{uniqueSuffix}@test.com",
                PhoneNumber = "13900139000",
                Role = UserRole.Doctor
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, createUserDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.UserName.Should().Be(createUserDto.UserName);
            result.Data.RealName.Should().Be(createUserDto.RealName);
            result.Data.Role.Should().Be(UserRole.Doctor);
            result.Data.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_WithDuplicateUserName_ShouldReturnConflict()
        {
            // Arrange
            var createUserDto = new UserInputDto
            {
                UserName = "doctor_test", // 已存在的用户名
                RealName = "重复用户",
                Email = "duplicate@test.com",
                Role = UserRole.Doctor
            };

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, createUserDto);

            // Assert
            // 可能返回Conflict或BadRequest，取决于实现
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
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

            // Act
            var response = await Client.PostAsJsonAsync(BaseUrl, createUserDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_WithValidData_ShouldReturnUpdatedUser()
        {
            // Arrange
            var updateUserDto = new UserInputDto
            {
                Id = _testUserId,
                RealName = "更新后的医生",
                Email = "updated@test.com",
                PhoneNumber = "13900139001"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{_testUserId}", updateUserDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(_testUserId);
            result.Data.RealName.Should().Be("更新后的医生");
            result.Data.Email.Should().Be("updated@test.com");
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

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", updateUserDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateUser_WithMismatchedId_ShouldReturnBadRequest()
        {
            // Arrange
            var urlId = Guid.NewGuid();
            var updateUserDto = new UserInputDto
            {
                Id = Guid.NewGuid(), // 不匹配的ID
                RealName = "ID不匹配的用户"
            };

            // Act
            var response = await Client.PutAsJsonAsync($"{BaseUrl}/{urlId}", updateUserDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_WithValidId_ShouldReturnSuccess()
        {
            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{_testUserId}");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

            // 验证用户已被删除
            var getResponse = await Client.GetAsync($"{BaseUrl}/{_testUserId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteUser_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"{BaseUrl}/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_WithPasswordMismatch_ShouldReturnValidationError()
        {
            // Arrange
            var changePasswordDto = new ChangePasswordDto
            {
                UserId = _testUserId,
                OldPassword = "DefaultPassword123!",
                NewPassword = "NewPassword456!",
                ConfirmNewPassword = "DifferentPassword" // 不匹配
            };

            // Act
            var response = await Client.PostAsJsonAsync($"{BaseUrl}/{_testUserId}/change-password", changePasswordDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Status Toggle Tests

        [Fact]
        public async Task ToggleUserStatus_ShouldUpdateStatus()
        {
            // Act - 停用用户
            var disableResponse = await Client.PostAsync($"{BaseUrl}/{_testUserId}/disable", null);

            // Assert
            disableResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

            // 验证用户状态
            var getResponse = await Client.GetAsync($"{BaseUrl}/{_testUserId}");
            if (getResponse.StatusCode == HttpStatusCode.OK)
            {
                var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserDetailDto>>();
                result?.Data?.IsEnabled.Should().BeFalse();
            }

            // Act - 启用用户
            var enableResponse = await Client.PostAsync($"{BaseUrl}/{_testUserId}/enable", null);

            // Assert
            enableResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        }

        #endregion
    }
}
