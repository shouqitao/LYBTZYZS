using System.Text.Json;

namespace LYBT.Desktop.Models.Mappers
{
    /// <summary>
    /// 简化的对象映射器 - 遵循"适度设计、拒绝过度工程"原则
    /// 提供基本的对象映射功能，避免引入重型映射框架
    /// </summary>
    public static class SimpleMapper
    {
        /// <summary>
        /// 将源对象映射到目标类型
        /// 使用JSON序列化/反序列化进行深拷贝映射
        /// </summary>
        public static TTarget? Map<TSource, TTarget>(TSource? source)
            where TSource : class
            where TTarget : class
        {
            if (source == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(source);
                return JsonSerializer.Deserialize<TTarget>(json);
            }
            catch (JsonException)
            {
                // 如果JSON映射失败，返回null
                return null;
            }
        }

        /// <summary>
        /// 将源对象列表映射到目标类型列表
        /// </summary>
        public static List<TTarget> MapList<TSource, TTarget>(IEnumerable<TSource>? source)
            where TSource : class
            where TTarget : class
        {
            if (source == null) return new List<TTarget>();

            var result = new List<TTarget>();
            foreach (var item in source)
            {
                var mapped = Map<TSource, TTarget>(item);
                if (mapped != null)
                {
                    result.Add(mapped);
                }
            }
            return result;
        }

        /// <summary>
        /// 将源对象的属性复制到目标对象
        /// 仅复制同名且类型相同的属性
        /// </summary>
        public static void CopyProperties<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class
        {
            if (source == null || target == null) return;

            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            var sourceProperties = sourceType.GetProperties();
            var targetProperties = targetType.GetProperties();

            foreach (var sourceProp in sourceProperties)
            {
                if (!sourceProp.CanRead) continue;

                var targetProp = targetProperties.FirstOrDefault(p =>
                    p.Name == sourceProp.Name &&
                    p.PropertyType == sourceProp.PropertyType &&
                    p.CanWrite);

                if (targetProp != null)
                {
                    var value = sourceProp.GetValue(source);
                    targetProp.SetValue(target, value);
                }
            }
        }

        /// <summary>
        /// 创建对象的深拷贝
        /// </summary>
        public static T? Clone<T>(T? source) where T : class
        {
            return Map<T, T>(source);
        }
    }
}
