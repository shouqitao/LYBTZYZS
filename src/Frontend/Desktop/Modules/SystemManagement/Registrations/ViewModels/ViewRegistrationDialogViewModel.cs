using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels
{
    /// <summary>
    /// 查看挂号对话框视图模型
    /// </summary>
    public class ViewRegistrationDialogViewModel : BindableBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly Window _window;
        private Guid _registrationId;

        #region Properties

        private RegistrationInfo? _registration;
        public RegistrationInfo? Registration
        {
            get => _registration;
            set => SetProperty(ref _registration, value);
        }

        #endregion

        #region Commands

        public DelegateCommand PrintCommand { get; }
        public DelegateCommand CloseCommand { get; }

        #endregion

        public ViewRegistrationDialogViewModel(IRegistrationService registrationService)
        {
            _registrationService = registrationService;

            PrintCommand = new DelegateCommand(ExecutePrint);
            CloseCommand = new DelegateCommand(ExecuteClose);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];
        }

        public async void Initialize(Guid registrationId)
        {
            _registrationId = registrationId;
            await LoadRegistrationData();
        }

        private async Task LoadRegistrationData()
        {
            try
            {
                var registration = await _registrationService.GetByIdAsync(_registrationId);
                if (registration != null)
                {
                    Registration = registration;
                }
                else
                {
                    MessageBox.Show("未找到挂号信息", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载挂号信息失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _window.Close();
            }
        }

        private void ExecutePrint()
        {
            // TODO: 实现打印功能
            MessageBox.Show("打印功能待实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteClose()
        {
            _window.Close();
        }
    }
}