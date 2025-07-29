using System.Windows.Input;
using System.Windows.Threading;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using LYBT.UI.PrismWpf.Services;
using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.ViewModels
{
    /// <summary>
    /// 主窗口ViewModel
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {
        private readonly IAuthService _authService;
        private readonly IRegionManager _regionManager;
        private readonly DispatcherTimer _timer;

        private string _title = "LYBT中医诊所管理系统";
        private string _statusMessage = "就绪";
        private DateTime _currentTime = DateTime.Now;

        public MainWindowViewModel(IAuthService authService, IRegionManager regionManager)
        {
            _authService = authService;
            _regionManager = regionManager;

            // 初始化命令
            NavigateCommand = new DelegateCommand<string>(Navigate);
            LogoutCommand = new DelegateCommand(async () => await LogoutAsync());

            // 初始化定时器更新时间
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 初始化状态
            Initialize();
        }

        #region Properties

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 当前时间
        /// </summary>
        public DateTime CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        /// <summary>
        /// 当前用户信息
        /// </summary>
        public UserInfo? CurrentUser => _authService.CurrentUser;

        #endregion

        #region Commands

        /// <summary>
        /// 导航命令
        /// </summary>
        public ICommand NavigateCommand { get; }

        /// <summary>
        /// 注销命令
        /// </summary>
        public ICommand LogoutCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            // 默认导航到用户管理页面
            Navigate("UserManagement");
            
            // 更新状态消息
            if (CurrentUser != null)
            {
                StatusMessage = $"欢迎使用，{CurrentUser.RealName}";
            }
        }

        /// <summary>
        /// 导航到指定页面
        /// </summary>
        private void Navigate(string? viewName)
        {
            if (string.IsNullOrEmpty(viewName))
                return;

            try
            {
                StatusMessage = $"正在加载 {GetViewDisplayName(viewName)}...";

                // 使用Prism区域管理器导航
                _regionManager.RequestNavigate("ContentRegion", viewName);

                StatusMessage = $"{GetViewDisplayName(viewName)} 加载完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导航失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 用户注销
        /// </summary>
        private async Task LogoutAsync()
        {
            try
            {
                StatusMessage = "正在注销...";
                
                await _authService.LogoutAsync();
                
                // 关闭当前窗口，返回登录界面
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusMessage = $"注销失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取视图显示名称
        /// </summary>
        private string GetViewDisplayName(string viewName)
        {
            return viewName switch
            {
                "UserManagement" => "用户管理",
                "RoleManagement" => "角色管理",
                "PatientManagement" => "患者管理",
                "DoctorManagement" => "医生管理",
                "HerbManagement" => "药材管理",
                "FormulaTemplateManagement" => "经验方模板",
                "SystemSettings" => "系统设置",
                "SystemLogs" => "操作日志",
                _ => viewName
            };
        }

        /// <summary>
        /// 定时器事件处理
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            _timer?.Stop();
        }

        #endregion
    }
}
