using FluentAssertions;
using LYBT.Entities.Common;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace LYBT.Infrastructure.Tests.Data
{
    /// <summary>
    /// AppDbContext 审计字段测试
    /// </summary>
    public class AppDbContextAuditTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly Mock<ICurrentUserService> _mockUserService;
        private readonly Guid _testUserId = Guid.NewGuid();

        public AppDbContextAuditTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockUserService = new Mock<ICurrentUserService>();
            _mockUserService.Setup(x => x.IsAuthenticated).Returns(true);
            _mockUserService.Setup(x => x.UserId).Returns(_testUserId);
        }

        [Fact]
        public async Task SaveChangesAsync_NewEntity_ShouldSetCreatedFields()
        {
            // Arrange
            using var context = new AppDbContext(_options, _mockUserService.Object);
            var patient = new Patient
            {
                Name = "测试患者",
                Gender = "男",
                Age = 30
            };

            // Act
            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // Assert
            patient.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            patient.CreatedBy.Should().Be(_testUserId);
            patient.UpdatedAt.Should().BeNull();
            patient.UpdatedBy.Should().BeNull();
        }

        [Fact]
        public async Task SaveChangesAsync_UpdatedEntity_ShouldSetUpdatedFields()
        {
            // Arrange
            var patientId = Guid.NewGuid();
            var originalCreatedAt = DateTime.Now.AddDays(-1);
            var originalCreatedBy = Guid.NewGuid();

            // 首先创建一个实体
            using (var context = new AppDbContext(_options))
            {
                var patient = new Patient
                {
                    Id = patientId,
                    Name = "测试患者",
                    Gender = "男",
                    Age = 30,
                    CreatedAt = originalCreatedAt,
                    CreatedBy = originalCreatedBy
                };
                context.Patients.Add(patient);
                await context.SaveChangesAsync();
            }

            // Act - 更新实体
            using (var context = new AppDbContext(_options, _mockUserService.Object))
            {
                var patient = await context.Patients.FindAsync(patientId);
                patient!.Name = "更新后的患者";
                await context.SaveChangesAsync();

                // Assert
                patient.CreatedAt.Should().Be(originalCreatedAt);
                patient.CreatedBy.Should().Be(originalCreatedBy);
                patient.UpdatedAt.Should().NotBeNull();
                patient.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
                patient.UpdatedBy.Should().Be(_testUserId);
            }
        }

        [Fact]
        public async Task SaveChangesAsync_NoAuthentication_ShouldUseSystemUserId()
        {
            // Arrange
            var mockUserServiceNoAuth = new Mock<ICurrentUserService>();
            mockUserServiceNoAuth.Setup(x => x.IsAuthenticated).Returns(false);

            using var context = new AppDbContext(_options, mockUserServiceNoAuth.Object);
            var patient = new Patient
            {
                Name = "测试患者",
                Gender = "女",
                Age = 25
            };

            // Act
            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // Assert
            patient.CreatedBy.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        }

        [Fact]
        public async Task SaveChangesAsync_MultipleEntities_ShouldSetAuditFieldsForAll()
        {
            // Arrange
            using var context = new AppDbContext(_options, _mockUserService.Object);
            var patient1 = new Patient { Name = "患者1", Gender = "男", Age = 30 };
            var patient2 = new Patient { Name = "患者2", Gender = "女", Age = 25 };

            // Act
            context.Patients.AddRange(patient1, patient2);
            await context.SaveChangesAsync();

            // Assert
            patient1.CreatedBy.Should().Be(_testUserId);
            patient2.CreatedBy.Should().Be(_testUserId);
            patient1.CreatedAt.Should().BeCloseTo(patient2.CreatedAt, TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void SaveChanges_Synchronous_ShouldAlsoSetAuditFields()
        {
            // Arrange
            using var context = new AppDbContext(_options, _mockUserService.Object);
            var patient = new Patient
            {
                Name = "同步测试患者",
                Gender = "男",
                Age = 35
            };

            // Act
            context.Patients.Add(patient);
            context.SaveChanges();

            // Assert
            patient.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            patient.CreatedBy.Should().Be(_testUserId);
        }

        public void Dispose()
        {
            // 清理测试数据库
            using var context = new AppDbContext(_options);
            context.Database.EnsureDeleted();
        }
    }
}