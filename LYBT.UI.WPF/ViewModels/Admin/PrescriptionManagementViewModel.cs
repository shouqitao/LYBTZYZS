using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 处方管理视图模型
    /// </summary>
    public class PrescriptionManagementViewModel : BindableBase {
        private readonly IPrescriptionService _service;
        public ObservableCollection<PrescriptionDto> Prescriptions { get; } = new();

        private PrescriptionDto? _selectedPrescription;
        public PrescriptionDto? SelectedPrescription {
            get => _selectedPrescription;
            set => SetProperty(ref _selectedPrescription, value);
        }

        public PrescriptionProfileViewModel ProfileViewModel { get; }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand DeleteCommand { get; }

        public PrescriptionManagementViewModel(IPrescriptionService service, PrescriptionProfileViewModel profileViewModel) {
            _service = service;
            ProfileViewModel = profileViewModel;
            RefreshCommand = new DelegateCommand(async () => await LoadAsync());
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedPrescription != null).ObservesProperty(() => SelectedPrescription);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedPrescription != null).ObservesProperty(() => SelectedPrescription);
            _ = LoadAsync();
        }

        private async Task LoadAsync() {
            var list = await _service.GetListAsync();
            Prescriptions.Clear();
            foreach (var p in list)
                Prescriptions.Add(p);
        }

        private void Add() {
            ProfileViewModel.IsEditable = true;
            ProfileViewModel.CancelAction = async () => {
                ProfileViewModel.IsEditable = false;
                await LoadAsync();
            };
            _ = ProfileViewModel.LoadAsync();
        }

        private void Edit() {
            if (SelectedPrescription == null)
                return;
            ProfileViewModel.IsEditable = true;
            ProfileViewModel.CancelAction = async () => {
                ProfileViewModel.IsEditable = false;
                await LoadAsync();
            };
            _ = ProfileViewModel.LoadAsync(SelectedPrescription.Id);
        }

        private async Task DeleteAsync() {
            if (SelectedPrescription == null)
                return;
            if (MessageBox.Show("确定删除该处方吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                var ok = await _service.CancelAsync(SelectedPrescription.Id);
                if (!ok)
                    MessageBox.Show("作废失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadAsync();
            }
        }
    }
}
