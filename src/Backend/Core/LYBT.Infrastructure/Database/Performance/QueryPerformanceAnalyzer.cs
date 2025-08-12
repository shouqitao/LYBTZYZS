using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Models;
using LYBT.Shared.Models;

namespace LYBT.Infrastructure.Database.Performance
{
    /// <summary>
    /// 查询性能分析器 - UltraThink重构数据库优化
    /// 分析和监控数据库查询性能，验证索引效果
    /// </summary>
    public class QueryPerformanceAnalyzer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QueryPerformanceAnalyzer> _logger;

        public QueryPerformanceAnalyzer(AppDbContext context, ILogger<QueryPerformanceAnalyzer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行完整的性能基准测试
        /// </summary>
        public async Task<PerformanceBenchmarkResult> RunFullBenchmarkAsync()
        {
            _logger.LogInformation("开始执行数据库性能基准测试...");

            var result = new PerformanceBenchmarkResult
            {
                TestStartTime = DateTime.UtcNow,
                UserQueryTests = await RunUserQueryBenchmarksAsync(),
                PatientQueryTests = await RunPatientQueryBenchmarksAsync(),
                HerbQueryTests = await RunHerbQueryBenchmarksAsync(),
                PrescriptionQueryTests = await RunPrescriptionQueryBenchmarksAsync()
            };

            result.TestEndTime = DateTime.UtcNow;
            result.TotalDuration = result.TestEndTime - result.TestStartTime;

            _logger.LogInformation("性能基准测试完成，总耗时: {Duration}ms", 
                result.TotalDuration.TotalMilliseconds);

            return result;
        }

        /// <summary>
        /// 用户查询性能测试
        /// </summary>
        private async Task<List<QueryPerformanceTest>> RunUserQueryBenchmarksAsync()
        {
            var tests = new List<QueryPerformanceTest>();

            // 1. 用户名查询测试 - IX_Users_UserName_Unique
            tests.Add(await MeasureQueryAsync(
                "GetUserByUsername",
                "SELECT * FROM Users WHERE UserName = 'sysadmin'",
                async () => await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == "sysadmin")
            ));

            // 2. 用户分页查询测试 - IX_Users_Role_IsActive_CreatedAt  
            tests.Add(await MeasureQueryAsync(
                "GetUsersPagedByRole",
                "SELECT * FROM Users WHERE Role = 1 AND IsActive = 1 ORDER BY CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY",
                async () => await _context.Users.AsNoTracking()
                    .Where(u => u.Role == UserRole.Doctor && u.IsActive)
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip(0).Take(20)
                    .ToListAsync()
            ));

            // 3. 用户搜索查询测试 - IX_Users_RealName_IsActive
            tests.Add(await MeasureQueryAsync(
                "SearchUsersByRealName",
                "SELECT * FROM Users WHERE RealName LIKE '%管理%' AND IsActive = 1 ORDER BY RealName",
                async () => await _context.Users.AsNoTracking()
                    .Where(u => u.RealName.Contains("管理") && u.IsActive)
                    .OrderBy(u => u.RealName)
                    .Take(20)
                    .ToListAsync()
            ));

            // 4. 用户统计查询测试 - IX_Users_IsActive_Role_CreatedAt
            tests.Add(await MeasureQueryAsync(
                "GetUserStatistics",
                "SELECT Role, COUNT(*) FROM Users WHERE IsActive = 1 GROUP BY Role",
                async () => await _context.Users.AsNoTracking()
                    .Where(u => u.IsActive)
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToListAsync()
            ));

            // 5. 用户日期范围查询测试 - IX_Users_CreatedAt_Role
            tests.Add(await MeasureQueryAsync(
                "GetUsersByDateRange",
                "SELECT * FROM Users WHERE CreatedAt >= @startDate AND CreatedAt <= @endDate ORDER BY CreatedAt DESC",
                async () => await _context.Users.AsNoTracking()
                    .Where(u => u.CreatedAt >= DateTime.Now.AddDays(-30))
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync()
            ));

            return tests;
        }

        /// <summary>
        /// 患者查询性能测试
        /// </summary>
        private async Task<List<QueryPerformanceTest>> RunPatientQueryBenchmarksAsync()
        {
            var tests = new List<QueryPerformanceTest>();

            // 1. 患者姓名查询测试 - IX_Patients_Name_IsActive
            tests.Add(await MeasureQueryAsync(
                "GetPatientsByName",
                "SELECT * FROM Patients WHERE Name LIKE '%张%' AND IsActive = 1",
                async () => await _context.Set<PatientModel>().AsNoTracking()
                    .Where(p => p.Name.Contains("张") && p.IsActive)
                    .ToListAsync()
            ));

            // 2. 患者电话查询测试 - IX_Patients_PhoneNumber
            tests.Add(await MeasureQueryAsync(
                "GetPatientByPhoneNumber", 
                "SELECT * FROM Patients WHERE PhoneNumber = '13800138000'",
                async () => await _context.Set<PatientModel>().AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PhoneNumber == "13800138000")
            ));

            // 3. 患者搜索测试 - IX_Patients_Name_PhoneNumber_CreatedAt
            tests.Add(await MeasureQueryAsync(
                "SearchPatients",
                "SELECT * FROM Patients WHERE Name LIKE '%王%' OR PhoneNumber LIKE '%138%' ORDER BY CreatedAt DESC",
                async () => await _context.Set<PatientModel>().AsNoTracking()
                    .Where(p => p.Name.Contains("王") || p.PhoneNumber.Contains("138"))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(20)
                    .ToListAsync()
            ));

            return tests;
        }

        /// <summary>
        /// 中药材查询性能测试
        /// </summary>
        private async Task<List<QueryPerformanceTest>> RunHerbQueryBenchmarksAsync()
        {
            var tests = new List<QueryPerformanceTest>();

            // 1. 中药材名称查询测试 - IX_Herbs_Name_IsEnabled
            tests.Add(await MeasureQueryAsync(
                "GetHerbsByName",
                "SELECT * FROM Herbs WHERE Name LIKE '%甘草%' AND IsEnabled = 1",
                async () => await _context.Set<HerbModel>().AsNoTracking()
                    .Where(h => h.Name.Contains("甘草") && h.IsEnabled)
                    .ToListAsync()
            ));

            // 2. 中药材分类查询测试 - IX_Herbs_Category_IsEnabled_CreatedAt
            tests.Add(await MeasureQueryAsync(
                "GetHerbsByCategory",
                "SELECT * FROM Herbs WHERE Category = '补益类' AND IsEnabled = 1 ORDER BY CreatedAt DESC",
                async () => await _context.Set<HerbModel>().AsNoTracking()
                    .Where(h => h.Category == "补益类" && h.IsEnabled)
                    .OrderByDescending(h => h.CreatedAt)
                    .ToListAsync()
            ));

            // 3. 库存查询测试 - IX_Herbs_Stock_Price_IsEnabled
            tests.Add(await MeasureQueryAsync(
                "GetHerbsByStock",
                "SELECT * FROM Herbs WHERE Stock > 100 AND IsEnabled = 1 ORDER BY Price ASC",
                async () => await _context.Set<HerbModel>().AsNoTracking()
                    .Where(h => h.Stock > 100 && h.IsEnabled)
                    .OrderBy(h => h.Price)
                    .ToListAsync()
            ));

            return tests;
        }

        /// <summary>
        /// 处方查询性能测试
        /// </summary>
        private async Task<List<QueryPerformanceTest>> RunPrescriptionQueryBenchmarksAsync()
        {
            var tests = new List<QueryPerformanceTest>();

            // 注意：这些查询假设相关的模型存在，如果不存在需要根据实际情况调整

            // 1. 患者处方查询测试 - IX_Prescriptions_PatientId_CreatedAt
            tests.Add(await MeasureQueryAsync(
                "GetPrescriptionsByPatient",
                "SELECT * FROM Prescriptions WHERE PatientId = @patientId ORDER BY CreatedAt DESC",
                async () => {
                    var samplePatientId = await _context.Set<PatientModel>().AsNoTracking()
                        .Select(p => p.Id).FirstOrDefaultAsync();
                    if (samplePatientId != Guid.Empty)
                    {
                        return await _context.Set<PrescriptionModel>().AsNoTracking()
                            .Where(p => p.PatientId == samplePatientId)
                            .OrderByDescending(p => p.CreatedAt)
                            .ToListAsync();
                    }
                    return new List<PrescriptionModel>();
                }
            ));

            // 2. 医生处方查询测试 - IX_Prescriptions_DoctorId_CreatedAt
            tests.Add(await MeasureQueryAsync(
                "GetPrescriptionsByDoctor",
                "SELECT * FROM Prescriptions WHERE DoctorId = @doctorId ORDER BY CreatedAt DESC",
                async () => {
                    var sampleDoctorId = await _context.Users.AsNoTracking()
                        .Where(u => u.Role == UserRole.Doctor)
                        .Select(u => u.Id).FirstOrDefaultAsync();
                    if (sampleDoctorId != Guid.Empty)
                    {
                        return await _context.Set<PrescriptionModel>().AsNoTracking()
                            .Where(p => p.DoctorId == sampleDoctorId)
                            .OrderByDescending(p => p.CreatedAt)
                            .ToListAsync();
                    }
                    return new List<PrescriptionModel>();
                }
            ));

            return tests;
        }

        /// <summary>
        /// 测量单个查询的执行时间
        /// </summary>
        private async Task<QueryPerformanceTest> MeasureQueryAsync<T>(
            string testName, 
            string sqlQuery, 
            Func<Task<T>> queryFunc)
        {
            var test = new QueryPerformanceTest
            {
                TestName = testName,
                SqlQuery = sqlQuery,
                StartTime = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 预热查询（第一次执行可能较慢）
                await queryFunc();
                
                // 重置计时器，开始正式测量
                stopwatch.Restart();
                
                // 执行多次取平均值
                const int iterations = 5;
                for (int i = 0; i < iterations; i++)
                {
                    await queryFunc();
                }
                
                stopwatch.Stop();
                
                test.ExecutionTimeMs = stopwatch.ElapsedMilliseconds / iterations;
                test.IsSuccessful = true;
                
                _logger.LogDebug("查询性能测试 [{TestName}] 完成: {Duration}ms (平均)", 
                    testName, test.ExecutionTimeMs);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                test.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "查询性能测试 [{TestName}] 失败", testName);
            }
            
            test.EndTime = DateTime.UtcNow;
            return test;
        }

        /// <summary>
        /// 分析索引使用情况
        /// </summary>
        public async Task<List<IndexUsageInfo>> AnalyzeIndexUsageAsync()
        {
            try
            {
                var indexUsageQuery = @"
                    SELECT 
                        t.name AS TableName,
                        i.name AS IndexName,
                        i.type_desc AS IndexType,
                        ius.user_seeks AS UserSeeks,
                        ius.user_scans AS UserScans,
                        ius.user_lookups AS UserLookups,
                        ius.user_updates AS UserUpdates,
                        ius.last_user_seek AS LastUserSeek,
                        ius.last_user_scan AS LastUserScan
                    FROM sys.indexes i
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    LEFT JOIN sys.dm_db_index_usage_stats ius ON i.object_id = ius.object_id AND i.index_id = ius.index_id
                    WHERE t.name IN ('Users', 'Patients', 'Herbs', 'Prescriptions', 'FormulaTemplates', 'Consultations', 'MedicalCases')
                    ORDER BY t.name, i.name";

                var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                command.CommandText = indexUsageQuery;
                
                var results = new List<IndexUsageInfo>();
                
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new IndexUsageInfo
                    {
                        TableName = reader.GetString("TableName"),
                        IndexName = reader.GetString("IndexName"),
                        IndexType = reader.GetString("IndexType"),
                        UserSeeks = reader.IsDBNull("UserSeeks") ? 0 : reader.GetInt64("UserSeeks"),
                        UserScans = reader.IsDBNull("UserScans") ? 0 : reader.GetInt64("UserScans"),
                        UserLookups = reader.IsDBNull("UserLookups") ? 0 : reader.GetInt64("UserLookups"),
                        UserUpdates = reader.IsDBNull("UserUpdates") ? 0 : reader.GetInt64("UserUpdates"),
                        LastUserSeek = reader.IsDBNull("LastUserSeek") ? null : reader.GetDateTime("LastUserSeek"),
                        LastUserScan = reader.IsDBNull("LastUserScan") ? null : reader.GetDateTime("LastUserScan")
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析索引使用情况时发生错误");
                return new List<IndexUsageInfo>();
            }
        }

        /// <summary>
        /// 生成性能优化建议
        /// </summary>
        public List<PerformanceRecommendation> GenerateRecommendations(PerformanceBenchmarkResult benchmarkResult)
        {
            var recommendations = new List<PerformanceRecommendation>();

            // 分析用户查询性能
            AnalyzeUserQueryPerformance(benchmarkResult.UserQueryTests, recommendations);
            
            // 分析患者查询性能
            AnalyzePatientQueryPerformance(benchmarkResult.PatientQueryTests, recommendations);
            
            // 分析其他查询性能
            AnalyzeOtherQueryPerformance(benchmarkResult.HerbQueryTests, recommendations);
            AnalyzeOtherQueryPerformance(benchmarkResult.PrescriptionQueryTests, recommendations);

            return recommendations;
        }

        private void AnalyzeUserQueryPerformance(List<QueryPerformanceTest> tests, List<PerformanceRecommendation> recommendations)
        {
            foreach (var test in tests)
            {
                if (test.ExecutionTimeMs > 100) // 超过100ms的查询需要优化
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Category = "用户查询优化",
                        TestName = test.TestName,
                        Issue = $"查询执行时间过长: {test.ExecutionTimeMs}ms",
                        Recommendation = GetUserQueryRecommendation(test.TestName),
                        Priority = test.ExecutionTimeMs > 1000 ? "高" : "中"
                    });
                }
            }
        }

        private void AnalyzePatientQueryPerformance(List<QueryPerformanceTest> tests, List<PerformanceRecommendation> recommendations)
        {
            foreach (var test in tests)
            {
                if (test.ExecutionTimeMs > 200) // 患者查询阈值稍高
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Category = "患者查询优化", 
                        TestName = test.TestName,
                        Issue = $"患者查询执行时间过长: {test.ExecutionTimeMs}ms",
                        Recommendation = "考虑优化患者搜索索引或增加全文搜索",
                        Priority = test.ExecutionTimeMs > 1000 ? "高" : "中"
                    });
                }
            }
        }

        private void AnalyzeOtherQueryPerformance(List<QueryPerformanceTest> tests, List<PerformanceRecommendation> recommendations)
        {
            foreach (var test in tests)
            {
                if (test.ExecutionTimeMs > 150)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Category = "通用查询优化",
                        TestName = test.TestName,
                        Issue = $"查询执行时间: {test.ExecutionTimeMs}ms",
                        Recommendation = "检查索引使用情况，考虑添加复合索引",
                        Priority = test.ExecutionTimeMs > 1000 ? "高" : "中"
                    });
                }
            }
        }

        private string GetUserQueryRecommendation(string testName)
        {
            return testName switch
            {
                "GetUserByUsername" => "确保IX_Users_UserName_Unique索引存在且被使用",
                "GetUsersPagedByRole" => "优化IX_Users_Role_IsActive_CreatedAt复合索引",
                "SearchUsersByRealName" => "考虑添加全文搜索或优化LIKE查询",
                "GetUserStatistics" => "确保IX_Users_IsActive_Role_CreatedAt索引覆盖查询",
                "GetUsersByDateRange" => "优化日期范围查询的索引策略",
                _ => "检查相关索引并考虑查询优化"
            };
        }
    }

    #region 性能测试数据模型

    /// <summary>
    /// 性能基准测试结果
    /// </summary>
    public class PerformanceBenchmarkResult
    {
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<QueryPerformanceTest> UserQueryTests { get; set; } = new();
        public List<QueryPerformanceTest> PatientQueryTests { get; set; } = new();
        public List<QueryPerformanceTest> HerbQueryTests { get; set; } = new();
        public List<QueryPerformanceTest> PrescriptionQueryTests { get; set; } = new();
        
        public double AverageExecutionTime => 
            GetAllTests().Where(t => t.IsSuccessful).Average(t => t.ExecutionTimeMs);
            
        public int TotalTests => GetAllTests().Count;
        public int SuccessfulTests => GetAllTests().Count(t => t.IsSuccessful);
        public int FailedTests => GetAllTests().Count(t => !t.IsSuccessful);

        public List<QueryPerformanceTest> GetAllTests()
        {
            var allTests = new List<QueryPerformanceTest>();
            allTests.AddRange(UserQueryTests);
            allTests.AddRange(PatientQueryTests);
            allTests.AddRange(HerbQueryTests);
            allTests.AddRange(PrescriptionQueryTests);
            return allTests;
        }
    }

    /// <summary>
    /// 单个查询性能测试
    /// </summary>
    public class QueryPerformanceTest
    {
        public string TestName { get; set; }
        public string SqlQuery { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 索引使用信息
    /// </summary>
    public class IndexUsageInfo
    {
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public long UserSeeks { get; set; }
        public long UserScans { get; set; }
        public long UserLookups { get; set; }
        public long UserUpdates { get; set; }
        public DateTime? LastUserSeek { get; set; }
        public DateTime? LastUserScan { get; set; }
        
        public long TotalReads => UserSeeks + UserScans + UserLookups;
        public bool IsUsed => TotalReads > 0;
        public double SeekScanRatio => UserScans == 0 ? double.MaxValue : (double)UserSeeks / UserScans;
    }

    /// <summary>
    /// 性能优化建议
    /// </summary>
    public class PerformanceRecommendation
    {
        public string Category { get; set; }
        public string TestName { get; set; }
        public string Issue { get; set; }
        public string Recommendation { get; set; }
        public string Priority { get; set; } // 高/中/低
    }

    #endregion
}