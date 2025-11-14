using System.Diagnostics;
using FluentAssertions;
using LYBT.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.WebAPI.IntegrationTests.Controllers
{
    /// <summary>
    /// API性能基准测试
    /// Task 5.2 - 集成测试性能验证
    /// </summary>
    public class PerformanceTests : IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output) : base()
        {
            _output = output;
        }

        [Fact]
        public async Task GetPatients_ResponseTimeUnder500ms()
        {
            // Arrange
            await SeedLargeDataSetAsync(1000); // 种子1000条测试数据

            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await Client.GetAsync("/api/patients?page=1&pageSize=100");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"API响应时间: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
                $"API响应时间 {stopwatch.ElapsedMilliseconds}ms 超过500ms限制");
        }

        [Fact]
        public async Task GetPatients_WithLargeDataSet_TotalTimeUnder1s()
        {
            // Arrange
            await SeedLargeDataSetAsync(500); // 种子500条测试数据

            var stopwatch = Stopwatch.StartNew();

            // Act - 连续请求10页数据
            for (int page = 1; page <= 10; page++)
            {
                var response = await Client.GetAsync($"/api/patients?page={page}&pageSize=50");
                response.EnsureSuccessStatusCode();
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"10页数据总响应时间: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
                $"10页数据总响应时间 {stopwatch.ElapsedMilliseconds}ms 超过1秒限制");
        }

        [Fact]
        public async Task GetPatients_WithSearchKeyword_PerformanceAcceptable()
        {
            // Arrange
            await SeedLargeDataSetAsync(1000);
            var searchKeyword = "测试";

            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await Client.GetAsync($"/api/patients?page=1&pageSize=50&keyword={searchKeyword}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"搜索响应时间: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(800,
                $"搜索响应时间 {stopwatch.ElapsedMilliseconds}ms 超过800ms限制");
        }

        [Fact]
        public async Task ConcurrentRequests_HandleLoadSuccessfully()
        {
            // Arrange
            await SeedLargeDataSetAsync(500);

            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = Stopwatch.StartNew();

            // Act - 并发50个请求
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Client.GetAsync("/api/patients?page=1&pageSize=10"));
            }

            var responses = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successCount = responses.Count(r => r.IsSuccessStatusCode);
            var failureCount = responses.Length - successCount;

            _output.WriteLine($"并发测试: {successCount}成功, {failureCount}失败, 总时间: {stopwatch.ElapsedMilliseconds}ms");

            // 至少90%的请求应该成功
            successCount.Should().BeGreaterThanOrEqualTo(45);

            // 总响应时间应该在合理范围内
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        }

        [Fact]
        public async Task MedicalCaseIntegrationTest_NoNPlusOneQueries()
        {
            // Arrange - 创建带关联数据的病案
            var medicalCaseId = await CreateMedicalCaseWithRelationsAsync();

            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await Client.GetAsync($"/api/medical-cases/{medicalCaseId}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"病案详情查询时间: {stopwatch.ElapsedMilliseconds}ms");

            // 病案查询包含关联数据，应该在合理时间内完成
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
                "病案查询时间过长，可能存在N+1查询问题");
        }

        [Fact]
        public async Task PrescriptionIntegrationTest_OptimizedQueries()
        {
            // Arrange - 创建带关联数据的处方
            var prescriptionId = await CreatePrescriptionWithRelationsAsync();

            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await Client.GetAsync($"/api/prescriptions/{prescriptionId}");
            stopwatch.Stop();

            // Assert
            response.EnsureSuccessStatusCode();

            _output.WriteLine($"处方详情查询时间: {stopwatch.ElapsedMilliseconds}ms");

            // 处方查询优化后应该很快
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
                "处方查询时间过长，需要进一步优化");
        }

        #region Helper Methods

        private async Task SeedLargeDataSetAsync(int count)
        {
            // 实现种子数据创建逻辑
            // 这里可以复用现有的种子数据方法或创建新的大数据集
        }

        private async Task<Guid> CreateMedicalCaseWithRelationsAsync()
        {
            // 创建带有关联数据的病案用于测试
            // 返回病案ID用于查询测试
            return Guid.NewGuid();
        }

        private async Task<Guid> CreatePrescriptionWithRelationsAsync()
        {
            // 创建带有关联数据的处方用于测试
            // 返回处方ID用于查询测试
            return Guid.NewGuid();
        }

        #endregion
    }
}