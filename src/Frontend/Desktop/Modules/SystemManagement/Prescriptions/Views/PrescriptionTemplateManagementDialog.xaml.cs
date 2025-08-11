using System;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Admin.Prescriptions.ViewModels;

namespace LYBT.Desktop.Admin.Prescriptions.Views
{
    /// <summary>
    /// 处方模板管理对话框
    /// </summary>
    public partial class PrescriptionTemplateManagementDialog : Window
    {
        private PrescriptionTemplateManagementViewModel? _viewModel;

        public PrescriptionTemplateManagementDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        /// <summary>
        /// 获取选中的模板（用于应用到处方）
        /// </summary>
        public PrescriptionTemplate? SelectedTemplate { get; private set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as PrescriptionTemplateManagementViewModel;
            if (_viewModel != null)
            {
                _viewModel.TemplateApplied += OnTemplateApplied;
                _viewModel.LoadTemplatesAsync();
            }
        }

        private void OnTemplateApplied(object? sender, PrescriptionTemplate template)
        {
            SelectedTemplate = template;
            DialogResult = true;
            Close();
        }

        private void CreateTemplate_Click(object sender, RoutedEventArgs e)
        {
            // 打开模板编辑器创建新模板
            var editor = new PrescriptionTemplateEditorDialog
            {
                Owner = this
            };
            
            if (editor.ShowDialog() == true && editor.Template != null)
            {
                _viewModel?.AddTemplate(editor.Template);
            }
        }

        private void ImportTemplates_Click(object sender, RoutedEventArgs e)
        {
            // 导入模板功能
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "导入处方模板",
                Filter = "JSON文件|*.json|所有文件|*.*",
                DefaultExt = ".json"
            };

            if (openDialog.ShowDialog() == true)
            {
                _viewModel?.ImportTemplatesAsync(openDialog.FileName);
            }
        }

        private void ExportTemplates_Click(object sender, RoutedEventArgs e)
        {
            // 导出模板功能
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "导出处方模板",
                Filter = "JSON文件|*.json",
                FileName = $"处方模板_{DateTime.Now:yyyyMMdd}.json",
                DefaultExt = ".json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                _viewModel?.ExportTemplatesAsync(saveDialog.FileName);
            }
        }

        private void CategoryFilter_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && _viewModel != null)
            {
                var category = radioButton.Tag?.ToString() ?? "全部";
                _viewModel.FilterByCategory(category);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}