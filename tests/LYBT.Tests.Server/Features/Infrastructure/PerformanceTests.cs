using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using LYBT.Tests.Server.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Server.Features.Infrastructure;

/// <summary>
/// API 性能基准测试 -- 对齐 NFR-PERF-001~004。
///
/// 测试分两组:
///   1. CI 友好测试 (20 条 seed): 验证端点可达 + 基本性能
///   2. NFR 基准测试 (5000 患者 + 25000 医案): P95 指标校准
///
/// P95 统计: 每个测试执行 20 次，取第 19 个最小值 (95th percentile)。
/// NFR 基准测试标记为 [Trait("Category", "Performance")]，CI 中可选跳过。
/// </summary>
public sealed class PerformanceTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    // CI 友好阈值 (小数据量)
    private const int PaginationTimeoutMs = 500;
    private const int SearchTimeoutMs = 800;
    private const int MedicalCaseDetailTimeoutMs = 1000;
    private const int ConcurrentRequestCount = 10;
    private const int MinConcurrentSuccessRate = 8; // 80%

    // NFR P95 阈值 (大数据量, 对齐 nfr.md)
    private const int NfrSimpleQueryP95Ms = 500;     // NFR-PERF-001a
    private const int NfrListQueryP95Ms = 1000;      // NFR-PERF-001b
    private const int NfrAggregateSaveP95Ms = 2000;  // NFR-PERF-001c
    private const int NfrBatchImportP95Ms = 5000;    // NFR-PERF-001d

    // P95 统计参数
    private const int P95Iterations = 20;

    // seed 数据量
    private const int SeedPatientCount = 20;

    // NFR 标准数据量
    private const int NfrPatientCount = 5000;
    private const int NfrHerbCount = 200;
    private const int NfrMedicalCaseCount = 25000;

    public PerformanceTests(ServerFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
    }

    #region Helpers

    private static int _idSeq;
    private static string UniqueIdNumber()
    {
        var seq = Interlocked.Increment(ref _idSeq);
        return $"50010019880601{seq:D3}X";
    }

    private async Task SeedPatientsAsync(int count)
    {
        var admin = await LoginAsAdminAsync();
        var adminUserId = await GetAdminUserIdAsync(admin);

        await Fixture.WithDbContextAsync(async db =>
        {
            for (var i = 0; i < count; i++)
            {
                db.Set<LYBT.Entities.Patients.Patient>().Add(new LYBT.Entities.Patients.Patient
                {
                    Id = Guid.NewGuid(),
                    Name = $"性能患者{i:D4}",
                    Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                    PhoneNumber = $"136{Random.Shared.Next(10000000, 99999999)}",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = adminUserId,
                    UpdatedBy = adminUserId
                });
            }
            await db.SaveChangesAsync();
        });
        _output.WriteLine($"Seeded {count} patients");
    }

    private async Task<Guid> CreatePatientAsync()
    {
        var admin = await LoginAsAdminAsync();
        var input = new PatientInputDto
        {
            Name = "性能医案患者_" + Guid.NewGuid().ToString("N")[..4],
            Gender = Gender.Male,
            IdNumber = UniqueIdNumber(),
            PhoneNumber = $"135{Random.Shared.Next(10000000, 99999999)}",
            Address = "性能测试地址"
        };
        var resp = await admin.PostAsJsonAsync("/api/v1/patients", input);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PatientDetailDto>>(JsonOptions);
        return body!.Data!.Id;
    }

    /// <summary>DoctorClient 创建医案</summary>
    private async Task<MedicalCaseDetailDto> CreateMedicalCaseAsync(Guid patientId)
    {
        var doctor = await LoginAsDoctorAsync();
        var doctorUserId = await GetDoctorUserIdAsync(await LoginAsAdminAsync());

        var input = new MedicalCaseInputDto
        {
            PatientId = patientId,
            UserId = doctorUserId, // FluentValidation 要求 UserId 不为空
            Consultation = new LYBT.Shared.Models.Contracts.Consultation.ConsultationInputDto
            {
                PresentIllness = "性能测试",
                TcmDiagnosis = "气虚体倦"
            }
        };
        var resp = await doctor.PostAsJsonAsync("/api/v1/medicalcases", input);
        resp.IsSuccessStatusCode.Should().BeTrue($"创建医案失败: {resp.StatusCode}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<MedicalCaseDetailDto>>(JsonOptions);
        return body!.Data!;
    }

    /// <summary>
    /// 播种 NFR 标准数据集 (5000 患者 + 200 药材 + 25000 医案)。
    /// 仅在第一次调用时执行，后续跳过 (因为 Respawn 在 NFR 测试中不重置)。
    /// </summary>
    private async Task<Guid> SeedNfrDatasetAsync()
    {
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        // 检查是否已有足够数据
        var existingCount = await Fixture.WithDbContextAsync(async db =>
            await db.Set<LYBT.Entities.Patients.Patient>().CountAsync());

        if (existingCount >= NfrPatientCount)
        {
            _output.WriteLine($"[NFR] 已有 {existingCount} 患者，跳过播种");
            return doctorUserId;
        }

        _output.WriteLine($"[NFR] 开始播种标准数据集...");
        await Fixture.WithDbContextAsync(async db =>
        {
            await PerformanceDataSeeder.SeedAsync(
                db, doctorUserId,
                NfrPatientCount, NfrHerbCount, NfrMedicalCaseCount,
                _output);
        });

        return doctorUserId;
    }

    /// <summary>
    /// 执行 N 次操作并计算 P95 响应时间。
    /// </summary>
    private static long CalculateP95(List<long> latencies)
    {
        latencies.Sort();
        var p95Index = (int)Math.Ceiling(latencies.Count * 0.95) - 1;
        return latencies[Math.Max(0, p95Index)];
    }

    private void ReportP95(string testName, List<long> latencies, int thresholdMs)
    {
        var p95 = CalculateP95(latencies);
        var avg = latencies.Average();
        var min = latencies.Min();
        var max = latencies.Max();

        _output.WriteLine($"[{testName}] P95={p95}ms, Avg={avg:F0}ms, Min={min}ms, Max={max}ms ({latencies.Count} runs)");
        _output.WriteLine($"[{testName}] NFR threshold: {thresholdMs}ms | {(p95 <= thresholdMs ? "PASS" : "FAIL")}");
    }

    #endregion

    // ================================================================
    // CI 友好测试 (小数据量, 每次 Respawn 重置)
    // ================================================================

    #region 患者分页查询性能

    [Fact]
    public async Task Patients_PaginationQuery_ShouldRespondWithin500ms()
    {
        // Arrange
        await SeedPatientsAsync(SeedPatientCount);
        var admin = await LoginAsAdminAsync();

        // Act
        var sw = Stopwatch.StartNew();
        var resp = await admin.GetAsync("/api/v1/patients?page=1&pageSize=20");
        sw.Stop();

        // Assert
        resp.EnsureSuccessStatusCode();
        _output.WriteLine($"Pagination response: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(PaginationTimeoutMs);
    }

    #endregion

    #region 搜索性能

    [Fact]
    public async Task Patients_SearchByKeyword_ShouldRespondWithin800ms()
    {
        // Arrange
        await SeedPatientsAsync(SeedPatientCount);
        var admin = await LoginAsAdminAsync();

        // Act
        var sw = Stopwatch.StartNew();
        var resp = await admin.GetAsync("/api/v1/patients?page=1&pageSize=50&keyword=性能");
        sw.Stop();

        // Assert
        resp.EnsureSuccessStatusCode();
        _output.WriteLine($"Search response: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(SearchTimeoutMs);
    }

    #endregion

    #region 并发请求

    [Fact]
    public async Task Patients_ConcurrentRequests_ShouldHandleLoad()
    {
        // Arrange
        await SeedPatientsAsync(SeedPatientCount);
        var admin = await LoginAsAdminAsync();

        var tasks = new List<Task<HttpResponseMessage>>();
        var sw = Stopwatch.StartNew();

        // Act -- 10 个并发请求，每个使用独立 HttpClient with same auth
        var clients = new List<HttpClient>();
        for (var i = 0; i < ConcurrentRequestCount; i++)
        {
            var client = await LoginAsAdminAsync();
            clients.Add(client);
            tasks.Add(client.GetAsync("/api/v1/patients?page=1&pageSize=10"));
        }

        var responses = await Task.WhenAll(tasks);
        sw.Stop();

        // Cleanup clients
        foreach (var c in clients) c.Dispose();

        // Assert
        var successCount = responses.Count(r => r.IsSuccessStatusCode);
        _output.WriteLine($"Concurrent: {successCount}/{ConcurrentRequestCount} success, {sw.ElapsedMilliseconds}ms");

        successCount.Should().BeGreaterOrEqualTo(MinConcurrentSuccessRate,
            $"至少 {MinConcurrentSuccessRate}/{ConcurrentRequestCount} 请求应成功");
    }

    #endregion

    #region 医案关联查询性能

    [Fact]
    public async Task MedicalCase_DetailWithRelations_ShouldRespondWithin1s()
    {
        // Arrange -- DoctorClient 创建医案 (正确角色)
        var patientId = await CreatePatientAsync();
        var medicalCase = await CreateMedicalCaseAsync(patientId);
        _output.WriteLine($"医案: {medicalCase.Id}");

        // Act
        var doctor = await LoginAsDoctorAsync();
        var sw = Stopwatch.StartNew();
        var resp = await doctor.GetAsync($"/api/v1/medicalcases/{medicalCase.Id}");
        sw.Stop();

        // Assert
        resp.EnsureSuccessStatusCode();
        _output.WriteLine($"MedicalCase detail response: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(MedicalCaseDetailTimeoutMs,
            "医案详情查询应在 1s 内完成 (含 Consultation + Prescription 关联)");
    }

    #endregion

    // ================================================================
    // NFR 基准测试 (大数据量, P95 统计)
    // 标记 [Trait("Category", "Performance")] 以便 CI 按需跳过
    // ================================================================

    #region NFR-PERF-001a: 简单查询 P95 < 500ms

    [Fact]
    [Trait("Category", "Performance")]
    public async Task NFR_SimpleQuery_P95ShouldBeLessThan500ms()
    {
        // Arrange: 播种标准数据集
        await SeedNfrDatasetAsync();
        var doctor = await LoginAsDoctorAsync();

        // 先获取一个患者 ID
        var resp = await doctor.GetAsync("/api/v1/patients?page=1&pageSize=1");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PatientListDto>>>(JsonOptions);
        var patientId = body!.Data!.Items.First().Id;

        // Act: 执行 P95Iterations 次简单查询
        var latencies = new List<long>(P95Iterations);
        for (var i = 0; i < P95Iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var r = await doctor.GetAsync($"/api/v1/patients/{patientId}");
            sw.Stop();
            r.EnsureSuccessStatusCode();
            latencies.Add(sw.ElapsedMilliseconds);
        }

        // Assert
        ReportP95("NFR-PERF-001a", latencies, NfrSimpleQueryP95Ms);
        CalculateP95(latencies).Should().BeLessThan(NfrSimpleQueryP95Ms,
            "简单查询 P95 应 < 500ms (NFR-PERF-001a)");
    }

    #endregion

    #region NFR-PERF-001b: 列表查询 P95 < 1s

    [Fact]
    [Trait("Category", "Performance")]
    public async Task NFR_ListQuery_P95ShouldBeLessThan1s()
    {
        // Arrange
        await SeedNfrDatasetAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act: 各种列表查询
        var latencies = new List<long>(P95Iterations);
        for (var i = 0; i < P95Iterations; i++)
        {
            var page = 1 + (i % 10);
            var sw = Stopwatch.StartNew();
            var r = await doctor.GetAsync($"/api/v1/patients?page={page}&pageSize=20&keyword=王");
            sw.Stop();
            r.EnsureSuccessStatusCode();
            latencies.Add(sw.ElapsedMilliseconds);
        }

        // Assert
        ReportP95("NFR-PERF-001b", latencies, NfrListQueryP95Ms);
        CalculateP95(latencies).Should().BeLessThan(NfrListQueryP95Ms,
            "列表查询 P95 应 < 1s (NFR-PERF-001b)");
    }

    #endregion

    #region NFR-PERF-001c: 聚合保存 P95 < 2s

    [Fact]
    [Trait("Category", "Performance")]
    public async Task NFR_AggregateSave_P95ShouldBeLessThan2s()
    {
        // Arrange
        await SeedNfrDatasetAsync();
        var doctor = await LoginAsDoctorAsync();
        var admin = await LoginAsAdminAsync();
        var doctorUserId = await GetDoctorUserIdAsync(admin);

        // 预创建用于测试的患者
        var patientIds = new List<Guid>();
        for (var i = 0; i < P95Iterations; i++)
        {
            patientIds.Add(await CreatePatientAsync());
        }

        // Act: 创建含 Consultation 的医案
        var latencies = new List<long>(P95Iterations);
        for (var i = 0; i < P95Iterations; i++)
        {
            var input = new MedicalCaseInputDto
            {
                PatientId = patientIds[i],
                UserId = doctorUserId,
                Consultation = new LYBT.Shared.Models.Contracts.Consultation.ConsultationInputDto
                {
                    PresentIllness = $"性能基准测试_{i}",
                    TcmDiagnosis = "气虚体倦",
                    TongueDiagnosis = "舌红苔薄白",
                    PulseDiagnosis = "脉弦细"
                }
            };

            var sw = Stopwatch.StartNew();
            var r = await doctor.PostAsJsonAsync("/api/v1/medicalcases", input);
            sw.Stop();
            r.IsSuccessStatusCode.Should().BeTrue($"创建医案失败 (第{i}次): {r.StatusCode}");
            latencies.Add(sw.ElapsedMilliseconds);
        }

        // Assert
        ReportP95("NFR-PERF-001c", latencies, NfrAggregateSaveP95Ms);
        CalculateP95(latencies).Should().BeLessThan(NfrAggregateSaveP95Ms,
            "聚合保存 P95 应 < 2s (NFR-PERF-001c)");
    }

    #endregion

    #region NFR-PERF-001d: 医案列表 (模拟大数据量列表查询)

    [Fact]
    [Trait("Category", "Performance")]
    public async Task NFR_MedicalCaseList_P95ShouldBeLessThan1s()
    {
        // Arrange
        await SeedNfrDatasetAsync();
        var doctor = await LoginAsDoctorAsync();

        // Act: 医案列表分页查询 (含关联数据)
        var latencies = new List<long>(P95Iterations);
        for (var i = 0; i < P95Iterations; i++)
        {
            var page = 1 + (i % 50);
            var sw = Stopwatch.StartNew();
            var r = await doctor.GetAsync($"/api/v1/medicalcases?page={page}&pageSize=20");
            sw.Stop();
            r.EnsureSuccessStatusCode();
            latencies.Add(sw.ElapsedMilliseconds);
        }

        // Assert
        ReportP95("NFR-PERF-001d-MedicalCaseList", latencies, NfrListQueryP95Ms);
        CalculateP95(latencies).Should().BeLessThan(NfrListQueryP95Ms,
            "医案列表查询 P95 应 < 1s (NFR-PERF-001b 扩展)");
    }

    #endregion

    #region NFR-PERF-004: 并发能力

    [Fact]
    [Trait("Category", "Performance")]
    public async Task NFR_ConcurrentLoad_ShouldHandleMultipleUsers()
    {
        // Arrange
        await SeedNfrDatasetAsync();

        // 模拟 3 个并发用户，每用户连续 10 个请求
        const int userCount = 3;
        const int requestsPerUser = 10;

        var allLatencies = new List<long>();
        var totalSuccess = 0;
        var totalRequests = userCount * requestsPerUser;

        // Act: 并发用户
        var userTasks = new List<Task<(int success, List<long> latencies)>>();
        for (var u = 0; u < userCount; u++)
        {
            var client = u % 2 == 0
                ? await LoginAsAdminAsync()
                : await LoginAsDoctorAsync();

            userTasks.Add(Task.Run(async () =>
            {
                var localLatencies = new List<long>(requestsPerUser);
                var localSuccess = 0;
                for (var r = 0; r < requestsPerUser; r++)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        var resp = await client.GetAsync($"/api/v1/patients?page={r + 1}&pageSize=20");
                        sw.Stop();
                        if (resp.IsSuccessStatusCode) localSuccess++;
                    }
                    catch
                    {
                        sw.Stop();
                    }
                    localLatencies.Add(sw.ElapsedMilliseconds);
                }
                return (localSuccess, localLatencies);
            }));
        }

        var results = await Task.WhenAll(userTasks);
        foreach (var (success, latencies) in results)
        {
            totalSuccess += success;
            allLatencies.AddRange(latencies);
        }

        // Assert
        var successRate = (double)totalSuccess / totalRequests * 100;
        _output.WriteLine($"[NFR-PERF-004] {totalSuccess}/{totalRequests} success ({successRate:F0}%)");
        ReportP95("NFR-PERF-004", allLatencies, NfrListQueryP95Ms);

        totalSuccess.Should().BeGreaterOrEqualTo((int)(totalRequests * 0.95),
            "95% 请求应成功 (1-3 并发用户)");
    }

    #endregion
}
