using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using Prism.Events;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;

namespace LYBT.Desktop.Core.ViewModels
{

    /// <summary>
    /// 导航参数类（兼容性）
    /// </summary>
    public class NavigationParameters : Dictionary<string, object>
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