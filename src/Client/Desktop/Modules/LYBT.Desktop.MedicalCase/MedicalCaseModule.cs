using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.MedicalCase.Controls;
using LYBT.Desktop.MedicalCase.Dialogs;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Mappers;
using LYBT.Desktop.MedicalCase.Models.Items;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Desktop.Modules.MedicalCase.Models;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase
{
    /// <summary>
    /// 医疗案例管理模块 - 简化版
    /// </summary>
    [Module(ModuleName = nameof(MedicalCaseModule))]
    [ModuleDependency("PatientsModule")]
    [ModuleDependency("HerbsModule")]
    [ModuleDependency("FormulaModule")]
    public class MedicalCaseModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ViewModelLocationProvider.Register(typeof(MedicalCaseMasterDetailControl).ToString(), typeof(ViewModels.MedicalCaseMasterDetailViewModel));

            // S7: MedicalCaseService 拆分
            containerRegistry.Register<MedicalCaseEditContext>();
            containerRegistry.Register<IMedicalCaseQueryService, MedicalCaseQueryService>();
            containerRegistry.Register<IMedicalCaseCommandService, MedicalCaseCommandService>();
            containerRegistry.Register<IMedicalCaseLifecycleService, MedicalCaseLifecycleService>();
            containerRegistry.Register<IMedicalCaseService, MedicalCaseService>();

            containerRegistry.RegisterSingleton<MedicalCaseDetailModelMapper>();
            containerRegistry.Register<ViewModels.MedicalCaseMasterDetailViewModel>();

            containerRegistry.RegisterDialog<FormulaImportDialog, FormulaImportDialogViewModel>();
            containerRegistry.RegisterDialog<HistoryCopyDialog, HistoryCopyDialogViewModel>();
            containerRegistry.RegisterDialog<UnsavedChangesDialog, UnsavedChangesDialogViewModel>();

            containerRegistry.AddMasterDetailServices<MedicalCaseListDto, MedicalCaseDetailModel>();

            containerRegistry.RegisterForNavigation<Views.MedicalCaseMasterDetailView>();

            containerRegistry.Register<ViewModels.AuditLogViewModel>();
            containerRegistry.RegisterForNavigation<Views.AuditLogView>();
        }
    }
}
