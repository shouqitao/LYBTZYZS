using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Models.Mapping;

/// <summary>
/// 映射服务接口 - 简化版本
/// </summary>
public interface IMappingService
{
    /// <summary>
    /// 映射对象
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source)
        where TDestination : new();

    /// <summary>
    /// 映射对象集合
    /// </summary>
    List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
        where TDestination : new();

    /// <summary>
    /// 深拷贝对象
    /// </summary>
    T Clone<T>(T source) where T : new();
}

/// <summary>
/// 映射服务实现 - 基于反射的简化版本，避免AutoMapper的复杂性
/// </summary>
public class MappingService : IMappingService
{
    private readonly ILogger<MappingService> _logger;

    public MappingService(ILogger<MappingService> logger)
    {
        _logger = logger;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
        where TDestination : new()
    {
        if (source == null)
            return new TDestination();

        try
        {
            var destination = new TDestination();
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var sourceProperties = sourceType.GetProperties()
                .Where(p => p.CanRead)
                .ToDictionary(p => p.Name, p => p);

            var destinationProperties = destinationType.GetProperties()
                .Where(p => p.CanWrite);

            foreach (var destProp in destinationProperties)
            {
                if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
                {
                    if (sourceProp.PropertyType == destProp.PropertyType ||
                        destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                    {
                        var value = sourceProp.GetValue(source);
                        destProp.SetValue(destination, value);
                    }
                }
            }

            return destination;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "映射失败: {SourceType} -> {DestType}",
                typeof(TSource).Name, typeof(TDestination).Name);
            return new TDestination();
        }
    }

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
        where TDestination : new()
    {
        if (source == null)
            return new List<TDestination>();

        try
        {
            return source.Select(Map<TSource, TDestination>).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "映射列表失败: {SourceType} -> {DestType}",
                typeof(TSource).Name, typeof(TDestination).Name);
            return new List<TDestination>();
        }
    }

    public T Clone<T>(T source) where T : new()
    {
        if (source == null)
            return new T();

        try
        {
            // 使用JSON序列化进行深拷贝
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "克隆对象失败: {Type}", typeof(T).Name);
            return new T();
        }
    }
}
