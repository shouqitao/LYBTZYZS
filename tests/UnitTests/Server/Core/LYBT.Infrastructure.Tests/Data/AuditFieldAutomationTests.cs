using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LYBT.Infrastructure.Tests.Data
{
    /// <summary>
    /// 审计字段自动化功能测试
    /// </summary>
    public class AuditFieldAutomationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Guid _testUserId = Guid.NewGuid();

        public AuditFieldAutomationTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            SetupMockHttpContext();

            _context = new AppDbContext(options, _mockHttpContextAccessor.Object);
        }

        private void SetupMockHttpContext()
        {
            var claims = new[]
            {
                new Claim("user_id", _testUserId.ToString())
            };

            var mockIdentity = new ClaimsIdentity(claims, "Test");
            var mockPrincipal = new ClaimsPrincipal(mockIdentity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(mockPrincipal);

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        }

        [Fact]
        public async Task Should_Set_CreatedAt_And_CreatedBy_When_Adding_Entity()
        {
            // Arrange
            var beforeTime = DateTime.Now.AddSeconds(-1);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
                // 注意：不设置审计字段，让自动化处理
            };

            // Act
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assert
            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser!.CreatedAt.Should().BeAfter(beforeTime);
            savedUser.CreatedBy.Should().Be(_testUserId);
            savedUser.UpdatedAt.Should().BeNull();
            savedUser.UpdatedBy.Should().BeNull();
        }

        [Fact]
        public async Task Should_Set_UpdatedAt_And_UpdatedBy_When_Updating_Entity()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var beforeUpdateTime = DateTime.Now.AddSeconds(-1);

            // Act - Update entity
            user.RealName = "更新后的用户";
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.UpdatedAt.Should().NotBeNull();
            updatedUser.UpdatedAt.Should().BeAfter(beforeUpdateTime);
            updatedUser.UpdatedBy.Should().Be(_testUserId);
            updatedUser.CreatedBy.Should().Be(_testUserId); // Should remain unchanged
        }

        [Fact]
        public async Task Should_Handle_Multiple_Entities_In_Single_Transaction()
        {
            // Arrange
            var beforeTime = DateTime.Now.AddSeconds(-1);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
            };

            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Male,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
            };

            // Act
            _context.Users.Add(user);
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Assert
            var savedUser = await _context.Users.FindAsync(user.Id);
            var savedPatient = await _context.Patients.FindAsync(patient.Id);

            savedUser!.CreatedAt.Should().BeAfter(beforeTime);
            savedUser.CreatedBy.Should().Be(_testUserId);

            savedPatient!.CreatedAt.Should().BeAfter(beforeTime);
            savedPatient.CreatedBy.Should().Be(_testUserId);
        }

        [Fact]
        public async Task Should_Not_Override_Manual_Audit_Fields_If_Already_Set()
        {
            // Arrange
            var manualCreatedTime = DateTime.Now.AddDays(-1);
            var manualUserId = Guid.NewGuid();
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                CreatedAt = manualCreatedTime,
                CreatedBy = manualUserId
            };

            // Act
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assert
            var savedUser = await _context.Users.FindAsync(user.Id);
            savedUser!.CreatedAt.Should().Be(manualCreatedTime);
            savedUser.CreatedBy.Should().Be(manualUserId);
        }

        [Fact]
        public async Task Should_Work_Without_HttpContext()
        {
            // Arrange - Create context without HttpContextAccessor
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var contextWithoutHttp = new AppDbContext(options);
            
            var beforeTime = DateTime.Now.AddSeconds(-1);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = "hashedpassword",
                RealName = "测试用户",
                Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
            };

            // Act
            contextWithoutHttp.Users.Add(user);
            await contextWithoutHttp.SaveChangesAsync();

            // Assert
            var savedUser = await contextWithoutHttp.Users.FindAsync(user.Id);
            savedUser!.CreatedAt.Should().BeAfter(beforeTime);
            savedUser.CreatedBy.Should().BeNull(); // Should be null when no HttpContext
        }

        [Fact]
        public async Task Should_Set_Audit_Fields_For_All_BaseEntity_Derived_Entities()
        {
            // Arrange
            var beforeTime = DateTime.Now.AddSeconds(-1);
            
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = LYBT.Shared.Models.Enums.Gender.Female,
                Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
            };

            // Act
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Update
            patient.Name = "更新后的患者";
            _context.Patients.Update(patient);
            var beforeUpdateTime = DateTime.Now.AddSeconds(-1);
            await _context.SaveChangesAsync();

            // Assert
            var savedPatient = await _context.Patients.FindAsync(patient.Id);
            savedPatient!.CreatedAt.Should().BeAfter(beforeTime);
            savedPatient.CreatedBy.Should().Be(_testUserId);
            savedPatient.UpdatedAt.Should().NotBeNull();
            savedPatient.UpdatedAt.Should().BeAfter(beforeUpdateTime);
            savedPatient.UpdatedBy.Should().Be(_testUserId);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}