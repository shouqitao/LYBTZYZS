using Bogus;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Herbs.Tests.Base
{
    /// <summary>
    /// 中药材测试数据生成器
    /// </summary>
    public static class HerbTestDataGenerator
    {
        private static readonly string[] HerbNames = {
            "人参", "黄芪", "当归", "白术", "茯苓", "甘草", "陈皮", "半夏",
            "生姜", "大枣", "川芎", "白芍", "熟地黄", "枸杞子", "菊花",
            "金银花", "连翘", "板蓝根", "蒲公英", "鱼腥草"
        };

        private static readonly string[] Origins = {
            "吉林", "河北", "甘肃", "四川", "云南", "广西", "山东", "河南",
            "安徽", "浙江", "江苏", "湖北", "湖南", "广东", "福建"
        };

        private static readonly string[] Units = { "克", "两", "钱", "斤", "公斤" };

        /// <summary>
        /// 中药材数据生成器
        /// </summary>
        public static Faker<Herb> HerbGenerator => new Faker<Herb>("zh_CN")
            .RuleFor(h => h.Id, f => Guid.NewGuid())
            .RuleFor(h => h.Name, f => f.PickRandom(HerbNames))
            .RuleFor(h => h.PinYinCode, (f, h) => GetPinyinCode(h.Name))
            .RuleFor(h => h.Origin, f => f.PickRandom(Origins))
            .RuleFor(h => h.Spec, f => f.PickRandom("特级", "一级", "二级", "三级", "统货"))
            .RuleFor(h => h.Unit, f => f.PickRandom(Units))
            .RuleFor(h => h.Price, f => f.Random.Decimal(1, 1000))
            .RuleFor(h => h.CostPrice, (f, h) => h.Price * f.Random.Decimal(0.5m, 0.8m))
            .RuleFor(h => h.Effect, f => f.Lorem.Sentence())
            .RuleFor(h => h.Usage, f => f.Lorem.Sentence())
            .RuleFor(h => h.Remark, f => f.Lorem.Sentence())
            .RuleFor(h => h.Status, f => f.PickRandom<CommonStatus>());

        /// <summary>
        /// 创建测试中药材
        /// </summary>
        public static Herb CreateTestHerb(
            string? name = null,
            decimal? price = null,
            string? unit = null,
            CommonStatus status = CommonStatus.Enabled)
        {
            var herb = HerbGenerator.Generate();

            if (!string.IsNullOrEmpty(name))
                herb.Name = name;

            if (price.HasValue)
                herb.Price = price.Value;

            if (!string.IsNullOrEmpty(unit))
                herb.Unit = unit;

            herb.Status = status;

            return herb;
        }

        /// <summary>
        /// 批量创建测试中药材
        /// </summary>
        public static List<Herb> CreateTestHerbs(int count, CommonStatus? status = null)
        {
            var generator = HerbGenerator;

            if (status.HasValue)
                generator = generator.RuleFor(h => h.Status, status.Value);

            // 确保名称唯一性
            var herbs = generator.Generate(count);
            for (int i = 0; i < herbs.Count; i++)
            {
                herbs[i].Name = $"{herbs[i].Name}_{i + 1}";
            }

            return herbs;
        }

        /// <summary>
        /// 创建启用的测试中药材
        /// </summary>
        public static Herb CreateEnabledHerb()
        {
            return CreateTestHerb(status: CommonStatus.Enabled);
        }

        /// <summary>
        /// 创建禁用的测试中药材
        /// </summary>
        public static Herb CreateDisabledHerb()
        {
            return CreateTestHerb(status: CommonStatus.Disabled);
        }

        /// <summary>
        /// 创建具有特定名称的中药材
        /// </summary>
        public static Herb CreateHerbWithName(string name)
        {
            return CreateTestHerb(name: name);
        }

        /// <summary>
        /// 创建具有特定价格的中药材
        /// </summary>
        public static Herb CreateHerbWithPrice(decimal price)
        {
            return CreateTestHerb(price: price);
        }

        /// <summary>
        /// 创建高价中药材（价格 > 100）
        /// </summary>
        public static Herb CreateExpensiveHerb()
        {
            return CreateTestHerb(price: 150.00m);
        }

        /// <summary>
        /// 创建低价中药材（价格 < 20）
        /// </summary>
        public static Herb CreateCheapHerb()
        {
            return CreateTestHerb(price: 10.00m);
        }

        /// <summary>
        /// 简单的拼音码生成（用于测试）
        /// </summary>
        private static string GetPinyinCode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // 简化的拼音码映射（仅用于测试）
            var pinyinMap = new Dictionary<string, string>
            {
                { "人参", "RC" }, { "黄芪", "HQ" }, { "当归", "DG" }, { "白术", "BS" },
                { "茯苓", "FL" }, { "甘草", "GC" }, { "陈皮", "CP" }, { "半夏", "BX" },
                { "生姜", "SJ" }, { "大枣", "DZ" }, { "川芎", "CX" }, { "白芍", "BS" },
                { "熟地黄", "SDH" }, { "枸杞子", "GQZ" }, { "菊花", "JH" },
                { "金银花", "JYH" }, { "连翘", "LQ" }, { "板蓝根", "BLG" },
                { "蒲公英", "PGY" }, { "鱼腥草", "YXC" }
            };

            return pinyinMap.TryGetValue(name, out var pinyin) ? pinyin :
                   string.Join(string.Empty, name.Take(Math.Min(name.Length, 6)).Select(c => char.ToUpper(c)));
        }
    }
}
