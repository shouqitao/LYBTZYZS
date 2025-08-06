using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Base;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Payment;
using LYBT.WPF.Client.Modules.Payment.Dialogs;
using Prism.Commands;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.Payment.ViewModels
{
    /// <summary>
    /// 付费管理视图模型
    /// </summary>
    public class PaymentManagementViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IPaymentService _paymentService;

        #region 属性

        private ObservableCollection<PaymentInfo> _paymentList;
        public ObservableCollection<PaymentInfo> PaymentList
        {
            get => _paymentList;
            set => SetProperty(ref _paymentList, value);
        }

        private PaymentInfo _selectedPayment;
        public PaymentInfo SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        private string _searchKeyword;
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetProperty(ref _searchKeyword, value);
        }

        private string _selectedStatus = "全部状态";
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                SetProperty(ref _selectedStatus, value);
                _ = SearchPayments();
            }
        }

        private DateTime? _selectedDate = DateTime.Today;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                SetProperty(ref _selectedDate, value);
                _ = SearchPayments();
            }
        }

        private decimal _todayIncome;
        public decimal TodayIncome
        {
            get => _todayIncome;
            set => SetProperty(ref _todayIncome, value);
        }

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        #endregion

        #region 命令

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand<PaymentInfo> ChargeCommand { get; }
        public DelegateCommand<PaymentInfo> RefundCommand { get; }
        public DelegateCommand<PaymentInfo> ViewDetailCommand { get; }
        public DelegateCommand<PaymentInfo> PrintReceiptCommand { get; }

        #endregion

        public PaymentManagementViewModel(
            IDialogService dialogService,
            IPaymentService paymentService)
        {
            _dialogService = dialogService;
            _paymentService = paymentService;

            PaymentList = new ObservableCollection<PaymentInfo>();

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchPayments());
            RefreshCommand = new DelegateCommand(async () => await LoadPayments());
            ExportCommand = new DelegateCommand(async () => await ExportPayments());
            ChargeCommand = new DelegateCommand<PaymentInfo>(Charge);
            RefundCommand = new DelegateCommand<PaymentInfo>(Refund);
            ViewDetailCommand = new DelegateCommand<PaymentInfo>(ViewDetail);
            PrintReceiptCommand = new DelegateCommand<PaymentInfo>(PrintReceipt);

            // 加载数据
            _ = LoadPayments();
            _ = LoadStatistics();
        }

        private async Task LoadPayments()
        {
            try
            {
                SetBusy(true);

                var payments = await _paymentService.GetTodayPaymentsAsync();
                PaymentList.Clear();

                foreach (var payment in payments)
                {
                    PaymentList.Add(new PaymentInfo
                    {
                        Id = payment.Id,
                        PaymentNumber = payment.PaymentNumber,
                        PatientName = payment.PatientName,
                        Gender = payment.Gender,
                        Age = payment.Age,
                        DoctorName = payment.DoctorName,
                        PaymentType = payment.PaymentType,
                        TotalAmount = payment.TotalAmount,
                        ActualAmount = payment.ActualAmount,
                        PaymentMethod = payment.PaymentMethod,
                        Status = payment.Status,
                        StatusText = GetStatusText(payment.Status),
                        StatusColor = GetStatusColor(payment.Status),
                        CreateTime = payment.CreateTime,
                        CanCharge = payment/* .Status = */= "待收费",
                        CanRefund = payment/* .Status = */= "已收费",
                        CanPrint = payment/* .Status = */= "已收费" || payment/* .Status = */= "已退费"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"加载付费列表失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task SearchPayments()
        {
            try
            {
                SetBusy(true);

                var searchDto = new PaymentSearchDto
                {
                    Keyword = SearchKeyword,
                    Status = _selectedStatus == "全部状态" ? null : _selectedStatus,
                    Date = _selectedDate
                };

                var payments = await _paymentService.SearchPaymentsAsync(searchDto);
                PaymentList.Clear();

                foreach (var payment in payments)
                {
                    PaymentList.Add(new PaymentInfo
                    {
                        Id = payment.Id,
                        PaymentNumber = payment.PaymentNumber,
                        PatientName = payment.PatientName,
                        Gender = payment.Gender,
                        Age = payment.Age,
                        DoctorName = payment.DoctorName,
                        PaymentType = payment.PaymentType,
                        TotalAmount = payment.TotalAmount,
                        ActualAmount = payment.ActualAmount,
                        PaymentMethod = payment.PaymentMethod,
                        Status = payment.Status,
                        StatusText = GetStatusText(payment.Status),
                        StatusColor = GetStatusColor(payment.Status),
                        CreateTime = payment.CreateTime,
                        CanCharge = payment/* .Status = */= "待收费",
                        CanRefund = payment/* .Status = */= "已收费",
                        CanPrint = payment/* .Status = */= "已收费" || payment/* .Status = */= "已退费"
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError($"搜索付费记录失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task LoadStatistics()
        {
            try
            {
                var statistics = await _paymentService.GetTodayStatisticsAsync();
                TodayIncome = statistics.TotalIncome;
                PendingCount = statistics.PendingCount;
            }
            catch (Exception ex)
            {
                ShowError($"加载统计数据失败：{ex.Message}");
            }
        }

        private async Task ExportPayments()
        {
            try
            {
                SetBusy(true);

                var exportData = PaymentList.ToList();
                await _paymentService.ExportPaymentsAsync(exportData);

                ShowInfo("导出成功");
            }
            catch (Exception ex)
            {
                ShowError($"导出失败：{ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void Charge(PaymentInfo payment)
        {
            if (payment == null) return;

            var parameters = new DialogParameters
            {
                { "Payment", payment }
            };

            _dialogService.ShowDialog("ChargeDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 刷新列表
                    _ = LoadPayments();
                    _ = LoadStatistics();
                }
            });
        }

        private void Refund(PaymentInfo payment)
        {
            if (payment == null) return;

            if (!ShowConfirm($"确定要对"{payment.PatientName}"的付费进行退费吗？"))
                return;

            var parameters = new DialogParameters
            {
                { "Payment", payment }
            };

            _dialogService.ShowDialog("RefundDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    // 刷新列表
                    _ = LoadPayments();
                    _ = LoadStatistics();
                }
            });
        }

        private void ViewDetail(PaymentInfo payment)
        {
            if (payment == null) return;

            var parameters = new DialogParameters
            {
                { "PaymentId", payment.Id }
            };

            _dialogService.ShowDialog("PaymentDetailDialog", parameters);
        }

        private void PrintReceipt(PaymentInfo payment)
        {
            if (payment == null) return;

            try
            {
                // TODO: 实现收据打印功能
                ShowInfo($"正在打印{payment.PatientName}的收据...");
            }
            catch (Exception ex)
            {
                ShowError($"打印失败：{ex.Message}");
            }
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "待收费" => "待收费",
                "已收费" => "已收费",
                "已退费" => "已退费",
                "已取消" => "已取消",
                _ => status
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "待收费" => "#FF9800",
                "已收费" => "#4CAF50",
                "已退费" => "#9E9E9E",
                "已取消" => "#F44336",
                _ => "#9E9E9E"
            };
        }
    }
}