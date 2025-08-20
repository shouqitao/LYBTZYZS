using System;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Extensions;

namespace LYBT.Desktop.Core.Models.Patients
{
    /// <summary>
    /// 患者信息模型 - 前端专用，继承共享基础模型
    /// UltraThink四层架构：Info层，包含UI状态和显示逻辑
    /// </summary>
    public class PatientInfo : BasePatient
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
        
        /// <summary>性别显示文本</summary>
        public string GenderDisplay 
        {
            get
            {
                return Gender switch
                {
                    Gender.Male => "男",
                    Gender.Female => "女",
                    _ => "未知"
                };
            }
        }
        
        /// <summary>状态文本</summary>
        public string StatusText => Status.GetDescription();
        
        /// <summary>年龄显示文本</summary>
        public string AgeText => Age > 0 ? $"{Age}岁" : "未知";
        
        /// <summary>完整显示名称（含年龄性别）</summary>
        public string FullDisplayName => $"{Name} {GenderDisplay} {AgeText}";
        
        /// <summary>状态颜色（用于UI显示）</summary>
        public string StatusColor => Status switch
        {
            CommonStatus.Enabled => "#4CAF50",    // 绿色
            CommonStatus.Disabled => "#F44336",   // 红色
            _ => "#9E9E9E"                         // 灰色
        };
        
        /// <summary>过敏信息显示</summary>
        public string AllergyDisplay => string.IsNullOrEmpty(AllergyHistory) ? "无过敏史" : AllergyHistory;
        
        #endregion
        
        #region UI业务逻辑
        
        /// <summary>是否有过敏史</summary>
        public bool HasAllergy => !string.IsNullOrEmpty(AllergyHistory);
        
        /// <summary>是否活跃患者</summary>
        public bool IsActive => Status == CommonStatus.Enabled;
        
        /// <summary>是否可以编辑</summary>
        public bool CanEdit => Status == CommonStatus.Enabled;
        
        /// <summary>是否可以删除</summary>
        public bool CanDelete => Status != CommonStatus.Enabled;
        
        /// <summary>创建时间显示文本</summary>
        public string CreateTimeText => CreateTime.ToString("yyyy-MM-dd HH:mm");
        
        /// <summary>更新时间显示文本</summary>
        public string UpdateTimeText => UpdateTime?.ToString("yyyy-MM-dd HH:mm") ?? "从未更新";
        
        #endregion

        #region 兼容性属性（保持向后兼容）
        
        /// <summary>电话号码（映射到PhoneNumber）</summary>
        public string? Phone 
        { 
            get => PhoneNumber; 
            set => PhoneNumber = value; 
        }
        
        /// <summary>出生日期（映射到BirthDate）</summary>
        public DateTime? DateOfBirth 
        { 
            get => BirthDate; 
            set => BirthDate = value; 
        }
        
        #endregion
    }
}