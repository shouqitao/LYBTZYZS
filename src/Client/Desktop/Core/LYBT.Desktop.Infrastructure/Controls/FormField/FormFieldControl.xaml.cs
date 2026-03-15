using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 表单字段控件 - 标签 + 输入 + 验证错误的组合
    /// </summary>
    public partial class FormFieldControl : UserControl
    {
        public FormFieldControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(FormFieldControl),
                new PropertyMetadata(string.Empty));

        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(FormFieldControl),
                new PropertyMetadata(false));

        public object InputContent
        {
            get => GetValue(InputContentProperty);
            set => SetValue(InputContentProperty, value);
        }

        public static readonly DependencyProperty InputContentProperty =
            DependencyProperty.Register(nameof(InputContent), typeof(object), typeof(FormFieldControl),
                new PropertyMetadata(null));

        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(nameof(ErrorMessage), typeof(string), typeof(FormFieldControl),
                new PropertyMetadata(string.Empty));

        public bool HasError
        {
            get => (bool)GetValue(HasErrorProperty);
            set => SetValue(HasErrorProperty, value);
        }

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(FormFieldControl),
                new PropertyMetadata(false));

        #endregion
    }
}
