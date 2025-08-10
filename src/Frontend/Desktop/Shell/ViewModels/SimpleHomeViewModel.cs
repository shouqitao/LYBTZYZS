using System;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Shell.ViewModels
{
    /// <summary>
    /// 简单版HomeViewModel - 无复杂依赖，用于测试
    /// </summary>
    public class SimpleHomeViewModel : BindableBase
    {
        private string _welcomeMessage = "欢迎，管理员";
        private string _subTitle = "系统管理工作台";
        private bool _isAdminRole = true;
        private bool _isDoctorRole = false;
        private string _currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string SubTitle
        {
            get => _subTitle;
            set => SetProperty(ref _subTitle, value);
        }

        public bool IsAdminRole
        {
            get => _isAdminRole;
            set => SetProperty(ref _isAdminRole, value);
        }

        public bool IsDoctorRole
        {
            get => _isDoctorRole;
            set => SetProperty(ref _isDoctorRole, value);
        }

        public string CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        // 模拟统计数据
        public int TodayCompletedCount { get; set; } = 5;
        public int TodayInProgressCount { get; set; } = 3;
        public decimal TodayTotalAmount { get; set; } = 750m;
        
        public string StatusMessage { get; set; } = "测试就绪";

        public SimpleHomeViewModel()
        {
            // 简单的构造函数，无依赖注入
        }
    }
}