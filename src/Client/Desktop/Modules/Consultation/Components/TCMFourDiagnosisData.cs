using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 中医四诊数据 - UltraThink v2.0 简化版
    /// 用于传输和保存四诊数据的DTO类
    /// </summary>
    public class TCMFourDiagnosisData
    {
        /// <summary>诊疗ID</summary>
        public Guid ConsultationId { get; set; }

        #region 望诊数据
        
        /// <summary>面色</summary>
        public string Complexion { get; set; } = "";
        
        /// <summary>神态</summary>
        public string Spirit { get; set; } = "";
        
        /// <summary>体型</summary>
        public string Build { get; set; } = "";
        
        /// <summary>舌象</summary>
        public string TongueBody { get; set; } = "";
        
        /// <summary>舌苔</summary>
        public string TongueCoating { get; set; } = "";

        #endregion

        #region 闻诊数据
        
        /// <summary>语音</summary>
        public string Voice { get; set; } = "";
        
        /// <summary>呼吸</summary>
        public string Breathing { get; set; } = "";
        
        /// <summary>咳嗽</summary>
        public string Cough { get; set; } = "";

        #endregion

        #region 问诊数据
        
        /// <summary>主诉</summary>
        public string ChiefComplaint { get; set; } = "";
        
        /// <summary>现病史</summary>
        public string PresentIllness { get; set; } = "";
        
        /// <summary>既往史</summary>
        public string PastHistory { get; set; } = "";
        
        /// <summary>寒热</summary>
        public string ColdHeat { get; set; } = "";
        
        /// <summary>汗出</summary>
        public string Sweating { get; set; } = "";
        
        /// <summary>二便</summary>
        public string Stools { get; set; } = "";

        #endregion

        #region 切诊数据
        
        /// <summary>脉象</summary>
        public string Pulse { get; set; } = "";
        
        /// <summary>脉率</summary>
        public string PulseRate { get; set; } = "";
        
        /// <summary>脉力</summary>
        public string PulseStrength { get; set; } = "";
        
        /// <summary>腹诊</summary>
        public string Abdomen { get; set; } = "";

        #endregion

        /// <summary>综合诊断</summary>
        public string Syndrome { get; set; } = "";
        
        /// <summary>治法</summary>
        public string TreatmentPrinciple { get; set; } = "";
        
        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;
        
        /// <summary>更新时间</summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;

        #region 综合描述属性（用于生成器）

        /// <summary>望诊综合描述</summary>
        public string Inspection { get; set; } = "";

        /// <summary>闻诊综合描述</summary>
        public string Auscultation { get; set; } = "";

        /// <summary>问诊综合描述</summary>
        public string Inquiry { get; set; } = "";

        /// <summary>切诊综合描述</summary>
        public string Palpation { get; set; } = "";

        /// <summary>舌诊综合描述</summary>
        public string TongueInspection { get; set; } = "";

        /// <summary>脉诊综合描述</summary>
        public string PulseCondition { get; set; } = "";

        #endregion
    }
}