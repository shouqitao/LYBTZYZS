using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using LYBT.Infrastructure.Services;

namespace LYBT.Infrastructure.Services.Tests
{
    /// <summary>
    /// BaseService统一权限验证基类单元测试
    /// Epic #1612: MedicalCase模块权限优化 - Phase 2 Task 2.2
    /// </summary>
    public class BaseServiceTests
    {
        private readonly Mock<ILogger<BaseServiceTests>> _loggerMock;
        private readonly TestableBaseService _baseService;

        public BaseServiceTests()
        {
            _loggerMock = new Mock<ILogger<BaseServiceTests>>();
            _baseService = new TestableBaseService(_loggerMock.Object);
        }

        #region 编辑权限验证测试

        [Fact]
        public void ValidateEditPermission_AdminUser_ShouldReturnAuthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var createdUserId = Guid.NewGuid();
            var createdDate = DateTime.Today.AddDays(-1); // 非当天创建
            var isAdmin = true;
            var entityType = "测试实体";

            // Act
            var result = _baseService.ValidateEditPermission(
                entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType);

            // Assert
            Assert.True(result.IsAuthorized);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void ValidateEditPermission_OwnerToday_ShouldReturnAuthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdDate = DateTime.Today; // 当天创建
            var isAdmin = false;
            var entityType = "测试实体";

            // Act
            var result = _baseService.ValidateEditPermission(
                entityId, userId, userId, createdDate, isAdmin, entityType);

            // Assert
            Assert.True(result.IsAuthorized);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void ValidateEditPermission_OwnerButNotToday_ShouldReturnUnauthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdDate = DateTime.Today.AddDays(-1); // 非当天创建
            var isAdmin = false;
            var entityType = "测试实体";

            // Act
            var result = _baseService.ValidateEditPermission(
                entityId, userId, userId, createdDate, isAdmin, entityType);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Contains("只能编辑当天创建的测试实体", result.ErrorMessage);
        }

        [Fact]
        public void ValidateEditPermission_DifferentOwner_ShouldReturnUnauthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var createdUserId = Guid.NewGuid();
            var createdDate = DateTime.Today;
            var isAdmin = false;
            var entityType = "测试实体";

            // Act
            var result = _baseService.ValidateEditPermission(
                entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Contains("只能编辑自己创建的测试实体", result.ErrorMessage);
        }

        #endregion

        #region 删除权限验证测试

        [Fact]
        public void ValidateDeletePermission_AdminUser_ShouldReturnAuthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var createdUserId = Guid.NewGuid();
            var createdDate = DateTime.Today.AddDays(-1);
            var isAdmin = true;
            var entityType = "测试实体";
            var hasRelatedData = true;

            // Act
            var result = _baseService.ValidateDeletePermission(
                entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType, hasRelatedData);

            // Assert
            Assert.True(result.IsAuthorized);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void ValidateDeletePermission_OwnerTodayNoRelatedData_ShouldReturnAuthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdDate = DateTime.Today;
            var isAdmin = false;
            var entityType = "测试实体";
            var hasRelatedData = false;

            // Act
            var result = _baseService.ValidateDeletePermission(
                entityId, userId, userId, createdDate, isAdmin, entityType, hasRelatedData);

            // Assert
            Assert.True(result.IsAuthorized);
            Assert.Empty(result.ErrorMessage);
        }

        [Fact]
        public void ValidateDeletePermission_HasRelatedData_ShouldReturnUnauthorized()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdDate = DateTime.Today;
            var isAdmin = false;
            var entityType = "测试实体";
            var hasRelatedData = true;

            // Act
            var result = _baseService.ValidateDeletePermission(
                entityId, userId, userId, createdDate, isAdmin, entityType, hasRelatedData);

            // Assert
            Assert.False(result.IsAuthorized);
            Assert.Contains("存在关联数据，无法删除测试实体", result.ErrorMessage);
        }

        #endregion

        #region 用户信息提取测试

        [Fact]
        public async Task ExtractUserInfoAsync_FromMiddleware_ShouldReturnUserInfo()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var userId = Guid.NewGuid();
            var userInfo = new MedicalCaseUserInfo
            {
                UserId = userId,
                UserName = "testuser",
                Role = "Doctor",
                IsAdmin = false
            };
            httpContext.Items["MedicalCaseUserInfo"] = userInfo;

            // Act
            var result = await _baseService.ExtractUserInfoAsync(httpContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Value.UserId);
            Assert.False(result.Value.IsAdmin);
            Assert.Equal("Doctor", result.Value.Role);
        }

        [Fact]
        public async Task ExtractUserInfoAsync_FromClaims_ShouldReturnUserInfo()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            var userId = Guid.NewGuid();
            var user = CreateAuthenticatedUser(userId, "Doctor");
            httpContext.User = user;

            // Act
            var result = await _baseService.ExtractUserInfoAsync(httpContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Value.UserId);
            Assert.False(result.Value.IsAdmin);
            Assert.Equal("Doctor", result.Value.Role);
        }

        [Fact]
        public async Task ExtractUserInfoAsync_Unauthenticated_ShouldReturnNull()
        {
            // Arrange
            var httpContext = CreateHttpContext();
            httpContext.User = new ClaimsPrincipal(); // 未认证用户

            // Act
            var result = await _baseService.ExtractUserInfoAsync(httpContext);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExtractUserInfoAsync_NullHttpContext_ShouldReturnNull()
        {
            // Act
            var result = await _baseService.ExtractUserInfoAsync(null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region 辅助方法测试

        [Theory]
        [InlineData("Admin", "管理员")]
        [InlineData("Administrator", "管理员")]
        [InlineData("Doctor", "医生")]
        [InlineData("User", "用户")]
        [InlineData("Unknown", "未知角色")]
        public void GetRoleDisplayName_ShouldReturnCorrectDisplayName(string role, string expected)
        {
            // Act
            var result = BaseService.GetRoleDisplayName(role);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsToday_ShouldReturnCorrectResult(bool isToday)
        {
            // Arrange
            var date = isToday ? DateTime.Today : DateTime.Today.AddDays(-1);

            // Act
            var result = BaseService.IsToday(date);

            // Assert
            Assert.Equal(isToday, result);
        }

        #endregion

        #region 辅助方法

        private static HttpContext CreateHttpContext()
        {
            return new DefaultHttpContext();
        }

        private static ClaimsPrincipal CreateAuthenticatedUser(Guid userId, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, role)
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        #endregion
    }

    /// <summary>
    /// 可测试的BaseService实现类
    /// </summary>
    public class TestableBaseService : BaseService
    {
        public TestableBaseService(ILogger logger) : base(logger)
        {
        }

        // 公开基类的受保护方法用于测试
        public new (bool IsAuthorized, string ErrorMessage) ValidateEditPermission(
            Guid entityId, Guid currentUserId, Guid createdUserId, DateTime createdDate,
            bool isAdmin = false, string entityType = "实体")
        {
            return base.ValidateEditPermission(entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType);
        }

        public new (bool IsAuthorized, string ErrorMessage) ValidateDeletePermission(
            Guid entityId, Guid currentUserId, Guid createdUserId, DateTime createdDate,
            bool isAdmin = false, string entityType = "实体", bool hasRelatedData = false)
        {
            return base.ValidateDeletePermission(entityId, currentUserId, createdUserId, createdDate, isAdmin, entityType, hasRelatedData);
        }

        public new Task<(Guid UserId, bool IsAdmin, string Role)?> ExtractUserInfoAsync(HttpContext? context)
        {
            return base.ExtractUserInfoAsync(context);
        }

        public new static string GetRoleDisplayName(string role)
        {
            return BaseService.GetRoleDisplayName(role);
        }

        public new static bool IsToday(DateTime date)
        {
            return BaseService.IsToday(date);
        }
    }
}