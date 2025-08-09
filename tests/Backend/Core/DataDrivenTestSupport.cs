using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace LYBT.Tests.Backend.Core
{
    /// <summary>
    /// 数据驱动测试支持类 - 提供自定义测试数据源特性
    /// </summary>

    /// <summary>
    /// 测试数据源特性 - 从静态方法获取测试数据
    /// </summary>
    public class TestDataSourceAttribute : DataAttribute
    {
        private readonly string _methodName;
        private readonly Type? _sourceType;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="methodName">提供测试数据的静态方法名</param>
        /// <param name="sourceType">包含静态方法的类型，null表示使用测试类本身</param>
        public TestDataSourceAttribute(string methodName, Type? sourceType = null)
        {
            _methodName = methodName;
            _sourceType = sourceType;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var type = _sourceType ?? testMethod.DeclaringType;
            if (type == null)
                throw new InvalidOperationException("无法确定测试数据源类型");

            var method = type.GetMethod(_methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException($"未找到静态方法: {_methodName}");

            var result = method.Invoke(null, null);
            if (result is IEnumerable<object[]> enumerable)
                return enumerable;

            throw new InvalidOperationException($"方法 {_methodName} 必须返回 IEnumerable<object[]>");
        }
    }

    /// <summary>
    /// 边界值测试特性 - 自动生成边界值测试数据
    /// </summary>
    public class BoundaryTestAttribute : DataAttribute
    {
        private readonly Type _dataType;

        public BoundaryTestAttribute(Type dataType)
        {
            _dataType = dataType;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return TestDataFactory.GetBoundaryTestData<object>().Where(data => 
                data.Length > 0 && (data[0]?.GetType() == _dataType || (_dataType.IsAssignableFrom(data[0]?.GetType() ?? typeof(object)))));
        }
    }

    /// <summary>
    /// 分页测试特性 - 生成分页参数测试数据
    /// </summary>
    public class PaginationTestAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return TestDataFactory.GetPaginationTestData();
        }
    }

    /// <summary>
    /// GUID测试特性
    /// </summary>
    public class GuidTestAttribute : DataAttribute
    {
        private readonly bool _validOnly;

        public GuidTestAttribute(bool validOnly = true)
        {
            _validOnly = validOnly;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (_validOnly)
                return TestDataFactory.GetValidGuidTestData();
            
            return TestDataFactory.GetInvalidGuidTestData().Concat(TestDataFactory.GetValidGuidTestData());
        }
    }

    /// <summary>
    /// 密码复杂度测试特性
    /// </summary>
    public class PasswordComplexityTestAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return TestDataFactory.GetPasswordComplexityTestData();
        }
    }

    /// <summary>
    /// 测试类别特性 - 用于标记不同类型的测试
    /// </summary>
    public class TestCategoryAttribute : Attribute
    {
        public string Category { get; }

        public TestCategoryAttribute(string category)
        {
            Category = category;
        }
    }

    /// <summary>
    /// 测试类别常量
    /// </summary>
    public static class TestCategories
    {
        public const string Unit = "Unit";
        public const string Integration = "Integration";
        public const string Repository = "Repository";
        public const string Service = "Service";
        public const string Controller = "Controller";
        public const string Performance = "Performance";
        public const string BoundaryValue = "BoundaryValue";
        public const string DataDriven = "DataDriven";
        public const string ExceptionHandling = "ExceptionHandling";
    }

    /// <summary>
    /// 测试扩展方法
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// 断言异常类型和消息
        /// </summary>
        public static T ShouldThrow<T>(this Action action, string? expectedMessage = null) where T : Exception
        {
            var exception = Assert.Throws<T>(action);
            
            if (!string.IsNullOrEmpty(expectedMessage))
            {
                Assert.Contains(expectedMessage, exception.Message);
            }
            
            return exception;
        }

        /// <summary>
        /// 断言异步异常类型和消息
        /// </summary>
        public static async Task<T> ShouldThrowAsync<T>(this Task action, string? expectedMessage = null) where T : Exception
        {
            var exception = await Assert.ThrowsAsync<T>(async () => await action);
            
            if (!string.IsNullOrEmpty(expectedMessage))
            {
                Assert.Contains(expectedMessage, exception.Message);
            }
            
            return exception;
        }

        /// <summary>
        /// 断言集合不为空且包含指定数量的元素
        /// </summary>
        public static IEnumerable<T> ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount)
        {
            Assert.NotNull(collection);
            Assert.Equal(expectedCount, collection.Count());
            return collection;
        }

        /// <summary>
        /// 断言字符串不为空且包含指定内容
        /// </summary>
        public static string ShouldContain(this string actual, string expectedSubstring)
        {
            Assert.NotNull(actual);
            Assert.Contains(expectedSubstring, actual);
            return actual;
        }

        /// <summary>
        /// 断言对象不为null且满足条件
        /// </summary>
        public static T ShouldSatisfy<T>(this T obj, Func<T, bool> condition, string? message = null) where T : class
        {
            Assert.NotNull(obj);
            Assert.True(condition(obj), message ?? "对象不满足指定条件");
            return obj;
        }

        /// <summary>
        /// 断言GUID不为空
        /// </summary>
        public static Guid ShouldNotBeEmpty(this Guid guid)
        {
            Assert.NotEqual(Guid.Empty, guid);
            return guid;
        }

        /// <summary>
        /// 断言日期时间在指定范围内
        /// </summary>
        public static DateTime ShouldBeWithin(this DateTime actual, TimeSpan tolerance, DateTime? expected = null)
        {
            var expectedTime = expected ?? DateTime.UtcNow;
            var difference = Math.Abs((actual - expectedTime).TotalMilliseconds);
            Assert.True(difference <= tolerance.TotalMilliseconds, 
                $"时间差异 {difference}ms 超出容忍范围 {tolerance.TotalMilliseconds}ms");
            return actual;
        }
    }

    /// <summary>
    /// 测试数据构建器 - 链式构建测试数据
    /// </summary>
    public class TestDataBuilder<T> where T : class, new()
    {
        private readonly T _instance;
        private readonly Dictionary<string, object> _properties;

        public TestDataBuilder()
        {
            _instance = new T();
            _properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        public TestDataBuilder<T> With<TProperty>(string propertyName, TProperty value)
        {
            _properties[propertyName] = value!;
            return this;
        }

        /// <summary>
        /// 构建最终对象
        /// </summary>
        public T Build()
        {
            var type = typeof(T);
            foreach (var kvp in _properties)
            {
                var property = type.GetProperty(kvp.Key);
                property?.SetValue(_instance, kvp.Value);
            }
            
            return _instance;
        }

        /// <summary>
        /// 构建多个对象
        /// </summary>
        public List<T> Build(int count)
        {
            var result = new List<T>();
            for (int i = 0; i < count; i++)
            {
                result.Add(Build());
            }
            return result;
        }
    }
}