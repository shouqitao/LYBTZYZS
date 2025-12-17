using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 数据列表工具栏控件
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 功能：
    /// - 新增/刷新/导出按钮组
    /// - 可选的批量操作按钮
    /// - 支持自定义附加按钮
    /// </summary>
    public partial class DataGridToolbar : UserControl
    {
        public DataGridToolbar() => InitializeComponent();

        #region CreateCommand - 新增命令

        public ICommand CreateCommand
        {
            get => (ICommand)GetValue(CreateCommandProperty);
            set => SetValue(CreateCommandProperty, value);
        }

        public static readonly DependencyProperty CreateCommandProperty =
            DependencyProperty.Register(nameof(CreateCommand), typeof(ICommand), typeof(DataGridToolbar),
                new PropertyMetadata(null));

        #endregion

        #region RefreshCommand - 刷新命令

        public ICommand RefreshCommand
        {
            get => (ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(DataGridToolbar),
                new PropertyMetadata(null));

        #endregion

        #region ExportCommand - 导出命令

        public ICommand ExportCommand
        {
            get => (ICommand)GetValue(ExportCommandProperty);
            set => SetValue(ExportCommandProperty, value);
        }

        public static readonly DependencyProperty ExportCommandProperty =
            DependencyProperty.Register(nameof(ExportCommand), typeof(ICommand), typeof(DataGridToolbar),
                new PropertyMetadata(null));

        #endregion

        #region BatchDeleteCommand - 批量删除命令

        public ICommand BatchDeleteCommand
        {
            get => (ICommand)GetValue(BatchDeleteCommandProperty);
            set => SetValue(BatchDeleteCommandProperty, value);
        }

        public static readonly DependencyProperty BatchDeleteCommandProperty =
            DependencyProperty.Register(nameof(BatchDeleteCommand), typeof(ICommand), typeof(DataGridToolbar),
                new PropertyMetadata(null));

        #endregion

        #region AdditionalContent - 附加内容

        public object AdditionalContent
        {
            get => GetValue(AdditionalContentProperty);
            set => SetValue(AdditionalContentProperty, value);
        }

        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register(nameof(AdditionalContent), typeof(object), typeof(DataGridToolbar),
                new PropertyMetadata(null));

        #endregion
    }
}
