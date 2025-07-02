using LYBT.Module.Billing.Dtos;
using LYBT.UI.WPF.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels {
    public class BillingStaffViewModel : BindableBase {
        public ObservableCollection<BillingDto> Bills { get; } = new();

        private BillingDto? _selectedBill;
        public BillingDto? SelectedBill {
            get => _selectedBill;
            set => SetProperty(ref _selectedBill, value);
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand MarkPaidCommand { get; }

        private readonly IBillingService _billingService;

        public BillingStaffViewModel(IBillingService billingService) {
            _billingService = billingService;
            RefreshCommand = new DelegateCommand(async () => await LoadBills());
            MarkPaidCommand = new DelegateCommand(async () => await MarkPaid(), () => SelectedBill != null)
                .ObservesProperty(() => SelectedBill);
            _ = LoadBills();
        }

        private async Task LoadBills() {
            var list = await _billingService.GetAllAsync();
            Bills.Clear();
            foreach (var b in list)
                Bills.Add(b);
        }

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
