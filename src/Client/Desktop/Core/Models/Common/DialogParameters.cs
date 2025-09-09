namespace LYBT.Desktop.Core.Models.Common
{

    /// <summary>
    /// 对话框参数集合
    /// 替代 Prism DialogParameters，兼容 Prism 8.1.97
    /// </summary>
    public class DialogParameters : Dictionary<string, object>
    {

        /// <summary>
        /// 获取强类型参数值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <returns>参数值</returns>
        public T GetValue<T>(string key)
        {
            if (TryGetValue(key, out var value) && value is T result)
            {
                return result;
            }

            return default(T)!;
        }

        /// <summary>
        /// 获取强类型参数值，带默认值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值或默认值</returns>
        public T GetValue<T>(string key, T defaultValue)
        {
            if (TryGetValue(key, out var value) && value is T result)
            {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// 尝试获取强类型参数值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <param name="value">输出参数值</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (TryGetValue(key, out var objValue) && objValue is T result)
            {
                value = result;
                return true;
            }

            value = default(T)!;
            return false;
        }

        /// <summary>
        /// 检查是否包含指定类型的参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <returns>是否包含指定类型的参数</returns>
        public bool ContainsKey<T>(string key)
        {
            return TryGetValue(key, out var value) && value is T;
        }
    }
}
