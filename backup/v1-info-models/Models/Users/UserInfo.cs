using System;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;

namespace LYBT.Desktop.Core.Models.Users
{
    /// <summary>
    /// 用户信息模型 - 前端专用，继承共享基础模型
    /// UltraThink四层架构：Info层，包含UI状态和显示逻辑
    /// </summary>
    public class UserInfo : BaseUser
    {
        #region UI状态属性
        
        /// <summary>是否被选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }
        
        /// <summary>是否展开</summary>
        public bool IsExpanded { get; set; }
        
        /// <summary>是否正在编辑</summary>
        public bool IsEditing { get; set; }
        
        /// <summary>是否正在加载</summary>
        public bool IsLoading { get; set; }
        
        #endregion
        
        #region 显示逻辑属性
        
        /// <summary>显示名称</summary>
        public string DisplayName => string.IsNullOrEmpty(RealName) ? Username : RealName;

        /// <summary>状态文本</summary>
        public string StatusText => Status.GetDescription();
        
        /// <summary>角色文本</summary>
        public string RoleText => Role.GetDescription();
        
        /// <summary>完整显示名称（含用户名）</summary>
        public string FullDisplayName => string.IsNullOrEmpty(RealName) ? Username : $"{RealName}（{Username}）";
        
        /// <summary>状态颜色（用于UI显示）</summary>
        public string StatusColor => Status switch
        {
            CommonStatus.Enabled => "#4CAF50",    // 绿色
            CommonStatus.Disabled => "#F44336",   // 红色
            _ => "#9E9E9E"                         // 灰色
        };
        
        #endregion
        
        #region UI业务逻辑
        
        /// <summary>是否为系统管理员（基于用户名判断）</summary>
        public bool IsSysAdmin => Username == "sysadmin";
        
        /// <summary>是否可以编辑</summary>
        public bool CanEdit => Status == CommonStatus.Enabled && !IsSysAdmin;
        
        /// <summary>是否可以删除</summary>
        public bool CanDelete => !IsSysAdmin && Status != CommonStatus.Enabled;
        
        /// <summary>是否可以重置密码</summary>
        public bool CanResetPassword => Status == CommonStatus.Enabled;
        
        /// <summary>是否活跃用户</summary>
        public bool IsActive => Status == CommonStatus.Enabled;
        
        /// <summary>创建时间显示文本</summary>
        public string CreateTimeText => CreateTime.ToString("yyyy-MM-dd HH:mm");
        
        /// <summary>更新时间显示文本</summary>
        public string UpdateTimeText => UpdateTime?.ToString("yyyy-MM-dd HH:mm") ?? "从未更新";
        
        #endregion
    }
}