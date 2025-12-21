using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using LYBT.Entities.Herbs;
using LYBT.Entities.Patients;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Server.PerformanceTests
{
    /// <summary>
    /// Server端性能基准测试（Users/Patients/Herbs）
    /// Issue #2005: 直接测试EF Core CRUD操作性能
    ///
    /// 注意：此测试使用InMemory数据库，测试EF Core基础性能
    /// 实际SQL Server性能可能有差异
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
        Justification = "BenchmarkDotNet通过[GlobalCleanup]管理资源释放")]
    public class ServerPerformanceTests
    {
        private AppDbContext _context = null!;
        private List<User> _testUsers = null!;
        private List<Patient> _testPatients = null!;
        private List<Herb> _testHerbs = null!;

        [GlobalSetup]
        public void Setup()
        {
            // 设置InMemory数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 初始化测试数据
            InitializeTestData().GetAwaiter().GetResult();
        }

        private async Task InitializeTestData()
        {
            var baseTime = DateTime.Now;

            // 准备Users测试数据（100条用于查询，1000条用于批量导入模拟）
            _testUsers = new List<User>();
            for (int i = 1; i <= 100; i++)
            {
                _testUsers.Add(new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"user{i}",
                    RealName = $"测试用户{i}",
                    PasswordHash = "dummy_hash_for_performance_test",
                    Email = $"user{i}@example.com",
                    PhoneNumber = $"1380013800{i:00}",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime.AddDays(-i),
                    CreatedBy = Guid.Empty
                });
            }
            _context.Users.AddRange(_testUsers);

            // 准备Patients测试数据（100条用于查询）
            _testPatients = new List<Patient>();
            for (int i = 1; i <= 100; i++)
            {
                _testPatients.Add(new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"患者{i}",
                    Gender = i % 3 == 0 ? Gender.Unknown : (i % 2 == 0 ? Gender.Female : Gender.Male),
                    BirthDate = new DateTime(1950 + i % 50, 1, 1),
                    PhoneNumber = $"1380013800{i:00}",
                    Address = $"北京市朝阳区{i}号",
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime.AddDays(-i),
                    CreatedBy = Guid.Empty
                });
            }
            _context.Patients.AddRange(_testPatients);

            // 准备Herbs测试数据（100条用于查询）
            _testHerbs = new List<Herb>();
            for (int i = 1; i <= 100; i++)
            {
                _testHerbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"中药{i}",
                    PinYinCode = $"zy{i}",
                    Category = i % 3 == 0 ? "补益药" : (i % 2 == 0 ? "清热药" : "理气药"),
                    Unit = "克",
                    Price = 10.00m + i,
                    CostPrice = 5.00m + i,
                    Status = CommonStatus.Enabled,
                    CreatedAt = baseTime.AddDays(-i),
                    CreatedBy = Guid.Empty
                });
            }
            _context.Herbs.AddRange(_testHerbs);

            await _context.SaveChangesAsync();
        }

        #region Users模块性能测试

        /// <summary>
        /// 测试Users分页查询性能（100条数据，每页20条）
        /// 性能基准：P95 &lt; 500ms
        /// </summary>
        [Benchmark(Description = "Users-分页查询（100条，第1页20条）")]
        public async Task Users_GetPaged_Benchmark()
        {
            var result = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .Skip(0)
                .Take(20)
                .ToListAsync();
        }

        /// <summary>
        /// 测试Users单条创建性能
        /// 性能基准：P95 &lt; 300ms
        /// </summary>
        [Benchmark(Description = "Users-单条创建")]
        public async Task Users_Create_Benchmark()
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = $"benchmark_{Guid.NewGuid():N}",
                RealName = "性能测试用户",
                PasswordHash = "dummy_hash",
                Email = $"benchmark_{Guid.NewGuid():N}@example.com",
                PhoneNumber = "13800138888",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                CreatedBy = Guid.Empty
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 测试Users批量导入性能（1000条，循环创建模拟）
        /// 性能基准：1000条 &lt; 10s
        /// </summary>
        [Benchmark(Description = "Users-批量导入（1000条模拟）")]
        public async Task Users_BatchImport_Benchmark()
        {
            var users = new List<User>();
            for (int i = 0; i < 1000; i++)
            {
                users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    UserName = $"batch_{i}_{Guid.NewGuid():N}",
                    RealName = $"批量用户{i}",
                    PasswordHash = "dummy_hash",
                    Email = $"batch{i}@example.com",
                    PhoneNumber = $"1390013900{i:000}",
                    Role = UserRole.Doctor,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = Guid.Empty
                });
            }

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Patients模块性能测试

        /// <summary>
        /// 测试Patients分页查询性能（100条数据，每页20条）
        /// 性能基准：P95 &lt; 500ms
        /// </summary>
        [Benchmark(Description = "Patients-分页查询（100条，第1页20条）")]
        public async Task Patients_GetPaged_Benchmark()
        {
            var result = await _context.Patients
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(0)
                .Take(20)
                .ToListAsync();
        }

        /// <summary>
        /// 测试Patients单条创建性能
        /// 性能基准：P95 &lt; 300ms
        /// </summary>
        [Benchmark(Description = "Patients-单条创建")]
        public async Task Patients_Create_Benchmark()
        {
            var newPatient = new Patient
            {
                Id = Guid.NewGuid(),
                Name = $"性能测试患者_{Guid.NewGuid():N}",
                Gender = Gender.Male,
                BirthDate = new DateTime(1980, 1, 1),
                PhoneNumber = "13800138888",
                Address = "北京市朝阳区测试地址",
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                CreatedBy = Guid.Empty
            };

            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 测试Patients批量导入性能（1000条，循环创建模拟）
        /// 性能基准：1000条 &lt; 10s
        /// </summary>
        [Benchmark(Description = "Patients-批量导入（1000条模拟）")]
        public async Task Patients_BatchImport_Benchmark()
        {
            var patients = new List<Patient>();
            for (int i = 0; i < 1000; i++)
            {
                patients.Add(new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"批量患者{i}_{Guid.NewGuid():N}",
                    Gender = i % 3 == 0 ? Gender.Unknown : (i % 2 == 0 ? Gender.Female : Gender.Male),
                    BirthDate = new DateTime(1960 + i % 40, 1, 1),
                    PhoneNumber = $"1390013900{i:000}",
                    Address = $"上海市浦东新区{i}号",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = Guid.Empty
                });
            }

            _context.Patients.AddRange(patients);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Herbs模块性能测试

        /// <summary>
        /// 测试Herbs分页查询性能（100条数据，每页20条）
        /// 性能基准：P95 &lt; 500ms
        /// </summary>
        [Benchmark(Description = "Herbs-分页查询（100条，第1页20条）")]
        public async Task Herbs_GetPaged_Benchmark()
        {
            var result = await _context.Herbs
                .Where(h => !h.IsDeleted)
                .OrderByDescending(h => h.CreatedAt)
                .Skip(0)
                .Take(20)
                .ToListAsync();
        }

        /// <summary>
        /// 测试Herbs单条创建性能
        /// 性能基准：P95 &lt; 300ms
        /// </summary>
        [Benchmark(Description = "Herbs-单条创建")]
        public async Task Herbs_Create_Benchmark()
        {
            var newHerb = new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"性能测试中药_{Guid.NewGuid():N}",
                PinYinCode = $"test_{Guid.NewGuid():N}",
                Category = "补益药",
                Unit = "克",
                Price = 15.00m,
                CostPrice = 8.00m,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.Now,
                CreatedBy = Guid.Empty
            };

            _context.Herbs.Add(newHerb);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 测试Herbs批量导入性能（1000条，循环创建模拟）
        /// 性能基准：1000条 &lt; 10s
        /// </summary>
        [Benchmark(Description = "Herbs-批量导入（1000条模拟）")]
        public async Task Herbs_BatchImport_Benchmark()
        {
            var herbs = new List<Herb>();
            for (int i = 0; i < 1000; i++)
            {
                herbs.Add(new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = $"批量中药{i}_{Guid.NewGuid():N}",
                    PinYinCode = $"batch_{i}",
                    Category = i % 3 == 0 ? "补益药" : (i % 2 == 0 ? "清热药" : "理气药"),
                    Unit = "克",
                    Price = 10.00m + i,
                    CostPrice = 5.00m + i,
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    CreatedBy = Guid.Empty
                });
            }

            _context.Herbs.AddRange(herbs);
            await _context.SaveChangesAsync();
        }

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }
    }
}
