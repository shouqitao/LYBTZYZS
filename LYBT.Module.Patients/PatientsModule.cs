namespace LYBT.Module.Patients {
    /// <summary>
    /// Prism 模块初始化类（注册仓储和服务）
    /// </summary>
    public class PatientsModule : IModule {
        /// <summary>
        /// 模块初始化事件（目前无额外逻辑）
        /// </summary>
        public void OnInitialized(IContainerProvider containerProvider) {
            // 可用于初始化事件订阅、日志等
        }

        /// <summary>
        /// 注册服务和仓储实现（供依赖注入使用）
        /// </summary>
        public void RegisterTypes(IContainerRegistry containerRegistry) {
            //containerRegistry.Register<IPatientRepository, PatientRepository>(); // 数据访问
            //containerRegistry.Register<IPatientService, PatientService>();       // 业务逻辑
        }
    }
}
