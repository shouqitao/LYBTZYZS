using System;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using LYBT.WPF.Client.Modules.Consultation.ViewModels;

namespace LYBT.WPF.Client.Modules.Consultation.Components
{
    /// <summary>
    /// 中医四诊数据管理器 - UltraThink重构专门组件
    /// 专门负责管理所有四诊数据属性和选项集合
    /// </summary>
    public class TCMFourDiagnosisDataManager : BindableBase
    {
        #region 基础属性

        private Guid _consultationId;
        public Guid ConsultationId
        {
            get => _consultationId;
            set => SetProperty(ref _consultationId, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private ObservableCollection<string> _recommendedSyndromes = new();
        public ObservableCollection<string> RecommendedSyndromes
        {
            get => _recommendedSyndromes;
            set => SetProperty(ref _recommendedSyndromes, value);
        }

        #endregion

        #region 望诊属性

        private string _complexion = ""; // 面色
        public string Complexion
        {
            get => _complexion;
            set => SetProperty(ref _complexion, value);
        }

        private string _spirit = ""; // 神态
        public string Spirit
        {
            get => _spirit;
            set => SetProperty(ref _spirit, value);
        }

        private string _bodyShape = ""; // 形态
        public string BodyShape
        {
            get => _bodyShape;
            set => SetProperty(ref _bodyShape, value);
        }

        private string _tongueBody = ""; // 舌质
        public string TongueBody
        {
            get => _tongueBody;
            set => SetProperty(ref _tongueBody, value);
        }

        private string _tongueCoating = ""; // 舌苔
        public string TongueCoating
        {
            get => _tongueCoating;
            set => SetProperty(ref _tongueCoating, value);
        }

        #endregion

        #region 闻诊属性

        private string _voice = ""; // 声音
        public string Voice
        {
            get => _voice;
            set => SetProperty(ref _voice, value);
        }

        private string _breath = ""; // 呼吸
        public string Breath
        {
            get => _breath;
            set => SetProperty(ref _breath, value);
        }

        private string _cough = ""; // 咳嗽
        public string Cough
        {
            get => _cough;
            set => SetProperty(ref _cough, value);
        }

        private string _odor = ""; // 气味
        public string Odor
        {
            get => _odor;
            set => SetProperty(ref _odor, value);
        }

        #endregion

        #region 问诊属性

        private string _chiefComplaint = ""; // 主诉
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set => SetProperty(ref _chiefComplaint, value);
        }

        private string _presentIllness = ""; // 现病史
        public string PresentIllness
        {
            get => _presentIllness;
            set => SetProperty(ref _presentIllness, value);
        }

        private string _coldHeat = ""; // 寒热
        public string ColdHeat
        {
            get => _coldHeat;
            set => SetProperty(ref _coldHeat, value);
        }

        private string _sweat = ""; // 汗
        public string Sweat
        {
            get => _sweat;
            set => SetProperty(ref _sweat, value);
        }

        private string _headBody = ""; // 头身
        public string HeadBody
        {
            get => _headBody;
            set => SetProperty(ref _headBody, value);
        }

        private string _chestAbdomen = ""; // 胸腹
        public string ChestAbdomen
        {
            get => _chestAbdomen;
            set => SetProperty(ref _chestAbdomen, value);
        }

        private string _appetite = ""; // 饮食
        public string Appetite
        {
            get => _appetite;
            set => SetProperty(ref _appetite, value);
        }

        private string _stoolUrine = ""; // 二便
        public string StoolUrine
        {
            get => _stoolUrine;
            set => SetProperty(ref _stoolUrine, value);
        }

        private string _sleep = ""; // 睡眠
        public string Sleep
        {
            get => _sleep;
            set => SetProperty(ref _sleep, value);
        }

        private string _menstruation = ""; // 月经（女性）
        public string Menstruation
        {
            get => _menstruation;
            set => SetProperty(ref _menstruation, value);
        }

        #endregion

        #region 切诊属性

        private string _pulseRate = ""; // 脉率
        public string PulseRate
        {
            get => _pulseRate;
            set => SetProperty(ref _pulseRate, value);
        }

        private string _pulseRhythm = ""; // 脉律
        public string PulseRhythm
        {
            get => _pulseRhythm;
            set => SetProperty(ref _pulseRhythm, value);
        }

        private string _pulseStrength = ""; // 脉力
        public string PulseStrength
        {
            get => _pulseStrength;
            set => SetProperty(ref _pulseStrength, value);
        }

        private string _pulseShape = ""; // 脉形
        public string PulseShape
        {
            get => _pulseShape;
            set => SetProperty(ref _pulseShape, value);
        }

        private string _leftPulse = ""; // 左脉
        public string LeftPulse
        {
            get => _leftPulse;
            set => SetProperty(ref _leftPulse, value);
        }

        private string _rightPulse = ""; // 右脉
        public string RightPulse
        {
            get => _rightPulse;
            set => SetProperty(ref _rightPulse, value);
        }

        private string _palpation = ""; // 按诊
        public string Palpation
        {
            get => _palpation;
            set => SetProperty(ref _palpation, value);
        }

        #endregion

        #region 综合诊断

        private string _tcmSyndrome = ""; // 中医证型
        public string TCMSyndrome
        {
            get => _tcmSyndrome;
            set => SetProperty(ref _tcmSyndrome, value);
        }

        private string _treatmentPrinciple = ""; // 治法
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set => SetProperty(ref _treatmentPrinciple, value);
        }

        #endregion

        #region 常用选项集合

        // 面色选项
        public ObservableCollection<string> ComplexionOptions { get; } = new ObservableCollection<string>
        {
            "红润", "苍白", "萎黄", "晦暗", "潮红", "青紫"
        };

        // 舌质选项
        public ObservableCollection<string> TongueBodyOptions { get; } = new ObservableCollection<string>
        {
            "淡红", "淡白", "红", "绛", "紫暗", "有瘀斑"
        };

        // 舌苔选项
        public ObservableCollection<string> TongueCoatingOptions { get; } = new ObservableCollection<string>
        {
            "薄白", "薄黄", "厚白", "厚黄", "白腻", "黄腻", "少苔", "无苔"
        };

        // 脉象选项
        public ObservableCollection<string> PulseOptions { get; } = new ObservableCollection<string>
        {
            "浮", "沉", "迟", "数", "虚", "实", "滑", "涩", "弦", "紧", "缓", "洪", "细", "弱"
        };

        // 常见证型
        public ObservableCollection<string> SyndromeOptions { get; } = new ObservableCollection<string>
        {
            "风寒感冒", "风热感冒", "暑湿感冒", "气虚感冒",
            "肝郁气滞", "肝火上炎", "肝阳上亢", "肝风内动",
            "脾胃虚寒", "脾胃湿热", "食积内停", "胃阴不足",
            "肺热咳嗽", "肺寒咳嗽", "燥邪犯肺", "痰湿蕴肺",
            "肾阳虚", "肾阴虚", "肾气不固", "肾精不足",
            "心血虚", "心阴虚", "心火亢盛", "心脾两虚",
            "气虚", "血虚", "阴虚", "阳虚", "气滞", "血瘀", "痰湿", "湿热"
        };

        #endregion

        #region 公共方法

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void ClearAllData()
        {
            // 清空望诊
            Complexion = "";
            Spirit = "";
            BodyShape = "";
            TongueBody = "";
            TongueCoating = "";
            
            // 清空闻诊
            Voice = "";
            Breath = "";
            Cough = "";
            Odor = "";
            
            // 清空问诊
            ChiefComplaint = "";
            PresentIllness = "";
            ColdHeat = "";
            Sweat = "";
            HeadBody = "";
            ChestAbdomen = "";
            Appetite = "";
            StoolUrine = "";
            Sleep = "";
            Menstruation = "";
            
            // 清空切诊
            PulseRate = "";
            PulseRhythm = "";
            PulseStrength = "";
            PulseShape = "";
            LeftPulse = "";
            RightPulse = "";
            Palpation = "";
            
            // 清空诊断
            TCMSyndrome = "";
            TreatmentPrinciple = "";

            // 清空推荐证型
            RecommendedSyndromes.Clear();
        }

        /// <summary>
        /// 应用模板数据
        /// </summary>
        public void ApplyTemplateData(TCMTemplate templateData)
        {
            if (templateData == null) return;

            // 望诊
            Complexion = templateData.Complexion ?? Complexion;
            Spirit = templateData.Spirit ?? Spirit;
            TongueBody = templateData.TongueBody ?? TongueBody;
            TongueCoating = templateData.TongueCoating ?? TongueCoating;
            
            // 闻诊
            Voice = templateData.Voice ?? Voice;
            Breath = templateData.Breath ?? Breath;
            Cough = templateData.Cough ?? Cough;
            
            // 问诊
            ChiefComplaint = templateData.ChiefComplaint ?? ChiefComplaint;
            ColdHeat = templateData.ColdHeat ?? ColdHeat;
            Sweat = templateData.Sweat ?? Sweat;
            
            // 切诊
            LeftPulse = templateData.LeftPulse ?? LeftPulse;
            RightPulse = templateData.RightPulse ?? RightPulse;
            
            // 诊断
            TCMSyndrome = templateData.Syndrome ?? TCMSyndrome;
            TreatmentPrinciple = templateData.TreatmentPrinciple ?? TreatmentPrinciple;
        }

        #endregion
    }
}