using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Users.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using AutoMapper;

namespace LYBT.Module.Users.Tests.Services
{
    public class UserQueryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UserQueryService _service;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserQueryService>> _mockLogger;

        public UserQueryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserQueryService>>();

            _service = new UserQueryService(
                _context,
                _mockMapper.Object,
                _mockLogger.Object);

            SetupMockMapper();
        }

        private void SetupMockMapper()
        {
            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns((User user) => new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreateTime = user.CreatedTime
                });

            _mockMapper.Setup(x => x.Map<List<UserDto>>(It.IsAny<List<User>>()))
                .Returns((List<User> users) => users.Select(user => new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    CreateTime = user.CreatedTime
                }).ToList());

            _mockMapper.Setup(x => x.Map<PagedResult<UserDto>>(It.IsAny<PagedResult<User>>()))
                .Returns((PagedResult<User> pagedUsers) => new PagedResult<UserDto>
                {
                    Items = pagedUsers.Items.Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        RealName = u.RealName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Role = u.Role,
                        CreateTime = u.CreatedTime
                    }).ToList(),
                    TotalCount = pagedUsers.TotalCount,
                    CurrentPage = pagedUsers.CurrentPage,
                    PageSize = pagedUsers.PageSize
                });
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_Should_Return_User_When_Found()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "13800138000",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(user.Id);
            result.Data.Username.Should().Be(user.Username);
            result.Data.RealName.Should().Be(user.RealName);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Return_Failure_When_Not_Found()
        {
            // Act
            var result = await _service.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        #endregion

        #region GetByUsernameAsync Tests

        [Fact]
        public async Task GetByUsernameAsync_Should_Return_User_When_Found()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "13800138000"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByUsernameAsync("testuser");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetByUsernameAsync_Should_Return_Failure_When_Not_Found()
        {
            // Act
            var result = await _service.GetByUsernameAsync("nonexistent");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户不存在");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task GetByUsernameAsync_Should_Return_Failure_When_Username_Invalid(string username)
        {
            // Act
            var result = await _service.GetByUsernameAsync(username);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("用户名");
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_Should_Return_All_Users_When_No_Criteria()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "user1", RealName = "User One", Status = CommonStatus.Enabled },
                new User { Username = "user2", RealName = "User Two", Status = CommonStatus.Enabled },
                new User { Username = "user3", RealName = "User Three", Status = CommonStatus.Enabled }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var criteria = new UserSearchDto { PageIndex = 1, PageSize = 10 };

            // Act
            var result = await _service.GetPagedAsync(criteria);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.TotalCount.Should().Be(3);
            result.Data.Items.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Username()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "admin", RealName = "Administrator" },
                new User { Username = "doctor1", RealName = "Doctor One" },
                new User { Username = "doctor2", RealName = "Doctor Two" }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var criteria = new UserSearchDto
            {
                Username = "doctor",
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(criteria);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().AllSatisfy(u => u.Username.Should().Contain("doctor"));
        }

        [Fact]
        public async Task GetPagedAsync_Should_Filter_By_Role()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "admin", Role = UserRole.Admin },
                new User { Username = "doctor1", Role = UserRole.Doctor },
                new User { Username = "doctor2", Role = UserRole.Doctor }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            var criteria = new UserSearchDto
            {
                Role = UserRole.Doctor,
                PageIndex = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetPagedAsync(criteria);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.Items.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Doctor));
        }

        #endregion

        #region GetActiveUsersAsync Tests

        [Fact]
        public async Task GetActiveUsersAsync_Should_Return_Only_Active_Users()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "active1", Status = CommonStatus.Enabled },
                new User { Username = "inactive", Status = CommonStatus.Disabled },
                new User { Username = "active2", Status = CommonStatus.Enabled }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(u => u.Username.Should().Match(name => name == "active1" || name == "active2"));
        }

        #endregion

        #region SearchAsync Tests

        [Fact]
        public async Task SearchAsync_Should_Find_Users_By_Username()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "john_doe", RealName = "John Doe" },
                new User { Username = "jane_smith", RealName = "Jane Smith" },
                new User { Username = "admin", RealName = "Administrator" }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.SearchAsync("john");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(1);
            result.Data.First().Username.Should().Be("john_doe");
        }

        [Fact]
        public async Task SearchAsync_Should_Find_Users_By_RealName()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "user1", RealName = "张三" },
                new User { Username = "user2", RealName = "李四" },
                new User { Username = "user3", RealName = "张五" }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.SearchAsync("张");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
        }

        #endregion

        #region GetDoctorsAsync Tests

        [Fact]
        public async Task GetDoctorsAsync_Should_Return_Only_Doctors()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Username = "admin", Role = UserRole.Admin, Status = CommonStatus.Enabled },
                new User { Username = "doctor1", Role = UserRole.Doctor, Status = CommonStatus.Enabled },
                new User { Username = "doctor2", Role = UserRole.Doctor, Status = CommonStatus.Enabled }
            };
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetDoctorsAsync();

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Should().HaveCount(2);
            result.Data.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Doctor));
        }

        #endregion

        #region ValidateUsernameAsync Tests

        [Fact]
        public async Task ValidateUsernameAsync_Should_Return_False_When_Username_Exists()
        {
            // Arrange
            var user = new User { Username = "existinguser" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ValidateUsernameAsync("existinguser");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateUsernameAsync_Should_Return_True_When_Username_Available()
        {
            // Act
            var result = await _service.ValidateUsernameAsync("newuser");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region IsDoctorAvailableAsync Tests

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_True_For_Available_Doctor()
        {
            // Arrange
            var doctor = new User
            {
                Id = Guid.NewGuid(),
                Username = "doctor",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            };
            await _context.Users.AddAsync(doctor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.IsDoctorAvailableAsync(doctor.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task IsDoctorAvailableAsync_Should_Return_False_For_Disabled_Doctor()
        {
            // Arrange
            var doctor = new User
            {
                Id = Guid.NewGuid(),
                Username = "doctor",
                Role = UserRole.Doctor,
                Status = CommonStatus.Disabled
            };
            await _context.Users.AddAsync(doctor);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.IsDoctorAvailableAsync(doctor.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        #endregion
    }
}