using FluentValidation;

namespace LYBT.Desktop.Core.Validation {

    /// <summary>
    /// 通用验证规则扩展
    /// </summary>
    public static class CommonValidatorExtensions {

        /// <summary>
        /// 验证中文姓名
        /// </summary>
        public static IRuleBuilderOptions<T, string?> ChineseName<T>(this IRuleBuilder<T, string?> ruleBuilder) {
            return ruleBuilder
                .NotEmpty().WithMessage("姓名不能为空")
                .Length(2, 10).WithMessage("姓名长度应在2-10个字符之间")
                .Matches(@"^[\u4e00-\u9fa5]+$").WithMessage("姓名只能包含中文字符");
        }

        /// <summary>
        /// 验证手机号码
        /// </summary>
        public static IRuleBuilderOptions<T, string?> PhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder) {
            return ruleBuilder
                .NotEmpty().WithMessage("手机号不能为空")
                .Matches(@"^1[3-9]\d{9}$").WithMessage("请输入正确的手机号码");
        }

        /// <summary>
        /// 验证身份证号
        /// </summary>
        public static IRuleBuilderOptions<T, string?> IdCardNumber<T>(this IRuleBuilder<T, string?> ruleBuilder) {
            return ruleBuilder
                .NotEmpty().WithMessage("身份证号不能为空")
                .Length(18).WithMessage("身份证号必须是18位")
                .Must(BeValidIdCard).WithMessage("请输入有效的身份证号");
        }

        /// <summary>
        /// 验证年龄范围
        /// </summary>
        public static IRuleBuilderOptions<T, int> AgeRange<T>(this IRuleBuilder<T, int> ruleBuilder, int min = 0, int max = 150) {
            return ruleBuilder
                .InclusiveBetween(min, max).WithMessage($"年龄必须在{min}到{max}岁之间");
        }

        /// <summary>
        /// 验证密码强度
        /// </summary>
        public static IRuleBuilderOptions<T, string?> StrongPassword<T>(this IRuleBuilder<T, string?> ruleBuilder) {
            return ruleBuilder
                .NotEmpty().WithMessage("密码不能为空")
                .MinimumLength(8).WithMessage("密码长度至少8位")
                .Matches(@"[A-Z]").WithMessage("密码必须包含至少一个大写字母")
                .Matches(@"[a-z]").WithMessage("密码必须包含至少一个小写字母")
                .Matches(@"[0-9]").WithMessage("密码必须包含至少一个数字")
                .Matches(@"[!@#$%^&*]").WithMessage("密码必须包含至少一个特殊字符");
        }

        /// <summary>
        /// 验证用户名
        /// </summary>
        public static IRuleBuilderOptions<T, string?> Username<T>(this IRuleBuilder<T, string?> ruleBuilder) {
            return ruleBuilder
                .NotEmpty().WithMessage("用户名不能为空")
                .Length(3, 20).WithMessage("用户名长度应在3-20个字符之间")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("用户名只能包含字母、数字和下划线");
        }

        /// <summary>
        /// 验证邮箱
        /// </summary>
        public static IRuleBuilderOptions<T, string?> Email<T>(this IRuleBuilder<T, string?> ruleBuilder) {
            return ruleBuilder
                .EmailAddress().When(x => !string.IsNullOrEmpty(x as string))
                .WithMessage("请输入有效的邮箱地址");
        }

        /// <summary>
        /// 验证金额
        /// </summary>
        public static IRuleBuilderOptions<T, decimal> Money<T>(this IRuleBuilder<T, decimal> ruleBuilder, decimal max = 999999.99m) {
            return ruleBuilder
                .GreaterThanOrEqualTo(0).WithMessage("金额不能为负数")
                .LessThanOrEqualTo(max).WithMessage($"金额不能超过{max:C}")
                .PrecisionScale(10, 2, true).WithMessage("金额最多保留两位小数");
        }

        /// <summary>
        /// 验证日期不在未来
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> NotInFuture<T>(this IRuleBuilder<T, DateTime> ruleBuilder) {
            return ruleBuilder
                .LessThanOrEqualTo(DateTime.Now).WithMessage("日期不能超过当前时间");
        }

        /// <summary>
        /// 验证日期在合理范围内
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> ReasonableDate<T>(this IRuleBuilder<T, DateTime> ruleBuilder) {
            var minDate = new DateTime(1900, 1, 1);
            var maxDate = DateTime.Now.AddYears(1);

            return ruleBuilder
                .InclusiveBetween(minDate, maxDate)
                .WithMessage($"日期必须在{minDate:yyyy-MM-dd}到{maxDate:yyyy-MM-dd}之间");
        }

        /// <summary>
        /// 验证身份证号码有效性
        /// </summary>
        private static bool BeValidIdCard(string? idCard) {
            if (string.IsNullOrWhiteSpace(idCard) || idCard.Length != 18) {
                return false;
            }

            // 检查前17位是否都是数字
            if (!idCard.Substring(0, 17).All(char.IsDigit)) {
                return false;
            }

            // 验证日期部分
            var year = idCard.Substring(6, 4);
            var month = idCard.Substring(10, 2);
            var day = idCard.Substring(12, 2);

            if (!DateTime.TryParse($"{year}-{month}-{day}", out var date)) {
                return false;
            }

            if (date > DateTime.Now || date < new DateTime(1900, 1, 1)) {
                return false;
            }

            // 验证校验码
            var weights = new[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            var checkCodes = new[] { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

            var sum = 0;
            for (var i = 0; i < 17; i++) {
                sum += (idCard[i] - '0') * weights[i];
            }

            var checkCode = checkCodes[sum % 11];
            return idCard[17] == checkCode || (idCard[17] == 'x' && checkCode == 'X');
        }
    }

    /// <summary>
    /// 登录表单验证器
    /// </summary>
    public class LoginValidator : AbstractValidator<LoginFormModel> {

        public LoginValidator() {
            RuleFor(x => x.Username)
                .Username();

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("密码不能为空")
                .MinimumLength(6).WithMessage("密码长度至少6位");
        }
    }

    /// <summary>
    /// 患者信息验证器
    /// </summary>
    public class PatientValidator : AbstractValidator<PatientModel> {

        public PatientValidator() {
            RuleFor(x => x.Name)
                .ChineseName();

            RuleFor(x => x.Age)
                .AgeRange(0, 150);

            RuleFor(x => x.Phone)
                .PhoneNumber()
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.IdCard)
                .IdCardNumber()
                .When(x => !string.IsNullOrEmpty(x.IdCard));

            RuleFor(x => x.Address)
                .MaximumLength(200).WithMessage("地址长度不能超过200个字符")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }

    /// <summary>
    /// 处方验证器
    /// </summary>
    public class PrescriptionValidator : AbstractValidator<PrescriptionModel> {

        public PrescriptionValidator() {
            RuleFor(x => x.PatientId)
                .NotEmpty().WithMessage("请选择患者");

            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("医生信息缺失");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("处方不能为空")
                .Must(items => items != null && items.Any())
                .WithMessage("处方至少包含一味药材");

            RuleForEach(x => x.Items).ChildRules(item => {
                item.RuleFor(i => i.HerbName)
                    .NotEmpty().WithMessage("药材名称不能为空");

                item.RuleFor(i => i.Dosage)
                    .GreaterThan(0).WithMessage("剂量必须大于0")
                    .LessThanOrEqualTo(500).WithMessage("单味药剂量不能超过500g");

                item.RuleFor(i => i.Price)
                    .Money(9999.99m);
            });

            RuleFor(x => x.TotalAmount)
                .Money(99999.99m);

            RuleFor(x => x.Days)
                .InclusiveBetween(1, 30).WithMessage("处方天数应在1-30天之间");
        }
    }

    /// <summary>
    /// 药材验证器
    /// </summary>
    public class HerbValidator : AbstractValidator<HerbModel> {

        public HerbValidator() {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("药材名称不能为空")
                .Length(2, 50).WithMessage("药材名称长度应在2-50个字符之间");

            RuleFor(x => x.PinYin)
                .Matches(@"^[a-zA-Z]+$").WithMessage("拼音只能包含字母")
                .When(x => !string.IsNullOrEmpty(x.PinYin));

            RuleFor(x => x.Price)
                .Money(9999.99m);

            RuleFor(x => x.Unit)
                .NotEmpty().WithMessage("单位不能为空")
                .Must(unit => new[] { "g", "kg", "个", "片", "粒", "支", "包" }.Contains(unit))
                .WithMessage("单位格式不正确");

            RuleFor(x => x.Stock)
                .GreaterThanOrEqualTo(0).WithMessage("库存不能为负数")
                .When(x => x.Stock.HasValue);
        }
    }

    // 示例模型类（实际应该从Models文件夹引用）
    public class LoginFormModel {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class PatientModel {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Phone { get; set; }
        public string? IdCard { get; set; }
        public string? Address { get; set; }
    }

    public class PrescriptionModel {
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public List<PrescriptionItemModel>? Items { get; set; }
        public decimal TotalAmount { get; set; }
        public int Days { get; set; }
    }

    public class PrescriptionItemModel {
        public string? HerbName { get; set; }
        public decimal Dosage { get; set; }
        public decimal Price { get; set; }
    }

    public class HerbModel {
        public string? Name { get; set; }
        public string? PinYin { get; set; }
        public decimal Price { get; set; }
        public string? Unit { get; set; }
        public int? Stock { get; set; }
    }
}
