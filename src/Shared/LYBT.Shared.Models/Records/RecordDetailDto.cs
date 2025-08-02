using System;
using System.Collections.Generic;

namespace LYBT.Shared.Models.Records
{
    /// <summary>
    /// 病例详情DTO
    /// </summary>
    public class RecordDetailDto : RecordDto
    {
        /// <summary>家族史</summary>
        public string? FamilyHistory { get; set; }

        /// <summary>个人史</summary>
        public string? PersonalHistory { get; set; }

        /// <summary>月经史（女性）</summary>
        public string? MenstrualHistory { get; set; }

        /// <summary>婚育史</summary>
        public string? MaritalHistory { get; set; }

        /// <summary>望诊</summary>
        public string? Inspection { get; set; }

        /// <summary>闻诊</summary>
        public string? Auscultation { get; set; }

        /// <summary>问诊</summary>
        public string? Inquiry { get; set; }

        /// <summary>切诊</summary>
        public string? Palpation { get; set; }

        /// <summary>舌诊</summary>
        public string? TongueExamination { get; set; }

        /// <summary>脉诊</summary>
        public string? PulseExamination { get; set; }

        /// <summary>辨证</summary>
        public string? SyndromeDifferentiation { get; set; }

        /// <summary>治法</summary>
        public string? TreatmentPrinciple { get; set; }

        /// <summary>辅助检查</summary>
        public List<AuxiliaryExamination> AuxiliaryExaminations { get; set; } = new();

        /// <summary>图片附件</summary>
        public List<RecordAttachment> Attachments { get; set; } = new();

        /// <summary>随访记录</summary>
        public List<FollowUpRecord> FollowUps { get; set; } = new();
    }

    /// <summary>
    /// 辅助检查
    /// </summary>
    public class AuxiliaryExamination
    {
        /// <summary>检查项目</summary>
        public string ExaminationItem { get; set; } = string.Empty;

        /// <summary>检查结果</summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>检查日期</summary>
        public DateTime ExaminationDate { get; set; }
    }

    /// <summary>
    /// 病例附件
    /// </summary>
    public class RecordAttachment
    {
        /// <summary>附件ID</summary>
        public Guid Id { get; set; }

        /// <summary>文件名</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>文件路径</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>文件类型</summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>文件大小</summary>
        public long FileSize { get; set; }

        /// <summary>上传时间</summary>
        public DateTime UploadTime { get; set; }
    }

    /// <summary>
    /// 随访记录
    /// </summary>
    public class FollowUpRecord
    {
        /// <summary>随访ID</summary>
        public Guid Id { get; set; }

        /// <summary>随访时间</summary>
        public DateTime FollowUpTime { get; set; }

        /// <summary>随访内容</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>随访医生</summary>
        public string DoctorName { get; set; } = string.Empty;
    }
}