using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Dialogs
{
    /// <summary>
    /// 未保存修改确认对话框ViewModel
    /// 提供三个选项：保存修改、放弃修改、取消
    /// </summary>
    public partial class UnsavedChangesDialogViewModel : DialogViewModelBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public UnsavedChangesDialogViewModel(IViewModelServices services)
            : base(services)
        {
            Title = "未保存的修改";
        }

        /// <summary>
        /// 保存修改命令 - 保存当前修改后返回列表
        /// ButtonResult.Yes 表示用户选择保存修改
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            CloseDialog(ButtonResult.Yes);
        }

        /// <summary>
        /// 放弃修改命令 - 不保存修改直接返回列表
        /// ButtonResult.No 表示用户选择放弃修改
        /// </summary>
        [RelayCommand]
        private void Discard()
        {
            CloseDialog(ButtonResult.No);
        }
    }
}
