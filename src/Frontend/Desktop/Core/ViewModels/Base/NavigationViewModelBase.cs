using System;
using System.Collections.Generic;
using Prism.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Core.ViewModels.Base
{
    /// <summary>
    /// 导航ViewModel基类
    /// 提供页面导航、参数传递、导航状态管理等功能（简化版，不依赖Prism.Regions）
    /// </summary>
    public abstract class NavigationViewModelBase : ServiceViewModel
    {
        private Dictionary<string, object> _navigationParameters = new();
        private bool _isNavigationTarget = true;

        /// <summary>
        /// 导航参数
        /// </summary>
        protected Dictionary<string, object> NavigationParameters => _navigationParameters;

        /// <summary>
        /// 是否为导航目标
        /// </summary>
        protected bool IsNavigationTargetFlag
        {
            get => _isNavigationTarget;
            set => SetProperty(ref _isNavigationTarget, value);
        }

        public NavigationViewModelBase(
            IEventAggregator eventAggregator, 
            IErrorHandlingService errorHandlingService) 
            : base(eventAggregator, errorHandlingService)
        {
            IsNavigationTargetFlag = true;
        }

        public NavigationViewModelBase(IEventAggregator eventAggregator) 
            : base(eventAggregator)
        {
            IsNavigationTargetFlag = true;
        }

        /// <summary>
        /// 导航到此视图时调用（简化版）
        /// </summary>
        public virtual void OnNavigatedTo(Dictionary<string, object>? parameters = null)
        {
            try
            {
                // 保存导航参数
                _navigationParameters.Clear();
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        _navigationParameters[parameter.Key] = parameter.Value;
                    }
                }

                // 异步初始化
                _ = InitializeAsync();

                // 调用子类实现
                OnNavigatedToOverride(parameters);
            }
            catch (Exception ex)
            {
                HandleError("导航到页面", ex);
            }
        }

        /// <summary>
        /// 从此视图导航离开时调用
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
            try
            {
                OnNavigatedFromOverride();
            }
            catch (Exception ex)
            {
                HandleError("导航离开页面", ex);
            }
        }

        /// <summary>
        /// 判断是否为导航目标
        /// </summary>
        public virtual bool IsNavigationTarget()
        {
            return IsNavigationTargetFlag;
        }

        /// <summary>
        /// 确认导航请求
        /// </summary>
        public virtual bool ConfirmNavigationRequest()
        {
            try
            {
                var canNavigate = CanNavigateAway();
                if (canNavigate)
                {
                    return true;
                }
                else
                {
                    return ShowConfirmNavigationDialog();
                }
            }
            catch (Exception ex)
            {
                HandleError("确认导航", ex);
                return false;
            }
        }

        /// <summary>
        /// 子类重写此方法处理导航到页面的逻辑
        /// </summary>
        protected virtual void OnNavigatedToOverride(Dictionary<string, object>? parameters)
        {
            // 子类可以重写此方法
        }

        /// <summary>
        /// 子类重写此方法处理导航离开页面的逻辑
        /// </summary>
        protected virtual void OnNavigatedFromOverride()
        {
            // 子类可以重写此方法
        }

        /// <summary>
        /// 判断是否可以导航离开
        /// </summary>
        protected virtual bool CanNavigateAway()
        {
            // 如果有错误或正在加载，可能需要用户确认
            return !IsLoading && !HasError;
        }

        /// <summary>
        /// 显示导航确认对话框
        /// </summary>
        protected virtual bool ShowConfirmNavigationDialog()
        {
            return ShowConfirmDialog(
                "当前页面有未保存的更改或正在进行的操作，确定要离开吗？", 
                "确认导航");
        }

        /// <summary>
        /// 获取导航参数
        /// </summary>
        protected T? GetNavigationParameter<T>(string key, T? defaultValue = default)
        {
            if (_navigationParameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected bool ShowConfirmDialog(string message, string title = "确认")
        {
            var result = System.Windows.MessageBox.Show(
                message, title, 
                System.Windows.MessageBoxButton.YesNo, 
                System.Windows.MessageBoxImage.Question);
            return result == System.Windows.MessageBoxResult.Yes;
        }
    }
}