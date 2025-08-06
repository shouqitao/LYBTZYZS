using System;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Doctors;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Doctors.ViewModels
{
    /// <summary>
    /// 查看医生详情对话框视图模型
    /// </summary>
    public class ViewDoctorDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private readonly ICommonDialogService _commonDialogService;
        private readonly IDoctorService _doctorService;

        #region 属性

        private DoctorInfo? _doctor;
        private bool _isLoading = true;

        /// <summary>医生信息</summary>
        public DoctorInfo? Doctor
        {
            get => _doctor;
            set => SetProperty(ref _doctor, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region 计算属性

        /// <summary>年龄描述</summary>
        public string AgeDescription => $"{"暂无"}岁";

        /// <summary>性别描述</summary>
        public string GenderDescription => "暂无";

        /// <summary>职称描述</summary>
        public string TitleDescription => "暂无";

        /// <summary>状态描述</summary>
        public string StatusDescription => Doctor?.StatusDisplayName ?? "-";

        /// <summary>工作状态描述</summary>
        public string WorkStatusDescription => _doctor?.IsActive == true ? "在岗" : "离岗";

        /// <summary>启用状态描述</summary>
        public string ActiveStatusDescription => Doctor?.IsActive == true ? "已启用" : "已禁用";

        /// <summary>创建时间描述</summary>
        public string CreateTimeDescription => Doctor?.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }

        #endregion

        public ViewDoctorDialogViewModel(IDoctorService doctorService,
            ICommonDialogService commonDialogService)
        {
            Title = "医生详情";
            _commonDialogService = commonDialogService;
            _doctorService = doctorService;

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("doctorId"))
            {
                var doctorId = parameters.GetValue<Guid>("doctorId");
                _ = LoadDoctorAsync(doctorId);
            }

        }

        private async System.Threading.Tasks.Task LoadDoctorAsync(Guid doctorId)
        {
            try
            {
                IsLoading = true;
                var result = await _doctorService.GetDoctorByIdAsync(doctorId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    Doctor = result.Data;
                    UpdateComputedProperties();
                }
                else
                {
                    await _commonDialogService.ShowErrorAsync($"加载医生信息失败：{result.ErrorMessage}", "错误");
                    RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
                }
            }
            catch (Exception ex)
            {
                await _commonDialogService.ShowErrorAsync($"加载医生信息失败：{ex.Message}", "错误");
                RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClose()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private async void ExecutePrint()
        {
            // TODO: 实现打印功能
            await _commonDialogService.ShowInformationAsync("打印功能开发中...", "提示");
        }

        private void UpdateComputedProperties()
        {
            // 触发计算属性更新
            RaisePropertyChanged(nameof(AgeDescription));
            RaisePropertyChanged(nameof(GenderDescription));
            RaisePropertyChanged(nameof(TitleDescription));
            RaisePropertyChanged(nameof(StatusDescription));
            RaisePropertyChanged(nameof(WorkStatusDescription));
            RaisePropertyChanged(nameof(ActiveStatusDescription));
            RaisePropertyChanged(nameof(CreateTimeDescription));
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