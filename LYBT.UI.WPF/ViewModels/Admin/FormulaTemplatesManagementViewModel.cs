using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.Common.Enums;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Prescriptions.Dtos;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace LYBT.UI.WPF.ViewModels.Admin {
    /// <summary>
    /// 经验方模板管理视图模型
    /// </summary>
    public class FormulaTemplatesManagementViewModel : BindableBase {
        private readonly IFormulaTemplateService _service;
        private readonly IHerbService _herbService;

        private int _pageIndex = 1;
        public int PageIndex {
            get => _pageIndex;
            set => SetProperty(ref _pageIndex, value);
        }

        private int _totalCount;
        public int TotalCount {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int PageSize { get; set; } = 20;

        public ObservableCollection<FormulaTemplateDto> Templates { get; } = new();
        public ObservableCollection<PrescriptionItemDto> InputItems { get; } = new();
        public ObservableCollection<HerbDto> Herbs { get; } = new();

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

        public FormulaTemplatesManagementViewModel(IFormulaTemplateService service, IHerbService herbService, FormulaTemplatesProfileViewModel profileViewModel) {
            _service = service;
            _herbService = herbService;
            ProfileViewModel = profileViewModel;
            RefreshCommand = new DelegateCommand(async () => await LoadAsync());
            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedTemplate != null).ObservesProperty(() => SelectedTemplate);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => SelectedTemplate != null).ObservesProperty(() => SelectedTemplate);
            ImportCommand = new DelegateCommand(async () => await ImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExportAsync());
            _ = LoadAsync();
            _ = LoadHerbsAsync();
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
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var count = await _service.ImportFromExcelAsync(dlg.FileName);
                    MessageBox.Show($"已导入 {count} 条模板", "提示");
                    await LoadAsync();
                } catch (Exception ex) {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误");
                }
            }
        }

        private async Task ExportAsync() {
            var dlg = new Microsoft.Win32.SaveFileDialog {
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                FileName = "经典方.xlsx"
            };
            if (dlg.ShowDialog() == true) {
                try {
                    var count = await _service.ExportToExcelAsync(dlg.FileName);
                    MessageBox.Show($"已导出 {count} 条模板", "提示");
                } catch (Exception ex) {
                    MessageBox.Show($"导出失败：{ex.Message}", "错误");
                }
            }
        }

        private async Task LoadHerbsAsync() {
            var list = await _herbService.GetListAsync();
            Herbs.Clear();
            foreach (var h in list)
                Herbs.Add(h);
        }
    }
}
