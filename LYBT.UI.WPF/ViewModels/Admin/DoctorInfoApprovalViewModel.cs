using LYBT.Models.Doctors;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 医生信息审批视图模型
    /// </summary>
    public class DoctorInfoApprovalViewModel : BindableBase {
        private readonly Services.IDoctorService _doctorService;
        public ObservableCollection<DoctorInfoRequestModel> Requests { get; } = new();

        public DoctorInfoApprovalViewModel(Services.IDoctorService doctorService) {
            _doctorService = doctorService;
            ApproveCommand = new DelegateCommand(async () => await ApproveAsync(), () => SelectedRequest != null).ObservesProperty(() => SelectedRequest);
            RejectCommand = new DelegateCommand(async () => await RejectAsync(), () => SelectedRequest != null).ObservesProperty(() => SelectedRequest);
            _ = LoadAsync();
        }

        public DelegateCommand ApproveCommand { get; }
        public DelegateCommand RejectCommand { get; }

        private DoctorInfoRequestModel? _selectedRequest;
        public DoctorInfoRequestModel? SelectedRequest { get => _selectedRequest; set => SetProperty(ref _selectedRequest, value); }

        private async Task LoadAsync() {
            var list = await _doctorService.GetPendingRequestsAsync();
            Requests.Clear();
            foreach (var item in list)
                Requests.Add(item);
        }

        private async Task ApproveAsync() {
            if (SelectedRequest == null) return;
            var ok = await _doctorService.ApproveRequestAsync(SelectedRequest.Id);
            if (ok) {
                Requests.Remove(SelectedRequest);
                MessageBox.Show("已批准", "提示");
            }
        }

        private async Task RejectAsync() {
            if (SelectedRequest == null) return;
            var ok = await _doctorService.RejectRequestAsync(SelectedRequest.Id);
            if (ok) {
                Requests.Remove(SelectedRequest);
                MessageBox.Show("已驳回", "提示");
            }
        }
    }
}
