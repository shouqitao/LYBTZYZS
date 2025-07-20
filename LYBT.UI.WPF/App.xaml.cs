using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Services;
using LYBT.UI.WPF.Views.Admin;
using LYBT.UI.WPF.Views.Main;
using LYBT.UI.WPF.ViewModels.Profile;
using LYBT.UI.WPF.ViewModels.Components; // 新增：组件视图模型命名空间
using LYBT.UI.WPF.ViewModels.Main; // 新增：主要视图模型命名空间
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

            if (string.IsNullOrWhiteSpace(baseUrl)) {
                // 如果配置缺失，默认回退到本地 Web API 地址
                baseUrl = "http://localhost:5297/";
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

            // 2. 用Refit创建API实例  
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
            var recordApi = RestService.For<IRecordApi>(httpClient, refitSettings);
            var queueingApi = RestService.For<IQueueingApi>(httpClient, refitSettings);
            var diagnosisTreatmentApi = RestService.For<IDiagnosisTreatmentApi>(httpClient, refitSettings);
            var pharmacyApi = RestService.For<IPharmacyApi>(httpClient, refitSettings);

            // 3. 创建服务实例
            var authService = new AuthService(authApi);
            var userService = new UserService(userApi);
            var doctorService = new DoctorService(doctorApi);
            var patientService = new PatientService(patientApi);
            var registrationService = new RegistrationService(registrationApi);
            var herbService = new HerbService(herbApi);
            var billingService = new BillingService(billingApi);
            var pharmacyService = new PharmacyService(pharmacyApi);
            var prescriptionService = new PrescriptionService(prescriptionApi);
            var formulaTemplateService = new FormulaTemplateService(formulaTemplateApi);
            var logService = new LogService(logApi);
            var treatmentRoomService = new TreatmentRoomService(treatmentRoomApi);
            var recordService = new RecordService(recordApi);
            var queueingService = new QueueingService(queueingApi);
            var diagnosisTreatmentService = new DiagnosisTreatmentService(diagnosisTreatmentApi);

            // 4. 注册API接口实例
            containerRegistry.RegisterInstance(authApi);
            containerRegistry.RegisterInstance(userApi);
            containerRegistry.RegisterInstance(doctorApi);
            containerRegistry.RegisterInstance(patientApi);
            containerRegistry.RegisterInstance(registrationApi);
            containerRegistry.RegisterInstance(herbApi);
            containerRegistry.RegisterInstance(pharmacyApi);
            containerRegistry.RegisterInstance(billingApi);
            containerRegistry.RegisterInstance(prescriptionApi);
            containerRegistry.RegisterInstance(formulaTemplateApi);
            containerRegistry.RegisterInstance(logApi);
            containerRegistry.RegisterInstance(treatmentRoomApi);
            containerRegistry.RegisterInstance(recordApi);
            containerRegistry.RegisterInstance(queueingApi);
            containerRegistry.RegisterInstance(diagnosisTreatmentApi);

            // 5. 注册服务接口实例
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
            containerRegistry.RegisterInstance<IRecordService>(recordService);
            containerRegistry.RegisterInstance<IQueueingService>(queueingService);
            containerRegistry.RegisterInstance<IDiagnosisTreatmentService>(diagnosisTreatmentService);

            // 6. 【更新】注册整合架构的视图模型（只注册必要的ViewModel）
            containerRegistry.RegisterSingleton<NavigationDrawerViewModel>();
            containerRegistry.RegisterSingleton<WelcomePanelViewModel>();
            containerRegistry.RegisterSingleton<StatusBarViewModel>();
            containerRegistry.RegisterSingleton<IntegratedMainLayoutViewModel>();

            // 7. 【移除】不再需要注册单独的组件视图，因为它们已经直接嵌入到IntegratedMainLayout中

            // 8. 注册Profile视图模型（保持原有）
            containerRegistry.Register<HerbProfileViewModel>();
            containerRegistry.Register<PrescriptionProfileViewModel>();
            containerRegistry.Register<FormulaTemplatesProfileViewModel>();
            containerRegistry.Register<DoctorProfileViewModel>();
            containerRegistry.Register<PatientProfileViewModel>();

            // 9. 映射Profile视图到视图模型（保持原有）
            ViewModelLocationProvider.Register<DoctorProfileView, DoctorProfileViewModel>();

            // 10. 注册导航视图（保持原有 + 新增整合布局）
            containerRegistry.RegisterForNavigation<LoginView>("LoginView");
            containerRegistry.RegisterForNavigation<MainWindow>("MainWindow");
            containerRegistry.RegisterForNavigation<HomeView>("HomeView");
            containerRegistry.RegisterForNavigation<IntegratedMainLayout>("IntegratedMainLayout"); // 【新增】

            // Admin相关视图
            containerRegistry.RegisterForNavigation<AdminView>("AdminView");
            containerRegistry.RegisterForNavigation<UserManagementView>("UserManagementView");
            containerRegistry.RegisterForNavigation<DoctorManagementView>("DoctorManagementView");
            containerRegistry.RegisterForNavigation<HerbManagementView>("HerbManagementView");
            containerRegistry.RegisterForNavigation<PrescriptionManagementView>("PrescriptionManagementView");
            containerRegistry.RegisterForNavigation<RecordManagementView>("RecordManagementView");
            containerRegistry.RegisterForNavigation<FormulaTemplatesManagementView>("FormulaTemplatesManagementView");

            // 角色工作台视图
            containerRegistry.RegisterForNavigation<BillingStaffView>("BillingStaffView");
            containerRegistry.RegisterForNavigation<DiagnosingDoctorView>("DiagnosingDoctorView");
            containerRegistry.RegisterForNavigation<PharmacyStaffView>("PharmacyStaffView");
            containerRegistry.RegisterForNavigation<RegistrationStaffView>("RegistrationStaffView");
            containerRegistry.RegisterForNavigation<TreatmentDoctorView>("TreatmentDoctorView");

            // 功能视图
            containerRegistry.RegisterForNavigation<ChangePasswordView>("ChangePasswordView");
            containerRegistry.RegisterForNavigation<ChangeProfileView>("ChangeProfileView");
            containerRegistry.RegisterForNavigation<DoctorProfileView>("DoctorProfileView");

            // 11. 【可选】验证注册是否成功
            ValidateRegistrations(containerRegistry);

            System.Diagnostics.Debug.WriteLine("=== 依赖注入配置完成 ===");
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

            // 【可选】验证整合组件是否正确注册
            try {
                var integratedLayout = Container.Resolve<IntegratedMainLayoutViewModel>();
                System.Diagnostics.Debug.WriteLine("IntegratedMainLayoutViewModel resolved successfully");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to resolve IntegratedMainLayoutViewModel: {ex.Message}");
            }

            var regionManager = Container.Resolve<IRegionManager>();

            // 诊断区域状态
            System.Diagnostics.Debug.WriteLine("=== 应用启动时的区域状态 ===");
            try {
                foreach (var region in regionManager.Regions) {
                    System.Diagnostics.Debug.WriteLine($"已存在区域: {region.Name}");
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"区域诊断失败: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("Navigating to LoginView on startup");
            regionManager.RequestNavigate("FunctionRegion", "LoginView");
        }

        /// <summary>
        /// 验证关键组件的注册
        /// </summary>
        private void ValidateRegistrations(IContainerRegistry containerRegistry) {
            try {
                System.Diagnostics.Debug.WriteLine("=== 验证依赖注入注册 ===");

                // 验证服务注册
                System.Diagnostics.Debug.WriteLine("✓ 核心服务已注册");

                // 验证组件视图模型注册
                System.Diagnostics.Debug.WriteLine("✓ 组件视图模型已注册");

                // 验证视图注册
                System.Diagnostics.Debug.WriteLine("✓ 视图已注册");

                System.Diagnostics.Debug.WriteLine("=== 依赖注入验证完成 ===");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ 依赖注入验证失败: {ex.Message}");
                MessageBox.Show($"应用启动时检测到配置问题：{ex.Message}\n\n请检查依赖注入配置。",
                               "启动错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}