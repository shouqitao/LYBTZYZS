using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using LYBT.WebAPI.Controllers;

namespace Security.UnitTests
{
    /// <summary>
    /// 授权配置测试 - 验证控制器和方法的授权属性配置是否正确
    /// </summary>
    public class AuthorizationTests
    {
        [Fact]
        public void AuthController_Login_ShouldAllowAnonymous()
        {
            // Arrange
            var method = typeof(AuthController).GetMethod("LoginAsync");

            // Act
            var allowAnonymous = method?.GetCustomAttribute<AllowAnonymousAttribute>();

            // Assert
            allowAnonymous.Should().NotBeNull("登录端点应允许匿名访问");
        }

        [Fact]
        public void AuthController_ChangeSysAdminPassword_ShouldRequireAdminRole()
        {
            // Arrange
            var method = typeof(AuthController).GetMethod("ChangeSysAdminPasswordAsync");

            // Act
            var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorize.Should().NotBeNull("修改系统管理员密码应需要授权");
            authorize?.Roles.Should().Be("Admin", "修改系统管理员密码应仅限管理员");
        }

        [Fact]
        public void HealthController_BasicEndpoint_ShouldAllowAnonymous()
        {
            // Arrange
            var method = typeof(HealthController).GetMethod("Get");

            // Act
            var allowAnonymous = method?.GetCustomAttribute<AllowAnonymousAttribute>();

            // Assert
            allowAnonymous.Should().NotBeNull("基础健康检查应允许匿名访问");
        }

        [Fact]
        public void HealthController_DetailedEndpoint_ShouldRequireAuthentication()
        {
            // Arrange
            var method = typeof(HealthController).GetMethod("GetDetailedHealth");

            // Act
            var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorize.Should().NotBeNull("详细健康检查应需要认证");
        }

        [Fact]
        public void UsersOperationController_ShouldRequireAdminRole()
        {
            // Arrange
            var controllerType = typeof(UsersOperationController);

            // Act
            var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorize.Should().NotBeNull("用户操作控制器应需要授权");
            authorize?.Roles.Should().Be("Admin", "用户操作控制器应仅限管理员");
        }

        [Fact]
        public void UsersController_CreateUser_ShouldRequireAdminRole()
        {
            // Arrange
            var method = typeof(UsersController).GetMethod("CreateUser");

            // Act
            var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorize.Should().NotBeNull("创建用户应需要授权");
            authorize?.Roles.Should().Be("Admin", "创建用户应仅限管理员");
        }

        [Fact]
        public void UsersController_UpdateUser_ShouldRequireAdminRole()
        {
            // Arrange
            var method = typeof(UsersController).GetMethod("UpdateUser");

            // Act
            var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorize.Should().NotBeNull("更新用户应需要授权");
            authorize?.Roles.Should().Be("Admin", "更新用户应仅限管理员");
        }

        [Fact]
        public void UsersController_ToggleStatus_ShouldRequireAdminRole()
        {
            // Arrange
            var method = typeof(UsersController).GetMethod("ToggleStatus");

            // Act
            var authorize = method?.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            authorize.Should().NotBeNull("切换用户状态应需要授权");
            authorize?.Roles.Should().Be("Admin", "切换用户状态应仅限管理员");
        }

        [Fact]
        public void AllControllers_ShouldHaveAuthorizeAttribute()
        {
            // Arrange
            var assembly = typeof(AuthController).Assembly;
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract)
                .ToList();

            // Act & Assert
            foreach (var controllerType in controllerTypes)
            {
                var hasAuthorize = controllerType.GetCustomAttribute<AuthorizeAttribute>() != null;
                var hasAllowAnonymous = controllerType.GetCustomAttribute<AllowAnonymousAttribute>() != null;

                // 每个控制器要么有 Authorize 要么有 AllowAnonymous（不应该有AllowAnonymous在控制器级别）
                (hasAuthorize || hasAllowAnonymous).Should().BeTrue(
                    $"控制器 {controllerType.Name} 应该有授权属性");

                // 确保没有控制器级别的 AllowAnonymous
                hasAllowAnonymous.Should().BeFalse(
                    $"控制器 {controllerType.Name} 不应在控制器级别使用 AllowAnonymous");
            }
        }

        [Fact]
        public void PublicEndpoints_ShouldHaveExplicitAllowAnonymous()
        {
            // 定义应该公开的端点
            var publicEndpoints = new[]
            {
                (typeof(AuthController), "LoginAsync"),
                (typeof(AuthController), "RefreshTokenAsync"),
                (typeof(HealthController), "Get"),
                (typeof(HealthController), "Ping")
            };

            foreach (var (controllerType, methodName) in publicEndpoints)
            {
                var method = controllerType.GetMethod(methodName);
                var allowAnonymous = method?.GetCustomAttribute<AllowAnonymousAttribute>();

                allowAnonymous.Should().NotBeNull(
                    $"{controllerType.Name}.{methodName} 应该明确标记为 AllowAnonymous");
            }
        }
    }
}