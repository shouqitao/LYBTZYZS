using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.WPF.Client.Modules.SystemManagement.Records.Views;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Records;
using Prism.Dialogs;

namespace LYBT.WPF.Client.Modules.SystemManagement.Records.ViewModels
{
    /// <summary>
    /// 病历管理视图模型 - 重构版本，基于BaseManagementViewModel
    /// </summary>
    public class RecordManagementViewModelRefactored : BaseManagementViewModel<RecordDto, IRecordService>
    {
        private readonly ICommonDialogService _commonDialogService;
        private readonly IDialogService _dialogService;
        private readonly IPatientService _patientService;
        
        protected override string ModuleName => "病历";

        public RecordManagementViewModelRefactored(IRecordService recordService, IPatientService patientService,
            ICommonDialogService commonDialogService,
            IDialogService dialogService)
            : base(recordService)
        {
            _commonDialogService = commonDialogService;
            _dialogService = dialogService;
            _patientService = patientService;
        }

        #region 实现抽象方法

        /// <summary>
        /// 从服务加载病历数据（由于服务不支持分页，这里手动实现）
        /// </summary>
        protected override async Task<ServiceResult<PagedResult<RecordDto>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                // 获取所有病历数据
                var result = await Service.GetListAsync();
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return ServiceResult<PagedResult<RecordDto>>.Failure(
                        result.ErrorMessage ?? "加载病历列表失败", 
                        result.Exception);
                }

                var allRecords = result.Data;

                // 应用搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
                {
                    allRecords = allRecords.Where(r =>
                        r.PatientName.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        r.Diagnosis.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 手动实现分页
                var totalCount = allRecords.Count;
                var pagedRecords = allRecords
                    .OrderByDescending(r => r.RecordTime) // 最新记录在前
                    .Skip((request.CurrentPage - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // 返回分页结果
                return ServiceResult<PagedResult<RecordDto>>.Success(new PagedResult<RecordDto>
                {
                    TotalCount = totalCount,
                    Items = pagedRecords,
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<RecordDto>>.Failure($"加载病历列表失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从服务删除病历
        /// </summary>
        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(RecordDto record)
        {
            if (record == null) return ServiceResult<bool>.Failure("病历信息不能为空");

            var result = await Service.DeleteAsync(record.Id);
            if (result.IsSuccess)
            {
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                return ServiceResult<bool>.Failure(result.ErrorMessage ?? "删除病历失败", result.Exception);
            }
        }

        /// <summary>
        /// 获取病历显示名称
        /// </summary>
        protected override string GetItemDisplayName(RecordDto record)
        {
            return $"{record.PatientName} - {record.RecordTime:yyyy-MM-dd}";
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 重写删除前确认消息
        /// </summary>
        protected override bool CanExecuteDelete(RecordDto record)
        {
            if (record == null) return false;

            var result = _commonDialogService.ShowConfirmationAsync($"确定要删除患者 \"{record.PatientName}\" 的病历记录吗？\n" +
                $"病历时间：{record.RecordTime:yyyy-MM-dd HH:mm}\n" +
                $"诊断：{record.Diagnosis}\n\n" +
                $"此操作不可恢复！", "确认删除").GetAwaiter().GetResult();
                
            return result ;
        }

        /// <summary>
        /// 执行新增病历
        /// </summary>
        protected override void ExecuteAdd()
        {
            try
            {
                var dialog = new AddRecordDialog
                {
                    Owner = Application.Current.MainWindow
                };

                // 创建简化版的 ViewModel
                var viewModel = new SimpleAddRecordDialogViewModel();
                dialog.DataContext = viewModel;

                viewModel.CloseDialogCallback = (success) =>
                {
                    dialog.DialogResult = success;
                    dialog.Close();
                    if (success)
                    {
                        RefreshCommand.Execute();
                    }
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开新增病历对话框失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行编辑病历
        /// </summary>
        protected override void ExecuteEdit(RecordDto record)
        {
            if (record == null) return;

            try
            {
                var viewModel = new EditRecordDialogViewModel(Service, _patientService, record.Id);
                var dialog = new EditRecordDialog
                {
                    DataContext = viewModel,
                    Owner = Application.Current.MainWindow
                };

                viewModel.CloseDialogCallback = (success) =>
                {
                    dialog.DialogResult = success;
                    dialog.Close();
                    if (success)
                    {
                        RefreshCommand.Execute();
                    }
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开编辑病历对话框失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行查看病历详情
        /// </summary>
        protected override void ExecuteView(RecordDto record)
        {
            if (record == null) return;

            try
            {
                var parameters = new DialogParameters
                {
                    { "recordId", record.Id }
                };

                _dialogService.ShowDialog("ViewRecordDialog", parameters, result =>
                {
                    // 对话框关闭后的处理（如果需要）
                });
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"打开病历详情对话框失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion

        #region 扩展功能

        /// <summary>
        /// 共享病历命令
        /// </summary>
        public async Task ShareRecordAsync(RecordDto record)
        {
            if (record == null) return;

            // TODO: 实现医生选择对话框
            await _commonDialogService.ShowInformationAsync("共享病历功能待实现", "提示");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 撤销共享命令
        /// </summary>
        public async Task UnshareRecordAsync(RecordDto record)
        {
            if (record == null) return;

            var confirmResult = await _commonDialogService.ShowConfirmationAsync($"确定要撤销病历的共享吗？\n患者：{record.PatientName}", "确认撤销");
                
            if (confirmResult != MessageBoxResult.Yes) return;

            try
            {
                var result = await Service.RevokeSharingAsync(record.Id);
                
                if (result.IsSuccess)
                {
                    _commonDialogService.ShowInformationAsync("病历共享已撤销", "成功").GetAwaiter().GetResult();
                    RefreshCommand.Execute();
                }
                else
                {
                    _commonDialogService.ShowErrorAsync($"撤销共享失败：{result.ErrorMessage}", "错误").GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _commonDialogService.ShowErrorAsync($"撤销共享失败：{ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        #endregion
    }
}