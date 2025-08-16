using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Consultation.ViewModels;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 中医四诊数据解析器 - UltraThink重构专门组件
    /// 专门负责解析API数据并映射到数据属性
    /// </summary>
    public class TCMFourDiagnosisDataParser
    {
        #region 公共方法

        /// <summary>
        /// 从看诊详情中映射数据
        /// </summary>
        public void ParseFromConsultationDetail(ConsultationDetailDto detail, TCMFourDiagnosisDataManager dataManager)
        {
            if (detail == null || dataManager == null) return;

            // 解析望诊数据
            if (!string.IsNullOrEmpty(detail.Inspection))
            {
                ParseInspectionData(detail.Inspection, dataManager);
            }

            // 解析闻诊数据
            if (!string.IsNullOrEmpty(detail.AuscultationOlfaction))
            {
                ParseAuscultationData(detail.AuscultationOlfaction, dataManager);
            }

            // 解析问诊数据
            if (!string.IsNullOrEmpty(detail.Inquiry))
            {
                ParseInquiryData(detail.Inquiry, dataManager);
            }

            // 解析舌诊数据
            if (!string.IsNullOrEmpty(detail.TongueInspection))
            {
                ParseTongueData(detail.TongueInspection, dataManager);
            }

            // 解析脉诊数据
            if (!string.IsNullOrEmpty(detail.PulseCondition))
            {
                ParsePulseData(detail.PulseCondition, dataManager);
            }

            // 解析诊断数据
            dataManager.TCMSyndrome = detail.TCMDiagnosis ?? "";
            dataManager.TreatmentPrinciple = detail.TreatmentPrinciple ?? "";
        }

        #endregion

        #region 私有解析方法

        /// <summary>
        /// 解析望诊数据
        /// </summary>
        private void ParseInspectionData(string inspectionText, TCMFourDiagnosisDataManager dataManager)
        {
            if (string.IsNullOrWhiteSpace(inspectionText)) return;

            // 面色解析
            if (inspectionText.Contains("面色"))
            {
                foreach (var option in dataManager.ComplexionOptions)
                {
                    if (inspectionText.Contains(option))
                    {
                        dataManager.Complexion = option;
                        break;
                    }
                }
            }

            // 神态解析
            if (inspectionText.Contains("神"))
            {
                var patterns = new[] { "精神", "神疲", "神倦", "神清", "神志" };
                foreach (var pattern in patterns)
                {
                    if (inspectionText.Contains(pattern))
                    {
                        var startIndex = inspectionText.IndexOf(pattern);
                        var endIndex = FindNextSeparator(inspectionText, startIndex);
                        if (endIndex > startIndex)
                        {
                            dataManager.Spirit = inspectionText.Substring(startIndex, endIndex - startIndex).Trim();
                        }
                        break;
                    }
                }
            }

            // 形态解析
            if (inspectionText.Contains("形") || inspectionText.Contains("体"))
            {
                var patterns = new[] { "体型", "形体", "身材", "体态" };
                foreach (var pattern in patterns)
                {
                    if (inspectionText.Contains(pattern))
                    {
                        var startIndex = inspectionText.IndexOf(pattern);
                        var endIndex = FindNextSeparator(inspectionText, startIndex);
                        if (endIndex > startIndex)
                        {
                            dataManager.BodyShape = inspectionText.Substring(startIndex, endIndex - startIndex).Trim();
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 解析闻诊数据
        /// </summary>
        private void ParseAuscultationData(string auscultationText, TCMFourDiagnosisDataManager dataManager)
        {
            if (string.IsNullOrWhiteSpace(auscultationText)) return;

            // 声音解析
            if (auscultationText.Contains("声音"))
            {
                var startIndex = auscultationText.IndexOf("声音");
                var endIndex = FindNextSeparator(auscultationText, startIndex);
                if (endIndex > startIndex)
                {
                    dataManager.Voice = auscultationText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            // 呼吸解析
            if (auscultationText.Contains("呼吸"))
            {
                var startIndex = auscultationText.IndexOf("呼吸");
                var endIndex = FindNextSeparator(auscultationText, startIndex);
                if (endIndex > startIndex)
                {
                    dataManager.Breath = auscultationText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            // 咳嗽解析
            if (auscultationText.Contains("咳嗽"))
            {
                var startIndex = auscultationText.IndexOf("咳嗽");
                var endIndex = FindNextSeparator(auscultationText, startIndex);
                if (endIndex > startIndex)
                {
                    dataManager.Cough = auscultationText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            // 气味解析
            if (auscultationText.Contains("气味"))
            {
                var startIndex = auscultationText.IndexOf("气味");
                var endIndex = FindNextSeparator(auscultationText, startIndex);
                if (endIndex > startIndex)
                {
                    dataManager.Odor = auscultationText.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
        }

        /// <summary>
        /// 解析问诊数据
        /// </summary>
        private void ParseInquiryData(string inquiryText, TCMFourDiagnosisDataManager dataManager)
        {
            if (string.IsNullOrWhiteSpace(inquiryText)) return;

            var parts = inquiryText.Split('；', '，', ';', ',');
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                
                if (trimmedPart.Contains("主诉"))
                {
                    dataManager.ChiefComplaint = ExtractValue(trimmedPart, "主诉");
                }
                else if (trimmedPart.Contains("寒热"))
                {
                    dataManager.ColdHeat = ExtractValue(trimmedPart, "寒热");
                }
                else if (trimmedPart.Contains("汗"))
                {
                    dataManager.Sweat = ExtractValue(trimmedPart, "汗");
                }
                else if (trimmedPart.Contains("头身"))
                {
                    dataManager.HeadBody = ExtractValue(trimmedPart, "头身");
                }
                else if (trimmedPart.Contains("胸腹"))
                {
                    dataManager.ChestAbdomen = ExtractValue(trimmedPart, "胸腹");
                }
                else if (trimmedPart.Contains("饮食"))
                {
                    dataManager.Appetite = ExtractValue(trimmedPart, "饮食");
                }
                else if (trimmedPart.Contains("二便"))
                {
                    dataManager.StoolUrine = ExtractValue(trimmedPart, "二便");
                }
                else if (trimmedPart.Contains("睡眠"))
                {
                    dataManager.Sleep = ExtractValue(trimmedPart, "睡眠");
                }
                else if (trimmedPart.Contains("月经"))
                {
                    dataManager.Menstruation = ExtractValue(trimmedPart, "月经");
                }
            }
        }

        /// <summary>
        /// 解析舌诊数据
        /// </summary>
        private void ParseTongueData(string tongueText, TCMFourDiagnosisDataManager dataManager)
        {
            if (string.IsNullOrWhiteSpace(tongueText)) return;

            // 舌质解析
            foreach (var bodyOption in dataManager.TongueBodyOptions)
            {
                if (tongueText.Contains(bodyOption))
                {
                    dataManager.TongueBody = bodyOption;
                    break;
                }
            }

            // 舌苔解析
            foreach (var coatingOption in dataManager.TongueCoatingOptions)
            {
                if (tongueText.Contains(coatingOption))
                {
                    dataManager.TongueCoating = coatingOption;
                    break;
                }
            }
        }

        /// <summary>
        /// 解析脉诊数据
        /// </summary>
        private void ParsePulseData(string pulseText, TCMFourDiagnosisDataManager dataManager)
        {
            if (string.IsNullOrWhiteSpace(pulseText)) return;

            // 左脉解析
            if (pulseText.Contains("左脉"))
            {
                var leftPulseIndex = pulseText.IndexOf("左脉") + 2;
                if (leftPulseIndex < pulseText.Length)
                {
                    var leftPart = pulseText.Substring(leftPulseIndex);
                    var separators = new char[] { '、', '，', ',', '；', ';' };
                    
                    foreach (var separator in separators)
                    {
                        if (leftPart.Contains(separator))
                        {
                            leftPart = leftPart.Split(separator)[0];
                            break;
                        }
                    }
                    
                    dataManager.LeftPulse = leftPart.Trim();
                }
            }

            // 右脉解析
            if (pulseText.Contains("右脉"))
            {
                var rightPulseIndex = pulseText.IndexOf("右脉") + 2;
                if (rightPulseIndex < pulseText.Length)
                {
                    var rightPart = pulseText.Substring(rightPulseIndex);
                    var separators = new char[] { '、', '，', ',', '；', ';' };
                    
                    foreach (var separator in separators)
                    {
                        if (rightPart.Contains(separator))
                        {
                            rightPart = rightPart.Split(separator)[0];
                            break;
                        }
                    }
                    
                    dataManager.RightPulse = rightPart.Trim();
                }
            }

            // 脉率、脉律、脉力、脉形解析
            ParseSpecificPulseAttributes(pulseText, dataManager);
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 查找下一个分隔符位置
        /// </summary>
        private int FindNextSeparator(string text, int startIndex)
        {
            var separators = new char[] { '，', ',', '；', ';', '。', '.', ' ', '\n', '\r' };
            var minIndex = text.Length;

            foreach (var separator in separators)
            {
                var index = text.IndexOf(separator, startIndex);
                if (index > startIndex && index < minIndex)
                {
                    minIndex = index;
                }
            }

            return minIndex;
        }

        /// <summary>
        /// 提取键值对中的值
        /// </summary>
        private string ExtractValue(string text, string key)
        {
            var keyIndex = text.IndexOf(key);
            if (keyIndex == -1) return "";

            var colonIndex = text.IndexOf('：', keyIndex);
            if (colonIndex == -1) colonIndex = text.IndexOf(':', keyIndex);
            if (colonIndex == -1) return "";

            var startIndex = colonIndex + 1;
            if (startIndex >= text.Length) return "";

            return text.Substring(startIndex).Trim();
        }

        /// <summary>
        /// 解析特定脉象属性
        /// </summary>
        private void ParseSpecificPulseAttributes(string pulseText, TCMFourDiagnosisDataManager dataManager)
        {
            // 脉率解析
            if (pulseText.Contains("脉率"))
            {
                dataManager.PulseRate = ExtractValue(pulseText, "脉率");
            }

            // 脉律解析
            if (pulseText.Contains("脉律"))
            {
                dataManager.PulseRhythm = ExtractValue(pulseText, "脉律");
            }

            // 脉力解析
            if (pulseText.Contains("脉力"))
            {
                dataManager.PulseStrength = ExtractValue(pulseText, "脉力");
            }

            // 脉形解析
            if (pulseText.Contains("脉形"))
            {
                dataManager.PulseShape = ExtractValue(pulseText, "脉形");
            }
        }

        #endregion
    }
}