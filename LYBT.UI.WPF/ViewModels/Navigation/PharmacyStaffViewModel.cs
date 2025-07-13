using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using LYBT.Common.Enums;
using LYBT.Module.Pharmacy.Dtos;
using LYBT.UI.WPF.Interfaces;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 药房工作人员视图模型，管理待抓药和已处理处方列表
    /// </summary>
    public class PharmacyStaffViewModel : BindableBase {
        public ObservableCollection<PharmacyDto> PendingItems { get; } = new();
        public ObservableCollection<PharmacyDto> ProcessedItems { get; } = new();

        private PharmacyDto? _selectedPending;
        public PharmacyDto? SelectedPending {
            get => _selectedPending;
            set => SetProperty(ref _selectedPending, value);
        }

        private PharmacyDto? _selectedProcessed;
        public PharmacyDto? SelectedProcessed {
            get => _selectedProcessed;
            set => SetProperty(ref _selectedProcessed, value);
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand MarkDispensedCommand { get; }
        public DelegateCommand MarkCompletedCommand { get; }

        private readonly IPharmacyService _pharmacyService;
        private readonly IAuthService _authService;

        public PharmacyStaffViewModel(IPharmacyService pharmacyService, IAuthService authService) {
            _pharmacyService = pharmacyService;
            _authService = authService;

            RefreshCommand = new DelegateCommand(async () => await LoadAsync());
            MarkDispensedCommand = new DelegateCommand(async () => await MarkDispensedAsync(), () => SelectedPending != null)
                .ObservesProperty(() => SelectedPending);
            MarkCompletedCommand = new DelegateCommand(async () => await MarkCompletedAsync(), () => SelectedProcessed != null)
                .ObservesProperty(() => SelectedProcessed);

            _ = LoadAsync();
        }

        private async Task LoadAsync() {
            var waiting = await _pharmacyService.GetWaitingListAsync();
            var all = await _pharmacyService.GetListAsync();
            var processed = all.Where(p => p.Status != (int)PharmacyStatus.Waiting).ToList();

            PendingItems.Clear();
            foreach (var item in waiting)
                PendingItems.Add(item);

            ProcessedItems.Clear();
            foreach (var item in processed)
                ProcessedItems.Add(item);
        }

        private async Task MarkDispensedAsync() {
            if (SelectedPending == null)
                return;
            bool ok = await _pharmacyService.MarkAsPreparedAsync(SelectedPending.Id);
            if (ok)
                await LoadAsync();
            else
                MessageBox.Show("操作失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async Task MarkCompletedAsync() {
            if (SelectedProcessed == null)
                return;

            var detail = await _pharmacyService.GetByIdAsync(SelectedProcessed.Id);
            if (detail == null)
                return;

            var dto = new PharmacyEditDto {
                Id = detail.Id,
                OperatorId = _authService.UserId,
                DispenseTime = detail.DispenseTime,
                Status = PharmacyStatus.Completed,
                Remark = detail.Remark
            };

            bool ok = await _pharmacyService.UpdateAsync(dto);
            if (ok)
                await LoadAsync();
            else
                MessageBox.Show("操作失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
