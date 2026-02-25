namespace LYBT.Shared.Primitives.Validation
{
    /// <summary>
    /// 验证常量类 - 统一管理所有验证规则
    /// </summary>
    /// <remarks>
    /// 设计目标：
    /// 1. 集中管理所有验证规则，避免硬编码重复
    /// 2. 提高可维护性，修改验证规则只需改一处
    /// 3. 提供清晰的文档说明每个常量的用途
    ///
    /// 相关文档：
    /// - 设计文档：docs/explanation/fluentvalidation-unified-design.md
    /// - 任务文档：docs/tasks/fluentvalidation-unified-tasks.md
    /// - GitHub Epic：Issue #1961
    ///
    /// OpenSpec: consolidate-shared-utilities - 移至Primitives作为基础层
    /// </remarks>
    public static class ValidationConstants
    {
        #region 长度限制常量

        /// <summary>
        /// 通用名称最大长度（如患者姓名、医生姓名、药品名称等）
        /// </summary>
        public const int NameMaxLength = 100;

        /// <summary>
        /// 短备注最大长度（如处方备注、医案备注等）
        /// </summary>
        public const int RemarkMaxLength = 1000;

        /// <summary>
        /// 长备注最大长度（如详细诊疗记录、医案详情等）
        /// </summary>
        public const int LongRemarkMaxLength = 2000;

        /// <summary>
        /// 地址最大长度
        /// </summary>
        public const int AddressMaxLength = 200;

        /// <summary>
        /// 手机号码最大长度
        /// </summary>
        public const int PhoneMaxLength = 20;

        /// <summary>
        /// 身份证号最大长度（中国身份证号为18位）
        /// </summary>
        public const int IdCardMaxLength = 18;

        /// <summary>
        /// 用户名最大长度
        /// </summary>
        public const int UserNameMaxLength = 50;

        /// <summary>
        /// 密码最大长度
        /// </summary>
        public const int PasswordMaxLength = 100;

        /// <summary>
        /// 药品编码最大长度
        /// </summary>
        public const int HerbCodeMaxLength = 50;

        /// <summary>
        /// 处方编号最大长度
        /// </summary>
        public const int PrescriptionNumberMaxLength = 50;

        /// <summary>
        /// 代码字段最大长度（如拼音码、五笔码、分类码）
        /// </summary>
        public const int CodeMaxLength = 50;

        /// <summary>
        /// 用法说明最大长度
        /// </summary>
        public const int UsageMaxLength = 500;

        /// <summary>
        /// 诊断最大长度（如中医诊断、舌诊、脉诊）
        /// </summary>
        public const int DiagnosisMaxLength = 500;

        /// <summary>
        /// 四诊最大长度（现病史、望闻问切综合）
        /// </summary>
        public const int FourDiagnosisMaxLength = 2000;

        #endregion

        #region 数值范围常量

        /// <summary>
        /// 剂数最小值（处方至少1剂）
        /// </summary>
        public const int DosageCountMinValue = 1;

        /// <summary>
        /// 剂数最大值（处方最多100剂）
        /// </summary>
        public const int DosageCountMaxValue = 100;

        /// <summary>
        /// 药品剂量最小值（克）
        /// </summary>
        public const decimal HerbDosageMinValue = 0.1m;

        /// <summary>
        /// 药品剂量最大值（克）
        /// </summary>
        public const decimal HerbDosageMaxValue = 1000m;

        /// <summary>
        /// 价格最小值（元）
        /// </summary>
        public const decimal PriceMinValue = 0.01m;

        /// <summary>
        /// 价格最大值（元）
        /// </summary>
        public const decimal PriceMaxValue = 100000m;

        /// <summary>
        /// 年龄最小值
        /// </summary>
        public const int AgeMinValue = 0;

        /// <summary>
        /// 年龄最大值
        /// </summary>
        public const int AgeMaxValue = 200;

        #endregion

        #region 正则表达式常量

        /// <summary>
        /// 中国身份证号正则表达式（18位，最后一位可以是X）
        /// </summary>
        public const string IdCardRegex = @"^\d{17}[\dXx]$";

        /// <summary>
        /// 中国手机号正则表达式（1开头，第二位为3-9，共11位）
        /// </summary>
        public const string PhoneRegex = @"^1[3-9]\d{9}$";

        /// <summary>
        /// 邮箱正则表达式
        /// </summary>
        public const string EmailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

        #endregion

        #region 错误消息常量

        /// <summary>
        /// 必填字段错误消息
        /// </summary>
        public const string RequiredErrorMessage = "{PropertyName}不能为空";

        /// <summary>
        /// 最大长度错误消息模板
        /// </summary>
        public const string MaxLengthErrorMessage = "{PropertyName}长度不能超过{MaxLength}个字符";

        /// <summary>
        /// 数值范围错误消息模板
        /// </summary>
        public const string RangeErrorMessage = "{PropertyName}必须在{From}到{To}之间";

        /// <summary>
        /// 正则表达式匹配错误消息
        /// </summary>
        public const string RegexErrorMessage = "{PropertyName}格式不正确";

        /// <summary>
        /// 身份证号格式错误消息
        /// </summary>
        public const string IdCardFormatErrorMessage = "身份证号格式不正确，应为18位数字，最后一位可以是X";

        /// <summary>
        /// 手机号格式错误消息
        /// </summary>
        public const string PhoneFormatErrorMessage = "手机号格式不正确，应为11位数字，以1开头";

        /// <summary>
        /// 邮箱格式错误消息
        /// </summary>
        public const string EmailFormatErrorMessage = "邮箱格式不正确";

        /// <summary>
        /// 集合为空错误消息
        /// </summary>
        public const string CollectionEmptyErrorMessage = "{PropertyName}不能为空";

        /// <summary>
        /// 集合项数量错误消息模板
        /// </summary>
        public const string CollectionCountErrorMessage = "{PropertyName}数量必须在{MinCount}到{MaxCount}之间";

        #endregion

        #region 业务规则常量

        /// <summary>
        /// 处方明细最小条目数（至少1条药品）
        /// </summary>
        public const int PrescriptionDetailsMinCount = 1;

        /// <summary>
        /// 处方明细最大条目数（最多50条药品）
        /// </summary>
        public const int PrescriptionDetailsMaxCount = 50;

        /// <summary>
        /// 默认剂数（默认为3剂）
        /// </summary>
        public const int DefaultDosageCount = 3;

        #endregion
    }
}
