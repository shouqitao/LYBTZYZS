using FluentAssertions;
using LYBT.Module.Users.Repositories;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Module.Users.Tests.Repositories;

/// <summary>
/// UserRepository 单元测试
/// Issue #866 - Phase 2.2: Users 模块测试
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=:memory:")
            .Options;

        _context = new AppDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _sut = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    #region GetByIdAsync 测试

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Email = "test@example.com",
            PasswordHash = "hash123",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync 测试

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), UserName = "user1", RealName = "用户1", PasswordHash = "hash1", Role = UserRole.Doctor, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), UserName = "user2", RealName = "用户2", PasswordHash = "hash2", Role = UserRole.Admin, CreatedAt = DateTime.UtcNow }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.UserName == "user1");
        result.Should().Contain(u => u.UserName == "user2");
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        // 数据库为空

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPagedAsync 测试

    [Fact]
    public async Task GetPagedAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"user{i}",
                RealName = $"用户{i}",
                Email = $"user{i}@example.com",
                PasswordHash = $"hash{i}",
                Role = UserRole.Doctor,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            };
            await _context.Users.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        // Act
        var page1 = await _sut.GetPagedAsync(1, 10);
        var page2 = await _sut.GetPagedAsync(2, 10);

        // Assert
        page1.Items.Should().HaveCount(10);
        page1.TotalCount.Should().Be(15);
        page2.Items.Should().HaveCount(5);
        page2.TotalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetPagedAsync_WithPredicate_ReturnsFilteredResults()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), UserName = "doctor1", RealName = "医生1", PasswordHash = "hash1", Role = UserRole.Doctor, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), UserName = "doctor2", RealName = "医生2", PasswordHash = "hash2", Role = UserRole.Doctor, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), UserName = "admin1", RealName = "管理员1", PasswordHash = "hash3", Role = UserRole.Admin, CreatedAt = DateTime.UtcNow }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetPagedAsync(u => u.Role == UserRole.Doctor, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(u => u.Role.Should().Be(UserRole.Doctor));
    }

    #endregion

    #region GetByUsernameAsync 测试

    [Fact]
    public async Task GetByUsernameAsync_WithExactUsername_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testdoctor",
            RealName = "测试医生",
            Email = "doctor@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByUsernameAsync("testdoctor");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("testdoctor");
        result.Email.Should().Be("doctor@example.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithEmail_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            RealName = "测试用户",
            Email = "test@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByUsernameAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistentUsername_ReturnsNull()
    {
        // Arrange
        // 数据库为空

        // Act
        var result = await _sut.GetByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_ExcludesDeletedUsers()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleted",
            RealName = "已删除用户",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByUsernameAsync("deleted");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEmailAsync 测试

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "emailtest",
            RealName = "邮箱测试",
            Email = "test@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.UserName.Should().Be("emailtest");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Arrange
        // 数据库为空

        // Act
        var result = await _sut.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ExcludesDeletedUsers()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleted",
            RealName = "已删除用户",
            Email = "deleted@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEmailAsync("deleted@example.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsUsernameExistsAsync 测试

    [Fact]
    public async Task IsUsernameExistsAsync_WithExistingUsername_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "existinguser",
            RealName = "已存在用户",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsUsernameExistsAsync("existinguser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUsernameExistsAsync_WithNonExistentUsername_ReturnsFalse()
    {
        // Arrange
        // 数据库为空

        // Act
        var result = await _sut.IsUsernameExistsAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUsernameExistsAsync_ExcludesDeletedUsers()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleted",
            RealName = "已删除用户",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsUsernameExistsAsync("deleted");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsEmailExistsAsync 测试

    [Fact]
    public async Task IsEmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "user",
            RealName = "用户",
            Email = "exists@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsEmailExistsAsync("exists@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Arrange
        // 数据库为空

        // Act
        var result = await _sut.IsEmailExistsAsync("nonexistent@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEmailExistsAsync_ExcludesDeletedUsers()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "deleted",
            RealName = "已删除用户",
            Email = "deleted@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.IsEmailExistsAsync("deleted@example.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AddAsync 测试

    [Fact]
    public async Task AddAsync_WithValidUser_ReturnsAddedUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "newuser",
            RealName = "新用户",
            Email = "new@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _sut.AddAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);

        var saved = await _context.Users.FindAsync(user.Id);
        saved.Should().NotBeNull();
        saved!.UserName.Should().Be("newuser");
    }

    [Fact]
    public async Task AddAsync_WithNullUser_ThrowsArgumentNullException()
    {
        // Arrange
        User? nullUser = null;

        // Act & Assert
        await _sut.Invoking(s => s.AddAsync(nullUser!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync 测试

    [Fact]
    public async Task UpdateAsync_WithValidUser_UpdatesUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "original",
            RealName = "原名",
            Email = "original@example.com",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _context.Entry(user).State = EntityState.Detached;

        user.RealName = "新名字";
        user.Email = "updated@example.com";

        // Act
        var result = await _sut.UpdateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.RealName.Should().Be("新名字");
        result.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task UpdateAsync_WithNullUser_ThrowsArgumentNullException()
    {
        // Arrange
        User? nullUser = null;

        // Act & Assert
        await _sut.Invoking(s => s.UpdateAsync(nullUser!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteAsync 测试

    [Fact]
    public async Task DeleteAsync_WithExistingId_MarksAsDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "tobedeleted",
            RealName = "待删除",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(userId);

        // Assert
        result.Should().BeTrue();

        var deleted = await _context.Users.FindAsync(userId);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CountAsync 测试

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = Guid.NewGuid(), UserName = "user1", RealName = "用户1", PasswordHash = "hash1", Role = UserRole.Doctor, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), UserName = "user2", RealName = "用户2", PasswordHash = "hash2", Role = UserRole.Doctor, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), UserName = "user3", RealName = "用户3", PasswordHash = "hash3", Role = UserRole.Admin, CreatedAt = DateTime.UtcNow }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var totalCount = await _sut.CountAsync();
        var doctorCount = await _sut.CountAsync(u => u.Role == UserRole.Doctor);

        // Assert
        totalCount.Should().Be(3);
        doctorCount.Should().Be(2);
    }

    #endregion

    #region ExistsAsync 测试

    [Fact]
    public async Task ExistsAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            UserName = "exists",
            RealName = "存在",
            PasswordHash = "hash123",
            Role = UserRole.Doctor,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ExistsAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.ExistsAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
