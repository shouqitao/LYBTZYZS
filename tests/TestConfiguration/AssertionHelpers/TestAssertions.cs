using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Tests.Common.AssertionHelpers
{
    /// <summary>
    /// 测试断言辅助类 - 提供常用的断言方法
    /// 统一断言模式，提高测试代码可读性和维护性
    /// </summary>
    public static class TestAssertions
    {
        /// <summary>
        /// 验证HTTP响应状态码
        /// </summary>
        public static void ShouldHaveStatusCode(this HttpResponseMessage response, HttpStatusCode expectedStatusCode)
        {
            response.StatusCode.Should().Be(expectedStatusCode);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (OK)
        /// </summary>
        public static void ShouldBeOk(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.OK);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (Created)
        /// </summary>
        public static void ShouldBeCreated(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.Created);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (NoContent)
        /// </summary>
        public static void ShouldBeNoContent(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.NoContent);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (BadRequest)
        /// </summary>
        public static void ShouldBeBadRequest(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (Unauthorized)
        /// </summary>
        public static void ShouldBeUnauthorized(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (Forbidden)
        /// </summary>
        public static void ShouldBeForbidden(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (NotFound)
        /// </summary>
        public static void ShouldBeNotFound(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// 验证HTTP响应状态码 (InternalServerError)
        /// </summary>
        public static void ShouldBeInternalServerError(this HttpResponseMessage response)
        {
            response.ShouldHaveStatusCode(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// 验证响应内容不为空
        /// </summary>
        public static async Task ShouldHaveContentAsync(this HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// 验证响应内容为空
        /// </summary>
        public static async Task ShouldNotHaveContentAsync(this HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().BeNullOrEmpty();
        }

        /// <summary>
        /// 验证响应内容包含指定文本
        /// </summary>
        public static async Task ShouldContainTextAsync(this HttpResponseMessage response, string expectedText)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain(expectedText);
        }

        /// <summary>
        /// 验证响应内容不包含指定文本
        /// </summary>
        public static async Task ShouldNotContainTextAsync(this HttpResponseMessage response, string unexpectedText)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain(unexpectedText);
        }

        /// <summary>
        /// 验证响应内容可以反序列化为指定类型
        /// </summary>
        public static async Task<T> ShouldBeJsonAsync<T>(this HttpResponseMessage response)
        {
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            var content = await response.Content.ReadAsStringAsync();

            // Issue #1669: 使用System.Net.Http.Json扩展方法，自动配置JSON序列化选项
            var result = await response.Content.ReadFromJsonAsync<T>();

            result.Should().NotBeNull();
            return result!;
        }

        /// <summary>
        /// 验证响应内容为有效的API响应结构
        /// </summary>
        public static async Task<ApiResponse<T>> ShouldBeValidApiResponseAsync<T>(this HttpResponseMessage response)
        {
            var apiResponse = await response.ShouldBeJsonAsync<ApiResponse<T>>();

            apiResponse.Should().NotBeNull();
            return apiResponse;
        }

        /// <summary>
        /// 验证API响应成功
        /// </summary>
        public static async Task<ApiResponse<T>> ShouldBeSuccessfulApiResponseAsync<T>(this HttpResponseMessage response)
        {
            var apiResponse = await response.ShouldBeValidApiResponseAsync<T>();
            apiResponse.Success.Should().BeTrue();
            return apiResponse;
        }

        /// <summary>
        /// 验证API响应失败
        /// </summary>
        public static async Task<ApiResponse> ShouldBeFailedApiResponseAsync(this HttpResponseMessage response)
        {
            var apiResponse = await response.ShouldBeJsonAsync<ApiResponse>();
            apiResponse.Success.Should().BeFalse();
            return apiResponse;
        }

        /// <summary>
        /// 验证API响应失败且包含消息
        /// </summary>
        public static async Task<ApiResponse> ShouldBeFailedApiResponseWithMessageAsync(this HttpResponseMessage response)
        {
            var apiResponse = await response.ShouldBeFailedApiResponseAsync();
            apiResponse.Message.Should().NotBeNullOrEmpty();
            return apiResponse;
        }

        /// <summary>
        /// 验证MVC操作结果为成功响应
        /// </summary>
        public static void ShouldBeSuccessResponse<T>(this IActionResult result)
        {
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeAssignableTo<T>();
        }


        /// <summary>
        /// 验证MVC操作结果为无内容响应
        /// </summary>
        public static void ShouldBeNoContentResponse(this IActionResult result)
        {
            result.Should().BeOfType<NoContentResult>();
        }

        /// <summary>
        /// 验证MVC操作结果为错误响应
        /// </summary>
        public static void ShouldBeErrorResponse(this IActionResult result)
        {
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().BeInRange(400, 599);
        }

        /// <summary>
        /// 验证MVC操作结果为BadRequest响应
        /// </summary>
        public static void ShouldBeBadRequestResponse(this IActionResult result)
        {
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        /// 验证MVC操作结果为NotFound响应
        /// </summary>
        public static void ShouldBeNotFoundResponse(this IActionResult result)
        {
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        /// 验证集合不为空
        /// </summary>
        public static void ShouldNotBeEmpty<T>(this IEnumerable<T> collection)
        {
            collection.Should().NotBeNull();
            collection.Should().NotBeEmpty();
        }

        /// <summary>
        /// 验证集合为空
        /// </summary>
        public static void ShouldBeEmpty<T>(this IEnumerable<T> collection)
        {
            collection.Should().NotBeNull();
            collection.Should().BeEmpty();
        }

        /// <summary>
        /// 验证集合包含指定数量的元素
        /// </summary>
        public static void ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount)
        {
            collection.Should().NotBeNull();
            collection.Should().HaveCount(expectedCount);
        }

        /// <summary>
        /// 验证集合包含指定元素
        /// </summary>
        public static void ShouldContain<T>(this IEnumerable<T> collection, T expectedItem)
        {
            collection.Should().NotBeNull();
            collection.Should().Contain(expectedItem);
        }

        /// <summary>
        /// 验证集合不包含指定元素
        /// </summary>
        public static void ShouldNotContain<T>(this IEnumerable<T> collection, T unexpectedItem)
        {
            collection.Should().NotBeNull();
            collection.Should().NotContain(unexpectedItem);
        }

        /// <summary>
        /// 验证字符串不为空或空白
        /// </summary>
        public static void ShouldNotBeNullOrEmpty(this string value)
        {
            value.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        /// 验证字符串为空或空白
        /// </summary>
        public static void ShouldBeNullOrEmpty(this string value)
        {
            value.Should().BeNullOrEmpty();
        }

        /// <summary>
        /// 验证日期时间接近指定时间（在指定秒数范围内）
        /// </summary>
        public static void ShouldBeCloseTo(this DateTime actual, DateTime expected, TimeSpan tolerance)
        {
            var difference = Math.Abs((actual - expected).TotalSeconds);
            difference.Should().BeLessOrEqualTo(tolerance.TotalSeconds);
        }

        /// <summary>
        /// 验证日期时间接近指定时间（默认5秒范围内）
        /// </summary>
        public static void ShouldBeCloseTo(this DateTime actual, DateTime expected)
        {
            actual.ShouldBeCloseTo(expected, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// 验证数值在指定范围内
        /// </summary>
        public static void ShouldBeInRange<T>(this T value, T minimum, T maximum) where T : IComparable<T>
        {
            value.Should().BeGreaterOrEqualTo(minimum);
            value.Should().BeLessOrEqualTo(maximum);
        }

        /// <summary>
        /// 验证Guid不为空
        /// </summary>
        public static void ShouldNotBeEmpty(this Guid guid)
        {
            guid.Should().NotBe(Guid.Empty);
        }

        /// <summary>
        /// 验证Guid为空
        /// </summary>
        public static void ShouldBeEmpty(this Guid guid)
        {
            guid.Should().Be(Guid.Empty);
        }
    }

    /// <summary>
    /// API响应模型
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// 泛型API响应模型
    /// </summary>
    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }
    }
}
