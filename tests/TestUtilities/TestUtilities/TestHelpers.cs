using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Backend.TestUtilities
{
    /// <summary>
    /// 测试辅助工具类 - 提供通用的测试工具方法
    /// </summary>
    public static class TestHelpers
    {
        #region 异步断言辅助

        /// <summary>
        /// 断言异步操作抛出指定类型的异常
        /// </summary>
        public static async Task<TException> AssertThrowsAsync<TException>(Func<Task> asyncAction)
            where TException : Exception
        {
            try
            {
                await asyncAction();
                throw new XunitException($"期望抛出 {typeof(TException).Name} 异常，但操作成功完成");
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new XunitException($"期望抛出 {typeof(TException).Name} 异常，但实际抛出了 {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 断言异步操作抛出指定类型的异常并包含指定消息
        /// </summary>
        public static async Task<TException> AssertThrowsAsync<TException>(Func<Task> asyncAction, string expectedMessage)
            where TException : Exception
        {
            var exception = await AssertThrowsAsync<TException>(asyncAction);
            Assert.Contains(expectedMessage, exception.Message);
            return exception;
        }

        /// <summary>
        /// 断言异步操作不抛出异常
        /// </summary>
        public static async Task AssertDoesNotThrowAsync(Func<Task> asyncAction)
        {
            try
            {
                await asyncAction();
            }
            catch (Exception ex)
            {
                throw new XunitException($"期望操作不抛出异常，但抛出了 {ex.GetType().Name}: {ex.Message}");
            }
        }

        #endregion

        #region ServiceResult 断言辅助

        /// <summary>
        /// 断言 ServiceResult 成功
        /// </summary>
        public static T AssertSuccess<T>(ServiceResult<T> result, string message = null)
        {
            Assert.True(result.IsSuccess, message ?? $"操作应该成功，但失败了: {result.ErrorMessage}");
            Assert.NotNull(result.Data);
            return result.Data;
        }

        /// <summary>
        /// 断言 ServiceResult 失败
        /// </summary>
        public static void AssertFailure<T>(ServiceResult<T> result, string expectedErrorMessage = null, string message = null)
        {
            Assert.False(result.IsSuccess, message ?? "操作应该失败");
            Assert.Null(result.Data);
            
            if (!string.IsNullOrEmpty(expectedErrorMessage))
            {
                Assert.Contains(expectedErrorMessage, result.ErrorMessage);
            }
        }

        /// <summary>
        /// 断言 ServiceResult 失败并包含特定错误类型
        /// </summary>
        public static void AssertFailureWithErrorType<T>(ServiceResult<T> result, string errorType, string message = null)
        {
            AssertFailure(result, message);
            // 可以根据项目的错误处理机制来检查错误类型
            // 例如检查 result.ErrorCode 或其他错误标识
        }

        #endregion

        #region 分页结果断言

        /// <summary>
        /// 断言分页结果
        /// </summary>
        public static void AssertPagedResult<T>(PagedResult<T> result, int expectedTotal, int expectedPage, int expectedPageSize, int expectedItemCount = -1)
        {
            Assert.NotNull(result);
            Assert.Equal(expectedTotal, result.Total);
            Assert.Equal(expectedPage, result.Page);
            Assert.Equal(expectedPageSize, result.PageSize);
            Assert.NotNull(result.Items);
            
            if (expectedItemCount >= 0)
            {
                Assert.Equal(expectedItemCount, result.Items.Count());
            }
        }

        /// <summary>
        /// 断言分页结果为空
        /// </summary>
        public static void AssertEmptyPagedResult<T>(PagedResult<T> result, int page = 1, int pageSize = 10)
        {
            AssertPagedResult(result, 0, page, pageSize, 0);
        }

        #endregion

        #region 集合断言辅助

        /// <summary>
        /// 断言集合包含指定数量的元素
        /// </summary>
        public static void AssertCollectionCount<T>(IEnumerable<T> collection, int expectedCount, string message = null)
        {
            Assert.NotNull(collection);
            var actualCount = collection.Count();
            Assert.Equal(expectedCount, actualCount);
        }

        /// <summary>
        /// 断言集合不为空
        /// </summary>
        public static void AssertCollectionNotEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            Assert.NotNull(collection);
            Assert.True(collection.Any(), message ?? "集合不应该为空");
        }

        /// <summary>
        /// 断言集合为空
        /// </summary>
        public static void AssertCollectionEmpty<T>(IEnumerable<T> collection, string message = null)
        {
            Assert.NotNull(collection);
            Assert.False(collection.Any(), message ?? "集合应该为空");
        }

        /// <summary>
        /// 断言集合包含满足条件的元素
        /// </summary>
        public static void AssertCollectionContains<T>(IEnumerable<T> collection, Expression<Func<T, bool>> predicate, string message = null)
        {
            Assert.NotNull(collection);
            var compiledPredicate = predicate.Compile();
            Assert.True(collection.Any(compiledPredicate), message ?? $"集合应该包含满足条件的元素: {predicate}");
        }

        /// <summary>
        /// 断言集合所有元素都满足条件
        /// </summary>
        public static void AssertCollectionAll<T>(IEnumerable<T> collection, Expression<Func<T, bool>> predicate, string message = null)
        {
            Assert.NotNull(collection);
            var compiledPredicate = predicate.Compile();
            Assert.True(collection.All(compiledPredicate), message ?? $"集合中所有元素都应该满足条件: {predicate}");
        }

        #endregion

        #region 字符串断言辅助

        /// <summary>
        /// 断言字符串不为空或null
        /// </summary>
        public static void AssertStringNotEmpty(string value, string message = null)
        {
            Assert.False(string.IsNullOrEmpty(value), message ?? "字符串不应该为空");
        }

        /// <summary>
        /// 断言字符串不为空白
        /// </summary>
        public static void AssertStringNotWhiteSpace(string value, string message = null)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), message ?? "字符串不应该为空白");
        }

        /// <summary>
        /// 断言字符串长度在指定范围内
        /// </summary>
        public static void AssertStringLength(string value, int minLength, int maxLength, string message = null)
        {
            Assert.NotNull(value);
            Assert.True(value.Length >= minLength && value.Length <= maxLength, 
                message ?? $"字符串长度应该在 {minLength} 到 {maxLength} 之间，实际长度: {value.Length}");
        }

        #endregion

        #region GUID 断言辅助

        /// <summary>
        /// 断言 GUID 不为空
        /// </summary>
        public static void AssertGuidNotEmpty(Guid value, string message = null)
        {
            Assert.NotEqual(Guid.Empty, value);
        }

        /// <summary>
        /// 断言 GUID 列表中所有值都不为空
        /// </summary>
        public static void AssertGuidsNotEmpty(IEnumerable<Guid> values, string message = null)
        {
            Assert.NotNull(values);
            foreach (var guid in values)
            {
                AssertGuidNotEmpty(guid, message);
            }
        }

        #endregion

        #region 枚举断言辅助

        /// <summary>
        /// 断言枚举值有效
        /// </summary>
        public static void AssertEnumValid<TEnum>(TEnum value, string message = null)
            where TEnum : Enum
        {
            Assert.True(Enum.IsDefined(typeof(TEnum), value), 
                message ?? $"枚举值 {value} 在类型 {typeof(TEnum).Name} 中无效");
        }

        /// <summary>
        /// 断言枚举值在指定集合中
        /// </summary>
        public static void AssertEnumInSet<TEnum>(TEnum value, IEnumerable<TEnum> validValues, string message = null)
            where TEnum : Enum
        {
            Assert.Contains(value, validValues);
        }

        #endregion

        #region 时间断言辅助

        /// <summary>
        /// 断言时间在指定范围内
        /// </summary>
        public static void AssertDateTimeInRange(DateTime value, DateTime minValue, DateTime maxValue, string message = null)
        {
            Assert.True(value >= minValue && value <= maxValue,
                message ?? $"时间 {value:yyyy-MM-dd HH:mm:ss} 应该在 {minValue:yyyy-MM-dd HH:mm:ss} 到 {maxValue:yyyy-MM-dd HH:mm:ss} 之间");
        }

        /// <summary>
        /// 断言时间接近当前时间（在指定容差范围内）
        /// </summary>
        public static void AssertDateTimeNear(DateTime value, DateTime expected, TimeSpan tolerance, string message = null)
        {
            var difference = Math.Abs((value - expected).TotalMilliseconds);
            var toleranceMs = Math.Abs(tolerance.TotalMilliseconds);
            
            Assert.True(difference <= toleranceMs,
                message ?? $"时间差异 {difference}ms 超过了容差范围 {toleranceMs}ms");
        }

        /// <summary>
        /// 断言时间接近当前时间（默认1秒容差）
        /// </summary>
        public static void AssertDateTimeNearNow(DateTime value, string message = null)
        {
            AssertDateTimeNear(value, DateTime.Now, TimeSpan.FromSeconds(1), message);
        }

        #endregion

        #region 测试数据生成辅助

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        public static string GenerateRandomString(int length = 10, bool includeNumbers = true, bool includeSpecialChars = false)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            if (includeNumbers) chars += "0123456789";
            if (includeSpecialChars) chars += "!@#$%^&*()_+-=[]{}|;:,.<>?";

            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// 生成随机 GUID 列表
        /// </summary>
        public static List<Guid> GenerateRandomGuids(int count)
        {
            return Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
        }

        /// <summary>
        /// 生成边界测试值
        /// </summary>
        public static IEnumerable<T> GenerateBoundaryValues<T>()
        {
            var type = typeof(T);
            
            if (type == typeof(int))
            {
                yield return (T)(object)int.MinValue;
                yield return (T)(object)-1;
                yield return (T)(object)0;
                yield return (T)(object)1;
                yield return (T)(object)int.MaxValue;
            }
            else if (type == typeof(string))
            {
                yield return (T)(object)null;
                yield return (T)(object)"";
                yield return (T)(object)" ";
                yield return (T)(object)"a";
                yield return (T)(object)new string('a', 255);
            }
            // 可以添加更多类型的边界值生成
        }

        #endregion

        #region 性能测试辅助

        /// <summary>
        /// 测量操作执行时间
        /// </summary>
        public static async Task<TimeSpan> MeasureAsync(Func<Task> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await operation();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// 测量操作执行时间（带返回值）
        /// </summary>
        public static async Task<(T Result, TimeSpan Duration)> MeasureAsync<T>(Func<Task<T>> operation)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }

        /// <summary>
        /// 断言操作在指定时间内完成
        /// </summary>
        public static void AssertPerformance(TimeSpan actualTime, TimeSpan expectedMaxTime, string operationName = "操作")
        {
            Assert.True(actualTime <= expectedMaxTime,
                $"{operationName}执行时间({actualTime.TotalMilliseconds:F2}ms)超过了预期的最大时间({expectedMaxTime.TotalMilliseconds:F2}ms)");
        }

        #endregion

        #region 模拟数据辅助

        /// <summary>
        /// 创建模拟的分页查询参数
        /// </summary>
        public static PagedQueryDto CreateMockPagedQuery(int page = 1, int pageSize = 10, string keyword = null)
        {
            return new PagedQueryDto
            {
                Page = page,
                PageSize = pageSize,
                Keyword = keyword
            };
        }

        /// <summary>
        /// 创建测试用的状态列表
        /// </summary>
        public static List<CommonStatus> GetTestStatuses()
        {
            return Enum.GetValues<CommonStatus>().ToList();
        }

        #endregion
    }
}