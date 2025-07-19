using LYBT.UI.WPF.Events;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LYBT.UI.WPF.ViewModels.Components {
    /// <summary>
    /// 状态栏视图模型
    /// </summary>
    public class StatusBarViewModel : BindableBase {
        private readonly IEventAggregator _eventAggregator;
        private DispatcherTimer _timer;

        #region Properties

        private string _systemStatus = "系统正常";
        /// <summary>
        /// 系统状态
        /// </summary>
        public string SystemStatus {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        private string _currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        /// <summary>
        /// 当前时间
        /// </summary>
        public string CurrentTime {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 刷新状态命令
        /// </summary>
        public DelegateCommand RefreshStatusCommand { get; private set; }

        #endregion

        public StatusBarViewModel(IEventAggregator eventAggregator) {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            InitializeCommands();
            InitializeTimer();
            SubscribeToEvents();
        }

        #region Private Methods

        private void InitializeCommands() {
            RefreshStatusCommand = new DelegateCommand(async () => await RefreshSystemStatusAsync());
        }

        private void InitializeTimer() {
            _timer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void SubscribeToEvents() {
            _eventAggregator.GetEvent<SystemStatusUpdatedEvent>().Subscribe(OnSystemStatusUpdated);
        }

        private void Timer_Tick(object sender, EventArgs e) {
            CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void OnSystemStatusUpdated(string status) {
            SystemStatus = status;
        }

        private async Task RefreshSystemStatusAsync() {
            try {
                SystemStatus = "正在刷新系统状态...";

                // 这里可以添加实际的系统状态检查逻辑
                await Task.Delay(1000); // 模拟检查过程

                SystemStatus = "系统运行正常";
            } catch (Exception ex) {
                SystemStatus = $"系统状态异常：{ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Refresh system status error: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 更新系统状态
        /// </summary>
        public void UpdateSystemStatus(string status) {
            SystemStatus = status;
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset() {
            SystemStatus = "系统正常";
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup() {
            _timer?.Stop();
        }

        #endregion
    }
}