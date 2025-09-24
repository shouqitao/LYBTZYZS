using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Infrastructure.Tests.Repositories
{
    /// <summary>
    /// ReadOnlyRepository基类测试
    /// 验证缓存策略、软删除过滤、缓存穿透防护等核心功能
    /// </summary>
    public class ReadOnlyRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<TestReadOnlyRepository>> _loggerMock;
        private readonly TestReadOnlyRepository _repository;

        public ReadOnlyRepositoryTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 设置AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<User, TestUserDto>();
            });
            _mapper = mapperConfig.CreateMapper();

            // 设置内存缓存
            _cache = new MemoryCache(new MemoryCacheOptions());

            // 设置Logger Mock
            _loggerMock = new Mock<ILogger<TestReadOnlyRepository>>();

            // 创建测试仓储
            _repository = new TestReadOnlyRepository(_context, _mapper, _loggerMock.Object, _cache);
        }

        #region 缓存测试

        [Fact]
        public async Task GetByIdAsync_Should_Cache_Result()
        {
            // Arrange
            var user = new User 
            { 
                Id = Guid.NewGuid(), 
                UserName = "testuser",
                Email = "test@example.com",
                IsDeleted = false 
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act - 第一次查询
            var result1 = await _repository.GetByIdAsync(user.Id);

            // Act - 第二次查询（应该从缓存获取）
            var result2 = await _repository.GetByIdAsync(user.Id);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!.Id.Should().Be(result2!.Id);

            // 验证日志 - 第二次应该有缓存命中日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("缓存命中")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task GetByIdAsync_Should_Cache_Null_Result_To_Prevent_Cache_Penetration()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act - 第一次查询不存在的ID
            var result1 = await _repository.GetByIdAsync(nonExistentId);

            // Act - 第二次查询同一个不存在的ID
            var result2 = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            result1.Should().BeNull();
            result2.Should().BeNull();

            // 验证缓存穿透防护日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("空值标记")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region 分页查询测试

        [Fact]
        public async Task GetPagedAsync_Should_Return_Correct_Page()
        {
            // Arrange
            var users = new List<User>();
            for (int i = 1; i <= 10; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now.AddDays(-i)
                });
            }
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(null, 1, 5);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(10);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(5);
            result.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_Should_Cache_Result()
        {
            // Arrange
            var users = new List<User>();
            for (int i = 1; i <= 5; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    IsDeleted = false
                });
            }
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act - 第一次查询
            var result1 = await _repository.GetPagedAsync(null, 1, 5);

            // Act - 第二次查询相同参数
            var result2 = await _repository.GetPagedAsync(null, 1, 5);

            // Assert
            result1.TotalCount.Should().Be(result2.TotalCount);
            
            // 验证缓存命中日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("缓存命中") && v.ToString()!.Contains("分页查询")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region 软删除过滤测试

        [Fact]
        public async Task GetAllAsync_Should_Exclude_Soft_Deleted_Records()
        {
            // Arrange
            var activeUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "active",
                Email = "active@example.com",
                IsDeleted = false
            };
            var deletedUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = "deleted",
                Email = "deleted@example.com",
                IsDeleted = true
            };
            _context.Users.AddRange(activeUser, deletedUser);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().UserName.Should().Be("active");
        }

        #endregion

        #region 空集合缓存测试

        [Fact]
        public async Task GetAllAsync_Should_Cache_Empty_Result()
        {
            // Arrange - 不添加任何数据

            // Act - 第一次查询
            var result1 = await _repository.GetAllAsync();

            // Act - 第二次查询
            var result2 = await _repository.GetAllAsync();

            // Assert
            result1.Should().BeEmpty();
            result2.Should().BeEmpty();

            // 验证空集合缓存日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("空集合")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region 存在性检查测试

        [Fact]
        public async Task ExistsAsync_Should_Return_True_For_Existing_Entity()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                Email = "test@example.com",
                IsDeleted = false
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _repository.ExistsAsync(user.Id);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_Should_Return_False_For_NonExisting_Entity()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var exists = await _repository.ExistsAsync(nonExistentId);

            // Assert
            exists.Should().BeFalse();
        }

        #endregion

        #region 计数测试

        [Fact]
        public async Task CountAsync_Should_Return_Correct_Count()
        {
            // Arrange
            var users = new List<User>();
            for (int i = 1; i <= 3; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"user{i}",
                    Email = $"user{i}@example.com",
                    IsDeleted = false
                });
            }
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.CountAsync();

            // Assert
            count.Should().Be(3);
        }

        [Fact]
        public async Task CountAsync_With_Predicate_Should_Return_Filtered_Count()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), UserName = "admin1", Email = "admin1@example.com", IsDeleted = false },
                new User { Id = Guid.NewGuid(), UserName = "admin2", Email = "admin2@example.com", IsDeleted = false },
                new User { Id = Guid.NewGuid(), UserName = "user1", Email = "user1@example.com", IsDeleted = false }
            };
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var count = await _repository.CountAsync(u => u.UserName.StartsWith("admin"));

            // Assert
            count.Should().Be(2);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _cache?.Dispose();
        }
    }

    /// <summary>
    /// 测试用的ReadOnlyRepository实现
    /// </summary>
    internal class TestReadOnlyRepository : ReadOnlyRepository<User>
    {
        public TestReadOnlyRepository(
            AppDbContext context, 
            IMapper mapper,
            ILogger<TestReadOnlyRepository> logger,
            IMemoryCache cache) 
            : base(context, mapper, logger, cache)
        {
        }

        protected override IQueryable<User> ApplyGlobalFilters(IQueryable<User> query)
        {
            // 应用软删除过滤
            return query.Where(u => !u.IsDeleted);
        }
    }

    /// <summary>
    /// 测试用DTO
    /// </summary>
    internal class TestUserDto
    {
        public Guid Id { get; set; }
        public string Username = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}