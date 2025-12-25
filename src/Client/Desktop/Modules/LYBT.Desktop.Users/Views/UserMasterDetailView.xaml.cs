using System.Windows.Controls;

namespace LYBT.Desktop.Users.Views
{
    /// <summary>
    /// 用户Master-Detail视图V2（组合模式）
    /// OpenSpec: refactor-viewmodel-composition
    ///
    /// 迁移完成后将移除V2后缀，替换原UserMasterDetailView
    /// </summary>
    public partial class UserMasterDetailView : UserControl
    {
        public UserMasterDetailView()
        {
            InitializeComponent();
        }
    }
}
