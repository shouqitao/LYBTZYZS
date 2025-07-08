using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    public class HerbProfileViewModel : BindableBase {
        private readonly IHerbService _herbService;

        private HerbDetailDto _herb = new();
        public HerbDetailDto Herb { get => _herb; set => SetProperty(ref _herb, value); }

        private string _editModeTitle = "新增药材";
        public string EditModeTitle { get => _editModeTitle; set => SetProperty(ref _editModeTitle, value); }

        private bool _isEditable;
        public bool IsEditable { get => _isEditable; set => SetProperty(ref _isEditable, value); }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action? CancelAction { get; set; }

        public HerbProfileViewModel(IHerbService herbService) {
            _herbService = herbService;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public async Task LoadAsync(Guid? id = null) {
            if (id.HasValue && id.Value != Guid.Empty) {
                var info = await _herbService.GetByIdAsync(id.Value);
                if (info != null) {
                    Herb = info;
                    EditModeTitle = "编辑药材";
                }
            } else {
                Herb = new HerbDetailDto();
                EditModeTitle = "新增药材";
            }
        }

        private async Task SaveAsync() {
            try {
                bool success;
                if (Herb.Id == Guid.Empty) {
                    success = await _herbService.AddAsync(new HerbCreateDto {
                        Name = Herb.Name,
                        Pinyin = Herb.Pinyin,
                        Origin = Herb.Origin,
                        Spec = Herb.Spec,
                        Unit = Herb.Unit,
                        Price = Herb.Price,
                        Effect = Herb.Effect,
                        Remark = Herb.Remark
                    });
                } else {
                    success = await _herbService.UpdateAsync(new HerbEditDto {
                        Id = Herb.Id,
                        Name = Herb.Name,
                        Pinyin = Herb.Pinyin,
                        Origin = Herb.Origin,
                        Spec = Herb.Spec,
                        Unit = Herb.Unit,
                        Price = Herb.Price,
                        Effect = Herb.Effect,
                        Remark = Herb.Remark
                    });
                }
                if (!success)
                    MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            } catch (Exception ex) {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel() {
            CancelAction?.Invoke();
        }
    }
}
