using FluentValidation;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Patients.Controls;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Patients.Models;
using LYBT.Desktop.Patients.Models.Items;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Validators.Patients;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;

namespace LYBT.Desktop.Patients
{
    /// <summary>
    /// 患者管理模块 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    [Module(ModuleName = nameof(PatientsModule))]
    [ModuleDependency("AuthenticationModule")]
    [ModuleDependency("UsersModule")]
    public class PatientsModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ViewModelLocationProvider.Register(typeof(PatientMasterDetailControl).ToString(), typeof(ViewModels.PatientMasterDetailViewModel));

            // IPatientRepository 由 Shell DI 注册 (Refit API)
            containerRegistry.RegisterSingleton<IPatientCardReaderIntegration, PatientCardReaderIntegration>();

            // 注册FluentValidation验证器
            containerRegistry.Register<IValidator<PatientInputDto>, PatientInputDtoValidator>();

            // Issue #1790: 注册患者导入服务

            // Issue #1790: 注册PatientSelectionViewModel组件化服务
            containerRegistry.Register<PatientSearchManager>();
            containerRegistry.RegisterSingleton<IPatientSearchCache, PatientSearchCache>();
            containerRegistry.Register<IMedicalCaseStartCoordinator, ViewModels.Components.MedicalCaseStartCoordinator>();

            // Epic #1773 Task 4: 注册患者模块组件化组件（Scoped生命周期）
            containerRegistry.Register<IPatientService, Services.PatientService>();
            containerRegistry.Register<IPatientValidator, ViewModels.Components.PatientValidator>();
            // PatientDetailViewModel/PatientDetailView已删除，改用PatientMasterDetailControl（内嵌在角色台的PatientManagementView中）
            // PatientSelectionViewModel已迁移到Clinical模块
            // QuickCreatePatientDialog已删除（无调用）
            // 注册Patients模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<PatientListDto, PatientDetailModel>();

            // Handler DI注册
            containerRegistry.Register<ViewModels.Handlers.IPatientStatusHandler, ViewModels.Handlers.PatientStatusHandler>();
            // PatientMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.PatientMasterDetailViewModel>();
            // 拆分PatientMasterDetailViewModel的读卡器功能
            containerRegistry.Register<ViewModels.PatientCardReaderViewModel>();
            containerRegistry.Register<ViewModels.PatientEditorViewModel>();
            

        }
    }
}
