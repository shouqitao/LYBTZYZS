using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Module.Auth.Tests
{
    public class AuthModuleTests
    {
        [Fact]
        public void AddAuthModule_Should_Register_All_Services()
        {
            // Arrange
            var services = new ServiceCollection();

            // 添加必要的配置
            services.AddLogging();
            services.Configure<AuthOptions>(options =>
            {
                options.MaxFailedLoginAttempts = 5;
                options.AccountLockoutDuration = TimeSpan.FromMinutes(15);
            });
            services.Configure<SysAdminOptions>(options =>
            {
                options.Username = "sysadmin";
                options.DefaultPassword = "LybtAdmin2025@SecurePass!";
            });
            services.Configure<JwtOptions>(options =>
            {
                options.Secret = "test-secret-key-for-jwt-authentication-minimum-32-characters";
                options.Issuer = "test-issuer";
                options.Audience = "test-audience";
                options.ExpireMinutes = 60;
            });

            // Act
            services.AddAuthModule();
            var serviceProvider = services.BuildServiceProvider();

            // Assert - 验证Repository注册
            serviceProvider.GetService<IAuthRepository>()
                .Should().NotBeNull("IAuthRepository should be registered");

            // Assert - 验证Service注册
            serviceProvider.GetService<IAuthQueryService>()
                .Should().NotBeNull("IAuthQueryService should be registered");

            serviceProvider.GetService<IAuthBusinessService>()
                .Should().NotBeNull("IAuthBusinessService should be registered");

            serviceProvider.GetService<IAuthService>()
                .Should().NotBeNull("IAuthService should be registered");

            // Assert - 验证JWT服务注册
            serviceProvider.GetService<IJwtAuthenticationService>()
                .Should().NotBeNull("IJwtAuthenticationService should be registered");

            // Assert - 验证SysAdminHandler注册
            serviceProvider.GetService<ISysAdminHandler>()
                .Should().NotBeNull("ISysAdminHandler should be registered");

            // Assert - 验证Options注册
            serviceProvider.GetService<IOptions<AuthOptions>>()
                .Should().NotBeNull("AuthOptions should be registered");

            serviceProvider.GetService<IOptions<SysAdminOptions>>()
                .Should().NotBeNull("SysAdminOptions should be registered");
        }

        [Fact]
        public void AddAuthModule_Should_Register_Services_As_Scoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.Configure<AuthOptions>(options => { });
            services.Configure<SysAdminOptions>(options => { });
            services.Configure<JwtOptions>(options => { });

            // Act
            services.AddAuthModule();

            // Assert - 验证服务生命周期
            services.Should().Contain(x =>
                x.ServiceType == typeof(IAuthRepository) &&
                x.Lifetime == ServiceLifetime.Scoped);

            services.Should().Contain(x =>
                x.ServiceType == typeof(IAuthQueryService) &&
                x.Lifetime == ServiceLifetime.Scoped);

            services.Should().Contain(x =>
                x.ServiceType == typeof(IAuthBusinessService) &&
                x.Lifetime == ServiceLifetime.Scoped);

            services.Should().Contain(x =>
                x.ServiceType == typeof(IAuthService) &&
                x.Lifetime == ServiceLifetime.Scoped);

            services.Should().Contain(x =>
                x.ServiceType == typeof(IJwtAuthenticationService) &&
                x.Lifetime == ServiceLifetime.Scoped);

            services.Should().Contain(x =>
                x.ServiceType == typeof(ISysAdminHandler) &&
                x.Lifetime == ServiceLifetime.Scoped);
        }
    }
}