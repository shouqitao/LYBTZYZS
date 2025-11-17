using FluentAssertions;
using LYBT.Shared.Models.Common;

namespace LYBT.Server.Tests.Common.TestHelpers;

/// <summary>
/// 测试辅助工具类
/// 提供通用的测试数据创建、验证等功能
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// 创建测试用的实体
    /// </summary>
    public static TEntity CreateTestEntity<TEntity>(Action<TEntity>? configure = null)
        where TEntity : class, new()
    {
        var entity = new TEntity();
        configure?.Invoke(entity);
        return entity;
    }

    /// <summary>
    /// 创建测试用的实体列表
    /// </summary>
    public static List<TEntity> CreateTestEntities<TEntity>(int count, Action<TEntity, int>? configure = null)
        where TEntity : class, new()
    {
        var entities = new List<TEntity>();
        for (int i = 0; i < count; i++)
        {
            var entity = new TEntity();
            configure?.Invoke(entity, i);
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// 验证ApiResponse格式
    /// </summary>
    public static void AssertApiResponseFormat<T>(ApiResponse<T> response, bool shouldSucceed = true)
    {
        if (shouldSucceed)
        {
            response.Success.Should().BeTrue("操作应该成功");
            response.Data.Should().NotBeNull("成功响应应包含数据");
            response.Code.Should().Be(200, "成功响应状态码应为200");
        }
        else
        {
            response.Success.Should().BeFalse("操作应该失败");
            response.Data.Should().BeNull("失败响应不应包含数据");
            response.Code.Should().NotBe(200, "失败响应状态码不应为200");
        }
        response.RequestId.Should().NotBeEmpty("响应应包含请求ID");
        response.Message.Should().NotBeNullOrEmpty("响应应包含消息");
    }

    /// <summary>
    /// 验证分页响应格式
    /// </summary>
    public static void AssertPagedApiResponseFormat<T>(ApiResponse<PagedResult<T>> response, bool shouldSucceed = true)
    {
        AssertApiResponseFormat(response, shouldSucceed);

        if (shouldSucceed && response.Data != null)
        {
            response.Data.Items.Should().NotBeNull("分页响应应包含数据项");
            response.Data.TotalCount.Should().BeGreaterOrEqualTo(0, "总数应大于等于0");
            response.Data.CurrentPage.Should().BeGreaterOrEqualTo(1, "当前页应大于等于1");
            response.Data.PageSize.Should().BeGreaterOrEqualTo(1, "页面大小应大于等于1");
            response.Data.TotalPages.Should().BeGreaterOrEqualTo(0, "总页数应大于等于0");

            // 验证分页逻辑一致性
            if (response.Data.TotalCount > 0)
            {
                var expectedTotalPages = (int)Math.Ceiling((double)response.Data.TotalCount / response.Data.PageSize);
                response.Data.TotalPages.Should().Be(expectedTotalPages, "总页数计算应正确");
            }
        }
    }

    /// <summary>
    /// 创建测试用的Guid
    /// </summary>
    public static Guid CreateTestGuid(int seed = 0)
    {
        if (seed == 0)
        {
            return Guid.NewGuid();
        }

        // 基于种子创建可重现的Guid
        var bytes = new byte[16];
        BitConverter.GetBytes(seed).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    /// <summary>
    /// 创建测试用的日期时间
    /// </summary>
    public static DateTime CreateTestDateTime(int daysOffset = 0, int hour = 0, int minute = 0, int second = 0)
    {
        return DateTime.UtcNow.AddDays(daysOffset).AddHours(hour).AddMinutes(minute).AddSeconds(second);
    }

    /// <summary>
    /// 创建测试用的字符串
    /// </summary>
    public static string CreateTestString(string prefix = "Test", int? suffix = null)
    {
        return suffix.HasValue ? $"{prefix}_{suffix.Value}" : $"{prefix}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 创建测试用的邮箱
    /// </summary>
    public static string CreateTestEmail(string username = null, string domain = "lybt.test")
    {
        var safeUsername = username ?? CreateTestString("user", null);
        return $"{safeUsername}@{domain}";
    }

    /// <summary>
    /// 创建测试用的手机号
    /// </summary>
    public static string CreateTestPhoneNumber(string prefix = "138")
    {
        var random = new Random();
        var suffix = random.Next(10000000, 99999999);
        return $"{prefix}{suffix}";
    }

    /// <summary>
    /// 验证Mock对象调用
    /// </summary>
    public static void VerifyMockCall<T>(Mock<T> mock, Expression<Action<T>> expression, Times times)
        where T : class
    {
        mock.Verify(expression, times);
    }

    /// <summary>
    /// 验证Mock对象调用（使用默认Times.Once）
    /// </summary>
    public static void VerifyMockCall<T>(Mock<T> mock, Expression<Action<T>> expression)
        where T : class
    {
        mock.Verify(expression, Times.Once);
    }

    /// <summary>
    /// 验证Mock对象属性调用
    /// </summary>
    public static void VerifyMockGet<T, TProperty>(Mock<T> mock, Expression<Func<T, TProperty>> expression, Times times)
        where T : class
    {
        mock.VerifyGet(expression, times);
    }

    /// <summary>
    /// 验证Mock对象属性调用（使用默认Times.Once）
    /// </summary>
    public static void VerifyMockGet<T, TProperty>(Mock<T> mock, Expression<Func<T, TProperty>> expression)
        where T : class
    {
        mock.VerifyGet(expression, Times.Once);
    }

    /// <summary>
    /// 设置Mock对象在调用时抛出异常
    /// </summary>
    public static void SetupMockThrow<T>(Mock<T> mock, Expression<Action<T>> expression, Exception exception)
        where T : class
    {
        mock.Setup(expression).Throws(exception);
    }

    /// <summary>
    /// 设置Mock对象属性在调用时抛出异常
    /// </summary>
    public static void SetupMockThrow<T, TProperty>(Mock<T> mock, Expression<Func<T, TProperty>> expression, Exception exception)
        where T : class
    {
        mock.Setup(expression).Throws(exception);
    }

    /// <summary>
    /// 验证Result类型的成功状态
    /// </summary>
    public static void AssertResultSuccess<T>(Result<T> result, string expectedMessage = null)
    {
        result.Should().NotBeNull("Result不应为null");
        result.IsSuccess.Should().BeTrue("操作应该成功");
        result.Data.Should().NotBeNull("成功Result应包含数据");

        if (expectedMessage != null)
        {
            result.Message.Should().Be(expectedMessage, "消息应该匹配预期值");
        }
    }

    /// <summary>
    /// 验证Result类型的失败状态
    /// </summary>
    public static void AssertResultFailure<T>(Result<T> result, string expectedErrorMessage = null)
    {
        result.Should().NotBeNull("Result不应为null");
        result.IsSuccess.Should().BeFalse("操作应该失败");
        result.Data.Should().BeNull("失败Result不应包含数据");
        result.ErrorMessage.Should().NotBeNullOrEmpty("失败Result应包含错误消息");

        if (expectedErrorMessage != null)
        {
            result.ErrorMessage.Should().Contain(expectedErrorMessage, "错误消息应该包含预期内容");
        }
    }

    /// <summary>
    /// 等待异步操作完成（带超时）
    /// </summary>
    public static async Task<T> WaitWithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));

        if (completedTask == task)
        {
            cts.Cancel();
            return await task;
        }
        else
        {
            throw new TimeoutException($"操作在 {timeout.TotalSeconds} 秒后超时");
        }
    }

    /// <summary>
    /// 创建测试用的文件内容
    /// </summary>
    public static byte[] CreateTestFileContent(string content = "Test file content")
    {
        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    /// <summary>
    /// 创建测试用的图片内容（简单的PNG文件头）
    /// </summary>
    public static byte[] CreateTestImageContent()
    {
        // 简单的1x1像素PNG文件
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
    }
}