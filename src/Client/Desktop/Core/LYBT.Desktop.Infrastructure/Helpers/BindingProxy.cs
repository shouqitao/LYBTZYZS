using System.Windows;

namespace LYBT.Desktop.Infrastructure.Helpers
{
    /// <summary>
    /// 绑定代理类 - 用于解决 VisualBrush、DataGridColumn 等不在可视化树中的元素绑定问题
    /// </summary>
    /// <remarks>
    /// 使用方式：
    /// 1. 在Resources中声明: <helpers:BindingProxy x:Key="proxy" Data="{Binding}"/>
    /// 2. 在需要绑定的地方使用: {Binding Data.PropertyName, Source={StaticResource proxy}}
    /// </remarks>
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() => new BindingProxy();

        /// <summary>
        /// 数据属性 - 存储需要代理的数据源
        /// </summary>
        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}
