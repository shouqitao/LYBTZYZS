using System;
using System.Collections.Generic;
using System.Linq;

namespace LYBT.Domain.Common
{
    /// <summary>
    /// 值对象基类 - UltraThink重构DDD架构
    /// 实现值对象的相等性比较和不变性特征
    /// </summary>
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        /// <summary>
        /// 获取用于相等性比较的原子值
        /// </summary>
        /// <returns>原子值集合</returns>
        protected abstract IEnumerable<object> GetEqualityComponents();

        /// <summary>
        /// 相等性比较
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        /// <summary>
        /// 相等性比较（泛型版本）
        /// </summary>
        /// <param name="other">比较对象</param>
        /// <returns>是否相等</returns>
        public bool Equals(ValueObject other)
        {
            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }

            return ReferenceEquals(left, right) || left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }

        /// <summary>
        /// 字符串表示
        /// </summary>
        public override string ToString()
        {
            var values = GetEqualityComponents()
                .Select(x => x?.ToString() ?? "null");
            
            return $"{GetType().Name}({string.Join(", ", values)})";
        }
    }

    /// <summary>
    /// 单值对象基类 - 用于包装单个值的值对象
    /// </summary>
    /// <typeparam name="T">值类型</typeparam>
    public abstract class SingleValueObject<T> : ValueObject
        where T : notnull
    {
        /// <summary>
        /// 值
        /// </summary>
        public T Value { get; protected init; }

        protected SingleValueObject(T value)
        {
            Value = value;
        }

        /// <summary>
        /// 获取相等性比较组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <summary>
        /// 隐式转换为底层值类型
        /// </summary>
        public static implicit operator T(SingleValueObject<T> valueObject)
        {
            if (valueObject == null)
                throw new ArgumentNullException(nameof(valueObject), "Cannot convert null SingleValueObject to its value type");
            
            return valueObject.Value;
        }

        /// <summary>
        /// 字符串表示
        /// </summary>
        public override string ToString()
        {
            return Value?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// 枚举值对象基类 - 用于实现类型安全的枚举
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    public abstract class Enumeration<T> : ValueObject, IComparable<T>
        where T : Enumeration<T>
    {
        /// <summary>
        /// 值
        /// </summary>
        public int Value { get; protected init; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; protected init; }

        protected Enumeration(int value, string name)
        {
            Value = value;
            Name = name;
        }

        /// <summary>
        /// 获取相等性比较组件
        /// </summary>
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        /// <summary>
        /// 字符串表示
        /// </summary>
        public override string ToString() => Name;

        /// <summary>
        /// 获取所有枚举值
        /// </summary>
        public static IEnumerable<T> GetAll()
        {
            var type = typeof(T);
            var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                       System.Reflection.BindingFlags.Static |
                                       System.Reflection.BindingFlags.DeclaredOnly);

            return fields
                .Select(f => f.GetValue(null))
                .Cast<T>();
        }

        /// <summary>
        /// 根据值获取枚举实例
        /// </summary>
        public static T FromValue(int value)
        {
            var matchingItem = GetAll().FirstOrDefault(item => item.Value == value);
            
            if (matchingItem == null)
            {
                throw new InvalidOperationException($"'{value}' is not a valid value for {typeof(T).Name}");
            }

            return matchingItem;
        }

        /// <summary>
        /// 根据名称获取枚举实例
        /// </summary>
        public static T FromName(string name)
        {
            var matchingItem = GetAll()
                .FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

            if (matchingItem == null)
            {
                throw new InvalidOperationException($"'{name}' is not a valid name for {typeof(T).Name}");
            }

            return matchingItem;
        }

        /// <summary>
        /// 比较方法
        /// </summary>
        public int CompareTo(T other)
        {
            return Value.CompareTo(other?.Value);
        }

        /// <summary>
        /// 隐式转换为整数
        /// </summary>
        public static implicit operator int(Enumeration<T> enumeration)
        {
            return enumeration?.Value ?? 0;
        }

        /// <summary>
        /// 隐式转换为字符串
        /// </summary>
        public static implicit operator string(Enumeration<T> enumeration)
        {
            return enumeration?.Name ?? string.Empty;
        }
    }
}