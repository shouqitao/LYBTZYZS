namespace LYBT.Desktop.Foundation.Http;

using System.Text;

/// <summary>
/// URL 查询字符串构建器
/// </summary>
internal static class QueryStringBuilder
{
    /// <summary>
    /// 构建带查询参数的 URL
    /// </summary>
    public static string Build(string baseUrl, IDictionary<string, object?>? parameters = null)
    {
        if (parameters == null || parameters.Count == 0)
            return baseUrl;

        var sb = new StringBuilder(baseUrl);
        var first = !baseUrl.Contains('?');

        foreach (var kv in parameters.Where(kv => kv.Value != null))
        {
            sb.Append(first ? '?' : '&');
            sb.Append(Uri.EscapeDataString(kv.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(kv.Value!.ToString()!));
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建带分页参数的 URL
    /// </summary>
    public static string BuildPaged(string baseUrl, int page, int pageSize,
        IDictionary<string, object?>? extra = null)
    {
        extra ??= new Dictionary<string, object?>();
        extra["page"] = page;
        extra["pageSize"] = pageSize;
        return Build(baseUrl, extra);
    }
}
