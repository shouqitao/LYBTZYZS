using System;
using Prism.Mvvm;

namespace LYBT.Desktop.Shell.ViewModels
{
    /// <summary>
    /// 诊断用的HomeViewModel - 最小化依赖
    /// </summary>
    public class DiagnosticHomeViewModel : BindableBase
    {
        private string _title = "诊断HomeViewModel";
        private string _message = "ViewModel成功创建和绑定！";
        
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }
        
        public DiagnosticHomeViewModel()
        {
            // 无参数构造函数，确保能被DI容器创建
            Message = $"ViewModel创建时间: {DateTime.Now:HH:mm:ss}";
        }
    }
}