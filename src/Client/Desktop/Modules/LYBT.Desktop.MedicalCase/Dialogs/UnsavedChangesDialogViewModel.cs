using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008)
    /// 未保存修改确认对话框ViewModel
    /// 提供三个选项：保存修改、放弃修改、取消
    /// </summary>
    public class UnsavedChangesDialogViewModel : BindableBase, IDialogAware
    {
        #region IDialogAware

        public string Title => "未保存的修改";

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            // 无需参数，对话框仅用于确认操作
        }

        #endregion

        #region 命令

        /// <summary>
        /// 保存修改命令 - 保存当前修改后返回列表
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 放弃修改命令 - 不保存修改直接返回列表
        /// </summary>
        public DelegateCommand DiscardCommand { get; }

        /// <summary>
        /// 取消命令 - 留在当前编辑界面
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        #endregion

        #region 构造函数

        public UnsavedChangesDialogViewModel()
        {
            SaveCommand = new DelegateCommand(ExecuteSave);
            DiscardCommand = new DelegateCommand(ExecuteDiscard);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行保存修改
        /// ButtonResult.Yes 表示用户选择保存修改
        /// </summary>
        private void ExecuteSave()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Yes));
        }

        /// <summary>
        /// 执行放弃修改
        /// ButtonResult.No 表示用户选择放弃修改
        /// </summary>
        private void ExecuteDiscard()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.No));
        }

        /// <summary>
        /// 执行取消
        /// ButtonResult.Cancel 表示用户选择取消，留在当前界面
        /// </summary>
        private void ExecuteCancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
