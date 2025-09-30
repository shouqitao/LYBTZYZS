using System.Windows;
using Prism.Ioc;

namespace LYBT.Desktop.Infrastructure.Extensions
{

    /// <summary>
    /// 对话框注册扩展方法
    /// 简化模块中的对话框注册过程
    /// </summary>
    public static class DialogRegistrationExtensions
    {

        /// <summary>
        /// 注册自定义对话框（泛型版本）
        /// </summary>
        /// <typeparam name="TWindow">对话框窗口类型</typeparam>
        /// <param name="containerRegistry">容器注册器</param>
        /// <param name="dialogName">对话框名称</param>
        public static void RegisterCustomDialog<TWindow>(this IContainerRegistry containerRegistry, string dialogName)
            where TWindow : Window
        {
            // 只注册窗口到容器，对话框注册由 WpfDialogService 在初始化时处理
            containerRegistry.Register<TWindow>();
        }

        /// <summary>
        /// 注册自定义对话框（类型版本）
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <param name="dialogName">对话框名称</param>
        /// <param name="windowType">对话框窗口类型</param>
        public static void RegisterCustomDialog(this IContainerRegistry containerRegistry, string dialogName, Type windowType)
        {
            if (!typeof(Window).IsAssignableFrom(windowType))
            {
                throw new ArgumentException("对话框类型必须继承自 Window", nameof(windowType));
            }

            // 只注册窗口到容器，对话框注册由 WpfDialogService 在初始化时处理
            containerRegistry.Register(windowType);
        }

        /// <summary>
        /// 批量注册对话框
        /// </summary>
        /// <param name="containerRegistry">容器注册器</param>
        /// <param name="dialogRegistrations">对话框注册信息</param>
        public static void RegisterCustomDialogs(
            this IContainerRegistry containerRegistry,
            params (string DialogName, Type WindowType)[] dialogRegistrations)
        {
            foreach (var (dialogName, windowType) in dialogRegistrations)
            {
                containerRegistry.RegisterCustomDialog(dialogName, windowType);
            }
        }
    }
}
