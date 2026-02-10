using LYBT.Desktop.IntegrationTests.EndToEnd.Fixtures;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Desktop.IntegrationTests.EndToEnd.Users;

/// <summary>
/// User 模块 E2E 集成测试
/// ViewModel -> Repository -> DataSource -> LocalDbContext(SQLite InMemory)
/// </summary>
public class UserE2ETests : IDisposable
{
    private readonly DesktopE2ETestFixture _fixture;

    public UserE2ETests()
    {
        _fixture = new DesktopE2ETestFixture();
        _fixture.CreateServiceProvider();
    }

    [Fact]
    public async Task User_Create_EndToEnd()
    {
        var vm = _fixture.ServiceProvider.GetRequiredService<UserMasterDetailViewModel>();

        // Act - 新建用户
        await vm.CreateNewCommand.ExecuteAsync(null);
        await Task.Delay(200);

        vm.IsEditMode.Should().BeTrue();
        vm.CurrentDetail.Should().NotBeNull();

        vm.CurrentDetail!.UserName = "newdoctor";
        vm.CurrentDetail.RealName = "新医生";
        vm.CurrentDetail.Role = UserRole.Doctor;
        vm.CurrentDetail.PhoneNumber = "13900000001";

        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert - 验证 DB
        var db = _fixture.GetDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == "newdoctor");
        user.Should().NotBeNull();
        user!.RealName.Should().Be("新医生");
        user.Role.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public async Task User_RoleAssignment_ShouldPersist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _fixture.SeedDataAsync(async db =>
        {
            db.Users.Add(new User
            {
                Id = userId,
                UserName = "adminuser",
                RealName = "管理员",
                Role = UserRole.Admin,
                Status = CommonStatus.Enabled,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<UserMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);
        vm.SelectedItem = vm.Items.First();
        await Task.Delay(300);

        vm.EditCommand.Execute(null);
        vm.CurrentDetail!.Role = UserRole.Doctor;

        await vm.SaveCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        var db = _fixture.GetDbContext();
        var updated = await db.Users.FindAsync(userId);
        updated.Should().NotBeNull();
        updated!.Role.Should().Be(UserRole.Doctor);
    }

    [Fact]
    public async Task User_LoadList_ShouldReturnAllUsers()
    {
        // Arrange
        await _fixture.SeedDataAsync(async db =>
        {
            db.Users.AddRange(
                new User { Id = Guid.NewGuid(), UserName = "admin1", RealName = "管理员1", Role = UserRole.Admin, Status = CommonStatus.Enabled, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1!"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new User { Id = Guid.NewGuid(), UserName = "doctor1", RealName = "医生1", Role = UserRole.Doctor, Status = CommonStatus.Enabled, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass2!"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new User { Id = Guid.NewGuid(), UserName = "doctor2", RealName = "医生2", Role = UserRole.Doctor, Status = CommonStatus.Enabled, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass3!"), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();
        });

        var vm = _fixture.ServiceProvider.GetRequiredService<UserMasterDetailViewModel>();

        // Act
        await vm.RefreshCommand.ExecuteAsync(null);
        await Task.Delay(500);

        // Assert
        vm.Items.Should().HaveCount(3);
        vm.TotalCount.Should().Be(3);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
