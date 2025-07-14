using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows;
using LYBT.Common.Enums;

namespace LYBT.UI.WPF.ViewModels.Profile {
    public class HerbProfileViewModel : BindableBase {
        private readonly IHerbService _herbService;

        private HerbDetailDto _herb = new();
        public HerbDetailDto Herb { get => _herb; set => SetProperty(ref _herb, value); }

        private string _editModeTitle = "新增药材";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        private bool _isEditable;
        public bool IsEditable { get => _isEditable; set => SetProperty(ref _isEditable, value); }

        private ProfileMode _mode;
        /// <summary>
        /// 当前视图模式
        /// </summary>
        public ProfileMode Mode {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action? CancelAction { get; set; }

        public HerbProfileViewModel(IHerbService herbService) {
            _herbService = herbService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync(Guid? id = null, ProfileMode mode = ProfileMode.View) {
            Mode = mode;
            if (id.HasValue && id.Value != Guid.Empty) {
                var info = await _herbService.GetByIdAsync(id.Value);
                if (info != null)
                    Herb = info;
                else
                    Herb = new HerbDetailDto();
            } else {
                Herb = new HerbDetailDto();
            }

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新增药材";
                    IsEditable = true;
                    break;
                case ProfileMode.Edit:
                    EditModeTitle = "编辑药材";
                    IsEditable = true;
                    break;
                default:
                    EditModeTitle = "药材详情";
                    IsEditable = false;
                    break;
            }
        }

        private async Task SaveAsync() {
            try {
                bool success;
                if (Herb.Id == Guid.Empty) {
                    success = await _herbService.AddAsync(Herb);
                } else {
                    success = await _herbService.UpdateAsync(Herb);
                }
                if (!success)
                    MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                else {
                    Mode = ProfileMode.View;
                    IsEditable = false;
                    EditModeTitle = "药材详情";
                    CancelAction?.Invoke();
                }
            } catch (Exception ex) {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel() {
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "药材详情";
            CancelAction?.Invoke();
        }
    }
}
