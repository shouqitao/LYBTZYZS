using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Ioc;
using Prism.Navigation.Regions;
using LYBT.WPF.Client.Core.Interfaces.Services;

namespace LYBT.WPF.Client.Shell.Views
{
    /// <summary>
    /// SafeHomeView.xaml 的交互逻辑 - 防故障版本
    /// </summary>
    public partial class SafeHomeView : UserControl
    {
        private readonly IRegionManager _regionManager;
        private readonly IAuthenticationService _authService;
        private readonly ICommonDialogService _dialogService;
        private readonly DispatcherTimer _timer;

        public SafeHomeView()
        {
            InitializeComponent();
            
            // 手动解析依赖，避免ViewModelLocator问题
            try
            {
                var container = (Application.Current as App)?.Container;
                if (container != null)
                {
                    _regionManager = container.Resolve<IRegionManager>();
                    _authService = container.Resolve<IAuthenticationService>();
                    _dialogService = container.Resolve<ICommonDialogService>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化服务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 初始化定时器更新时间
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateDateTime();
            
            // 获取并显示用户信息
            try
            {
                if (_authService != null)
                {
                    var user = await _authService.GetCurrentUserAsync();
                    if (user != null)
                    {
                        WelcomeText.Text = $"欢迎，{user.RealName} ({(user.IsSysAdmin ? "系统管理员" : "医生")})";
                        
                        // 根据角色显示/隐藏按钮
                        if (!user.IsSysAdmin)
                        {
                            SystemManageBtn.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"加载用户信息失败: {ex.Message}";
            }
        }

        private void UpdateDateTime()
        {
            DateTimeText.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // 事件处理方法
        private void StartConsultation_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("PatientReceptionView", "开始看诊");
        }

        private void PatientReception_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("PatientReceptionView", "患者接待");
        }

        private void MedicalCase_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("MedicalCaseListView", "医疗案例");
        }

        private void Prescription_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("PrescriptionManagementView", "处方查询");
        }

        private void PatientManage_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("PatientManagementView", "患者管理");
        }

        private void HerbView_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("HerbManagementView", "药材查看");
        }

        private void FormulaView_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("FormulaManagementView", "验方库");
        }

        private void SystemManage_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("AdminMainView", "系统管理");
        }

        private void NavigateTo(string viewName, string displayName)
        {
            try
            {
                if (_regionManager != null)
                {
                    _regionManager.RequestNavigate("ContentRegion", viewName);
                    StatusText.Text = $"已导航到 {displayName}";
                }
                else
                {
                    StatusText.Text = "导航服务不可用";
                    MessageBox.Show($"无法导航到 {displayName}，导航服务未初始化", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"导航失败: {ex.Message}";
                
                if (_dialogService != null)
                {
                    _dialogService.ShowErrorAsync($"导航到 {displayName} 失败: {ex.Message}", "错误");
                }
                else
                {
                    MessageBox.Show($"导航到 {displayName} 失败: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}