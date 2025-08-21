using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LYBT.Desktop.Consultation.Components;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 中医四诊统一服务 - UltraThink Phase 1: 合并望诊、闻诊、问诊、切诊功能
    /// 整合了 TCMInspectionService + TCMAuscultationService + 四诊数据处理
    /// </summary>
    public class TCMDiagnosisService : INotifyPropertyChanged
    {
        #region 望诊属性 (来自 TCMInspectionService)

        private string _complexion = ""; // 面色
        public string Complexion
        {
            get => _complexion;
            set
            {
                if (_complexion != value)
                {
                    _complexion = value;
                    OnPropertyChanged(nameof(Complexion));
                }
            }
        }

        private string _spirit = ""; // 神态
        public string Spirit
        {
            get => _spirit;
            set
            {
                if (_spirit != value)
                {
                    _spirit = value;
                    OnPropertyChanged(nameof(Spirit));
                }
            }
        }

        private string _bodyShape = ""; // 形体
        public string BodyShape
        {
            get => _bodyShape;
            set
            {
                if (_bodyShape != value)
                {
                    _bodyShape = value;
                    OnPropertyChanged(nameof(BodyShape));
                }
            }
        }

        private string _tongueBody = ""; // 舌体
        public string TongueBody
        {
            get => _tongueBody;
            set
            {
                if (_tongueBody != value)
                {
                    _tongueBody = value;
                    OnPropertyChanged(nameof(TongueBody));
                }
            }
        }

        private string _tongueCoating = ""; // 舌苔
        public string TongueCoating
        {
            get => _tongueCoating;
            set
            {
                if (_tongueCoating != value)
                {
                    _tongueCoating = value;
                    OnPropertyChanged(nameof(TongueCoating));
                }
            }
        }

        private string _skinCondition = ""; // 皮肤状态
        public string SkinCondition
        {
            get => _skinCondition;
            set
            {
                if (_skinCondition != value)
                {
                    _skinCondition = value;
                    OnPropertyChanged(nameof(SkinCondition));
                }
            }
        }

        #endregion

        #region 闻诊属性 (来自 TCMAuscultationService)

        private string _voice = ""; // 声音
        public string Voice
        {
            get => _voice;
            set
            {
                if (_voice != value)
                {
                    _voice = value;
                    OnPropertyChanged(nameof(Voice));
                }
            }
        }

        private string _breathing = ""; // 呼吸
        public string Breathing
        {
            get => _breathing;
            set
            {
                if (_breathing != value)
                {
                    _breathing = value;
                    OnPropertyChanged(nameof(Breathing));
                }
            }
        }

        private string _cough = ""; // 咳嗽
        public string Cough
        {
            get => _cough;
            set
            {
                if (_cough != value)
                {
                    _cough = value;
                    OnPropertyChanged(nameof(Cough));
                }
            }
        }

        private string _bodyOdor = ""; // 体味
        public string BodyOdor
        {
            get => _bodyOdor;
            set
            {
                if (_bodyOdor != value)
                {
                    _bodyOdor = value;
                    OnPropertyChanged(nameof(BodyOdor));
                }
            }
        }

        private string _breathOdor = ""; // 口气
        public string BreathOdor
        {
            get => _breathOdor;
            set
            {
                if (_breathOdor != value)
                {
                    _breathOdor = value;
                    OnPropertyChanged(nameof(BreathOdor));
                }
            }
        }

        #endregion

        #region 问诊属性

        private string _chiefComplaint = ""; // 主诉
        public string ChiefComplaint
        {
            get => _chiefComplaint;
            set
            {
                if (_chiefComplaint != value)
                {
                    _chiefComplaint = value;
                    OnPropertyChanged(nameof(ChiefComplaint));
                }
            }
        }

        private string _presentIllness = ""; // 现病史
        public string PresentIllness
        {
            get => _presentIllness;
            set
            {
                if (_presentIllness != value)
                {
                    _presentIllness = value;
                    OnPropertyChanged(nameof(PresentIllness));
                }
            }
        }

        private string _pastHistory = ""; // 既往史
        public string PastHistory
        {
            get => _pastHistory;
            set
            {
                if (_pastHistory != value)
                {
                    _pastHistory = value;
                    OnPropertyChanged(nameof(PastHistory));
                }
            }
        }

        private string _coldHeat = ""; // 寒热
        public string ColdHeat
        {
            get => _coldHeat;
            set
            {
                if (_coldHeat != value)
                {
                    _coldHeat = value;
                    OnPropertyChanged(nameof(ColdHeat));
                }
            }
        }

        private string _sweating = ""; // 汗出
        public string Sweating
        {
            get => _sweating;
            set
            {
                if (_sweating != value)
                {
                    _sweating = value;
                    OnPropertyChanged(nameof(Sweating));
                }
            }
        }

        private string _stools = ""; // 二便
        public string Stools
        {
            get => _stools;
            set
            {
                if (_stools != value)
                {
                    _stools = value;
                    OnPropertyChanged(nameof(Stools));
                }
            }
        }

        #endregion

        #region 切诊属性

        private string _pulse = ""; // 脉象
        public string Pulse
        {
            get => _pulse;
            set
            {
                if (_pulse != value)
                {
                    _pulse = value;
                    OnPropertyChanged(nameof(Pulse));
                }
            }
        }

        private string _pulseRate = ""; // 脉率
        public string PulseRate
        {
            get => _pulseRate;
            set
            {
                if (_pulseRate != value)
                {
                    _pulseRate = value;
                    OnPropertyChanged(nameof(PulseRate));
                }
            }
        }

        private string _pulseStrength = ""; // 脉力
        public string PulseStrength
        {
            get => _pulseStrength;
            set
            {
                if (_pulseStrength != value)
                {
                    _pulseStrength = value;
                    OnPropertyChanged(nameof(PulseStrength));
                }
            }
        }

        private string _abdomen = ""; // 腹诊
        public string Abdomen
        {
            get => _abdomen;
            set
            {
                if (_abdomen != value)
                {
                    _abdomen = value;
                    OnPropertyChanged(nameof(Abdomen));
                }
            }
        }

        #endregion

        #region 综合诊断属性

        private string _syndrome = ""; // 证候
        public string Syndrome
        {
            get => _syndrome;
            set
            {
                if (_syndrome != value)
                {
                    _syndrome = value;
                    OnPropertyChanged(nameof(Syndrome));
                }
            }
        }

        private string _treatmentPrinciple = ""; // 治法
        public string TreatmentPrinciple
        {
            get => _treatmentPrinciple;
            set
            {
                if (_treatmentPrinciple != value)
                {
                    _treatmentPrinciple = value;
                    OnPropertyChanged(nameof(TreatmentPrinciple));
                }
            }
        }

        #endregion

        #region 常用选项 (来自合并的服务)

        // 望诊选项
        public List<string> ComplexionOptions { get; } = new()
        {
            "红润", "苍白", "萎黄", "青黑", "潮红", "面赤", "面青", "面黄", "面黑", "面白"
        };

        public List<string> SpiritOptions { get; } = new()
        {
            "神清气爽", "精神萎靡", "烦躁不安", "神昏", "神疲乏力", "精神振奋"
        };

        public List<string> BodyShapeOptions { get; } = new()
        {
            "正常", "肥胖", "消瘦", "浮肿", "腹胀", "驼背", "佝偻"
        };

        public List<string> TongueBodyOptions { get; } = new()
        {
            "淡红", "红", "绛", "淡白", "紫暗", "胖大", "瘦小", "有齿痕", "光滑", "粗糙"
        };

        public List<string> TongueCoatingOptions { get; } = new()
        {
            "薄白", "厚白", "薄黄", "厚黄", "白腻", "黄腻", "少苔", "无苔", "剥苔", "花剥苔"
        };

        public List<string> SkinConditionOptions { get; } = new()
        {
            "正常", "干燥", "油腻", "粗糙", "细腻", "有皮疹", "瘙痒", "色素沉着"
        };

        // 闻诊选项
        public List<string> VoiceOptions { get; } = new()
        {
            "洪亮", "低沉", "嘶哑", "微弱", "高亢", "断续", "正常"
        };

        public List<string> BreathingOptions { get; } = new()
        {
            "平稳", "急促", "微弱", "喘息", "气短", "呼吸困难", "正常"
        };

        public List<string> CoughOptions { get; } = new()
        {
            "无咳嗽", "干咳", "咳痰", "咳血", "夜间咳嗽", "晨起咳嗽", "阵发性咳嗽"
        };

        public List<string> BodyOdorOptions { get; } = new()
        {
            "无异味", "汗臭", "腥臭", "腐臭", "酸臭", "甜腻味", "其他异味"
        };

        public List<string> BreathOdorOptions { get; } = new()
        {
            "无异味", "口臭", "酸味", "腥味", "腐败味", "甜腻味", "其他异味"
        };

        // 问诊选项
        public List<string> ColdHeatOptions { get; } = new()
        {
            "正常", "畏寒", "发热", "寒热往来", "身热不扬", "五心烦热"
        };

        public List<string> SweatingOptions { get; } = new()
        {
            "正常", "无汗", "自汗", "盗汗", "大汗", "手足汗", "头汗"
        };

        public List<string> StoolsOptions { get; } = new()
        {
            "正常", "便秘", "腹泻", "便溏", "小便频", "小便少", "尿黄", "尿清"
        };

        // 切诊选项
        public List<string> PulseOptions { get; } = new()
        {
            "平脉", "浮脉", "沉脉", "迟脉", "数脉", "滑脉", "涩脉", "弦脉", "紧脉", "缓脉"
        };

        public List<string> PulseRateOptions { get; } = new()
        {
            "正常(60-90)", "缓慢(<60)", "快速(>90)", "不规则"
        };

        public List<string> PulseStrengthOptions { get; } = new()
        {
            "有力", "无力", "微弱", "洪大", "细小"
        };

        public List<string> AbdomenOptions { get; } = new()
        {
            "正常", "胀满", "疼痛", "拒按", "喜按", "包块", "积水"
        };

        #endregion

        #region 分析方法

        /// <summary>
        /// 获取四诊综合分析结果
        /// </summary>
        public TCMDiagnosisAnalysis GetComprehensiveAnalysis()
        {
            var analysis = new TCMDiagnosisAnalysis();

            // 望诊分析 (来自 TCMInspectionService)
            analysis.InspectionAnalysis = GetInspectionAnalysis();
            
            // 闻诊分析 (来自 TCMAuscultationService)
            analysis.AuscultationAnalysis = GetAuscultationAnalysis();
            
            // 问诊分析
            analysis.InquiryAnalysis = GetInquiryAnalysis();
            
            // 切诊分析
            analysis.PalpationAnalysis = GetPalpationAnalysis();
            
            // 综合评估
            analysis.OverallAssessment = GetOverallAssessment(analysis);

            return analysis;
        }

        /// <summary>
        /// 望诊分析
        /// </summary>
        private InspectionAnalysis GetInspectionAnalysis()
        {
            var analysis = new InspectionAnalysis();
            
            analysis.ComplexionAnalysis = AnalyzeComplexion(_complexion);
            analysis.SpiritAnalysis = AnalyzeSpirit(_spirit);
            analysis.TongueAnalysis = AnalyzeTongue(_tongueBody, _tongueCoating);
            analysis.OverallAssessment = GetInspectionOverallAssessment(analysis);

            return analysis;
        }

        /// <summary>
        /// 闻诊分析
        /// </summary>
        private AuscultationAnalysis GetAuscultationAnalysis()
        {
            var analysis = new AuscultationAnalysis();
            
            analysis.VoiceAnalysis = AnalyzeVoice(_voice);
            analysis.BreathingAnalysis = AnalyzeBreathing(_breathing);
            analysis.CoughAnalysis = AnalyzeCough(_cough);
            analysis.OdorAnalysis = AnalyzeOdor(_bodyOdor, _breathOdor);
            analysis.OverallAssessment = GetAuscultationOverallAssessment(analysis);

            return analysis;
        }

        /// <summary>
        /// 问诊分析
        /// </summary>
        private InquiryAnalysis GetInquiryAnalysis()
        {
            var analysis = new InquiryAnalysis();
            
            analysis.ChiefComplaintAnalysis = AnalyzeChiefComplaint(_chiefComplaint);
            analysis.ColdHeatAnalysis = AnalyzeColdHeat(_coldHeat);
            analysis.SweatingAnalysis = AnalyzeSweating(_sweating);
            analysis.StoolsAnalysis = AnalyzeStools(_stools);

            return analysis;
        }

        /// <summary>
        /// 切诊分析
        /// </summary>
        private PalpationAnalysis GetPalpationAnalysis()
        {
            var analysis = new PalpationAnalysis();
            
            analysis.PulseAnalysis = AnalyzePulse(_pulse, _pulseRate, _pulseStrength);
            analysis.AbdomenAnalysis = AnalyzeAbdomen(_abdomen);

            return analysis;
        }

        #endregion

        #region 具体分析方法 (来自原服务)

        private string AnalyzeComplexion(string complexion)
        {
            return complexion switch
            {
                "红润" => "气血充足，健康状态良好",
                "苍白" => "可能存在血虚、阳虚或失血",
                "萎黄" => "脾胃虚弱，营养不良",
                "青黑" => "寒证、血瘀或肾虚",
                "潮红" => "阴虚火旺或热证",
                _ => "需要结合其他症状综合分析"
            };
        }

        private string AnalyzeSpirit(string spirit)
        {
            return spirit switch
            {
                "神清气爽" => "精神状态良好，脏腑功能正常",
                "精神萎靡" => "可能存在气虚、血虚或脏腑功能低下",
                "烦躁不安" => "可能有热证、痰热或肝火上炎",
                "神昏" => "严重病证，需要紧急处理",
                _ => "需要进一步观察"
            };
        }

        private string AnalyzeTongue(string tongueBody, string tongueCoating)
        {
            var bodyAnalysis = tongueBody switch
            {
                "淡红" => "正常舌体，气血调和",
                "红" => "热证",
                "绛" => "热盛伤阴",
                "淡白" => "阳虚或血虚",
                "紫暗" => "血瘀证",
                _ => "舌体正常"
            };

            var coatingAnalysis = tongueCoating switch
            {
                "薄白" => "正常舌苔或表证",
                "厚白" => "痰湿或食积",
                "薄黄" => "热证初起",
                "厚黄" => "热证较重",
                "腻苔" => "痰湿重",
                _ => "舌苔正常"
            };

            return $"{bodyAnalysis}；{coatingAnalysis}";
        }

        private string AnalyzeVoice(string voice)
        {
            return voice switch
            {
                "洪亮" => "声音洪亮，肺气充足，体质较好",
                "低沉" => "可能肾气不足或体质虚弱",
                "嘶哑" => "肺阴不足或声带问题",
                "微弱" => "气虚明显，体质虚弱",
                "高亢" => "可能有热证或情志激动",
                _ => "声音正常"
            };
        }

        private string AnalyzeBreathing(string breathing)
        {
            return breathing switch
            {
                "平稳" => "呼吸正常，肺功能良好",
                "急促" => "可能有热证、痰热或肺热",
                "微弱" => "肺气虚，体质虚弱",
                "喘息" => "肺气不足或有痰阻",
                "气短" => "肺气虚或心气不足",
                _ => "呼吸基本正常"
            };
        }

        private string AnalyzeCough(string cough)
        {
            return cough switch
            {
                "无咳嗽" => "无咳嗽症状",
                "干咳" => "肺阴不足或燥热伤肺",
                "咳痰" => "痰湿阻肺，需要化痰",
                "咳血" => "肺热伤络，需要紧急处理",
                "夜间咳嗽" => "可能阴虚火旺",
                "晨起咳嗽" => "可能痰湿重",
                _ => "咳嗽情况需要进一步观察"
            };
        }

        private string AnalyzeOdor(string bodyOdor, string breathOdor)
        {
            var findings = new List<string>();

            if (!string.IsNullOrEmpty(bodyOdor) && bodyOdor != "无异味")
            {
                var bodyAnalysis = bodyOdor switch
                {
                    "汗臭" => "汗液分泌旺盛，可能湿热重",
                    "腥臭" => "可能有寒湿或肾阳虚",
                    "腐臭" => "可能有内热或感染",
                    "酸臭" => "可能肝胃不和",
                    _ => $"体味{bodyOdor}，需要结合其他症状分析"
                };
                findings.Add($"体味：{bodyAnalysis}");
            }

            if (!string.IsNullOrEmpty(breathOdor) && breathOdor != "无异味")
            {
                var breathAnalysis = breathOdor switch
                {
                    "口臭" => "可能胃火重或消化不良",
                    "酸味" => "可能胃酸过多或肝胃不和",
                    "腥味" => "可能有寒证或肾虚",
                    "腐败味" => "可能有胃肠积滞",
                    _ => $"口气{breathOdor}，需要进一步检查"
                };
                findings.Add($"口气：{breathAnalysis}");
            }

            return findings.Count > 0 
                ? string.Join("；", findings)
                : "气味正常";
        }

        private string AnalyzeChiefComplaint(string chiefComplaint)
        {
            if (string.IsNullOrWhiteSpace(chiefComplaint))
                return "未记录主诉";

            return $"主诉：{chiefComplaint}";
        }

        private string AnalyzeColdHeat(string coldHeat)
        {
            return coldHeat switch
            {
                "畏寒" => "阳虚或表虚证",
                "发热" => "热证或表证",
                "寒热往来" => "少阳证或肝胆问题",
                "身热不扬" => "湿热证",
                "五心烦热" => "阴虚内热",
                _ => "寒热平衡"
            };
        }

        private string AnalyzeSweating(string sweating)
        {
            return sweating switch
            {
                "无汗" => "表实证或津液不足",
                "自汗" => "表虚或气虚证",
                "盗汗" => "阴虚内热证",
                "大汗" => "阳明热证或脱证",
                "手足汗" => "脾胃湿热或血虚",
                _ => "汗出正常"
            };
        }

        private string AnalyzeStools(string stools)
        {
            return stools switch
            {
                "便秘" => "热证或津液不足",
                "腹泻" => "脾虚或湿热证",
                "便溏" => "脾虚湿盛",
                "小便频" => "膀胱湿热或肾阳虚",
                "小便少" => "肾阳虚或水湿内停",
                "尿黄" => "热证或湿热下注",
                "尿清" => "肾阳虚或寒证",
                _ => "二便正常"
            };
        }

        private string AnalyzePulse(string pulse, string pulseRate, string pulseStrength)
        {
            var findings = new List<string>();

            if (!string.IsNullOrWhiteSpace(pulse))
            {
                var pulseAnalysis = pulse switch
                {
                    "浮脉" => "表证或虚阳外浮",
                    "沉脉" => "里证或气血不足",
                    "迟脉" => "寒证或脏腑功能低下",
                    "数脉" => "热证或阴虚火旺",
                    "滑脉" => "痰湿或食积",
                    "涩脉" => "血瘀或精血不足",
                    "弦脉" => "肝胆病或痰饮",
                    "紧脉" => "寒证或疼痛",
                    _ => $"脉象{pulse}"
                };
                findings.Add(pulseAnalysis);
            }

            if (!string.IsNullOrWhiteSpace(pulseStrength))
            {
                findings.Add($"脉力{pulseStrength}");
            }

            return findings.Count > 0 
                ? string.Join("，", findings)
                : "脉象平和";
        }

        private string AnalyzeAbdomen(string abdomen)
        {
            return abdomen switch
            {
                "胀满" => "气滞或脾胃虚弱",
                "疼痛" => "气血瘀滞或寒凝",
                "拒按" => "实证或瘀血内停",
                "喜按" => "虚证或脾胃虚寒",
                "包块" => "瘀血内停或痰湿凝聚",
                "积水" => "脾肾阳虚或水湿内停",
                _ => "腹诊正常"
            };
        }

        #endregion

        #region 综合评估方法

        private string GetInspectionOverallAssessment(InspectionAnalysis analysis)
        {
            var findings = new List<string>();
            
            if (!string.IsNullOrEmpty(analysis.ComplexionAnalysis))
                findings.Add($"面色：{analysis.ComplexionAnalysis}");
            
            if (!string.IsNullOrEmpty(analysis.SpiritAnalysis))
                findings.Add($"神态：{analysis.SpiritAnalysis}");
            
            if (!string.IsNullOrEmpty(analysis.TongueAnalysis))
                findings.Add($"舌诊：{analysis.TongueAnalysis}");

            return findings.Count > 0 
                ? string.Join("；", findings)
                : "望诊基本正常";
        }

        private string GetAuscultationOverallAssessment(AuscultationAnalysis analysis)
        {
            var findings = new List<string>();
            
            if (!string.IsNullOrEmpty(analysis.VoiceAnalysis))
                findings.Add($"声音：{analysis.VoiceAnalysis}");
            
            if (!string.IsNullOrEmpty(analysis.BreathingAnalysis))
                findings.Add($"呼吸：{analysis.BreathingAnalysis}");
            
            if (!string.IsNullOrEmpty(analysis.CoughAnalysis))
                findings.Add($"咳嗽：{analysis.CoughAnalysis}");

            if (!string.IsNullOrEmpty(analysis.OdorAnalysis))
                findings.Add(analysis.OdorAnalysis);

            return findings.Count > 0 
                ? string.Join("；", findings)
                : "闻诊基本正常";
        }

        private string GetOverallAssessment(TCMDiagnosisAnalysis analysis)
        {
            var findings = new List<string>();

            if (analysis.InspectionAnalysis?.OverallAssessment != "望诊基本正常")
                findings.Add($"望诊：{analysis.InspectionAnalysis?.OverallAssessment}");

            if (analysis.AuscultationAnalysis?.OverallAssessment != "闻诊基本正常")
                findings.Add($"闻诊：{analysis.AuscultationAnalysis?.OverallAssessment}");

            if (analysis.InquiryAnalysis != null)
            {
                var inquiryFindings = new List<string>();
                if (!string.IsNullOrEmpty(analysis.InquiryAnalysis.ColdHeatAnalysis) && analysis.InquiryAnalysis.ColdHeatAnalysis != "寒热平衡")
                    inquiryFindings.Add(analysis.InquiryAnalysis.ColdHeatAnalysis);
                if (!string.IsNullOrEmpty(analysis.InquiryAnalysis.SweatingAnalysis) && analysis.InquiryAnalysis.SweatingAnalysis != "汗出正常")
                    inquiryFindings.Add(analysis.InquiryAnalysis.SweatingAnalysis);
                if (inquiryFindings.Count > 0)
                    findings.Add($"问诊：{string.Join("，", inquiryFindings)}");
            }

            if (analysis.PalpationAnalysis?.PulseAnalysis != "脉象平和")
                findings.Add($"切诊：{analysis.PalpationAnalysis?.PulseAnalysis}");

            return findings.Count > 0 
                ? string.Join("；", findings)
                : "四诊合参基本正常，建议继续观察";
        }

        #endregion

        #region 数据操作方法

        /// <summary>
        /// 从 TCMFourDiagnosisData 加载数据
        /// </summary>
        public void LoadFromData(TCMFourDiagnosisData data)
        {
            if (data == null) return;

            // 望诊数据
            Complexion = data.Complexion;
            Spirit = data.Spirit;
            BodyShape = data.Build;
            TongueBody = data.TongueBody;
            TongueCoating = data.TongueCoating;

            // 闻诊数据
            Voice = data.Voice;
            Breathing = data.Breathing;
            Cough = data.Cough;

            // 问诊数据
            ChiefComplaint = data.ChiefComplaint;
            PresentIllness = data.PresentIllness;
            PastHistory = data.PastHistory;
            ColdHeat = data.ColdHeat;
            Sweating = data.Sweating;
            Stools = data.Stools;

            // 切诊数据
            Pulse = data.Pulse;
            PulseRate = data.PulseRate;
            PulseStrength = data.PulseStrength;
            Abdomen = data.Abdomen;

            // 综合诊断
            Syndrome = data.Syndrome;
            TreatmentPrinciple = data.TreatmentPrinciple;
        }

        /// <summary>
        /// 转换为 TCMFourDiagnosisData
        /// </summary>
        public TCMFourDiagnosisData ToData()
        {
            return new TCMFourDiagnosisData
            {
                // 望诊数据
                Complexion = Complexion,
                Spirit = Spirit,
                Build = BodyShape,
                TongueBody = TongueBody,
                TongueCoating = TongueCoating,

                // 闻诊数据
                Voice = Voice,
                Breathing = Breathing,
                Cough = Cough,

                // 问诊数据
                ChiefComplaint = ChiefComplaint,
                PresentIllness = PresentIllness,
                PastHistory = PastHistory,
                ColdHeat = ColdHeat,
                Sweating = Sweating,
                Stools = Stools,

                // 切诊数据
                Pulse = Pulse,
                PulseRate = PulseRate,
                PulseStrength = PulseStrength,
                Abdomen = Abdomen,

                // 综合诊断
                Syndrome = Syndrome,
                TreatmentPrinciple = TreatmentPrinciple
            };
        }

        /// <summary>
        /// 重置所有四诊数据
        /// </summary>
        public void Reset()
        {
            // 望诊数据
            Complexion = "";
            Spirit = "";
            BodyShape = "";
            TongueBody = "";
            TongueCoating = "";
            SkinCondition = "";

            // 闻诊数据
            Voice = "";
            Breathing = "";
            Cough = "";
            BodyOdor = "";
            BreathOdor = "";

            // 问诊数据
            ChiefComplaint = "";
            PresentIllness = "";
            PastHistory = "";
            ColdHeat = "";
            Sweating = "";
            Stools = "";

            // 切诊数据
            Pulse = "";
            PulseRate = "";
            PulseStrength = "";
            Abdomen = "";

            // 综合诊断
            Syndrome = "";
            TreatmentPrinciple = "";
        }

        /// <summary>
        /// 验证四诊数据完整性
        /// </summary>
        public bool IsDataValid()
        {
            return !string.IsNullOrWhiteSpace(Complexion) || 
                   !string.IsNullOrWhiteSpace(Spirit) || 
                   !string.IsNullOrWhiteSpace(TongueBody) ||
                   !string.IsNullOrWhiteSpace(Voice) || 
                   !string.IsNullOrWhiteSpace(Breathing) || 
                   !string.IsNullOrWhiteSpace(ChiefComplaint) ||
                   !string.IsNullOrWhiteSpace(Pulse);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region 分析结果类

    /// <summary>
    /// 四诊综合分析结果
    /// </summary>
    public class TCMDiagnosisAnalysis
    {
        public InspectionAnalysis? InspectionAnalysis { get; set; }
        public AuscultationAnalysis? AuscultationAnalysis { get; set; }
        public InquiryAnalysis? InquiryAnalysis { get; set; }
        public PalpationAnalysis? PalpationAnalysis { get; set; }
        public string OverallAssessment { get; set; } = "";
    }

    /// <summary>
    /// 望诊分析结果 (来自 TCMInspectionService)
    /// </summary>
    public class InspectionAnalysis
    {
        public string ComplexionAnalysis { get; set; } = "";
        public string SpiritAnalysis { get; set; } = "";
        public string TongueAnalysis { get; set; } = "";
        public string OverallAssessment { get; set; } = "";
    }

    /// <summary>
    /// 闻诊分析结果 (来自 TCMAuscultationService)
    /// </summary>
    public class AuscultationAnalysis
    {
        public string VoiceAnalysis { get; set; } = "";
        public string BreathingAnalysis { get; set; } = "";
        public string CoughAnalysis { get; set; } = "";
        public string OdorAnalysis { get; set; } = "";
        public string OverallAssessment { get; set; } = "";
    }

    /// <summary>
    /// 问诊分析结果
    /// </summary>
    public class InquiryAnalysis
    {
        public string ChiefComplaintAnalysis { get; set; } = "";
        public string ColdHeatAnalysis { get; set; } = "";
        public string SweatingAnalysis { get; set; } = "";
        public string StoolsAnalysis { get; set; } = "";
    }

    /// <summary>
    /// 切诊分析结果
    /// </summary>
    public class PalpationAnalysis
    {
        public string PulseAnalysis { get; set; } = "";
        public string AbdomenAnalysis { get; set; } = "";
    }

    #endregion
}