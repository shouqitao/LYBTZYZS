namespace LYBT.SharedKernel.Primitives;

/// <summary>
/// 值对象基类。不可变，无身份标识，通过属性值相等性比较。
/// record类型自动提供基于属性的相等性比较，无需额外实现。
/// </summary>
public abstract record ValueObject;


