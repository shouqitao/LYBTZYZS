using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Data.Seeding;

/// <summary>
/// Seed Data服务 - 通过EF Core HasData提供种子数据
/// 用于Migration阶段的初始化数据
/// 
/// 设计决策(DD-006):
/// - 生产环境：仅SuperAdmin（始终执行）
/// - 开发环境：测试数据（仅DEBUG构建）
/// - 使用预处理器指令实现环境分离
/// 
/// 注意：运行时动态创建用户使用DatabaseInitializationService
/// </summary>
public static class SeedDataService
{
    /// <summary>
    /// SuperAdmin固定ID - 用于Migration和测试数据的一致性
    /// </summary>
    public static readonly Guid SuperAdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// 应用种子数据到ModelBuilder
    /// 在AppDbContext.OnModelCreating中调用
    /// </summary>
    /// <param name="modelBuilder">EF Core ModelBuilder</param>
    public static void Seed(ModelBuilder modelBuilder)
    {
        // 生产环境：仅SuperAdmin（始终执行）
        // 注意：实际SuperAdmin创建已由DatabaseInitializationService在运行时处理
        // 此处仅作为Migration的备用种子数据（如需通过Migration创建）
        // SeedSuperAdmin(modelBuilder);

#if DEBUG
        // 开发环境：测试数据（仅DEBUG构建）
        SeedTestDoctor(modelBuilder);
        SeedTestPatients(modelBuilder);
        SeedTestHerbs(modelBuilder);
#endif
    }

    /// <summary>
    /// 种子数据：SuperAdmin用户
    /// 注意：当前系统通过DatabaseInitializationService在运行时创建SuperAdmin
    /// 此方法保留作为Migration种子数据的备选方案
    /// </summary>
    private static void SeedSuperAdmin(ModelBuilder modelBuilder)
    {
        // 使用固定的SuperAdmin配置
        // 密码由DatabaseInitializationService在运行时设置
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = SuperAdminId,
            UserName = "sysadmin",
            RealName = "系统管理员",
            Email = "sysadmin@lybt.local",
            Role = UserRole.SuperAdmin,
            Status = CommonStatus.Enabled,
            // 注意：HasData中的密码哈希需要预先计算
            // 实际使用时由DatabaseInitializationService设置
            PasswordHash = string.Empty,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = Guid.Empty,
            IsDeleted = false
        });
    }

#if DEBUG
    // 固定测试ID - 保证跨环境一致性
    private static readonly Guid TestDoctorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid TestPatient1Id = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid TestPatient2Id = Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid TestPatient3Id = Guid.Parse("00000000-0000-0000-0000-000000000103");

    /// <summary>
    /// 测试数据：医生用户
    /// 仅在DEBUG构建中包含
    /// </summary>
    private static void SeedTestDoctor(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = TestDoctorId,
            UserName = "testdoctor",
            RealName = "测试医生",
            PinYinCode = "csys",
            Email = "testdoctor@lybt.local",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = "AQAAAAIAAYagAAAAEMJGmCj5t2K0P2Z1Q8L5F9X8Y7W6V4U3T2S1R0Q9P8O7N6M5L4K3J2I1H0G9F8E7D6C5B4A3", // 占位哈希
            CreatedAt = seedDate,
            CreatedBy = SuperAdminId,
            IsDeleted = false
        });
    }

    /// <summary>
    /// 测试数据：患者
    /// 仅在DEBUG构建中包含
    /// </summary>
    private static void SeedTestPatients(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Patient>().HasData(
            new Patient
            {
                Id = TestPatient1Id,
                Name = "张三",
                PinYinCode = "zs",
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 5, 15),
                PhoneNumber = "13800000001",
                IdNumber = "110101198005150001",
                Address = "北京市朝阳区测试路1号",
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                CreatedBy = TestDoctorId,
                IsDeleted = false
            },
            new Patient
            {
                Id = TestPatient2Id,
                Name = "李四",
                PinYinCode = "ls",
                Gender = Gender.Female,
                BirthDate = new DateTime(1992, 8, 20),
                PhoneNumber = "13800000002",
                Address = "上海市浦东新区测试路2号",
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                CreatedBy = TestDoctorId,
                IsDeleted = false
            },
            new Patient
            {
                Id = TestPatient3Id,
                Name = "王五",
                PinYinCode = "ww",
                Gender = Gender.Male,
                BirthDate = new DateTime(1975, 3, 10),
                PhoneNumber = "13800000003",
                MedicalHistory = "高血压病史5年",
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                CreatedBy = TestDoctorId,
                IsDeleted = false
            }
        );
    }

    /// <summary>
    /// 测试数据：药材
    /// 仅在DEBUG构建中包含
    /// 常用中药材样本数据
    /// </summary>
    private static void SeedTestHerbs(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Herb>().HasData(
            new Herb
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000201"),
                Name = "甘草",
                PinYinCode = "gc",
                Category = "补气药",
                Unit = "克",
                Price = 0.5m,
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new Herb
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000202"),
                Name = "黄芪",
                PinYinCode = "hq",
                Category = "补气药",
                Unit = "克",
                Price = 1.2m,
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new Herb
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000203"),
                Name = "当归",
                PinYinCode = "dg",
                Category = "补血药",
                Unit = "克",
                Price = 1.5m,
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new Herb
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000204"),
                Name = "白术",
                PinYinCode = "bz",
                Category = "补气药",
                Unit = "克",
                Price = 0.8m,
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                IsDeleted = false
            },
            new Herb
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000205"),
                Name = "茯苓",
                PinYinCode = "fl",
                Category = "利水渗湿药",
                Unit = "克",
                Price = 0.6m,
                Status = CommonStatus.Enabled,
                CreatedAt = seedDate,
                IsDeleted = false
            }
        );
    }
#endif
}
