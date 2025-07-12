using LYBT.Module.FormulaTemplates.Dtos;
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
    /// 经验方模板管理视图模型
    /// </summary>
    public class FormulaTemplatesManagementViewModel : BindableBase {
        private readonly IFormulaTemplateService _service;
        public ObservableCollection<FormulaTemplateDto> Templates { get; } = new();

        private FormulaTemplateDto? _selectedTemplate;
        public FormulaTemplateDto? SelectedTemplate {
            get => _selectedTemplate;
            set => SetProperty(ref _selectedTemplate, value);
        }

        public FormulaTemplatesProfileViewModel ProfileViewModel { get; }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand DeleteCommand { get; }

        public FormulaTemplatesManagementViewModel(IFormulaTemplateService service, FormulaTemplatesProfileViewModel profileViewModel) {
            _service = service;
            ProfileViewModel = profileViewModel;
            RefreshCommand = new DelegateCommand(async () => await LoadAsync());
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedTemplate != null).ObservesProperty(() => SelectedTemplate);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedTemplate != null).ObservesProperty(() => SelectedTemplate);
            _ = LoadAsync();
        }

        private async Task LoadAsync() {
            var list = await _service.GetListAsync();
            Templates.Clear();
            foreach (var t in list)
                Templates.Add(t);
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
            if (SelectedTemplate == null)
                return;
            ProfileViewModel.IsEditable = true;
            ProfileViewModel.CancelAction = async () => {
                ProfileViewModel.IsEditable = false;
                await LoadAsync();
            };
            _ = ProfileViewModel.LoadAsync(SelectedTemplate.Id);
        }

        private async Task DeleteAsync() {
            if (SelectedTemplate == null)
                return;
            if (MessageBox.Show("确定删除该模板吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                var ok = await _service.DeleteAsync(SelectedTemplate.Id);
                if (!ok)
                    MessageBox.Show("删除失败", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadAsync();
            }
        }
    }
}
