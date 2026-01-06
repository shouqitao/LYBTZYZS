using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// 未保存修改确认对话框ViewModel
    /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008)
    /// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
    /// 提供三个选项：保存修改、放弃修改、取消
    /// </summary>
    public partial class UnsavedChangesDialogViewModel : ObservableObject, IDialogAware
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
        /// ButtonResult.Yes 表示用户选择保存修改
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Yes));
        }

        /// <summary>
        /// 放弃修改命令 - 不保存修改直接返回列表
        /// ButtonResult.No 表示用户选择放弃修改
        /// </summary>
        [RelayCommand]
        private void Discard()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.No));
        }

        /// <summary>
        /// 取消命令 - 留在当前编辑界面
        /// ButtonResult.Cancel 表示用户选择取消
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion
    }
}
