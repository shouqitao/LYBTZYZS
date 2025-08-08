using System;
using System.Threading.Tasks;
using Prism.Events;

namespace LYBT.WPF.Client.Core.ViewModels
{
    /// <summary>
    /// 支持导航的视图模型基类（简化版）
    /// </summary>
    public abstract class NavigationViewModel : BaseViewModel
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventAggregator">事件聚合器</param>
        protected NavigationViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
        }

        #region 导航方法

        /// <summary>
        /// 导航到此视图时调用
        /// </summary>
        /// <param name="parameters">导航参数</param>
        public virtual void OnNavigatedTo(NavigationParameters parameters)
        {
            // 默认实现：触发初始化
            _ = InitializeAsync();
        }

        /// <summary>
        /// 从此视图导航离开时调用
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
            // 子类可以重写此方法进行清理
        }

        /// <summary>
        /// 确定此实例是否可以处理导航请求
        /// </summary>
        /// <returns>如果可以处理返回true，否则返回false</returns>
        public virtual bool IsNavigationTarget()
        {
            // 默认返回true，表示可以重用视图模型实例
            return true;
        }

        #endregion

        #region 导航辅助方法

        /// <summary>
        /// 从导航参数获取值
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="parameters">导航参数</param>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        protected T GetNavigationParameter<T>(NavigationParameters parameters, string key, T defaultValue = default!)
        {
            if (parameters != null && parameters.ContainsKey(key))
            {
                var value = parameters[key];
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // 尝试类型转换
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // 转换失败，返回默认值
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// 检查导航参数是否存在
        /// </summary>
        /// <param name="parameters">导航参数</param>
        /// <param name="key">参数键</param>
        /// <returns>如果存在返回true</returns>
        protected bool HasNavigationParameter(NavigationParameters parameters, string key)
        {
            return parameters != null && parameters.ContainsKey(key);
        }

        #endregion
    }

    /// <summary>
    /// 导航参数类
    /// </summary>
    public class NavigationParameters : System.Collections.Generic.Dictionary<string, object>
    {
        /// <summary>
        /// 创建空的导航参数
        /// </summary>
        public NavigationParameters() { }

        /// <summary>
        /// 创建包含单个参数的导航参数
        /// </summary>
        public NavigationParameters(string key, object value)
        {
            Add(key, value);
        }
    }
}