using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Input;

namespace LYBT.WPFControls {
    /// <summary>
    /// 通用档案编辑视图控件，提供标题、内容呈现以及保存/取消按钮。
    /// </summary>
    [ContentProperty(nameof(ProfileContent))]
    public partial class CommonProfileView : UserControl {
        public CommonProfileView() {
            InitializeComponent();
        }

        public object? ProfileContent {
            get => GetValue(ProfileContentProperty);
            set => SetValue(ProfileContentProperty, value);
        }

        public static readonly DependencyProperty ProfileContentProperty =
            DependencyProperty.Register(nameof(ProfileContent), typeof(object), typeof(CommonProfileView));

        public string? Title {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(CommonProfileView));

        public ICommand? SaveCommand {
            get => (ICommand?)GetValue(SaveCommandProperty);
            set => SetValue(SaveCommandProperty, value);
        }

        public static readonly DependencyProperty SaveCommandProperty =
            DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(CommonProfileView));

        public ICommand? CancelCommand {
            get => (ICommand?)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(CommonProfileView));

        public bool IsEditable {
            get => (bool)GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(CommonProfileView), new PropertyMetadata(false));

        public bool IsBusy {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(CommonProfileView));
    }
}
