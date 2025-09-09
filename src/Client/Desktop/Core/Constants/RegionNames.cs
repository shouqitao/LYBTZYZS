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
        public const string ConsultationWorkbenchContentRegion = "ConsultationWorkbenchContentRegion";

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
                "ConsultationWorkbench" => ConsultationWorkbenchContentRegion,
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
                "ConsultationWorkbenchMainView" => ConsultationWorkbenchContentRegion,
                _ => ContentRegion
            };
        }
    }
}
