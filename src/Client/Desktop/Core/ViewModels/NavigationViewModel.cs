namespace LYBT.Desktop.Core.ViewModels
{

    /// <summary>
    /// 导航参数类（兼容性）
    /// </summary>
    public class NavigationParameters : Dictionary<string, object>
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationParameters"/> class.
        /// 创建空的导航参数
        /// </summary>
        public NavigationParameters()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationParameters"/> class.
        /// 创建包含单个参数的导航参数
        /// </summary>
        public NavigationParameters(string key, object value)
        {
            Add(key, value);
        }
    }
}
