using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Consultation.Components
{
    /// <summary>
    /// 工作流步骤 - UltraThink v2.0 简化版
    /// 定义诊疗工作流中的各个步骤
    /// </summary>
    public class WorkflowStep
    {
        /// <summary>步骤ID</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>步骤名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>步骤标题</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>步骤描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>步骤顺序</summary>
        public int Order { get; set; }

        /// <summary>步骤类型</summary>
        public WorkflowStepType StepType { get; set; }

        /// <summary>是否必需</summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>是否完成</summary>
        public bool IsCompleted { get; set; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>前置步骤列表</summary>
        public List<string> Prerequisites { get; set; } = new();

        /// <summary>验证规则</summary>
        public List<string> ValidationRules { get; set; } = new();

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>完成时间</summary>
        public DateTime? CompletedTime { get; set; }

        /// <summary>备注</summary>
        public string Remark { get; set; } = string.Empty;
    }

    /// <summary>
    /// 工作流步骤类型枚举
    /// </summary>
    public enum WorkflowStepType
    {
        /// <summary>望诊</summary>
        Inspection = 1,

        /// <summary>闻诊</summary>
        Auscultation = 2,

        /// <summary>问诊</summary>
        Inquiry = 3,

        /// <summary>切诊</summary>
        Palpation = 4,

        /// <summary>诊断</summary>
        Diagnosis = 5,

        /// <summary>处方</summary>
        Prescription = 6,

        /// <summary>其他</summary>
        Other = 99
    }
}