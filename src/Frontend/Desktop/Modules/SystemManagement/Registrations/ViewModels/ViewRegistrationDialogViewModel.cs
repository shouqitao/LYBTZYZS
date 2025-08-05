using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Registration;

using Prism.Dialogs;
namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels
{
    /// <summary>
    /// 查看挂号对话框视图模型
    /// </summary>
    public class ViewRegistrationDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private readonly ICommonDialogService _commonDialogService;

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

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        public ViewRegistrationDialogViewModel(IRegistrationService registrationService,
            ICommonDialogService commonDialogService)
        {
            Title = "挂号详情";
            _commonDialogService = commonDialogService;
            _registrationService = registrationService;

            PrintCommand = new DelegateCommand(ExecutePrint);
            CloseCommand = new DelegateCommand(ExecuteClose);

            // 获取当前窗口实例
            _window = Application.Current.Windows[Application.Current.Windows.Count - 1];
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("registrationId"))
            {
                var id = parameters.GetValue<Guid>("registrationId");
                _ = LoadRegistrationAsync(id);
            }

        }
        
        private async System.Threading.Tasks.Task LoadRegistrationAsync(Guid registrationId)
        {
            try
            {
                IsLoading = true;
                _registrationId = registrationId;
                await LoadRegistrationData();
            }
            catch (Exception)
            {
                // Handle error
            }
            finally
            {
                IsLoading = false;
            }
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
                    await _commonDialogService.ShowErrorAsync("未找到挂号信息", "错误");
                    _window.Close();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载挂号信息失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                _window.Close();
            }
        }

        private void ExecutePrint()
        {
            // TODO: 实现打印功能
            _commonDialogService.ShowInformationAsync("打印功能待实现", "提示").GetAwaiter().GetResult();
        }

        private void ExecuteClose()
        {
            _window.Close();
        }
        // 临时占位方法 - 等待IDialogAware问题解决
        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            // TODO: 实现对话框关闭逻辑
        }



        /* #region IDialogAware Implementation

        event Action<IDialogResult> IDialogAware.RequestClose
        {
            add { _requestClose += value; }
            remove { _requestClose -= value; }
        }
        
        private Action<IDialogResult>? _requestClose;

        private void RaiseRequestClose(IDialogResult dialogResult)
        {
            _requestClose?.Invoke(dialogResult);
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        #endregion */
        }
}