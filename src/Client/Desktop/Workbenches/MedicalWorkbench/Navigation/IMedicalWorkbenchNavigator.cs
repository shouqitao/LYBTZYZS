namespace LYBT.Desktop.Workbench.Medical.Navigation
{

    /// <summary>
    /// 诊疗工作台导航接口
    /// 为医生提供专业的诊疗相关功能导航
    /// </summary>
    public interface IMedicalWorkbenchNavigator
    {

        /// <summary>
        /// 导航到患者管理（查看和快速注册）
        /// </summary>
        void NavigateToPatients();

        /// <summary>
        /// 导航到诊疗管理
        /// </summary>
        void NavigateToConsultations();

        /// <summary>
        /// 导航到医疗案例管理
        /// </summary>
        void NavigateToMedicalCases();

        /// <summary>
        /// 导航到处方管理
        /// </summary>
        void NavigateToPrescriptions();

        /// <summary>
        /// 导航到验方模板（供医生参考使用）
        /// </summary>
        void NavigateToFormulas();

        /// <summary>
        /// 导航到个人设置
        /// </summary>
        void NavigateToPersonalSettings();

        /// <summary>
        /// 通用导航方法
        /// </summary>
        /// <param name="viewName">视图名称</param>
        void NavigateToView(string viewName);

        /// <summary>
        /// 指定区域导航方法
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="viewName">视图名称</param>
        void NavigateToView(string regionName, string viewName);
    }
}
