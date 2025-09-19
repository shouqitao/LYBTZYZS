using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LYBT.Shared.Models.Enums
{
    /// <summary>
    /// 前端客户端专用枚举定义 - 集中管理避免重复定义
    /// 注意：这些枚举主要用于前端UI和事件处理，不涉及后端API数据传输
    /// </summary>

    #region 数据刷新和事件相关

    /// <summary>
    /// 数据刷新类型枚举 - 前端事件系统专用
    /// </summary>
    [Description("数据刷新类型")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataRefreshType
    {
        /// <summary>无刷新</summary>
        [Description("无刷新")]
        None = 0,

        /// <summary>全部刷新</summary>
        [Description("全部刷新")]
        Full = 1,

        /// <summary>增量刷新</summary>
        [Description("增量刷新")]
        Incremental = 2,

        /// <summary>局部刷新</summary>
        [Description("局部刷新")]
        Partial = 3,

        /// <summary>选择性刷新</summary>
        [Description("选择性刷新")]
        Selective = 4
    }

    /// <summary>
    /// 数据刷新范围枚举 - 前端数据管理专用
    /// </summary>
    [Description("数据刷新范围")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataRefreshScope
    {
        /// <summary>当前视图</summary>
        [Description("当前视图")]
        CurrentView = 0,

        /// <summary>所有视图</summary>
        [Description("所有视图")]
        AllViews = 1,

        /// <summary>相关模块</summary>
        [Description("相关模块")]
        RelatedModules = 2,

        /// <summary>全局范围</summary>
        [Description("全局范围")]
        Global = 3
    }

    #endregion

    #region 状态消息和通知相关

    /// <summary>
    /// 状态消息类型枚举 - 前端状态显示专用
    /// </summary>
    [Description("状态消息类型")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StatusMessageType
    {
        /// <summary>信息</summary>
        [Description("信息")]
        Info = 0,

        /// <summary>成功</summary>
        [Description("成功")]
        Success = 1,

        /// <summary>警告</summary>
        [Description("警告")]
        Warning = 2,

        /// <summary>错误</summary>
        [Description("错误")]
        Error = 3,

        /// <summary>处理中</summary>
        [Description("处理中")]
        Processing = 4
    }

    /// <summary>
    /// 通知类型枚举 - 前端通知系统专用
    /// </summary>
    [Description("通知类型")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType
    {
        /// <summary>信息通知</summary>
        [Description("信息通知")]
        Information = 0,

        /// <summary>成功通知</summary>
        [Description("成功通知")]
        Success = 1,

        /// <summary>警告通知</summary>
        [Description("警告通知")]
        Warning = 2,

        /// <summary>错误通知</summary>
        [Description("错误通知")]
        Error = 3,

        /// <summary>确认通知</summary>
        [Description("确认通知")]
        Confirmation = 4,

        /// <summary>系统通知</summary>
        [Description("系统通知")]
        System = 5
    }

    #endregion

    #region 错误和验证相关

    /// <summary>
    /// 错误严重级别枚举 - 前端错误处理专用
    /// </summary>
    [Description("错误严重级别")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ErrorSeverity
    {
        /// <summary>信息</summary>
        [Description("信息")]
        Info = 0,

        /// <summary>警告</summary>
        [Description("警告")]
        Warning = 1,

        /// <summary>错误</summary>
        [Description("错误")]
        Error = 2,

        /// <summary>严重错误</summary>
        [Description("严重错误")]
        Critical = 3,

        /// <summary>致命错误</summary>
        [Description("致命错误")]
        Fatal = 4
    }

    /// <summary>
    /// 验证严重级别枚举 - 前端数据验证专用
    /// </summary>
    [Description("验证严重级别")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ValidationSeverity
    {
        /// <summary>信息</summary>
        [Description("信息")]
        Info = 0,

        /// <summary>警告</summary>
        [Description("警告")]
        Warning = 1,

        /// <summary>错误</summary>
        [Description("错误")]
        Error = 2,

        /// <summary>阻塞错误</summary>
        [Description("阻塞错误")]
        Blocking = 3
    }

    /// <summary>
    /// 验证错误级别枚举 - 前端验证显示专用
    /// </summary>
    [Description("验证错误级别")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ValidationErrorLevel
    {
        /// <summary>字段级别</summary>
        [Description("字段级别")]
        Field = 0,

        /// <summary>表单级别</summary>
        [Description("表单级别")]
        Form = 1,

        /// <summary>页面级别</summary>
        [Description("页面级别")]
        Page = 2,

        /// <summary>模块级别</summary>
        [Description("模块级别")]
        Module = 3
    }

    #endregion

    #region 验方和处方相关

    /// <summary>
    /// 验方合并模式枚举 - 前端验方处理专用
    /// </summary>
    [Description("验方合并模式")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FormulaMergeMode
    {
        /// <summary>替换模式</summary>
        [Description("替换模式")]
        Replace = 0,

        /// <summary>追加模式</summary>
        [Description("追加模式")]
        Append = 1,

        /// <summary>合并模式</summary>
        [Description("合并模式")]
        Merge = 2,

        /// <summary>智能合并</summary>
        [Description("智能合并")]
        SmartMerge = 3
    }

    #endregion

    #region 工作流和步骤相关

    /// <summary>
    /// 工作流步骤枚举 - 前端流程控制专用
    /// </summary>
    [Description("工作流步骤")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WorkflowStep
    {
        /// <summary>开始</summary>
        [Description("开始")]
        Start = 0,

        /// <summary>患者接待</summary>
        [Description("患者接待")]
        PatientReception = 1,

        /// <summary>病历录入</summary>
        [Description("病历录入")]
        MedicalRecordEntry = 2,

        /// <summary>医生诊断</summary>
        [Description("医生诊断")]
        DoctorDiagnosis = 3,

        /// <summary>处方开具</summary>
        [Description("处方开具")]
        PrescriptionWriting = 4,

        /// <summary>处方审核</summary>
        [Description("处方审核")]
        PrescriptionReview = 5,

        /// <summary>完成</summary>
        [Description("完成")]
        Completed = 6
    }

    /// <summary>
    /// 诊疗步骤枚举 - 前端诊疗流程专用
    /// </summary>
    [Description("诊疗步骤")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConsultationStep
    {
        /// <summary>望诊</summary>
        [Description("望诊")]
        Inspection = 1,

        /// <summary>闻诊</summary>
        [Description("闻诊")]
        Auscultation = 2,

        /// <summary>问诊</summary>
        [Description("问诊")]
        Inquiry = 3,

        /// <summary>切诊</summary>
        [Description("切诊")]
        Palpation = 4,

        /// <summary>辨证</summary>
        [Description("辨证")]
        Differentiation = 5,

        /// <summary>论治</summary>
        [Description("论治")]
        Treatment = 6
    }

    #endregion

    #region UI和用户体验相关

    /// <summary>
    /// 对话框类型枚举 - 前端UI专用
    /// </summary>
    [Description("对话框类型")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DialogType
    {
        /// <summary>信息对话框</summary>
        [Description("信息对话框")]
        Information = 0,

        /// <summary>确认对话框</summary>
        [Description("确认对话框")]
        Confirmation = 1,

        /// <summary>警告对话框</summary>
        [Description("警告对话框")]
        Warning = 2,

        /// <summary>错误对话框</summary>
        [Description("错误对话框")]
        Error = 3,

        /// <summary>输入对话框</summary>
        [Description("输入对话框")]
        Input = 4,

        /// <summary>选择对话框</summary>
        [Description("选择对话框")]
        Selection = 5
    }

    /// <summary>
    /// 按钮结果枚举 - 前端对话框专用
    /// </summary>
    [Description("按钮结果")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ButtonResult
    {
        /// <summary>无</summary>
        [Description("无")]
        None = 0,

        /// <summary>确定</summary>
        [Description("确定")]
        OK = 1,

        /// <summary>取消</summary>
        [Description("取消")]
        Cancel = 2,

        /// <summary>是</summary>
        [Description("是")]
        Yes = 3,

        /// <summary>否</summary>
        [Description("否")]
        No = 4,

        /// <summary>重试</summary>
        [Description("重试")]
        Retry = 5,

        /// <summary>忽略</summary>
        [Description("忽略")]
        Ignore = 6
    }

    /// <summary>
    /// 用户显示模式枚举 - 前端用户控件专用
    /// </summary>
    [Description("用户显示模式")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserDisplayMode
    {
        /// <summary>列表模式</summary>
        [Description("列表模式")]
        List = 0,

        /// <summary>卡片模式</summary>
        [Description("卡片模式")]
        Card = 1,

        /// <summary>详情模式</summary>
        [Description("详情模式")]
        Detail = 2,

        /// <summary>简洁模式</summary>
        [Description("简洁模式")]
        Compact = 3
    }

    #endregion

    #region 数据变更和同步相关

    /// <summary>
    /// 数据变更类型枚举 - 前端数据协调专用
    /// </summary>
    [Description("数据变更类型")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DataChangeType
    {
        /// <summary>创建</summary>
        [Description("创建")]
        Create = 0,

        /// <summary>更新</summary>
        [Description("更新")]
        Update = 1,

        /// <summary>删除</summary>
        [Description("删除")]
        Delete = 2,

        /// <summary>批量操作</summary>
        [Description("批量操作")]
        Batch = 3,

        /// <summary>同步操作</summary>
        [Description("同步操作")]
        Sync = 4
    }

    #endregion
}