namespace LYBT.Desktop.Contracts.Models.Navigation;

/// <summary>
/// 患者管理导航参数工厂。
/// 消费方：<c>PatientMasterDetailViewModel</c>（经 PatientManagementView 转发）。
/// Action 取值："AddNew" | "Create" | "Search"（字面量，避免架构测试将非键常量误判为 NavParams 键）。
/// </summary>
public static class PatientManagementNav
{
    /// <summary>动作："AddNew" | "Create" | "Search"</summary>
    public const string Action = "Action";

    /// <summary>搜索关键词</summary>
    public const string SearchKeyword = "SearchKeyword";

    /// <summary>进入新建患者模式（Action=AddNew）</summary>
    public static Dictionary<string, object> AddNew()
        => new() { [Action] = "AddNew" };

    /// <summary>按关键词搜索患者（Action=Search + SearchKeyword）</summary>
    public static Dictionary<string, object> Search(string keyword)
        => new() { [Action] = "Search", [SearchKeyword] = keyword ?? string.Empty };
}
