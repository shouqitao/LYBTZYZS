using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Infrastructure.Logging;
using LYBT.Models.Users;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Backend.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LYBT.Tests.Backend.Examples
{
    /// <summary>
    /// 统一测试架构示例 - UserService测试
    /// 展示如何使用新的测试基础设施进行全面测试
    /// </summary>
    [TestCategory(TestCategories.Service)]
    public class UnifiedUserServiceTests : BaseTestFixture
    {
        private readonly IUserService _service;
        private readonly Mock<IUserRepository> _mockRepository;
        private readonly IMapper _mapper;

        public UnifiedUserServiceTests()
        {
            // 设置AutoMapper配置
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserCreateDto, UserModel>();
                cfg.CreateMap<UserUpdateDto, UserModel>();
                cfg.CreateMap<UserModel, UserDto>();
            }, NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            // 创建Mock Repository
            _mockRepository = CreateMockRepository<IUserRepository, UserModel>();

            // 创建Service实例
            _service = new UserService(_mockRepository.Object, _mapper, MockLogService.Object);
        }

        #region 基础CRUD测试

        [Fact]
        [TestCategory(TestCategories.Unit)]
        public async Task GetByIdAsync_WithValidId_ShouldReturnUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userModel = DataFactory.UserModelFaker.Generate() with { Id = userId };
            
            _mockRepository.Setup(x => x.GetByIdAsync(userId))
                          .ReturnsAsync(userModel);

            // Act
            var result = await _service.GetByIdAsync(userId);

            // Assert
            result.ShouldNotBeNull();
            result.Id.Should().Be(userId);
            result.Username.Should().Be(userModel.Username);
            result.RealName.Should().Be(userModel.RealName);
            
            // 验证Mock调用
            _mockRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            VerifyNoErrorLogs();
        }

        [Theory]
        [GuidTest(validOnly: false)]
        [TestCategory(TestCategories.BoundaryValue)]
        public async Task GetByIdAsync_WithInvalidGuid_ShouldReturnNull(Guid invalidId)
        {
            // Arrange
            _mockRepository.Setup(x => x.GetByIdAsync(invalidId))
                          .ReturnsAsync((UserModel?)null);

            // Act
            var result = await _service.GetByIdAsync(invalidId);

            // Assert
            result.Should().BeNull();
            _mockRepository.Verify(x => x.GetByIdAsync(invalidId), Times.Once);
        }

        [Fact]
        [TestCategory(TestCategories.Unit)]
        public async Task CreateAsync_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var createDto = DataFactory.UserCreateDtoFaker.Generate();
            var createdUser = DataFactory.UserModelFaker.Generate() with 
            { 
                Username = createDto.Username,
                RealName = createDto.RealName 
            };

            _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserModel>()))
                          .ReturnsAsync(createdUser);
            _mockRepository.Setup(x => x.ExistsByUsernameAsync(createDto.Username, null))
                          .ReturnsAsync(false);

            // Act
            var result = await _service.AddAsync(createDto, Guid.NewGuid(), "TestUser");

            // Assert
            result.ShouldNotBeNull();
            result.Username.Should().Be(createDto.Username);
            result.RealName.Should().Be(createDto.RealName);
            
            _mockRepository.Verify(x => x.ExistsByUsernameAsync(createDto.Username, null), Times.Once);
            _mockRepository.Verify(x => x.AddAsync(It.IsAny<UserModel>()), Times.Once);
            VerifyNoErrorLogs();
        }

        #endregion

        #region 数据驱动测试

        [Theory]
        [TestDataSource(nameof(GetInvalidUserCreateData))]
        [TestCategory(TestCategories.DataDriven)]
        public async Task CreateAsync_WithInvalidData_ShouldThrowException(
            UserCreateDto invalidDto, string expectedErrorType)
        {
            // Act & Assert
            var exception = await _service.AddAsync(invalidDto, Guid.NewGuid(), "TestUser")
                                         .ShouldThrowAsync<ArgumentException>();
            
            exception.Message.ShouldContain(expectedErrorType);
        }

        [Theory]
        [PasswordComplexityTest]
        [TestCategory(TestCategories.DataDriven)]
        public void ValidatePasswordComplexity_WithVariousPasswords_ShouldValidateCorrectly(
            string password, bool expectedValid, string reason)
        {
            // Act
            var isValid = IsPasswordComplexityValid(password);

            // Assert
            isValid.Should().Be(expectedValid, reason);
        }

        [Theory]
        [PaginationTest]
        [TestCategory(TestCategories.BoundaryValue)]
        public async Task GetPagedAsync_WithVariousPaginationParams_ShouldHandleCorrectly(
            int pageNumber, int pageSize, bool expectedValid)
        {
            // Arrange
            var query = new UserPagedQueryDto 
            { 
                PageNumber = pageNumber, 
                PageSize = pageSize 
            };

            if (expectedValid)
            {
                var mockResult = new PaginatedResult<UserModel>
                {
                    Items = DataFactory.UserModelFaker.Generate(Math.Min(pageSize, 10)),
                    TotalCount = 50,
                    CurrentPage = pageNumber,
                    PageSize = pageSize
                };

                _mockRepository.Setup(x => x.GetPagedAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserModel, bool>>>(),
                    pageNumber, pageSize))
                              .ReturnsAsync(mockResult);

                // Act
                var result = await _service.GetPagedAsync(query);

                // Assert
                result.ShouldNotBeNull();
                result.CurrentPage.Should().Be(pageNumber);
                result.PageSize.Should().Be(pageSize);
            }
            else
            {
                // Act & Assert
                await _service.GetPagedAsync(query)
                             .ShouldThrowAsync<ArgumentException>();
            }
        }

        #endregion

        #region 边界值和异常处理测试

        [Theory]
        [BoundaryTest(typeof(int))]
        [TestCategory(TestCategories.BoundaryValue)]
        public async Task BatchEnableAsync_WithBoundaryCount_ShouldHandleCorrectly(int count)
        {
            // Arrange
            var ids = new List<Guid>();
            for (int i = 0; i < Math.Abs(count); i++)
            {
                ids.Add(Guid.NewGuid());
            }

            if (count > 0 && count <= 100)
            {
                _mockRepository.Setup(x => x.BatchEnableAsync(ids, It.IsAny<Guid>(), It.IsAny<string>()))
                              .ReturnsAsync(ids.Count);

                // Act
                var result = await _service.BatchEnableAsync(ids, Guid.NewGuid(), "TestUser");

                // Assert
                result.Should().Be(ids.Count);
            }
            else
            {
                // Act & Assert
                await _service.BatchEnableAsync(ids, Guid.NewGuid(), "TestUser")
                             .ShouldThrowAsync<ArgumentException>();
            }
        }

        [Fact]
        [TestCategory(TestCategories.ExceptionHandling)]
        public async Task CreateAsync_WithDuplicateUsername_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = DataFactory.UserCreateDtoFaker.Generate();
            
            _mockRepository.Setup(x => x.ExistsByUsernameAsync(createDto.Username, null))
                          .ReturnsAsync(true);

            // Act & Assert
            var exception = await _service.AddAsync(createDto, Guid.NewGuid(), "TestUser")
                                         .ShouldThrowAsync<InvalidOperationException>("用户名已存在");
            
            _mockRepository.Verify(x => x.ExistsByUsernameAsync(createDto.Username, null), Times.Once);
            _mockRepository.Verify(x => x.AddAsync(It.IsAny<UserModel>()), Times.Never);
        }

        [Fact]
        [TestCategory(TestCategories.ExceptionHandling)]
        public async Task UpdateAsync_WithRepositoryException_ShouldLogErrorAndThrow()
        {
            // Arrange
            var updateDto = DataFactory.UserUpdateDtoFaker.Generate();
            var existingUser = DataFactory.UserModelFaker.Generate() with { Id = updateDto.Id };
            var repositoryException = new InvalidOperationException("数据库连接失败");

            _mockRepository.Setup(x => x.GetByIdAsync(updateDto.Id))
                          .ReturnsAsync(existingUser);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<UserModel>()))
                          .ThrowsAsync(repositoryException);

            // Act & Assert
            var exception = await _service.UpdateAsync(updateDto, Guid.NewGuid(), "TestUser")
                                         .ShouldThrowAsync<InvalidOperationException>();
            
            exception.Should().Be(repositoryException);
        }

        #endregion

        #region 性能测试

        [Fact]
        [TestCategory(TestCategories.Performance)]
        public async Task CreateMultipleUsers_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            const int userCount = 100;
            var createDtos = DataFactory.UserCreateDtoFaker.Generate(userCount);
            var startTime = DateTime.UtcNow;

            foreach (var dto in createDtos)
            {
                var createdUser = DataFactory.UserModelFaker.Generate() with 
                { 
                    Username = dto.Username,
                    RealName = dto.RealName 
                };

                _mockRepository.Setup(x => x.ExistsByUsernameAsync(dto.Username, null))
                              .ReturnsAsync(false);
                _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserModel>()))
                              .ReturnsAsync(createdUser);
            }

            // Act
            var tasks = createDtos.Select(dto => 
                _service.AddAsync(dto, Guid.NewGuid(), "TestUser")).ToArray();
            
            await Task.WhenAll(tasks);

            // Assert
            var duration = DateTime.UtcNow - startTime;
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), 
                $"创建{userCount}个用户应在5秒内完成");
            
            tasks.Should().AllSatisfy(task => task.Result.ShouldNotBeNull());
        }

        #endregion

        #region 集成测试场景

        [Fact]
        [TestCategory(TestCategories.Integration)]
        public async Task UserLifecycle_CreateUpdateEnableDisableDelete_ShouldWorkEndToEnd()
        {
            // Arrange
            var operatorId = Guid.NewGuid();
            const string operatorName = "TestOperator";

            var createDto = DataFactory.UserCreateDtoFaker.Generate();
            var createdUser = DataFactory.UserModelFaker.Generate() with 
            { 
                Username = createDto.Username,
                Status = CommonStatus.Enabled 
            };

            // Setup mocks for complete lifecycle
            _mockRepository.Setup(x => x.ExistsByUsernameAsync(createDto.Username, null))
                          .ReturnsAsync(false);
            _mockRepository.Setup(x => x.AddAsync(It.IsAny<UserModel>()))
                          .ReturnsAsync(createdUser);
            _mockRepository.Setup(x => x.GetByIdAsync(createdUser.Id))
                          .ReturnsAsync(createdUser);
            _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<UserModel>()))
                          .ReturnsAsync(createdUser);
            _mockRepository.Setup(x => x.DisableAsync(createdUser.Id, operatorId, operatorName))
                          .ReturnsAsync(true);
            _mockRepository.Setup(x => x.EnableAsync(createdUser.Id, operatorId, operatorName))
                          .ReturnsAsync(true);
            _mockRepository.Setup(x => x.DeleteAsync(createdUser.Id))
                          .ReturnsAsync(true);

            // Act & Assert - Create
            var created = await _service.AddAsync(createDto, operatorId, operatorName);
            created.ShouldNotBeNull();
            created.Username.Should().Be(createDto.Username);

            // Act & Assert - Update
            var updateDto = DataFactory.UserUpdateDtoFaker.Generate() with { Id = created.Id };
            var updateResult = await _service.UpdateAsync(updateDto, operatorId, operatorName);
            updateResult.Should().BeTrue();

            // Act & Assert - Disable
            var disableResult = await _service.DisableAsync(created.Id, operatorId, operatorName);
            disableResult.Should().BeTrue();

            // Act & Assert - Enable
            var enableResult = await _service.EnableAsync(created.Id, operatorId, operatorName);
            enableResult.Should().BeTrue();

            // Act & Assert - Delete
            var deleteResult = await _service.DeleteAsync(created.Id);
            deleteResult.Should().BeTrue();

            // Verify all operations were logged
            var actionLogs = GetCapturedLogs<UserActionLogDto>();
            actionLogs.Should().HaveCountGreaterOrEqualTo(5); // Create, Update, Disable, Enable, Delete
        }

        #endregion

        #region 测试数据生成方法

        /// <summary>
        /// 生成无效的用户创建数据
        /// </summary>
        public static IEnumerable<object[]> GetInvalidUserCreateData()
        {
            var factory = new TestDataFactory();

            yield return new object[] 
            { 
                factory.UserCreateDtoFaker.Generate() with { Username = null! }, 
                "用户名" 
            };
            yield return new object[] 
            { 
                factory.UserCreateDtoFaker.Generate() with { Username = "" }, 
                "用户名" 
            };
            yield return new object[] 
            { 
                factory.UserCreateDtoFaker.Generate() with { RealName = null! }, 
                "真实姓名" 
            };
            yield return new object[] 
            { 
                factory.UserCreateDtoFaker.Generate() with { RealName = "" }, 
                "真实姓名" 
            };
            yield return new object[] 
            { 
                factory.UserCreateDtoFaker.Generate() with { Password = "123" }, 
                "密码复杂度" 
            };
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 验证密码复杂度（示例实现）
        /// </summary>
        private static bool IsPasswordComplexityValid(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        #endregion
    }
}