using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 中医望诊服务
    /// 负责处理面色、体态、舌诊等望诊相关数据和分析
    /// </summary>
    public class TCMInspectionService : INotifyPropertyChanged
    {
        #region 望诊属性

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

        #region 常用选项

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

        #endregion

        #region 分析方法

        /// <summary>
        /// 获取望诊综合分析结果
        /// </summary>
        public InspectionAnalysis GetAnalysis()
        {
            var analysis = new InspectionAnalysis();

            // 面色分析
            analysis.ComplexionAnalysis = AnalyzeComplexion(_complexion);
            
            // 神态分析
            analysis.SpiritAnalysis = AnalyzeSpirit(_spirit);
            
            // 舌诊分析
            analysis.TongueAnalysis = AnalyzeTongue(_tongueBody, _tongueCoating);
            
            // 综合评估
            analysis.OverallAssessment = GetOverallAssessment(analysis);

            return analysis;
        }

        /// <summary>
        /// 分析面色
        /// </summary>
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

        /// <summary>
        /// 分析神态
        /// </summary>
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

        /// <summary>
        /// 分析舌诊
        /// </summary>
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

        /// <summary>
        /// 综合评估
        /// </summary>
        private string GetOverallAssessment(InspectionAnalysis analysis)
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
                : "望诊基本正常，建议结合其他诊断方法";
        }

        /// <summary>
        /// 重置所有望诊数据
        /// </summary>
        public void Reset()
        {
            Complexion = "";
            Spirit = "";
            BodyShape = "";
            TongueBody = "";
            TongueCoating = "";
            SkinCondition = "";
        }

        /// <summary>
        /// 验证望诊数据完整性
        /// </summary>
        public bool IsDataValid()
        {
            return !string.IsNullOrWhiteSpace(Complexion) || 
                   !string.IsNullOrWhiteSpace(Spirit) || 
                   !string.IsNullOrWhiteSpace(TongueBody);
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
    /// 望诊分析结果
    /// </summary>
    public class InspectionAnalysis
    {
        public string ComplexionAnalysis { get; set; } = "";
        public string SpiritAnalysis { get; set; } = "";
        public string TongueAnalysis { get; set; } = "";
        public string OverallAssessment { get; set; } = "";
    }
}