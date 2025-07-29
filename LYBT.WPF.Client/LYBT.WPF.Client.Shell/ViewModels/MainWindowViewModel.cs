using System;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Enums;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Modules.Authentication.Views;
using LYBT.WPF.Client.Modules.Authentication.ViewModels;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.Dialogs;
using Prism.Events;

namespace LYBT.WPF.Client.Shell.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IRegionManager _regionManager;
        private readonly IEventAggregator _eventAggregator;
        
        private string _title = "凌隐宝堂中医诊所管理系统";
        private UserInfo? _currentUser;
        private bool _isLoggedIn = false;

        public MainWindowViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IAuthenticationService authService)
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _authService = authService;

            // 订阅登录成功事件
            _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);

            // 延迟检查登录状态，等待主窗口完全加载
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                CheckLoginStatus();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>窗口标题</summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>当前用户</summary>
        public UserInfo? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        /// <summary>是否已登录</summary>
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set 
            { 
                SetProperty(ref _isLoggedIn, value);
                RaisePropertyChanged(nameof(IsNotLoggedIn));
            }
        }

        /// <summary>是否未登录（用于界面显示）</summary>
        public bool IsNotLoggedIn => !_isLoggedIn;

        /// <summary>
        /// 检查登录状态
        /// </summary>
        private async void CheckLoginStatus()
        {
            if (_authService.IsLoggedIn)
            {
                CurrentUser = await _authService.GetCurrentUserAsync();
                IsLoggedIn = true;
                LoadMainContent();
            }
            else
            {
                ShowLoginDialog();
            }
        }

        /// <summary>
        /// 登录成功事件处理
        /// </summary>
        private void OnLoginSuccess()
        {
            // 重新检查登录状态
            CheckLoginStatus();
        }

        /// <summary>
        /// 显示登录界面
        /// </summary>
        private void ShowLoginDialog()
        {
            // 在单窗口模式下，导航到登录视图
            if (_regionManager != null)
            {
                _regionManager.RequestNavigate("LoginRegion", "LoginView");
            }
        }

        /// <summary>
        /// 加载主界面内容
        /// </summary>
        private void LoadMainContent()
        {
            if (CurrentUser == null) return;

            // 根据用户角色加载不同的主界面
            switch (CurrentUser.Role)
            {
                case UserRole.SuperAdmin:
                case UserRole.Admin:
                    LoadManagementInterface();
                    break;
                case UserRole.FrontDesk:
                    LoadFrontDeskInterface();
                    break;
                case UserRole.DiagnosingDoctor:
                case UserRole.InternDoctor:
                    LoadDoctorInterface();
                    break;
                case UserRole.Cashier:
                    LoadCashierInterface();
                    break;
                default:
                    MessageBox.Show("未知的用户角色，请联系管理员", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        /// <summary>
        /// 加载管理界面
        /// </summary>
        private void LoadManagementInterface()
        {
            // 导航到系统管理界面
            Title = $"凌隐宝堂中医诊所管理系统 - {CurrentUser?.RealName} (管理员)";
            
            // 加载系统管理模块的主界面
            LoadSystemManagementModule();
        }

        /// <summary>
        /// 加载前台界面
        /// </summary>
        private void LoadFrontDeskInterface()
        {
            Title = $"凌隐宝堂中医诊所管理系统 - {CurrentUser?.RealName} (前台)";
            
            // 导航到前台主界面
            if (_regionManager != null)
            {
                _regionManager.RequestNavigate("ContentRegion", "FrontDeskMainView");
            }
        }

        /// <summary>
        /// 加载医生界面
        /// </summary>
        private void LoadDoctorInterface()
        {
            Title = $"凌隐宝堂中医诊所管理系统 - {CurrentUser?.RealName} (医生)";
            
            // 导航到医生主界面
            if (_regionManager != null)
            {
                _regionManager.RequestNavigate("ContentRegion", "DoctorMainView");
            }
        }

        /// <summary>
        /// 加载收银界面
        /// </summary>
        private void LoadCashierInterface()
        {
            Title = $"凌隐宝堂中医诊所管理系统 - {CurrentUser?.RealName} (收银员)";
            
            // 导航到收银主界面
            if (_regionManager != null)
            {
                _regionManager.RequestNavigate("ContentRegion", "CashierMainView");
            }
        }

        /// <summary>
        /// 加载系统管理模块
        /// </summary>
        private void LoadSystemManagementModule()
        {
            if (_regionManager != null)
            {
                // 导航到系统管理主界面
                _regionManager.RequestNavigate("ContentRegion", "SystemManagementView");
            }
            else
            {
                // 显示开发中提示
                MessageBox.Show($"欢迎您，{CurrentUser?.RealName}！\n\n系统管理模块正在加载...", "登录成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}