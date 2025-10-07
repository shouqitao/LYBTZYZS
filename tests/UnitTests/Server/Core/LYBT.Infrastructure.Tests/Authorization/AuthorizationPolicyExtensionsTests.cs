using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Infrastructure.Authorization;
using LYBT.Shared.Utilities.Security;
using Xunit;
using FluentAssertions;

namespace LYBT.Infrastructure.Tests.Authorization
{
    public class AuthorizationPolicyExtensionsTests
    {
        private IServiceCollection _services;
        private ServiceProvider _serviceProvider;

        public AuthorizationPolicyExtensionsTests()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ReturnServiceCollection_When_Called()
        {
            // Act
            var result = _services.AddRoleAuthorizationPolicies();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(_services);
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_RegisterAuthorizationPolicies_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var authorizationOptions = _serviceProvider.GetService<IAuthorizationPolicyProvider>();

            // Assert
            authorizationOptions.Should().NotBeNull();
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ConfigureAdminPolicy_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();
            var policyProvider = _serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Act & Assert
            var adminPolicy = policyProvider.GetPolicyAsync(RoleHelper.Policies.AdminOnly).Result;
            adminPolicy.Should().NotBeNull();
            adminPolicy.Requirements.Should().HaveCount(1);
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ConfigureDoctorPolicy_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();
            var policyProvider = _serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Act & Assert
            var doctorPolicy = policyProvider.GetPolicyAsync(RoleHelper.Policies.DoctorOnly).Result;
            doctorPolicy.Should().NotBeNull();
            doctorPolicy.Requirements.Should().HaveCount(1);
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ConfigureDoctorOrAdminPolicy_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();
            var policyProvider = _serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Act & Assert
            var doctorOrAdminPolicy = policyProvider.GetPolicyAsync(RoleHelper.Policies.DoctorOrAdmin).Result;
            doctorOrAdminPolicy.Should().NotBeNull();
            doctorOrAdminPolicy.Requirements.Should().HaveCount(1);
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ConfigureUserPolicy_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();
            var policyProvider = _serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Act & Assert
            var userPolicy = policyProvider.GetPolicyAsync("UserPolicy").Result;
            userPolicy.Should().NotBeNull();
            userPolicy.Requirements.Should().HaveCount(1);
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ConfigureDefaultPolicy_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();
            var policyProvider = _serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Act & Assert
            var defaultPolicy = policyProvider.GetDefaultPolicyAsync().Result;
            defaultPolicy.Should().NotBeNull();
            defaultPolicy.Requirements.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void AddRoleAuthorizationPolicies_Should_ConfigureFallbackPolicy_When_Called()
        {
            // Arrange
            _services.AddRoleAuthorizationPolicies();
            _serviceProvider = _services.BuildServiceProvider();
            var policyProvider = _serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

            // Act & Assert
            var fallbackPolicy = policyProvider.GetFallbackPolicyAsync().Result;
            fallbackPolicy.Should().NotBeNull();
            fallbackPolicy.Requirements.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void AddClaimsNormalization_Should_ReturnServiceCollection_When_Called()
        {
            // Act
            var result = _services.AddClaimsNormalization();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(_services);
        }

        [Fact]
        public void AddClaimsNormalization_Should_NotThrow_When_CalledMultipleTimes()
        {
            // Act & Assert
            _services.Invoking(s => s.AddClaimsNormalization())
                .Should().NotThrow();

            _services.Invoking(s => s.AddClaimsNormalization())
                .Should().NotThrow();
        }

        [Fact]
        public void AddUnifiedRoleAuthorization_Should_ReturnServiceCollection_When_Called()
        {
            // Act
            var result = _services.AddUnifiedRoleAuthorization();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(_services);
        }

        [Fact]
        public void AddUnifiedRoleAuthorization_Should_ConfigureCompleteAuthorizationSystem_When_Called()
        {
            // Arrange
            _services.AddUnifiedRoleAuthorization();
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var policyProvider = _serviceProvider.GetService<IAuthorizationPolicyProvider>();

            // Assert
            policyProvider.Should().NotBeNull();
        }

        [Fact]
        public void AddUnifiedRoleAuthorization_Should_NotThrow_When_CalledWithNullServices()
        {
            // Arrange
            IServiceCollection nullServices = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => nullServices.AddUnifiedRoleAuthorization());
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}