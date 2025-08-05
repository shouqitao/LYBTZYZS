namespace LYBT.WPF.Client.Core.Models.Settings
{
    /// <summary>
    /// 设置信息模型 - 前端专用
    /// </summary>
    public class SettingInfo
    {
        /// <summary>主键ID</summary>
        public Guid Id { get; set; }

        /// <summary>设置键（唯一标识）</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>设置值</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>中文说明</summary>
        public string? Description { get; set; }

        /// <summary>数据类型</summary>
        public string ValueType { get; set; } = "string";

        /// <summary>设置分组</summary>
        public string? Group { get; set; }

        /// <summary>排序序号</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>是否为系统设置</summary>
        public bool IsSystem { get; set; } = false;

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>分组显示名称（前端显示字段）</summary>
        public string GroupDisplayName => GetGroupDisplayName();

        /// <summary>数据类型显示名称（前端显示字段）</summary>
        public string ValueTypeDisplayName => GetValueTypeDisplayName();

        /// <summary>系统设置标识（前端显示字段）</summary>
        public string SystemText => IsSystem ? "系统" : "自定义";

        /// <summary>系统设置颜色（前端显示字段）</summary>
        public string SystemColor => IsSystem ? "#FFC107" : "#28A745";

        /// <summary>启用状态文本（前端显示字段）</summary>
        public string EnabledText => IsEnabled ? "启用" : "禁用";

        /// <summary>启用状态颜色（前端显示字段）</summary>
        public string EnabledColor => IsEnabled ? "#28A745" : "#DC3545";

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        private string GetGroupDisplayName()
        {
            return Group switch
            {
                "System" => "系统设置",
                "Database" => "数据库设置",
                "Performance" => "性能设置",
                "Security" => "安全设置",
                "Backup" => "备份设置",
                "Log" => "日志设置",
                "UI" => "界面设置",
                "Business" => "业务设置",
                _ => Group ?? "未分组"
            };
        }

        private string GetValueTypeDisplayName()
        {
            return ValueType switch
            {
                "string" => "字符串",
                "int" => "整数",
                "bool" => "布尔值",
                "decimal" => "小数",
                "datetime" => "日期时间",
                "json" => "JSON对象",
                _ => ValueType
            };
        }
    }
}