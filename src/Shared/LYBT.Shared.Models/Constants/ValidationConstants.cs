namespace LYBT.Shared.Models.Constants
{
    /// <summary>
    /// 全局验证常量定义 - 统一管理所有DTO的验证规则
    /// </summary>
    public static class ValidationConstants
    {
        #region 通用长度限制

        /// <summary>用户名最小长度</summary>
        public const int UsernameMinLength = 3;

        /// <summary>用户名最大长度</summary>
        public const int UsernameMaxLength = 32;

        /// <summary>密码最小长度</summary>
        public const int PasswordMinLength = 6;

        /// <summary>密码最大长度</summary>
        public const int PasswordMaxLength = 128;

        /// <summary>名称字段最大长度（如真实姓名、患者姓名等）</summary>
        public const int NameMaxLength = 50;

        /// <summary>简短名称最大长度（如药材名称、验方名称）</summary>
        public const int ShortNameMaxLength = 100;

        /// <summary>长名称最大长度（如机构名称、详细名称）</summary>
        public const int LongNameMaxLength = 200;

        /// <summary>手机号码最大长度</summary>
        public const int PhoneMaxLength = 20;

        /// <summary>邮箱最大长度</summary>
        public const int EmailMaxLength = 100;

        /// <summary>地址最大长度</summary>
        public const int AddressMaxLength = 200;

        /// <summary>URL最大长度</summary>
        public const int UrlMaxLength = 500;

        /// <summary>备注最大长度</summary>
        public const int RemarkMaxLength = 500;

        /// <summary>长备注最大长度</summary>
        public const int LongRemarkMaxLength = 1000;

        /// <summary>描述最大长度</summary>
        public const int DescriptionMaxLength = 1000;

        /// <summary>长描述最大长度</summary>
        public const int LongDescriptionMaxLength = 2000;

        /// <summary>代码字段最大长度（如拼音码、五笔码）</summary>
        public const int CodeMaxLength = 50;

        /// <summary>用法说明最大长度</summary>
        public const int UsageMaxLength = 200;

        /// <summary>诊断最大长度</summary>
        public const int DiagnosisMaxLength = 500;

        #endregion

        #region 数值范围限制

        /// <summary>年龄最小值</summary>
        public const int AgeMinValue = 0;

        /// <summary>年龄最大值</summary>
        public const int AgeMaxValue = 150;

        /// <summary>价格最小值</summary>
        public const decimal PriceMinValue = 0m;

        /// <summary>价格最大值</summary>
        public const decimal PriceMaxValue = 999999.99m;

        /// <summary>数量最小值</summary>
        public const decimal QuantityMinValue = 0.01m;

        /// <summary>数量最大值</summary>
        public const decimal QuantityMaxValue = 9999.99m;

        /// <summary>药材用量最小值（克）</summary>
        public const decimal HerbDoseMinValue = 0.1m;

        /// <summary>药材用量最大值（克）</summary>
        public const decimal HerbDoseMaxValue = 1000m;

        /// <summary>处方剂数最小值</summary>
        public const int PrescriptionDoseMinCount = 1;

        /// <summary>处方剂数最大值</summary>
        public const int PrescriptionDoseMaxCount = 100;

        /// <summary>折扣最小值</summary>
        public const decimal DiscountMinValue = 0m;

        /// <summary>折扣最大值</summary>
        public const decimal DiscountMaxValue = 1m;

        /// <summary>库存最小值</summary>
        public const int StockMinValue = 0;

        /// <summary>库存最大值</summary>
        public const int StockMaxValue = 999999;

        /// <summary>排序值最小值</summary>
        public const int SortOrderMinValue = 0;

        /// <summary>排序值最大值</summary>
        public const int SortOrderMaxValue = 9999;

        #endregion

        #region 分页限制

        /// <summary>默认页大小</summary>
        public const int DefaultPageSize = 20;

        /// <summary>最小页大小</summary>
        public const int MinPageSize = 1;

        /// <summary>最大页大小</summary>
        public const int MaxPageSize = 100;

        /// <summary>导出最大记录数</summary>
        public const int MaxExportRecords = 10000;

        #endregion

        #region 正则表达式

        /// <summary>用户名正则表达式（字母、数字、下划线）</summary>
        public const string UsernameRegex = @"^[a-zA-Z0-9_]+$";

        /// <summary>手机号正则表达式（中国大陆）</summary>
        public const string PhoneRegex = @"^1[3-9]\d{9}$";

        /// <summary>身份证号正则表达式（18位）</summary>
        public const string IdCardRegex = @"^\d{17}[\dXx]$";

        /// <summary>邮政编码正则表达式</summary>
        public const string PostalCodeRegex = @"^\d{6}$";

        /// <summary>拼音码正则表达式（大写字母）</summary>
        public const string PinYinCodeRegex = @"^[A-Z]+$";

        /// <summary>五笔码正则表达式（小写字母）</summary>
        public const string WuBiCodeRegex = @"^[a-z]+$";

        #endregion

        #region 验证错误消息

        /// <summary>必填字段错误消息</summary>
        public const string RequiredErrorMessage = "{0}不能为空";

        /// <summary>字符串长度错误消息</summary>
        public const string StringLengthErrorMessage = "{0}长度必须在{2}-{1}个字符之间";

        /// <summary>最大长度错误消息</summary>
        public const string MaxLengthErrorMessage = "{0}长度不能超过{1}个字符";

        /// <summary>最小长度错误消息</summary>
        public const string MinLengthErrorMessage = "{0}长度不能少于{1}个字符";

        /// <summary>范围错误消息</summary>
        public const string RangeErrorMessage = "{0}必须在{1}-{2}之间";

        /// <summary>正则表达式错误消息</summary>
        public const string RegexErrorMessage = "{0}格式不正确";

        /// <summary>邮箱格式错误消息</summary>
        public const string EmailErrorMessage = "邮箱格式不正确";

        /// <summary>电话格式错误消息</summary>
        public const string PhoneErrorMessage = "电话号码格式不正确";

        /// <summary>比较错误消息</summary>
        public const string CompareErrorMessage = "两次输入的{0}不一致";

        /// <summary>唯一性错误消息</summary>
        public const string UniqueErrorMessage = "{0}已存在";

        #endregion
    }
}