using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Models.Prescriptions;
using LYBT.WPF.Client.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Commands;
using Prism.Mvvm;

using LYBT.WPF.Client.Core.Interfaces.Services;
using Prism.Dialogs;
namespace LYBT.WPF.Client.Modules.SystemManagement.Prescriptions.ViewModels
{
    /// <summary>
    /// 查看处方详情对话框视图模型
    /// </summary>
    public class ViewPrescriptionDialogViewModel : BindableBase
    {
        private string _title = "详情";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }


        private readonly ICommonDialogService _commonDialogService;

        private readonly IPrescriptionsApiService _prescriptionService;

        #region 属性

        private PrescriptionDetailDto? _prescription;
        private bool _isLoading = true;

        /// <summary>处方详情</summary>
        public PrescriptionDetailDto? Prescription
        {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        /// <summary>是否正在加载</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>处方项目集合</summary>
        public ObservableCollection<PrescriptionItemDto> Items { get; }

        #endregion

        #region 计算属性

        /// <summary>处方编号</summary>
        public string PrescriptionNumber => Prescription != null 
            ? $"CF{Prescription.CreateTime:yyyyMMdd}{Prescription.Id.ToString("N")[..6].ToUpper()}"
            : string.Empty;

        /// <summary>状态描述</summary>
        public string StatusDescription => Prescription?.Status switch
        {
            PrescriptionStatus.Draft => "草稿",
            PrescriptionStatus.Completed => "已完成",
            _ => "未知状态"
        };

        /// <summary>状态颜色</summary>
        public string StatusColor => Prescription?.Status switch
        {
            PrescriptionStatus.Draft => "#FFC107",
            PrescriptionStatus.Completed => "#28A745",
            _ => "#6C757D"
        };

        /// <summary>患者姓名</summary>
        public string PatientName => "患者" + (Prescription?.PatientId.ToString()[..8] ?? "未知");

        /// <summary>医生姓名</summary>
        public string DoctorName => "医生" + (Prescription?.DoctorId.ToString()[..8] ?? "未知");

        /// <summary>创建时间描述</summary>
        public string CreateTimeDescription => Prescription?.CreateTime.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";

        /// <summary>总药材数量</summary>
        public int TotalItemCount => Items.Count;

        /// <summary>总重量</summary>
        public decimal TotalWeight => Items.Sum(item => item.Quantity);

        /// <summary>药材清单文本</summary>
        public string ItemsListText => Items.Any() 
            ? string.Join("、", Items.Select(item => $"{item.HerbName} {item.Quantity}{item.Unit}"))
            : "暂无药材";

        /// <summary>是否可以编辑</summary>
        public bool CanEdit => Prescription?.Status == PrescriptionStatus.Draft;

        /// <summary>是否可以作废</summary>
        public bool CanVoid => Prescription?.Status == PrescriptionStatus.Draft;

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand VoidCommand { get; }

        #endregion

        public ViewPrescriptionDialogViewModel(IPrescriptionsApiService prescriptionService,
            ICommonDialogService commonDialogService)
        {
            Title = "处方详情";
            _commonDialogService = commonDialogService;
            _prescriptionService = prescriptionService;

            Items = new ObservableCollection<PrescriptionItemDto>();

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
            EditCommand = new DelegateCommand(ExecuteEdit, () => CanEdit);
            VoidCommand = new DelegateCommand(ExecuteVoid, () => CanVoid);

            // 加载处方详情在 OnDialogOpened 中处理
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("prescriptionId"))
            {
                var id = parameters.GetValue<Guid>("prescriptionId");
                _ = LoadPrescriptionAsync(id);
            }

        }

        private async Task LoadPrescriptionAsync(Guid id)
        {
            try
            {
                IsLoading = true;
                var response = await _prescriptionService.GetByIdAsync(id);
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Prescription = response.Content;
                    
                    // 更新药材项目
                    Items.Clear();
                    if (Prescription.Items != null)
                    {
                        foreach (var item in Prescription.Items)
                        {
                            Items.Add(item);
                        }
                    }
                    
                    UpdateComputedProperties();
                    
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    VoidCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    var error = response.Error?.Content ?? "获取处方详情失败";
                    _commonDialogService.ShowErrorAsync($"加载处方详情失败: {error}", "错误").GetAwaiter().GetResult();
                    RaiseRequestClose(new DialogResult(ButtonResult.OK));
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"加载处方详情失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
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

        private void ExecutePrint()
        {
            if (Prescription == null) return;

            try
            {
                // TODO: 实现处方打印功能
                _commonDialogService.ShowInformationAsync($"处方打印功能开发中...\n处方编号：{PrescriptionNumber}", "提示").GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打印处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        private void ExecuteEdit()
        {
            if (Prescription != null)
            {
                // EditPrescriptionCallback removed - using dialog service
                RaiseRequestClose(new DialogResult(ButtonResult.OK));
            }
        }

        private async void ExecuteVoid()
        {
            if (Prescription == null) return;

            var result = await _commonDialogService.ShowConfirmationAsync($"确定要作废该处方吗？\n处方编号：{PrescriptionNumber}\n患者：{PatientName}\n\n作废后将无法恢复！", "确认作废");

            if (result )
            {
                try
                {
                    IsLoading = true;
                    var response = await _prescriptionService.CancelAsync(Prescription.Id);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _commonDialogService.ShowInformationAsync("处方作废成功", "成功").GetAwaiter().GetResult();
                        
                        // 重新加载处方信息以更新状态
                        await LoadPrescriptionAsync(Prescription.Id);
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "作废处方失败";
                        _commonDialogService.ShowErrorAsync($"作废处方失败: {error}", "错误").GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _commonDialogService.ShowErrorAsync($"作废处方失败: {ex.Message}", "错误").GetAwaiter().GetResult();
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
        
        private void UpdateComputedProperties()
        {
            // 触发计算属性更新
            RaisePropertyChanged(nameof(PrescriptionNumber));
            RaisePropertyChanged(nameof(StatusDescription));
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(PatientName));
            RaisePropertyChanged(nameof(DoctorName));
            RaisePropertyChanged(nameof(CreateTimeDescription));
            RaisePropertyChanged(nameof(TotalItemCount));
            RaisePropertyChanged(nameof(TotalWeight));
            RaisePropertyChanged(nameof(ItemsListText));
            RaisePropertyChanged(nameof(CanEdit));
            RaisePropertyChanged(nameof(CanVoid));
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