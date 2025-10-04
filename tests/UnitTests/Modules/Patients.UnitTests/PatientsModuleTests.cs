using FluentAssertions;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Options;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Module.Patients.Tests
{
    /// <summary>
    /// PatientsModule 模块注册单元测试
    /// 验证服务注册和中间件配置
    /// </summary>
    public class PatientsModuleTests
    {
        [Fact]
        public void AddPatientsModule_Should_Register_Repository_Services()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Modules:Patients:MaxPageSize", "100" },
                    { "Modules:Patients:DefaultPageSize", "20" }
                })
                .Build();

            // Act
            services.AddPatientsModule(configuration);

            // Assert - 验证 Repository 注册
            var repositoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPatientRepository));
            repositoryDescriptor.Should().NotBeNull();
            repositoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
            repositoryDescriptor.ImplementationType.Should().Be(typeof(PatientRepository));
        }

        [Fact]
        public void AddPatientsModule_Should_Register_Service_Services()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Modules:Patients:MaxPageSize", "100" }
                })
                .Build();

            // Act
            services.AddPatientsModule(configuration);

            // Assert - 验证 Service 注册
            var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPatientService));
            serviceDescriptor.Should().NotBeNull();
            serviceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
            serviceDescriptor.ImplementationType.Should().Be(typeof(PatientService));
        }

        [Fact]
        public void AddPatientsModule_Should_Register_Options()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Modules:Patients:MaxPageSize", "200" },
                    { "Modules:Patients:DefaultPageSize", "50" }
                })
                .Build();

            // Act
            services.AddPatientsModule(configuration);

            // Assert - 验证 Options 注册
            var optionsDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(IOptions<>) &&
                d.Lifetime == ServiceLifetime.Singleton);
            optionsDescriptor.Should().NotBeNull();

            // 验证配置绑定 - 需要手动添加 Options 服务
            services.AddOptions();
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<PatientModuleOptions>>();
            options.Should().NotBeNull();
            options!.Value.MaxPageSize.Should().Be(200);
            options.Value.DefaultPageSize.Should().Be(50);
        }

        [Fact]
        public void AddPatientsModule_Should_Return_ServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Modules:Patients:MaxPageSize", "100" }
                })
                .Build();

            // Act
            var result = services.AddPatientsModule(configuration);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void UsePatientsModule_Should_Return_ApplicationBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            var result = appBuilder.UsePatientsModule();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(appBuilder);
        }
    }
}
