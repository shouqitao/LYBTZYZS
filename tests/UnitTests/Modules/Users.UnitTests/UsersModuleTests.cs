using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Repositories;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Microsoft.EntityFrameworkCore;
using LYBT.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using LYBT.Module.Users.Mapping;
using LYBT.Infrastructure.Configuration.Services;
using Moq;

namespace LYBT.Module.Users.Tests
{
    public class UsersModuleTests
    {
        [Fact]
        public void AddUsersModuleServices_Should_Register_All_Services()
        {
            // Arrange
            var services = new ServiceCollection();

            // 添加必要的配置
            services.AddLogging();
            
            // 添加AppDbContext - 使用InMemory数据库
            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            });
            
            // 添加IMemoryCache
            services.AddMemoryCache();
            
            // 添加AutoMapper
            services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
            
            // 添加DefaultPasswordService及其依赖
            services.Configure<DefaultPasswordOptions>(options =>
            {
                options.SystemAdmin = "AdminPass123!";
                options.NewUser = "DefaultPass123!";
                options.EnableInDevelopment = true;
                options.OnlyWhenDatabaseEmpty = false;
                options.ExpiryDays = 30;
            });
            
            // Mock IWebHostEnvironment
            var mockEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
            services.AddSingleton(mockEnvironment.Object);
            
            services.AddScoped<DefaultPasswordService>();
            services.Configure<UserOptions>(options =>
            {
                options.EnableUserCache = true;
                options.UserCacheExpirationMinutes = 30;
                options.MaxBatchOperationSize = 100;
                options.EnableDetailedAuditLogging = true;
                options.SendPasswordResetNotification = false;
                options.SessionTimeoutMinutes = 480;
                options.EnableOnlineStatusTracking = true;
            });

            // Act
            services.AddUsersModuleServices();
            var serviceProvider = services.BuildServiceProvider();

            // Assert - 验证Repository注册
            serviceProvider.GetService<IUserRepository>()
                .Should().NotBeNull("IUserRepository should be registered");

            // Assert - 验证Service注册

            serviceProvider.GetService<LYBT.Module.Users.Interfaces.IUserService>()
                .Should().NotBeNull("IUserService should be registered");

            // Assert - 验证Options注册
            serviceProvider.GetService<IOptions<UserOptions>>()
                .Should().NotBeNull("UserOptions should be registered");
        }

        [Fact]
        public void AddUsersModuleServices_Should_Register_Services_As_Scoped()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.Configure<UserOptions>(options => { });

            // Act
            services.AddUsersModuleServices();

            // Assert - 验证服务生命周期
            services.Should().Contain(x =>
                x.ServiceType == typeof(IUserRepository) &&
                x.Lifetime == ServiceLifetime.Scoped);

            

            services.Should().Contain(x =>
                x.ServiceType == typeof(LYBT.Module.Users.Interfaces.IUserService) &&
                x.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddUsersModuleServices_Should_Return_ServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddUsersModuleServices();

            // Assert
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddUsersModuleServices_Can_Be_Called_Multiple_Times()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddUsersModuleServices();
            services.AddUsersModuleServices(); // Should not throw

            // Assert
            services.Should().NotBeNull();
        }
    }
}