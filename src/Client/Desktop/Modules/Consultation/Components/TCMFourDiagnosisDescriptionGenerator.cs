using LYBT.Shared.Models.Contracts.Common;
using System.Linq;
using LYBT.Desktop.Consultation.ViewModels;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 中医四诊描述生成器 - UltraThink重构专门组件
    /// 专门负责生成各种诊断描述文字
    /// </summary>
    public class TCMFourDiagnosisDescriptionGenerator
    {
        #region 公共方法

        /// <summary>
        /// 获取完整的望诊描述
        /// </summary>
        public string GetInspectionDescription(TCMFourDiagnosisDataManager dataManager)
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(dataManager.Complexion) ? $"面色{dataManager.Complexion}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Spirit) ? $"神{dataManager.Spirit}" : null,
                !string.IsNullOrWhiteSpace(dataManager.BodyShape) ? $"形态{dataManager.BodyShape}" : null,
                !string.IsNullOrWhiteSpace(dataManager.TongueBody) ? $"舌质{dataManager.TongueBody}" : null,
                !string.IsNullOrWhiteSpace(dataManager.TongueCoating) ? $"苔{dataManager.TongueCoating}" : null
            };

            return string.Join("，", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取完整的闻诊描述
        /// </summary>
        public string GetAuscultationDescription(TCMFourDiagnosisDataManager dataManager)
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(dataManager.Voice) ? $"声音{dataManager.Voice}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Breath) ? $"呼吸{dataManager.Breath}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Cough) ? $"咳嗽{dataManager.Cough}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Odor) ? $"气味{dataManager.Odor}" : null
            };

            return string.Join("，", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取完整的问诊描述
        /// </summary>
        public string GetInquiryDescription(TCMFourDiagnosisDataManager dataManager)
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(dataManager.ChiefComplaint) ? $"主诉：{dataManager.ChiefComplaint}" : null,
                !string.IsNullOrWhiteSpace(dataManager.ColdHeat) ? $"寒热：{dataManager.ColdHeat}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Sweat) ? $"汗：{dataManager.Sweat}" : null,
                !string.IsNullOrWhiteSpace(dataManager.HeadBody) ? $"头身：{dataManager.HeadBody}" : null,
                !string.IsNullOrWhiteSpace(dataManager.ChestAbdomen) ? $"胸腹：{dataManager.ChestAbdomen}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Appetite) ? $"饮食：{dataManager.Appetite}" : null,
                !string.IsNullOrWhiteSpace(dataManager.StoolUrine) ? $"二便：{dataManager.StoolUrine}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Sleep) ? $"睡眠：{dataManager.Sleep}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Menstruation) ? $"月经：{dataManager.Menstruation}" : null
            };

            return string.Join("；", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取完整的切诊描述
        /// </summary>
        public string GetPalpationDescription(TCMFourDiagnosisDataManager dataManager)
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(dataManager.LeftPulse) ? $"左脉{dataManager.LeftPulse}" : null,
                !string.IsNullOrWhiteSpace(dataManager.RightPulse) ? $"右脉{dataManager.RightPulse}" : null,
                !string.IsNullOrWhiteSpace(dataManager.PulseRate) ? $"脉率{dataManager.PulseRate}" : null,
                !string.IsNullOrWhiteSpace(dataManager.PulseRhythm) ? $"脉律{dataManager.PulseRhythm}" : null,
                !string.IsNullOrWhiteSpace(dataManager.PulseStrength) ? $"脉力{dataManager.PulseStrength}" : null,
                !string.IsNullOrWhiteSpace(dataManager.PulseShape) ? $"脉形{dataManager.PulseShape}" : null,
                !string.IsNullOrWhiteSpace(dataManager.Palpation) ? $"按诊：{dataManager.Palpation}" : null
            };

            return string.Join("，", parts.Where(p => p != null));
        }

        /// <summary>
        /// 获取舌诊描述
        /// </summary>
        public string GetTongueInspectionDescription(TCMFourDiagnosisDataManager dataManager)
        {
            var tongueDescription = "";
            
            if (!string.IsNullOrWhiteSpace(dataManager.TongueBody) && 
                !string.IsNullOrWhiteSpace(dataManager.TongueCoating))
            {
                tongueDescription = $"舌质{dataManager.TongueBody}，苔{dataManager.TongueCoating}";
            }
            else if (!string.IsNullOrWhiteSpace(dataManager.TongueBody))
            {
                tongueDescription = $"舌质{dataManager.TongueBody}";
            }
            else if (!string.IsNullOrWhiteSpace(dataManager.TongueCoating))
            {
                tongueDescription = $"苔{dataManager.TongueCoating}";
            }

            return tongueDescription.Trim('，');
        }

        /// <summary>
        /// 获取脉象描述
        /// </summary>
        public string GetPulseConditionDescription(TCMFourDiagnosisDataManager dataManager)
        {
            var pulseDescription = "";

            if (!string.IsNullOrWhiteSpace(dataManager.LeftPulse) && 
                !string.IsNullOrWhiteSpace(dataManager.RightPulse))
            {
                pulseDescription = $"左脉{dataManager.LeftPulse}，右脉{dataManager.RightPulse}";
            }
            else if (!string.IsNullOrWhiteSpace(dataManager.LeftPulse))
            {
                pulseDescription = $"左脉{dataManager.LeftPulse}";
            }
            else if (!string.IsNullOrWhiteSpace(dataManager.RightPulse))
            {
                pulseDescription = $"右脉{dataManager.RightPulse}";
            }

            return pulseDescription.Trim('，');
        }

        /// <summary>
        /// 生成完整四诊描述
        /// </summary>
        public TCMFourDiagnosisData GenerateCompleteDescription(TCMFourDiagnosisDataManager dataManager)
        {
            return new TCMFourDiagnosisData
            {
                Inspection = GetInspectionDescription(dataManager),
                Auscultation = GetAuscultationDescription(dataManager),
                Inquiry = GetInquiryDescription(dataManager),
                Palpation = GetPalpationDescription(dataManager),
                TongueInspection = GetTongueInspectionDescription(dataManager),
                PulseCondition = GetPulseConditionDescription(dataManager)
            };
        }

        /// <summary>
        /// 生成诊断摘要
        /// </summary>
        public string GenerateDiagnosisSummary(TCMFourDiagnosisDataManager dataManager)
        {
            var parts = new[]
            {
                !string.IsNullOrWhiteSpace(dataManager.TCMSyndrome) ? $"证型：{dataManager.TCMSyndrome}" : null,
                !string.IsNullOrWhiteSpace(dataManager.TreatmentPrinciple) ? $"治法：{dataManager.TreatmentPrinciple}" : null
            };

            return string.Join("；", parts.Where(p => p != null));
        }

        #endregion
    }
}