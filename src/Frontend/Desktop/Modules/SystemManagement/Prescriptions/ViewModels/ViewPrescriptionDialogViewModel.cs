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

namespace LYBT.WPF.Client.Modules.SystemManagement.Prescriptions.ViewModels
{
    /// <summary>
    /// 查看处方详情对话框视图模型
    /// </summary>
    public class ViewPrescriptionDialogViewModel : BindableBase
    {
        private readonly IPrescriptionsApiService _prescriptionService;
        private readonly Guid _prescriptionId;

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
            PrescriptionStatus.Issued => "已开具",
            PrescriptionStatus.Confirmed => "已确认",
            PrescriptionStatus.Dispensed => "已调配",
            PrescriptionStatus.Completed => "已完成",
            PrescriptionStatus.Cancelled => "已取消",
            PrescriptionStatus.Voided => "已作废",
            _ => "未知状态"
        };

        /// <summary>状态颜色</summary>
        public string StatusColor => Prescription?.Status switch
        {
            PrescriptionStatus.Draft => "#FFC107",
            PrescriptionStatus.Issued => "#17A2B8",
            PrescriptionStatus.Confirmed => "#007BFF",
            PrescriptionStatus.Dispensed => "#28A745",
            PrescriptionStatus.Completed => "#6F42C1",
            PrescriptionStatus.Cancelled or PrescriptionStatus.Voided => "#DC3545",
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
        public bool CanVoid => Prescription?.Status != PrescriptionStatus.Voided && 
                               Prescription?.Status != PrescriptionStatus.Cancelled;

        #endregion

        #region 命令

        public DelegateCommand CloseCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand VoidCommand { get; }

        #endregion

        public Action? CloseDialogCallback { get; set; }
        public Action<PrescriptionDetailDto>? EditPrescriptionCallback { get; set; }

        public ViewPrescriptionDialogViewModel(IPrescriptionsApiService prescriptionService, Guid prescriptionId)
        {
            _prescriptionService = prescriptionService;
            _prescriptionId = prescriptionId;

            Items = new ObservableCollection<PrescriptionItemDto>();

            CloseCommand = new DelegateCommand(ExecuteClose);
            PrintCommand = new DelegateCommand(ExecutePrint);
            EditCommand = new DelegateCommand(ExecuteEdit, () => CanEdit);
            VoidCommand = new DelegateCommand(ExecuteVoid, () => CanVoid);

            // 加载处方详情
            _ = LoadPrescriptionAsync();
        }

        private async Task LoadPrescriptionAsync()
        {
            try
            {
                IsLoading = true;
                var response = await _prescriptionService.GetByIdAsync(_prescriptionId);
                
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
                    
                    // 更新命令状态
                    EditCommand.RaiseCanExecuteChanged();
                    VoidCommand.RaiseCanExecuteChanged();
                }
                else
                {
                    var error = response.Error?.Content ?? "获取处方详情失败";
                    MessageBox.Show($"加载处方详情失败: {error}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    CloseDialogCallback?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载处方详情失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CloseDialogCallback?.Invoke();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteClose()
        {
            CloseDialogCallback?.Invoke();
        }

        private void ExecutePrint()
        {
            if (Prescription == null) return;

            try
            {
                // TODO: 实现处方打印功能
                MessageBox.Show($"处方打印功能开发中...\n处方编号：{PrescriptionNumber}", 
                    "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印处方失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteEdit()
        {
            if (Prescription != null)
            {
                EditPrescriptionCallback?.Invoke(Prescription);
                CloseDialogCallback?.Invoke();
            }
        }

        private async void ExecuteVoid()
        {
            if (Prescription == null) return;

            var result = MessageBox.Show($"确定要作废该处方吗？\n处方编号：{PrescriptionNumber}\n患者：{PatientName}\n\n作废后将无法恢复！", 
                "确认作废", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    var response = await _prescriptionService.CancelAsync(Prescription.Id);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("处方作废成功", "成功", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // 重新加载处方信息以更新状态
                        await LoadPrescriptionAsync();
                    }
                    else
                    {
                        var error = response.Error?.Content ?? "作废处方失败";
                        MessageBox.Show($"作废处方失败: {error}", "错误", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"作废处方失败: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }
}