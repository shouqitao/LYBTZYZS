using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 中医闻诊服务
    /// 负责处理听声音、嗅气味等闻诊相关数据和分析
    /// </summary>
    public class TCMAuscultationService : INotifyPropertyChanged
    {
        #region 闻诊属性

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

        #region 常用选项

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

        #endregion

        #region 分析方法

        /// <summary>
        /// 获取闻诊综合分析结果
        /// </summary>
        public AuscultationAnalysis GetAnalysis()
        {
            var analysis = new AuscultationAnalysis();

            // 声音分析
            analysis.VoiceAnalysis = AnalyzeVoice(_voice);
            
            // 呼吸分析
            analysis.BreathingAnalysis = AnalyzeBreathing(_breathing);
            
            // 咳嗽分析
            analysis.CoughAnalysis = AnalyzeCough(_cough);
            
            // 气味分析
            analysis.OdorAnalysis = AnalyzeOdor(_bodyOdor, _breathOdor);
            
            // 综合评估
            analysis.OverallAssessment = GetOverallAssessment(analysis);

            return analysis;
        }

        /// <summary>
        /// 分析声音
        /// </summary>
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

        /// <summary>
        /// 分析呼吸
        /// </summary>
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

        /// <summary>
        /// 分析咳嗽
        /// </summary>
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

        /// <summary>
        /// 分析气味
        /// </summary>
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

        /// <summary>
        /// 综合评估
        /// </summary>
        private string GetOverallAssessment(AuscultationAnalysis analysis)
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

        /// <summary>
        /// 重置所有闻诊数据
        /// </summary>
        public void Reset()
        {
            Voice = "";
            Breathing = "";
            Cough = "";
            BodyOdor = "";
            BreathOdor = "";
        }

        /// <summary>
        /// 验证闻诊数据完整性
        /// </summary>
        public bool IsDataValid()
        {
            return !string.IsNullOrWhiteSpace(Voice) || 
                   !string.IsNullOrWhiteSpace(Breathing) || 
                   !string.IsNullOrWhiteSpace(Cough);
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

    /// <summary>
    /// 闻诊分析结果
    /// </summary>
    public class AuscultationAnalysis
    {
        public string VoiceAnalysis { get; set; } = "";
        public string BreathingAnalysis { get; set; } = "";
        public string CoughAnalysis { get; set; } = "";
        public string OdorAnalysis { get; set; } = "";
        public string OverallAssessment { get; set; } = "";
    }
}