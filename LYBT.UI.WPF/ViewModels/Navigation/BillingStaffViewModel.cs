using LYBT.Module.Billing.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.Common.Enums;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 类 BillingStaffViewModel 的说明
    /// </summary>
    public class BillingStaffViewModel : BindableBase {
        /// <summary>
        /// 属性 Bills 的说明
        /// </summary>
        public ObservableCollection<BillingDto> Bills { get; } = new();

        private BillingDto? _selectedBill;
        public BillingDto? SelectedBill {
            get => _selectedBill;
            set => SetProperty(ref _selectedBill, value);
        }

        /// <summary>
        /// 属性 RefreshCommand 的说明
        /// </summary>
        public DelegateCommand RefreshCommand { get; }
        /// <summary>
        /// 属性 MarkPaidCommand 的说明
        /// </summary>
        public DelegateCommand MarkPaidCommand { get; }

        private readonly IBillingService _billingService;

        public BillingStaffViewModel(IBillingService billingService) {
            _billingService = billingService;
            RefreshCommand = new DelegateCommand(async () => await LoadBills());
            MarkPaidCommand = new DelegateCommand(async () => await MarkPaid(), () => SelectedBill != null)
                .ObservesProperty(() => SelectedBill);
            _ = LoadBills();
        }

        /// <summary>
        /// 方法 LoadBills 的说明
        /// </summary>
        private async Task LoadBills() {
            var list = await _billingService.GetByStatusAsync(BillingStatus.Pending);
            Bills.Clear();
            foreach (var b in list)
                Bills.Add(b);
        }

        /// <summary>
        /// 方法 MarkPaid 的说明
        /// </summary>
        private async Task MarkPaid() {
            if (SelectedBill == null)
                return;
            bool ok = await _billingService.MarkAsPaidAsync(SelectedBill.Id);
            if (ok)
                await LoadBills();
            else
                MessageBox.Show("操作失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
