using System;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 中医诊疗模板 - UltraThink v2.0 简化版
    /// 用于保存和应用常用诊疗模板数据
    /// </summary>
    public class TCMTemplate
    {
        /// <summary>模板ID</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>模板名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>模板类型</summary>
        public string Type { get; set; } = string.Empty;

        #region 望诊模板数据
        
        /// <summary>面色</summary>
        public string? Complexion { get; set; }
        
        /// <summary>神态</summary>
        public string? Spirit { get; set; }

        /// <summary>体型</summary>
        public string? BodyShape { get; set; }
        
        /// <summary>舌质</summary>
        public string? TongueBody { get; set; }
        
        /// <summary>舌苔</summary>
        public string? TongueCoating { get; set; }

        #endregion

        #region 闻诊模板数据
        
        /// <summary>声音</summary>
        public string? Voice { get; set; }
        
        /// <summary>呼吸</summary>
        public string? Breath { get; set; }
        
        /// <summary>咳嗽</summary>
        public string? Cough { get; set; }

        #endregion

        #region 问诊模板数据
        
        /// <summary>主诉</summary>
        public string? ChiefComplaint { get; set; }
        
        /// <summary>寒热</summary>
        public string? ColdHeat { get; set; }
        
        /// <summary>汗出</summary>
        public string? Sweat { get; set; }

        /// <summary>头身</summary>
        public string? HeadBody { get; set; }

        /// <summary>胸腹</summary>
        public string? ChestAbdomen { get; set; }

        /// <summary>饮食</summary>
        public string? Appetite { get; set; }

        /// <summary>二便</summary>
        public string? StoolUrine { get; set; }

        /// <summary>睡眠</summary>
        public string? Sleep { get; set; }

        #endregion

        #region 切诊模板数据
        
        /// <summary>左脉</summary>
        public string? LeftPulse { get; set; }
        
        /// <summary>右脉</summary>
        public string? RightPulse { get; set; }

        #endregion

        #region 诊断模板数据
        
        /// <summary>证型</summary>
        public string? Syndrome { get; set; }
        
        /// <summary>治法</summary>
        public string? TreatmentPrinciple { get; set; }

        #endregion

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>备注</summary>
        public string Remark { get; set; } = string.Empty;
    }
}