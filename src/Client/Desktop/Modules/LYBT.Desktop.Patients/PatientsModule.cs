using FluentValidation;
using LYBT.Desktop.CardReader.Integration;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.DependencyInjection;
using LYBT.Desktop.Patients.Controls;
// OpenSpec: standardize-module-structure - Components已合并到Services
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Patients.Mappers;
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

            // SYNC-D02: IPatientRepository 由 Shell DI 工厂注册 (根据 IConnectionModeProvider 选择实现)
            // 远程模式 → PatientRepository (Patients 模块)
            // 本地模式 → LocalPatientRepository (LocalData 模块)

            // OpenSpec: integrate-cardreader-module - 患者读卡器集成服务
            containerRegistry.RegisterSingleton<IPatientCardReaderIntegration, PatientCardReaderIntegration>();

            // OpenSpec: standardize-api-architecture - MappingService已删除，使用直接Mapper实例

            // 注册FluentValidation验证器
            containerRegistry.Register<IValidator<PatientInputDto>, PatientInputDtoValidator>();

            // Issue #1790: 注册患者导入服务
            containerRegistry.RegisterSingleton<Services.PatientImportDataMapper>();
            containerRegistry.Register<Services.PatientImportExecutor>();

            // Issue #1790: 注册PatientSelectionViewModel组件化服务
            containerRegistry.Register<PatientSearchManager>();
            containerRegistry.RegisterSingleton<IPatientSearchCache, PatientSearchCache>();  // OpenSpec: refactor-patient-selection - 搜索缓存
            containerRegistry.Register<UnfinishedCaseHandler>();
            containerRegistry.Register<IPendingQueueManager, PendingQueueManager>();

            // OpenSpec: cleanup-ui-layer Phase 1.2 - 注册医案启动协调器
            containerRegistry.Register<IMedicalCaseStartCoordinator, ViewModels.Components.MedicalCaseStartCoordinator>();

            // Epic #1773 Task 4: 注册患者模块组件化组件（Scoped生命周期）
            // OpenSpec: cleanup-patient-dead-code - PatientStateManager已删除（死代码，从未被使用）
            containerRegistry.Register<IPatientService, Services.PatientService>();
            containerRegistry.Register<IPatientValidator, ViewModels.Components.PatientValidator>();

            // OpenSpec: migrate-views-to-role-modules - 视图模型和视图已迁移到角色台模块
            // PatientDetailViewModel/PatientDetailView已删除，改用PatientMasterDetailControl（内嵌在角色台的PatientManagementView中）
            // PatientSelectionViewModel已迁移到Clinical模块
            // QuickCreatePatientDialog已删除（无调用）

            // OpenSpec: refactor-viewmodel-composition - V2组合模式ViewModel
            // 注册Patients模块的MasterDetail服务
            containerRegistry.AddMasterDetailServices<PatientListDto, PatientDetailModel>();

            // Handler DI注册
            containerRegistry.Register<ViewModels.Handlers.IPatientStatusHandler, ViewModels.Handlers.PatientStatusHandler>();
            containerRegistry.Register<ViewModels.Handlers.IPatientImportExportHandler, ViewModels.Handlers.PatientImportExportHandler>();

            // OpenSpec: refactor-admin-workspace - Control模式重构
            // PatientMasterDetailControl供角色台View复用，ViewModel在Control内部解析
            containerRegistry.Register<ViewModels.PatientMasterDetailViewModel>();

            // OpenSpec: refactor-viewmodel-composition - Child ViewModels
            // 拆分PatientMasterDetailViewModel的读卡器和导入导出功能
            containerRegistry.Register<ViewModels.PatientCardReaderViewModel>();
            containerRegistry.Register<ViewModels.PatientImportExportViewModel>();
            
            // 注册MasterDetail View用于导航
            containerRegistry.RegisterForNavigation<Views.PatientMasterDetailView>();
        }
    }
}
