using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Consultation.ViewModels;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 中医四诊模板管理器 - UltraThink重构专门组件
    /// 专门负责管理和应用快速输入模板
    /// </summary>
    public class TCMFourDiagnosisTemplateManager
    {
        #region 模板数据

        private readonly Dictionary<string, TCMTemplate> _templates = new();

        #endregion

        #region 构造函数

        public TCMFourDiagnosisTemplateManager()
        {
            InitializeDefaultTemplates();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取模板
        /// </summary>
        public TCMTemplate? GetTemplate(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName)) return null;
            return _templates.TryGetValue(templateName, out var template) ? template : null;
        }

        /// <summary>
        /// 获取所有可用模板名称
        /// </summary>
        public IEnumerable<string> GetAvailableTemplates()
        {
            return _templates.Keys;
        }

        /// <summary>
        /// 应用模板到数据管理器
        /// </summary>
        public bool ApplyTemplate(string templateName, TCMFourDiagnosisDataManager dataManager)
        {
            if (string.IsNullOrWhiteSpace(templateName) || dataManager == null) 
                return false;

            var templateData = GetTemplate(templateName);
            if (templateData == null) return false;

            dataManager.ApplyTemplateData(templateData);
            return true;
        }

        /// <summary>
        /// 添加自定义模板
        /// </summary>
        public void AddCustomTemplate(string templateName, TCMTemplate template)
        {
            if (string.IsNullOrWhiteSpace(templateName) || template == null) return;
            
            _templates[templateName] = template;
        }

        /// <summary>
        /// 从当前数据创建模板
        /// </summary>
        public TCMTemplate CreateTemplateFromCurrentData(TCMFourDiagnosisDataManager dataManager)
        {
            if (dataManager == null) return new TCMTemplate();

            return new TCMTemplate
            {
                ChiefComplaint = dataManager.ChiefComplaint,
                Complexion = dataManager.Complexion,
                Spirit = dataManager.Spirit,
                BodyShape = dataManager.BodyShape,
                TongueBody = dataManager.TongueBody,
                TongueCoating = dataManager.TongueCoating,
                Voice = dataManager.Voice,
                Breath = dataManager.Breath,
                Cough = dataManager.Cough,
                ColdHeat = dataManager.ColdHeat,
                Sweat = dataManager.Sweat,
                LeftPulse = dataManager.LeftPulse,
                RightPulse = dataManager.RightPulse,
                Syndrome = dataManager.TCMSyndrome,
                TreatmentPrinciple = dataManager.TreatmentPrinciple
            };
        }

        /// <summary>
        /// 删除自定义模板
        /// </summary>
        public bool RemoveCustomTemplate(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName)) return false;
            
            // 保护默认模板不被删除
            var defaultTemplates = new[] { "风寒感冒", "风热感冒", "脾胃虚弱", "肾虚证", "肝郁气滞" };
            if (defaultTemplates.Contains(templateName)) return false;

            return _templates.Remove(templateName);
        }

        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        public bool TemplateExists(string templateName)
        {
            return !string.IsNullOrWhiteSpace(templateName) && _templates.ContainsKey(templateName);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化默认模板
        /// </summary>
        private void InitializeDefaultTemplates()
        {
            _templates["风寒感冒"] = new TCMTemplate
            {
                ChiefComplaint = "恶寒重，发热轻，头痛，鼻塞，流清涕",
                Complexion = "苍白",
                Spirit = "精神疲惫",
                Voice = "声音低微",
                ColdHeat = "恶寒重，发热轻",
                TongueBody = "淡红",
                TongueCoating = "薄白",
                LeftPulse = "浮紧",
                RightPulse = "浮紧",
                Syndrome = "风寒束表证",
                TreatmentPrinciple = "辛温解表，宣肺散寒"
            };

            _templates["风热感冒"] = new TCMTemplate
            {
                ChiefComplaint = "发热重，恶寒轻，头痛，咽痛，口渴",
                Complexion = "面色潮红",
                Voice = "声音咳嗄",
                ColdHeat = "发热重，恶寒轻",
                TongueBody = "红",
                TongueCoating = "薄黄",
                LeftPulse = "浮数",
                RightPulse = "浮数",
                Syndrome = "风热犯表证",
                TreatmentPrinciple = "辛凉解表，清热宣肺"
            };

            _templates["脾胃虚弱"] = new TCMTemplate
            {
                ChiefComplaint = "食欲不振，腹胀，便溏，乏力",
                Complexion = "萎黄",
                Spirit = "神疲乏力",
                ColdHeat = "喜温怕凉",
                Appetite = "食少纳呆",
                StoolUrine = "便溏不成形",
                TongueBody = "淡白",
                TongueCoating = "白腻",
                LeftPulse = "沉弱",
                RightPulse = "沉弱",
                Syndrome = "脾胃虚弱证",
                TreatmentPrinciple = "健脾益胃，补中益气"
            };

            _templates["肾虚证"] = new TCMTemplate
            {
                ChiefComplaint = "腰膝酸软，耳鸣，夜尿频多",
                Complexion = "面色晦暗",
                Spirit = "精神萎靡",
                HeadBody = "腰膝酸软",
                Sleep = "失眠多梦",
                StoolUrine = "夜尿频多",
                TongueBody = "淡红",
                TongueCoating = "少苔",
                LeftPulse = "沉细",
                RightPulse = "沉细",
                Syndrome = "肾虚证",
                TreatmentPrinciple = "补肾固本，滋阴壮阳"
            };

            _templates["肝郁气滞"] = new TCMTemplate
            {
                ChiefComplaint = "胸胁胀痛，情志不舒，易怒",
                Spirit = "情志不舒",
                ChestAbdomen = "胸胁胀满",
                Appetite = "食欲时好时差",
                Sleep = "失眠易醒",
                TongueBody = "红",
                TongueCoating = "薄白",
                LeftPulse = "弦",
                RightPulse = "弦",
                Syndrome = "肝郁气滞证",
                TreatmentPrinciple = "疏肝理气，调畅气机"
            };

            _templates["血瘀证"] = new TCMTemplate
            {
                ChiefComplaint = "胸痛，刺痛固定不移",
                Complexion = "面色晦暗",
                TongueBody = "紫暗",
                TongueCoating = "薄白",
                LeftPulse = "涩",
                RightPulse = "涩",
                Syndrome = "血瘀证",
                TreatmentPrinciple = "活血化瘀，理气止痛"
            };

            _templates["痰湿蕴肺"] = new TCMTemplate
            {
                ChiefComplaint = "咳嗽痰多，痰白粘腻",
                Breath = "气短胸闷",
                Cough = "咳嗽痰多",
                TongueBody = "淡红",
                TongueCoating = "白腻",
                LeftPulse = "滑",
                RightPulse = "滑",
                Syndrome = "痰湿蕴肺证",
                TreatmentPrinciple = "燥湿化痰，宣肺止咳"
            };
        }

        #endregion
    }
}