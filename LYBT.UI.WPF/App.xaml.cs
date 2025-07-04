using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.Views;
using LYBT.UI.WPF.Views.Admin;
using LYBT.UI.WPF.Views.Main;
using Prism.Ioc;
using Refit;
using System;
using System.Configuration;
using System.Net.Http;
using System.Windows;

namespace LYBT.UI.WPF {
    /// <summary>
    /// 类 App 的说明
    /// </summary>
    public partial class App : PrismApplication {
        /// <summary>
        /// 方法 RegisterTypes 的说明
        /// </summary>
        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            // 1. 构造无Token的HttpClient，首次登录还没有token，正常用即可  
            string? baseUrl = ConfigurationManager.AppSettings["WebApiBaseUrl"];

            if (string.IsNullOrEmpty(baseUrl)) {
                throw new InvalidOperationException("WebApiBaseUrl is not configured in AppSettings.");
            }

            var httpClient = new HttpClient() {
                BaseAddress = new Uri(baseUrl)
            };

            // 2. 用Refit创建IAuthApi实例  
            var authApi = RestService.For<IAuthApi>(httpClient);
            var userApi = RestService.For<IUserApi>(httpClient);
            var billingApi = RestService.For<IBillingApi>(httpClient);
            var diagnosisTreatmentApi = RestService.For<IDiagnosisTreatmentApi>(httpClient);
            var doctorApi = RestService.For<IDoctorApi>(httpClient);
            var formulaTemplateApi = RestService.For<IFormulaTemplateApi>(httpClient);
            var herbApi = RestService.For<IHerbApi>(httpClient);
            var patientApi = RestService.For<IPatientApi>(httpClient);
            var queueingApi = RestService.For<IQueueingApi>(httpClient);
            var recordApi = RestService.For<IRecordApi>(httpClient);
            var registrationApi = RestService.For<IRegistrationApi>(httpClient);
            var settingsApi = RestService.For<ISettingsApi>(httpClient);
            var logApi = RestService.For<ILogApi>(httpClient);
            var syncApi = RestService.For<ISyncApi>(httpClient);

            // 3. 手动new AuthService（注入authApi实例），不让Unity自动构造！
            var authService = new AuthService(authApi);
            var userService = new Services.UserService(userApi);
            var billingService = new BillingService(billingApi);
            var diagnosisTreatmentService = new DiagnosisTreatmentService(diagnosisTreatmentApi);
            var doctorService = new DoctorService(doctorApi);
            var formulaTemplateService = new FormulaTemplateService(formulaTemplateApi);
            var herbService = new HerbService(herbApi);
            var patientService = new PatientService(patientApi);
            var queueingService = new QueueingService(queueingApi);
            var recordService = new RecordService(recordApi);
            var registrationService = new RegistrationService(registrationApi);
            var settingsService = new SettingsService(settingsApi);
            var logService = new LogService(logApi);
            var syncService = new SyncService(syncApi);

            // 4. 用RegisterInstance注册，后续所有用IAuthService和IAuthApi的地方都能用  
            containerRegistry.RegisterInstance(authApi);
            containerRegistry.RegisterInstance(userApi);
            containerRegistry.RegisterInstance(billingApi);
            containerRegistry.RegisterInstance(diagnosisTreatmentApi);
            containerRegistry.RegisterInstance(doctorApi);
            containerRegistry.RegisterInstance(formulaTemplateApi);
            containerRegistry.RegisterInstance(herbApi);
            containerRegistry.RegisterInstance(patientApi);
            containerRegistry.RegisterInstance(queueingApi);
            containerRegistry.RegisterInstance(recordApi);
            containerRegistry.RegisterInstance(registrationApi);
            containerRegistry.RegisterInstance(settingsApi);
            containerRegistry.RegisterInstance(logApi);
            containerRegistry.RegisterInstance(syncApi);
            containerRegistry.RegisterInstance<IAuthService>(authService);
            containerRegistry.RegisterInstance<Services.IUserService>(userService);
            containerRegistry.RegisterInstance<IBillingService>(billingService);
            containerRegistry.RegisterInstance<IDiagnosisTreatmentService>(diagnosisTreatmentService);
            containerRegistry.RegisterInstance<IDoctorService>(doctorService);
            containerRegistry.RegisterInstance<IFormulaTemplateService>(formulaTemplateService);
            containerRegistry.RegisterInstance<IHerbService>(herbService);
            containerRegistry.RegisterInstance<IPatientService>(patientService);
            containerRegistry.RegisterInstance<IQueueingService>(queueingService);
            containerRegistry.RegisterInstance<IRecordService>(recordService);
            containerRegistry.RegisterInstance<IRegistrationService>(registrationService);
            containerRegistry.RegisterInstance<ISettingsService>(settingsService);
            containerRegistry.RegisterInstance<ILogService>(logService);
            containerRegistry.RegisterInstance<ISyncService>(syncService);

            // 5. 如果你还有其他API接口，也用同样方式new出来后RegisterInstance  
            containerRegistry.RegisterForNavigation<LoginView>("LoginView");
            containerRegistry.RegisterForNavigation<MainWindow>("MainWindow");
            containerRegistry.RegisterForNavigation<HomeView>("HomeView");
            containerRegistry.RegisterForNavigation<AdminView>("AdminView");
            containerRegistry.RegisterForNavigation<UserManagementView>("UserManagementView");
            containerRegistry.RegisterForNavigation<DoctorManagementView>("DoctorManagementView");
            containerRegistry.RegisterForNavigation<BillingStaffView>("BillingStaffView");
            containerRegistry.RegisterForNavigation<ChangePasswordView>("ChangePasswordView");
            containerRegistry.RegisterForNavigation<ChangeProfileView>("ChangeProfileView");

        }

        /// <summary>
        /// 方法 CreateShell 的说明
        /// </summary>
        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        /// <summary>
        /// 方法 OnInitialized 的说明
        /// </summary>
        protected override void OnInitialized() {
            base.OnInitialized();
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("FunctionRegion", "LoginView");
        }
    }
}
