using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using LYBT.Common.Enums;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LYBT.UI.WPF.ViewModels.Profile {
    /// <summary>
    /// 经验方模板详情与编辑视图模型
    /// </summary>
    public class FormulaTemplatesProfileViewModel : BindableBase {
        private readonly IFormulaTemplateService _service;

        private FormulaTemplateDetailDto _template = new();
        public FormulaTemplateDetailDto Template {
            get => _template;
            set => SetProperty(ref _template, value);
        }

        public ObservableCollection<HerbDto> Herbs { get; } = new();

        private HerbDto? _selectedHerb;
        public HerbDto? SelectedHerb {
            get => _selectedHerb;
            set => SetProperty(ref _selectedHerb, value);
        }

        private string _editModeTitle = "新增模板";
        public string EditModeTitle {
            get => _editModeTitle;
            set => SetProperty(ref _editModeTitle, value);
        }

        private bool _isEditable;
        public bool IsEditable {
            get => _isEditable;
            set => SetProperty(ref _isEditable, value);
        }

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
        public DelegateCommand AddHerbCommand { get; }
        public DelegateCommand RemoveHerbCommand { get; }

        public Action? CancelAction { get; set; }

        public FormulaTemplatesProfileViewModel(IFormulaTemplateService service) {
            _service = service;
            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
            AddHerbCommand = new DelegateCommand(AddHerb);
            RemoveHerbCommand = new DelegateCommand(RemoveHerb, () => SelectedHerb != null).ObservesProperty(() => SelectedHerb);
        }

        public async Task LoadAsync(Guid? id = null, ProfileMode mode = ProfileMode.View) {
            Mode = mode;
            if (id.HasValue && id.Value != Guid.Empty) {
                var detail = await _service.GetByIdAsync(id.Value);
                if (detail != null) {
                    Template = detail;
                    Herbs.Clear();
                    foreach (var h in detail.Herbs)
                        Herbs.Add(h);
                } else {
                    Template = new FormulaTemplateDetailDto();
                    Herbs.Clear();
                }
            } else {
                Template = new FormulaTemplateDetailDto();
                Herbs.Clear();
            }

            switch (mode) {
                case ProfileMode.Create:
                    EditModeTitle = "新增模板";
                    IsEditable = true;
                    break;
                case ProfileMode.Edit:
                    EditModeTitle = "编辑模板";
                    IsEditable = true;
                    break;
                default:
                    EditModeTitle = "模板详情";
                    IsEditable = false;
                    break;
            }
        }

        private void AddHerb() {
            Herbs.Add(new HerbDto());
        }

        private void RemoveHerb() {
            if (SelectedHerb != null)
                Herbs.Remove(SelectedHerb);
        }

        private async Task SaveAsync() {
            Template.Herbs = Herbs.ToList();
            bool ok;
            if (Template.Id == Guid.Empty) {
                var dto = new FormulaTemplateCreateDto {
                    Name = Template.Name,
                    Herbs = Herbs.ToList(),
                    Remark = Template.Remark
                };
                ok = await _service.AddAsync(dto);
            } else {
                var dto = new FormulaTemplateEditDto {
                    Id = Template.Id,
                    Name = Template.Name,
                    Herbs = Herbs.ToList(),
                    Remark = Template.Remark
                };
                ok = await _service.UpdateAsync(dto);
            }
            if (!ok)
                MessageBox.Show("保存失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            else {
                Mode = ProfileMode.View;
                IsEditable = false;
                EditModeTitle = "模板详情";
                CancelAction?.Invoke();
            }
        }

        private void Cancel() {
            Mode = ProfileMode.View;
            IsEditable = false;
            EditModeTitle = "模板详情";
            CancelAction?.Invoke();
        }
    }
}
