using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.Views;
using LYBT.UI.WPF.Views.Admin;
using LYBT.UI.WPF.Views.Main;
using LYBT.UI.WPF.Views.Profile;
using LYBT.UI.WPF.ViewModels.Profile;
using Refit;
using System.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using LYBT.UI.WPF.Interfaces;
using LYBT.UI.WPF.Views.Navigation;

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

            var tokenHandler = new TokenHandler { InnerHandler = new HttpClientHandler() };
            var httpClient = new HttpClient(tokenHandler) {
                BaseAddress = new Uri(baseUrl)
            };

            // 优化：统一配置 Refit 的枚举序列化方式
            var refitSettings = new RefitSettings {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                })
            };

            // 2. 用Refit创建IAuthApi实例  
            var authApi = RestService.For<IAuthApi>(httpClient, refitSettings);
            var userApi = RestService.For<IUserApi>(httpClient, refitSettings);
            var doctorApi = RestService.For<IDoctorApi>(httpClient, refitSettings);
            var patientApi = RestService.For<IPatientApi>(httpClient, refitSettings);
            var registrationApi = RestService.For<IRegistrationApi>(httpClient, refitSettings);
            var herbApi = RestService.For<IHerbApi>(httpClient, refitSettings);
            var billingApi = RestService.For<IBillingApi>(httpClient, refitSettings);
            var prescriptionApi = RestService.For<IPrescriptionApi>(httpClient, refitSettings);
            var formulaTemplateApi = RestService.For<IFormulaTemplateApi>(httpClient, refitSettings);
            var logApi = RestService.For<ILogApi>(httpClient, refitSettings);
            var treatmentRoomApi = RestService.For<ITreatmentRoomApi>(httpClient, refitSettings);

            // 3. 手动new AuthService（注入authApi实例），不让Unity自动构造！
            var authService = new AuthService(authApi);
            var userService = new UserService(userApi);
            var doctorService = new DoctorService(doctorApi);
            var patientService = new PatientService(patientApi);
            var registrationService = new RegistrationService(registrationApi);
            var herbService = new HerbService(herbApi);
            var billingService = new BillingService(billingApi);
            var pharmacyApi = RestService.For<IPharmacyApi>(httpClient, refitSettings);
            var pharmacyService = new PharmacyService(pharmacyApi);
            var prescriptionService = new PrescriptionService(prescriptionApi);
            var formulaTemplateService = new FormulaTemplateService(formulaTemplateApi);
            var logService = new LogService(logApi);
            var treatmentRoomService = new TreatmentRoomService(treatmentRoomApi);


            // 4. 用RegisterInstance注册，后续所有用IAuthService和IAuthApi的地方都能用  
            containerRegistry.RegisterInstance(authApi);
            containerRegistry.RegisterInstance(userApi);
            containerRegistry.RegisterInstance(doctorApi);
            containerRegistry.RegisterInstance(patientApi);
            containerRegistry.RegisterInstance(registrationApi);
            containerRegistry.RegisterInstance(herbApi);
            containerRegistry.RegisterInstance(pharmacyApi);
            containerRegistry.RegisterInstance(billingService);
            containerRegistry.RegisterInstance(prescriptionApi);
            containerRegistry.RegisterInstance(formulaTemplateApi);
            containerRegistry.RegisterInstance(logApi);
            containerRegistry.RegisterInstance(treatmentRoomApi);

            containerRegistry.RegisterInstance<IAuthService>(authService);
            containerRegistry.RegisterInstance<IUserService>(userService);
            containerRegistry.RegisterInstance<IDoctorService>(doctorService);
            containerRegistry.RegisterInstance<IPatientService>(patientService);
            containerRegistry.RegisterInstance<IHerbService>(herbService);

            containerRegistry.RegisterInstance<IPharmacyService>(pharmacyService);

            containerRegistry.RegisterInstance<IRegistrationService>(registrationService);

            containerRegistry.RegisterInstance<IBillingService>(billingService);
            containerRegistry.RegisterInstance<IPrescriptionService>(prescriptionService);
            containerRegistry.RegisterInstance<IFormulaTemplateService>(formulaTemplateService);
            containerRegistry.RegisterInstance<ILogService>(logService);
            containerRegistry.RegisterInstance<ITreatmentRoomService>(treatmentRoomService);
            // Register profile view models for dependency injection
            containerRegistry.Register<HerbProfileViewModel>();
            containerRegistry.Register<PrescriptionProfileViewModel>();
            containerRegistry.Register<FormulaTemplatesProfileViewModel>();
            containerRegistry.Register<DoctorProfileViewModel>();
            containerRegistry.Register<PatientProfileViewModel>();
            containerRegistry.Register<UserProfileViewModel>();
            // 5. 如果你还有其他API接口，也用同样方式new出来后RegisterInstance  
            containerRegistry.RegisterForNavigation<LoginView>("LoginView");
            containerRegistry.RegisterForNavigation<MainWindow>("MainWindow");
            containerRegistry.RegisterForNavigation<HomeView>("HomeView");
            containerRegistry.RegisterForNavigation<AdminView>("AdminView");
            containerRegistry.RegisterForNavigation<UserManagementView>("UserManagementView");
            containerRegistry.RegisterForNavigation<DoctorManagementView>("DoctorManagementView");
            containerRegistry.RegisterForNavigation<HerbManagementView>("HerbManagementView");
            containerRegistry.RegisterForNavigation<PrescriptionManagementView>("PrescriptionManagementView");
            containerRegistry.RegisterForNavigation<RecordManagementView>("RecordManagementView");
            containerRegistry.RegisterForNavigation<FormulaTemplatesManagementView>("FormulaTemplatesManagementView");
            containerRegistry.RegisterForNavigation<BillingStaffView>("BillingStaffView");
            containerRegistry.RegisterForNavigation<DiagnosingDoctorView>("DiagnosingDoctorView");
            containerRegistry.RegisterForNavigation<PharmacyStaffView>("PharmacyStaffView");
            containerRegistry.RegisterForNavigation<RegistrationStaffView>("RegistrationStaffView");
            containerRegistry.RegisterForNavigation<TreatmentDoctorView>("TreatmentDoctorView");
            containerRegistry.RegisterForNavigation<ChangePasswordView>("ChangePasswordView");
            containerRegistry.RegisterForNavigation<ChangeProfileView>("ChangeProfileView");
            containerRegistry.RegisterForNavigation<DoctorProfileView>("DoctorProfileView");
            containerRegistry.RegisterForNavigation<HerbProfileView>("HerbProfileView");
            containerRegistry.RegisterForNavigation<FormulaTemplatesProfileView>("FormulaTemplatesProfileView");
            containerRegistry.RegisterForNavigation<UserProfileView>("UserProfileView");

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
            System.Diagnostics.Debug.WriteLine("App.OnInitialized called");
            var regionManager = Container.Resolve<IRegionManager>();
            System.Diagnostics.Debug.WriteLine("Navigating to LoginView on startup");
            regionManager.RequestNavigate("FunctionRegion", "LoginView");
        }
    }
}
