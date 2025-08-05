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
    public class ViewRegistrationDialogViewModel : BindableBase, IDialogAware
    {
        
        #region IDialogAware

        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("commondialoginfoId"))
            {
                var id = parameters.GetValue<Guid>("commondialoginfoId");
                _ = LoadDataAsync(id);
            }
            else if (parameters.ContainsKey("commondialoginfo"))
            {
                var data = parameters.GetValue<CommonDialogInfo>("commondialoginfo");
                SetData(data);
                IsLoading = false;
                UpdateComputedProperties();
            }
        }

        #endregion

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

        #endregion

        public ViewRegistrationDialogViewModel(IRegistrationService registrationService,
            ICommonDialogService commonDialogService)
        {
            _commonDialogService = commonDialogService;
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
    }
}