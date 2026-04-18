using System;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Navigation
{
    /// <summary>
    /// Navigation Shortcuts Manager - Phase 2.1: Navigation Improvements
    /// 管理键盘导航快捷键
    /// </summary>
    public class NavigationShortcutsManager
    {
        private readonly IEnhancedNavigationService _navigationService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public NavigationShortcutsManager(IEnhancedNavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        /// <summary>
        /// 注册所有键盘快捷键
        /// </summary>
        public void RegisterShortcuts(InputBindingCollection inputBindings)
        {
            if (inputBindings == null)
                throw new ArgumentNullException(nameof(inputBindings));

            // Alt+H: Navigate to Home
            inputBindings.Add(new KeyBinding(
                new DelegateCommand(async () => await _navigationService.NavigateHomeAsync()),
                Key.H,
                ModifierKeys.Alt
            )
            {
                CommandParameter = "Home"
            });

            // Alt+Left: Go Back
            inputBindings.Add(new KeyBinding(
                new DelegateCommand(async () => await _navigationService.GoBackAsync()),
                Key.Left,
                ModifierKeys.Alt
            )
            {
                CommandParameter = "Back"
            });

            // Alt+Right: Go Forward
            inputBindings.Add(new KeyBinding(
                new DelegateCommand(async () => await _navigationService.GoForwardAsync()),
                Key.Right,
                ModifierKeys.Alt
            )
            {
                CommandParameter = "Forward"
            });

            // Ctrl+Shift+H: Show Navigation History
            inputBindings.Add(new KeyBinding(
                new DelegateCommand(ExecuteShowHistory),
                Key.H,
                ModifierKeys.Control | ModifierKeys.Shift
            )
            {
                CommandParameter = "ShowHistory"
            });

            // F6: Cycle through regions
            inputBindings.Add(new KeyBinding(
                new DelegateCommand(ExecuteCycleRegions),
                new KeyGesture(Key.F6)
            )
            {
                CommandParameter = "CycleRegions"
            });

            // Ctrl+1..5: Quick switch to recent destinations
            for (int i = 1; i <= 5; i++)
            {
                var index = i;
                inputBindings.Add(new KeyBinding(
                    new DelegateCommand(async () => await ExecuteNavigateToRecent(index)),
                    Key.D1 + (index - 1),
                    ModifierKeys.Control
                )
                {
                    CommandParameter = $"Recent{index}"
                });
            }
        }

        /// <summary>
        /// 显示导航历史
        /// </summary>
        private void ExecuteShowHistory()
        {
            // TODO: Implement show history panel
            // This would typically open or focus the navigation history panel
            // For now, publish an event that the view can subscribe to
        }

        /// <summary>
        /// 在区域间循环
        /// </summary>
        private void ExecuteCycleRegions()
        {
            // TODO: Implement cycle through regions
            // This would move focus between different navigation regions
        }

        /// <summary>
        /// 导航到最近的目的地
        /// </summary>
        private async Task ExecuteNavigateToRecent(int index)
        {
            try
            {
                var recent = _navigationService.History.Skip(index - 1).FirstOrDefault();
                if (recent != null)
                {
                    await _navigationService.NavigateAsync(recent.Uri, recent.Parameters);
                }
            }
            catch
            {
                // Ignore if recent not available
            }
        }
    }

    /// <summary>
    /// Navigation Command Factory - Phase 2.1: Navigation Improvements
    /// 创建导航相关的命令
    /// </summary>
    public static class NavigationCommandFactory
    {
        /// <summary>
        /// 创建导航命令
        /// </summary>
        public static ICommand CreateNavigateCommand(
            IEnhancedNavigationService navigationService,
            string uri)
        {
            return new DelegateCommand(async () =>
            {
                await navigationService.NavigateAsync(uri);
            });
        }

        /// <summary>
        /// 创建返回命令
        /// </summary>
        public static ICommand CreateGoBackCommand(
            IEnhancedNavigationService navigationService)
        {
            return new DelegateCommand(async () =>
            {
                await navigationService.GoBackAsync();
            }, () => navigationService.CanGoBack);
        }

        /// <summary>
        /// 创建前进命令
        /// </summary>
        public static ICommand CreateGoForwardCommand(
            IEnhancedNavigationService navigationService)
        {
            return new DelegateCommand(async () =>
            {
                await navigationService.GoForwardAsync();
            }, () => navigationService.CanGoForward);
        }

        /// <summary>
        /// 创建主页命令
        /// </summary>
        public static ICommand CreateNavigateHomeCommand(
            IEnhancedNavigationService navigationService)
        {
            return new DelegateCommand(async () =>
            {
                await navigationService.NavigateHomeAsync();
            });
        }
    }

    /// <summary>
    /// Navigation Keyboard Behaviors - Phase 2.1: Navigation Improvements
    /// 附加到控件以启用键盘导航行为
    /// </summary>
    public static class NavigationKeyboardBehaviors
    {
        /// <summary>
        /// 为控件附加键盘导航行为
        /// </summary>
        public static readonly DependencyProperty IsNavigationTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsNavigationTarget",
                typeof(bool),
                typeof(NavigationKeyboardBehaviors),
                new PropertyMetadata(false, OnIsNavigationTargetChanged)
            );

        public static bool GetIsNavigationTarget(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNavigationTargetProperty);
        }

        public static void SetIsNavigationTarget(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNavigationTargetProperty, value);
        }

        private static void OnIsNavigationTargetChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is bool isTarget && isTarget)
            {
                element.Loaded += (s, args) =>
                {
                    element.Focusable = true;
                    element.Focus();
                };
            }
        }
    }

    /// <summary>
    /// Extended NavigationResult - Phase 2.1: Navigation Improvements
    /// 扩展的导航结果，包含更多上下文信息
    /// </summary>
    public class ExtendedNavigationResult
    {
        public bool Result { get; }
        public string? ErrorMessage { get; }

        public ExtendedNavigationResult(bool result, string? errorMessage = null)
        {
            Result = result;
            ErrorMessage = errorMessage;
        }
    }
}
