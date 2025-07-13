using LYBT.Module.TreatmentRoom.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Common.Enums;

namespace LYBT.UI.WPF.ViewModels.Navigation {
    /// <summary>
    /// 理疗医生页面 ViewModel
    /// </summary>
    public class TreatmentDoctorViewModel : BindableBase {
        public ObservableCollection<TreatmentRoomDto> Tasks { get; } = new();

        private TreatmentRoomDto? _selectedTask;
        public TreatmentRoomDto? SelectedTask {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand StartCommand { get; }
        public DelegateCommand FinishCommand { get; }

        private readonly ITreatmentRoomService _treatmentRoomService;

        public TreatmentDoctorViewModel(ITreatmentRoomService treatmentRoomService) {
            _treatmentRoomService = treatmentRoomService;
            RefreshCommand = new DelegateCommand(async () => await LoadTasks());
            StartCommand = new DelegateCommand(async () => await StartTask(), () => SelectedTask != null)
                .ObservesProperty(() => SelectedTask);
            FinishCommand = new DelegateCommand(async () => await FinishTask(), () => SelectedTask != null)
                .ObservesProperty(() => SelectedTask);
            _ = LoadTasks();
        }

        private async Task LoadTasks() {
            var list = await _treatmentRoomService.GetListAsync();
            Tasks.Clear();
            foreach (var t in list)
                Tasks.Add(t);
        }

        private async Task StartTask() {
            if (SelectedTask == null)
                return;
            var detail = await _treatmentRoomService.GetByIdAsync(SelectedTask.Id);
            if (detail == null) {
                MessageBox.Show("无法获取任务详情", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dto = new TreatmentRoomEditDto {
                Id = detail.Id,
                TreatmentItem = detail.TreatmentItem,
                Count = detail.Count,
                Status = TreatmentTaskStatus.InProgress.ToString(),
                EndTime = detail.EndTime ?? DateTime.Now,
                Remark = detail.Remark
            };
            bool ok = await _treatmentRoomService.UpdateAsync(dto);
            if (!ok)
                MessageBox.Show("开始治疗失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            await LoadTasks();
        }

        private async Task FinishTask() {
            if (SelectedTask == null)
                return;
            var detail = await _treatmentRoomService.GetByIdAsync(SelectedTask.Id);
            if (detail == null) {
                MessageBox.Show("无法获取任务详情", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var dto = new TreatmentRoomEditDto {
                Id = detail.Id,
                TreatmentItem = detail.TreatmentItem,
                Count = detail.Count,
                Status = TreatmentTaskStatus.Completed.ToString(),
                EndTime = DateTime.Now,
                Remark = detail.Remark
            };
            bool ok = await _treatmentRoomService.UpdateAsync(dto);
            if (!ok)
                MessageBox.Show("完成治疗失败", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            await LoadTasks();
        }
    }
}
