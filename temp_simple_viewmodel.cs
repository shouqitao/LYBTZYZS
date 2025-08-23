using System;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 临时简化的MainWindowViewModel - 用于诊断DI问题
    /// </summary>
    public class TempMainWindowViewModel : BindableBase
    {
        private string _title = "凌隐宝堂中医诊所诊疗系统";

        /// <summary>窗口标题</summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public TempMainWindowViewModel()
        {
            // 最简构造函数，无依赖项
        }
    }
}