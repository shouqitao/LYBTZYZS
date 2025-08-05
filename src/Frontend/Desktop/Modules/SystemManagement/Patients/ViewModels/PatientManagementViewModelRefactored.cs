using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Models.Patients;
using LYBT.WPF.Client.Modules.SystemManagement.Common.ViewModels;
using LYBT.Shared.Models.Common;
using Prism.Commands;

namespace LYBT.WPF.Client.Modules.SystemManagement.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型 - 重构版本，基于BaseManagementViewModel
    /// </summary>
    public class PatientManagementViewModelRefactored : BaseManagementViewModel<PatientInfo, IPatientService>
    {
        private readonly ICommonDialogService _commonDialogService;

        protected override string ModuleName => "患者";

        public PatientManagementViewModelRefactored(IPatientService patientService,
            ICommonDialogService commonDialogService) 
            : base(patientService)
        {
            _commonDialogService = commonDialogService;
        }

        #region 实现抽象方法

        /// <summary>
        /// 从服务加载患者数据
        /// </summary>
        protected override async Task<ServiceResult<PagedResult<PatientInfo>>> LoadDataFromServiceAsync(PaginationRequest request)
        {
            try
            {
                // 创建查询对象
                var query = new LYBT.Shared.Models.Contracts.Patients.PatientPagedQueryDto
                {
                    CurrentPage = request.CurrentPage,
                    PageSize = request.PageSize,
                    SearchKeyword = request.SearchKeyword,
                    Name = request.SearchKeyword // 用搜索关键词搜索姓名
                };
                
                // 调用患者服务获取分页数据
                var result = await Service.GetPagedAsync(query);
                
                // 转换为ServiceResult格式
                return ServiceResult<PagedResult<PatientInfo>>.Success(new PagedResult<PatientInfo>
                {
                    TotalCount = result.TotalCount,
                    Items = result.Items,
                    CurrentPage = result.CurrentPage,
                    PageSize = result.PageSize
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<PatientInfo>>.Failure($"加载患者列表失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从服务删除患者
        /// </summary>
        protected override async Task<ServiceResult<bool>> DeleteFromServiceAsync(PatientInfo patient)
        {
            if (patient == null) return ServiceResult<bool>.Failure("患者信息不能为空");
            
            // 使用禁用功能代替删除（软删除）
            var result = await Service.DisableAsync(patient.Id);
            if (result.IsSuccess)
            {
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                return ServiceResult<bool>.Failure(result.ErrorMessage ?? "禁用患者失败", result.Exception);
            }
        }

        /// <summary>
        /// 获取患者显示名称
        /// </summary>
        protected override string GetItemDisplayName(PatientInfo patient)
        {
            return patient?.Name ?? "未知患者";
        }

        #endregion

        #region 重写虚方法

        /// <summary>
        /// 执行新增患者
        /// </summary>
        protected override void ExecuteAdd()
        {
            try
            {
                var dialog = new Views.AddPatientDialog();
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                dialog.Title = "新增患者";
                
                // 创建 ViewModel 并设置为添加模式
                var viewModel = new AddPatientDialogViewModel(Service);
                dialog.DataContext = viewModel;
                
                // 设置保存成功回调
                viewModel.CloseDialogCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                    else
                    {
                        dialog.Close();
                    }
                };
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                System.Windows._commonDialogService.ShowErrorAsync($"添加患者失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行编辑患者
        /// </summary>
        protected override void ExecuteEdit(PatientInfo patient)
        {
            if (patient == null) return;

            try
            {
                var dialog = new Views.AddPatientDialog();
                dialog.Owner = System.Windows.Application.Current.MainWindow;
                dialog.Title = "编辑患者";
                
                // 创建 ViewModel 并设置为编辑模式
                var viewModel = new AddPatientDialogViewModel(Service, patient);
                dialog.DataContext = viewModel;
                
                // 设置保存成功回调
                viewModel.CloseDialogCallback = (success) =>
                {
                    if (success)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                    else
                    {
                        dialog.Close();
                    }
                };
                
                if (dialog.ShowDialog() == true)
                {
                    RefreshCommand.Execute();
                }
            }
            catch (Exception ex)
            {
                System.Windows._commonDialogService.ShowErrorAsync($"编辑患者失败: {ex.Message}", "错误").GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 执行查看患者详情
        /// </summary>
        protected override void ExecuteView(PatientInfo patient)
        {
            if (patient == null) return;

            // TODO: 实现查看患者详情对话框
            // var parameters = new DialogParameters
            // {
            //     { "PatientId", patient.Id }
            // };
            // _dialogService.ShowDialog("ViewPatientDialog", parameters, null);
            
            // 暂时显示简单信息
            var info = $"患者信息：\n" +
                      $"姓名：{patient.Name}\n" +
                      $"性别：{patient.GenderText}\n" +
                      $"年龄：{patient.Age}岁\n" +
                      $"电话：{patient.PhoneNumber ?? "未填写"}\n" +
                      $"地址：{patient.Address ?? "未填写"}";
            
            System.Windows._commonDialogService.ShowInformationAsync(info, "患者详情").GetAwaiter().GetResult();
        }

        #endregion
    }
}