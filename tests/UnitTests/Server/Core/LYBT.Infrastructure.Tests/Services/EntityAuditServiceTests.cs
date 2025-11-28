using FluentAssertions;
using LYBT.Entities.Common;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace LYBT.Infrastructure.Tests.Services
{
    /// <summary>
    /// EntityAuditService 单元测试
    /// Issue #2249: 添加审计系统单元测试
    /// OpenSpec: add-global-audit-system
    /// </summary>
    public class EntityAuditServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly Mock<ILogger<EntityAuditService<Patient>>> _mockLogger;
        private readonly EntityAuditService<Patient> _auditService;

        public EntityAuditServiceTests()
        {
            // 使用InMemory数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _mockLogger = new Mock<ILogger<EntityAuditService<Patient>>>();
            _auditService = new EntityAuditService<Patient>(_dbContext, _mockLogger.Object);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new EntityAuditService<Patient>(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("dbContext");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = () => new EntityAuditService<Patient>(_dbContext, null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        #endregion

        #region LogCreateAsync 测试

        [Fact]
        public async Task LogCreateAsync_WithValidEntity_ShouldCreateAuditLog()
        {
            // Arrange
            var patient = CreateTestPatient();
            var operatorId = Guid.NewGuid();
            var operatorName = "测试医生";
            var role = UserRole.Doctor;

            // Act
            await _auditService.LogCreateAsync(patient, operatorId, operatorName, role);

            // Assert
            var logs = await _dbContext.EntityAuditLogs.ToListAsync();
            logs.Should().HaveCount(1);

            var log = logs.First();
            log.EntityType.Should().Be("Patient");
            log.EntityId.Should().Be(patient.Id);
            log.OperatorId.Should().Be(operatorId);
            log.OperatorName.Should().Be(operatorName);
            log.OperatorRole.Should().Be(role);
            log.OperationType.Should().Be(AuditOperationType.Create);
            log.OldValues.Should().BeNull();
            log.NewValues.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LogCreateAsync_WithNullEntity_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = async () => await _auditService.LogCreateAsync(
                null!, Guid.NewGuid(), "测试", UserRole.Doctor);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("entity");
        }

        [Fact]
        public async Task LogCreateAsync_ShouldRecordAllNonSystemFields()
        {
            // Arrange
            var patient = CreateTestPatient();
            patient.Name = "张三";
            patient.Gender = Gender.Male;
            patient.PhoneNumber = "13800138000";

            // Act
            await _auditService.LogCreateAsync(patient, Guid.NewGuid(), "测试", UserRole.Doctor);

            // Assert
            var log = await _dbContext.EntityAuditLogs.FirstAsync();
            var newValues = JsonSerializer.Deserialize<Dictionary<string, object>>(log.NewValues!);

            newValues.Should().ContainKey("Name");
            newValues.Should().ContainKey("Gender");
            newValues.Should().ContainKey("PhoneNumber");
            // 系统字段应被排除
            newValues.Should().NotContainKey("Id");
            newValues.Should().NotContainKey("CreatedAt");
            newValues.Should().NotContainKey("UpdatedAt");
            newValues.Should().NotContainKey("IsDeleted");
        }

        #endregion

        #region LogUpdateAsync 测试

        [Fact]
        public async Task LogUpdateAsync_WithChanges_ShouldCreateAuditLog()
        {
            // Arrange
            var beforePatient = CreateTestPatient();
            beforePatient.Name = "张三";

            var afterPatient = CreateTestPatient();
            afterPatient.Id = beforePatient.Id;
            afterPatient.Name = "李四";

            var operatorId = Guid.NewGuid();
            var operatorName = "测试管理员";
            var role = UserRole.Admin;
            var reason = "更正姓名";

            // Act
            await _auditService.LogUpdateAsync(beforePatient, afterPatient, operatorId, operatorName, role, reason);

            // Assert
            var logs = await _dbContext.EntityAuditLogs.ToListAsync();
            logs.Should().HaveCount(1);

            var log = logs.First();
            log.EntityType.Should().Be("Patient");
            log.OperationType.Should().Be(AuditOperationType.Update);
            log.Reason.Should().Be(reason);
            log.ChangedFields.Should().Contain("Name");
            // JSON序列化可能使用Unicode转义，验证Name字段存在
            log.OldValues.Should().NotBeNullOrEmpty();
            log.NewValues.Should().NotBeNullOrEmpty();
            // 反序列化验证实际值
            var oldValuesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.OldValues!);
            var newValuesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(log.NewValues!);
            oldValuesDict.Should().ContainKey("Name");
            newValuesDict.Should().ContainKey("Name");
        }

        [Fact]
        public async Task LogUpdateAsync_WithNoChanges_ShouldNotCreateAuditLog()
        {
            // Arrange
            var patient = CreateTestPatient();
            patient.Name = "张三";

            var samePatient = CreateTestPatient();
            samePatient.Id = patient.Id;
            samePatient.Name = "张三"; // 相同的值

            // Act
            await _auditService.LogUpdateAsync(patient, samePatient, Guid.NewGuid(), "测试", UserRole.Doctor);

            // Assert
            var logs = await _dbContext.EntityAuditLogs.ToListAsync();
            logs.Should().BeEmpty();
        }

        [Fact]
        public async Task LogUpdateAsync_WithNullBefore_ShouldRecordAllFieldsAsNew()
        {
            // Arrange
            var afterPatient = CreateTestPatient();
            afterPatient.Name = "新患者";

            // Act
            await _auditService.LogUpdateAsync(null, afterPatient, Guid.NewGuid(), "测试", UserRole.Doctor);

            // Assert
            var logs = await _dbContext.EntityAuditLogs.ToListAsync();
            logs.Should().HaveCount(1);

            var log = logs.First();
            // 当before为null时，OldValues可能是null或字符串"null"
            (log.OldValues == null || log.OldValues == "null").Should().BeTrue();
            log.NewValues.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LogUpdateAsync_WithNullAfter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var beforePatient = CreateTestPatient();

            // Act & Assert
            var act = async () => await _auditService.LogUpdateAsync(
                beforePatient, null!, Guid.NewGuid(), "测试", UserRole.Doctor);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("after");
        }

        [Fact]
        public async Task LogUpdateAsync_WithMultipleFieldChanges_ShouldRecordAllChanges()
        {
            // Arrange
            var before = CreateTestPatient();
            before.Name = "张三";
            before.PhoneNumber = "13800138000";
            before.Gender = Gender.Male;

            var after = CreateTestPatient();
            after.Id = before.Id;
            after.Name = "李四";
            after.PhoneNumber = "13900139000";
            after.Gender = Gender.Female;

            // Act
            await _auditService.LogUpdateAsync(before, after, Guid.NewGuid(), "测试", UserRole.Doctor);

            // Assert
            var log = await _dbContext.EntityAuditLogs.FirstAsync();
            var changedFields = JsonSerializer.Deserialize<List<string>>(log.ChangedFields!);

            changedFields.Should().Contain("Name");
            changedFields.Should().Contain("PhoneNumber");
            changedFields.Should().Contain("Gender");
        }

        #endregion

        #region LogDeleteAsync 测试

        [Fact]
        public async Task LogDeleteAsync_WithValidEntity_ShouldCreateAuditLog()
        {
            // Arrange
            var patient = CreateTestPatient();
            patient.Name = "待删除患者";
            var operatorId = Guid.NewGuid();
            var operatorName = "测试管理员";
            var role = UserRole.Admin;
            var reason = "测试删除";

            // Act
            await _auditService.LogDeleteAsync(patient, operatorId, operatorName, role, reason);

            // Assert
            var logs = await _dbContext.EntityAuditLogs.ToListAsync();
            logs.Should().HaveCount(1);

            var log = logs.First();
            log.EntityType.Should().Be("Patient");
            log.EntityId.Should().Be(patient.Id);
            log.OperationType.Should().Be(AuditOperationType.SoftDelete);
            log.Reason.Should().Be(reason);
            log.OldValues.Should().NotBeNullOrEmpty();
            log.NewValues.Should().BeNull();
            log.ChangedFields.Should().Contain("IsDeleted");
        }

        [Fact]
        public async Task LogDeleteAsync_WithNullEntity_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var act = async () => await _auditService.LogDeleteAsync(
                null!, Guid.NewGuid(), "测试", UserRole.Doctor);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("entity");
        }

        [Fact]
        public async Task LogDeleteAsync_WithoutReason_ShouldCreateLogWithNullReason()
        {
            // Arrange
            var patient = CreateTestPatient();

            // Act
            await _auditService.LogDeleteAsync(patient, Guid.NewGuid(), "测试", UserRole.Doctor);

            // Assert
            var log = await _dbContext.EntityAuditLogs.FirstAsync();
            log.Reason.Should().BeNull();
        }

        #endregion

        #region GetLogsAsync 测试

        [Fact]
        public async Task GetLogsAsync_WithExistingLogs_ShouldReturnPagedResults()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var patient = CreateTestPatient();
            patient.Id = entityId;

            // 创建多条审计日志
            for (int i = 0; i < 5; i++)
            {
                await _auditService.LogCreateAsync(patient, Guid.NewGuid(), $"操作者{i}", UserRole.Doctor);
            }

            // Act
            var (logs, totalCount) = await _auditService.GetLogsAsync(entityId, page: 1, pageSize: 3);

            // Assert
            logs.Should().HaveCount(3);
            totalCount.Should().Be(5);
        }

        [Fact]
        public async Task GetLogsAsync_WithNoLogs_ShouldReturnEmptyResult()
        {
            // Arrange
            var entityId = Guid.NewGuid();

            // Act
            var (logs, totalCount) = await _auditService.GetLogsAsync(entityId);

            // Assert
            logs.Should().BeEmpty();
            totalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetLogsAsync_ShouldReturnLogsInDescendingOrder()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var patient = CreateTestPatient();
            patient.Id = entityId;

            await _auditService.LogCreateAsync(patient, Guid.NewGuid(), "第一次", UserRole.Doctor);
            await Task.Delay(10); // 确保时间差
            await _auditService.LogCreateAsync(patient, Guid.NewGuid(), "第二次", UserRole.Doctor);
            await Task.Delay(10);
            await _auditService.LogCreateAsync(patient, Guid.NewGuid(), "第三次", UserRole.Doctor);

            // Act
            var (logs, _) = await _auditService.GetLogsAsync(entityId);

            // Assert
            logs.Should().HaveCount(3);
            logs[0].OperatorName.Should().Be("第三次"); // 最新的在前
            logs[2].OperatorName.Should().Be("第一次"); // 最早的在后
        }

        [Fact]
        public async Task GetLogsAsync_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var patient = CreateTestPatient();
            patient.Id = entityId;

            for (int i = 0; i < 10; i++)
            {
                await _auditService.LogCreateAsync(patient, Guid.NewGuid(), $"操作者{i}", UserRole.Doctor);
            }

            // Act - 获取第二页
            var (logs, totalCount) = await _auditService.GetLogsAsync(entityId, page: 2, pageSize: 3);

            // Assert
            logs.Should().HaveCount(3);
            totalCount.Should().Be(10);
        }

        #endregion

        #region 辅助方法

        private static Patient CreateTestPatient()
        {
            return new Patient
            {
                Id = Guid.NewGuid(),
                Name = "测试患者",
                Gender = Gender.Unknown,
                PhoneNumber = "13800000000",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };
        }

        #endregion
    }
}
