using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

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
            var dlg = new Microsoft.Win32.OpenFileDialog {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var json = await File.ReadAllTextAsync(dlg.FileName);
                    var list = JsonSerializer.Deserialize<List<FormulaTemplateImportDto>>(json);
                    if (list != null) {
                        var count = await _service.ImportAsync(list);
                        MessageBox.Show($"已导入 {count} 条模板", "提示");
                        await LoadAsync();
                    }
                } catch (Exception ex) {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误");
                }
            }
        }

        private async Task ExportAsync() {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = "templates.json"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var data = await _service.ExportAsync();
                    var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(dlg.FileName, json);
                    MessageBox.Show($"已导出 {data.Count} 条模板", "提示");
                } catch (Exception ex) {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误");
                }
            }
        }
    }
}
