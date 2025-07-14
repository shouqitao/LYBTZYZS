using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;
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
        public DelegateCommand ImportCommand { get; }
        public DelegateCommand ExportCommand { get; }

        public FormulaTemplatesManagementViewModel(IFormulaTemplateService service, FormulaTemplatesProfileViewModel profileViewModel) {
            _service = service;
            ProfileViewModel = profileViewModel;
            RefreshCommand = new DelegateCommand(async () => await LoadAsync());
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedTemplate != null).ObservesProperty(() => SelectedTemplate);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedTemplate != null).ObservesProperty(() => SelectedTemplate);
            ImportCommand = new DelegateCommand(async () => await ImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExportAsync());
            _ = LoadAsync();
        }

        private async Task LoadAsync() {
            var list = await _service.GetListAsync();
            Templates.Clear();
            foreach (var t in list)
                Templates.Add(t);
        }

        private void Add() {
            ProfileViewModel.CancelAction = async () => {
                await LoadAsync();
                await ProfileViewModel.LoadAsync(SelectedTemplate?.Id, ProfileMode.View);
            };
            _ = ProfileViewModel.LoadAsync(null, ProfileMode.Create);
        }

        private void Edit() {
            if (SelectedTemplate == null)
                return;
            ProfileViewModel.CancelAction = async () => {
                await LoadAsync();
                await ProfileViewModel.LoadAsync(SelectedTemplate.Id, ProfileMode.View);
            };
            _ = ProfileViewModel.LoadAsync(SelectedTemplate.Id, ProfileMode.Edit);
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

        private async Task ImportAsync() {
            // 在此示例中仅演示调用服务导入接口，实际场景可结合文件选择等
            var count = await _service.ImportAsync(new List<FormulaTemplateImportDto>());
            MessageBox.Show($"已导入 {count} 条模板", "提示");
            await LoadAsync();
        }

        private async Task ExportAsync() {
            var data = await _service.ExportAsync();
            // 此处仅简单提示，实际可保存到文件
            MessageBox.Show($"已导出 {data.Count} 条模板", "提示");
        }
    }
}
