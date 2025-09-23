namespace LYBT.Desktop.Core.Constants
{

    /// <summary>
    /// 统一的Region名称常量
    /// </summary>
    public static class RegionNames
    {

        /// <summary>
        /// 主窗口登录区域
        /// </summary>
        public const string LoginRegion = "LoginRegion";

        /// <summary>
        /// 主窗口内容区域（工作台容器）
        /// </summary>
        public const string ContentRegion = "ContentRegion";

        /// <summary>
        /// 系统工作台内容区域
        /// </summary>
        public const string SystemWorkbenchContentRegion = "SystemWorkbenchContentRegion";

        /// <summary>
        /// 诊疗工作台内容区域
        /// </summary>
        public const string MedicalWorkbenchContentRegion = "MedicalWorkbenchContentRegion";

        /// <summary>
        /// 主Shell内容区域
        /// </summary>
        public const string ShellContent = "ShellContentRegion";
        
        /// <summary>
        /// 诊疗工作台查询区域
        /// </summary>
        public const string MedicalWorkbenchQuery = "MedicalWorkbenchQueryRegion";
        
        /// <summary>
        /// 工作流内容区域
        /// </summary>
        public const string WorkflowContent = "WorkflowContentRegion";
        
        /// <summary>
        /// 工作流步骤区域
        /// </summary>
        public const string WorkflowSteps = "WorkflowStepsRegion";
        
        /// <summary>
        /// 对话框内容区域
        /// </summary>
        public const string DialogContent = "DialogContentRegion";
        
        /// <summary>
        /// 导航菜单区域
        /// </summary>
        public const string NavigationMenu = "NavigationMenuRegion";
        
        /// <summary>
        /// 工具栏区域
        /// </summary>
        public const string Toolbar = "ToolbarRegion";
        
        /// <summary>
        /// 主要内容区域别名
        /// </summary>
        public const string MainContent = ContentRegion;

        /// <summary>
        /// 获取工作台内容区域名称
        /// </summary>
        /// <param name="workbenchType">工作台类型</param>
        /// <returns>对应的Region名称</returns>
        public static string GetWorkbenchContentRegion(string workbenchType)
        {
            return workbenchType switch
            {
                "SystemWorkbench" => SystemWorkbenchContentRegion,
                "MedicalWorkbench" => MedicalWorkbenchContentRegion,
                _ => ContentRegion // 默认返回主内容区域
            };
        }

        /// <summary>
        /// 根据工作台视图名称获取内容区域名称
        /// </summary>
        /// <param name="workbenchViewName">工作台视图名称</param>
        /// <returns>对应的内容区域名称</returns>
        public static string GetContentRegionByWorkbenchView(string workbenchViewName)
        {
            return workbenchViewName switch
            {
                "SystemWorkbenchMainView" => SystemWorkbenchContentRegion,
                "MedicalWorkbenchMainView" => MedicalWorkbenchContentRegion,
                _ => ContentRegion
            };
        }
    }
}
